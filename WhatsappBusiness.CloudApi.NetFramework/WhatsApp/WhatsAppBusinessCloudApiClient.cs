using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Configuration;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Interfaces;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Requests;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Responses;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp
{
    /// <summary>
    /// WhatsApp Business Cloud API client implementation using singleton HttpClient
    /// </summary>
    public class WhatsAppBusinessCloudApiClient : IWhatsAppBusinessCloudApiClient
    {
        private static readonly Lazy<HttpClient> _lazyHttpClient = new Lazy<HttpClient>(() => CreateHttpClient());
        private static HttpClient SharedHttpClient => _lazyHttpClient.Value;

        private readonly HttpClient _httpClient;
        private readonly WhatsAppBusinessCloudApiConfig _config;
        private readonly bool _ownsHttpClient;

        /// <summary>
        /// Initializes a new instance of the WhatsAppBusinessCloudApiClient using singleton HttpClient
        /// </summary>
        /// <param name="config">Configuration for the WhatsApp Business Cloud API</param>
        public WhatsAppBusinessCloudApiClient(WhatsAppBusinessCloudApiConfig config, bool throwIfConfigIsNull = true)
        {
            if (config == null && throwIfConfigIsNull)
                throw new ArgumentNullException(nameof(config));

            _config = config;
            _httpClient = SharedHttpClient;
            _ownsHttpClient = false; // Don't dispose the shared instance
        }

        /// <summary>
        /// Initializes a new instance of the WhatsAppBusinessCloudApiClient with custom HttpClient
        /// </summary>
        /// <param name="httpClient">Custom HttpClient instance</param>
        /// <param name="config">Configuration for the WhatsApp Business Cloud API</param>
        public WhatsAppBusinessCloudApiClient(HttpClient httpClient, WhatsAppBusinessCloudApiConfig config, bool throwIfConfigIsNull = true)
        {
            if (config == null && throwIfConfigIsNull)
                throw new ArgumentNullException(nameof(config));

            _config = config;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _ownsHttpClient = false; // Don't dispose external HttpClient

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri($"https://graph.facebook.com/{_config.WhatsAppGraphApiVersion}/");
            }
        }

        /// <summary>
        /// Creates and configures the singleton HttpClient instance
        /// </summary>
        /// <returns>Configured HttpClient instance</returns>
        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("https://graph.facebook.com/v23.0/");
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            // Set default headers
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        public virtual async Task<ExchangeTokenResponse> ExchangeTokenAsync(string code, string redirectUri = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Code cannot be null or empty", nameof(code));

            var request = new ExchangeTokenRequest
            {
                ClientId = _config.WhatsAppEmbeddedSignupMetaAppId,
                ClientSecret = _config.WhatsAppEmbeddedSignupMetaAppSecret,
                Code = code,
                RedirectUri = redirectUri
            };

            var formData = new Dictionary<string, string>
            {
                { "grant_type", request.GrantType },
                { "client_id", request.ClientId },
                { "client_secret", request.ClientSecret },
                { "code", request.Code }
            };

            if (!string.IsNullOrEmpty(request.RedirectUri))
            {
                formData.Add("redirect_uri", request.RedirectUri);
            }

            var formContent = new FormUrlEncodedContent(formData);

            // Use proper base address for OAuth endpoint
            var oauthBaseUri = new Uri("https://graph.facebook.com/");
            var requestUri = new Uri(oauthBaseUri, "oauth/access_token");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = formContent
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ExchangeTokenResponse>(responseContent);

            if (!response.IsSuccessStatusCode && result?.Error != null)
            {
                throw new HttpRequestException($"OAuth token exchange failed: {result.Error} - {result.ErrorDescription}");
            }

            return result ?? new ExchangeTokenResponse();
        }

        public virtual async Task<ExchangeTokenResponse> GetLongLivedTokenAsync(string shortLivedToken, bool setTokenExpiresInSixtyDays = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(shortLivedToken))
                throw new ArgumentException("Short-lived token cannot be null or empty", nameof(shortLivedToken));

            var formData = new Dictionary<string, string>
            {
                { "grant_type", "fb_exchange_token" },
                { "client_id", _config.WhatsAppEmbeddedSignupMetaAppId },
                { "client_secret", _config.WhatsAppEmbeddedSignupMetaAppSecret },
                { "set_token_expires_in_60_days", setTokenExpiresInSixtyDays.ToString().ToLowerInvariant() },
                { "fb_exchange_token", shortLivedToken }
            };

            var formContent = new FormUrlEncodedContent(formData);

            var oauthBaseUri = new Uri("https://graph.facebook.com/");
            var requestUri = new Uri(oauthBaseUri, "oauth/access_token");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = formContent
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ExchangeTokenResponse>(responseContent);

            if (!response.IsSuccessStatusCode && result?.Error != null)
            {
                throw new HttpRequestException($"Long-lived token exchange failed: {result.Error} - {result.ErrorDescription}");
            }

            return result ?? new ExchangeTokenResponse();
        }

        public virtual async Task<SharedWABAIDResponse> GetSharedWABAIdAsync(string inputToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(inputToken))
                throw new ArgumentException("Input token cannot be null or empty", nameof(inputToken));

            var endpoint = $"debug_token?input_token={inputToken}";
            var requestUri = new Uri(new Uri($"https://graph.facebook.com/{_config.WhatsAppGraphApiVersion}/"), endpoint);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.WhatsAppAccessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SharedWABAIDResponse>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get shared WABA ID. Status: {response.StatusCode}, Content: {errorContent}");
            }
        }

        public virtual async Task<WABADetailsResponse> GetWABADetailsAsync(string whatsAppBusinessAccountId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(whatsAppBusinessAccountId))
                throw new ArgumentException("WhatsApp Business Account ID cannot be null or empty", nameof(whatsAppBusinessAccountId));

            var endpoint = $"{whatsAppBusinessAccountId}?fields=id,name,currency,timezone_id,message_template_namespace,account_review_status,business_verification_status,country,owner_business_info,primary_business_location,purchase_order_number,status,health_status";
            var requestUri = new Uri(new Uri($"https://graph.facebook.com/{_config.WhatsAppGraphApiVersion}/"), endpoint);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.WhatsAppAccessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<WABADetailsResponse>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get WABA details. Status: {response.StatusCode}, Content: {errorContent}");
            }
        }

        public virtual async Task<PhoneNumberResponse> GetWhatsAppBusinessAccountPhoneNumberAsync(string whatsAppBusinessAccountId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(whatsAppBusinessAccountId))
                throw new ArgumentException("WhatsApp Business Account ID cannot be null or empty", nameof(whatsAppBusinessAccountId));

            var endpoint = $"{whatsAppBusinessAccountId}/phone_numbers";
            var requestUri = new Uri(new Uri($"https://graph.facebook.com/{_config.WhatsAppGraphApiVersion}/"), endpoint);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.WhatsAppAccessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PhoneNumberResponse>(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get phone numbers. Status: {response.StatusCode}, Content: {errorContent}");
            }
        }

        public virtual async Task<string> RefreshIfExpiredAsync(string currentToken, TimeSpan? expirationCheckBufferTime = null, bool setTokenExpiresInSixtyDays = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(currentToken))
                throw new ArgumentException("Current token cannot be null or empty", nameof(currentToken));

            var bufferTime = expirationCheckBufferTime ?? TimeSpan.FromHours(7 * 24); // Default 7-day buffer

            try
            {
                var response = await GetSharedWABAIdAsync(currentToken, cancellationToken);

                var tokenInfo = response.GetAccessTokenInformation(currentToken);

                // Handle never-expiring tokens
                if (response.Data?.ExpiresAt == 0)
                {
                    return currentToken;
                }

                bool needsRefresh = tokenInfo.IsExpired ||
                    (tokenInfo.ExpiresAt.HasValue && tokenInfo.ExpiresAt.Value <= DateTimeOffset.UtcNow.Add(bufferTime));

                if (!needsRefresh)
                {
                    return currentToken;
                }

                var longLivedResponse = await GetLongLivedTokenAsync(currentToken, setTokenExpiresInSixtyDays, cancellationToken);

                if (!string.IsNullOrEmpty(longLivedResponse.AccessToken))
                {
                    //TODO Revoke the old token
                    return longLivedResponse.AccessToken;
                }

                // If refresh failed but token is still technically valid (not expired yet), continue using it
                if (tokenInfo.IsValid && !tokenInfo.IsExpired)
                {
                    return currentToken;
                }

                throw new InvalidOperationException("Token refresh failed and current token is expired");
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                throw new InvalidOperationException($"Token refresh failed: {ex.Message}", ex);
            }
        }

        public virtual async Task<bool> RevokeAsync(string currentToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(currentToken))
                throw new ArgumentException("Current token cannot be null or empty", nameof(currentToken));

            var endpoint = $"oauth/revoke?client_id={_config.WhatsAppEmbeddedSignupMetaAppId}&client_secret={_config.WhatsAppEmbeddedSignupMetaAppSecret}&revoke_token={currentToken}&access_token={_config.WhatsAppAccessToken}";
            var requestUri = new Uri(new Uri($"https://graph.facebook.com/{_config.WhatsAppGraphApiVersion}/"), endpoint);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.WhatsAppAccessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<OperationSuccessfulOrNotResponse>(responseContent);
                return responseObj.Success;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get shared WABA ID. Status: {response.StatusCode}, Content: {errorContent}");
            }
        }

        /// <summary>
        /// Disposes the client. Note: The shared HttpClient instance is not disposed to ensure singleton behavior.
        /// </summary>
        public virtual void Dispose()
        {
            // Only dispose if we own the HttpClient (i.e., it was passed in via constructor)
            // The shared singleton HttpClient should not be disposed
            if (_ownsHttpClient && _httpClient != null && _httpClient != SharedHttpClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}