$(function () {
    const reasonBox = $("#failure-reason-input");
    const checkFailed = $("#check-failed");

    $(document).ready(function() {
        updateFailureReasonVisibility();
    });

    $(".check-radios").on("change", function () {
        updateFailureReasonVisibility();
    });

    function updateFailureReasonVisibility() {
        var checked = $(checkFailed).is(':checked');

        if (checked) {
            $(reasonBox).show();
        } else {
            $(reasonBox).hide();
        }
    }

});