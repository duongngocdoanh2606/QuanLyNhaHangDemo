using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuanLyNhaHangDemo.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class CleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public CleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUserModel>>();

             
                var cutoffTime = DateTime.Now.AddMinutes(-3);

                
                var usersToDelete = userManager.Users
                    .Where(u => !u.PhoneNumberConfirmed && u.CreatedAt < cutoffTime)
                    .ToList();

                if (usersToDelete.Any())
                {
                    foreach (var user in usersToDelete)
                    {
                        
                        await userManager.DeleteAsync(user);
                    }
                }
            }

            // Nghỉ 1 tiếng trước khi bắt đầu lượt quét tiếp theo
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}