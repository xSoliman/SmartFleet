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
    const notificationItems = document.querySelectorAll(`[data-notification-id="${data.id}"]`);
    let wasUnread = false;
    
    notificationItems.forEach(notificationItem => {
        if (notificationItem && notificationItem.classList.contains('unread')) {
            wasUnread = true;
            notificationItem.classList.remove('unread');
            const badge = notificationItem.querySelector('.badge');
            if (badge) badge.remove();
            const title = notificationItem.querySelector('h6');
            if (title) title.classList.remove('fw-bold');
            
            // Remove the "New" badge if it exists
            const newBadge = notificationItem.querySelector('.notification-new-badge');
            if (newBadge) newBadge.remove();
            
            // Remove the unread-badge if it exists
            const unreadBadge = notificationItem.querySelector('.unread-badge');
            if (unreadBadge) unreadBadge.remove();
            
            // Remove the mark-read button if it exists
            const markReadBtn = notificationItem.querySelector('.action-btn.mark-read');
            if (markReadBtn) markReadBtn.remove();
        }
    });
    
    // Update count only if the notification was actually unread
    if (wasUnread) {
        updateNotificationCount(-1);
    }
});

// Handle all notifications marked as read
connection.on("AllNotificationsMarkedAsRead", () => {
    console.log('All notifications marked as read');
    
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
        
        // Remove the "New" badge if it exists
        const newBadge = item.querySelector('.notification-new-badge');
        if (newBadge) newBadge.remove();
        
        // Remove the unread-badge if it exists
        const unreadBadge = item.querySelector('.unread-badge');
        if (unreadBadge) unreadBadge.remove();
        
        // Remove the mark-read button if it exists
        const markReadBtn = item.querySelector('.action-btn.mark-read');
        if (markReadBtn) markReadBtn.remove();
    });
    
    // Update notification count using the proper function
    if (unreadCount > 0) {
        updateNotificationCount(-unreadCount);
    }
});

// Global flag to prevent double updates
let isUpdatingCounter = false;

// Function to update notification count
function updateNotificationCount(increment = 0) {
    if (isUpdatingCounter) {
        console.log('Counter update already in progress, skipping...');
        return;
    }
    
    isUpdatingCounter = true;
    
    const notificationCount = document.getElementById('notificationCount');
    const notificationBell = document.querySelector('.notification-bell');
    
    if (notificationCount) {
        const currentCount = parseInt(notificationCount.textContent || '0');
        const newCount = Math.max(0, currentCount + increment);
        
        console.log(`Updating notification count: ${currentCount} + ${increment} = ${newCount}`);
        
        notificationCount.textContent = newCount;
        
        // Show/hide badge based on count
        if (newCount > 0) {
            notificationCount.style.setProperty('display', 'flex', 'important');
            notificationCount.classList.remove('no-unread');
            // Show "Mark all as read" button when there are unread notifications
            showMarkAllAsReadButton();
        } else {
            notificationCount.style.setProperty('display', 'none', 'important');
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
        
        // Force a reflow to ensure the display change takes effect
        notificationCount.offsetHeight;
        
        // Double-check the display state after a brief delay
        setTimeout(() => {
            if (newCount === 0 && notificationCount.style.display !== 'none') {
                console.log('Forcing notification count to hide');
                notificationCount.style.setProperty('display', 'none', 'important');
            } else if (newCount > 0 && notificationCount.style.display !== 'flex') {
                console.log('Forcing notification count to show');
                notificationCount.style.setProperty('display', 'flex', 'important');
            }
            isUpdatingCounter = false;
        }, 50);
    } else {
        isUpdatingCounter = false;
    }
}

// Function to add notification to the list
function addNotificationToList(notification) {
    const notificationList = document.getElementById('notificationList');
    if (notificationList) {
        const notificationItem = document.createElement('div');
        notificationItem.className = `dropdown-item notification-item unread type-${notification.type || 'info'}`;
        notificationItem.setAttribute('data-notification-id', notification.id);
        notificationItem.onclick = () => markAsRead(notification.id);
        
        const createdAt = new Date(notification.createdAt).toLocaleString();
        
        notificationItem.innerHTML = `
            <div class="notification-content">
                <div class="notification-header">
                    <h6 class="notification-title fw-bold">${notification.title}</h6>
                    <span class="badge notification-new-badge type-${notification.type || 'info'}">New</span>
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
        // Check if the notification is currently unread before making the request
        const notificationItems = document.querySelectorAll(`[data-notification-id="${notificationId}"]`);
        const wasUnread = Array.from(notificationItems).some(item => item.classList.contains('unread'));
        
        console.log(`Marking notification ${notificationId} as read. Was unread: ${wasUnread}`);
        
        const response = await fetch('/Notifications/MarkAsRead', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ id: notificationId })
        });
        
        if (response.ok) {
            // Update the notification item in both dropdown and main list
            notificationItems.forEach(notificationItem => {
                if (notificationItem && notificationItem.classList.contains('unread')) {
                    notificationItem.classList.remove('unread');
                    const badge = notificationItem.querySelector('.badge');
                    if (badge) badge.remove();
                    const title = notificationItem.querySelector('h6');
                    if (title) title.classList.remove('fw-bold');
                    
                    // Remove the "New" badge if it exists
                    const newBadge = notificationItem.querySelector('.notification-new-badge');
                    if (newBadge) newBadge.remove();
                    
                    // Remove the unread-badge if it exists
                    const unreadBadge = notificationItem.querySelector('.unread-badge');
                    if (unreadBadge) unreadBadge.remove();
                    
                    // Remove the mark-read button if it exists
                    const markReadBtn = notificationItem.querySelector('.action-btn.mark-read');
                    if (markReadBtn) markReadBtn.remove();
                }
            });
            
            // Small delay to ensure DOM updates are processed before updating counter
            setTimeout(() => {
                // Update count only if the notification was actually unread
                if (wasUnread) {
                    updateNotificationCount(-1);
                }
            }, 10);
        } else {
            console.error('Failed to mark notification as read:', response.statusText);
        }
    } catch (error) {
        console.error('Error marking notification as read:', error);
    }
}

// Function to mark all notifications as read
async function markAllAsRead() {
    try {
        // Count unread notifications before updating
        const unreadItems = document.querySelectorAll('.notification-item.unread');
        const unreadCount = unreadItems.length;
        
        const response = await fetch('/Notifications/MarkAllAsRead', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        });
        
        if (response.ok) {
            // Update all notification items
            unreadItems.forEach(item => {
                item.classList.remove('unread');
                const badge = item.querySelector('.badge');
                if (badge) badge.remove();
                const title = item.querySelector('h6');
                if (title) title.classList.remove('fw-bold');
                
                // Remove the "New" badge if it exists
                const newBadge = item.querySelector('.notification-new-badge');
                if (newBadge) newBadge.remove();
                
                // Remove the unread-badge if it exists
                const unreadBadge = item.querySelector('.unread-badge');
                if (unreadBadge) unreadBadge.remove();
                
                // Remove the mark-read button if it exists
                const markReadBtn = item.querySelector('.action-btn.mark-read');
                if (markReadBtn) markReadBtn.remove();
            });
            
            // Update notification count badge by decrementing the actual unread count
            if (unreadCount > 0) {
                updateNotificationCount(-unreadCount);
            }
        }
    } catch (error) {
        console.error('Error marking all notifications as read:', error);
    }
}

// Helper function to show notification toast
function showNotificationToast(notification) {
    const toast = document.createElement('div');
    toast.className = `notification-toast type-${notification.type || 'info'}`;
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
    // Use a more specific selector to find existing button
    let markAllButton = document.querySelector('#markAllAsReadButton');
    
    // If button doesn't exist, create it
    if (!markAllButton) {
        const headerSection = document.querySelector('.notification-header-section .d-flex');
        if (headerSection) {
            // Create the button
            markAllButton = document.createElement('button');
            markAllButton.id = 'markAllAsReadButton';
            markAllButton.type = 'button';
            markAllButton.className = 'btn btn-sm btn-outline-primary';
            markAllButton.onclick = markAllAsRead;
            markAllButton.textContent = 'Mark all as read';
            
            // Insert the button at the end of the flex container
            headerSection.appendChild(markAllButton);
        }
    }
    
    // Show the button if it exists
    if (markAllButton) {
        markAllButton.style.display = 'block';
        markAllButton.style.visibility = 'visible';
    }
}

// Function to hide "Mark all as read" button
function hideMarkAllAsReadButton() {
    const markAllButton = document.querySelector('#markAllAsReadButton');
    if (markAllButton) {
        // Remove the button entirely instead of just hiding it
        markAllButton.remove();
    }
}

// Make functions globally available
window.markAsRead = markAsRead;
window.markAllAsRead = markAllAsRead;