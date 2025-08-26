using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Twilio.Rest.Api.V2010;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Api.V2010.Account.AvailablePhoneNumberCountry;
using Twilio.Rest.Messaging.V2;
using Twilio.Clients;
using Twilio.Types;
using TwilioExceptions = Twilio.Exceptions;
using WhatsappBusiness.CloudApi;
using WhatsappBusiness.CloudApi.AccountMetrics;
using WhatsappBusiness.CloudApi.Configurations;
using WhatsappBusiness.CloudApi.Exceptions;
using WhatsappBusiness.CloudApi.Interfaces;
using WhatsappBusiness.CloudApi.Media.Requests;
using WhatsappBusiness.CloudApi.Messages.Requests;
using WhatsappBusiness.CloudApi.Response;
using WhatsAppBusinessCloudAPI.Web.Extensions.Alerts;
using WhatsAppBusinessCloudAPI.Web.Models;
using WhatsAppBusinessCloudAPI.Web.ViewModel;
using WhatsAppBusinessCloudAPI.Web.TwilioIntegration;

namespace WhatsAppBusinessCloudAPI.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWhatsAppBusinessClient _whatsAppBusinessClient;
        private readonly WhatsAppBusinessCloudApiConfig _whatsAppConfig;
        private readonly EmbeddedSignupConfiguration _embeddedSignupConfig;
        private readonly IWebHostEnvironment _environment;
        private readonly ITwilioRestClient _twilioClient;
        private readonly ITwilioIntegrationService _twilioIntegrationService;

        public HomeController(ILogger<HomeController> logger, IWhatsAppBusinessClient whatsAppBusinessClient,
            IOptions<WhatsAppBusinessCloudApiConfig> whatsAppConfig, IOptions<EmbeddedSignupConfiguration> embeddedSignupConfig, 
            IWebHostEnvironment environment, ITwilioRestClient twilioClient, ITwilioIntegrationService twilioIntegrationService)
        {
            _logger = logger;
            _whatsAppBusinessClient = whatsAppBusinessClient;
            _whatsAppConfig = whatsAppConfig.Value;
            _embeddedSignupConfig = embeddedSignupConfig.Value;
            _environment = environment;
            _twilioClient = twilioClient;
            _twilioIntegrationService = twilioIntegrationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Messages()
        {
            return View();
        }

        public IActionResult SendWhatsAppTextMessage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppTextMessage(SendTextMessageViewModel sendTextMessageViewModel)
        {
            try
            {
                TextMessageRequest textMessageRequest = new TextMessageRequest();
                textMessageRequest.To = sendTextMessageViewModel.RecipientPhoneNumber;
                textMessageRequest.Text = new WhatsAppText();
                textMessageRequest.Text.Body = sendTextMessageViewModel.Message;
                textMessageRequest.Text.PreviewUrl = false;

                var results = await _whatsAppBusinessClient.SendTextMessageAsync(textMessageRequest);

                return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent text message");
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return View().WithDanger("Error", ex.Message);
            }
        }

        public IActionResult SendWhatsAppMediaMessage()
        {
            SendMediaMessageViewModel sendMediaMessageViewModel = new SendMediaMessageViewModel();
            sendMediaMessageViewModel.MediaType = new List<SelectListItem>()
            {
                new SelectListItem(){ Text = "Audio", Value = "Audio" },
                new SelectListItem(){ Text = "Document", Value = "Document" },
                new SelectListItem(){ Text = "Image", Value = "Image" },
                new SelectListItem(){ Text = "Sticker", Value = "Sticker" },
                new SelectListItem(){ Text = "Video", Value = "Video" },
            };

            return View(sendMediaMessageViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppMediaMessage(SendMediaMessageViewModel sendMediaMessage)
        {
            try
            {
                WhatsAppResponse results = null;
                switch (sendMediaMessage.SelectedMediaType)
                {
                    case "Audio":
                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaId))
                        {
                            AudioMessageByIdRequest audioMessage = new AudioMessageByIdRequest();
                            audioMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            audioMessage.Audio = new MediaAudio();
                            audioMessage.Audio.Id = sendMediaMessage.MediaId;

                            results = await _whatsAppBusinessClient.SendAudioAttachmentMessageByIdAsync(audioMessage);
                        }

                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaLink))
                        {
                            AudioMessageByUrlRequest audioMessage = new AudioMessageByUrlRequest();
                            audioMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            audioMessage.Audio = new MediaAudioUrl();
                            audioMessage.Audio.Link = sendMediaMessage.MediaLink;

                            results = await _whatsAppBusinessClient.SendAudioAttachmentMessageByUrlAsync(audioMessage);
                        }
                        break;

                    case "Document":
                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaId))
                        {
                            DocumentMessageByIdRequest documentMessage = new DocumentMessageByIdRequest();
                            documentMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            documentMessage.Document = new MediaDocument();
                            documentMessage.Document.Id = sendMediaMessage.MediaId;
                            documentMessage.Document.Caption = sendMediaMessage.Message;

                            results = await _whatsAppBusinessClient.SendDocumentAttachmentMessageByIdAsync(documentMessage);
                        }

                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaLink))
                        {
                            DocumentMessageByUrlRequest documentMessage = new DocumentMessageByUrlRequest();
                            documentMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            documentMessage.Document = new MediaDocumentUrl();
                            documentMessage.Document.Link = sendMediaMessage.MediaLink;
                            documentMessage.Document.Caption = sendMediaMessage.Message;

                            results = await _whatsAppBusinessClient.SendDocumentAttachmentMessageByUrlAsync(documentMessage);
                        }
                        break;

                    case "Image":
                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaId))
                        {
                            ImageMessageByIdRequest imageMessage = new ImageMessageByIdRequest();
                            imageMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            imageMessage.Image = new MediaImage();
                            imageMessage.Image.Id = sendMediaMessage.MediaId;
                            imageMessage.Image.Caption = sendMediaMessage.Message;

                            results = await _whatsAppBusinessClient.SendImageAttachmentMessageByIdAsync(imageMessage);
                        }

                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaLink))
                        {
                            ImageMessageByUrlRequest imageMessage = new ImageMessageByUrlRequest();
                            imageMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            imageMessage.Image = new MediaImageUrl();
                            imageMessage.Image.Link = sendMediaMessage.MediaLink;
                            imageMessage.Image.Caption = sendMediaMessage.Message;

                            results = await _whatsAppBusinessClient.SendImageAttachmentMessageByUrlAsync(imageMessage);
                        }
                        break;

                    case "Sticker":
                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaId))
                        {
                            StickerMessageByIdRequest stickerMessage = new StickerMessageByIdRequest();
                            stickerMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            stickerMessage.Sticker = new MediaSticker();
                            stickerMessage.Sticker.Id = sendMediaMessage.MediaId;

                            results = await _whatsAppBusinessClient.SendStickerMessageByIdAsync(stickerMessage);
                        }

                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaLink))
                        {
                            StickerMessageByUrlRequest stickerMessage = new StickerMessageByUrlRequest();
                            stickerMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            stickerMessage.Sticker = new MediaStickerUrl();
                            stickerMessage.Sticker.Link = sendMediaMessage.MediaLink;

                            results = await _whatsAppBusinessClient.SendStickerMessageByUrlAsync(stickerMessage);
                        }
                        break;

                    case "Video":
                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaId))
                        {
                            VideoMessageByIdRequest videoMessage = new VideoMessageByIdRequest();
                            videoMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            videoMessage.Video = new MediaVideo();
                            videoMessage.Video.Id = sendMediaMessage.MediaId;
                            videoMessage.Video.Caption = sendMediaMessage.Message;

                            results = await _whatsAppBusinessClient.SendVideoAttachmentMessageByIdAsync(videoMessage);
                        }

                        if (!string.IsNullOrWhiteSpace(sendMediaMessage.MediaLink))
                        {
                            VideoMessageByUrlRequest videoMessage = new VideoMessageByUrlRequest();
                            videoMessage.To = sendMediaMessage.RecipientPhoneNumber;
                            videoMessage.Video = new MediaVideoUrl();
                            videoMessage.Video.Link = sendMediaMessage.MediaLink;
                            videoMessage.Video.Caption = sendMediaMessage.Message;

                            results = await _whatsAppBusinessClient.SendVideoAttachmentMessageByUrlAsync(videoMessage);
                        }
                        break;
                }

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent media message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppMediaMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppMediaMessage)).WithDanger("Error", ex.Message);
            }
        }

        public IActionResult SendWhatsAppLocationMessage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppLocationMessage(SendLocationMessageViewModel sendLocationMessageViewModel)
        {
            try
            {
                LocationMessageRequest locationMessageRequest = new LocationMessageRequest();
                locationMessageRequest.To = sendLocationMessageViewModel.RecipientPhoneNumber;
                locationMessageRequest.Location = new Location();
                locationMessageRequest.Location.Name = "Location Test";
                locationMessageRequest.Location.Address = "Address Test";
                locationMessageRequest.Location.Longitude = sendLocationMessageViewModel.Longitude;
                locationMessageRequest.Location.Latitude = sendLocationMessageViewModel.Latitude;

                var results = await _whatsAppBusinessClient.SendLocationMessageAsync(locationMessageRequest);

                return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent location message");
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return View().WithDanger("Error", ex.Message);
            }
        }

        public IActionResult SendWhatsAppInteractiveMessage()
        {
            SendInteractiveMessageViewModel sendInteractiveMessageViewModel = new SendInteractiveMessageViewModel();
            sendInteractiveMessageViewModel.InteractiveType = new List<SelectListItem>()
            {
                new SelectListItem(){ Text = "List Message", Value = "List Message" },
                new SelectListItem(){ Text = "Reply Button", Value = "Reply Button" },
                new SelectListItem(){ Text = "Location Request Message", Value = "Location Request Message" }
            };
            return View(sendInteractiveMessageViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppInteractiveMessage(SendInteractiveMessageViewModel sendInteractiveMessageViewModel)
        {
            try
            {
                WhatsAppResponse results = null;

                if (sendInteractiveMessageViewModel.SelectedInteractiveType.Equals("List Message"))
                {
                    InteractiveListMessageRequest interactiveListMessage = new InteractiveListMessageRequest();
                    interactiveListMessage.To = sendInteractiveMessageViewModel.RecipientPhoneNumber;
                    interactiveListMessage.Interactive = new InteractiveListMessage();

                    interactiveListMessage.Interactive.Header = new Header();
                    interactiveListMessage.Interactive.Header.Type = "text";
                    interactiveListMessage.Interactive.Header.Text = "List Header Sample Test";

                    interactiveListMessage.Interactive.Body = new ListBody();
                    interactiveListMessage.Interactive.Body.Text = sendInteractiveMessageViewModel.Message;

                    interactiveListMessage.Interactive.Footer = new Footer();
                    interactiveListMessage.Interactive.Footer.Text = "List Footer Sample Test";

                    interactiveListMessage.Interactive.Action = new ListAction();
                    interactiveListMessage.Interactive.Action.Button = "Send";
                    interactiveListMessage.Interactive.Action.Sections = new List<Section>()
                    {
                        new Section()
                        {
                            Title = "Category A",
                            Rows = new List<Row>()
                            {
                                new Row()
                                {
                                    Id = "Item_A1",
                                    Title = "Apples",
                                    Description = "Enjoy fruits for free"
                                },
                                new Row()
                                {
                                    Id = "Item_A2",
                                    Title = "Tangerines",
                                    Description = "Enjoy fruits for free"
                                },
                            },
                        },
                        new Section()
                        {
                            Title = "Category B",
                            Rows = new List<Row>()
                            {
                                new Row()
                                {
                                    Id = "Item_B1",
                                    Title = "2JZ",
                                    Description = "Engine discounts"
                                },
                                new Row()
                                {
                                    Id = "Item_2",
                                    Title = "1JZ",
                                    Description = "Engine discounts"
                                },
                            }
                        }
                    };

                    results = await _whatsAppBusinessClient.SendInteractiveListMessageAsync(interactiveListMessage);
                }

                if (sendInteractiveMessageViewModel.SelectedInteractiveType.Equals("Reply Button"))
                {
                    InteractiveReplyButtonMessageRequest interactiveReplyButtonMessage = new InteractiveReplyButtonMessageRequest();
                    interactiveReplyButtonMessage.To = sendInteractiveMessageViewModel.RecipientPhoneNumber;
                    interactiveReplyButtonMessage.Interactive = new InteractiveReplyButtonMessage();

                    interactiveReplyButtonMessage.Interactive.Header = new ReplyButtonHeader();
                    interactiveReplyButtonMessage.Interactive.Header.Type = "text";
                    interactiveReplyButtonMessage.Interactive.Header.Text = "Reply Button Header Sample Test";

                    interactiveReplyButtonMessage.Interactive.Body = new ReplyButtonBody();
                    interactiveReplyButtonMessage.Interactive.Body.Text = sendInteractiveMessageViewModel.Message;

                    interactiveReplyButtonMessage.Interactive.Footer = new ReplyButtonFooter();
                    interactiveReplyButtonMessage.Interactive.Footer.Text = "Reply Button Footer Sample Test";

                    interactiveReplyButtonMessage.Interactive.Action = new ReplyButtonAction();
                    interactiveReplyButtonMessage.Interactive.Action.Buttons = new List<ReplyButton>()
                    {
                        new ReplyButton()
                        {
                            Type = "reply",
                            Reply = new Reply()
                            {
                                Id = "SAMPLE_1_CLICK",
                                Title = "CLICK ME!!!"
                            }
                        },

                        new ReplyButton()
                        {
                            Type = "reply",
                            Reply = new Reply()
                            {
                                Id = "SAMPLE_2_CLICK",
                                Title = "LATER"
                            }
                        }
                    };

                    results = await _whatsAppBusinessClient.SendInteractiveReplyButtonMessageAsync(interactiveReplyButtonMessage);
                }

                if (sendInteractiveMessageViewModel.SelectedInteractiveType.Equals("Location Request Message"))
                {
                    InteractiveLocationMessageRequest interactiveLocationMessageRequest = new InteractiveLocationMessageRequest();
                    interactiveLocationMessageRequest.To = sendInteractiveMessageViewModel.RecipientPhoneNumber;
                    interactiveLocationMessageRequest.Interactive = new InteractiveLocationRequestMessage();
                    interactiveLocationMessageRequest.Interactive.Body = new InteractiveLocationBody();
                    interactiveLocationMessageRequest.Interactive.Body.Text = (!string.IsNullOrWhiteSpace(sendInteractiveMessageViewModel.Message)) ? sendInteractiveMessageViewModel.Message : "Let us start with your pickup. You can either manually *enter an address* or *share your current location*.";
                    interactiveLocationMessageRequest.Interactive.Action = new InteractiveLocationAction();

                    results = await _whatsAppBusinessClient.SendLocationRequestMessageAsync(interactiveLocationMessageRequest);
                }

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent interactive message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppInteractiveMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppInteractiveMessage)).WithDanger("Error", ex.Message);
            }
        }

        public IActionResult SendWhatsAppFlowMessage()
        {
            SendFlowMessageViewModel sendFlowMessageViewModel = new SendFlowMessageViewModel();
            sendFlowMessageViewModel.FlowAction = new List<SelectListItem>()
            {
                new SelectListItem(){ Text = "Navigate", Value = "navigate" },
                new SelectListItem(){ Text = "Data Exchange", Value = "data_exchange" }
            };
            sendFlowMessageViewModel.Mode = new List<SelectListItem>()
            {
                new SelectListItem(){ Text = "Draft", Value = "Draft" },
                new SelectListItem(){ Text = "Published", Value = "Published" }
            };
            return View(sendFlowMessageViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppFlowMessage(SendFlowMessageViewModel sendFlowMessageViewModel)
        {
            try
            {
                FlowMessageRequest flowMessageRequest = new FlowMessageRequest();
                flowMessageRequest.To = sendFlowMessageViewModel.RecipientPhoneNumber;
                flowMessageRequest.Interactive = new FlowMessageInteractive();

                flowMessageRequest.Interactive.Header = new FlowMessageHeader();
                flowMessageRequest.Interactive.Header.Type = "text";
                flowMessageRequest.Interactive.Header.Text = "Header flow";

                flowMessageRequest.Interactive.Body = new FlowMessageBody();
                flowMessageRequest.Interactive.Body.Text = "Body flow";

                flowMessageRequest.Interactive.Footer = new FlowMessageFooter();
                flowMessageRequest.Interactive.Footer.Text = "Footer flow";

                flowMessageRequest.Interactive.Action = new FlowMessageAction();
                flowMessageRequest.Interactive.Action.Parameters = new FlowMessageParameters();
                flowMessageRequest.Interactive.Action.Parameters.FlowToken = sendFlowMessageViewModel.FlowToken;
                flowMessageRequest.Interactive.Action.Parameters.FlowId = sendFlowMessageViewModel.FlowId;
                flowMessageRequest.Interactive.Action.Parameters.FlowCta = sendFlowMessageViewModel.FlowButtonText;
                flowMessageRequest.Interactive.Action.Parameters.FlowAction = sendFlowMessageViewModel.SelectedFlowAction;
                flowMessageRequest.Interactive.Action.Parameters.IsInDraftMode = (sendFlowMessageViewModel.SelectedMode.Equals("Draft", StringComparison.OrdinalIgnoreCase));

                flowMessageRequest.Interactive.Action.Parameters.FlowActionPayload = new FlowActionPayload();
                flowMessageRequest.Interactive.Action.Parameters.FlowActionPayload.Screen = sendFlowMessageViewModel.ScreenId;

                var results = await _whatsAppBusinessClient.SendFlowMessageAsync(flowMessageRequest);

                return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent flow message");
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppFlowMessage)).WithDanger("Error", ex.Message);
            }
        }

        public IActionResult SendWhatsAppTemplateMessage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppTextTemplateMessage(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                TextTemplateMessageRequest textTemplateMessage = new TextTemplateMessageRequest();
                textTemplateMessage.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                textTemplateMessage.Template = new TextMessageTemplate();
                textTemplateMessage.Template.Name = sendTemplateMessageViewModel.TemplateName;
                textTemplateMessage.Template.Language = new TextMessageLanguage();
                textTemplateMessage.Template.Language.Code = "en_US";

                var results = await _whatsAppBusinessClient.SendTextMessageTemplateAsync(textTemplateMessage);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent template text message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppTextTemplateMessageWithParameters(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                // For Text Template message with parameters supported component type is body only
                TextTemplateMessageRequest textTemplateMessage = new TextTemplateMessageRequest();
                textTemplateMessage.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                textTemplateMessage.Template = new TextMessageTemplate();
                textTemplateMessage.Template.Name = sendTemplateMessageViewModel.TemplateName;
                textTemplateMessage.Template.Language = new TextMessageLanguage();
                textTemplateMessage.Template.Language.Code = LanguageCode.English_US;
                textTemplateMessage.Template.Components = new List<TextMessageComponent>();
                textTemplateMessage.Template.Components.Add(new TextMessageComponent()
                {
                    Type = "body",
                    Parameters = new List<TextMessageParameter>()
                    {
                        new TextMessageParameter()
                        {
                            Type = "text",
                            Text = "Testing Parameter Placeholder Position 1"
                        },
                        new TextMessageParameter()
                        {
                            Type = "text",
                            Text = "Testing Parameter Placeholder Position 2"
                        }
                    }
                });

                var results = await _whatsAppBusinessClient.SendTextMessageTemplateAsync(textTemplateMessage);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent template text message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppInteractiveTemplateMessageWithParameters(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                // Tested with facebook predefined template name: sample_issue_resolution
                InteractiveTemplateMessageRequest interactiveTemplateMessage = new InteractiveTemplateMessageRequest();
                interactiveTemplateMessage.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                interactiveTemplateMessage.Template = new InteractiveMessageTemplate();
                interactiveTemplateMessage.Template.Name = sendTemplateMessageViewModel.TemplateName;
                interactiveTemplateMessage.Template.Language = new InteractiveMessageLanguage();
                interactiveTemplateMessage.Template.Language.Code = LanguageCode.English_US;
                interactiveTemplateMessage.Template.Components = new List<InteractiveMessageComponent>();
                interactiveTemplateMessage.Template.Components.Add(new InteractiveMessageComponent()
                {
                    Type = "body",
                    Parameters = new List<InteractiveMessageParameter>()
                    {
                        new InteractiveMessageParameter()
                        {
                            Type = "text",
                            Text = "Interactive Parameter Placeholder Position 1"
                        }
                    }
                });

                var results = await _whatsAppBusinessClient.SendInteractiveTemplateMessageAsync(interactiveTemplateMessage);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent interactive template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppMediaTemplateMessageWithParameters(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                // Tested with facebook predefined template name: sample_movie_ticket_confirmation
                ImageTemplateMessageRequest imageTemplateMessage = new ImageTemplateMessageRequest();
                imageTemplateMessage.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                imageTemplateMessage.Template = new ImageMessageTemplate();
                imageTemplateMessage.Template.Name = sendTemplateMessageViewModel.TemplateName;
                imageTemplateMessage.Template.Language = new ImageMessageLanguage();
                imageTemplateMessage.Template.Language.Code = LanguageCode.English_US;
                imageTemplateMessage.Template.Components = new List<ImageMessageComponent>()
                {
                    new ImageMessageComponent()
                    {
                        Type = "header",
                        Parameters = new List<ImageMessageParameter>()
                        {
                            new ImageMessageParameter()
                            {
                                Type = "image",
                                Image = new Image()
                                {
                                    //Id = sendTemplateMessageViewModel.MediaId,
                                    Link = "https://otakukart.com/wp-content/uploads/2022/03/Upcoming-Marvel-Movies-In-2022-23.jpg"
                                }
                            }
                        },
                    },
                    new ImageMessageComponent()
                    {
                        Type = "body",
                        Parameters = new List<ImageMessageParameter>()
                        {
                            new ImageMessageParameter()
                            {
                                Type = "text",
                                Text = "Movie Testing"
                            },

                            new ImageMessageParameter()
                            {
                                Type = "date_time",
                                DateTime = new ImageTemplateDateTime()
                                {
                                    FallbackValue = DateTime.Now.ToString("dddd d, yyyy"),
                                    DayOfWeek = (int)DateTime.Now.DayOfWeek,
                                    Year = DateTime.Now.Year,
                                    Month = DateTime.Now.Month,
                                    DayOfMonth = DateTime.Now.Day,
                                    Hour = DateTime.Now.Hour,
                                    Minute = DateTime.Now.Minute,
                                    Calendar = "GREGORIAN"
                                }
                            },

                            new ImageMessageParameter()
                            {
                                Type = "text",
                                Text = "Venue Test"
                            },

                            new ImageMessageParameter()
                            {
                                Type = "text",
                                Text = "Seat 1A, 2A, 3A and 4A"
                            }
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendImageAttachmentTemplateMessageAsync(imageTemplateMessage);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent image template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppDocumentTemplateMessageWithParameters(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                DocumentTemplateMessageRequest documentTemplateMessage = new DocumentTemplateMessageRequest();
                documentTemplateMessage.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                documentTemplateMessage.Template = new DocumentMessageTemplate();
                documentTemplateMessage.Template.Name = sendTemplateMessageViewModel.TemplateName;
                documentTemplateMessage.Template.Language = new DocumentMessageLanguage();
                documentTemplateMessage.Template.Language.Code = LanguageCode.English_US;
                documentTemplateMessage.Template.Components = new List<DocumentMessageComponent>()
                {
                    new DocumentMessageComponent()
                    {
                        Type = "header",
                        Parameters = new List<DocumentMessageParameter>()
                        {
                            new DocumentMessageParameter()
                            {
                                Type = "document",
                                Document = new Document()
                                {
                                    //Id = sendTemplateMessageViewModel.MediaId,
                                    Link = "<EXTERNAL_LINK_DOCUMENT>" // Link point where your document can be downloaded or retrieved by WhatsApp
                                }
                            }
                        },
                    },
                    new DocumentMessageComponent()
                    {
                        Type = "body",
                        Parameters = new List<DocumentMessageParameter>()
                        {
                            new DocumentMessageParameter()
                            {
                                Type = "text",
                                Text = "Order Invoice"
                            },
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendDocumentAttachmentTemplateMessageAsync(documentTemplateMessage);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent document template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppVideoTemplateMessageWithParameters(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                VideoTemplateMessageRequest videoTemplateMessage = new();
                videoTemplateMessage.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                videoTemplateMessage.Template = new();
                videoTemplateMessage.Template.Name = sendTemplateMessageViewModel.TemplateName;
                videoTemplateMessage.Template.Language = new();
                videoTemplateMessage.Template.Language.Code = LanguageCode.English_US;
                videoTemplateMessage.Template.Components = new List<VideoMessageComponent>()
                {
                    new VideoMessageComponent()
                    {
                        Type = "header",
                        Parameters = new List<VideoMessageParameter>()
                        {
                            new VideoMessageParameter()
                            {
                                Type = "video",
                                Video = new Video()
                                {
                                    //Id = sendTemplateMessageViewModel.MediaId,
                                    Link = sendTemplateMessageViewModel.LinkUrl // Link point where your document can be downloaded or retrieved by WhatsApp
                                }
                            }
                        },
                    },
                    new VideoMessageComponent()
                    {
                        Type = "body",
                        Parameters = new List<VideoMessageParameter>()
                        {
                            new VideoMessageParameter()
                            {
                                Type = "text",
                                Text = "Video Information"
                            },
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendVideoAttachmentTemplateMessageAsync(videoTemplateMessage);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent video template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppAuthenticationTemplateMessage(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                AuthenticationTemplateMessageRequest authenticationTemplateMessageRequest = new();
                authenticationTemplateMessageRequest.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                authenticationTemplateMessageRequest.Template = new();
                authenticationTemplateMessageRequest.Template.Name = sendTemplateMessageViewModel.TemplateName;
                authenticationTemplateMessageRequest.Template.Language = new();
                authenticationTemplateMessageRequest.Template.Language.Code = LanguageCode.English_US;
                authenticationTemplateMessageRequest.Template.Components = new List<AuthenticationMessageComponent>()
                {
                    new AuthenticationMessageComponent()
                    {
                        Type = "body",
                        Parameters = new List<AuthenticationMessageParameter>()
                        {
                            new AuthenticationMessageParameter()
                            {
                                Type = "text",
                                Text = "J$FpnYnP" // One time password value
							}
                        }
                    },
                    new AuthenticationMessageComponent()
                    {
                        Type = "button",
                        SubType = "url",
                        Index = 0,
                        Parameters = new List<AuthenticationMessageParameter>()
                        {
                            new AuthenticationMessageParameter()
                            {
                                Type = "text",
                                Text = "J$FpnYnP" // One time password value
							}
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendAuthenticationMessageTemplateAsync(authenticationTemplateMessageRequest);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent authentication template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppCatalogueTemplateMessage(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                CatalogTemplateMessageRequest catalogTemplateMessageRequest = new CatalogTemplateMessageRequest();
                catalogTemplateMessageRequest.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                catalogTemplateMessageRequest.Template = new();
                catalogTemplateMessageRequest.Template.Name = sendTemplateMessageViewModel.TemplateName;
                catalogTemplateMessageRequest.Template.Language = new();
                catalogTemplateMessageRequest.Template.Language.Code = LanguageCode.English_US;
                catalogTemplateMessageRequest.Template.Components = new List<CatalogMessageComponent>()
                {
                    new CatalogMessageComponent()
                    {
                        Type = "Body",
                        Parameters = new List<CatalogTemplateMessageParameter>()
                        {
                            new CatalogTemplateMessageParameter()
                            {
                                Type = "text",
                                Text = "100"
                            },
                            new CatalogTemplateMessageParameter()
                            {
                                Type = "text",
                                Text = "400"
                            },
                            new CatalogTemplateMessageParameter()
                            {
                                Type = "text",
                                Text = "3"
                            },
                        }
                    },
                    new CatalogMessageComponent()
                    {
                        Type = "button",
                        SubType = "CATALOG",
                        Index = 0,
                        Parameters = new List<CatalogTemplateMessageParameter>()
                        {
                            new CatalogTemplateMessageParameter()
                            {
                                Type = "action",
                                Action = new CatalogTemplateMessageAction()
                                {
                                    ThumbnailProductRetailerId = "2lc20305pt"
                                }
                            }
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendCatalogMessageTemplateAsync(catalogTemplateMessageRequest);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent catalogue template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppCarouselTemplateMessage(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                CarouselTemplateMessageRequest carouselTemplateMessageRequest = new CarouselTemplateMessageRequest();
                carouselTemplateMessageRequest.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                carouselTemplateMessageRequest.Template = new();
                carouselTemplateMessageRequest.Template.Name = sendTemplateMessageViewModel.TemplateName;
                carouselTemplateMessageRequest.Template.Language = new();
                carouselTemplateMessageRequest.Template.Language.Code = LanguageCode.English_US;
                carouselTemplateMessageRequest.Template.Components = new List<CarouselMessageTemplateComponent>()
                {
                    new CarouselMessageTemplateComponent()
                    {
                        Type = "BODY",
                        Parameters = new List<CarouselMessageParameter>()
                        {
                            new CarouselMessageParameter()
                            {
                                Type = "Text",
                                Text = "20OFF"
                            },
                            new CarouselMessageParameter()
                            {
                                Type = "Text",
                                Text = "20%"
                            }
                        }
                    },
                    new CarouselMessageTemplateComponent()
                    {
                        Type = "CAROUSEL",
                        Cards = new List<CarouselMessageCard>()
                        {
                            new CarouselMessageCard()
                            {
                                CardIndex = 0,
                                Components = new List<CarouselCardComponent>()
                                {
                                    new CarouselCardComponent()
                                    {
                                        Type = "HEADER",
                                        Parameters = new List<CardMessageParameter>()
                                        {
                                            new CardMessageParameter()
                                            {
                                                Type = "IMAGE",
                                                Image = new CardImage()
                                                {
                                                    Id = "24230790383178626"
                                                }
                                            }
                                        }
                                    },
                                    new CarouselCardComponent()
                                    {
                                        Type = "BODY",
                                        Parameters = new List<CardMessageParameter>()
                                        {
                                            new CardMessageParameter()
                                            {
                                                Type = "Text",
                                                Text = "10OFF"
                                            },
                                            new CardMessageParameter()
                                            {
                                                Type = "Text",
                                                Text = "10%"
                                            }
                                        }
                                    },
                                    new CarouselCardComponent()
                                    {
                                        Type = "BUTTON",
                                        SubType = "QUICK_REPLY",
                                        Index = 0,
                                        Parameters = new List<CardMessageParameter>()
                                        {
                                            new CardMessageParameter()
                                            {
                                                Type = "PAYLOAD",
                                                Payload = "59NqSd"
                                            }
                                        }
                                    },
                                    new CarouselCardComponent()
                                    {
                                        Type = "button",
                                        SubType = "URL",
                                        Index = 1,
                                        Parameters = new List<CardMessageParameter>()
                                        {
                                            new CardMessageParameter()
                                            {
                                                Type = "PAYLOAD",
                                                Payload = "last_chance_2023"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                };

                var results = await _whatsAppBusinessClient.SendCarouselMessageTemplateAsync(carouselTemplateMessageRequest);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent carousel template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppCouponCodeTemplateMessage(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                CouponCodeTemplateMessageRequest couponCodeTemplateMessageRequest = new CouponCodeTemplateMessageRequest();
                couponCodeTemplateMessageRequest.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                couponCodeTemplateMessageRequest.Template = new();
                couponCodeTemplateMessageRequest.Template.Name = sendTemplateMessageViewModel.TemplateName;
                couponCodeTemplateMessageRequest.Template.Language = new();
                couponCodeTemplateMessageRequest.Template.Language.Code = LanguageCode.English_US;
                couponCodeTemplateMessageRequest.Template.Components = new List<CouponCodeMessageComponent>()
                {
                    new CouponCodeMessageComponent()
                    {
                        Type = "body",
                        Parameters = new List<CouponCodeMessageParameter>()
                        {
                            new CouponCodeMessageParameter()
                            {
                                Type = "text",
                                Text = "25OFF"
                            },
                            new CouponCodeMessageParameter()
                            {
                                Type = "text",
                                Text = "25%"
                            }
                        }
                    },
                    new CouponCodeMessageComponent()
                    {
                        Type = "button",
                        SubType = "COPY_CODE",
                        Index = 1,
                        Parameters = new List<CouponCodeMessageParameter>()
                        {
                            new CouponCodeMessageParameter()
                            {
                                Type = "coupon_code",
                                Text = "25OFF"
                            }
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendCouponCodeMessageTemplateAsync(couponCodeTemplateMessageRequest);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent coupon code template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppLimitedTimeOfferTemplateMessage(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                LimitedTimeOfferTemplateMessageRequest limitedTimeOfferTemplateMessageRequest = new LimitedTimeOfferTemplateMessageRequest();
                limitedTimeOfferTemplateMessageRequest.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                limitedTimeOfferTemplateMessageRequest.Template = new();
                limitedTimeOfferTemplateMessageRequest.Template.Name = sendTemplateMessageViewModel.TemplateName;
                limitedTimeOfferTemplateMessageRequest.Template.Language = new();
                limitedTimeOfferTemplateMessageRequest.Template.Language.Code = LanguageCode.English_US;
                limitedTimeOfferTemplateMessageRequest.Template.Components = new List<LimitedTimeOfferMessageComponent>()
                {
                    new LimitedTimeOfferMessageComponent()
                    {
                        Type = "body",
                        Parameters = new List<LimitedTimeOfferMessageParameter>()
                        {
                            new LimitedTimeOfferMessageParameter()
                            {
                                Type = "text",
                                Text = "Pablo"
                            },
                            new LimitedTimeOfferMessageParameter()
                            {
                                Type = "text",
                                Text = "CARIBE25"
                            }
                        }
                    },
                    new LimitedTimeOfferMessageComponent()
                    {
                        Type = "limited_time_offer",
                        Parameters = new List<LimitedTimeOfferMessageParameter>()
                        {
                            new LimitedTimeOfferMessageParameter()
                            {
                                Type = "limited_time_offer",
                                LimitedTimeOffer = new LimitedTimeOffer()
                                {
                                    ExpirationTimeMs = new DateTimeOffset(DateTime.UtcNow.AddHours(2)).ToUnixTimeMilliseconds()
                                }
                            }
                        }
                    },
                    new LimitedTimeOfferMessageComponent()
                    {
                        Type = "button",
                        SubType = "copy_code",
                        Index = 0,
                        Parameters = new List<LimitedTimeOfferMessageParameter>()
                        {
                            new LimitedTimeOfferMessageParameter()
                            {
                                Type = "coupon_code",
                                CouponCode = "CARIBE25"
                            }
                        }
                    },
                    new LimitedTimeOfferMessageComponent()
                    {
                        Type = "button",
                        SubType = "url",
                        Index = 1,
                        Parameters = new List<LimitedTimeOfferMessageParameter>()
                        {
                            new LimitedTimeOfferMessageParameter()
                            {
                                Type = "text",
                                Text = "https://www.google.com/maps"
                            }
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendLimitedTimeOfferMessageTemplateAsync(limitedTimeOfferTemplateMessageRequest);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent limited time offer template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppFlowTemplateMessage(SendTemplateMessageViewModel sendTemplateMessageViewModel)
        {
            try
            {
                FlowTemplateMessageRequest flowTemplateMessageRequest = new FlowTemplateMessageRequest();
                flowTemplateMessageRequest.To = sendTemplateMessageViewModel.RecipientPhoneNumber;
                flowTemplateMessageRequest.Template = new();
                flowTemplateMessageRequest.Template.Name = sendTemplateMessageViewModel.TemplateName;
                flowTemplateMessageRequest.Template.Language = new();
                flowTemplateMessageRequest.Template.Language.Code = LanguageCode.English_US;
                flowTemplateMessageRequest.Template.Components = new List<FlowMessageComponent>()
                {
                    new FlowMessageComponent()
                    {
                        Type = "button",
                        SubType = "flow",
                        Index = 0,
                        Parameters = new List<FlowTemplateMessageParameter>()
                        {
                            new FlowTemplateMessageParameter()
                            {
                                Type = "action",
                                Action = new FlowTemplateMessageAction()
                                {
                                    FlowToken = "",
                                }
                            }
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendFlowMessageTemplateAsync(flowTemplateMessageRequest);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent flow template message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppTemplateMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(SendWhatsAppTemplateMessage)).WithDanger("Error", ex.Message);
            }
        }

        public IActionResult SendWhatsAppContactMessage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendWhatsAppContactMessage(SendContactMessageViewModel sendContactMessageViewModel)
        {
            try
            {
                ContactMessageRequest contactMessageRequest = new ContactMessageRequest();
                contactMessageRequest.To = sendContactMessageViewModel.RecipientPhoneNumber;
                contactMessageRequest.Contacts = new List<ContactData>()
                {
                    new ContactData()
                    {
                        Addresses = new List<Address>()
                        {
                            new Address()
                            {
                                State = "State Test",
                                City = "City Test",
                                Zip = "Zip Test",
                                Country = "Country Test",
                                CountryCode = "Country Code Test",
                                Type = "Home"
                            }
                        },
                        Name = new Name()
                        {
                            FormattedName = "Testing name",
                            FirstName = "FName",
                            LastName = "LName",
                            MiddleName = "MName"
                        }
                    }
                };

                var results = await _whatsAppBusinessClient.SendContactAttachmentMessageAsync(contactMessageRequest);

                if (results != null)
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("Success", "Successfully sent contact message");
                }
                else
                {
                    return RedirectToAction(nameof(SendWhatsAppContactMessage));
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return View().WithDanger("Error", ex.Message);
            }
        }

        public IActionResult UploadMedia()
        {
            UploadMediaViewModel uploadMediaViewModel = new UploadMediaViewModel();
            uploadMediaViewModel.UploadType = new List<SelectListItem>()
            {
                new SelectListItem(){ Text = "Normal Upload", Value = "Normal Upload" },
                new SelectListItem(){ Text = "Resumable Upload", Value = "Resumable Upload" },
            };

            return View(uploadMediaViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMedia(UploadMediaViewModel uploadMediaViewModel, IFormFile mediaFile)
        {
            try
            {
                var fileName = Path.GetFileName(mediaFile.FileName).Trim('"');

                var rootPath = Path.Combine(_environment.WebRootPath, "Application_Files\\MediaUploads\\");

                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                // Get the path of filename
                var filePath = Path.Combine(_environment.WebRootPath, "Application_Files\\MediaUploads\\", fileName);

                // Upload Csv file to the browser
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await mediaFile.CopyToAsync(stream);
                }

                if (uploadMediaViewModel.SelectedUploadType.Equals("Normal Upload", StringComparison.OrdinalIgnoreCase))
                {
                    UploadMediaRequest uploadMediaRequest = new UploadMediaRequest();
                    uploadMediaRequest.File = filePath;
                    uploadMediaRequest.Type = mediaFile.ContentType;

                    var uploadMediaResult = await _whatsAppBusinessClient.UploadMediaAsync(uploadMediaRequest);

                    var mediaUrlResult = await _whatsAppBusinessClient.GetMediaUrlAsync(uploadMediaResult.MediaId);

                    var mediaBytes = await _whatsAppBusinessClient.DownloadMediaAsync(mediaUrlResult.Url);

                    ViewBag.MediaId = uploadMediaResult.MediaId;
                }
                else // Resumable upload generates header file response to be used for creating message templates
                {
                    var resumableUploadMediaResult = await _whatsAppBusinessClient.CreateResumableUploadSessionAsync(mediaFile.Length, mediaFile.ContentType, mediaFile.FileName);

                    if (resumableUploadMediaResult is not null)
                    {
                        var uploadSessionId = resumableUploadMediaResult.Id;

                        var resumableUploadResponse = await _whatsAppBusinessClient.UploadFileDataAsync(uploadSessionId, filePath, mediaFile.ContentType);

                        var queryResumableUploadStatus = await _whatsAppBusinessClient.QueryFileUploadStatusAsync(uploadSessionId);

                        if (resumableUploadResponse is not null)
                        {
                            ViewBag.H = resumableUploadResponse.H;
                        }

                        if (queryResumableUploadStatus is not null)
                        {
                            ViewBag.StatusId = queryResumableUploadStatus.Id;
                            ViewBag.FileOffset = queryResumableUploadStatus.FileOffset;
                        }
                    }
                }

                return View(uploadMediaViewModel).WithSuccess("Success", "Successfully upload media.");
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(UploadMedia)).WithDanger("Error", ex.Message);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult CreateQRCodeMessage()
        {
            QRCodeMessageViewModel qrCodeMessageViewModel = new QRCodeMessageViewModel();
            qrCodeMessageViewModel.ImageFormat = new List<SelectListItem>()
            {
                new SelectListItem(){ Text = "SVG", Value = "SVG" },
                new SelectListItem(){ Text = "PNG", Value = "PNG" },
            };

            return View(qrCodeMessageViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQRCodeMessage(QRCodeMessageViewModel qrCodeMessageViewModel)
        {
            try
            {
                var results = await _whatsAppBusinessClient.CreateQRCodeMessageAsync(qrCodeMessageViewModel.Message, qrCodeMessageViewModel.SelectedImageFormat);

                if (results is not null)
                {
                    ViewBag.QRCodeId = results.Code;
                    ViewBag.QRCodeMessage = results.PrefilledMessage;
                    ViewBag.QRCodeUrl = results.QrImageUrl;
                    return View(qrCodeMessageViewModel).WithSuccess("Success", "Successfully created QR code Message.");
                }
                else
                {
                    return View(qrCodeMessageViewModel).WithDanger("Error", "QR code message is null");
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return View(qrCodeMessageViewModel).WithDanger("Error", ex.Message);
            }
        }

        public async Task<IActionResult> QRCodeMessageList()
        {
            try
            {
                var results = await _whatsAppBusinessClient.GetQRCodeMessageListAsync();

                if (results is not null)
                {
                    if (results.Data.Any())
                    {
                        return View(results.Data).WithSuccess("Success", "Successfully retrieved QR code Message List");
                    }
                    else
                    {
                        return View(results.Data).WithDanger("Error", "QR code message list is empty");
                    }
                }
                else
                {
                    return View().WithDanger("Error", "QR code message list not availble");
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return View().WithDanger("Error", ex.Message);
            }
        }

        public async Task<IActionResult> Analytics()
        {
            try
            {
                DateTime currentDate = DateTime.UtcNow;
                var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                var results = await _whatsAppBusinessClient.GetAnalyticMetricsAsync(_whatsAppConfig.WhatsAppBusinessAccountId, startOfMonth, endOfMonth, Granularity.AnalyticsGranularity.MONTH);

                if (results is not null)
                {
                    return View(results).WithSuccess("Success", "Analytics retrieved successfully");
                }
                else
                {
                    return View().WithDanger("Error", "Analytics not available");
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return View().WithDanger("Error", ex.Message);
            }
        }

        public async Task<IActionResult> ConversationAnalytics()
        {
            try
            {
                DateTime currentDate = DateTime.UtcNow;
                var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                var results = await _whatsAppBusinessClient.GetConversationAnalyticMetricsAsync(_whatsAppConfig.WhatsAppBusinessAccountId, startOfMonth, endOfMonth, Granularity.ConversationGranularity.MONTHLY);

                if (results is not null)
                {
                    return View(results).WithSuccess("Success", "Conversation Analytics retrieved successfully");
                }
                else
                {
                    return View().WithDanger("Error", "Conversation Analytics not available");
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return View().WithDanger("Error", ex.Message);
            }
        }

        public IActionResult BulkSendWhatsApps()
        {
            BulkSendWhatsAppsViewModel bulkSendWhatsAppsViewModel = new BulkSendWhatsAppsViewModel();

            return View(bulkSendWhatsAppsViewModel);
        }

        /// <summary>
		/// Make use of BulkSendWhatsAppController to read a CSV file, loop through the file and send whatsApp per record
		/// </summary>
		/// <param name="bulkSendWhatsAppsViewModel"></param>
		/// <param name="bulkFile"></param>
		/// <returns></returns>
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkSendWhatsApps(BulkSendWhatsAppsViewModel bulkSendWhatsAppsViewModel, IFormFile bulkFile)
        {
            try
            { // This is to call the relevant methods to run through the file and Bulk Send WhatsApps
              //List<string> WAMIds = new List<string>();			

                // Upload the Bulk File to the Local Server
                FileInfo fileInfo = new();
                FileManagmentController fileController = new(_logger, _whatsAppBusinessClient, _environment);
                fileInfo = await fileController.UploadFileToLocalServer(bulkFile);

                // Now go through the file and send the WhatsApps
                BulkSendWhatsAppsController bulkSendWhatsAppsController = new(_logger, _whatsAppBusinessClient, _environment);
                var WAMIDs = bulkSendWhatsAppsController.ReadAndTraverseCSV(fileInfo);

                return View(bulkSendWhatsAppsViewModel);
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, ex.Message);
                return RedirectToAction(nameof(bulkFile)).WithDanger("Error", ex.Message);
            }
        }

        public IActionResult EmbeddedSignup()
        {
            // Create a copy of the configuration with runtime base URL
            var config = new EmbeddedSignupConfiguration
            {
                AppId = _embeddedSignupConfig.AppId,
                AppSecret = _embeddedSignupConfig.AppSecret,
                ConfigurationId = _embeddedSignupConfig.ConfigurationId,
                GraphApiVersion = _embeddedSignupConfig.GraphApiVersion,
                // Use configured BaseUrl if provided, otherwise use runtime detection
                BaseUrl = string.IsNullOrEmpty(_embeddedSignupConfig.BaseUrl) 
                    ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}" 
                    : _embeddedSignupConfig.BaseUrl
            };
            
            return View(config);
        }







        public IActionResult TwilioEmbeddedSignup()
        {
            // Create a copy of the configuration with runtime base URL
            var config = new EmbeddedSignupConfiguration
            {
                AppId = _embeddedSignupConfig.AppId,
                AppSecret = _embeddedSignupConfig.AppSecret,
                ConfigurationId = _embeddedSignupConfig.ConfigurationId,
                GraphApiVersion = _embeddedSignupConfig.GraphApiVersion,
                // Use configured BaseUrl if provided, otherwise use runtime detection
                BaseUrl = string.IsNullOrEmpty(_embeddedSignupConfig.BaseUrl) 
                    ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}" 
                    : _embeddedSignupConfig.BaseUrl
            };
            
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExchangeToken([FromBody] ExchangeTokenRequestViewModel request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
                {
                    return Json(new { success = false, error = "Missing required parameters" });
                }

                // Use the CloudApi library for token exchange
                var exchangeRequest = new WhatsappBusiness.CloudApi.OAuth.Requests.ExchangeTokenRequest
                {
                    ClientId = request.ClientId,
                    ClientSecret = request.ClientSecret,
                    Code = request.Code,
                    RedirectUri = request.RedirectUri
                };

                var result = await _whatsAppBusinessClient.ExchangeTokenAsync(exchangeRequest);

                if (!string.IsNullOrEmpty(result.AccessToken))
                {
                    _logger.LogInformation("Token exchange successful");
                    return Json(new { success = true, data = result });
                }
                else
                {
                    _logger.LogError("Token exchange failed: {Error}", result.Error ?? "Unknown error");
                    return Json(new { success = false, error = result.Error ?? "Token exchange failed" });
                }
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, "WhatsApp API error during token exchange");
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token exchange");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetSharedWABADetails([FromBody] GetSharedWABADetailsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AccessToken))
                {
                    return Json(new { success = false, error = "Access token is required" });
                }

                // Get the shared WABA ID using the access token
                var sharedWabaResponse = await _whatsAppBusinessClient.GetSharedWABAIdAsync(request.AccessToken);
                var wabaId = sharedWabaResponse.GetSharedWABAId();

                if (string.IsNullOrEmpty(wabaId))
                {
                    return Json(new { success = false, error = "Could not retrieve WABA ID from access token" });
                }

                _logger.LogInformation("Retrieved WABA ID: {WabaId}", wabaId);

                // Get detailed WABA information using the new endpoint
                var wabaDetailsResponse = await _whatsAppBusinessClient.GetWABADetailsAsync(wabaId);
                
                // Try to get phone numbers for the WABA to get phone details and verified business name
                // Note: This may return empty or null if no phone numbers are associated with the WABA yet
                // (e.g., when only WABA sharing is selected and phone number selection is skipped)
                PhoneNumberResponse? phoneNumberResponse = null;
                string? phoneNumberId = null;
                PhoneNumberData? phoneNumberData = null;
                string? displayPhoneNumber = null;
                
                try
                {
                    phoneNumberResponse = await _whatsAppBusinessClient.GetWhatsAppBusinessAccountPhoneNumberAsync(wabaId);
                    
                    if (phoneNumberResponse?.Data != null && phoneNumberResponse.Data.Any())
                    {
                        // Get the most recently onboarded phone number details
                        phoneNumberId = phoneNumberResponse.GetMostRecentlyOnboardedPhoneNumberId();
                        phoneNumberData = phoneNumberResponse.Data?.FirstOrDefault(p => p.Id == phoneNumberId);
                        displayPhoneNumber = phoneNumberData?.DisplayPhoneNumber;
                        
                        _logger.LogInformation("Retrieved phone number details for WABA {WabaId} - PhoneId: {PhoneId}, Display: {DisplayPhoneNumber}", 
                            wabaId, phoneNumberId, displayPhoneNumber);
                    }
                    else
                    {
                        _logger.LogInformation("No phone numbers found for WABA {WabaId} - this is expected when only WABA sharing is enabled", wabaId);
                    }
                }
                catch (Exception phoneEx)
                {
                    _logger.LogWarning(phoneEx, "Could not retrieve phone numbers for WABA {WabaId} - this may be expected if no phone numbers are associated yet", wabaId);
                    // Continue execution - phone numbers are optional for WABA-only sharing
                }
                
                // Get business name from multiple sources with priority:
                // 1. Phone number verified_name (most reliable, but may be null)
                // 2. WABA owner business info name
                // 3. WABA name
                // 4. Fallback to "Unknown Business"
                string businessName = phoneNumberData?.VerifiedName ?? 
                                      wabaDetailsResponse.OwnerBusinessInfo?.Name ?? 
                                      wabaDetailsResponse.Name ?? 
                                      "Unknown Business";
                
                _logger.LogInformation("Retrieved enhanced business details - Name: {BusinessName}, Phone: {PhoneNumber}, PhoneId: {PhoneId}, Status: {Status}, HealthStatus: {HealthStatus}", 
                    businessName, displayPhoneNumber ?? "None", phoneNumberId ?? "None", wabaDetailsResponse.Status, wabaDetailsResponse.HealthStatus?.CanSendMessage);

                return Json(new { 
                    success = true, 
                    data = new {
                        id = wabaId,
                        name = businessName,
                        phone_number_id = phoneNumberId, // May be null if no phone numbers available
                        display_phone_number = displayPhoneNumber, // May be null if no phone numbers available
                        verified_name = phoneNumberData?.VerifiedName, // May be null if no phone numbers available
                        has_phone_numbers = phoneNumberResponse?.Data?.Any() == true, // Indicates if phone numbers are available
                        
                        // Enhanced WABA details
                        currency = wabaDetailsResponse.Currency,
                        timezone_id = wabaDetailsResponse.TimezoneId,
                        account_review_status = wabaDetailsResponse.AccountReviewStatus,
                        business_verification_status = wabaDetailsResponse.BusinessVerificationStatus,
                        country = wabaDetailsResponse.Country,
                        status = wabaDetailsResponse.Status,
                        primary_business_location = wabaDetailsResponse.PrimaryBusinessLocation,
                        
                        // Owner business information
                        owner_business_info = wabaDetailsResponse.OwnerBusinessInfo != null ? new {
                            id = wabaDetailsResponse.OwnerBusinessInfo.Id,
                            name = wabaDetailsResponse.OwnerBusinessInfo.Name
                        } : null,
                        
                        // Health status
                        health_status = wabaDetailsResponse.HealthStatus != null ? new {
                            can_send_message = wabaDetailsResponse.HealthStatus.CanSendMessage,
                            entities = wabaDetailsResponse.HealthStatus.Entities
                        } : null,
                        
                        // Phone numbers list for additional context (may be null if no phone numbers available)
                        phoneNumbers = phoneNumberResponse?.Data
                    }
                });
            }
            catch (WhatsappBusinessCloudAPIException ex)
            {
                _logger.LogError(ex, "WhatsApp API error during WABA details retrieval");
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during WABA details retrieval");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTwilioSubaccount([FromBody] CreateTwilioSubaccountRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.BusinessName) || string.IsNullOrEmpty(request.WabaId))
                {
                    return Json(new { success = false, error = "Business name and WABA ID are required" });
                }

                _logger.LogInformation("Creating Twilio subaccount for business: {BusinessName}, WABA: {WabaId}", 
                    request.BusinessName, request.WabaId);

                // Create Twilio subaccount using the service
                var (subaccountSuccess, subaccountSid, subaccountAuthToken, mainAccountSid, mainAccountToken, subaccountError) = 
                    await _twilioIntegrationService.CreateTwilioSubaccountAsync(request.BusinessName, request.WabaId);

                _logger.LogInformation("Subaccount creation result: Success={Success}, SID={SID}, AuthTokenPresent={AuthTokenPresent}, MainAccountAuthPresent={MainAccountAuthPresent}, Error={Error}", 
                    subaccountSuccess, subaccountSid, !string.IsNullOrEmpty(subaccountAuthToken), !string.IsNullOrEmpty(mainAccountToken), subaccountError);

                if (subaccountSuccess && !string.IsNullOrEmpty(subaccountSid))
                {
                    _logger.LogInformation("Twilio subaccount created successfully for {BusinessName} - SID: {SubaccountSid}", 
                        request.BusinessName, subaccountSid);

                    // Determine which authentication method to return
                    string authMethod = "none";
                    if (!string.IsNullOrEmpty(subaccountAuthToken))
                    {
                        authMethod = "subaccount_auth_token";
                    }
                    else if (!string.IsNullOrEmpty(mainAccountToken))
                    {
                        authMethod = "main_account_credentials";
                    }

                    // Return subaccount details and pass-through data for subsequent phone number operations
                    return Json(new { 
                        success = true, 
                        data = new {
                            twilioSubaccountSid = subaccountSid,
                            twilioAuthToken = subaccountAuthToken, // May be null - this is normal
                            twilioMainAccountSid = mainAccountSid, // Main account SID for authentication
                            twilioMainAccountToken = mainAccountToken, // Main account token for authentication
                            authenticationMethod = authMethod, // Indicates which auth method to use
                            businessName = request.BusinessName,
                            wabaId = request.WabaId,
                            // Pass-through data for phone number operations
                            phoneNumberType = request.PhoneNumberType,
                            wabaPhoneNumberId = request.PhoneNumberId,
                            wabaPhoneNumberForDisplay = request.DisplayPhoneNumber,
                            message = "Twilio subaccount created successfully. Use the provided credentials for phone number operations."
                        }
                    });
                }
                else
                {
                    _logger.LogError("Failed to create Twilio subaccount: {Error}", subaccountError);
                    return Json(new { success = false, error = $"Failed to create Twilio subaccount: {subaccountError}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Twilio subaccount creation");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAvailableCountries([FromBody] GetAvailableCountriesRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SubaccountSid) || string.IsNullOrEmpty(request.SubaccountAuthToken))
                {
                    return Json(new { success = false, error = "Subaccount SID and auth token are required" });
                }

                _logger.LogInformation("Getting available countries for Twilio subaccount: {SubaccountSid}", request.SubaccountSid);

                var countries = await _twilioIntegrationService.GetAvailableCountriesAsync(request.SubaccountSid, request.SubaccountAuthToken);
                
                _logger.LogInformation("Retrieved {Count} available countries", countries.Count);
                
                return Json(new { 
                    success = true, 
                    data = new {
                        countries = countries,
                        totalCount = countries.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available countries");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckRegulatoryRequirements([FromBody] CheckRegulatoryRequirementsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SubaccountSid) || string.IsNullOrEmpty(request.SubaccountAuthToken) || 
                    string.IsNullOrEmpty(request.IsoCountry) || string.IsNullOrEmpty(request.NumberType))
                {
                    return Json(new { success = false, error = "Subaccount SID, auth token, country code, and number type are required" });
                }

                _logger.LogInformation("Checking regulatory requirements for {IsoCountry} {NumberType}", 
                    request.IsoCountry, request.NumberType);

                var (requiresBundle, supportedCountry, message) = await _twilioIntegrationService.CheckRegulatoryRequirementsAsync(
                    request.SubaccountSid, request.SubaccountAuthToken, request.IsoCountry, request.NumberType);

                return Json(new { 
                    success = true, 
                    requiresBundle = requiresBundle,
                    supportedCountry = supportedCountry,
                    isoCountry = request.IsoCountry?.ToUpper(),
                    numberType = request.NumberType?.ToLower(),
                    message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking regulatory requirements for {IsoCountry} {NumberType}", 
                    request.IsoCountry, request.NumberType);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRegulatoryBundle([FromBody] CreateRegulatoryBundleRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SubaccountSid) || string.IsNullOrEmpty(request.SubaccountAuthToken) ||
                    string.IsNullOrEmpty(request.BusinessName) || string.IsNullOrEmpty(request.IsoCountry))
                {
                    return Json(new { success = false, error = "Subaccount SID, auth token, business name, and ISO country are required" });
                }

                // Validate required fields based on country
                if (request.IsoCountry?.ToUpper() == "GB")
                {
                    if (string.IsNullOrEmpty(request.BusinessRegistrationNumber) ||
                        string.IsNullOrEmpty(request.BusinessAddress) ||
                        string.IsNullOrEmpty(request.BusinessCity) ||
                        string.IsNullOrEmpty(request.BusinessPostalCode) ||
                        string.IsNullOrEmpty(request.AuthorizedContactFirstName) ||
                        string.IsNullOrEmpty(request.AuthorizedContactLastName) ||
                        string.IsNullOrEmpty(request.AuthorizedContactEmail) ||
                        string.IsNullOrEmpty(request.AuthorizedContactPhone))
                    {
                        return Json(new { success = false, error = "All business information fields are required for GB regulatory bundles" });
                    }
                }

                _logger.LogInformation("Creating regulatory bundle for {BusinessName} in {IsoCountry}", 
                    request.BusinessName, request.IsoCountry);

                var (bundleSuccess, bundleSid, bundleError) = await _twilioIntegrationService.CreateRegulatoryBundleAsync(request);

                if (bundleSuccess && !string.IsNullOrEmpty(bundleSid))
                {
                    _logger.LogInformation("Regulatory bundle created successfully: {BundleSid}", bundleSid);

                    return Json(new { 
                        success = true, 
                        bundleSid = bundleSid,
                        message = "Regulatory bundle created successfully and is pending approval.",
                        data = new {
                            bundleSid = bundleSid,
                            isoCountry = request.IsoCountry?.ToUpper(),
                            numberType = request.NumberType?.ToLower(),
                            businessName = request.BusinessName,
                            endUserType = request.EndUserType
                        }
                    });
                }
                else
                {
                    _logger.LogError("Failed to create regulatory bundle: {Error}", bundleError);
                    return Json(new { success = false, error = bundleError ?? "Failed to create regulatory bundle" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating regulatory bundle for {BusinessName} in {IsoCountry}", 
                    request.BusinessName, request.IsoCountry);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAvailablePhoneNumbers([FromBody] GetAvailablePhoneNumbersRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SubaccountSid) || string.IsNullOrEmpty(request.SubaccountAuthToken) || 
                    string.IsNullOrEmpty(request.CountryCode) || string.IsNullOrEmpty(request.PhoneNumberType))
                {
                    return Json(new { success = false, error = "Subaccount SID, auth token, country code, and phone number type are required" });
                }

                _logger.LogInformation("Getting available {PhoneNumberType} phone numbers for country: {CountryCode}, SubaccountSid: {SubaccountSid}", 
                    request.PhoneNumberType, request.CountryCode, request.SubaccountSid);

                var phoneNumbers = await _twilioIntegrationService.GetAvailablePhoneNumbersAsync(
                    request.SubaccountSid, 
                    request.SubaccountAuthToken, 
                    request.CountryCode, 
                    request.PhoneNumberType,
                    request.Limit ?? 20);
                
                _logger.LogInformation("Retrieved {Count} available {PhoneNumberType} phone numbers for {CountryCode}", 
                    phoneNumbers.Count, request.PhoneNumberType, request.CountryCode);
                
                return Json(new { 
                    success = true, 
                    data = new {
                        phoneNumbers = phoneNumbers,
                        countryCode = request.CountryCode,
                        phoneNumberType = request.PhoneNumberType,
                        totalCount = phoneNumbers.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available phone numbers for {CountryCode} {PhoneNumberType}", 
                    request.CountryCode, request.PhoneNumberType);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurchasePhoneNumber([FromBody] PurchasePhoneNumberRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SubaccountSid) || string.IsNullOrEmpty(request.SubaccountAuthToken) || 
                    string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.BusinessName))
                {
                    return Json(new { success = false, error = "Subaccount SID, auth token, phone number, and business name are required" });
                }

                _logger.LogInformation("Purchasing Twilio phone number {PhoneNumber} for business: {BusinessName}, SubaccountSid: {SubaccountSid}", 
                    request.PhoneNumber, request.BusinessName, request.SubaccountSid);

                var purchaseResult = await _twilioIntegrationService.PurchasePhoneNumberAsync(
                    request.SubaccountSid,
                    request.PhoneNumber,
                    request.BusinessName,
                    request.CountryCode ?? "");

                if (purchaseResult.Success)
                {
                    _logger.LogInformation("Twilio phone number {PhoneNumber} purchased successfully for {BusinessName}", 
                        request.PhoneNumber, request.BusinessName);

                    return Json(new { 
                        success = true, 
                        data = new {
                            phoneNumber = request.PhoneNumber,
                            phoneNumberSid = purchaseResult.PhoneNumberSid,
                            subaccountSid = request.SubaccountSid,
                            businessName = request.BusinessName,
                            message = $"Twilio phone number {request.PhoneNumber} purchased successfully"
                        }
                    });
                }
                else
                {
                    _logger.LogError("Failed to purchase Twilio phone number {PhoneNumber}: {Error}", request.PhoneNumber, purchaseResult.ErrorMessage);
                    return Json(new { success = false, error = $"Failed to purchase Twilio phone number: {purchaseResult.ErrorMessage}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purchasing Twilio phone number {PhoneNumber}", request.PhoneNumber);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPhoneNumberForWhatsApp([FromBody] RegisterPhoneNumberForWhatsAppRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SubaccountSid) || string.IsNullOrEmpty(request.SubaccountAuthToken) || 
                    string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.BusinessName) ||
                    string.IsNullOrEmpty(request.WabaId))
                {
                    return Json(new { success = false, error = "Subaccount SID, auth token, phone number, business name, and WABA ID are required" });
                }

                _logger.LogInformation("Registering phone number {PhoneNumber} for WhatsApp - Business: {BusinessName}, WABA: {WabaId}, SubaccountSid: {SubaccountSid}", 
                    request.PhoneNumber, request.BusinessName, request.WabaId, request.SubaccountSid);

                var webhookBaseUrl = $"{Request.Scheme}://{Request.Host}/api/WhatsAppNotification";
                var registrationResult = await _twilioIntegrationService.RegisterPhoneNumberForWhatsAppAsync(
                    request.SubaccountSid, 
                    request.SubaccountAuthToken, 
                    request.PhoneNumber, 
                    request.BusinessName,
                    request.WabaId,
                    webhookBaseUrl);

                if (registrationResult.Success)
                {
                    _logger.LogInformation("Phone number {PhoneNumber} registered for WhatsApp successfully - SenderSid: {SenderSid}", 
                        request.PhoneNumber, registrationResult.SenderSid);

                    return Json(new { 
                        success = true, 
                        data = new {
                            phoneNumber = request.PhoneNumber,
                            senderSid = registrationResult.SenderSid,
                            subaccountSid = request.SubaccountSid,
                            businessName = request.BusinessName,
                            wabaId = request.WabaId,
                            status = registrationResult.Status,
                            message = $"Phone number {request.PhoneNumber} registered for WhatsApp successfully"
                        }
                    });
                }
                else
                {
                    _logger.LogError("Failed to register phone number {PhoneNumber} for WhatsApp: {Error}", request.PhoneNumber, registrationResult.ErrorMessage);
                    return Json(new { success = false, error = $"Failed to register phone number for WhatsApp: {registrationResult.ErrorMessage}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering phone number {PhoneNumber} for WhatsApp", request.PhoneNumber);
                return Json(new { success = false, error = ex.Message });
            }
        }

    }
}
