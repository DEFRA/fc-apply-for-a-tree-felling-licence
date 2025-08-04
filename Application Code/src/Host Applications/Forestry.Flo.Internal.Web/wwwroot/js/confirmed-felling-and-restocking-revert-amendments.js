$(function () {
    $('.delete-amendments-visible').click(function (e) {
        e.preventDefault();

        const visibleId = $(this).attr('id');
        const suffix = visibleId.replace('delete-amendments-visible-', '');
        const $invisibleLink = $('#delete-amendments-invisible-' + suffix);

        if ($invisibleLink.length) {
            Swal.fire({
                title: 'Do you wish to delete amendments (revert to proposed felling and restocking)?',
                icon: 'warning',
                confirmButtonText: "Yes",
                cancelButtonText: "No",
                showCancelButton: true,
                focusConfirm: true,
                customClass: {
                    title: 'govuk-heading-s',
                    confirmButton: 'confirm-popup-button'
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    const targetHref = $invisibleLink.attr('href');
                    if (targetHref) {
                        window.location.href = targetHref;
                    }
                }
            });
        }
    });
});
