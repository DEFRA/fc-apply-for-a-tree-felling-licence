$(function () {

    $(document).ready(setSaveStatus);
    $('input[type=radio]').change(setSaveStatus);

    $('#save-conditional-btn').click(function() {
        var notConditional = $('#conditional-no').is(':checked');
        var conditionsCount = $('#number-of-conditions').val();
        var previouslyNotConditional = ($('#previously-not-conditional').val().toLowerCase() === 'true');
        if (notConditional && previouslyNotConditional === false && conditionsCount > 0) {
            Swal.fire({
                title: 'Are you sure you want to make this application unconditional?',
                icon: 'warning',
                confirmButtonText: "Yes",
                cancelButtonText: "No",
                showCancelButton: true,
                focusConfirm: true,
                customClass: {
                    title: 'govuk-heading-s',
                    confirmButton: 'confirm-button-popup'
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    $('#save-conditional-form').submit();
                }
            });
        } else {
            $('#save-conditional-form').submit();
        }
    });

    $('#confirm-generate-conditions-button').click(function () {
        Swal.fire({
            title: 'Are you sure you want to regenerate the conditions?',
            icon: 'warning',
            confirmButtonText: "Yes",
            cancelButtonText: "No",
            showCancelButton: true,
            focusConfirm: true,
            customClass: {
                title: 'govuk-heading-s',
                confirmButton: 'confirm-button-popup'
            }
        }).then((result) => {
            if (result.isConfirmed) {
                $('#generate-conditions-form').submit();
            }
        });
    });

    function setSaveStatus() {

        var optionSelected = $('#conditional-no').is(':checked') || $('#conditional-yes').is(':checked');
        var isEditable = ($('#is-editable').val().toLowerCase() === 'true');

        if (isEditable === false) {
            $('input[type=text]').attr('disabled', 'disabled');
            $('textarea').attr('disabled', 'disabled');
        }

        if (optionSelected && isEditable === true) {
            $('#save-conditional-btn').removeAttr('disabled');
        } else {
            $('#save-conditional-btn').attr('disabled', 'disabled');
        }
    }
    
});