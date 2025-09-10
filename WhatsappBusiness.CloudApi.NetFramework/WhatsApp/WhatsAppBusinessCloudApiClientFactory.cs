using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Configuration;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Interfaces;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.NetFramework.WhatsApp
{
	public class WhatsAppBusinessCloudApiClientFactory : IWhatsAppBusinessCloudApiClientFactory
	{
		public IWhatsAppBusinessCloudApiClient Create(WhatsAppBusinessCloudApiConfig config)
		{
			return new WhatsAppBusinessCloudApiClient(config);
		}
	}
}
