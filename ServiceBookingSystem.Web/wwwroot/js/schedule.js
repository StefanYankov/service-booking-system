/**
 * Toggles the visibility of the segments container and the "Add Shift" button
 * based on the "Closed" checkbox state.
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
 * @param {HTMLButtonElement} btn - The button element that triggered the removal.
 */
function removeSegment(btn) {
    const row = btn.closest('.segment-row');
    // We simply remove the row. If all are removed, the user can add one back via "Add Shift".
    // The backend handles empty lists correctly (as closed or no hours).
    row.remove();
}

/**
 * Dynamically adds a new time segment input row to the specified day.
 * @param {number} dayIndex - The index of the day (0-6).
 */
function addSegment(dayIndex) {
    const container = document.getElementById(`segments_list_${dayIndex}`);
    // Calculate the next index based on current count to ensure unique names for binding
    // Note: MVC Model Binding for lists works best with sequential indices.
    // If rows are deleted, indices might have gaps. 
    // Ideally, we would re-index on submit, but for this simple UI, appending is usually sufficient 
    // if the binder can handle gaps or if we don't delete from the middle often.
    // A more robust solution would be to use a client-side framework or re-index logic.
    // For now, we use the current length as a simple proxy.
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