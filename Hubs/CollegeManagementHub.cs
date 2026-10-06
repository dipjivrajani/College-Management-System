using Microsoft.AspNetCore.SignalR;

namespace CollegeManagementSystem.Hubs;

public class CollegeManagementHub : Hub
{
    // Real-time notification hub for multi-user, multi-role synchronization
    public async Task JoinStudentGroup(string studentId)
    {
        if (!string.IsNullOrEmpty(studentId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Student_{studentId}");
        }
    }

    public async Task JoinRoleGroup(string role)
    {
        if (!string.IsNullOrEmpty(role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
        }
    }
}
