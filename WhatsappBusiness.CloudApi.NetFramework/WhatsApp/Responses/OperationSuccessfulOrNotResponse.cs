using System.Text.Json.Serialization;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Responses
{
    internal class OperationSuccessfulOrNotResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}