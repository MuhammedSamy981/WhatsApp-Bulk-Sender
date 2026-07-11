using Microsoft.AspNetCore.Mvc;
using WhatsAppBulkSender.Models;
//using WhatsAppBulkSender.Services;

namespace WhatsAppBulkSender.Controllers
{
    public class HomeController : Controller
    {
        private readonly WhatsAppBulkSender.Services.WhatsAppService _whatsAppService;
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;

        public HomeController(WhatsAppBulkSender.Services.WhatsAppService whatsAppService,
                              ILogger<HomeController> logger,
                              IWebHostEnvironment env)
        {
            _whatsAppService = whatsAppService;
            _logger          = logger;
            _env             = env;
        }

        public IActionResult Index()
        {
            //ViewBag.Session = _whatsAppService.GetSession();
            ViewBag.Quota   = _whatsAppService.Quota;
            return View(new MessageRequest());
        }




        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(MessageRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Quota   = _whatsAppService.Quota;
                return View("Index", model);
            }

            // Parse "Name|Number" or plain "Number" entries
            var recipients = model.PhoneNumbers
                .Split('\n')
                .Select(line =>
                {
                    var parts = line.Trim().Split('|');
                    return parts.Length >= 2
                        ? (phone: parts[1].Trim(), name: parts[0].Trim())
                        : (phone: parts[0].Trim(), name: string.Empty);
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.phone))
                .DistinctBy(r => r.phone)
                .ToList();

            if (recipients.Count == 0)
            {
                ModelState.AddModelError("PhoneNumbers", "No valid phone numbers found.");
                ViewBag.Quota   = _whatsAppService.Quota;
                return View("Index", model);
            }

            // Save attachment
            string? attachmentPath = null;
            string? fileName = null;
            if (model.Attachment?.Length > 0)
            {
                var dir  = Path.Combine(_env.ContentRootPath, "uploads");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir,
                    $"{Guid.NewGuid()}{Path.GetExtension(model.Attachment.FileName)}");
                await using var s = System.IO.File.Create(file);
                await model.Attachment.CopyToAsync(s);
                attachmentPath = file;
                fileName = model.Attachment.FileName;
            }

            try
            {
                var result = await _whatsAppService.SendBulkMessagesAsync(
                    recipients,
                    model.Message,
                    model.DelayMinSeconds,
                    model.DelayMaxSeconds,
                    model.BatchSize,
                    model.CoolingPauseSeconds,
                    model.EnableSpinning,
                    model.DailyLimit,
                    attachmentPath,
                    fileName
                );

                if (attachmentPath != null && System.IO.File.Exists(attachmentPath))
                    System.IO.File.Delete(attachmentPath);

                TempData["Result"] = Newtonsoft.Json.JsonConvert.SerializeObject(result);
                return RedirectToAction("Result");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Quota   = _whatsAppService.Quota;
                return View("Index", model);
            }
        }

        public IActionResult Result()
        {
            if (TempData["Result"] is string json)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<BulkSendResult>(json);
                return View(result);
            }
            return RedirectToAction("Index");
        }
    }
}
