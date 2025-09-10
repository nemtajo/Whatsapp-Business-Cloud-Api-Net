using System;
using System.Threading;
using System.Threading.Tasks;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Requests;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Responses;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Interfaces
{
    public interface IWhatsAppBusinessCloudApiClient : IDisposable
    {
        Task<ExchangeTokenResponse> ExchangeTokenAsync(string code, string redirectUri = null, CancellationToken cancellationToken = default);

        Task<ExchangeTokenResponse> GetLongLivedTokenAsync(string shortLivedToken, CancellationToken cancellationToken = default);

        Task<string> RefreshIfExpiredAsync(string currentToken, TimeSpan? expirationCheckBufferTime = null, CancellationToken cancellationToken = default);

        Task<SharedWABAIDResponse> GetSharedWABAIdAsync(string inputToken, CancellationToken cancellationToken = default);

        Task<WABADetailsResponse> GetWABADetailsAsync(string whatsAppBusinessAccountId, CancellationToken cancellationToken = default);

        Task<PhoneNumberResponse> GetWhatsAppBusinessAccountPhoneNumberAsync(string whatsAppBusinessAccountId, CancellationToken cancellationToken = default);
    }
}