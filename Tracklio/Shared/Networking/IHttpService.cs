using System;
using Tracklio.Shared.Networking.Requests;

namespace Tracklio.Shared.Networking;

public interface IHttpService
{
    Task<T> SendPostRequest<T, U>(PostRequest<U> request, Dictionary<string, string> headers);

    Task<T> SendGetRequest<T>(GetRequest request, Dictionary<string, string> headers);

    Task<T> SendMultipartPostRequest<T>(MultipartPostRequest request, Dictionary<string, string> headers);
}
