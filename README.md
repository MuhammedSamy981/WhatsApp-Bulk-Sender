# WhatsApp Bulk Sender — ASP.NET 8 MVC

A production-ready ASP.NET 8 MVC application for sending WhatsApp messages to groups of phone numbers using the **WhatsApp API** [Ultramsg WhatsApp API](https://blog.ultramsg.com/send-whatsapp-message-by-whatsapp-api-c-sharp).

---

## Features

- ✅ Send text messages to unlimited numbers
- ✅ Attach images, PDFs, documents, videos
- ✅ Configurable delay between messages (avoid spam detection)
- ✅ Real-time live progress via SignalR WebSockets
- ✅ Per-number success/failure reporting
- ✅ Export results to CSV
- ✅ Phone number deduplication & normalization

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A smartphone with WhatsApp installed
- Windows, Linux, or macOS
- Create an account on [Ultramsg](https://user.ultramsg.com/signup.php)
---

## Quick Start

```bash
# Clone / download the project, then:
cd WhatsAppBulkSender

# Restore packages
dotnet restore

# Run
dotnet run
```

Open your browser at **http://localhost:5000**

---

## How to Use

1. **Enter phone numbers** (with country code):
   ```
   201012345678
   447911123456
   9715012345678
   ```
4. **Type your message**
5. Optionally attach a file (image, PDF, etc.)
6. Set the delay between messages (default: 3 seconds)
7. **Click "Send to All Numbers"**
8. Watch live progress — then export results as CSV

---

## Phone Number Format

Include the **country code without the `+`** sign:

| Country | Format | Example |
|---------|--------|---------|
| Egypt | 20XXXXXXXXXX | 201001234567 |
| UAE | 971XXXXXXXXX | 971501234567 |
| UK | 44XXXXXXXXXX | 447911123456 |
| US | 1XXXXXXXXXX | 12025551234 |

---

## Architecture

```
WhatsAppBulkSender/
├── Controllers/
│   └── HomeController.cs       # MVC Controller
├── Models/
│   └── MessageModels.cs        # Request/Response models
├── Services/
│   ├── WhatsAppService.cs      # WhatsappWeb.Net wrapper (Singleton)
│   ├── AntiSpamEngine.cs       # AntiSpam for perveting block phone number
│   └── WhatsAppHub.cs          # SignalR Hub for real-time updates
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml        # Main send page
│   │   └── Result.cshtml       # Results page
│   └── Shared/
│       └── _Layout.cshtml      # Layout
├── wwwroot/
│   ├── css/site.css            # Custom dark UI
│   └── js/site.js
└── Program.cs                  # App startup & DI
```

---

## Library Used (Optional)

**RestSharp** — free, open-source NuGet package  
- NuGet: `dotnet add package RestSharp -v 108.0.1`

---


## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET 8 MVC |
| WhatsApp | Ultramsg API (free) |
| Real-time | SignalR |
| Frontend | Razor Views + Vanilla JS |
| Styling | Custom CSS (dark industrial theme) |
| JSON | Newtonsoft.Json |
