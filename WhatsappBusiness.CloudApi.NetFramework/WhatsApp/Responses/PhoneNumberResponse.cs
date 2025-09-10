using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;

namespace WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Responses
{
    public class PhoneNumberResponse
    {
        [JsonPropertyName("data")]
        public List<PhoneNumberData> Data { get; set; }

        [JsonPropertyName("paging")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PagingData Paging { get; set; }

        public string GetMostRecentlyOnboardedPhoneNumberId()
        {
            if (Data == null || !Data.Any())
                return null;

            var phoneNumberWithMostRecentOnboarding = Data
                .Where(pn => !string.IsNullOrEmpty(pn.LastOnboardedTime))
                .Select(pn => new
                {
                    PhoneNumber = pn,
                    ParsedDate = TryParseLastOnboardedTime(pn.LastOnboardedTime)
                })
                .Where(x => x.ParsedDate.HasValue)
                .OrderByDescending(x => x.ParsedDate.Value)
                .FirstOrDefault();

            return phoneNumberWithMostRecentOnboarding?.PhoneNumber.Id;
        }

        private static DateTime? TryParseLastOnboardedTime(string lastOnboardedTime)
        {
            if (string.IsNullOrEmpty(lastOnboardedTime))
                return null;

            if (DateTimeOffset.TryParseExact(lastOnboardedTime, "yyyy-MM-ddTHH:mm:sszzz", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffset))
            {
                return dateTimeOffset.DateTime;
            }

            if (DateTimeOffset.TryParse(lastOnboardedTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeOffset))
            {
                return dateTimeOffset.DateTime;
            }

            return null;
        }
    }

    public class PhoneNumberData
    {
        [JsonPropertyName("verified_name")]
        public string VerifiedName { get; set; }

        [JsonPropertyName("display_phone_number")]
        public string DisplayPhoneNumber { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("quality_rating")]
        public string QualityRating { get; set; }

        [JsonPropertyName("code_verification_status")]
        public string CodeVerificationStatus { get; set; }

        [JsonPropertyName("platform_type")]
        public string PlatformType { get; set; }

        [JsonPropertyName("throughput")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ThroughputData Throughput { get; set; }

        [JsonPropertyName("last_onboarded_time")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LastOnboardedTime { get; set; }

        [JsonPropertyName("webhook_configuration")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WebhookConfigurationData WebhookConfiguration { get; set; }
    }

    public class ThroughputData
    {
        [JsonPropertyName("level")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Level { get; set; }
    }

    public class WebhookConfigurationData
    {
        [JsonPropertyName("application")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Application { get; set; }
    }

    public class PagingData
    {
        [JsonPropertyName("cursors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CursorsData Cursors { get; set; }
    }

    public class CursorsData
    {
        [JsonPropertyName("before")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Before { get; set; }

        [JsonPropertyName("after")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string After { get; set; }
    }
}