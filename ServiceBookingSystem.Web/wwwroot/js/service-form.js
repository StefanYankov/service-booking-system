$(document).ready(function() {
    /**
     * Toggles the visibility of address fields based on the "Is Online" checkbox.
     */
    function toggleAddress() {
        if ($('#isOnlineCheck').is(':checked')) {
            $('#addressFields').hide();
        } else {
            $('#addressFields').show();
        }
    }

    // Attach event listener
    $('#isOnlineCheck').change(toggleAddress);
    
    // Initialize state on page load
    toggleAddress();
});