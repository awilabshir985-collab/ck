// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Refresh the notification bell badge every 30 seconds.
(function () {
    var badge = document.querySelector('[data-notif-count]');
    if (!badge) return;

    function refresh() {
        fetch('/Notification/UnreadCount', { headers: { 'Accept': 'application/json' } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (data) {
                if (!data) return;
                badge.textContent = data.count > 99 ? '99+' : data.count;
                badge.classList.toggle('d-none', data.count === 0);
            })
            .catch(function () { });
    }

    setInterval(refresh, 30000);
})();
