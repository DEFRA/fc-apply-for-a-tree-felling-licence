$(function () {
    $(document).ready(function () {
        $('#submit-import-files-btn').attr('disabled', 'disabled');

        var selectedFiles = [];

        var clearErrors = function () {
            $('#data-import-files-error').addClass('govuk-visually-hidden');
            $('#data-import-files-error').attr('aria-hidden', 'true');
            $('#submit-import-files-btn').removeAttr('disabled');
            $('#data-import-files').removeClass('govuk-file-upload--error');
            $('#data-import-error-text').html('');
            $('#file-select-group').removeClass('govuk-form-group--error');
        }

        var updateListVisibility = function () {
            if (selectedFiles.length === 0) {
                $('#import-selected-files-list').addClass('govuk-visually-hidden');
                $('#import-selected-files-list').attr('aria-hidden', 'true');
                $('#submit-import-files-btn').attr('disabled', 'disabled');
            }
            else {
                $('#import-selected-files-list').removeClass('govuk-visually-hidden');
                $('#import-selected-files-list').attr('aria-hidden', 'false');
                $('#submit-import-files-btn').removeAttr('disabled');
            }
        };

        var updateInputDetails = function () {
            if (selectedFiles.length > 0) {

                var inputText = selectedFiles.length + ' files chosen';

                if (selectedFiles.length === 1) {
                    inputText = selectedFiles[0].name;
                }

                $(".govuk-file-upload-button__status").text(inputText);
            } else {
                $(".govuk-file-upload-button__status").text('No files chosen');
                $("#data-import-files").addClass('govuk-file-upload-button--empty');
            }

            var data = new DataTransfer();
            $.each(selectedFiles, function (index, existingFile) {
                data.items.add(existingFile);
            });

            $('#data-import-files-input').prop('files', data.files);
        }

        var addError = function (errorMessage) {
            $('#data-import-files-error').removeClass('govuk-visually-hidden');
            $('#data-import-files-error').attr('aria-hidden', 'false');
            $('#data-import-error-text').append(errorMessage + "<br/>");
            $('#data-import-files').addClass('govuk-file-upload--error');
            $('#file-select-group').addClass('govuk-form-group--error');
        };

        var getIdForFile = function (file) {
            return file.lastModified + "" + file.size;
        };

        var removeFile = function (e) {
            e.preventDefault();
            clearErrors();

            var idToRemove = e.data.id,
                selector = ".govuk-table__row#" + idToRemove,
                rowToRemove = $(selector);

            if (rowToRemove !== null && rowToRemove.length > 0) {

                rowToRemove.remove();

                selectedFiles = $.grep(selectedFiles, function (file) {
                    return idToRemove !== getIdForFile(file);
                });

                updateListVisibility();
                updateInputDetails();
            }
        };

        var createTemplate = function (file, counter) {
            var $template = $('' +
                '<tr class="govuk-table__row">' +
                '<th scope="row" class="govuk-table__header"></th>' +
                '<td class="govuk-table__cell"><a data-module="govuk-button" class="govuk-button govuk-button--warning" href="#">Remove<span class="govuk-visually-hidden"></span></a></td>' +
                '</tr>'),
                id = getIdForFile(file);

            $template.attr('id', id);
            $template.find('.govuk-table__header').text(file.name);
            $template.find('.govuk-visually-hidden').text(file.name);

            $template.find('.govuk-button').click({ id: id }, removeFile);

            return $template;
        };

        var addFile = function (file) {
            selectedFiles.push(file);
            var $newTemplate = createTemplate(file, selectedFiles.length);
            $('#import-selected-files').append($newTemplate);
            updateListVisibility();
        };

        $('#data-import-files-input').on('change', function (e, data) {
            clearErrors();

            if (selectedFiles.length + this.files.length <= 3) {
                $.each(this.files, function (index, file) {

                    var fileName = file.name, fileExists = false;
                    var fileNameParts = file.name.split('.');
                    var extension = fileNameParts[fileNameParts.length - 1].toLowerCase();

                    if (extension !== "csv") {
                        addError(file.name + ' - selected files must be a .csv template file');
                    } else {
                        if (selectedFiles.length === 0) {
                            addFile(file);
                        } else {
                            $.each(selectedFiles, function (index, existingFile) {
                                if (existingFile.name === fileName) {
                                    fileExists = true;
                                    return false;
                                }
                            });
                            if (!fileExists && selectedFiles.length < 3) {
                                addFile(file);
                            }
                        }
                    }
                });
            }
            else {
                addError('You can only upload a maximum of 3 files.');
            };

            updateInputDetails();
        });
    });
});