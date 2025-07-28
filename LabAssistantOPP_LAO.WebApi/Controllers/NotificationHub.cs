using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class NotificationHub : Hub
{
    private readonly IHubContext<NotificationHub> _hubContext;

    // Có thể thêm các hàm xử lý nếu muốn
    public async Task SendNotification(string userId, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", message);
    }
}
