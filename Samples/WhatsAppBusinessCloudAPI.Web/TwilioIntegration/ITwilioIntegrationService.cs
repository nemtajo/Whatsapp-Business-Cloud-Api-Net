using WhatsAppBusinessCloudAPI.Web.ViewModel;
using Twilio.Rest.Content.V1;

namespace WhatsAppBusinessCloudAPI.Web.TwilioIntegration
{
    /// <summary>
    /// Interface for Twilio integration services providing phone number management and WhatsApp registration
    /// </summary>
    public interface ITwilioIntegrationService
    {
        /// <summary>
        /// Creates a Twilio subaccount for the onboarded business
        /// </summary>
        /// <param name="businessName">Name of the business for the subaccount</param>
        /// <param name="wabaId">WhatsApp Business Account ID from embedded signup that will be linked to this subaccount</param>
        /// <returns>Twilio subaccount information including SID and auth token</returns>
        Task<(bool Success, string? SubaccountSid, string? SubaccountAuthToken, string? ApiKeySid, string? ApiKeySecret, string? ErrorMessage)> CreateTwilioSubaccountAsync(
            string businessName, string wabaId);

        /// <summary>
        /// Gets available countries for phone number purchase using actual Twilio API
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token</param>
        /// <returns>List of available countries</returns>
        Task<List<AvailableCountry>> GetAvailableCountriesAsync(string subaccountSid, string subaccountAuthToken);

        /// <summary>
        /// Checks if regulatory requirements are needed for a specific country and number type
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token</param>
        /// <param name="isoCountry">ISO country code</param>
        /// <param name="numberType">Type of phone number: "local" or "mobile"</param>
        /// <returns>Tuple containing requirement status and message</returns>
        Task<(bool RequiresBundle, bool SupportedCountry, string Message)> CheckRegulatoryRequirementsAsync(
            string subaccountSid, string subaccountAuthToken, string isoCountry, string numberType);

        /// <summary>
        /// Creates a regulatory bundle for the specified country and number type
        /// </summary>
        /// <param name="request">Regulatory bundle creation request</param>
        /// <returns>Tuple containing success status, bundle SID, and error message</returns>
        Task<(bool Success, string? BundleSid, string? ErrorMessage)> CreateRegulatoryBundleAsync(CreateRegulatoryBundleRequest request);

        /// <summary>
        /// Gets available phone numbers for a specific country and type using actual Twilio API
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token</param>
        /// <param name="countryCode">ISO country code</param>
        /// <param name="phoneNumberType">Type of phone number: "local" or "mobile"</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>List of available phone numbers</returns>
        Task<List<AvailablePhoneNumber>> GetAvailablePhoneNumbersAsync(string subaccountSid, string subaccountAuthToken, 
            string countryCode, string phoneNumberType, int limit);

        /// <summary>
        /// Purchases a specific Twilio phone number for the subaccount using main account credentials
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID to assign the number to</param>
        /// <param name="phoneNumber">Specific Twilio phone number to purchase</param>
        /// <param name="businessName">Business name for logging</param>
        /// <param name="countryCode">Country code for regulatory bundle lookup</param>
        /// <param name="subaccountAuthToken">Subaccount Auth Token</param>
        /// <param name="subaccountBundleSid">Subaccount Bundle Sid</param>
        /// <returns>Purchase result</returns>
        Task<(bool Success, string? PhoneNumberSid, string? ErrorMessage)> PurchasePhoneNumberAsync(
            string subaccountSid, string phoneNumber, string businessName, string countryCode, string? subaccountAuthToken = null, string? subaccountBundleSid = null);

        /// <summary>
        /// Registers a phone number for WhatsApp using Twilio Channels Sender API
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token</param>
        /// <param name="phoneNumber">Phone number to register</param>
        /// <param name="businessName">Business name</param>
        /// <param name="wabaId">WhatsApp Business Account ID</param>
        /// <param name="webhookUrl">URL for webhook registration</param>
        /// <returns>Registration result</returns>
        Task<(bool Success, string? SenderSid, string? Status, string? ErrorMessage)> RegisterPhoneNumberForWhatsAppAsync(
            string subaccountSid, string subaccountAuthToken, string phoneNumber, string businessName, string wabaId, string webhookUrl);

        /// <summary>
        /// Creates a content template in the shared WABA using Twilio Content API
        /// </summary>
        /// <param name="contentCreateRequest">Twilio Content API create request</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Template creation result</returns>
        Task<(bool Success, string? TemplateSid, string? ErrorMessage)> CreateContentTemplateAsync(
            ContentResource.ContentCreateRequest contentCreateRequest, string? subaccountSid = null, string? subaccountAuthToken = null);

        /// <summary>
        /// Submits a content template for approval
        /// </summary>
        /// <param name="templateSid">Content template SID to approve</param>
        /// <param name="name">Name that uniquely identifies the Content. Only lowercase alphanumeric characters or underscores</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Template approval result</returns>
        Task<(bool Success, string? ErrorMessage)> SubmitContentTemplateForApprovalAsync(
            string templateSid, string name, string templateCategory, string? subaccountSid = null, string? subaccountAuthToken = null);

        /// <summary>
        /// Sends a WhatsApp message using a content template
        /// </summary>
        /// <param name="fromNumber">Sender WhatsApp phone number (with whatsapp: prefix)</param>
        /// <param name="toNumber">Recipient WhatsApp phone number (with whatsapp: prefix)</param>
        /// <param name="templateSid">Content template SID to use for the message</param>
        /// <param name="contentVariables">Template variables as key-value pairs</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Message sending result</returns>
        Task<(bool Success, string? MessageSid, string? ErrorMessage)> SendWhatsAppTemplateMessageAsync(
            string fromNumber, string toNumber, string templateSid, Dictionary<string, string> contentVariables, 
            string? subaccountSid = null, string? subaccountAuthToken = null);

        /// <summary>
        /// Gets all content templates for the subaccount using Twilio Content API
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>List of content templates</returns>
        Task<(bool Success, List<ContentResource>? Templates, string? ErrorMessage)> GetContentTemplatesAsync(
            string? subaccountSid = null, string? subaccountAuthToken = null);

        /// <summary>
        /// Deletes a content template using Twilio Content API
        /// </summary>
        /// <param name="templateSid">Content template SID to delete</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Template deletion result</returns>
        Task<(bool Success, string? ErrorMessage)> DeleteContentTemplateAsync(
            string templateSid, string? subaccountSid = null, string? subaccountAuthToken = null);

        /// <summary>
        /// Gets a specific content template by SID using Twilio Content API
        /// </summary>
        /// <param name="templateSid">Content template SID to retrieve</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Content template details</returns>
        Task<(bool Success, ContentResource? Template, string? ErrorMessage)> GetContentTemplateAsync(
            string templateSid, string? subaccountSid = null, string? subaccountAuthToken = null);

        /// <summary>
        /// Sends a WhatsApp message
        /// </summary>
        /// <param name="fromNumber">Sender WhatsApp phone number (with whatsapp: prefix)</param>
        /// <param name="toNumber">Recipient WhatsApp phone number (with whatsapp: prefix)</param>
        /// <param name="body">Message body</param>
        /// <param name="mediaUrl">Public-facing URL of the file attachment</param>
        /// <param name="contentVariables">Template variables as key-value pairs</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Message sending result</returns>
        (bool Success, string? MessageSid, string? ErrorMessage) SendWhatsAppMessage(
            string fromNumber, string toNumber, string body, string? mediaUrl, Dictionary<string, string>? contentVariables = null, 
            string? subaccountSid = null, string? subaccountAuthToken = null);
    }
}
