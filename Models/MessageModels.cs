using System.ComponentModel.DataAnnotations;

namespace WhatsAppBulkSender.Models
{
    public class MessageRequest
    {
        [Required(ErrorMessage = "Please enter at least one phone number.")]
        [Display(Name = "Phone Numbers")]
        public string PhoneNumbers { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [Display(Name = "Message")]
        [StringLength(4096, ErrorMessage = "Message cannot exceed 4096 characters.")]
        public string Message { get; set; } = string.Empty;

        // ── Anti-spam: smart delay ──────────────────────────────────────────
        [Display(Name = "Min Delay (seconds)")]
        [Range(3, 120, ErrorMessage = "Min delay must be 3–120 seconds.")]
        public int DelayMinSeconds { get; set; } = 5;

        [Display(Name = "Max Delay (seconds)")]
        [Range(3, 180, ErrorMessage = "Max delay must be 3–180 seconds.")]
        public int DelayMaxSeconds { get; set; } = 15;

        // ── Anti-spam: batch cooling ────────────────────────────────────────
        [Display(Name = "Batch Size (messages before cooling pause)")]
        [Range(5, 100, ErrorMessage = "Batch size must be 5–100.")]
        public int BatchSize { get; set; } = 20;

        [Display(Name = "Cooling Pause (seconds after each batch)")]
        [Range(30, 600, ErrorMessage = "Cooling pause must be 30–600 seconds.")]
        public int CoolingPauseSeconds { get; set; } = 120;

        // ── Anti-spam: message spinning ─────────────────────────────────────
        [Display(Name = "Enable Message Spinning")]
        public bool EnableSpinning { get; set; } = true;

        // ── Anti-spam: personalization ──────────────────────────────────────
        // Numbers can be supplied as "Name|Number" pairs to enable {name} token
        // e.g.  Ahmed|201001234567

        // ── Anti-spam: daily send limit ─────────────────────────────────────
        [Display(Name = "Daily Send Limit")]
        [Range(1, 500, ErrorMessage = "Daily limit must be 1–500.")]
        public int DailyLimit { get; set; } = 150;

        [Display(Name = "Attachment (optional)")]
        public IFormFile? Attachment { get; set; }
    }

    public class SendResult
    {
        public string PhoneNumber  { get; set; } = string.Empty;
        public string DisplayName  { get; set; } = string.Empty;   // for personalization
        public bool   Success      { get; set; }
        public string StatusMessage{ get; set; } = string.Empty;
        public DateTime SentAt     { get; set; }
        public string FinalMessage { get; set; } = string.Empty;   // after spinning/tokens
        public int    ActualDelay  { get; set; }                    // ms actually waited
        public bool   WasBatchPause{ get; set; }
    }

    public class BulkSendResult
    {
        public List<SendResult> Results       { get; set; } = new();
        public int TotalSent    => Results.Count(r => r.Success);
        public int TotalFailed  => Results.Count(r => !r.Success);
        public int TotalNumbers => Results.Count;
        public string SummaryMessage =>
            $"{TotalSent} sent successfully, {TotalFailed} failed out of {TotalNumbers} numbers.";
    }


    // ── Daily quota tracking (in-memory, reset at midnight) ─────────────────
    public class DailyQuota
    {
        public int       SentToday  { get; set; }
        public DateTime  Date       { get; set; } = DateTime.Today;

        public void IncrementAndRoll()
        {
            if (Date != DateTime.Today) { SentToday = 0; Date = DateTime.Today; }
            SentToday++;
        }

        public bool IsExceeded(int limit) =>
            Date == DateTime.Today && SentToday >= limit;
    }
}
