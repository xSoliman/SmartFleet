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
            
            // Toggle dropdown visibility
            const isVisible = dropdownMenu.style.display === 'block';
            if (isVisible) {
                dropdownMenu.style.display = 'none';
            } else {
                dropdownMenu.style.display = 'block';
            }
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!notificationDropdown.contains(e.target) && !dropdownMenu.contains(e.target)) {
                dropdownMenu.style.display = 'none';
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
        } else {
            notificationCount.style.display = 'none';
            notificationCount.classList.add('no-unread');
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
            }
        }
    } catch (error) {
        console.error('Error marking notification as read:', error);
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
            const markAllButton = document.querySelector('button[onclick="markAllAsRead()"]');
            if (markAllButton) {
                markAllButton.style.display = 'none';
            }
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

// Make functions globally available
window.markAsRead = markAsRead;
window.markAllAsRead = markAllAsRead;