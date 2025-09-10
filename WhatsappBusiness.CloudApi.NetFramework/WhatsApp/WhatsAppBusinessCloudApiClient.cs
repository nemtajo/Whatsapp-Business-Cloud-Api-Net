using System;
using System.Collections.Generic;
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
        public WhatsAppBusinessCloudApiClient(WhatsAppBusinessCloudApiConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = SharedHttpClient;
            _ownsHttpClient = false; // Don't dispose the shared instance
        }

        /// <summary>
        /// Initializes a new instance of the WhatsAppBusinessCloudApiClient with custom HttpClient
        /// </summary>
        /// <param name="httpClient">Custom HttpClient instance</param>
        /// <param name="config">Configuration for the WhatsApp Business Cloud API</param>
        public WhatsAppBusinessCloudApiClient(HttpClient httpClient, WhatsAppBusinessCloudApiConfig config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
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
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://graph.facebook.com/v23.0/");
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            
            // Set default headers
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            return httpClient;
        }

        /// <summary>
        /// Exchanges an authorization code for an access token asynchronously.
        /// This is used as part of the OAuth 2.0 authorization code flow for Meta Embedded Signup.
        /// </summary>
        /// <param name="code">Authorization code received from OAuth flow</param>
        /// <param name="redirectUri">Redirect URI used in OAuth flow</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ExchangeTokenResponse containing the access token or error information</returns>
        public async Task<ExchangeTokenResponse> ExchangeTokenAsync(string code, string redirectUri = null, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Exchanges a short-lived token for a long-lived token asynchronously.
        /// This is used to obtain a long-lived access token for the WhatsApp Business Cloud API.
        /// </summary>
        /// <param name="shortLivedToken">Short-lived access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ExchangeTokenResponse containing the long-lived access token</returns>
        public async Task<ExchangeTokenResponse> GetLongLivedTokenAsync(string shortLivedToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(shortLivedToken))
                throw new ArgumentException("Short-lived token cannot be null or empty", nameof(shortLivedToken));

            var formData = new Dictionary<string, string>
            {
                { "grant_type", "fb_exchange_token" },
                { "client_id", _config.WhatsAppEmbeddedSignupMetaAppId },
                { "client_secret", _config.WhatsAppEmbeddedSignupMetaAppSecret },
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

        /// <summary>
        /// Get Shared WhatsApp Business Account ID from input token
        /// </summary>
        /// <param name="inputToken">Input token obtained after embedded signup</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>SharedWABAIDResponse</returns>
        public async Task<SharedWABAIDResponse> GetSharedWABAIdAsync(string inputToken, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Get detailed WhatsApp Business Account information by WABA ID
        /// </summary>
        /// <param name="whatsAppBusinessAccountId">WhatsApp Business Account ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>WABADetailsResponse</returns>
        public async Task<WABADetailsResponse> GetWABADetailsAsync(string whatsAppBusinessAccountId, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Get all phone numbers in a WhatsApp Business Account
        /// </summary>
        /// <param name="whatsAppBusinessAccountId">WhatsApp Business Account ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PhoneNumberResponse</returns>
        public async Task<PhoneNumberResponse> GetWhatsAppBusinessAccountPhoneNumberAsync(string whatsAppBusinessAccountId, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Refreshes the access token if it expires within the specified buffer time.
        /// Never-expiring tokens (expires_at = 0) are not refreshed.
        /// </summary>
        /// <param name="currentToken">Current access token</param>
        /// <param name="expirationCheckBufferTime">Buffer time to determine if token needs refreshing (default: 24 hours)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New access token if refreshed, otherwise the current token</returns>
        public async Task<string> RefreshIfExpiredAsync(string currentToken, TimeSpan? expirationCheckBufferTime = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(currentToken))
                throw new ArgumentException("Current token cannot be null or empty", nameof(currentToken));

            var bufferTime = expirationCheckBufferTime ?? TimeSpan.FromHours(24); // Default 24-hour buffer

            try
            {
                // Check current token status
                var response = await GetSharedWABAIdAsync(currentToken, cancellationToken);
                
                // Get access token information using the new method
                var tokenInfo = response.GetAccessTokenInformation(currentToken);
                
                // Handle never-expiring tokens (expires_at = 0)
                if (response.Data?.ExpiresAt == 0)
                {
                    // Never-expiring tokens don't need refresh, just return the current token
                    return currentToken;
                }
                
                // Check if token needs refreshing based on buffer time
                bool needsRefresh = tokenInfo.IsExpired || 
                    (tokenInfo.ExpiresAt.HasValue && tokenInfo.ExpiresAt.Value <= DateTimeOffset.UtcNow.Add(bufferTime));
                
                // If token doesn't need refreshing, return current token
                if (!needsRefresh)
                {
                    return currentToken;
                }

                // Try to get a long-lived token
                var longLivedResponse = await GetLongLivedTokenAsync(currentToken, cancellationToken);
                
                if (!string.IsNullOrEmpty(longLivedResponse.AccessToken))
                {
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

        /// <summary>
        /// Disposes the client. Note: The shared HttpClient instance is not disposed to ensure singleton behavior.
        /// </summary>
        public void Dispose()
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