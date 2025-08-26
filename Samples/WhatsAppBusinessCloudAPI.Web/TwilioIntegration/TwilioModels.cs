namespace WhatsAppBusinessCloudAPI.Web.TwilioIntegration
{
    /// <summary>
    /// Model for available country information from Twilio
    /// </summary>
    public class AvailableCountry
    {
        public string CountryCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public Uri? Uri { get; set; }
        public bool Beta { get; set; }
        public List<string> SubresourceUris { get; set; } = new List<string>();
    }

    /// <summary>
    /// Model for available phone number information from Twilio
    /// </summary>
    public class AvailablePhoneNumber
    {
        public string FriendlyName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Lata { get; set; }
        public string? Locality { get; set; }
        public string? RateCenter { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? IsoCountry { get; set; }
        public string? AddressRequirements { get; set; }
        public bool Beta { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
    }

    /// <summary>
    /// Request models for Twilio integration endpoints
    /// </summary>
    public class GetSharedWABADetailsRequest
    {
        public string? AccessToken { get; set; }
    }

    public class CreateTwilioSubaccountRequest
    {
        public string? BusinessName { get; set; }
        public string? WabaId { get; set; }
        
        // Phone number details are not used for subaccount creation itself,
        // but are passed through for subsequent phone number operations
        public string? PhoneNumberId { get; set; }
        public string? DisplayPhoneNumber { get; set; }
        public string? PhoneNumberType { get; set; } // "purchase" or "own"
    }

    public class HandleTwilioPhoneNumberRequest
    {
        public string? SubaccountSid { get; set; }
        public string? SubaccountAuthToken { get; set; }
        public string? BusinessName { get; set; }
        /// <summary>
        /// Type of phone number operation:
        /// - "purchase": Purchase a new Twilio phone number (uses IncomingPhoneNumber API)
        /// - "register": Use existing user-owned phone number (skips Twilio phone registration, 
        ///   only registers WhatsApp sender via RegisterPhoneNumberForWhatsApp endpoint)
        /// </summary>
        public string? PhoneNumberType { get; set; }
        public string? ExistingPhoneNumber { get; set; } // For registration type (user-owned numbers)
        public string? CountryCode { get; set; } // For purchase type (e.g., "US", "GB", "CA")
        public string? NumberType { get; set; } // For purchase type ("local" or "mobile")
    }

    public class GetAvailableCountriesRequest
    {
        public string? SubaccountSid { get; set; }
        public string? SubaccountAuthToken { get; set; }
    }

    public class GetAvailablePhoneNumbersRequest
    {
        public string? SubaccountSid { get; set; }
        public string? SubaccountAuthToken { get; set; }
        public string? CountryCode { get; set; }
        public string? PhoneNumberType { get; set; } // "local" or "mobile"
        public int? Limit { get; set; } = 20;
    }

    public class PurchasePhoneNumberRequest
    {
        public string? SubaccountSid { get; set; }
        public string? SubaccountAuthToken { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BusinessName { get; set; }
        public string? WabaId { get; set; }
        public string? CountryCode { get; set; }
    }

    public class RegisterPhoneNumberForWhatsAppRequest
    {
        public string? SubaccountSid { get; set; }
        public string? SubaccountAuthToken { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BusinessName { get; set; }
        public string? WabaId { get; set; }
    }

    // Regulatory Bundle Support
    public class CreateRegulatoryBundleRequest
    {
        public string? SubaccountSid { get; set; }
        public string? SubaccountAuthToken { get; set; }
        public string? BusinessName { get; set; }
        public string? IsoCountry { get; set; }
        public string? EndUserType { get; set; } = "business"; // Always business for our use case
        public string? NumberType { get; set; } // "local" or "mobile"
        
        // Business information
        public string? BusinessRegistrationNumber { get; set; }
        public string? BusinessType { get; set; } = "corporation";
        public string? BusinessIndustry { get; set; } = "technology";
        public string? BusinessAddress { get; set; }
        public string? BusinessCity { get; set; }
        public string? BusinessState { get; set; }
        public string? BusinessPostalCode { get; set; }
        public string? BusinessWebsite { get; set; }
        
        // Authorized contact information
        public string? AuthorizedContactFirstName { get; set; }
        public string? AuthorizedContactLastName { get; set; }
        public string? AuthorizedContactEmail { get; set; }
        public string? AuthorizedContactPhone { get; set; }
        public DateTime? AuthorizedContactDateOfBirth { get; set; }
        public string? AuthorizedContactJobTitle { get; set; } = "Authorized Representative";
    }

    public class CheckRegulatoryRequirementsRequest
    {
        public string? SubaccountSid { get; set; }
        public string? SubaccountAuthToken { get; set; }
        public string? IsoCountry { get; set; }
        public string? NumberType { get; set; } // "local" or "mobile"
    }
}
