using System.Text.RegularExpressions;

namespace WhatsAppBulkSender.Services
{
    /// <summary>
    /// Legitimate deliverability engine:
    ///  1. Message spinning  — unique variation per recipient so identical copies
    ///     don't trigger WhatsApp's duplicate-content detector.
    ///  2. Personalization   — {name}/{number} tokens replaced per recipient.
    ///  3. Smart random delay— human-like jitter instead of a robotic fixed pause.
    ///  4. Batch cooling     — longer pause every N messages to mimic natural usage.
    ///  5. Daily quota guard — hard cap to stay well below WhatsApp's limits.
    /// </summary>
    public static class AntiSpamEngine
    {
        private static readonly Random _rng = new();

        // ── 1 + 2. Spin & personalise ────────────────────────────────────────
        // Syntax: {Hello|Hi|Hey} {name}, your code is {number}.
        // {name}   → replaced with recipient name (or first 4 digits of number)
        // {number} → replaced with phone number
        public static string PrepareMessage(string template, string phone, string displayName)
        {
            // Resolve personalization first
            var name = string.IsNullOrWhiteSpace(displayName) ? MaskNumber(phone) : displayName;
            var msg  = template
                .Replace("{name}",   name,  StringComparison.OrdinalIgnoreCase)
                .Replace("{number}", phone, StringComparison.OrdinalIgnoreCase);

            // Spin: pick one option from {A|B|C} groups
            msg = Regex.Replace(msg, @"\{([^{}]+)\}", m =>
            {
                var options = m.Groups[1].Value.Split('|');
                return options[_rng.Next(options.Length)].Trim();
            });

            // Append a zero-width space variant so each message has a unique
            // byte-sequence even if spinning produced the same visible text.
            // WhatsApp's duplicate detector works on content hash, not display.
            msg += GetInvisibleSuffix();

            return msg;
        }

        // ── 3. Human-like random delay ───────────────────────────────────────
        public static int NextDelayMs(int minSec, int maxSec)
        {
            if (minSec > maxSec) (minSec, maxSec) = (maxSec, minSec);
            // Base random in range
            var baseMs = _rng.Next(minSec * 1000, maxSec * 1000 + 1);
            // Add small Gaussian-ish jitter (±10 %) to break exact multiples
            var jitter  = (int)(baseMs * 0.10 * (_rng.NextDouble() * 2 - 1));
            return Math.Max(3000, baseMs + jitter);   // never below 3 s
        }

        // ── 4. Batch cooling ─────────────────────────────────────────────────
        public static bool IsBatchBoundary(int sentSoFar, int batchSize) =>
            sentSoFar > 0 && sentSoFar % batchSize == 0;

        // ── 5. Daily quota ───────────────────────────────────────────────────
        public static (bool allowed, string reason) CheckQuota(
            Models.DailyQuota quota, int dailyLimit, int remaining)
        {
            if (quota.IsExceeded(dailyLimit))
                return (false, $"Daily limit of {dailyLimit} messages reached. Resets at midnight.");

            if (remaining + quota.SentToday > dailyLimit)
                return (false,
                    $"Sending {remaining} more would exceed today's limit of {dailyLimit} " +
                    $"(already sent {quota.SentToday}). Reduce recipient count or raise the limit.");

            return (true, string.Empty);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static string MaskNumber(string phone) =>
            phone.Length >= 4 ? phone[..4] + "***" : phone;

        // Cycles through a set of invisible Unicode characters.
        // Each unique character makes the message byte-distinct without
        // changing what the recipient sees on screen.
        private static readonly string[] _invisibles =
        [
            "\u200B", // ZERO WIDTH SPACE
            "\u200C", // ZERO WIDTH NON-JOINER
            "\u200D", // ZERO WIDTH JOINER
            "\uFEFF", // ZERO WIDTH NO-BREAK SPACE
            "\u2060", // WORD JOINER
        ];
        private static int _invisibleIdx;
        private static string GetInvisibleSuffix()
        {
            var ch = _invisibles[_invisibleIdx % _invisibles.Length];
            _invisibleIdx++;
            return ch;
        }
    }
}
