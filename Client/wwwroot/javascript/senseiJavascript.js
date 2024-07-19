document.addEventListener("DOMContentLoaded", function (event) {



});


function showToast(isSuccess, message, icon) {
    // Prepare the toast HTML with dynamic values
    const toastHtml = `
         <div class="toast shadow align-items-center text-bg-${isSuccess ? 'success' : 'danger'} border-0" style="--bs-bg-opacity: .97;" id="liveToast" role="alert" aria-live="assertive" aria-atomic="true">
             <div class="d-flex">
                 <div class="toast-body">
                     <i class="${icon} me-1"></i>${message}
                 </div>
                 <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
             </div>
         </div>`;

    // Insert toast HTML into the toast container
    const toastContainer = document.querySelector('.toast-container');
    toastContainer.innerHTML = toastHtml;

    // Get the toast element and show it using Bootstrap's API
    const toastElement = document.getElementById('liveToast');
    const toastBootstrap = bootstrap.Toast.getOrCreateInstance(toastElement);
    toastBootstrap.show();
}


function dismissModal(modalId) {
    const modal = document.getElementById(modalId);
    const modalInstance = bootstrap.Modal.getInstance(modal);
    modalInstance.hide();
}

// In your Blazor component or in a separate JS file used in Blazor
window.initTooltips = () => {
    var tooltips = document.querySelectorAll('.tooltip-wrapper');
    tooltips.forEach(function (tooltip) {
        new bootstrap.Tooltip(tooltip);
    });
};

// Function to clear the file input
window.clearFileInput = (inputId) => {
    document.getElementById(inputId).value = null;
};