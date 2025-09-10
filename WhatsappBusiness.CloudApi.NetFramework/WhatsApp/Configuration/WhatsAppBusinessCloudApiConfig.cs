namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Configuration
{
    public class WhatsAppBusinessCloudApiConfig
    {
        public string WhatsAppBusinessId { get; set; }

        public string WhatsAppAccessToken { get; set; }

        public string WhatsAppGraphApiVersion { get; set; } = "v23.0";

        public string WhatsAppEmbeddedSignupMetaAppId { get; set; }

        public string WhatsAppEmbeddedSignupMetaAppSecret { get; set; }

        public string WhatsAppEmbeddedSignupMetaConfigurationId { get; set; }

        public string WhatsAppEmbeddedSignupPartnerSolutionId { get; set; }
    }
}