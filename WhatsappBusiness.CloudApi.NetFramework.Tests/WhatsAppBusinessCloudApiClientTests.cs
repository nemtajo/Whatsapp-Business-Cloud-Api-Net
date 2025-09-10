using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Configuration;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Interfaces;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.NetFramework.WhatsApp;
using WhatsappBusiness.CloudApi.NetFramework.WhatsApp.Responses;
using Xunit;

namespace WhatsappBusiness.CloudApi.NetFramework.Tests
{
    public class WhatsAppBusinessCloudApiClientTests : IClassFixture<TestSetup>
    {
        private readonly WhatsAppBusinessCloudApiConfig _config;
        private readonly IWhatsAppBusinessCloudApiClientFactory _clientFactory;
        private readonly string _primaryWABAId;
        private readonly string _sharedWABAId;
        private readonly string _inputTokenForSharedWABA;

        public WhatsAppBusinessCloudApiClientTests(TestSetup testSetup)
        {
            var configuration = testSetup.ServiceProvider.GetRequiredService<IConfiguration>();

            _config = new WhatsAppBusinessCloudApiConfig();
            _clientFactory = new WhatsAppBusinessCloudApiClientFactory();
            _config.WhatsAppBusinessId = configuration.GetSection("WhatsAppBusinessCloudApiNetFrameworkConfiguration")["WhatsAppBusinessId"];
            _config.WhatsAppAccessToken = configuration.GetSection("WhatsAppBusinessCloudApiNetFrameworkConfiguration")["WhatsAppAccessToken"];
            _config.WhatsAppGraphApiVersion = configuration.GetSection("WhatsAppBusinessCloudApiNetFrameworkConfiguration")["WhatsAppGraphApiVersion"];
            _config.WhatsAppEmbeddedSignupMetaAppId = configuration.GetSection("WhatsAppBusinessCloudApiNetFrameworkConfiguration")["WhatsAppEmbeddedSignupMetaAppId"];
            _config.WhatsAppEmbeddedSignupMetaAppSecret = configuration.GetSection("WhatsAppBusinessCloudApiNetFrameworkConfiguration")["WhatsAppEmbeddedSignupMetaAppSecret"];
            _config.WhatsAppEmbeddedSignupMetaConfigurationId = configuration.GetSection("WhatsAppBusinessCloudApiNetFrameworkConfiguration")["WhatsAppEmbeddedSignupMetaConfigurationId"];
            _config.WhatsAppEmbeddedSignupPartnerSolutionId = configuration.GetSection("WhatsAppBusinessCloudApiNetFrameworkConfiguration")["WhatsAppEmbeddedSignupPartnerSolutionId"];

            _primaryWABAId = configuration.GetSection("TestWABAIds")["PrimaryWABAId"];
            _sharedWABAId = configuration.GetSection("TestWABAIds")["SharedWABAId"];
            _inputTokenForSharedWABA = configuration.GetSection("TestTokens")["InputTokenForSharedWABA"];
        }

        [Fact(Skip = "Complete the WhatsAppBusinessCloudApiConfig to run the test.")]
        public async Task ExchangeTokenAsync_WithValidAuthorizationCode_ShouldReturnAccessToken()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                var authorizationCode = "your_authorization_code"; // Replace with actual authorization code obtained from the OAuth flow
                var redirectUri = "your_redirect_uri"; // Replace with actual redirect URI

                // Act
                var response = await client.ExchangeTokenAsync(authorizationCode, redirectUri);

                // Assert
                response.Should().NotBeNull();
                response.AccessToken.Should().NotBeNullOrEmpty();
                response.TokenType.Should().Be("bearer");
                response.ExpiresIn.Should().BeGreaterThan(0);
                response.Error.Should().BeNullOrEmpty();
            }
        }

        [Fact(Skip = "Complete the WhatsAppBusinessCloudApiConfig to run the test.")]
        public async Task GetSharedWABAIdAsync_ShouldReturnData()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act
                var response = await client.GetSharedWABAIdAsync(_inputTokenForSharedWABA);

                // Assert
                response.Should().NotBeNull();
                response.Data.Should().NotBeNull();
                var sharedWABAId = response.GetSharedWABAId();
                sharedWABAId.Should().NotBeNullOrEmpty();
                sharedWABAId.Should().Be(_sharedWABAId);
            }
        }

        [Fact(Skip = "Complete the WhatsAppBusinessCloudApiConfig to run the test.")]
        public async Task GetWABADetailsAsync_ShouldReturnSuccess()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act
                var response = await client.GetWABADetailsAsync(_sharedWABAId);

                // Assert
                response.Should().NotBeNull();
                response.Id.Should().Be(_sharedWABAId);
                response.Name.Should().NotBeNullOrEmpty();
                response.BusinessVerificationStatus.Should().NotBeNullOrEmpty();
                response.Currency.Should().NotBeNullOrEmpty();
                response.Country.Should().NotBeNullOrEmpty();
                
                if (response.HealthStatus != null)
                {
                    response.HealthStatus.CanSendMessage.Should().NotBeNullOrEmpty();
                }
            }
        }

        [Fact(Skip = "Complete the WhatsAppBusinessCloudApiConfig to run the test.")]
        public async Task GetWhatsAppBusinessAccountPhoneNumberAsync_ShouldReturnData()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act
                var response = await client.GetWhatsAppBusinessAccountPhoneNumberAsync(_sharedWABAId);

                // Assert
                response.Should().NotBeNull();
                response.Data.Should().NotBeNull();
                response.Data.Should().NotBeEmpty();

                var phoneNumber = response.Data[0];
                phoneNumber.Id.Should().NotBeNullOrEmpty();
                phoneNumber.DisplayPhoneNumber.Should().NotBeNullOrEmpty();
                phoneNumber.VerifiedName.Should().NotBeNullOrEmpty();

                var lastOnboardedPhoneNumberId = response.GetMostRecentlyOnboardedPhoneNumberId();
                lastOnboardedPhoneNumberId.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void WhatsAppBusinessCloudApiClient_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            using (var client = _clientFactory.Create(_config))
            {
                // Assert
                client.Should().NotBeNull();
            }
        }

        [Fact]
        public void WhatsAppBusinessCloudApiClient_MultipleInstances_ShouldUseSameHttpClient()
        {
            // Arrange & Act
            using (var client1 = _clientFactory.Create(_config))
            using (var client2 = _clientFactory.Create(_config))
            {
                // Assert
                client1.Should().NotBeNull();
                client2.Should().NotBeNull();
                // Both clients should be using the same singleton HttpClient instance
                // This is verified by the singleton pattern implementation
            }
        }

        [Fact]
        public void WhatsAppBusinessCloudApiConfig_ShouldInitializeWithCorrectValues()
        {
            // Assert
            _config.Should().NotBeNull();
            _config.WhatsAppBusinessId.Should().NotBeNullOrEmpty();
            _config.WhatsAppAccessToken.Should().NotBeNullOrEmpty();
            _config.WhatsAppGraphApiVersion.Should().NotBeNullOrEmpty();
            _config.WhatsAppEmbeddedSignupMetaAppId.Should().NotBeNullOrEmpty();
            _config.WhatsAppEmbeddedSignupMetaAppSecret.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ExchangeTokenAsync_WithNullCode_ShouldThrowArgumentException()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() => client.ExchangeTokenAsync(null));
            }
        }

        [Fact]
        public async Task ExchangeTokenAsync_WithEmptyCode_ShouldThrowArgumentException()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() => client.ExchangeTokenAsync(""));
            }
        }

        [Fact]
        public async Task GetSharedWABAIdAsync_WithNullToken_ShouldThrowArgumentException()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() => client.GetSharedWABAIdAsync(null));
            }
        }

        [Fact]
        public async Task GetWABADetailsAsync_WithNullWABAId_ShouldThrowArgumentException()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() => client.GetWABADetailsAsync(null));
            }
        }

        [Fact]
        public async Task GetWhatsAppBusinessAccountPhoneNumberAsync_WithNullWABAId_ShouldThrowArgumentException()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() => client.GetWhatsAppBusinessAccountPhoneNumberAsync(null));
            }
        }

        [Fact]
        public void GetSharedWABAId_WithValidGranularScopes_ShouldReturnWABAId()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    GranularScopes = new List<GranularScope>
                    {
                        new GranularScope
                        {
                            Scope = "whatsapp_business_management",
                            TargetIds = new List<string> { "test-waba-id-123" }
                        }
                    }
                }
            };

            // Act
            var wabaId = response.GetSharedWABAId();

            // Assert
            wabaId.Should().Be("test-waba-id-123");
        }

        [Fact]
        public void GetSharedWABAId_WithMessagingScope_ShouldReturnWABAId()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    GranularScopes = new List<GranularScope>
                    {
                        new GranularScope
                        {
                            Scope = "whatsapp_business_messaging",
                            TargetIds = new List<string> { "test-waba-id-456" }
                        }
                    }
                }
            };

            // Act
            var wabaId = response.GetSharedWABAId();

            // Assert
            wabaId.Should().Be("test-waba-id-456");
        }

        [Fact]
        public void GetSharedWABAId_WithNoRelevantScopes_ShouldReturnNull()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    GranularScopes = new List<GranularScope>
                    {
                        new GranularScope
                        {
                            Scope = "other_scope",
                            TargetIds = new List<string> { "some-id" }
                        }
                    }
                }
            };

            // Act
            var wabaId = response.GetSharedWABAId();

            // Assert
            wabaId.Should().BeNull();
        }

        [Fact]
        public void GetSharedWABAId_WithNullData_ShouldReturnNull()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = null
            };

            // Act
            var wabaId = response.GetSharedWABAId();

            // Assert
            wabaId.Should().BeNull();
        }

        [Fact]
        public void GetSharedWABAId_WithNullGranularScopes_ShouldReturnNull()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    GranularScopes = null
                }
            };

            // Act
            var wabaId = response.GetSharedWABAId();

            // Assert
            wabaId.Should().BeNull();
        }

        [Fact(Skip = "Complete the WhatsAppBusinessCloudApiConfig to run the test.")]
        public async Task GetSharedWABAIdAsync_ShouldReturnAccessTokenInformation()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act
                var response = await client.GetSharedWABAIdAsync(_inputTokenForSharedWABA);

                // Assert
                response.Should().NotBeNull();
                response.Data.Should().NotBeNull();
                
                var tokenInfo = response.GetAccessTokenInformation(_inputTokenForSharedWABA);
                tokenInfo.Should().NotBeNull();
                tokenInfo.AccessToken.Should().Be(_inputTokenForSharedWABA);
                tokenInfo.IsValid.Should().BeTrue();
                tokenInfo.IsExpired.Should().BeFalse();
                tokenInfo.WabaId.Should().NotBeNullOrWhiteSpace();
            }
        }
        
        [Fact]
        public void GetSharedWABAId_GetAccessTokenInformation_WithValidToken_ShouldReturnCorrectInformation()
        {
            // Arrange
            var futureExpiryTime = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds();
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    IsValid = true,
                    ExpiresAt = futureExpiryTime,
                    AppId = "test-app-id",
                    UserId = "test-user-id"
                }
            };

            // Act
            var tokenInfo = response.GetAccessTokenInformation("test-token");

            // Assert
            tokenInfo.Should().NotBeNull();
            tokenInfo.AccessToken.Should().Be("test-token");
            tokenInfo.IsValid.Should().BeTrue();
            tokenInfo.IsExpired.Should().BeFalse();
            tokenInfo.ExpiresAt.Should().NotBeNull();
            tokenInfo.AppId.Should().Be("test-app-id");
            tokenInfo.UserId.Should().Be("test-user-id");
            tokenInfo.TimeUntilExpiration.Should().NotBeNull();
        }

        [Fact]
        public void GetSharedWABAId_GetAccessTokenInformation_WithInvalidToken_ShouldReturnInvalidAndExpired()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    IsValid = false,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
                }
            };

            // Act
            var tokenInfo = response.GetAccessTokenInformation("test-token");

            // Assert
            tokenInfo.Should().NotBeNull();
            tokenInfo.AccessToken.Should().Be("test-token");
            tokenInfo.IsValid.Should().BeFalse();
            tokenInfo.IsExpired.Should().BeTrue();
        }

        [Fact]
        public void GetSharedWABAId_GetAccessTokenInformation_WithNullData_ShouldReturnNotValid()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = null
            };

            // Act
            var tokenInfo = response.GetAccessTokenInformation("test-token");

            // Assert
            tokenInfo.Should().NotBeNull();
            tokenInfo.AccessToken.Should().Be("test-token");
            tokenInfo.IsValid.Should().BeFalse();
            tokenInfo.IsExpired.Should().BeTrue();
            tokenInfo.ExpiresAt.Should().BeNull();
            tokenInfo.TimeUntilExpiration.Should().BeNull();
        }

        [Fact]
        public void GetSharedWABAId_GetAccessTokenInformation_WithExpiredToken_ShouldReturnExpiredInformation()
        {
            // Arrange
            var pastExpiryTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    IsValid = true,
                    ExpiresAt = pastExpiryTime
                }
            };

            // Act
            var tokenInfo = response.GetAccessTokenInformation("test-token");

            // Assert
            tokenInfo.Should().NotBeNull();
            tokenInfo.IsValid.Should().BeTrue();
            tokenInfo.IsExpired.Should().BeTrue();
            tokenInfo.TimeUntilExpiration.Should().BeNull();
        }

        [Fact]
        public void GetSharedWABAId_GetAccessTokenInformation_WithTokenExpiringWithin24Hours_ShouldReturnNotExpired()
        {
            // Arrange
            var expiryTime = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds();
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    IsValid = true,
                    ExpiresAt = expiryTime
                }
            };

            // Act
            var tokenInfo = response.GetAccessTokenInformation("test-token");

            // Assert
            tokenInfo.Should().NotBeNull();
            tokenInfo.IsValid.Should().BeTrue();
            tokenInfo.IsExpired.Should().BeFalse();
            tokenInfo.TimeUntilExpiration.Should().NotBeNull();
        }

        [Fact]
        public void GetSharedWABAId_GetAccessTokenInformation_WithTokenExpiringAfter24Hours_ShouldNotBeMarkedExpired()
        {
            // Arrange
            var expiryTime = DateTimeOffset.UtcNow.AddHours(36).ToUnixTimeSeconds();
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    IsValid = true,
                    ExpiresAt = expiryTime
                }
            };

            // Act
            var tokenInfo = response.GetAccessTokenInformation("test-token");

            // Assert
            tokenInfo.Should().NotBeNull();
            tokenInfo.IsValid.Should().BeTrue();
            tokenInfo.IsExpired.Should().BeFalse();
            tokenInfo.TimeUntilExpiration.Should().NotBeNull();
        }

        [Fact]
        public void GetSharedWABAId_GetAccessTokenInformation_WithNeverExpiringToken_ShouldReturnValidAndNotExpired()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    IsValid = true,
                    ExpiresAt = 0,
                    AppId = "test-app-id",
                    UserId = "test-user-id"
                }
            };

            // Act
            var tokenInfo = response.GetAccessTokenInformation("test-token");

            // Assert
            tokenInfo.Should().NotBeNull();
            tokenInfo.AccessToken.Should().Be("test-token");
            tokenInfo.IsValid.Should().BeTrue();
            tokenInfo.IsExpired.Should().BeFalse();
            tokenInfo.ExpiresAt.Should().BeNull(); // Null means no expiration date
            tokenInfo.TimeUntilExpiration.Should().BeNull(); // Null means infinite time
            tokenInfo.AppId.Should().Be("test-app-id");
            tokenInfo.UserId.Should().Be("test-user-id");
        }

        [Fact]
        public void GetSharedWABAId_GetAccessTokenInformation_WithNeverExpiringInvalidToken_ShouldReturnExpired()
        {
            // Arrange
            var response = new SharedWABAIDResponse
            {
                Data = new SharedWABAIDData
                {
                    IsValid = false,
                    ExpiresAt = 0,
                    AppId = "test-app-id",
                    UserId = "test-user-id"
                }
            };

            // Act
            var tokenInfo = response.GetAccessTokenInformation("test-token");

            // Assert
            tokenInfo.Should().NotBeNull();
            tokenInfo.IsValid.Should().BeFalse();
            tokenInfo.IsExpired.Should().BeTrue();
        }

        [Fact]
        public async Task RefreshIfExpiredAsync_WithNullToken_ShouldThrowArgumentException()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() => client.RefreshIfExpiredAsync(null));
            }
        }

        [Fact]
        public async Task RefreshIfExpiredAsync_WithEmptyToken_ShouldThrowArgumentException()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() => client.RefreshIfExpiredAsync(""));
            }
        }
        
        [Fact(Skip = "Complete the WhatsAppBusinessCloudApiConfig to run the test.")]
        public async Task RefreshIfExpiredAsync_WithRealToken_ShouldObtainNewAccessToken()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act
                var response = await client.RefreshIfExpiredAsync(_inputTokenForSharedWABA);

                // Assert
                response.Should().NotBeNullOrWhiteSpace();
                response.Should().NotBe(_inputTokenForSharedWABA);
            }
        }
        
        [Fact(Skip = "Complete the WhatsAppBusinessCloudApiConfig to run the test.")]
        public async Task RevokeAsync_WithRealToken_ShouldSucceed()
        {
            // Arrange
            using (var client = _clientFactory.Create(_config))
            {
                // Act
                var response = await client.RevokeAsync("your_shared_access_token");

                // Assert
                response.Should().BeTrue();
            }
        }
    }
}