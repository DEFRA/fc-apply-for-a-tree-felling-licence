$(function () {

    $(document).ready(function() {

        showHideCanApproveApplications();
        showHideAccountTypeOther();

        $('#RequestedAccountType').on("change", function() {
            showHideCanApproveApplications();
            showHideAccountTypeOther();
        });

    });

    function showHideCanApproveApplications() {
        var selectedAccountType = $('#RequestedAccountType').find(":selected").val();
        if (selectedAccountType == 2 || selectedAccountType == 4) {
            $('#can-approve-applications-group').show();
        } else {
            $('#can-approve-applications-group').hide();
        }
    }

    function showHideAccountTypeOther() {
        var selectedAccountType = $('#RequestedAccountType').find(":selected").val();
        if (selectedAccountType == 5) {
            $('#account-type-other-group').show();
        } else {
            $('#account-type-other-group').hide();
        }
    }

});