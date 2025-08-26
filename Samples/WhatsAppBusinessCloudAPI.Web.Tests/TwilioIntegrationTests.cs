using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Content.V1;
using Twilio.TwiML.Messaging;
using WhatsappBusiness.CloudApi.Configurations;
using WhatsAppBusinessCloudAPI.Web.TwilioIntegration;
using Xunit;
using static Twilio.Rest.Content.V1.ContentResource;

namespace WhatsAppBusinessCloudAPI.Web.Tests
{
    public class TwilioIntegrationTests
    {
        private readonly IConfiguration _configuration;
        private readonly ITwilioRestClient _twilioClient;
        private readonly EmbeddedSignupConfiguration _embeddedSignupConfig;
        private readonly TwilioIntegrationService _twilioIntegrationService;
        private readonly string _subaccountSid = "";
        private readonly string _subaccountAuthToken = "";
        private readonly string _subaccountBundleSid = "";

        public TwilioIntegrationTests()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = configurationBuilder.Build();

            var accountSid = _configuration["Twilio:Client:AccountSid"];
            var authToken = _configuration["Twilio:Client:AuthToken"];
            _twilioClient = new TwilioRestClient(accountSid, authToken);

            _embeddedSignupConfig = new EmbeddedSignupConfiguration();
            _configuration.GetSection("EmbeddedSignupConfiguration").Bind(_embeddedSignupConfig);

            var regulatoryBundleOptions = new TwilioRegulatoryBundleOptions();
            _configuration.GetSection("Twilio:RegulatoryBundles").Bind(regulatoryBundleOptions);
            var optionsWrapper = Microsoft.Extensions.Options.Options.Create(regulatoryBundleOptions);

            _twilioIntegrationService = new TwilioIntegrationService(
                NullLogger<TwilioIntegrationService>.Instance,
                _twilioClient,
                _configuration,
                optionsWrapper);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task CreateTwilioSubaccountAsync_ShouldCreateSubaccountSuccessfully()
        {
            // Arrange
            var businessName = "LingoDub Test";
            var wabaId = "1159069262707097";

            // Act
            var result = await _twilioIntegrationService.CreateTwilioSubaccountAsync(businessName, wabaId);

            // Assert
            Assert.True(result.Success);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task GetAvailableCountriesAsync_ShouldReturnCountriesList()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;

            // Act
            var result = await _twilioIntegrationService.GetAvailableCountriesAsync(subaccountSid, subaccountAuthToken);

            // Assert
            Assert.NotNull(result);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task CheckRegulatoryRequirementsAsync_ShouldCheckRequirements()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;
            var isoCountry = "GB";
            var numberType = "mobile";

            // Act
            var result = await _twilioIntegrationService.CheckRegulatoryRequirementsAsync(
                subaccountSid, subaccountAuthToken, isoCountry, numberType);

            // Assert
            Assert.True(result.RequiresBundle);
        }

        [Fact/*(Skip = "Integration test - requires real Twilio credentials")*/]
        public async Task CreateRegulatoryBundleAsync_ShouldCreateBundleForGBBusiness()
        {
            // Arrange
            var request = new CreateRegulatoryBundleRequest
            {
                SubaccountSid = _subaccountSid,
                SubaccountAuthToken = _subaccountAuthToken,
                BusinessName = "LINGODUB TECHNOLOGIES LTD",
                BusinessRegistrationNumber = "12467598",
                BusinessAddress = "71-75 Shelton Street, London, Greater London, United Kingdom, WC2H 9JQ",
                BusinessWebsite = "https://lingodub.com",
                AuthorizedContactFirstName = "Nemanja",
                AuthorizedContactLastName = "Stolic",
                AuthorizedContactEmail = "info@lingodub.com",
                AuthorizedContactPhone = "+447407046777",
                EndUserType = "business",
                IsoCountry = "GB",
                NumberType = "mobile"
            };

            // Act
            var result = await _twilioIntegrationService.CreateRegulatoryBundleAsync(request);

            // Assert
            Assert.True(result.Success);
        }

        [Fact/*(Skip = "Integration test - requires real Twilio credentials")*/]
        public async Task GetAvailablePhoneNumbersAsync_ShouldReturnPhoneNumbers()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;
            var countryCode = "GB";
            var phoneNumberType = "mobile";
            var limit = 20;

            // Act
            var result = await _twilioIntegrationService.GetAvailablePhoneNumbersAsync(
                subaccountSid, subaccountAuthToken, countryCode, phoneNumberType, limit);

            // Assert
            Assert.NotNull(result);
        }

        [Fact/*(Skip = "Integration test - requires real Twilio credentials")*/]
        public async Task PurchasePhoneNumberAsync_ShouldPurchaseAndAssignToSubaccount()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;
            var subaccountBundleSid = _subaccountBundleSid;
            var phoneNumber = "+447447193608";
            var businessName = "LINGODUB TECHNOLOGIES LTD";
            var countryCode = "GB";

            // Act
            var result = await _twilioIntegrationService.PurchasePhoneNumberAsync(
                subaccountSid, phoneNumber, businessName, countryCode, subaccountAuthToken, subaccountBundleSid);

            // Assert
            Assert.True(result.Success);
        }

        [Fact/*(Skip = "Integration test - requires real Twilio credentials")*/]
        public async Task RegisterPhoneNumberForWhatsAppAsync_ShouldRegisterWithWhatsApp()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;
            var phoneNumber = "+447447193608";
            var businessName = "LINGODUB TECHNOLOGIES LTD";
            var wabaId = "1159069262707097";
            var webhookUrl = "https://whatsapp.lingodub.com/api/WhatsAppNotification";

            // Act
            var result = await _twilioIntegrationService.RegisterPhoneNumberForWhatsAppAsync(
                subaccountSid, subaccountAuthToken, phoneNumber, businessName, wabaId, webhookUrl);

            // Assert
            Assert.True(result.Success);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task CreateWhatsAppTemplateAsync_ShouldCreate()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;

            var yesScheduleButton = new ContentResource.CardAction.Builder()
                .WithType(ContentResource.CardActionType.QuickReply)
                .WithTitle("Yes - Schedule")
                .WithId("YES")
                .Build();

            var noScheduleButton = new ContentResource.CardAction.Builder()
                .WithType(ContentResource.CardActionType.QuickReply)
                .WithTitle("No - Don't schedule")
                .WithId("NO")
                .Build();

            var whatsappCard = new ContentResource.WhatsappCard.Builder()
                .WithHeaderText("Virtual Recall Intro")
                .WithBody("Hey {{1}}! Virtual Recall would love for you to schedule an appointment with us.")
                .WithFooter("Reply STOP to unsubscribe.")
                .WithActions(new List<ContentResource.CardAction> { yesScheduleButton, noScheduleButton })
                .Build();

            var types = new ContentResource.Types.Builder()
                .WithWhatsappCard(whatsappCard)
                .Build();

            var contentCreateRequest = new ContentResource.ContentCreateRequest.Builder()
                .WithFriendlyName($"test_template_{DateTime.UtcNow:yyyyMMddHHmmss}")
                .WithLanguage("en")
                .WithVariables(new Dictionary<string, string>
                {
                    ["1"] = "Dan"
                })
                .WithTypes(types)
                .Build();

            // Act
            var result = await _twilioIntegrationService.CreateContentTemplateAsync(
                contentCreateRequest, subaccountSid, subaccountAuthToken);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.TemplateSid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task CreateWhatsAppTemplateWithMediaAttachmentAsync_ShouldCreate()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;

            // Create template with VARIABLE in media URL (following Twilio's rule)
            // Variables are only supported after the domain
            var twilioMedia = new TwilioMedia.Builder();
            twilioMedia.WithBody("Hey {{1}}! Please find attached the pdf.");
            twilioMedia.WithMedia(new List<string>() { "https://ontheline.trincoll.edu/images/{{2}}" });

            var types = new ContentResource.Types.Builder()
                .WithTwilioMedia(twilioMedia.Build())
                .Build();

            var contentCreateRequest = new ContentResource.ContentCreateRequest.Builder()
                .WithFriendlyName($"test_template_with_pdf_{DateTime.UtcNow:yyyyMMddHHmmss}")
                .WithLanguage("en")
                .WithVariables(new Dictionary<string, string>
                {
                    ["1"] = "Dan",                                    // For body text variable {{1}}
                    ["2"] = "bookdown/sample-local-pdf.pdf"          // For media URL path variable {{2}}
                })
                .WithTypes(types)
                .Build();

            // Act
            var result = await _twilioIntegrationService.CreateContentTemplateAsync(
                contentCreateRequest, subaccountSid, subaccountAuthToken);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.TemplateSid);
            Assert.Null(result.ErrorMessage);
            
            // Log the template SID for use in SendWhatsAppTemplateWithMediaAttachmentAsync_ShouldSend
            // Copy this SID and use it as predefinedTemplateSid in the send test
            Console.WriteLine($"Created template SID: {result.TemplateSid}");
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task SubmitContentTemplateForApprovalAsync_ShouldSubmit()
        {
            // Arrange
            var templateSid = "your-template-sid";
            string name = "some_name"; // Only lowercase alphanumeric characters and underscore allowed
            string templateCategory = "MARKETING";
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;

            // Act
            var result = await _twilioIntegrationService.SubmitContentTemplateForApprovalAsync(
                templateSid, name, templateCategory, subaccountSid, subaccountAuthToken);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task SendWhatsAppTemplate_ShouldSend()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;

            var templateSid = "content_template_sid_to_send";

            var toNumber = "+15551234567";
            var fromNumber = "+15557654321";

            var contentVariables = new Dictionary<string, string>
            {
                ["1"] = "Nem",                        // For body text variable {{1}}
                ["2"] = "documents/different-file.pdf" // For media URL path variable {{2}}
            };
            
            // Act
            var result = await _twilioIntegrationService.SendWhatsAppTemplateMessageAsync(
                fromNumber,
                toNumber,
                templateSid,
                contentVariables,
                subaccountSid,
                subaccountAuthToken
            );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.MessageSid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task GetContentTemplatesAsync_ShouldReturnTemplatesList()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;

            // Act
            var result = await _twilioIntegrationService.GetContentTemplatesAsync(
                subaccountSid, subaccountAuthToken);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Templates);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task DeleteContentTemplateAsync_ShouldDeleteTemplate()
        {
            // Arrange
            var templateSid = "your-template-sid";
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;

            // Act
            var result = await _twilioIntegrationService.DeleteContentTemplateAsync(
                templateSid, subaccountSid, subaccountAuthToken);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public async Task GetContentTemplateAsync_ShouldReturnSpecificTemplate()
        {
            // Arrange
            var templateSid = "your-template-sid";
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;

            // Act
            var result = await _twilioIntegrationService.GetContentTemplateAsync(
                templateSid, subaccountSid, subaccountAuthToken);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Template);
            Assert.Null(result.ErrorMessage);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public void SendWhatsAppMessage_WithAtttachment_ShouldSend()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;
            var toNumber = "+447407046777";
            var fromNumber = "+447723447057";

            // Act
            var result = _twilioIntegrationService.SendWhatsAppMessage(
                fromNumber,
                toNumber,
                body: "Here's the PDF you requested",
                mediaUrl: "https://ontheline.trincoll.edu/images/bookdown/sample-local-pdf.pdf",
                contentVariables: null,
                subaccountSid,
                subaccountAuthToken);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.MessageSid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact(Skip = "Integration test - requires real Twilio credentials")]
        public void SendWhatsAppMessage_WithTextOnly_ShouldSend()
        {
            // Arrange
            var subaccountSid = _subaccountSid;
            var subaccountAuthToken = _subaccountAuthToken;
            var toNumber = "+447407046777";
            var fromNumber = "+447723447057";

            // Act
            var result = _twilioIntegrationService.SendWhatsAppMessage(
                fromNumber,
                toNumber,
                body: "How can we help you today?",
                mediaUrl: null,
                contentVariables: null,
                subaccountSid,
                subaccountAuthToken);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.MessageSid);
            Assert.Null(result.ErrorMessage);
        }
    }
}
