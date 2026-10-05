// Shared JavaScript for College Management System
document.addEventListener("DOMContentLoaded", function () {
    // Mobile navigation toggle
    const toggleBtn = document.getElementById("navToggle");
    const navMenu = document.getElementById("navMenu");

    if (toggleBtn && navMenu) {
        toggleBtn.addEventListener("click", function () {
            navMenu.classList.toggle("open");
            toggleBtn.classList.toggle("active");
        });
    }

    // Dropdown for mobile touch devices
    const dropdownToggle = document.getElementById("programsDropdown");
    if (dropdownToggle) {
        dropdownToggle.addEventListener("click", function (e) {
            if (window.innerWidth <= 992) {
                const parent = this.parentElement;
                if (!parent.classList.contains("dropdown-open")) {
                    e.preventDefault();
                    parent.classList.toggle("dropdown-open");
                }
            }
        });
    }
});
