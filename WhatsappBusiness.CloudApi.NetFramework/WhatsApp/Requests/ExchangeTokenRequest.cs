using System.Text.Json.Serialization;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Requests
{
    public class ExchangeTokenRequest
    {
        public string GrantType { get; set; } = "authorization_code";

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Code { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RedirectUri { get; set; }
    }
}