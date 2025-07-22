$(function () {
    $(document).ready(function () {
        $('#submit-import-files-btn').attr('disabled', 'disabled');
    });

    $('#data-import-files').change(function (e) {

        $('#data-import-files-error').addClass('govuk-visually-hidden');
        $('#data-import-files-error').attr('aria-hidden', 'true');
        $('#submit-import-files-btn').removeAttr('disabled');
        $('#data-import-files').removeClass('govuk-file-upload--error');
        $('#data-import-error-text').html('');
        $('#file-select-group').removeClass('govuk-form-group--error');

        const files = e.target.files;
        const fileCount = files.length;
        const errors = [];

        if (fileCount === 0) {
            $('#data-import-files-label').text('Select import CSV files');
            $('#submit-import-files-btn').attr('disabled', 'disabled');
            return;
        }

        var stringLabelText = '';
        for (let i = 0; i < fileCount; i++) {

            var fileNameParts = files[i].name.split('.');
            var extension = fileNameParts[fileNameParts.length - 1].toLowerCase();
            if (extension !== "csv") {
                errors.push(files[i].name + ' - selected files must be a .csv');
            }
            
            stringLabelText += '"' + files[i].name + '" ';
        }
        $('#data-import-files-label').text(stringLabelText.trim());

        if (errors.length > 0) {
            var errorText = errors.join('<br/>');
            $('#data-import-files-error').removeClass('govuk-visually-hidden');
            $('#data-import-files-error').attr('aria-hidden', 'false');
            $('#data-import-error-text').html(errorText);
            $('#submit-import-files-btn').attr('disabled', 'disabled');
            $('#data-import-files').addClass('govuk-file-upload--error');
            $('#file-select-group').addClass('govuk-form-group--error');
        }
    });
});