// Connect to the SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

// Start the connection
connection.start()
    .then(() => console.log('SignalR Connected'))
    .catch(err => console.error('SignalR Connection Error: ', err));

// Handle incoming notifications
connection.on("ReceiveNotification", (notification) => {
    // Update notification count
    const notificationCount = document.getElementById('notificationCount');
    if (notificationCount) {
        const currentCount = parseInt(notificationCount.textContent);
        notificationCount.textContent = currentCount + 1;
    }

    // Add new notification to the list
    const notificationList = document.getElementById('notificationList');
    if (notificationList) {
        const notificationItem = document.createElement('div');
        notificationItem.className = 'notification-item';
        notificationItem.innerHTML = `
            <h5>${notification.title}</h5>
            <p>${notification.message}</p>
            <small>${new Date(notification.createdAt).toLocaleString()}</small>
        `;
        notificationList.insertBefore(notificationItem, notificationList.firstChild);
    }

    // Show notification toast
    showNotificationToast(notification);
});

// Helper function to show notification toast
function showNotificationToast(notification) {
    const toast = document.createElement('div');
    toast.className = 'notification-toast';
    toast.innerHTML = `
        <div class="toast-header">
            <strong class="mr-auto">${notification.title}</strong>
            <small>${new Date(notification.createdAt).toLocaleString()}</small>
            <button type="button" class="ml-2 mb-1 close" onclick="this.parentElement.parentElement.remove()">
                <span>&times;</span>
            </button>
        </div>
        <div class="toast-body">
            ${notification.message}
        </div>
    `;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 5000); // Remove toast after 5 seconds
}