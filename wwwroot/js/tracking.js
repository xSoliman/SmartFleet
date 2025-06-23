// Tracking System - Clean Implementation
document.addEventListener("DOMContentLoaded", function () {
    // Initialize map
    const mapContainer = document.getElementById('map');
    if (!mapContainer) {
        console.error('Map container not found');
        return;
    }

    const map = L.map('map', { 
        zoomControl: false,
        attributionControl: false
    }).setView([30.0444, 31.2357], 8);

    // Add map tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
        maxZoom: 18
    }).addTo(map);

    // Add zoom control
    L.control.zoom({
        position: 'topright'
    }).addTo(map);

    // Map resize function
    function resizeMap() {
        setTimeout(() => {
            map.invalidateSize();
        }, 100);
    }

    window.addEventListener('resize', resizeMap);
    resizeMap();

    // Vehicle icons
    const vehicleIcons = {
        car: L.divIcon({
            html: '<i class="fas fa-car" style="color: #3498db; font-size: 20px;"></i>',
            className: 'custom-vehicle-icon',
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        }),
        truck: L.divIcon({
            html: '<i class="fas fa-truck" style="color: #e74c3c; font-size: 20px;"></i>',
            className: 'custom-vehicle-icon',
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        }),
        bus: L.divIcon({
            html: '<i class="fas fa-bus" style="color: #f39c12; font-size: 20px;"></i>',
            className: 'custom-vehicle-icon',
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        }),
        van: L.divIcon({
            html: '<i class="fas fa-shuttle-van" style="color: #9b59b6; font-size: 20px;"></i>',
            className: 'custom-vehicle-icon',
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        }),
        motorcycle: L.divIcon({
            html: '<i class="fas fa-motorcycle" style="color: #1abc9c; font-size: 20px;"></i>',
            className: 'custom-vehicle-icon',
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        }),
        other: L.divIcon({
            html: '<i class="fas fa-truck-moving" style="color: #95a5a6; font-size: 20px;"></i>',
            className: 'custom-vehicle-icon',
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        })
    };

    // Data storage
    const markers = {};
    const paths = {};
    let vehicles = [];
    let showPaths = false;
    let selectedVehicleId = null;

    // SignalR connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/Tracking")
        .withAutomaticReconnect()
        .build();

    connection.start().then(function () {
        console.log("Connected to tracking hub");
    }).catch(function (err) {
        console.error("Error connecting to tracking hub: ", err);
    });

    // Real-time updates
    connection.on("ReceiveVehicleLocationUpdate", function (vehicleData) {
        const vehicleIndex = vehicles.findIndex(v => v.id === vehicleData.vehicleId);
        if (vehicleIndex !== -1) {
            vehicles[vehicleIndex] = {
                ...vehicles[vehicleIndex],
                model: vehicleData.vehicleModel,
                type: vehicleData.vehicleType,
                licensePlate: vehicleData.licensePlate,
                status: vehicleData.status,
                totalDistanceTraveled: vehicleData.totalDistanceTraveled,
                simCardNumber: vehicleData.simCardNumber,
                simCardStatus: vehicleData.simCardStatus,
                latestLocation: vehicleData.latestLocation
            };
        } else {
            vehicles.push({
                id: vehicleData.vehicleId,
                model: vehicleData.vehicleModel,
                type: vehicleData.vehicleType,
                licensePlate: vehicleData.licensePlate,
                status: vehicleData.status,
                totalDistanceTraveled: vehicleData.totalDistanceTraveled,
                simCardNumber: vehicleData.simCardNumber,
                simCardStatus: vehicleData.simCardStatus,
                latestLocation: vehicleData.latestLocation,
                recentLocations: [vehicleData.latestLocation]
            });
        }

        updateVehicleTable(vehicles);
        updateVehicleMarkers(vehicles);

        // Update position info for selected vehicle
        if (selectedVehicleId === vehicleData.vehicleId && vehicleData.latestLocation) {
            const positionData = {
                position: `${vehicleData.latestLocation.latitude}°, ${vehicleData.latestLocation.longitude}°`,
                speed: `${vehicleData.latestLocation.speed} kph`,
                timestamp: new Date(vehicleData.latestLocation.timestamp).toLocaleString()
            };
            updateInfoSection('position-info', positionData);
        }
    });

    // Fetch vehicle data
    function fetchVehicleData() {
        fetch('/api/tracking/vehicles')
            .then(response => response.json())
            .then(data => {
                vehicles = data;
                updateVehicleTable(vehicles);
                updateVehicleMarkers(vehicles);
            })
            .catch(error => console.error('Error fetching vehicle data:', error));
    }

    // Initial data fetch
    fetchVehicleData();

    // Periodic refresh (every 30 seconds as fallback)
    setInterval(fetchVehicleData, 30000);
    setInterval(() => {
        if (connection.state === signalR.HubConnectionState.Disconnected) {
            fetchVehicleData();
        }
    }, 60000);

    // Update vehicle table
    function updateVehicleTable(vehicles) {
        const tableBody = document.querySelector('#vehicleTable tbody');
        if (!tableBody) return;
        
        tableBody.innerHTML = '';

        vehicles.forEach(vehicle => {
            const movementStatus = getMovementStatus(vehicle);
            const tripStatus = getTripStatus(vehicle);
            const hasAlert = checkForAlerts(vehicle);
            
            const row = document.createElement('tr');
            row.onclick = function() { selectVehicle(vehicle.id); };
            
            // Determine speed display
            let speedDisplay = 'N/A';
            if (vehicle.latestLocation && movementStatus !== 'GPS Offline' && movementStatus !== 'No GPS Signal') {
                speedDisplay = `<strong>${vehicle.latestLocation.speed}</strong> kph`;
            }
            
            row.innerHTML = `
                <td>
                    <i class="fas fa-${getVehicleIcon(vehicle.type)} fa-lg"></i>&nbsp;&nbsp;${vehicle.model}
                    ${hasAlert ? '<div class="alert-indicator"></div>' : ''}
                </td>
                <td>${speedDisplay}</td>
                <td><span class="status-badge ${movementStatus.toLowerCase().replace(/ /g, '-')}">${movementStatus}</span></td>
                <td><span class="status-badge ${tripStatus.toLowerCase().replace(/ /g, '-')}">${tripStatus}</span></td>
            `;
            
            tableBody.appendChild(row);
        });
    }

    // Check for alerts
    function checkForAlerts(vehicle) {
        if (!vehicle.latestLocation) return false;
        return vehicle.latestLocation.speed > 120 || 
               (vehicle.latestLocation.speed === 0 && vehicle.status === 1);
    }

    // Update vehicle markers
    function updateVehicleMarkers(vehicles) {
        Object.values(markers).forEach(marker => map.removeLayer(marker));
        Object.values(paths).forEach(path => map.removeLayer(path));
        
        vehicles.forEach(vehicle => {
            // Only show markers for vehicles with valid GPS data
            if (!vehicle.latestLocation) return;
            
            const lat = parseFloat(vehicle.latestLocation.latitude);
            const lon = parseFloat(vehicle.latestLocation.longitude);
            
            if (isNaN(lat) || isNaN(lon)) return;
            
            const icon = vehicleIcons[getVehicleIcon(vehicle.type)] || vehicleIcons.other;
            
            markers[vehicle.id] = L.marker([lat, lon], { icon: icon }).addTo(map)
                .bindPopup(createVehiclePopup(vehicle));

            if (showPaths && vehicle.recentLocations && vehicle.recentLocations.length > 1) {
                const pathCoords = vehicle.recentLocations.map(loc => [parseFloat(loc.latitude), parseFloat(loc.longitude)]);
                paths[vehicle.id] = L.polyline(pathCoords, {
                    color: getVehiclePathColor(vehicle.type),
                    weight: 3,
                    opacity: 0.7,
                    className: 'vehicle-path'
                }).addTo(map);
            }
        });
    }

    // Get vehicle path color
    function getVehiclePathColor(type) {
        const colors = {
            car: '#3498db',
            truck: '#e74c3c',
            bus: '#f39c12',
            van: '#9b59b6',
            motorcycle: '#1abc9c',
            other: '#95a5a6'
        };
        return colors[getVehicleIcon(type)] || colors.other;
    }

    // Create vehicle popup
    function createVehiclePopup(vehicle) {
        const movementStatus = getMovementStatus(vehicle);
        const tripStatus = getTripStatus(vehicle);
        
        let popupContent = `
            <div class="vehicle-popup">
                <h4><i class="fas fa-${getVehicleIcon(vehicle.type)}"></i> ${vehicle.model}</h4>
                <p><strong>Plate:</strong> ${vehicle.licensePlate}</p>
                <p><strong>Movement:</strong> <span class="status-badge ${movementStatus.toLowerCase().replace(/ /g, '-')}">${movementStatus}</span></p>
                <p><strong>Trip Status:</strong> <span class="status-badge ${tripStatus.toLowerCase().replace(/ /g, '-')}">${tripStatus}</span></p>
        `;
        
        if (vehicle.latestLocation) {
            const timestamp = new Date(vehicle.latestLocation.timestamp).toLocaleString();
            popupContent += `
                <p><strong>Speed:</strong> ${vehicle.latestLocation.speed} kph</p>
                <p><strong>Last Update:</strong> ${timestamp}</p>
            `;
        } else {
            popupContent += `
                <p><strong>GPS:</strong> No signal available</p>
            `;
        }
        
        popupContent += `
                <button onclick="selectVehicle(${vehicle.id})" class="btn btn-primary btn-sm">View Details</button>
            </div>
        `;
        
        return popupContent;
    }

    // Helper functions
    function getMovementStatus(vehicle) {
        // Check if vehicle has SimCard
        if (!vehicle.simCardNumber) {
            return "No SimCard";
        }
        
        // Check if SimCard is active (status 1 = Active)
        if (vehicle.simCardStatus !== 1) {
            return "SimCard Inactive";
        }
        
        // Check if vehicle has GPS data
        if (!vehicle.latestLocation) {
            return "No GPS Signal";
        }
        
        // Check if GPS data is recent (within last 5 minutes)
        const lastUpdate = new Date(vehicle.latestLocation.timestamp);
        const now = new Date();
        const timeDiff = (now - lastUpdate) / (1000 * 60); // minutes
        
        if (timeDiff > 5) {
            return "GPS Offline";
        }
        
        // Vehicle has active GPS - determine movement status based on speed
        const speed = vehicle.latestLocation.speed;
        
        if (speed > 0) {
            return "Moving";
        } else {
            return "Stopped";
        }
    }

    function getTripStatus(vehicle) {
        // Check vehicle state for trip status
        switch(vehicle.status) {
            case 0: return "Available";
            case 1: return "In Use";
            case 2: return "Maintenance";
            case 3: return "Maintained";
            case 4: return "On Trip";
            case 5: return "Scheduled Trip";
            default: return "Unknown";
        }
    }

    function getVehicleStatus(vehicle) {
        // This function now combines both statuses for backward compatibility
        const movementStatus = getMovementStatus(vehicle);
        const tripStatus = getTripStatus(vehicle);
        
        // If there are GPS/SimCard issues, prioritize those
        if (movementStatus !== "Moving" && movementStatus !== "Stopped") {
            return movementStatus;
        }
        
        return tripStatus;
    }

    function getVehicleIcon(type) {
        switch(type) {
            case 0: return "car";
            case 1: return "truck";
            case 2: return "bus";
            case 3: return "van";
            case 4: return "motorcycle";
            default: return "other";
        }
    }

    function getVehicleTypeName(type) {
        const types = ["Car", "Truck", "Bus", "Van", "Motorcycle", "Other"];
        return types[type] || "Unknown";
    }

    // Select vehicle
    window.selectVehicle = function (vehicleId) {
        selectedVehicleId = vehicleId;
        const vehicle = vehicles.find(v => v.id === vehicleId);
        if (vehicle) {
            // Center map on vehicle if it has GPS data
            if (vehicle.latestLocation) {
                const lat = parseFloat(vehicle.latestLocation.latitude);
                const lon = parseFloat(vehicle.latestLocation.longitude);
                
                if (!isNaN(lat) && !isNaN(lon)) {
                    map.setView([lat, lon], 14);
                    if (markers[vehicle.id]) {
                        markers[vehicle.id].openPopup();
                    }
                }
            } else {
                // For vehicles without GPS, center on a default location or show a message
                map.setView([30.0444, 31.2357], 8);
                showSimpleNotification(`Vehicle ${vehicle.model} has no GPS signal`);
            }

            // Scroll to info panels
            const infoPanel = document.getElementById('info-panel');
            if (infoPanel) {
                setTimeout(() => {
                    infoPanel.scrollIntoView({ behavior: 'smooth' });
                }, 300);
            }

            loadVehicleDetails(vehicleId);

            // Highlight selected vehicle
            const rows = document.querySelectorAll('#vehicleTable tbody tr');
            rows.forEach(row => {
                row.classList.remove('selected-vehicle');
                if (row.onclick.toString().includes(vehicleId)) {
                    row.classList.add('selected-vehicle');
                }
            });

            resizeMap();
        }
    };

    // Load vehicle details
    function loadVehicleDetails(vehicleId) {
        fetch(`/api/tracking/vehicle/${vehicleId}/details`)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                if (!data) return;
                
                updateVehicleDetails(data);
                updateDriverInfo(data);
                updateTripInfo(data);
                updatePositionInfo(data);
            })
            .catch(error => {
                console.error('Error loading vehicle details:', error);
                const errorData = {
                    error: 'Failed to load vehicle data',
                    message: error.message
                };
                
                updateInfoSection('vehicle-details', errorData);
                updateInfoSection('driver-info', errorData);
                updateInfoSection('position-info', errorData);
                updateInfoSection('trip-info', errorData);
            });
    }

    // Update info sections
    function updateInfoSection(sectionId, data) {
        const section = document.getElementById(sectionId);
        if (section) {
            const content = section.querySelector('.info-content');
            if (content) {
                let html = '';
                for (const [key, value] of Object.entries(data)) {
                    html += `
                        <div class="info-row">
                            <span>${formatLabel(key)}:</span>
                            <span>${value}</span>
                        </div>
                    `;
                }
                content.innerHTML = html;
            }
        }
    }

    function formatLabel(key) {
        return key.split('_')
            .map(word => word.charAt(0).toUpperCase() + word.slice(1))
            .join(' ');
    }

    function updateVehicleDetails(data) {
        const movementStatus = getMovementStatus(data);
        const tripStatus = getTripStatus(data);
        
        const vehicleData = {
            model: data.model || 'Unknown',
            plate: data.licensePlate || 'Unknown',
            movement_status: movementStatus,
            trip_status: tripStatus,
            type: getVehicleTypeName(data.type),
            total_distance: `${(data.totalDistanceTraveled || 0).toFixed(2)} km`,
            sim_card: data.simCardNumber || 'Not assigned'
        };
        
        updateInfoSection('vehicle-details', vehicleData);
    }

    function updateDriverInfo(data) {
        if (data.activeTrip) {
            const driverData = {
                driver_name: data.activeTrip.driverName || 'Unknown',
                phone: data.activeTrip.driverPhone || 'Not available',
                trip_id: `#${data.activeTrip.id}`,
                destination: data.activeTrip.orderDestination || 'Unknown'
            };
            updateInfoSection('driver-info', driverData);
        } else {
            const driverData = {
                status: 'No active trip',
                message: 'Vehicle is not currently assigned to any trip'
            };
            updateInfoSection('driver-info', driverData);
        }
    }

    function updateTripInfo(data) {
        if (data.activeTrip) {
            const tripData = {
                trip_status: data.activeTrip.status,
                distance: `${(data.activeTrip.distance || 0).toFixed(2)} km`,
                destination: data.activeTrip.orderDestination || 'Unknown'
            };
            updateInfoSection('trip-info', tripData);
        } else {
            const tripData = {
                status: 'No active trip',
                message: 'Vehicle is available for assignment'
            };
            updateInfoSection('trip-info', tripData);
        }
    }

    function updatePositionInfo(data) {
        const movementStatus = data ? getMovementStatus(data) : null;
        if (data && data.latestLocation && movementStatus !== 'GPS Offline' && movementStatus !== 'No GPS Signal') {
            const positionData = {
                position: `${data.latestLocation.latitude}°, ${data.latestLocation.longitude}°`,
                speed: `${data.latestLocation.speed} kph`,
                timestamp: new Date(data.latestLocation.timestamp).toLocaleString()
            };
            updateInfoSection('position-info', positionData);
        } else if (data) {
            const positionData = {
                status: movementStatus === 'GPS Offline' ? 'GPS Offline' : 'No GPS Signal',
                message: data.simCardNumber ? 'GPS device not responding' : 'No SimCard attached',
                speed: 'N/A',
                last_known: 'No location data available'
            };
            updateInfoSection('position-info', positionData);
        }
    }

    // Map control functions
    window.toggleVehiclePaths = function() {
        showPaths = !showPaths;
        const btn = document.querySelector('.control-btn[onclick="toggleVehiclePaths()"]');
        if (btn) {
            btn.classList.toggle('active', showPaths);
        }
        
        if (showPaths) {
            updateVehicleMarkers(vehicles);
        } else {
            Object.values(paths).forEach(path => map.removeLayer(path));
        }
    };

    window.centerMap = function() {
        if (selectedVehicleId && markers[selectedVehicleId]) {
            const marker = markers[selectedVehicleId];
            map.setView(marker.getLatLng(), 14);
        } else {
            map.setView([30.0444, 31.2357], 8);
        }
    };

    window.refreshData = function() {
        const btn = document.querySelector('.control-btn[onclick="refreshData()"]');
        if (btn) {
            btn.classList.add('loading');
        }
        
        fetchVehicleData();
        showSimpleNotification('Data refreshed successfully');
        
        setTimeout(() => {
            if (btn) {
                btn.classList.remove('loading');
            }
        }, 1000);
    };

    // Filter vehicles
    window.filterVehicles = function () {
        const input = document.querySelector('.search-bar');
        if (!input) return;
        
        const filter = input.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#vehicleTable tbody tr');
        let hasVisibleRows = false;

        rows.forEach(row => {
            const vehicleName = row.querySelector('td:nth-child(1)')?.textContent.toLowerCase() || '';
            const speed = row.querySelector('td:nth-child(2)')?.textContent.toLowerCase() || '';
            const movementStatus = row.querySelector('td:nth-child(3)')?.textContent.toLowerCase() || '';
            const tripStatus = row.querySelector('td:nth-child(4)')?.textContent.toLowerCase() || '';
            
            const matchesSearch = 
                vehicleName.includes(filter) || 
                speed.includes(filter) || 
                movementStatus.includes(filter) ||
                tripStatus.includes(filter);

            if (matchesSearch) {
                row.style.display = '';
                hasVisibleRows = true;
                if (filter !== '') {
                    row.classList.add('vehicle-highlight');
                } else {
                    row.classList.remove('vehicle-highlight');
                }
            } else {
                row.style.display = 'none';
                row.classList.remove('vehicle-highlight');
            }
        });

        // Show/hide "no results" message
        let noResultsMsg = document.querySelector('.no-results-message');
        if (!hasVisibleRows && filter !== '') {
            if (!noResultsMsg) {
                noResultsMsg = document.createElement('tr');
                noResultsMsg.className = 'no-results-message';
                noResultsMsg.innerHTML = `
                    <td colspan="4" style="text-align: center; padding: 20px;">
                        No vehicles found matching "${filter}"
                    </td>
                `;
                const tbody = document.querySelector('#vehicleTable tbody');
                if (tbody) {
                    tbody.appendChild(noResultsMsg);
                }
            }
        } else if (noResultsMsg) {
            noResultsMsg.remove();
        }
    };

    // Event listeners
    const searchForm = document.querySelector('.search-form');
    const searchInput = document.querySelector('.search-bar');
    
    if (searchForm && searchInput) {
        searchForm.addEventListener('submit', function(e) {
            e.preventDefault();
            filterVehicles();
        });

        searchInput.addEventListener('keyup', function(e) {
            if (e.key === 'Escape') {
                searchInput.value = '';
                filterVehicles();
            }
        });
    }

    // Notification function
    function showSimpleNotification(message) {
        const notification = document.createElement('div');
        notification.className = 'simple-notification';
        notification.textContent = message;
        document.body.appendChild(notification);

        setTimeout(() => {
            notification.classList.add('show');
        }, 100);

        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 2000);
    }
});


