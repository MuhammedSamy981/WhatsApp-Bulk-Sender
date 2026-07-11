using Microsoft.AspNetCore.SignalR;

namespace WhatsAppBulkSender.Services
{
    public class WhatsAppHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}
