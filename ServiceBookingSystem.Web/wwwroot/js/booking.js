function initBookingForm(serviceId) {
    // 1. Get Context
    const dateInput = $('#bookingDate');
    const timeSelect = $('#bookingTime');

    // 2. Define Load Function
    function loadSlots() {
        const selectedDate = dateInput.val();
        
        // Validation: Don't call API if date is empty
        if (!selectedDate) {
            timeSelect.prop('disabled', true).html('<option value="">Select a date first</option>');
            return;
        }

        // UI Feedback: Show loading state
        timeSelect.prop('disabled', true).html('<option value="">Loading...</option>');

        // 3. AJAX Call to API
        $.get('/api/availability/slots', { serviceId: serviceId, date: selectedDate })
            .done(function (slots) {
                timeSelect.empty();
                
                // 4. Handle Response
                if (slots.length === 0) {
                    timeSelect.html('<option value="">No slots available</option>');
                } else {
                    timeSelect.append('<option value="">Select a time</option>');
                    
                    // 5. Populate Dropdown
                    $.each(slots, function (i, slot) {
                        // slot is "HH:mm:ss" (TimeOnly from API)
                        const timeParts = slot.split(':');
                        const hour = parseInt(timeParts[0]);
                        const minute = timeParts[1];
                        
                        // Format: 14:00 -> 2:00 PM
                        const ampm = hour >= 12 ? 'PM' : 'AM';
                        let displayHour = hour % 12;
                        displayHour = displayHour ? displayHour : 12; // the hour '0' should be '12'
                        const displayTime = displayHour + ':' + minute + ' ' + ampm;

                        // Value = "14:00:00", Text = "2:00 PM"
                        timeSelect.append($('<option></option>').val(slot).text(displayTime));
                    });
                    
                    // Enable dropdown
                    timeSelect.prop('disabled', false);
                }
            })
            .fail(function () {
                timeSelect.html('<option value="">Error loading slots</option>');
            });
    }

    // 6. Bind Event
    dateInput.change(loadSlots);

    // 7. Initial Load (if date is pre-filled)
    if (dateInput.val()) {
        loadSlots();
    }
}