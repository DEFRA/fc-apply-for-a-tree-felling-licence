$(function () {

    $('#tree-health-file-upload-input').change(function (e) {

        $('#tree-health-file-upload-input-error').addClass('govuk-visually-hidden');
        $('#tree-health-file-upload-input-error').attr('aria-hidden', 'true');
        $('#tree-health-file-upload-input-error').html('');
        $('#tree-health-file-upload-input').removeClass('govuk-file-upload--error');
        $('#file-select-group').removeClass('govuk-form-group--error');

        var fileExtensions = $('#allowed-extensions').val();
        var maxSize = $('#allowed-max-size').val();
        var maxSizeDescription = $('#allowed-max-size-description').val();

        var fileExtensionsArray = fileExtensions.split(', ');
        fileExtensionsArray[fileExtensionsArray.length - 1] = 'or ' + fileExtensionsArray[fileExtensionsArray.length - 1];
        var fileExtensionsDescription = fileExtensionsArray.join(', ');

        const files = e.target.files;
        const fileCount = files.length;
        const errors = [];

        if (fileCount === 0) {
            return;
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

            stringLabelText += '"' + files[i].name + '" ';
        }
        $('#tree-health-file-upload-input-label').text(stringLabelText.trim());

        if (errors.length > 0) {
            var errorText = errors.join('<br/>');
            $('#tree-health-file-upload-input-error').removeClass('govuk-visually-hidden');
            $('#tree-health-file-upload-input-error').attr('aria-hidden', 'false');
            $('#tree-health-file-upload-input-error').append(
                $("<p>").addClass("govuk-error-message")
                    .append($("<span/>").addClass("govuk-visually-hidden").text("Error:"))
                    .append(errorText)
            );

            $('#tree-health-file-upload-input').addClass('govuk-file-upload--error');
            $('#file-select-group').addClass('govuk-form-group--error');
        }
    });
});