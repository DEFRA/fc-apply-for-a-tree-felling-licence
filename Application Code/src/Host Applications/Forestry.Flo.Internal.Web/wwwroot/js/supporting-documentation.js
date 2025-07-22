$(function () {

    document.getElementsByName('view-supporting-document').forEach(e => {
        e.addEventListener('click',
            function(e) {
                $('input[name=documentIdentifier]').val(e.target.dataset.id);
                $('#documentListForm').submit();
            });
    });
});

$(function() {
    $(document).ready(function() {
        $('#submit-supporting-document-button').attr('disabled', 'disabled');
    });

    document.getElementsByName("visible-remove-supporting-document").forEach(e => {
        e.addEventListener('click',
            function() {
                Swal.fire({
                    title: 'Are you sure you want to remove this supporting document?',
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
                        $(this).closest("td").find(".hidden-remove-supporting-document")[0].click();
                    }
                });
            });
    });

    $('#supporting-documentation-files').change(function (e) {

        $('#supporting-documentation-files-error').addClass('govuk-visually-hidden');
        $('#supporting-documentation-files-error').attr('aria-hidden', 'true');
        $('#supporting-documentation-files').removeClass('govuk-file-upload--error');
        $('#file-upload-error-text').html('');
        $('#file-select-group').removeClass('govuk-form-group--error');
        $('#submit-supporting-document-button').removeAttr('disabled');

        var fileExtensions = $('#allowed-extensions').val();
        var maxSize = $('#allowed-max-size').val();
        var maxSizeDescription = $('#allowed-max-size-description').val();
        var maxNumber = parseInt($('#allowed-number-documents').val());
        var currentNumber = parseInt($('#current-number-documents').val());

        var fileExtensionsArray = fileExtensions.split(', ');
        fileExtensionsArray[fileExtensionsArray.length - 1] = 'or ' + fileExtensionsArray[fileExtensionsArray.length - 1];
        var fileExtensionsDescription = fileExtensionsArray.join(', ');

        const files = e.target.files;
        const fileCount = files.length;
        const errors = [];

        if (fileCount === 0) {
            $('#supporting-documentation-files-label').text('Select a document');
            $('#submit-supporting-document-button').attr('disabled', 'disabled');
            return;
        }

        if ((parseInt(fileCount) + currentNumber) > maxNumber) {
            let uploadsRemaining = parseInt(maxNumber - currentNumber);
            let docPluralityWord = 'documents'
            if (uploadsRemaining === 1) {
                docPluralityWord = 'document';
            }
            errors.push('You can only upload ' + parseInt(maxNumber - currentNumber) + ' more ' + docPluralityWord + ', as ' + maxNumber + ' is the maximum number of documents.');
        }

        var stringLabelText = '';
        for (let i = 0; i < fileCount; i++) {

            if (files[i].size > maxSize) {
                errors.push(files[i].name + ' - files must be smaller than ' + maxSizeDescription);
            }

            var fileNameParts = files[i].name.split('.');
            var extension = fileNameParts[fileNameParts.length - 1].toLowerCase();
            if (fileExtensions.toLowerCase().includes(extension) === false) {
                errors.push(files[i].name + ' - selected files must be a ' + fileExtensionsDescription);
            }

            stringLabelText += `"${files[i].name}" `;
        }
        $('#supporting-documentation-files-label').text(stringLabelText.trim());

        if (errors.length > 0) {
            var errorText = errors.join('<br/>');
            $('#supporting-documentation-files-error').removeClass('govuk-visually-hidden');
            $('#supporting-documentation-files-error').attr('aria-hidden', 'false');
            $('#file-upload-error-text').html(errorText);
            $('#submit-supporting-document-button').prop('disabled', 'disabled');
            $('#supporting-documentation-files').addClass('govuk-file-upload--error');
            $('#file-select-group').addClass('govuk-form-group--error');
        }
    });
});

$(function () {
    const canBeEdited = $('#application-state').text().trim().toLowerCase() === 'submitted';
    if (canBeEdited) {
        $('.govuk-input, .govuk-select, .govuk-checkboxes__input, .govuk-textarea, .govuk-radios__item, .govuk-button--secondary, .govuk-file-upload, .govuk-button[name="store-supporting-documents"], .govuk-button[name="delete-supporting-document"], #supporting-documentation-files, [data-restrictions~="edit"]').each(function () {
            $(this).removeAttr('disabled');
        });
    }
});