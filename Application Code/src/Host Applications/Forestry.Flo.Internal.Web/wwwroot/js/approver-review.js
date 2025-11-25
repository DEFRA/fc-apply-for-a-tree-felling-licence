$(function () {

    const durationHintTemplate = 'You can change the licence duration, the licence end date will be automatically generated based on this selection. If you approve this application today a [years] will expire [D/MM/YYYY].'

    $(document).ready(function () {
        updateDisableExempt();
        updateDisableConditionalReason();
        handleApprovedLicenceDurationChange();
        updateDecisionConditionals();
    });

    $("input[name='ApproverReview.PublicRegisterPublish']").on("change", function () {
        updateDisableExempt();
    });

    $("input[name='ApproverReview.RequestedStatus']").on("change", function () {
        updateDisableConditionalReason();
    });

    $('#ApproverReview_ApprovedLicenceDuration').change(function () {
        handleApprovedLicenceDurationChange();
    });

    $('input[name="Decision"]').on('change', updateDecisionConditionals);

    function handleApprovedLicenceDurationChange() {
        var selectedText = $('#ApproverReview_ApprovedLicenceDuration').find("option:selected").text();
        var selectedValue = parseInt($('#ApproverReview_ApprovedLicenceDuration').val(), 10);
        var endDate = new Date();
        endDate.setFullYear(endDate.getFullYear() + selectedValue);
        var formattedDate = endDate.toLocaleDateString('en-GB', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
        var updatedHint = durationHintTemplate.replace('[years]', selectedText).replace('[D/MM/YYYY]', formattedDate);
        $('#duration-hint').text(updatedHint);
    }

    function updateDisableExempt() {
        var checked = $("#dpr-no").is(':checked');
        var conditionalExemptFieldset = $("#conditional-exempt");

        if (checked) {
            $(conditionalExemptFieldset).prop('disabled', false);
        } else {
            $(conditionalExemptFieldset).prop('disabled', true);
        }
    }

    function updateDisableConditionalReason() {
        var isApproved = $("#ApproverReview_RequestedStatus").is(':checked');
        const conditionalReasonFieldset = $("#conditional-reason");

        if (isApproved) {
            $(conditionalReasonFieldset).prop('disabled', false);
        } else {
            $(conditionalReasonFieldset).prop('disabled', true);
        }
    }

    function updateDecisionConditionals() {
        var $yes = $('#decision-yes');
        var $no = $('#decision-no');
        var yesChecked = $yes.prop('checked');
        var noChecked = $no.prop('checked');

        if (yesChecked) {
            showOrHideElement($('#Decision'), true);
            showOrHideElement($('#conditional-decision-no'), false);
            showOrHideElement($('#conditional-decision-empty'), false);
        } else if (noChecked) {
            showOrHideElement($('#Decision'), false);
            showOrHideElement($('#conditional-decision-no'), true);
            showOrHideElement($('#conditional-decision-empty'), false);
        } else {
            showOrHideElement($('#Decision'), false);
            showOrHideElement($('#conditional-decision-no'), false);
            showOrHideElement($('#conditional-decision-empty'), true);
        }
    }
});

function showOrHideElement($element, show) {
    const element = $($element);
    if (show) {
        element.show();
        element.removeAttr('aria-hidden');
    } else {
        element.hide();
        element.attr('aria-hidden', 'true');
    }
};
