using FirebaseAdmin.Messaging;
using Tracklio.Shared.Domain.Dto.Notification;
using NotificationPriority = Tracklio.Shared.Domain.Enums.NotificationPriority;

namespace Tracklio.Shared.Services.Notification;

public class FirebaseService(ILogger<FirebaseService> logger) : IFirebaseService
{
        private readonly FirebaseMessaging _messaging = FirebaseMessaging.DefaultInstance;

        public async Task<NotificationResponse> SendNotificationAsync(SendNotificationRequest request)
        {
            try
            {
                var message = BuildMessage(request);
                message.Token = request.DeviceToken;

                var response = await _messaging.SendAsync(message);
                
                logger.LogInformation($"Successfully sent message: {response}");
                
                return new NotificationResponse 
                { 
                    Success = true, 
                    MessageId = response,
                    SuccessCount = 1
                };
            }
            catch (FirebaseMessagingException ex)
            {
                logger.LogError($"Firebase messaging error: {ex.Message}");
                return new NotificationResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
            catch (Exception ex)
            {
                logger.LogError($"Error sending notification: {ex.Message}");
                return new NotificationResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<NotificationResponse> SendBulkNotificationAsync(BulkNotificationRequest request)
        {
            try
            {
                var messages = request.DeviceTokens.Select(token => new Message
                {
                    Token = token,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = request.Title,
                        Body = request.Body,
                        ImageUrl = request.ImageUrl
                    },
                }).ToList();

                var response = await _messaging.SendEachAsync(messages);
                
                var failedTokens = new List<string>();
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        failedTokens.Add(request.DeviceTokens[i]);
                        logger.LogWarning($"Failed to send to token {request.DeviceTokens[i]}: {response.Responses[i].Exception?.Message}");
                    }
                }

                logger.LogInformation($"Bulk send completed. Success: {response.SuccessCount}, Failures: {response.FailureCount}");

                return new NotificationResponse
                {
                    Success = response.SuccessCount > 0,
                    SuccessCount = response.SuccessCount,
                    FailureCount = response.FailureCount,
                    FailedTokens = failedTokens
                };
            }
            catch (Exception ex)
            {
                logger.LogError($"Error sending bulk notification: {ex.Message}");
                return new NotificationResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<NotificationResponse> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string> data = null)
        {
            try
            {
                var message = new Message
                {
                    Topic = topic,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data ?? new Dictionary<string, string>()
                };

                var response = await _messaging.SendAsync(message);
                
                logger.LogInformation($"Successfully sent message to topic {topic}: {response}");
                
                return new NotificationResponse 
                { 
                    Success = true, 
                    MessageId = response,
                    SuccessCount = 1
                };
            }
            catch (Exception ex)
            {
                logger.LogError($"Error sending to topic {topic}: {ex.Message}");
                return new NotificationResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<NotificationResponse> SubscribeToTopicAsync(TopicSubscriptionRequest request)
        {
            try
            {
                var response = await _messaging.SubscribeToTopicAsync(request.DeviceTokens, request.Topic);
                
                logger.LogInformation($"Successfully subscribed {response.SuccessCount} tokens to topic {request.Topic}");
                
                return new NotificationResponse
                {
                    Success = response.SuccessCount > 0,
                    SuccessCount = response.SuccessCount,
                    FailureCount = response.FailureCount
                };
            }
            catch (Exception ex)
            {
                logger.LogError($"Error subscribing to topic {request.Topic}: {ex.Message}");
                return new NotificationResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<NotificationResponse> UnsubscribeFromTopicAsync(TopicSubscriptionRequest request)
        {
            try
            {
                var response = await _messaging.UnsubscribeFromTopicAsync(request.DeviceTokens, request.Topic);
                
                logger.LogInformation($"Successfully unsubscribed {response.SuccessCount} tokens from topic {request.Topic}");
                
                return new NotificationResponse
                {
                    Success = response.SuccessCount > 0,
                    SuccessCount = response.SuccessCount,
                    FailureCount = response.FailureCount
                };
            }
            catch (Exception ex)
            {
                logger.LogError($"Error unsubscribing from topic {request.Topic}: {ex.Message}");
                return new NotificationResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string deviceToken)
        {
            try
            {
                var message = new Message
                {
                    Token = deviceToken,
                    Data = new Dictionary<string, string> { { "test", "validation" } }
                };

                // This will throw an exception if the token is invalid
                await _messaging.SendAsync(message, dryRun: true);
                return true;
            }
            catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                logger.LogWarning($"Invalid token: {deviceToken}");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error validating token: {ex.Message}");
                return false;
            }
        }

        
        private Message BuildMessage(SendNotificationRequest request)
        {
            var message = new Message
            {
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = request.Title,
                    Body = request.Body,
                    ImageUrl = request.ImageUrl
                },
            };

            // Platform-specific configurations
            message.Android = new AndroidConfig
            {
                Priority = request.Priority == NotificationPriority.High ? Priority.High : Priority.Normal,
                Notification = new AndroidNotification
                {
                    ClickAction = request.ClickAction,
                    Icon = "ic_notification",
                    Color = "#FF6B35"
                }
            };

            message.Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["apns-priority"] = request.Priority == NotificationPriority.High ? "10" : "5"
                },
                Aps = new Aps
                {
                    Badge = 1,
                    Sound = "default"
                }
            };

            message.Webpush = new WebpushConfig
            {
                Notification = new WebpushNotification
                {
                    Icon = "/icon-192x192.png",
                    Badge = "/badge-72x72.png"
                }
            };

            return message;
        }
    }