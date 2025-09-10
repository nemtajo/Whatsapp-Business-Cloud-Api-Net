using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Configuration;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Interfaces
{
	public interface IWhatsAppBusinessCloudApiClientFactory
	{
		IWhatsAppBusinessCloudApiClient Create(WhatsAppBusinessCloudApiConfig config);
	}
}
