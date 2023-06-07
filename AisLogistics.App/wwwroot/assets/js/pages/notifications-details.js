
$(document).ready(function () {
    let hubConnectionAlert = new signalR.HubConnectionBuilder().withUrl("/NotificationHub").build();
    hubConnectionAlert.on("Alert", function (message) {
        NotificationCount($('#notificationCount'));
    });

    hubConnectionAlert.start().catch(function (err) {
        return console.error(err.toString());
    });
});