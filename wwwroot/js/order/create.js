// Dynamic resource availability check for order creation

document.addEventListener('DOMContentLoaded', function () {
    const vehicleTypeSelect = document.getElementById('vehicleTypeSelect');
    const passengerCountInput = document.getElementById('PassengerCount');
    const tripStartInput = document.getElementById('TripStartDate');
    const tripEndInput = document.getElementById('TripEndDate');
    const noteDiv = document.getElementById('resourceAvailabilityNote');

    // You may want to fetch this from the server via AJAX for real data
    // For now, use a simple heuristic: if vehicle type and passenger count are filled, show a note
    function checkAvailability() {
        const vehicleType = vehicleTypeSelect.value;
        const passengerCount = parseInt(passengerCountInput.value, 10);
        const tripStart = tripStartInput.value;
        const tripEnd = tripEndInput.value;

        if (!vehicleType || !passengerCount || !tripStart || !tripEnd) {
            noteDiv.style.display = 'none';
            return;
        }

        // Simulate a check: if passengerCount > 20 or vehicleType is Bus, assume less likely available
        let likelyAvailable = true;
        if (vehicleType === 'Bus' && passengerCount > 30) {
            likelyAvailable = false;
        } else if (passengerCount > 20) {
            likelyAvailable = false;
        }

        noteDiv.style.display = 'block';
        if (likelyAvailable) {
            noteDiv.innerHTML = '<i class="fas fa-info-circle"></i> <span style="color: #28a745;">Resources may be available for this order.</span>';
        } else {
            noteDiv.innerHTML = '<i class="fas fa-exclamation-triangle"></i> <span style="color: #dc3545;">There may be a lack of resources for your order.</span>';
        }
        noteDiv.style.opacity = 0.7;
    }

    vehicleTypeSelect.addEventListener('change', checkAvailability);
    passengerCountInput.addEventListener('input', checkAvailability);
    tripStartInput.addEventListener('change', checkAvailability);
    tripEndInput.addEventListener('change', checkAvailability);
}); 