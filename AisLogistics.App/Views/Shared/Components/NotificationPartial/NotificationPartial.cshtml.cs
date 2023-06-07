using AisLogistics.App.Service;
using AisLogistics.App.Utils;
using Microsoft.AspNetCore.Authorization;

namespace AisLogistics.App.Views.Shared.Components.NotificationPartial
{
    [Authorize]
    public class NotificationPartial : ViewComponent
    {
        private readonly NotificationManager _notificationManager;
        
        public NotificationPartial(NotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
        }

        public async Task<IViewComponentResult?> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated == false) return View("NotificationPartial");
            var notification = await _notificationManager.GetNotificationAsync();
            return View("NotificationPartial", notification);
        }
    }
}
