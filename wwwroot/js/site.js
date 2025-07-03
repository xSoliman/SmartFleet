// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// SignalR Connection for Real-time Updates
let connection = null;

// Initialize SignalR connection
function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/Notify")
        .withAutomaticReconnect()
        .build();

    // Handle driver state updates
    connection.on("ReceiveDriverStateUpdate", function (driverData) {
        console.log("Driver state update received:", driverData);
        
        // Show notification to user
        showDriverStateNotification(driverData);
        
        // Update driver status in UI if on driver dashboard
        updateDriverStatusInUI(driverData);
        
        // Update driver list if on drivers index page
        updateDriverListInUI(driverData);
    });

    // Handle general notifications
    connection.on("ReceiveNotification", function (notification) {
        console.log("Notification received:", notification);
        showNotification(notification);
    });

    // Start connection
    connection.start()
        .then(function () {
            console.log("SignalR Connected for real-time updates");
        })
        .catch(function (err) {
            console.error("SignalR Connection Error: ", err);
        });
}

// Show driver state change notification
function showDriverStateNotification(driverData) {
    const notificationDiv = document.createElement('div');
    notificationDiv.className = 'alert alert-info alert-dismissible fade show position-fixed';
    notificationDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    
    const icon = driverData.TripAssignment ? '🚗' : driverData.TripCompletion ? '✅' : '🔄';
    
    notificationDiv.innerHTML = `
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        <strong>${icon} Driver Status Update</strong><br>
        <strong>${driverData.DriverName}</strong><br>
        Status: <span class="badge bg-primary">${driverData.OldStatus}</span> → 
        <span class="badge bg-success">${driverData.NewStatus}</span><br>
        <small class="text-muted">${new Date(driverData.Timestamp).toLocaleTimeString()}</small>
    `;
    
    document.body.appendChild(notificationDiv);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notificationDiv.parentNode) {
            notificationDiv.remove();
        }
    }, 5000);
}

// Update driver status in UI (for driver dashboard)
function updateDriverStatusInUI(driverData) {
    const currentUserId = document.querySelector('[data-user-id]')?.getAttribute('data-user-id');
    
    if (currentUserId === driverData.DriverId) {
        // Update driver status display
        const statusElement = document.querySelector('.driver-status-display');
        if (statusElement) {
            statusElement.textContent = driverData.NewStatus;
            statusElement.className = `driver-status-display badge bg-${getStatusColor(driverData.NewStatus)}`;
        }
        
        // Update status in any forms
        const statusSelect = document.querySelector('select[name="DriverStatus"]');
        if (statusSelect) {
            statusSelect.value = driverData.NewStatus;
        }
    }
}

// Update driver list in UI (for drivers index page)
function updateDriverListInUI(driverData) {
    const driverRow = document.querySelector(`[data-driver-id="${driverData.DriverId}"]`);
    if (driverRow) {
        const statusCell = driverRow.querySelector('.driver-status-cell');
        if (statusCell) {
            statusCell.innerHTML = `<span class="badge bg-${getStatusColor(driverData.NewStatus)}">${driverData.NewStatus}</span>`;
        }
    }
}

// Get color class for status badge
function getStatusColor(status) {
    switch (status) {
        case 'Available':
            return 'success';
        case 'OnTrip':
            return 'warning';
        case 'AssignedOnScheduledTrip':
            return 'info';
        case 'NotAvailable':
            return 'danger';
        default:
            return 'secondary';
    }
}

// Show general notification
function showNotification(notification) {
    // Determine notification type based on title
    function getNotificationType(title) {
        if (!title) return 'info';
        
        title = title.toLowerCase();
        
        // Geofence breach - red
        if (title.includes('geofence breach') || title.includes('unauthorized vehicle use'))
            return 'danger';
        
        // Completed or accepted - green
        if (title.includes('completed') || title.includes('approved') || title.includes('started'))
            return 'success';
        
        // Rejected - red/orange
        if (title.includes('rejected') || title.includes('cancelled') || title.includes('failed'))
            return 'warning';
        
        // Default - informative
        return 'info';
    }
    
    const notificationType = getNotificationType(notification.title || notification);
    const notificationDiv = document.createElement('div');
    notificationDiv.className = `alert alert-${notificationType} alert-dismissible fade show position-fixed`;
    notificationDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    
    notificationDiv.innerHTML = `
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        <strong>${notification.title || 'Notification'}</strong><br>
        ${notification.message || notification}
    `;
    
    document.body.appendChild(notificationDiv);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notificationDiv.parentNode) {
            notificationDiv.remove();
        }
    }, 5000);
}

// Initialize SignalR when page loads
document.addEventListener('DOMContentLoaded', function() {
    // Check if SignalR is available
    if (typeof signalR !== 'undefined') {
        initializeSignalR();
    } else {
        console.warn('SignalR not available - real-time updates disabled');
    }
});

// Reconnect on page visibility change
document.addEventListener('visibilitychange', function() {
    if (!document.hidden && connection && connection.state === signalR.HubConnectionState.Disconnected) {
        connection.start();
    }
});
