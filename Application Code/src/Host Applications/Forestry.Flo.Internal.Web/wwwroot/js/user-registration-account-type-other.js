$(function () {

    $(document).ready(function() {

        showHideAccountTypeOther();

        $('#RequestedAccountType').on("change", function() {
            showHideAccountTypeOther();
        });

    });

    function showHideAccountTypeOther() {
        var selectedAccountType = $('#RequestedAccountType').find(":selected").val();
        if (selectedAccountType == 5) {
            $('#account-type-other-group').show();
        } else {
            $('#account-type-other-group').hide();
        }
    }

});