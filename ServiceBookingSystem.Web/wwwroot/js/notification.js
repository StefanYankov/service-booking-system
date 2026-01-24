"use strict";

/**
 * SignalR Client for Real-time Notifications.
 */

if (typeof signalR !== 'undefined') {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveNotification", function (message) {
        showToast(message);
    });

    connection.start().catch(function (err) {
        console.error("SignalR Connection Error: " + err.toString());
    });

} else {
    console.warn("SignalR library not loaded.");
}

function showToast(message) {
    // 1. Ensure Toast Container exists
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        toastContainer.style.zIndex = '1100'; 
        document.body.appendChild(toastContainer);
    }

    // 2. Create Toast HTML
    const toastId = 'toast-' + Date.now();
    const html = `
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header bg-info text-white">
                <strong class="me-auto">Notification</strong>
                <small>Just now</small>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    // 3. Append to DOM
    toastContainer.insertAdjacentHTML('beforeend', html);
    
    // 4. Initialize and Show
    const toastElement = document.getElementById(toastId);
    if (typeof bootstrap !== 'undefined') {
        const toast = new bootstrap.Toast(toastElement, { delay: 5000 });
        toast.show();
        
        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    } else {
        alert(message);
    }
}