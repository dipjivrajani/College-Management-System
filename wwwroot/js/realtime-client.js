// Real-time client for College Management System using SignalR
(function () {
    if (typeof signalR === "undefined") {
        console.warn("SignalR client library not loaded.");
        return;
    }

    // Create toast container in body if not present
    let toastContainer = document.querySelector(".realtime-toast-container");
    if (!toastContainer) {
        toastContainer = document.createElement("div");
        toastContainer.className = "realtime-toast-container";
        document.body.appendChild(toastContainer);
    }

    function showToast(title, message, icon) {
        const toast = document.createElement("div");
        toast.className = "realtime-toast";
        toast.innerHTML = `
            <span class="realtime-toast-icon">${icon || "🔔"}</span>
            <div class="realtime-toast-content">
                <span class="realtime-toast-title">${title}</span>
                <span class="realtime-toast-msg">${message}</span>
            </div>
        `;
        toastContainer.appendChild(toast);

        setTimeout(() => {
            toast.classList.add("hide");
            setTimeout(() => toast.remove(), 300);
        }, 4000);
    }

    // Helper: Refresh specific DOM section without losing filters, scroll position, or full page reload
    function refreshSection(selector) {
        const target = document.querySelector(selector);
        if (!target) return;

        // If user is currently actively typing in an input inside target, skip overwrite
        const active = document.activeElement;
        if (active && target.contains(active) && (active.tagName === "INPUT" || active.tagName === "SELECT")) {
            return;
        }

        fetch(window.location.href, {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
        .then(response => response.text())
        .then(html => {
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, "text/html");
            const fresh = doc.querySelector(selector);
            if (fresh && target) {
                target.innerHTML = fresh.innerHTML;
            }
        })
        .catch(err => {
            console.log("Partial refresh error:", err);
        });
    }

    // Initialize SignalR Connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/collegeHub")
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();

    // Event: Academic Marks Updated
    connection.on("MarksUpdated", function (data) {
        showToast("Marks Updated", "Academic marks have been updated in real-time.", "📈");

        const path = window.location.pathname.toLowerCase();
        if (path.includes("/student/marks")) {
            refreshSection(".s-card");
            refreshSection(".s-sem-tabs-bar");
            refreshSection(".s-profile-header-card");
        } else if (path.includes("/teacher/marks")) {
            refreshSection(".t-table-responsive");
        } else if (path.includes("/admin/marks")) {
            refreshSection(".semester-marks-card");
            refreshSection(".results-summary-grid");
            refreshSection(".students-marks-table-card");
        }
    });

    // Event: Attendance Updated
    connection.on("AttendanceUpdated", function (data) {
        showToast("Attendance Updated", "Attendance records synchronized in real-time.", "📅");

        const path = window.location.pathname.toLowerCase();
        if (path.includes("/student/attendance")) {
            refreshSection(".student-main-content");
        } else if (path.includes("/teacher/attendance")) {
            refreshSection(".t-table-responsive");
        } else if (path.includes("/admin/attendance")) {
            refreshSection(".attendance-table-card");
        }
    });

    // Event: Student Record Updated / Created / Deleted
    connection.on("StudentUpdated", function (data) {
        showToast("Student Updated", "Student profile updated in real-time.", "🎓");

        const path = window.location.pathname.toLowerCase();
        if (path.includes("/admin/students")) {
            refreshSection(".students-table-card");
            refreshSection(".table-responsive");
        } else if (path === "/admin" || path === "/admin/") {
            refreshSection(".stats-cards-grid");
        }
    });

    // Event: Teacher Record Updated / Created / Deleted
    connection.on("TeacherUpdated", function (data) {
        showToast("Faculty Updated", "Teacher record synchronized in real-time.", "👨‍🏫");

        const path = window.location.pathname.toLowerCase();
        if (path.includes("/admin/teachers")) {
            refreshSection(".teachers-table-card");
            refreshSection(".table-responsive");
        }
    });

    // Event: Course / Subject Updated
    connection.on("CourseUpdated", function (data) {
        showToast("Course Structure", "Academic subjects updated in real-time.", "🔬");

        const path = window.location.pathname.toLowerCase();
        if (path.includes("/admin/courses/subjects")) {
            refreshSection(".subjects-table-card");
        } else if (path.includes("/admin/courses")) {
            refreshSection(".courses-table-card");
        }
    });

    // Start Connection
    connection.start()
        .then(() => {
            console.log("Connected to CollegeManagementHub SignalR.");
        })
        .catch(err => {
            console.warn("SignalR connection failed:", err.toString());
        });
})();
