using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace NewGradingTest.grading_system.backend.Workers
{
	public class SubmissionHub : Hub
	{
		public async Task JoinSubmissionGroup(int submissionId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"submission_{submissionId}");
		}

		public async Task LeaveSubmissionGroup(int submissionId)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"submission_{submissionId}");
		}

		public override async Task OnConnectedAsync()
		{
			Console.WriteLine($"Client connected: {Context.ConnectionId}");
			await base.OnConnectedAsync();
		}

	}
}
