// Connect to the SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/Notify")
    .withAutomaticReconnect()
    .build();

// Start the connection
connection.start()
    .then(() => console.log('SignalR Connected'))
    .catch(err => console.error('SignalR Connection Error: ', err));

// Add click event listener for notification dropdown toggle
document.addEventListener('DOMContentLoaded', function() {
    const notificationDropdown = document.getElementById('notificationDropdown');
    const dropdownMenu = document.getElementById('notificationDropdownMenu');
    
    if (notificationDropdown && dropdownMenu) {
        notificationDropdown.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            // Toggle dropdown visibility using CSS class
            dropdownMenu.classList.toggle('show');
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!notificationDropdown.contains(e.target) && !dropdownMenu.contains(e.target)) {
                dropdownMenu.classList.remove('show');
            }
        });
        
        // Prevent dropdown from closing when clicking inside it
        dropdownMenu.addEventListener('click', function(e) {
            e.stopPropagation();
        });
    }
});

// Handle incoming notifications
connection.on("ReceiveNotification", (notification) => {
    console.log('Received notification:', notification);
    
    // Update notification count
    updateNotificationCount(1);
    
    // Add new notification to the list
    addNotificationToList(notification);
    
    // Show notification toast
    showNotificationToast(notification);
});

// Handle notification marked as read
connection.on("NotificationMarkedAsRead", (data) => {
    console.log('Notification marked as read:', data);
    
    // Update the notification item in both dropdown and main list
    const notificationItem = document.querySelector(`[data-notification-id="${data.id}"]`);
    if (notificationItem && notificationItem.classList.contains('unread')) {
        notificationItem.classList.remove('unread');
        const badge = notificationItem.querySelector('.badge');
        if (badge) badge.remove();
        const title = notificationItem.querySelector('h6');
        if (title) title.classList.remove('fw-bold');
        
        // Update count
        updateNotificationCount(-1);
    }
});

// Handle all notifications marked as read
connection.on("AllNotificationsMarkedAsRead", () => {
    console.log('All notifications marked as read');
    
    // Update all notification items
    const unreadItems = document.querySelectorAll('.notification-item.unread');
    unreadItems.forEach(item => {
        item.classList.remove('unread');
        const badge = item.querySelector('.badge');
        if (badge) badge.remove();
        const title = item.querySelector('h6');
        if (title) title.classList.remove('fw-bold');
    });
    
    // Update notification count badge
    const notificationCount = document.getElementById('notificationCount');
    const notificationBell = document.querySelector('.notification-bell');
    
    if (notificationCount) {
        notificationCount.textContent = '0';
        notificationCount.style.display = 'none';
        notificationCount.classList.add('no-unread');
    }
    
    // Update bell color
    if (notificationBell) {
        notificationBell.classList.remove('has-unread');
        notificationBell.classList.add('no-unread');
    }
    
    // Hide "Mark all as read" button
    hideMarkAllAsReadButton();
});

// Function to update notification count
function updateNotificationCount(increment = 0) {
    const notificationCount = document.getElementById('notificationCount');
    const notificationBell = document.querySelector('.notification-bell');
    
    if (notificationCount) {
        const currentCount = parseInt(notificationCount.textContent || '0');
        const newCount = Math.max(0, currentCount + increment);
        
        notificationCount.textContent = newCount;
        
        // Show/hide badge based on count
        if (newCount > 0) {
            notificationCount.style.display = 'flex';
            notificationCount.classList.remove('no-unread');
            // Show "Mark all as read" button when there are unread notifications
            showMarkAllAsReadButton();
        } else {
            notificationCount.style.display = 'none';
            notificationCount.classList.add('no-unread');
            // Hide "Mark all as read" button when there are no unread notifications
            hideMarkAllAsReadButton();
        }
        
        // Update bell color
        if (notificationBell) {
            if (newCount > 0) {
                notificationBell.classList.remove('no-unread');
                notificationBell.classList.add('has-unread');
            } else {
                notificationBell.classList.remove('has-unread');
                notificationBell.classList.add('no-unread');
            }
        }
    }
}

// Function to add notification to the list
function addNotificationToList(notification) {
    const notificationList = document.getElementById('notificationList');
    if (notificationList) {
        const notificationItem = document.createElement('div');
        notificationItem.className = 'dropdown-item notification-item unread';
        notificationItem.setAttribute('data-notification-id', notification.id);
        notificationItem.onclick = () => markAsRead(notification.id);
        
        const createdAt = new Date(notification.createdAt).toLocaleString();
        
        notificationItem.innerHTML = `
            <div class="notification-content">
                <div class="notification-header">
                    <h6 class="notification-title fw-bold">${notification.title}</h6>
                    <span class="badge notification-new-badge">New</span>
                </div>
                <p class="notification-message">${notification.message}</p>
                <small class="notification-time">${createdAt}</small>
            </div>
        `;
        
        // Insert at the top of the list
        if (notificationList.firstChild) {
            notificationList.insertBefore(notificationItem, notificationList.firstChild);
        } else {
            notificationList.appendChild(notificationItem);
        }
        
        // Remove "No notifications" message if it exists
        const noNotifications = notificationList.querySelector('.text-center.text-muted');
        if (noNotifications) {
            noNotifications.remove();
        }
        
        // Show "Mark all as read" button if it was hidden
        showMarkAllAsReadButton();
    }
}

// Function to mark notification as read
async function markAsRead(notificationId) {
    try {
        const response = await fetch('/Notifications/MarkAsRead', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ id: notificationId })
        });
        
        if (response.ok) {
            // Update the notification item
            const notificationItem = document.querySelector(`[data-notification-id="${notificationId}"]`);
            if (notificationItem && notificationItem.classList.contains('unread')) {
                notificationItem.classList.remove('unread');
                const badge = notificationItem.querySelector('.badge');
                if (badge) badge.remove();
                const title = notificationItem.querySelector('h6');
                if (title) title.classList.remove('fw-bold');
                
                // Update count only if the notification was unread
                updateNotificationCount(-1);
                
                // Update the notification list in the dropdown to reflect the change
                updateNotificationListAfterMarkAsRead(notificationId);
            }
        }
    } catch (error) {
        console.error('Error marking notification as read:', error);
    }
}

// Function to update notification list after marking as read
function updateNotificationListAfterMarkAsRead(notificationId) {
    // Find the notification item in the dropdown
    const dropdownNotificationItem = document.querySelector(`#notificationDropdownMenu [data-notification-id="${notificationId}"]`);
    if (dropdownNotificationItem && dropdownNotificationItem.classList.contains('unread')) {
        dropdownNotificationItem.classList.remove('unread');
        const badge = dropdownNotificationItem.querySelector('.badge');
        if (badge) badge.remove();
        const title = dropdownNotificationItem.querySelector('h6');
        if (title) title.classList.remove('fw-bold');
    }
}

// Function to mark all notifications as read
async function markAllAsRead() {
    try {
        const response = await fetch('/Notifications/MarkAllAsRead', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        });
        
        if (response.ok) {
            // Count unread notifications before updating
            const unreadItems = document.querySelectorAll('.notification-item.unread');
            const unreadCount = unreadItems.length;
            
            // Update all notification items
            unreadItems.forEach(item => {
                item.classList.remove('unread');
                const badge = item.querySelector('.badge');
                if (badge) badge.remove();
                const title = item.querySelector('h6');
                if (title) title.classList.remove('fw-bold');
            });
            
            // Update notification count badge
            const notificationCount = document.getElementById('notificationCount');
            const notificationBell = document.querySelector('.notification-bell');
            
            if (notificationCount) {
                notificationCount.textContent = '0';
                notificationCount.style.display = 'none';
                notificationCount.classList.add('no-unread');
            }
            
            // Update bell color
            if (notificationBell) {
                notificationBell.classList.remove('has-unread');
                notificationBell.classList.add('no-unread');
            }
            
            // Hide "Mark all as read" button
            hideMarkAllAsReadButton();
        }
    } catch (error) {
        console.error('Error marking all notifications as read:', error);
    }
}

// Helper function to show notification toast
function showNotificationToast(notification) {
    const toast = document.createElement('div');
    toast.className = 'notification-toast';
    toast.innerHTML = `
        <div class="toast-header">
            <strong class="me-auto">${notification.title}</strong>
            <small>${new Date(notification.createdAt).toLocaleString()}</small>
            <button type="button" class="btn-close" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
        <div class="toast-body">
            ${notification.message}
        </div>
    `;
    
    document.body.appendChild(toast);
    
    // Remove toast after 5 seconds
    setTimeout(() => {
        if (toast.parentElement) {
            toast.remove();
        }
    }, 5000);
}

// Function to show "Mark all as read" button
function showMarkAllAsReadButton() {
    const markAllButton = document.querySelector('button[onclick="markAllAsRead()"]');
    if (markAllButton) {
        markAllButton.style.display = 'block';
        markAllButton.style.visibility = 'visible';
    }
}

// Function to hide "Mark all as read" button
function hideMarkAllAsReadButton() {
    const markAllButton = document.querySelector('button[onclick="markAllAsRead()"]');
    if (markAllButton) {
        markAllButton.style.display = 'none';
        markAllButton.style.visibility = 'hidden';
    }
}

// Make functions globally available
window.markAsRead = markAsRead;
window.markAllAsRead = markAllAsRead;