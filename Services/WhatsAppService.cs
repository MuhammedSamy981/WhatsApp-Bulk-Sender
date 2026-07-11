using WhatsAppBulkSender.Models;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using RestSharp;

namespace WhatsAppBulkSender.Services
{
    public class WhatsAppService 
    {
                // Your SendZen API token (Get it from the SendZen dashboard)
        private const string InstanceId = "instance180253"; // your instanceId
        private const string Token = "ppooid1jse8b1xwy";//instance Token 

        private readonly HttpClient _httpClient;
        private readonly string _instanceId;
        private readonly string _token;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly IHubContext<WhatsAppHub> _hubContext;

        // Daily quota is a singleton counter (resets at midnight automatically)
        public readonly DailyQuota Quota = new();

        public WhatsAppService(ILogger<WhatsAppService> logger,
                               IHubContext<WhatsAppHub> hubContext
                               ,IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _logger      = logger;
            _hubContext  = hubContext;

            _httpClient = httpClientFactory.CreateClient("UltraMsgClient");
        _instanceId = config["UltraMsg:InstanceId"]!;
        _token = config["UltraMsg:Token"]!;
        }
 

        // ── Bulk send with anti-spam protections ─────────────────────────────
        public async Task<BulkSendResult> SendBulkMessagesAsync(
            IEnumerable<(string phone, string name)> recipients,
            string messageTemplate,
            int    delayMinSec,
            int    delayMaxSec,
            int    batchSize,
            int    coolingPauseSec,
            bool   enableSpinning,
            int    dailyLimit,
            string? attachmentPath = null, 
            string? fileName = null
            )
        {
            var result = new BulkSendResult();

            if ( _httpClient == null)
                throw new InvalidOperationException(
                    "WhatsApp is not connected. Please check your instance from this link https://user.ultramsg.com/app/instances/instances.php");

            // ── Daily quota pre-flight ────────────────────────────────────────
            var recipientList = recipients.ToList();
            var (allowed, quotaReason) = AntiSpamEngine.CheckQuota(
                Quota, dailyLimit, recipientList.Count);

            if (!allowed)
                throw new InvalidOperationException(quotaReason);

            int sentInSession = 0;

            foreach (var (rawPhone, name) in recipientList)
            {
                var phone = NormalizePhoneNumber(rawPhone);
                if (string.IsNullOrWhiteSpace(phone)) continue;

                // ── Batch cooling pause ───────────────────────────────────────
                bool wasBatchPause = false;
                if (AntiSpamEngine.IsBatchBoundary(sentInSession, batchSize))
                {
                    wasBatchPause = true;
                    _logger.LogInformation(
                        $"Batch boundary reached ({sentInSession} sent). " +
                        $"Cooling for {coolingPauseSec}s…");

                    await _hubContext.Clients.All.SendAsync("BatchPause",
                        new { sentSoFar = sentInSession, pauseSec = coolingPauseSec });

                    await Task.Delay(TimeSpan.FromSeconds(coolingPauseSec));
                }

                // ── Prepare unique message for this recipient ─────────────────
                string finalMsg = enableSpinning
                    ? AntiSpamEngine.PrepareMessage(messageTemplate, phone, name)
                    : messageTemplate
                        .Replace("{name}",   string.IsNullOrWhiteSpace(name) ? phone : name,
                                 StringComparison.OrdinalIgnoreCase)
                        .Replace("{number}", phone,
                                 StringComparison.OrdinalIgnoreCase);

                var sendResult = new SendResult
                {
                    PhoneNumber   = phone,
                    DisplayName   = name,
                    SentAt        = DateTime.Now,
                    FinalMessage  = finalMsg,
                    WasBatchPause = wasBatchPause
                };

                try
                {
                  
                    if (attachmentPath != null && File.Exists(attachmentPath))
                    {
                        var bytes    = await File.ReadAllBytesAsync(attachmentPath);
                        var file     = Convert.ToBase64String(bytes);
                        var ext      = Path.GetExtension(attachmentPath).ToLower();
                        var mime     = GetMimeType(ext);

                        if (mime.StartsWith("image/"))
                            await SendImageAsync(file, phone);
                        else
                        {
                           var test= await SendDocumentAsync(file, fileName??"File", phone);
                            Console.WriteLine($"\n{fileName}\n");
                        }
                    }

                   string sendingResult= await SendMessageAsync(finalMsg, phone)??"";
                     

                    sendResult.Success       = sendingResult.Contains("true");
                    sendResult.StatusMessage = sendingResult.Contains("true") ? "Sent" : "Failed";
                    Quota.IncrementAndRoll();
                    sentInSession++;
                }
                catch (Exception ex)
                {
                    sendResult.Success       = false;
                    sendResult.StatusMessage = ex.Message;
                    _logger.LogError(ex, $"Failed to send to {phone}");
                }

                result.Results.Add(sendResult);
                await _hubContext.Clients.All.SendAsync("MessageStatus", new
                {
                    phoneNumber   = sendResult.PhoneNumber,
                    displayName   = sendResult.DisplayName,
                    success       = sendResult.Success,
                    statusMessage = sendResult.StatusMessage,
                    wasBatchPause = sendResult.WasBatchPause,
                    sentToday     = Quota.SentToday
                });

                // ── Human-like random delay ───────────────────────────────────
                var delayMs = AntiSpamEngine.NextDelayMs(delayMinSec, delayMaxSec);
                sendResult.ActualDelay = delayMs;
                await Task.Delay(delayMs);
            }

            return result;
        }


        private static string NormalizePhoneNumber(string input) =>
            new(input.Where(char.IsDigit).ToArray());

        private static string GetMimeType(string ext) => ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"   => "image/png",
            ".gif"   => "image/gif",
            ".webp"  => "image/webp",
            ".pdf"   => "application/pdf",
            ".doc"   => "application/msword",
            ".docx"  => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".mp4"   => "video/mp4",
            ".mp3"   => "audio/mpeg",
            _        => "application/octet-stream"
        };

    
 private async Task<string?> SendMessageAsync(string messageBody, string toPhoneNumber)
        {
            try
            {
/*var url = "https://api.ultramsg.com/" + InstanceId  +  "/messages/chat";
        var client = new RestClient(url);
        var request = new RestRequest(url, Method.Post);    
        request.AddHeader("content-type", "application/x-www-form-urlencoded");
  			request.AddParameter("token", Token);
			request.AddParameter("to", toPhoneNumber);
			request.AddParameter("body", messageBody);
        RestResponse response = await client.ExecuteAsync(request);
        var output = response.Content;
        return output;*/

        // Define the target endpoint for text messages
        var endpoint = $"{_instanceId}/messages/chat";

        // Construct parameters as standard urlencoded values
        var parameters = new Dictionary<string, string>
        {
            { "token", _token! },
            { "to", toPhoneNumber },
            { "body", messageBody }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync(endpoint, content);
        var output = response.Content;
        Console.WriteLine($"Response from UltraMsg API: {await output.ReadAsStringAsync()}");
        return output.ReadAsStringAsync().Result;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while sending message: {ex.Message}");
                return null;
            }

        }


         private async Task<string?> SendImageAsync(string image, string toPhoneNumber)
        {
            try
            {  
/*            var url = "https://api.ultramsg.com/" + InstanceId  + "/messages/image";
        var client = new RestClient(url);
        var request = new RestRequest(url, Method.Post);    
        request.AddHeader("content-type", "application/x-www-form-urlencoded");
  			request.AddParameter("token", Token);
			request.AddParameter("to", toPhoneNumber);
			request.AddParameter("image", image);
        RestResponse response = await client.ExecuteAsync(request);
        var output = response.Content;
        return output;*/

    // Define the target endpoint for text messages
        var endpoint = $"{_instanceId}/messages/image";

        // Construct parameters as standard urlencoded values
        var parameters = new Dictionary<string, string>
        {
            { "token", _token! },
            { "to", toPhoneNumber },
            { "image", image }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync(endpoint, content);
        var output = response.Content;
        Console.WriteLine($"Response from UltraMsg API: {await output.ReadAsStringAsync()}");
        return output.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while sending message: {ex.Message}");
                return null;
            }
        }



         private async Task<string?> SendDocumentAsync(string document,string fileName, string toPhoneNumber)
        {
            try
            {
           /* var url = "https://api.ultramsg.com/" + InstanceId  + "/messages/document";
            var client = new RestClient(url);
            var request = new RestRequest(url, Method.Post);    
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
  			request.AddParameter("token", Token);
			request.AddParameter("to", toPhoneNumber);
            request.AddParameter("filename", fileName);
            request.AddParameter("document", document);
            RestResponse response = await client.ExecuteAsync(request);
            var output = response.Content;
            return output;*/

    // Define the target endpoint for text messages
        var endpoint = $"{_instanceId}/messages/document";

        // Construct parameters as standard urlencoded values
        var parameters = new Dictionary<string, string>
        {
            { "token", _token! },
            { "to", toPhoneNumber },
            { "filename", fileName },
            { "document", document }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync(endpoint, content);
        var output = response.Content;
        Console.WriteLine($"Response from UltraMsg API: {await output.ReadAsStringAsync()}");
        return output.ReadAsStringAsync().Result; 

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while sending message: {ex.Message}");
                return null;
            }
        }
    }
    }


