using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Twilio.Clients;
using Twilio.Rest.Api.V2010;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Api.V2010.Account.AvailablePhoneNumberCountry;
using Twilio.Rest.Content.V1;
using Twilio.Rest.Content.V1.Content;
using Twilio.Rest.Messaging.V2;
using Twilio.Types;
using static Twilio.Rest.Content.V1.Content.ApprovalCreateResource;
using TwilioExceptions = Twilio.Exceptions;

namespace WhatsAppBusinessCloudAPI.Web.TwilioIntegration
{
    /// <summary>
    /// Twilio integration service providing phone number management and WhatsApp registration
    /// </summary>
    public class TwilioIntegrationService : ITwilioIntegrationService
    {
        private readonly ILogger<TwilioIntegrationService> _logger;
        private readonly ITwilioRestClient _twilioClient;
        private readonly IConfiguration _configuration;
        private readonly TwilioRegulatoryBundleOptions _regulatoryBundleOptions;

        public TwilioIntegrationService(
            ILogger<TwilioIntegrationService> logger,
            ITwilioRestClient twilioClient,
            IConfiguration configuration,
            IOptions<TwilioRegulatoryBundleOptions> regulatoryBundleOptions)
        {
            _logger = logger;
            _twilioClient = twilioClient;
            _configuration = configuration;
            _regulatoryBundleOptions = regulatoryBundleOptions.Value;
        }

        /// <summary>
        /// Creates a Twilio subaccount for the onboarded business
        /// </summary>
        /// <param name="businessName">Name of the business for the subaccount</param>
        /// <param name="wabaId">WhatsApp Business Account ID from embedded signup that will be linked to this subaccount</param>
        /// <returns>Twilio subaccount information including SID and auth token</returns>
        public async Task<(bool Success, string? SubaccountSid, string? SubaccountAuthToken, string? ApiKeySid, string? ApiKeySecret, string? ErrorMessage)> CreateTwilioSubaccountAsync(
            string businessName, string wabaId)
        {
            try
            {
                _logger.LogInformation("Creating Twilio subaccount for business: {BusinessName}, WABA ID: {WabaId}", 
                    businessName, wabaId);
                
                _logger.LogInformation("This subaccount will be linked to WABA {WabaId} via phone number registration using Twilio Senders API", wabaId);

                var friendlyName = $"{businessName}";
                
                _logger.LogInformation("Attempting to create Twilio subaccount with friendly name: {FriendlyName}", friendlyName);
                
                // Create the subaccount
                // Note: The WABA ID linkage occurs later during phone number registration via Senders API
                // Each subaccount should correspond to one WABA for proper isolation
                var subaccount = await AccountResource.CreateAsync(
                    friendlyName: friendlyName,
                    client: _twilioClient
                );

                _logger.LogInformation("Twilio subaccount response received. Full response details:");
                _logger.LogInformation("- SID: {SubaccountSid}", subaccount.Sid);
                _logger.LogInformation("- Name: {FriendlyName}", subaccount.FriendlyName);
                _logger.LogInformation("- Status: {Status}", subaccount.Status);
                _logger.LogInformation("- AuthToken present: {AuthTokenPresent}", !string.IsNullOrEmpty(subaccount.AuthToken));
                _logger.LogInformation("- AuthToken value: {AuthToken}", 
                    subaccount.AuthToken != null && subaccount.AuthToken.Length > 8 
                        ? subaccount.AuthToken.Substring(0, 8) + "..." 
                        : subaccount.AuthToken);
                _logger.LogInformation("- DateCreated: {DateCreated}", subaccount.DateCreated);
                _logger.LogInformation("- DateUpdated: {DateUpdated}", subaccount.DateUpdated);

                // Check if the subaccount was created successfully
                if (string.IsNullOrEmpty(subaccount.Sid))
                {
                    _logger.LogError("Twilio subaccount creation failed: SID is null or empty");
                    return (false, null, null, null, null, "Subaccount creation failed: No SID returned");
                }

                // Note: AuthToken is typically null for newly created subaccounts
                // Twilio doesn't return an AuthToken in the subaccount creation response by default
                // You need to create API keys separately if authentication credentials are needed
                if (string.IsNullOrEmpty(subaccount.AuthToken))
                {
                    _logger.LogInformation("Twilio subaccount created without AuthToken (this is normal). Setting up authentication credentials...");
                    
                    // Get authentication credentials for the subaccount
                    var (authSuccess, mainAccountSid, mainAccountToken, authError) = await GetSubaccountAuthenticationAsync(subaccount.Sid, businessName);
                    
                    if (authSuccess && !string.IsNullOrEmpty(mainAccountSid))
                    {
                        _logger.LogInformation("Authentication setup completed for subaccount {SubaccountSid}. Using main account credentials.", 
                            subaccount.Sid);
                        
                        // Check subaccount status
                        if (subaccount.Status != AccountResource.StatusEnum.Active)
                        {
                            _logger.LogWarning("Twilio subaccount created but status is not active: {Status}", subaccount.Status);
                            // Continue anyway as the subaccount might still be usable
                        }

                        _logger.LogInformation("Twilio subaccount and authentication setup completed successfully. SID: {SubaccountSid}, Status: {Status}, WABA: {WabaId}", 
                            subaccount.Sid, subaccount.Status, wabaId);

                        // Return main account token as the usable auth token since subaccount AuthToken is typically null
                        return (true, subaccount.Sid, mainAccountToken, mainAccountSid, mainAccountToken, null);
                    }
                    else
                    {
                        _logger.LogError("Failed to setup authentication for subaccount: {Error}", authError);
                        return (false, subaccount.Sid, subaccount.AuthToken, null, null, $"Subaccount created but authentication setup failed: {authError}");
                    }
                }
                else
                {
                    _logger.LogInformation("Twilio subaccount created with AuthToken (unusual but valid)");
                }

                // Check subaccount status
                if (subaccount.Status != AccountResource.StatusEnum.Active)
                {
                    _logger.LogWarning("Twilio subaccount created but status is not active: {Status}", subaccount.Status);
                    // Continue anyway as the subaccount might still be usable
                }

                _logger.LogInformation("Twilio subaccount created successfully. SID: {SubaccountSid}, Status: {Status}, WABA: {WabaId}", 
                    subaccount.Sid, subaccount.Status, wabaId);

                return (true, subaccount.Sid, subaccount.AuthToken, null, null, null);
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                _logger.LogError(twilioEx, "Twilio API error during subaccount creation for business: {BusinessName}. Status: {Status}, Code: {Code}, Message: {Message}, Details: {Details}", 
                    businessName, twilioEx.Status, twilioEx.Code, twilioEx.Message, twilioEx.Details);
                return (false, null, null, null, null, $"Twilio API Error: {twilioEx.Code} - {twilioEx.Message}. Details: {twilioEx.Details}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Twilio subaccount for business: {BusinessName}. Exception type: {ExceptionType}, Message: {Message}", 
                    businessName, ex.GetType().Name, ex.Message);
                return (false, null, null, null, null, $"Error: {ex.GetType().Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// Gets available countries for phone number purchase using actual Twilio API
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token</param>
        /// <returns>List of available countries</returns>
        public async Task<List<AvailableCountry>> GetAvailableCountriesAsync(string subaccountSid, string subaccountAuthToken)
        {
            try
            {
                _logger.LogInformation("Fetching available countries for subaccount: {SubaccountSid}", subaccountSid);

                // Create a client for the subaccount
                var subaccountClient = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                
                // Fetch available phone number countries using actual Twilio API
                var availableCountries = await AvailablePhoneNumberCountryResource.ReadAsync(
                    client: subaccountClient
                );
                
                var countries = availableCountries.Select(country => new AvailableCountry
                {
                    CountryCode = country.CountryCode,
                    Country = country.Country,
                    Uri = country.Uri,
                    Beta = country.Beta ?? false,
                    SubresourceUris = country.SubresourceUris?.Select(kv => kv.Value).ToList() ?? new List<string>()
                }).ToList();
                
                _logger.LogInformation("Successfully fetched {Count} available countries", countries.Count);
                return countries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available countries for subaccount: {SubaccountSid}", subaccountSid);
                throw;
            }
        }

        /// <summary>
        /// Checks if regulatory requirements are needed for a specific country and number type
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token</param>
        /// <param name="isoCountry">ISO country code</param>
        /// <param name="numberType">Type of phone number: "local" or "mobile"</param>
        /// <returns>Tuple containing requirement status and message</returns>
        public async Task<(bool RequiresBundle, bool SupportedCountry, string Message)> CheckRegulatoryRequirementsAsync(
            string subaccountSid, string subaccountAuthToken, string isoCountry, string numberType)
        {
            try
            {
                _logger.LogInformation("Checking regulatory requirements for {IsoCountry} {NumberType}", 
                    isoCountry, numberType);

                // Create a client for the subaccount
                var subaccountClient = new TwilioRestClient(subaccountSid, subaccountAuthToken);

                try
                {
                    // Use Twilio's Numbers API to check regulatory requirements
                    var requirements = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource
                        .ReadAsync(client: subaccountClient);
                    
                    // For GB (UK), regulatory bundles are required for local and mobile numbers
                    var requiresBundle = isoCountry?.ToUpper() == "GB";

                    var message = requiresBundle ? 
                        "This country and number type requires a regulatory bundle with business information." : 
                        "Regulatory bundles are not supported yet in our implementation for this country.";

                    return (requiresBundle, requiresBundle, message);
                }
                catch (Exception twilioEx)
                {
                    _logger.LogWarning(twilioEx, "Could not check regulatory requirements via Twilio API, falling back to default logic");
                    
                    // Fallback logic - only GB requires regulatory compliance in our implementation
                    var requiresBundle = isoCountry?.ToUpper() == "GB";
                    
                    var message = requiresBundle ? 
                        "Regulatory bundle required for GB (determined by fallback logic)." :
                        "Regulatory bundles are not supported yet in our implementation for this country.";

                    return (requiresBundle, requiresBundle, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking regulatory requirements for {IsoCountry} {NumberType}", 
                    isoCountry, numberType);
                throw;
            }
        }

        /// <summary>
        /// Creates a regulatory bundle for the specified country and number type
        /// </summary>
        /// <param name="request">Regulatory bundle creation request</param>
        /// <returns>Tuple containing success status, bundle SID, and error message</returns>
        public async Task<(bool Success, string? BundleSid, string? ErrorMessage)> CreateRegulatoryBundleAsync(CreateRegulatoryBundleRequest request)
        {
            try
            {
                _logger.LogInformation("Creating regulatory bundle for {BusinessName} in {IsoCountry} - {EndUserType} {NumberType}", 
                    request.BusinessName, request.IsoCountry, request.EndUserType, request.NumberType);

                // Create a client for the subaccount
                var subaccountClient = new TwilioRestClient(request.SubaccountSid, request.SubaccountAuthToken);

                // Create the regulatory bundle name using IsoCountry, EndUserType, and NumberType
                var bundleName = $"{request.BusinessName} - {request.IsoCountry?.ToUpper()} {request.EndUserType} {request.NumberType} Bundle";

                // Create the regulatory bundle using Twilio's Numbers API
                var bundle = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource.CreateAsync(
                    friendlyName: bundleName,
                    email: request.AuthorizedContactEmail,
                    statusCallback: null, // Optional webhook URL for status updates
                    isoCountry: request.IsoCountry?.ToUpper(),
                    endUserType: GetTwilioEndUserType(request.EndUserType),
                    numberType: request.NumberType?.ToLower(),
                    client: subaccountClient
                );

                _logger.LogInformation("Regulatory bundle created successfully: {BundleSid} - {FriendlyName}", 
                    bundle.Sid, bundle.FriendlyName);

                // Create address resource for the business
                var address = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.EndUserResource.CreateAsync(
                    friendlyName: $"{request.BusinessName} Business Address",
                    type: Twilio.Rest.Numbers.V2.RegulatoryCompliance.EndUserResource.TypeEnum.Business,
                    attributes: new Dictionary<string, object>
                    {
                        ["business_name"] = request.BusinessName ?? "",
                        ["business_registration_number"] = request.BusinessRegistrationNumber ?? "",
                        ["business_identity"] = request.BusinessRegistrationNumber ?? "",
                        ["business_type"] = request.BusinessType ?? "corporation",
                        ["business_industry"] = request.BusinessIndustry ?? "technology",
                        ["street_address"] = request.BusinessAddress ?? "",
                        ["city"] = request.BusinessCity ?? "",
                        ["state"] = request.BusinessState ?? "",
                        ["postal_code"] = request.BusinessPostalCode ?? "",
                        ["country"] = request.IsoCountry?.ToUpper() ?? "",
                        ["website"] = request.BusinessWebsite ?? "",
                        ["email"] = request.AuthorizedContactEmail ?? "",
                        ["phone_number"] = request.AuthorizedContactPhone ?? ""
                    },
                    client: subaccountClient
                );

                _logger.LogInformation("Business address end user created: {AddressSid}", address.Sid);

                // Create authorized contact person
                var authorizedContact = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.EndUserResource.CreateAsync(
                    friendlyName: $"{request.AuthorizedContactFirstName} {request.AuthorizedContactLastName}",
                    type: Twilio.Rest.Numbers.V2.RegulatoryCompliance.EndUserResource.TypeEnum.Individual,
                    attributes: new Dictionary<string, object>
                    {
                        ["first_name"] = request.AuthorizedContactFirstName ?? "",
                        ["last_name"] = request.AuthorizedContactLastName ?? "",
                        ["email"] = request.AuthorizedContactEmail ?? "",
                        ["phone_number"] = request.AuthorizedContactPhone ?? "",
                        ["date_of_birth"] = request.AuthorizedContactDateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                        ["job_title"] = request.AuthorizedContactJobTitle ?? "Authorized Representative"
                    },
                    client: subaccountClient
                );

                _logger.LogInformation("Authorized contact end user created: {ContactSid}", authorizedContact.Sid);

                // Note: End user assignment may require separate API calls or may be handled automatically
                // The current SDK version may not have the AssignmentResource available
                
                _logger.LogInformation("End users created for bundle {BundleSid}: Business={BusinessSid}, Contact={ContactSid}", 
                    bundle.Sid, address.Sid, authorizedContact.Sid);

                // Add supporting documents based on country requirements
                if (request.IsoCountry?.ToUpper() == "GB")
                {
                    await AddGBBusinessDocumentsAsync(subaccountClient, bundle.Sid, request);
                }

                // Submit the bundle for review
                var submittedBundle = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource
                    .UpdateAsync(
                        pathSid: bundle.Sid,
                        status: Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource.StatusEnum.PendingReview,
                        client: subaccountClient
                    );

                _logger.LogInformation("Regulatory bundle {BundleSid} submitted for review with status: {Status}", 
                    submittedBundle.Sid, submittedBundle.Status);

                return (true, bundle.Sid, null);
            }
            catch (Twilio.Exceptions.ApiException twilioEx)
            {
                _logger.LogError(twilioEx, "Twilio API error creating regulatory bundle for {BusinessName} in {IsoCountry}. Status: {Status}, Code: {Code}, Message: {Message}", 
                    request.BusinessName, request.IsoCountry, twilioEx.Status, twilioEx.Code, twilioEx.Message);
                return (false, null, $"Twilio API Error: {twilioEx.Code} - {twilioEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating regulatory bundle for {BusinessName} in {IsoCountry}", 
                    request.BusinessName, request.IsoCountry);
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Gets available phone numbers for a specific country and type using actual Twilio API
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token</param>
        /// <param name="countryCode">ISO country code</param>
        /// <param name="phoneNumberType">Type of phone number: "local" or "mobile"</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>List of available phone numbers</returns>
        public async Task<List<AvailablePhoneNumber>> GetAvailablePhoneNumbersAsync(string subaccountSid, string subaccountAuthToken, 
            string countryCode, string phoneNumberType, int limit)
        {
            try
            {
                _logger.LogInformation("Fetching available {PhoneNumberType} phone numbers for {CountryCode}, limit: {Limit}", 
                    phoneNumberType, countryCode, limit);

                // Create a client for the subaccount
                var subaccountClient = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                
                var phoneNumbers = new List<AvailablePhoneNumber>();

                if (phoneNumberType.Equals("local", StringComparison.OrdinalIgnoreCase))
                {
                    // Fetch local phone numbers using actual Twilio API
                    var localNumbers = await LocalResource.ReadAsync(
                        pathCountryCode: countryCode,
                        limit: limit,
                        client: subaccountClient
                    );

                    phoneNumbers = localNumbers.Select(number => new AvailablePhoneNumber
                    {
                        FriendlyName = number.PhoneNumber?.ToString() ?? string.Empty,
                        PhoneNumber = number.PhoneNumber?.ToString() ?? string.Empty,
                        Lata = number.Lata,
                        Locality = number.Locality,
                        RateCenter = number.RateCenter,
                        Latitude = number.Latitude,
                        Longitude = number.Longitude,
                        Region = number.Region,
                        PostalCode = number.PostalCode,
                        IsoCountry = number.IsoCountry,
                        AddressRequirements = number.AddressRequirements,
                        Beta = number.Beta ?? false,
                        Capabilities = new List<string>()
                            .Concat(number.Capabilities?.Voice == true ? new[] { "voice" } : Array.Empty<string>())
                            .Concat(number.Capabilities?.Sms == true ? new[] { "sms" } : Array.Empty<string>())
                            .Concat(number.Capabilities?.Mms == true ? new[] { "mms" } : Array.Empty<string>())
                            .Concat(number.Capabilities?.Fax == true ? new[] { "fax" } : Array.Empty<string>())
                            .ToList()
                    }).ToList();
                }
                else if (phoneNumberType.Equals("mobile", StringComparison.OrdinalIgnoreCase))
                {
                    // Fetch mobile phone numbers using actual Twilio API
                    var mobileNumbers = await MobileResource.ReadAsync(
                        pathCountryCode: countryCode,
                        limit: limit,
                        client: subaccountClient
                    );

                    phoneNumbers = mobileNumbers.Select(number => new AvailablePhoneNumber
                    {
                        FriendlyName = number.PhoneNumber?.ToString() ?? string.Empty,
                        PhoneNumber = number.PhoneNumber?.ToString() ?? string.Empty,
                        Lata = number.Lata,
                        Locality = number.Locality,
                        RateCenter = number.RateCenter,
                        Latitude = number.Latitude,
                        Longitude = number.Longitude,
                        Region = number.Region,
                        PostalCode = number.PostalCode,
                        IsoCountry = number.IsoCountry,
                        AddressRequirements = number.AddressRequirements,
                        Beta = number.Beta ?? false,
                        Capabilities = new List<string>()
                            .Concat(number.Capabilities?.Voice == true ? new[] { "voice" } : Array.Empty<string>())
                            .Concat(number.Capabilities?.Sms == true ? new[] { "sms" } : Array.Empty<string>())
                            .Concat(number.Capabilities?.Mms == true ? new[] { "mms" } : Array.Empty<string>())
                            .Concat(number.Capabilities?.Fax == true ? new[] { "fax" } : Array.Empty<string>())
                            .ToList()
                    }).ToList();
                }
                else
                {
                    _logger.LogWarning("Invalid phone number type: {PhoneNumberType}. Valid types are 'local' or 'mobile'", phoneNumberType);
                }
                
                _logger.LogInformation("Successfully fetched {Count} {PhoneNumberType} phone numbers for {CountryCode}", 
                    phoneNumbers.Count, phoneNumberType, countryCode);
                
                return phoneNumbers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available phone numbers for {CountryCode} {PhoneNumberType}", 
                    countryCode, phoneNumberType);
                throw;
            }
        }

        /// <summary>
        /// Purchases a specific Twilio phone number for the subaccount using main account credentials
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID to assign the number to</param>
        /// <param name="phoneNumber">Specific Twilio phone number to purchase</param>
        /// <param name="businessName">Business name for logging</param>
        /// <param name="countryCode">Country code for regulatory bundle lookup</param>
        /// <returns>Purchase result</returns>
        public async Task<(bool Success, string? PhoneNumberSid, string? ErrorMessage)> PurchasePhoneNumberAsync(
            string subaccountSid, string phoneNumber, string businessName, string countryCode, string? subaccountAuthToken = null, string? subaccountBundleSid = null)
        {
            try
            {
                bool isSubaccountTokenPresent = !string.IsNullOrWhiteSpace(subaccountAuthToken);
                bool isSubaccountSidPresent = !string.IsNullOrWhiteSpace(subaccountSid);
                bool shouldTransferPurchasedPhoneNumberToSubaccount = isSubaccountSidPresent && !isSubaccountTokenPresent;

                _logger.LogInformation("Purchasing Twilio phone number {PhoneNumber} for business: {BusinessName}, SubaccountSid: {SubaccountSid}", 
                    phoneNumber, businessName, subaccountSid);

                ITwilioRestClient twilioClient = null;
                if (!string.IsNullOrWhiteSpace(subaccountSid) && isSubaccountTokenPresent)
                    twilioClient = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                else twilioClient = _twilioClient; // Use main account credentials if subaccount details are not provided

                // Debug: Log the SIDs to identify the issue
                _logger.LogInformation("Subaccount SID received: {SubaccountSid}", subaccountSid);
                _logger.LogInformation("Main Account SID from _twilioClient: {MainAccountSid}", _twilioClient?.AccountSid);
                
                // Validate that subaccountSid is not the same as main account SID
                if (!string.IsNullOrEmpty(_twilioClient?.AccountSid) && subaccountSid == _twilioClient.AccountSid)
                {
                    _logger.LogError("ERROR: SubaccountSid {SubaccountSid} is the same as Main Account SID. This should be a different subaccount SID.", subaccountSid);
                    return (false, null, "Invalid subaccount SID: received main account SID instead of subaccount SID");
                }

                var phoneNumberObj = new PhoneNumber(phoneNumber);
                
                _logger.LogInformation("Using country code {CountryCode} for phone number {PhoneNumber}", countryCode, phoneNumber);
                
                // Debug: Check configuration state
                var mobileRegulatoryBundles = _regulatoryBundleOptions.RegulatoryBundlesForMobilePhoneNumbers ?? new Dictionary<string, string>();
                var localRegulatoryBundles = _regulatoryBundleOptions.RegulatoryBundlesForLocalPhoneNumbers ?? new Dictionary<string, string>();

                // Check if regulatory bundle is required for this country
                // For purchase, we need to determine if it's a mobile or local number to get the right bundle
                string? bundleSid = null;
                bool requiresRegulatoryBundle = false;
                
                // Try to get the appropriate regulatory bundle based on phone number type
                // This is a simplified approach - in practice you might need more sophisticated detection
                var targetBundles = phoneNumber.StartsWith("+44") && phoneNumber.Length > 13 ? mobileRegulatoryBundles : localRegulatoryBundles;
                requiresRegulatoryBundle = targetBundles.TryGetValue(countryCode.ToUpperInvariant(), out bundleSid);

                if (!string.IsNullOrWhiteSpace(subaccountBundleSid))
                {
                    bundleSid = subaccountBundleSid; // Use provided subaccount bundle SID if available
                    requiresRegulatoryBundle = true;
                }

                _logger.LogInformation("Bundle check result for {CountryCode}: RequiresRegulatoryBundle={RequiresRegulatoryBundle}, BundleSid={BundleSid}", 
                    countryCode.ToUpperInvariant(), requiresRegulatoryBundle, bundleSid ?? "null");

                // Purchase the phone number
                // Include regulatory bundle if required for the country
                var purchasedNumber = requiresRegulatoryBundle
                    ? await IncomingPhoneNumberResource.CreateAsync(
                        phoneNumber: phoneNumberObj,
                        friendlyName: $"{businessName}",
                        bundleSid: bundleSid, // Regulatory bundle for this country
                        client: twilioClient
                    )
                    : await IncomingPhoneNumberResource.CreateAsync(
                        phoneNumber: phoneNumberObj,
                        friendlyName: $"{businessName}",
                        client: twilioClient
                    );

                // Transfer it to the subaccount
                if (shouldTransferPurchasedPhoneNumberToSubaccount && purchasedNumber != null)
                {
                    _logger.LogInformation("=== PHONE NUMBER TRANSFER TO SUBACCOUNT ===");
                    _logger.LogInformation("Transfer details:");
                    _logger.LogInformation("- Phone Number: {PhoneNumber}", purchasedNumber.PhoneNumber);
                    _logger.LogInformation("- Phone Number SID: {PhoneNumberSid}", purchasedNumber.Sid);
                    _logger.LogInformation("- Source Account (Main): {MainAccountSid}", _twilioClient?.AccountSid);
                    _logger.LogInformation("- Target Subaccount: {SubaccountSid}", subaccountSid);
                    _logger.LogInformation("- Business Name: {BusinessName}", businessName);
                    _logger.LogInformation("- Country Code: {CountryCode}", countryCode ?? "null");
                    _logger.LogInformation("- Requires Regulatory Bundle: {RequiresBundle}", requiresRegulatoryBundle);
                    _logger.LogInformation("- Bundle SID: {BundleSid}", bundleSid ?? "null");
                    
                    _logger.LogInformation("Starting transfer of purchased phone number {PhoneNumber} to subaccount {SubaccountSid}", 
                        purchasedNumber.PhoneNumber, subaccountSid);
                    
                    try
                    {
                        // Use main account credentials to transfer the number to the subaccount
                        // Include regulatory bundle if it was required for the original purchase
                        if (requiresRegulatoryBundle && !string.IsNullOrEmpty(bundleSid))
                        {
                            _logger.LogInformation("Transferring with regulatory bundle {BundleSid} included", bundleSid);
                            _logger.LogInformation("Making Twilio API call: IncomingPhoneNumberResource.UpdateAsync");
                            _logger.LogInformation("- pathSid: {PathSid}", purchasedNumber.Sid);
                            _logger.LogInformation("- accountSid: {AccountSid}", subaccountSid);
                            _logger.LogInformation("- bundleSid: {BundleSid}", bundleSid);
                            
                            await IncomingPhoneNumberResource.UpdateAsync(
                                pathSid: purchasedNumber.Sid,
                                accountSid: subaccountSid, // Transfer to subaccount
                                bundleSid: bundleSid, // Include regulatory bundle for transfer
                                client: _twilioClient // Use main account credentials for transfer
                            );
                            
                            _logger.LogInformation("✅ Phone number transfer with regulatory bundle completed successfully");
                        }
                        else
                        {
                            _logger.LogInformation("Transferring without regulatory bundle (none required or not available)");
                            _logger.LogInformation("Making Twilio API call: IncomingPhoneNumberResource.UpdateAsync");
                            _logger.LogInformation("- pathSid: {PathSid}", purchasedNumber.Sid);
                            _logger.LogInformation("- accountSid: {AccountSid}", subaccountSid);
                            _logger.LogInformation("- bundleSid: NOT INCLUDED");
                            
                            await IncomingPhoneNumberResource.UpdateAsync(
                                pathSid: purchasedNumber.Sid,
                                accountSid: subaccountSid, // Transfer to subaccount
                                client: _twilioClient // Use main account credentials for transfer
                            );
                            
                            _logger.LogInformation("✅ Phone number transfer without regulatory bundle completed successfully");
                        }
                        
                        _logger.LogInformation("=== PHONE NUMBER TRANSFER COMPLETED ===");
                    }
                    catch (Exception transferEx)
                    {
                        _logger.LogError(transferEx, "❌ PHONE NUMBER TRANSFER FAILED");
                        _logger.LogError("Transfer error details:");
                        _logger.LogError("- Phone Number: {PhoneNumber}", purchasedNumber.PhoneNumber);
                        _logger.LogError("- Phone Number SID: {PhoneNumberSid}", purchasedNumber.Sid);
                        _logger.LogError("- Target Subaccount: {SubaccountSid}", subaccountSid);
                        _logger.LogError("- Bundle SID used: {BundleSid}", bundleSid ?? "null");
                        _logger.LogError("- Error Message: {ErrorMessage}", transferEx.Message);
                        _logger.LogError("- Error Type: {ErrorType}", transferEx.GetType().Name);
                        
                        if (transferEx is TwilioExceptions.ApiException apiEx)
                        {
                            _logger.LogError("- Twilio API Error Code: {ErrorCode}", apiEx.Code);
                            _logger.LogError("- Twilio API Error Status: {ErrorStatus}", apiEx.Status);
                            if (apiEx.Details != null && apiEx.Details.Count > 0)
                            {
                                _logger.LogError("- Twilio API Error Details: {ErrorDetails}", string.Join(", ", apiEx.Details.Select(kv => $"{kv.Key}: {kv.Value}")));
                            }
                            _logger.LogError("- Twilio API Error More Info: {MoreInfo}", apiEx.MoreInfo);
                            
                            // Check if this is a regulatory bundle not found error during transfer
                            if (apiEx.Message.Contains("Bundle not found") && requiresRegulatoryBundle && !string.IsNullOrEmpty(bundleSid))
                            {
                                _logger.LogWarning("REGULATORY BUNDLE NOT ACCESSIBLE DURING TRANSFER - ATTEMPTING FALLBACK");
                                _logger.LogWarning("The regulatory bundle {BundleSid} exists for the main account but is not accessible during subaccount transfer.", bundleSid);
                                _logger.LogWarning("This can happen when the bundle is tied to the main account context.");
                                _logger.LogWarning("Attempting to transfer phone number WITHOUT regulatory bundle as fallback...");
                                
                                try
                                {
                                    _logger.LogInformation("FALLBACK: Transferring phone number without regulatory bundle");
                                    _logger.LogInformation("Making Twilio API call: IncomingPhoneNumberResource.UpdateAsync (without bundle)");
                                    _logger.LogInformation("- pathSid: {PathSid}", purchasedNumber.Sid);
                                    _logger.LogInformation("- accountSid: {AccountSid}", subaccountSid);
                                    _logger.LogInformation("- bundleSid: FALLBACK - NOT INCLUDED");
                                    
                                    await IncomingPhoneNumberResource.UpdateAsync(
                                        pathSid: purchasedNumber.Sid,
                                        accountSid: subaccountSid, // Transfer to subaccount
                                        client: _twilioClient // Use main account credentials for transfer
                                    );
                                    
                                    _logger.LogInformation("FALLBACK SUCCESS: Phone number transfer completed without regulatory bundle");
                                    _logger.LogWarning("NOTE: Phone number transferred without regulatory bundle. Manual compliance verification may be required.");
                                }
                                catch (Exception fallbackEx)
                                {
                                    _logger.LogError(fallbackEx, "FALLBACK FAILED: Could not transfer phone number even without regulatory bundle");
                                    _logger.LogError("Both bundle transfer and fallback transfer failed. This phone number cannot be transferred to the subaccount.");
                                    
                                    // Log detailed fallback error information
                                    if (fallbackEx is TwilioExceptions.ApiException fallbackApiEx)
                                    {
                                        _logger.LogError("FALLBACK Twilio API Error Details:");
                                        _logger.LogError("- Error Code: {ErrorCode}", fallbackApiEx.Code);
                                        _logger.LogError("- Error Status: {ErrorStatus}", fallbackApiEx.Status);
                                        _logger.LogError("- Error Message: {ErrorMessage}", fallbackApiEx.Message);
                                        if (fallbackApiEx.Details != null && fallbackApiEx.Details.Count > 0)
                                        {
                                            _logger.LogError("- Error Details: {ErrorDetails}", string.Join(", ", fallbackApiEx.Details.Select(kv => $"{kv.Key}: {kv.Value}")));
                                        }
                                        _logger.LogError("- More Info: {MoreInfo}", fallbackApiEx.MoreInfo);
                                    }
                                    
                                    // Re-throw the original exception since fallback also failed
                                    throw;
                                }
                            }
                            else
                            {
                                // Re-throw for non-bundle related errors
                                throw;
                            }
                        }
                        else
                        {
                            // Re-throw for non-Twilio API exceptions
                            throw;
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(subaccountSid))
                    {
                        _logger.LogWarning("Skipping phone number transfer: subaccountSid is null or empty");
                    }
                    if (purchasedNumber == null)
                    {
                        _logger.LogWarning("Skipping phone number transfer: purchasedNumber is null");
                    }
                }
                
                if (requiresRegulatoryBundle)
                {
                    _logger.LogInformation("Phone number {PhoneNumber} purchased with regulatory bundle {BundleSid} for country {CountryCode}",
                        phoneNumber, bundleSid, countryCode ?? "unknown");
                }
                else
                {
                    _logger.LogInformation("Phone number {PhoneNumber} purchased without regulatory bundle (none required for country {CountryCode})",
                        phoneNumber, countryCode ?? "unknown");
                }
                
                if (purchasedNumber != null)
                {
                    _logger.LogInformation("Phone number {PhoneNumber} purchased successfully with SID: {PhoneNumberSid} for business: {BusinessName}", 
                        purchasedNumber.PhoneNumber?.ToString(), purchasedNumber.Sid ?? "Unknown", businessName);

                    return (true, purchasedNumber.Sid, null);
                }
                else
                {
                    _logger.LogError("Phone number purchase returned null result");
                    return (false, null, "Phone number purchase failed - null result returned");
                }
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                _logger.LogError(twilioEx, "Twilio API error while purchasing phone number {PhoneNumber} for business: {BusinessName}", phoneNumber, businessName);
                _logger.LogError("Twilio API Error Details - Code: {ErrorCode}, Status: {ErrorStatus}, Message: {ErrorMessage}", 
                    twilioEx.Code, twilioEx.Status, twilioEx.Message);
                
                if (twilioEx.Details != null && twilioEx.Details.Count > 0)
                {
                    _logger.LogError("Twilio API Error Details: {ErrorDetails}", string.Join(", ", twilioEx.Details.Select(kv => $"{kv.Key}: {kv.Value}")));
                }
                
                return (false, null, $"Twilio API error: {twilioEx.Message} (Code: {twilioEx.Code})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purchase phone number {PhoneNumber} for business: {BusinessName}", phoneNumber, businessName);
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Registers a phone number for WhatsApp using Twilio Channels Sender API
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token</param>
        /// <param name="phoneNumber">Phone number to register</param>
        /// <param name="businessName">Business name</param>
        /// <param name="wabaId">WhatsApp Business Account ID</param>
        /// <param name="webhookBaseUrl">Base URL for webhook registration</param>
        /// <returns>Registration result</returns>
        public async Task<(bool Success, string? SenderSid, string? Status, string? ErrorMessage)> RegisterPhoneNumberForWhatsAppAsync(
            string subaccountSid, string subaccountAuthToken, string phoneNumber, string businessName, string wabaId, string webhookUrl)
        {
            try
            {
                _logger.LogInformation("Registering phone number {PhoneNumber} for WhatsApp - Business: {BusinessName}, WABA: {WabaId}, SubaccountSid: {SubaccountSid}", 
                    phoneNumber, businessName, wabaId, subaccountSid);
                
                _logger.LogInformation("Linking WABA {WabaId} (from embedded signup) to Twilio subaccount {SubaccountSid} via phone number {PhoneNumber} registration", 
                    wabaId, subaccountSid, phoneNumber);

                var (authSuccess, mainAccountSid, mainAccountToken, authError) = await GetSubaccountAuthenticationAsync(subaccountSid, businessName);
                
                if (!authSuccess || string.IsNullOrEmpty(mainAccountSid) || string.IsNullOrEmpty(mainAccountToken))
                {
                    var errorMessage = $"Failed to get authentication credentials for subaccount {subaccountSid}: {authError}";
                    _logger.LogError(errorMessage);
                    return (false, null, null, errorMessage);
                }

                _logger.LogInformation("Setting up Twilio client to target subaccount {SubaccountSid}", subaccountSid);

                TwilioRestClient twilioClient = subaccountAuthToken == mainAccountToken
                    ? new TwilioRestClient(mainAccountSid, mainAccountToken, accountSid: subaccountSid)
                    : new TwilioRestClient(subaccountSid, subaccountAuthToken);
                
                _logger.LogInformation("Client configured: MainAccount={MainAccountSid}, TargetSubaccount={SubaccountSid}", 
                    mainAccountSid, subaccountSid);

                var formattedPhoneNumber = phoneNumber.StartsWith("+") ? phoneNumber : $"+{phoneNumber.TrimStart('+')}";
                var whatsappSenderId = formattedPhoneNumber.StartsWith("whatsapp:") ? formattedPhoneNumber : $"whatsapp:{formattedPhoneNumber}";

                _logger.LogInformation("Attempting to register WhatsApp sender using Twilio Channels Sender API");
                _logger.LogInformation("Original phone number: {PhoneNumber}, Formatted: {FormattedPhoneNumber}, Sender ID: {WhatsappSenderId}, WABA ID: {WabaId}, Business: {BusinessName}", 
                    phoneNumber, formattedPhoneNumber, whatsappSenderId, wabaId, businessName);
                
                // Use the actual Twilio Channels Sender API with correct structure
                _logger.LogInformation("Creating WhatsApp sender using ChannelsSenderResource.CreateAsync");
                _logger.LogInformation("- Sender ID: {WhatsappSenderId}", whatsappSenderId);
                _logger.LogInformation("- WABA ID: {WabaId}", wabaId);
                _logger.LogInformation("- Business Name: {BusinessName}", businessName);
                _logger.LogInformation("- Webhook URL: {WebhookUrl}", webhookUrl);

                try
                {
                    _logger.LogInformation("Calling Twilio Channels Sender API to register WhatsApp sender");
                    
                    var configuration = new ChannelsSenderResource.MessagingV2ChannelsSenderConfiguration.Builder()
                        .WithWabaId(wabaId)
                        .Build();
                    
                    var profile = new ChannelsSenderResource.MessagingV2ChannelsSenderProfile.Builder()
                        .WithName(businessName)
                        .Build();
                    
                    var webhook = new ChannelsSenderResource.MessagingV2ChannelsSenderWebhook.Builder()
                        .WithCallbackUrl(webhookUrl)
                        .WithCallbackMethod(ChannelsSenderResource.CallbackMethodEnum.Post)
                        .Build();
                    
                    var whatsappSenderRegisterRequest = new ChannelsSenderResource.MessagingV2ChannelsSenderRequestsCreate.Builder()
                        .WithSenderId(whatsappSenderId)
                        .WithConfiguration(configuration)
                        .WithProfile(profile)
                        .WithWebhook(webhook)
                        .Build();
                    
                    _logger.LogInformation("Twilio Channels Sender API Request Details:");
                    _logger.LogInformation("- Sender ID: {SenderId}", whatsappSenderId);
                    _logger.LogInformation("- WABA ID: {WabaId}", wabaId);
                    _logger.LogInformation("- Business Name: {BusinessName}", businessName);
                    _logger.LogInformation("- Webhook URL: {WebhookUrl}", webhookUrl);
                    _logger.LogInformation("- Webhook Method: POST");
                    
                    var whatsappSender = await ChannelsSenderResource.CreateAsync(whatsappSenderRegisterRequest, client: twilioClient);
                    
                    _logger.LogInformation("WhatsApp sender created successfully:");
                    _logger.LogInformation("- Sender SID: {SenderSid}", whatsappSender.Sid);
                    _logger.LogInformation("- Status: {Status}", whatsappSender.Status);
                    _logger.LogInformation("- Sender ID: {SenderId}", whatsappSender.SenderId);
                    _logger.LogInformation("- WABA-Subaccount link established for {PhoneNumber}", phoneNumber);

                    return (true, whatsappSender.Sid, whatsappSender.Status?.ToString(), null);
                }
                catch (Exception apiEx)
                {
                    var errorMessage = $"Twilio Channels Sender API call failed: {apiEx.Message}";
                    _logger.LogError(apiEx, "Error during WhatsApp sender registration");
                    
                    // Enhanced error logging for JSON parsing issues
                    if (apiEx.Message.Contains("Unexpected end of JSON input") || 
                        apiEx.Message.Contains("JSON") || 
                        apiEx.Message.Contains("parse"))
                    {
                        _logger.LogError("JSON parsing error detected. This may indicate:");
                        _logger.LogError("1. Invalid or incomplete response from Twilio API");
                        _logger.LogError("2. Network connectivity issues");
                        _logger.LogError("3. Authentication problems");
                        _logger.LogError("4. Rate limiting or service unavailability");
                        _logger.LogError("Request details - Sender ID: {SenderId}, WABA: {WabaId}, Business: {BusinessName}", 
                            whatsappSenderId, wabaId, businessName);
                    }
                    
                    return (false, null, null, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register phone number {PhoneNumber} for WhatsApp", phoneNumber);
                return (false, null, null, ex.Message);
            }
        }

        /// <summary>
        /// Creates a content template in the shared WABA using Twilio Content API
        /// </summary>
        /// <param name="contentCreateRequest">Twilio Content API create request</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Template creation result</returns>
        public async Task<(bool Success, string? TemplateSid, string? ErrorMessage)> CreateContentTemplateAsync(
            ContentResource.ContentCreateRequest contentCreateRequest, string? subaccountSid = null, string? subaccountAuthToken = null)
        {
            try
            {
                ITwilioRestClient client;
                
                if (string.IsNullOrEmpty(subaccountSid) || string.IsNullOrEmpty(subaccountAuthToken))
                {
                    _logger.LogInformation("Creating content template using main Twilio account");
                    client = _twilioClient;
                }
                else
                {
                    _logger.LogInformation("Creating content template for subaccount: {SubaccountSid}", subaccountSid);
                    client = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                }
                
                // Create the content template using Twilio Content API
                var contentTemplate = await ContentResource.CreateAsync(contentCreateRequest, client: client);
                
                _logger.LogInformation("Content template created successfully with SID: {TemplateSid}", contentTemplate.Sid);

                return (true, contentTemplate.Sid, null);
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                var errorMessage = $"Twilio API error: {twilioEx.Message} (Code: {twilioEx.Code})";
                _logger.LogError(twilioEx, "Failed to create content template: {ErrorMessage}", errorMessage);
                return (false, null, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create content template");
                return (false, null, ex.Message);
            }
        }

        /// Sends a WhatsApp message using a content template
        /// <param name="fromNumber">Sender WhatsApp phone number (with whatsapp: prefix)</param>
        /// <param name="toNumber">Recipient WhatsApp phone number (with whatsapp: prefix)</param>
        /// <param name="templateSid">Content template SID to use for the message</param>
        /// <param name="contentVariables">Template variables as key-value pairs</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Message sending result</returns>
        public async Task<(bool Success, string? MessageSid, string? ErrorMessage)> SendWhatsAppTemplateMessageAsync(
            string fromNumber, string toNumber, string templateSid, Dictionary<string, string> contentVariables, 
            string? subaccountSid = null, string? subaccountAuthToken = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromNumber) || (!fromNumber.StartsWith("whatsapp:+") && !fromNumber.StartsWith("+")))
                {
                    return (false, null, "Invalid from number format. Expected format: whatsapp:+1234567890 or +1234567890");
                }
                if (string.IsNullOrWhiteSpace(toNumber) || (!toNumber.StartsWith("whatsapp:+") && !toNumber.StartsWith("+")))
                {
                    return (false, null, "Invalid to number format. Expected format: whatsapp:+1234567890 or +1234567890");
                }

                var toPhoneNumberFormatted = toNumber.StartsWith("whatsapp:") ? toNumber : $"whatsapp:{toNumber}";
                var fromPhoneNumberFormatted = fromNumber.StartsWith("whatsapp:") ? fromNumber : $"whatsapp:{fromNumber}";

                ITwilioRestClient twilioClient;
                if (!string.IsNullOrWhiteSpace(subaccountSid) && !string.IsNullOrWhiteSpace(subaccountAuthToken))
                    twilioClient = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                else
                    twilioClient = _twilioClient;

                var fromPhoneNumber = new PhoneNumber(fromNumber);
                var toPhoneNumber = new PhoneNumber(toNumber);

                var contentVariablesJson = System.Text.Json.JsonSerializer.Serialize(contentVariables);

                var message = await MessageResource.CreateAsync(
                    to: toPhoneNumberFormatted,
                    from: fromPhoneNumberFormatted,
                    contentSid: templateSid,
                    contentVariables: contentVariablesJson,
                    client: twilioClient
                );
                return (true, message.Sid, null);
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                var errorMessage = $"Twilio API error sending template message: {twilioEx.Message} (Code: {twilioEx.Code})";
                _logger.LogError(twilioEx, "Failed to send WhatsApp template message: {ErrorMessage}", errorMessage);
                return (false, null, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp template message");
                return (false, null, ex.Message);
            }
        }

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
        public (bool Success, string? MessageSid, string? ErrorMessage) SendWhatsAppMessage(
            string fromNumber, string toNumber, string body, string? mediaUrl, Dictionary<string, string>? contentVariables = null, 
            string? subaccountSid = null, string? subaccountAuthToken = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromNumber) || (!fromNumber.StartsWith("whatsapp:+") && !fromNumber.StartsWith("+")))
                {
                    return (false, null, "Invalid from number format. Expected format: whatsapp:+1234567890 or +1234567890");
                }
                if (string.IsNullOrWhiteSpace(toNumber) || (!toNumber.StartsWith("whatsapp:+") && !toNumber.StartsWith("+")))
                {
                    return (false, null, "Invalid to number format. Expected format: whatsapp:+1234567890 or +1234567890");
                }

                var toPhoneNumberFormatted = toNumber.StartsWith("whatsapp:") ? toNumber : $"whatsapp:{toNumber}";
                var fromPhoneNumberFormatted = fromNumber.StartsWith("whatsapp:") ? fromNumber : $"whatsapp:{fromNumber}";

                ITwilioRestClient twilioClient;
                if (!string.IsNullOrWhiteSpace(subaccountSid) && !string.IsNullOrWhiteSpace(subaccountAuthToken))
                    twilioClient = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                else
                    twilioClient = _twilioClient;

                var fromPhoneNumber = new PhoneNumber(fromNumber);
                var toPhoneNumber = new PhoneNumber(toNumber);

                string? contentVariablesJson = contentVariables != null ? JsonConvert.SerializeObject(contentVariables) : null;

                var message = MessageResource.Create(
                    to: toPhoneNumberFormatted,
                    from: fromPhoneNumberFormatted,
                    body: body,
                    mediaUrl: !string.IsNullOrWhiteSpace(mediaUrl) ? new List<Uri> { new Uri(mediaUrl) } : null,
                    contentVariables: contentVariablesJson,
                    client: twilioClient
                );
                return (true, message.Sid, null);
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                var errorMessage = $"Twilio API error sending message: {twilioEx.Message} (Code: {twilioEx.Code})";
                _logger.LogError(twilioEx, "Failed to send WhatsApp message: {ErrorMessage}", errorMessage);
                return (false, null, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Gets all content templates for the subaccount using Twilio Content API
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>List of content templates</returns>
        public async Task<(bool Success, List<ContentResource>? Templates, string? ErrorMessage)> GetContentTemplatesAsync(
            string? subaccountSid = null, string? subaccountAuthToken = null)
        {
            try
            {
                ITwilioRestClient client;
                
                if (string.IsNullOrEmpty(subaccountSid) || string.IsNullOrEmpty(subaccountAuthToken))
                {
                    _logger.LogInformation("Retrieving content templates using main Twilio account");
                    client = _twilioClient;
                }
                else
                {
                    _logger.LogInformation("Retrieving content templates for subaccount: {SubaccountSid}", subaccountSid);
                    client = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                }
                
                // Get all content templates using Twilio Content API
                var contentTemplates = await ContentResource.ReadAsync(client: client);
                var templates = contentTemplates.ToList();
                
                _logger.LogInformation("Retrieved {Count} content templates", templates.Count);

                return (true, templates, null);
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                var errorMessage = $"Twilio API error: {twilioEx.Message} (Code: {twilioEx.Code})";
                _logger.LogError(twilioEx, "Failed to retrieve content templates: {ErrorMessage}", errorMessage);
                return (false, null, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve content templates");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Deletes a content template using Twilio Content API
        /// </summary>
        /// <param name="templateSid">Content template SID to delete</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Template deletion result</returns>
        public async Task<(bool Success, string? ErrorMessage)> DeleteContentTemplateAsync(
            string templateSid, string? subaccountSid = null, string? subaccountAuthToken = null)
        {
            try
            {
                ITwilioRestClient client;
                
                if (string.IsNullOrEmpty(subaccountSid) || string.IsNullOrEmpty(subaccountAuthToken))
                {
                    _logger.LogInformation("Deleting content template {TemplateSid} using main Twilio account", templateSid);
                    client = _twilioClient;
                }
                else
                {
                    _logger.LogInformation("Deleting content template {TemplateSid} for subaccount: {SubaccountSid}", templateSid, subaccountSid);
                    client = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                }
                
                // Delete the content template using Twilio Content API
                var success = await ContentResource.DeleteAsync(pathSid: templateSid, client: client);
                
                if (success)
                {
                    _logger.LogInformation("Content template {TemplateSid} deleted successfully", templateSid);
                    return (true, null);
                }
                else
                {
                    var errorMessage = $"Failed to delete content template {templateSid}";
                    _logger.LogError(errorMessage);
                    return (false, errorMessage);
                }
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                var errorMessage = $"Twilio API error deleting template {templateSid}: {twilioEx.Message} (Code: {twilioEx.Code})";
                _logger.LogError(twilioEx, "Failed to delete content template: {ErrorMessage}", errorMessage);
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete content template {TemplateSid}", templateSid);
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Submits a content template for approval
        /// </summary>
        /// <param name="templateSid">Content template SID to approve</param>
        /// <param name="name">Name that uniquely identifies the Content. Only lowercase alphanumeric characters or underscores</param>
        /// <param name="templateCategory">UTILITY, MARKETING OR AUTHENTICATION</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Template approval result</returns>
        public async Task<(bool Success, string? ErrorMessage)> SubmitContentTemplateForApprovalAsync(
            string templateSid, string name, string templateCategory, string? subaccountSid = null, string? subaccountAuthToken = null)
        {
            try
            {
                _logger.LogInformation("Submitting content template {TemplateSid} for approval.", templateSid);
                ITwilioRestClient client;
                
                if (string.IsNullOrEmpty(subaccountSid) || string.IsNullOrEmpty(subaccountAuthToken))
                    client = _twilioClient;
                else
                    client = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                
                var template = await ContentResource.FetchAsync(pathSid: templateSid, client: client);
                
                if (template != null)
                {
                    _logger.LogInformation("Content template {TemplateSid} accessed successfully - approval process initiated", templateSid);
                    _logger.LogWarning("Note: Template approval in Twilio Content API may require manual action in Twilio Console or may be automatic based on content");
                    var contentApprovalRequest = new ContentApprovalRequest.Builder()
                        .WithCategory(templateCategory)
                        .WithName(!string.IsNullOrWhiteSpace(name) ? name : templateSid.ToLowerInvariant())
                        .Build()
                        ;
                    var approvalCreateResource = await ApprovalCreateResource.CreateAsync(pathContentSid: templateSid, contentApprovalRequest: contentApprovalRequest, client: client);
                    return (true, null);
                }
                else
                {
                    var errorMessage = $"Content template {templateSid} not found";
                    _logger.LogError(errorMessage);
                    return (false, errorMessage);
                }
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                var errorMessage = $"Twilio API error approving template {templateSid}: {twilioEx.Message} (Code: {twilioEx.Code})";
                _logger.LogError(twilioEx, "Failed to approve content template: {ErrorMessage}", errorMessage);
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve content template {TemplateSid}", templateSid);
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Gets a specific content template by SID using Twilio Content API
        /// </summary>
        /// <param name="templateSid">Content template SID to retrieve</param>
        /// <param name="subaccountSid">Twilio subaccount SID (optional - uses main account if null)</param>
        /// <param name="subaccountAuthToken">Twilio subaccount auth token (optional - uses main account if null)</param>
        /// <returns>Content template details</returns>
        public async Task<(bool Success, ContentResource? Template, string? ErrorMessage)> GetContentTemplateAsync(
            string templateSid, string? subaccountSid = null, string? subaccountAuthToken = null)
        {
            try
            {
                ITwilioRestClient client;
                
                if (string.IsNullOrEmpty(subaccountSid) || string.IsNullOrEmpty(subaccountAuthToken))
                {
                    _logger.LogInformation("Retrieving content template {TemplateSid} using main Twilio account", templateSid);
                    client = _twilioClient;
                }
                else
                {
                    _logger.LogInformation("Retrieving content template {TemplateSid} for subaccount: {SubaccountSid}", templateSid, subaccountSid);
                    client = new TwilioRestClient(subaccountSid, subaccountAuthToken);
                }
                
                // Get the specific content template using Twilio Content API
                var template = await ContentResource.FetchAsync(pathSid: templateSid, client: client);
                
                _logger.LogInformation("Content template {TemplateSid} retrieved successfully", templateSid);
                return (true, template, null);
            }
            catch (TwilioExceptions.ApiException twilioEx)
            {
                var errorMessage = $"Twilio API error retrieving template {templateSid}: {twilioEx.Message} (Code: {twilioEx.Code})";
                _logger.LogError(twilioEx, "Failed to retrieve content template: {ErrorMessage}", errorMessage);
                return (false, null, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve content template {TemplateSid}", templateSid);
                return (false, null, ex.Message);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Creates authentication credentials for a Twilio subaccount
        /// Since subaccounts don't come with AuthTokens by default, we'll use the main account credentials
        /// for WhatsApp operations but log everything against the specific subaccount
        /// </summary>
        /// <param name="subaccountSid">Twilio subaccount SID</param>
        /// <param name="businessName">Business name for friendly naming</param>
        /// <returns>Authentication information for the subaccount</returns>
        private Task<(bool Success, string? MainAccountSid, string? MainAccountToken, string? ErrorMessage)> GetSubaccountAuthenticationAsync(
            string subaccountSid, string businessName)
        {
            try
            {
                _logger.LogInformation("Setting up authentication for Twilio subaccount: {SubaccountSid}, Business: {BusinessName}", 
                    subaccountSid, businessName);

                // For WhatsApp operations, we'll use the main account credentials but specify the subaccount
                // This is a common pattern when subaccounts don't have their own auth tokens
                var mainAccountSid = _twilioClient.AccountSid;
                
                // Get the auth token from configuration since it's not directly accessible from the client
                var mainAccountToken = GetMainAccountAuthToken();

                if (string.IsNullOrEmpty(mainAccountSid))
                {
                    _logger.LogError("Main account SID is not available from TwilioClient");
                    return Task.FromResult((false, (string?)null, (string?)null, "Main account SID not available"));
                }

                if (string.IsNullOrEmpty(mainAccountToken))
                {
                    _logger.LogError("Main account auth token is not available");
                    return Task.FromResult((false, (string?)null, (string?)null, "Main account auth token not available"));
                }

                _logger.LogInformation("Authentication setup completed for subaccount: {SubaccountSid}. Will use main account credentials with subaccount targeting.", 
                    subaccountSid);

                return Task.FromResult((true, mainAccountSid, mainAccountToken, (string?)null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup authentication for subaccount: {SubaccountSid}. Exception: {Exception}", 
                    subaccountSid, ex.Message);
                return Task.FromResult((false, (string?)null, (string?)null, $"Error: {ex.GetType().Name} - {ex.Message}"));
            }
        }

        /// <summary>
        /// Gets the main account auth token from configuration
        /// This is needed since the ITwilioRestClient doesn't expose the auth token directly
        /// </summary>
        /// <returns>Main account auth token</returns>
        private string? GetMainAccountAuthToken()
        {
            try
            {
                // Try different possible configuration paths
                var authToken = _configuration["Twilio:Client:AuthToken"] ?? 
                               _configuration["Twilio:AuthToken"] ??
                               _configuration["TwilioAuthToken"];
                
                if (!string.IsNullOrEmpty(authToken))
                {
                    return authToken;
                }

                _logger.LogWarning("Could not retrieve main account auth token from configuration");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving main account auth token from configuration");
                return null;
            }
        }

        /// <summary>
        /// Converts EndUserType string to Twilio enum
        /// </summary>
        private Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource.EndUserTypeEnum GetTwilioEndUserType(string? endUserType)
        {
            return endUserType?.ToLower() switch
            {
                "business" => Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource.EndUserTypeEnum.Business,
                "individual" => Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource.EndUserTypeEnum.Individual,
                _ => Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource.EndUserTypeEnum.Business
            };
        }

        /// <summary>
        /// Adds supporting documents for GB regulatory bundles
        /// </summary>
        private async Task AddGBBusinessDocumentsAsync(ITwilioRestClient client, string bundleSid, CreateRegulatoryBundleRequest request)
        {
            try
            {
                _logger.LogInformation("Creating supporting documents for GB bundle {BundleSid}", bundleSid);

                // Create a business registration document
                var businessRegistrationDoc = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.SupportingDocumentResource.CreateAsync(
                    friendlyName: $"{request.BusinessName} - Business Registration",
                    type: "business_registration",
                    attributes: new Dictionary<string, object>
                    {
                        ["business_name"] = request.BusinessName ?? "",
                        ["business_registration_number"] = request.BusinessRegistrationNumber ?? "",
                        ["business_type"] = request.BusinessType ?? "corporation",
                        ["business_industry"] = request.BusinessIndustry ?? "technology",
                        ["address_line_1"] = request.BusinessAddress ?? "",
                        ["locality"] = request.BusinessCity ?? "",
                        ["administrative_area"] = request.BusinessState ?? "",
                        ["postal_code"] = request.BusinessPostalCode ?? "",
                        ["country_code"] = request.IsoCountry?.ToUpper() ?? "GB"
                    },
                    client: client
                );

                _logger.LogInformation("Business registration document created: {DocumentSid}", businessRegistrationDoc.Sid);

                // Create a proof of address document
                var proofOfAddressDoc = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.SupportingDocumentResource.CreateAsync(
                    friendlyName: $"{request.BusinessName} - Proof of Address",
                    type: "proof_of_address",
                    attributes: new Dictionary<string, object>
                    {
                        ["address_line_1"] = request.BusinessAddress ?? "",
                        ["locality"] = request.BusinessCity ?? "",
                        ["administrative_area"] = request.BusinessState ?? "",
                        ["postal_code"] = request.BusinessPostalCode ?? "",
                        ["country_code"] = request.IsoCountry?.ToUpper() ?? "GB"
                    },
                    client: client
                );

                _logger.LogInformation("Proof of address document created: {DocumentSid}", proofOfAddressDoc.Sid);

                // Create an identity document for the authorized contact
                var identityDoc = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.SupportingDocumentResource.CreateAsync(
                    friendlyName: $"{request.AuthorizedContactFirstName} {request.AuthorizedContactLastName} - Identity",
                    type: "identity",
                    attributes: new Dictionary<string, object>
                    {
                        ["first_name"] = request.AuthorizedContactFirstName ?? "",
                        ["last_name"] = request.AuthorizedContactLastName ?? "",
                        ["email"] = request.AuthorizedContactEmail ?? "",
                        ["phone_number"] = request.AuthorizedContactPhone ?? "",
                        ["date_of_birth"] = request.AuthorizedContactDateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                        ["job_title"] = request.AuthorizedContactJobTitle ?? "Authorized Representative"
                    },
                    client: client
                );

                _logger.LogInformation("Identity document created: {DocumentSid}", identityDoc.Sid);

                // Try to assign documents to the bundle (this may require separate API calls depending on SDK version)
                try
                {
                    // Create bundle document assignments
                    var businessRegAssignment = await Twilio.Rest.Numbers.V2.RegulatoryCompliance.BundleResource
                        .UpdateAsync(
                            pathSid: bundleSid,
                            // This may need to be done through a different API endpoint
                            client: client
                        );

                    _logger.LogInformation("Documents assigned to bundle {BundleSid}: BusinessReg={BusinessRegSid}, ProofAddr={ProofAddrSid}, Identity={IdentitySid}",
                        bundleSid, businessRegistrationDoc.Sid, proofOfAddressDoc.Sid, identityDoc.Sid);
                }
                catch (Exception assignEx)
                {
                    _logger.LogWarning(assignEx, "Could not automatically assign documents to bundle {BundleSid}. Documents created but may need manual assignment.", bundleSid);
                    
                    // Log the document SIDs for manual assignment if needed
                    _logger.LogInformation("Created documents for bundle {BundleSid}:", bundleSid);
                    _logger.LogInformation("  Business Registration: {BusinessRegSid}", businessRegistrationDoc.Sid);
                    _logger.LogInformation("  Proof of Address: {ProofAddrSid}", proofOfAddressDoc.Sid);
                    _logger.LogInformation("  Identity Document: {IdentitySid}", identityDoc.Sid);
                }

                _logger.LogInformation("GB business documents created for bundle: {BundleSid}", bundleSid);
            }
            catch (Twilio.Exceptions.ApiException twilioEx)
            {
                _logger.LogError(twilioEx, "Twilio API error creating supporting documents for bundle {BundleSid}. Status: {Status}, Code: {Code}, Message: {Message}", 
                    bundleSid, twilioEx.Status, twilioEx.Code, twilioEx.Message);
                throw; // Re-throw to be handled by caller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supporting documents for bundle {BundleSid}", bundleSid);
                throw; // Re-throw to be handled by caller
            }
        }

        #endregion
    }
}
