$(function () {

    $(document).ready(function () {

        $('#approve-user-account-btn').click(function (e) {

            e.preventDefault();

            if (confirm('Are you sure you want to approve this user account?')) {
                $('#confirm-user-account-form').submit();
            }

        });

        $('#deny-user-account-btn').click(function (e) {

            e.preventDefault();

            if (confirm('Are you sure you want to deny this user account?')) {
                $('#deny-user-account-form').submit();
            }
        });

        $('#can-approve-applications').change(function () {
            var value = $('#can-approve-applications').prop("checked");
            $('#CanApproveApplications').attr('value', value);
        });

    });

})