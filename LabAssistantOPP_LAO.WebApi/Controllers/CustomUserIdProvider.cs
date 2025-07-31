using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // 👉 Trả về Id người dùng để dùng Clients.User("userId")
        return connection.User?.FindFirst("userId")?.Value;
    }
}
