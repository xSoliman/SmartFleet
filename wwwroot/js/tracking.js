// Initialize map correctly after page load
document.addEventListener("DOMContentLoaded", function () {
    const map = L.map('map', { zoomControl: false }).setView([30.0444, 31.2357], 8); // Centered on Egypt

    // Make sure map resizes correctly when window size changes
    window.addEventListener('resize', function() {
        map.invalidateSize();
    });

    // Ensure map is properly sized
    setTimeout(function() {
        map.invalidateSize();
    }, 100);

    // Add OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    // Store markers for vehicles
    const markers = {};
    let vehicles = [];

    // Fetch real vehicle data from the API
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

    // Set up periodic refresh (every 30 seconds)
    setInterval(fetchVehicleData, 30000);

    // Update the vehicle table with real data
    function updateVehicleTable(vehicles) {
        const tableBody = document.querySelector('#vehicleTable tbody');
        tableBody.innerHTML = '';

        vehicles.forEach(vehicle => {
            if (!vehicle.latestLocation) return; // Skip vehicles without location data

            const status = getVehicleStatus(vehicle.latestLocation.speed, vehicle.status);
            const row = document.createElement('tr');
            row.onclick = function() { selectVehicle(vehicle.id); };
            
            row.innerHTML = `
                <td><i class="fas fa-${getVehicleIcon(vehicle.type)} fa-lg"></i>&nbsp;&nbsp;${vehicle.model}</td>
                <td><strong>${vehicle.latestLocation.speed}</strong> kph</td>
                <td><span class="status-badge ${status.toLowerCase()}">${status}</span></td>
            `;
            
            tableBody.appendChild(row);
        });
    }

    // Update vehicle markers on the map
    function updateVehicleMarkers(vehicles) {
        // Remove existing markers
        Object.values(markers).forEach(marker => map.removeLayer(marker));
        
        // Add new markers
        vehicles.forEach(vehicle => {
            if (!vehicle.latestLocation) return; // Skip vehicles without location data
            
            const lat = parseFloat(vehicle.latestLocation.latitude);
            const lon = parseFloat(vehicle.latestLocation.longitude);
            
            if (isNaN(lat) || isNaN(lon)) return; // Skip invalid coordinates
            
            markers[vehicle.id] = L.marker([lat, lon]).addTo(map)
                .bindPopup(`${vehicle.model} (${vehicle.licensePlate})`);
        });
    }

    // Helper function to determine vehicle status
    function getVehicleStatus(speed, vehicleStatus) {
        if (speed > 0) return "Moving";
        if (vehicleStatus === 1) return "In Use"; // in_use enum value
        if (vehicleStatus === 2) return "Maintenance"; // under_maintenance enum value
        return "Stopped";
    }

    // Helper function to get appropriate icon based on vehicle type
    function getVehicleIcon(type) {
        switch(type) {
            case 0: return "car"; // Car
            case 1: return "truck"; // Truck
            case 2: return "bus"; // Bus
            case 3: return "shuttle-van"; // Van
            case 4: return "motorcycle"; // Motorcycle
            default: return "truck-moving"; // Other
        }
    }

    // Select vehicle and zoom in
    window.selectVehicle = function (vehicleId) {
        const vehicle = vehicles.find(v => v.id === vehicleId);
        if (vehicle && vehicle.latestLocation) {
            // Update map view
            const lat = parseFloat(vehicle.latestLocation.latitude);
            const lon = parseFloat(vehicle.latestLocation.longitude);
            
            if (!isNaN(lat) && !isNaN(lon)) {
                map.setView([lat, lon], 12);
                markers[vehicle.id].openPopup();
            }

            // Scroll to info panel when a vehicle is selected
            const infoPanel = document.getElementById('info-panel');
            if (infoPanel) {
                setTimeout(() => {
                    infoPanel.scrollIntoView({ behavior: 'smooth' });

                    // Add highlight effect
                    infoPanel.classList.add('highlight');
                    setTimeout(() => {
                        infoPanel.classList.remove('highlight');
                    }, 1500);
                }, 300);
            }

            // Format status for display
            const status = getVehicleStatus(vehicle.latestLocation.speed, vehicle.status);

            // Update info sections with vehicle data
            updateInfoSection('vehicle-details', {
                model: vehicle.model,
                plate: vehicle.licensePlate,
                status: status,
                type: getVehicleTypeName(vehicle.type)
            });

            updateInfoSection('position-info', {
                position: `${vehicle.latestLocation.latitude}°, ${vehicle.latestLocation.longitude}°`,
                speed: `${vehicle.latestLocation.speed} kph`,
                timestamp: new Date(vehicle.latestLocation.timestamp).toLocaleString()
            });

            // Highlight selected vehicle in table
            const rows = document.querySelectorAll('#vehicleTable tbody tr');
            rows.forEach(row => {
                row.classList.remove('selected-vehicle');
                if (row.onclick.toString().includes(vehicleId)) {
                    row.classList.add('selected-vehicle');
                }
            });

            // Force map to resize in case layout changed
            setTimeout(() => {
                map.invalidateSize();
            }, 200);
        }
    }

    // Helper function to get vehicle type name
    function getVehicleTypeName(type) {
        const types = ["Car", "Truck", "Bus", "Van", "Motorcycle", "Other"];
        return types[type] || "Unknown";
    }

    // Helper function to update info sections (only .info-content)
    function updateInfoSection(sectionId, data) {
        const section = document.getElementById(sectionId);
        if (section) {
            const content = section.querySelector('.info-content');
            if (content) {
                content.innerHTML = Object.entries(data)
                    .map(([key, value]) => `
                        <div class="info-row">
                            <span>${formatLabel(key)}:</span>
                            <span>${value}</span>
                        </div>
                    `).join('');
            }
        }
    }

    // Helper function to format labels
    function formatLabel(key) {
        return key.split('_')
            .map(word => word.charAt(0).toUpperCase() + word.slice(1))
            .join(' ');
    }

    window.filterVehicles = function () {
        const input = document.querySelector('.search-bar');
        const filter = input.value.toLowerCase().trim();
        const rows = document.querySelectorAll('#vehicleTable tbody tr');
        let hasVisibleRows = false;

        rows.forEach(row => {
            const vehicleName = row.querySelector('td').textContent.toLowerCase();
            const speed = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
            const status = row.querySelector('td:nth-child(3)').textContent.toLowerCase();
            
            const matchesSearch = 
                vehicleName.includes(filter) || 
                speed.includes(filter) || 
                status.includes(filter);

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
                    <td colspan="3" style="text-align: center; padding: 20px;">
                        No vehicles found matching "${filter}"
                    </td>
                `;
                document.querySelector('#vehicleTable tbody').appendChild(noResultsMsg);
            }
        } else if (noResultsMsg) {
            noResultsMsg.remove();
        }
    };

    // Add event listeners for search
    document.addEventListener('DOMContentLoaded', function() {

        const searchForm = document.querySelector('.search-form');
        const searchInput = document.querySelector('.search-bar');
        
        if (searchForm && searchInput) {
            // Handle form submission (search button click)
            searchForm.addEventListener('submit', function(e) {
                e.preventDefault();
                filterVehicles();
            });

            // Handle ESC key to clear search
            searchInput.addEventListener('keyup', function(e) {
                if (e.key === 'Escape') {
                    searchInput.value = '';
                    filterVehicles();
                }
            });
        }
    });
});
