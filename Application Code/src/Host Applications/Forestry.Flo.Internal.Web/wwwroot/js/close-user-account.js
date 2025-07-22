$(function() {
    $(document).ready(function() {
        var visibleSubmit = document.getElementById("visible-submit");

        visibleSubmit.addEventListener('click',
            function () {
                Swal.fire({
                    title: 'Are you sure you want to close this account?',
                    icon: 'warning',
                    confirmButtonText: "Yes",
                    cancelButtonText: "No",
                    showCancelButton: true,
                    focusConfirm: false,
                    customClass: {
                        title: 'govuk-heading-s',
                        confirmButton: 'confirm-popup-button-red',
                        cancelButton: 'confirm-popup-button'
                    }

                }).then((result) => {
                    if (result.isConfirmed) {
                        $('#hidden-submit').click();
                    }
                });
            });
    });
});
