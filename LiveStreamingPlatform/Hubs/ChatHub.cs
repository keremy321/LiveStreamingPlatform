using LiveStreamingPlatform.Models;
using LiveStreamingPlatform.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LiveStreamingPlatform.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinChannel(string channelId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel-{channelId}");
        }

        public async Task SendMessage(string channelId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (message.Length > 300)
            {
                message = message.Substring(0, 300);
            }

            var userName = Context.User?.Identity?.Name ?? "Unknown User";

            var chatMessage = new ChatMessage
            {
                ChannelId = int.Parse(channelId),
                UserName = userName,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.Group($"channel-{channelId}")
                .SendAsync("ReceiveMessage", userName, message);
        }
    }
}
