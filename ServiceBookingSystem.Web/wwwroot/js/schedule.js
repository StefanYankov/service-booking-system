/**
 * Toggles the visibility of the segments container and the "Add Shift" button
 * based on the "Closed" checkbox state.
 * Used in: Schedule/Index.cshtml
 * @param {number} index - The index of the day (0-6).
 */
function toggleDay(index) {
    const isClosed = document.getElementById(`closed_${index}`).checked;
    const container = document.getElementById(`segments_list_${index}`);
    const addBtn = document.getElementById(`add_btn_${index}`);
    
    if (isClosed) {
        container.classList.add('d-none');
        addBtn.classList.add('d-none');
    } else {
        container.classList.remove('d-none');
        addBtn.classList.remove('d-none');
    }
}

/**
 * Removes a specific time segment row from the DOM.
 * Used in: Schedule/Index.cshtml
 * @param {HTMLButtonElement} btn - The button element that triggered the removal.
 */
function removeSegment(btn) {
    const row = btn.closest('.segment-row');
    row.remove();
}

/**
 * Dynamically adds a new time segment input row to the specified day.
 * Used in: Schedule/Index.cshtml
 * @param {number} dayIndex - The index of the day (0-6).
 */
function addSegment(dayIndex) {
    const container = document.getElementById(`segments_list_${dayIndex}`);
    const count = container.querySelectorAll('.segment-row').length;
    
    const html = `
        <div class="input-group mb-2 segment-row">
            <span class="input-group-text">Start</span>
            <input type="time" class="form-control" name="Days[${dayIndex}].Segments[${count}].Start" value="09:00" />
            <span class="input-group-text">End</span>
            <input type="time" class="form-control" name="Days[${dayIndex}].Segments[${count}].End" value="17:00" />
            <button type="button" class="btn btn-outline-danger" onclick="removeSegment(this)">Remove</button>
        </div>
    `;
    container.insertAdjacentHTML('beforeend', html);
}

/**
 * Toggles the visibility of the custom hours fields based on the "Is Day Off" checkbox.
 * Used in: Schedule/Overrides.cshtml
 */
function toggleHours() {
    const isDayOff = document.getElementById('isDayOff').checked;
    const hoursDiv = document.getElementById('customHours');
    if (isDayOff) {
        hoursDiv.classList.add('d-none');
    } else {
        hoursDiv.classList.remove('d-none');
    }
}