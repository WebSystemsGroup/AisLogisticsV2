using AisLogistics.App.Models;
using Microsoft.AspNetCore.Authorization;
using SmartBreadcrumbs.Attributes;
using System.Diagnostics;
using AisLogistics.App.Service;
using Sentry;

namespace AisLogistics.App.Controllers
{
    [Authorize]
    public class NotificationController : AbstractController
    {
        private readonly NotificationManager _notificationManager;
        private readonly EmployeeManager _employeeManager;
        public NotificationController(NotificationManager notificationManager, EmployeeManager employeeManager)
        {
            _notificationManager = notificationManager;
            _employeeManager = employeeManager;
        }

        public async Task Read(Guid? id)
        {
           await _notificationManager.ReadNotificationAsync(id);
        }

        public async Task Remove(Guid id)
        {
            await _notificationManager.RemoveNotificationAsync(id);
        }

        public async Task<bool> RemoveAll()
        {
            var employeeId = await _employeeManager.GetIdAsync();
            return await _notificationManager.RemoveAllEmployeeNotificationAsync((Guid)employeeId);
        }
    }
}