
const showSuccessToast = (title, message) => {
    Swal.fire({
        icon: "success",
        title: title,
        text: message,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        customClass: {
            popup: 'swal2-success-toast'
        },
        willOpen: () => {
            const swalContainer = Swal.getContainer();
            swalContainer.addEventListener('mouseenter', () => {
                Swal.stopTimer();
            });
            swalContainer.addEventListener('mouseleave', () => {
                Swal.resumeTimer();
            });
        }
    });
}

const showWarningToast = (title, message) => {
    Swal.fire({
        icon: "warning",
        title: title,
        text: message,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        customClass: {
            popup: 'swal2-warning-toast'
        },
        willOpen: () => {
            const swalContainer = Swal.getContainer();
            swalContainer.addEventListener('mouseenter', () => {
                Swal.stopTimer();
            });
            swalContainer.addEventListener('mouseleave', () => {
                Swal.resumeTimer();
            });
        }
    });
}

const showErrorToast = (title, message) => {
    Swal.fire({
        icon: "error",
        title: title,
        text: message,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        customClass: {
            popup: 'swal2-error-toast'
        },
        willOpen: () => {
            const swalContainer = Swal.getContainer();
            swalContainer.addEventListener('mouseenter', () => {
                Swal.stopTimer();
            });
            swalContainer.addEventListener('mouseleave', () => {
                Swal.resumeTimer();
            });
        }
    });
}
