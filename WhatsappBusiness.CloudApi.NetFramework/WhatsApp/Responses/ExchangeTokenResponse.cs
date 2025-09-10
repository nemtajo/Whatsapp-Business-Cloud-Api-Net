using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Responses
{
    public class ExchangeTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonPropertyName("error")]
        public JsonElement? ErrorElement { get; set; }

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }

        [JsonIgnore]
        public string Error
        {
            get
            {
                if (!ErrorElement.HasValue)
                    return null;

                var errorElement = ErrorElement.Value;
                
                if (errorElement.ValueKind == JsonValueKind.String)
                {
                    return errorElement.GetString();
                }
                else if (errorElement.ValueKind == JsonValueKind.Object)
                {
                    if (errorElement.TryGetProperty("message", out var messageElement))
                    {
                        return messageElement.GetString();
                    }
                    else if (errorElement.TryGetProperty("type", out var typeElement))
                    {
                        return typeElement.GetString();
                    }
                    else
                    {
                        return errorElement.GetRawText();
                    }
                }
                else
                {
                    return errorElement.GetRawText();
                }
            }
        }
    }
}