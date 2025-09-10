using System;
using System.Threading;
using System.Threading.Tasks;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Requests;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Responses;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Interfaces
{
    public interface IWhatsAppBusinessCloudApiClient : IDisposable
    {
        /// <summary>
        /// Exchanges an authorization code for an access token asynchronously.
        /// This is used as part of the OAuth 2.0 authorization code flow for Meta Embedded Signup.
        /// </summary>
        /// <param name="code">Authorization code received from OAuth flow</param>
        /// <param name="redirectUri">Redirect URI used in OAuth flow</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ExchangeTokenResponse containing the access token or error information</returns>
        Task<ExchangeTokenResponse> ExchangeTokenAsync(string code, string redirectUri = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exchanges a short-lived token for a long-lived token asynchronously.
        /// This is used to obtain a long-lived access token for the WhatsApp Business Cloud API.
        /// </summary>
        /// <param name="shortLivedToken">Short-lived access token</param>
        /// <param name="setTokenExpiresInSixtyDays">If set to true, this endpoint will return an access token that expires in 60 days. Otherwise, it will return a never-expiring access token.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ExchangeTokenResponse containing the long-lived access token</returns>
        Task<ExchangeTokenResponse> GetLongLivedTokenAsync(string shortLivedToken, bool setTokenExpiresInSixtyDays = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes the access token if it expires within the specified buffer time.
        /// Never-expiring tokens (expires_at = 0) are not refreshed.
        /// </summary>
        /// <param name="currentToken">Current access token</param>
        /// <param name="expirationCheckBufferTime">Buffer time to determine if token needs refreshing (default: 7 days)</param>
        /// <param name="setTokenExpiresInSixtyDays">If set to true and current time is within the buffer time, this endpoint will return an access token that expires in 60 days. Otherwise, it will return a never-expiring access token.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New access token if refreshed, otherwise the current token</returns>
        Task<string> RefreshIfExpiredAsync(string currentToken, TimeSpan? expirationCheckBufferTime = null, bool setTokenExpiresInSixtyDays = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes the access token.
        /// </summary>
        /// <param name="currentToken">Current access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if revoked, false otherwise.</returns>
        Task<bool> RevokeAsync(string currentToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Shared WhatsApp Business Account ID from input token
        /// </summary>
        /// <param name="inputToken">Input token obtained after embedded signup</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>SharedWABAIDResponse</returns>
        Task<SharedWABAIDResponse> GetSharedWABAIdAsync(string inputToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get detailed WhatsApp Business Account information by WABA ID
        /// </summary>
        /// <param name="whatsAppBusinessAccountId">WhatsApp Business Account ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>WABADetailsResponse</returns>
        Task<WABADetailsResponse> GetWABADetailsAsync(string whatsAppBusinessAccountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all phone numbers in a WhatsApp Business Account
        /// </summary>
        /// <param name="whatsAppBusinessAccountId">WhatsApp Business Account ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PhoneNumberResponse</returns>
        Task<PhoneNumberResponse> GetWhatsAppBusinessAccountPhoneNumberAsync(string whatsAppBusinessAccountId, CancellationToken cancellationToken = default);
    }
}