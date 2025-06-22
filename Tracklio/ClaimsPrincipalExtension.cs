using System.Security.Claims;

namespace Tracklio;


    public static class ClaimsPrincipalExtensions
    {
        private const string UserIdClaim = ClaimTypes.NameIdentifier;
        private const string EmailClaim = ClaimTypes.Email;
        private const string NameClaim = ClaimTypes.Name;
        private const string RoleClaim = ClaimTypes.Role;
        private const string FirstNameClaim = ClaimTypes.GivenName;
        private const string LastNameClaim = ClaimTypes.Surname;
        private const string PhoneClaim = ClaimTypes.MobilePhone;
        
        private const string MotoristRole = "Motorist";
        private const string AdminRole = "Admin";
        private const string AuditorRole = "Auditor";

        /// <summary>
        /// Gets the user ID from claims
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>User ID as string, or null if not found</returns>
        public static string? GetUserId(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(UserIdClaim)?.Value;
        }

        /// <summary>
        /// Gets the user ID as a Guid
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>User ID as Guid, or null if not found or invalid</returns>
        public static Guid GetUserIdAsGuid(this ClaimsPrincipal principal)
        {
            var userIdString = principal.GetUserId();
            if (string.IsNullOrEmpty(userIdString))
                return Guid.Empty;

            return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
        }

        /// <summary>
        /// Gets the user's email address
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>Email address, or null if not found</returns>
        public static string? GetEmail(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(EmailClaim)?.Value;
        }

        /// <summary>
        /// Gets the user's full name
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>Full name, or null if not found</returns>
        public static string? GetName(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(NameClaim)?.Value;
        }

        /// <summary>
        /// Gets the user's first name
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>First name, or null if not found</returns>
        public static string? GetFirstName(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(FirstNameClaim)?.Value;
        }

        /// <summary>
        /// Gets the user's last name
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>Last name, or null if not found</returns>
        public static string? GetLastName(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(LastNameClaim)?.Value;
        }

        /// <summary>
        /// Gets the user's phone number
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>Phone number, or null if not found</returns>
        public static string? GetPhoneNumber(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(PhoneClaim)?.Value;
        }

        /// <summary>
        /// Gets all roles for the user
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>List of roles</returns>
        public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
        {
            return principal?.FindAll(RoleClaim)?.Select(c => c.Value) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Checks if the user is a Motorist
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>True if user is a Motorist</returns>
        public static bool IsMotorist(this ClaimsPrincipal principal)
        {
            return principal.IsInRole(MotoristRole);
        }

        /// <summary>
        /// Checks if the user is an Admin
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>True if user is an Admin</returns>
        public static bool IsAdmin(this ClaimsPrincipal principal)
        {
            return principal.IsInRole(AdminRole);
        }

        /// <summary>
        /// Checks if the user is an Auditor
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>True if user is an Auditor</returns>
        public static bool IsAuditor(this ClaimsPrincipal principal)
        {
            return principal.IsInRole(AuditorRole);
        }

        /// <summary>
        /// Checks if the user has admin privileges (Admin or Auditor)
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>True if user has admin privileges</returns>
        public static bool HasAdminPrivileges(this ClaimsPrincipal principal)
        {
            return principal.IsAdmin() || principal.IsAuditor();
        }

        /// <summary>
        /// Gets a specific claim value by type
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <param name="claimType">The claim type to retrieve</param>
        /// <returns>Claim value, or null if not found</returns>
        public static string? GetClaimValue(this ClaimsPrincipal principal, string claimType)
        {
            return principal?.FindFirst(claimType)?.Value;
        }

        /// <summary>
        /// Gets all claim values for a specific type
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <param name="claimType">The claim type to retrieve</param>
        /// <returns>All claim values for the specified type</returns>
        public static IEnumerable<string> GetClaimValues(this ClaimsPrincipal principal, string claimType)
        {
            return principal?.FindAll(claimType)?.Select(c => c.Value) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Checks if the user has a specific claim
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <param name="claimType">The claim type</param>
        /// <param name="claimValue">The claim value (optional)</param>
        /// <returns>True if the claim exists</returns>
        public static bool HasClaim(this ClaimsPrincipal principal, string claimType, string? claimValue = null)
        {
            if (principal == null) return false;

            return claimValue == null 
                ? principal.HasClaim(claimType) 
                : principal.HasClaim(claimType, claimValue);
        }

        /// <summary>
        /// Checks if the user is authenticated and has a valid user ID
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>True if user is authenticated with valid ID</returns>
        public static bool IsValidUser(this ClaimsPrincipal principal)
        {
            return principal?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(principal.GetUserId());
        }

        /// <summary>
        /// Gets user display name (tries Name first, then First + Last, then Email)
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>Display name for the user</returns>
        public static string GetDisplayName(this ClaimsPrincipal principal)
        {
            var name = principal.GetName();
            if (!string.IsNullOrEmpty(name))
                return name;

            var firstName = principal.GetFirstName();
            var lastName = principal.GetLastName();
            
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                return $"{firstName} {lastName}";
            
            if (!string.IsNullOrEmpty(firstName))
                return firstName;
            
            if (!string.IsNullOrEmpty(lastName))
                return lastName;

            return principal.GetEmail() ?? "Unknown User";
        }

        /// <summary>
        /// Creates a dictionary of all user claims for debugging/logging
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal</param>
        /// <returns>Dictionary of claim types and values</returns>
        public static Dictionary<string, object> GetAllClaims(this ClaimsPrincipal principal)
        {
            if (principal?.Claims == null)
                return new Dictionary<string, object>();

            var claims = new Dictionary<string, object>();
            
            foreach (var claim in principal.Claims)
            {
                if (claims.ContainsKey(claim.Type))
                {
                    // Handle multiple claims of the same type
                    if (claims[claim.Type] is List<string> existingList)
                    {
                        existingList.Add(claim.Value);
                    }
                    else
                    {
                        claims[claim.Type] = new List<string> { claims[claim.Type].ToString()!, claim.Value };
                    }
                }
                else
                {
                    claims[claim.Type] = claim.Value;
                }
            }

            return claims;
        }
    }
