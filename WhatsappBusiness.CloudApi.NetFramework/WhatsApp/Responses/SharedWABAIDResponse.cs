using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Responses
{
    public class SharedWABAIDResponse
    {
        [JsonPropertyName("data")]
        public SharedWABAIDData Data { get; set; }

        public string GetSharedWABAId()
        {
            return Data
                ?.GranularScopes
                ?.FirstOrDefault(x => x.Scope == "whatsapp_business_management" || x.Scope == "whatsapp_business_messaging")
                ?.TargetIds
                ?.FirstOrDefault();
        }

        public AccessTokenInformation GetAccessTokenInformation(string accessToken = null)
        {
            var wabaId = GetSharedWABAId();
            if (Data == null || !Data.IsValid)
            {
                return new AccessTokenInformation
                {
                    AccessToken = accessToken,
                    IsValid = false,
                    IsExpired = true,
                    ExpiresAt = null,
                    TimeUntilExpiration = null,
                    WabaId = wabaId
                };
            }

            // Handle never-expiring tokens
            if (Data.ExpiresAt == 0)
            {
                return new AccessTokenInformation
                {
                    AccessToken = accessToken,
                    IsValid = true,
                    IsExpired = false,
                    ExpiresAt = null, // No expiration date
                    TimeUntilExpiration = null, // Infinite time until expiration
                    AppId = Data.AppId,
                    UserId = Data.UserId,
                    WabaId = wabaId
                };
            }

            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(Data.ExpiresAt);
            var currentTime = DateTimeOffset.UtcNow;
            var timeUntilExpiration = expirationTime - currentTime;
            // Mark as expired if either time-expired OR invalid (revoked/no permissions)
            var isExpired = timeUntilExpiration.TotalSeconds <= 0;

            return new AccessTokenInformation
            {
                AccessToken = accessToken,
                IsValid = true,
                IsExpired = isExpired,
                ExpiresAt = expirationTime,
                TimeUntilExpiration = isExpired ? (TimeSpan?)null : timeUntilExpiration,
                AppId = Data.AppId,
                UserId = Data.UserId,
                WabaId = wabaId
            };
        }
    }

    public class SharedWABAIDData
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("application")]
        public string Application { get; set; }

        [JsonPropertyName("data_access_expires_at")]
        public long DataAccessExpiresAt { get; set; }

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; set; }

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; }

        [JsonPropertyName("granular_scopes")]
        public List<GranularScope> GranularScopes { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
    }

    public class GranularScope
    {
        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonPropertyName("target_ids")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> TargetIds { get; set; }
    }
    public class AccessTokenInformation
    {
        public string AccessToken { get; set; }
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string AppId { get; set; }
        public string UserId { get; set; }
        public TimeSpan? TimeUntilExpiration { get; set; }
        public string WabaId { get; set; }
    }
}