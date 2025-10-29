$(function () {

    var $docsCount = parseInt($('#existing-files-count').val());
    var $maxFileUpload = parseInt($('#max-file-size').val());
    var $currentFilesCollectionSize = 0;

    const $isDisabled = ($('#is-disabled').val() === 'true');

    $(document).ready(function() {

        // click event for the remove/restore link on each metadata card
        var toggleCard = function() {
            var id = parseInt($(this).data("id"));
            var isRemoved = $('#site-visit-evidence-content-' + id).attr("aria-hidden") === "true";

            if (isRemoved) {
                $('#site-visit-evidence-content-' + id).removeAttr("aria-hidden");
                $('#site-visit-evidence-content-' + id).removeClass("govuk-visually-hidden");

                $('#removed-' + id).remove();
                $('#SiteVisitEvidenceMetadata_' + id + '__MarkedForDeletion').attr("value", "False");

                $(this).text("Remove");
            } else {
                $('#site-visit-evidence-content-' + id).attr("aria-hidden", "true");
                $('#site-visit-evidence-content-' + id).addClass("govuk-visually-hidden");

                $('#site-visit-evidence-card-' + id).find("h3").append($('<span id="removed-' + id + '"> (Removed)</span>'));
                $('#SiteVisitEvidenceMetadata_' + id + '__MarkedForDeletion').attr("value", "True");

                $(this).text("Restore");
            }
        }

        $(document).find('a[id^="remove-"]').click(toggleCard);

        var selectedFiles = [];

        // function to clear any existing error messages and highlighting from the file upload control
        var clearErrors = function () {
            $('#site-visit-files-error').addClass('govuk-visually-hidden');
            $('#site-visit-files-error').attr('aria-hidden', 'true');
            $('#site-visit-files').removeClass('govuk-file-upload--error');
            $('#site-visit-files-error-text').html('');
            $('#file-select-group').removeClass('govuk-form-group--error');
        }

        // function to add an error message to the error summary and highlight the file upload control
        var addError = function (errorMessage) {
            $('#site-visit-files-error').removeClass('govuk-visually-hidden');
            $('#site-visit-files-error').attr('aria-hidden', 'false');
            $('#site-visit-files-error-text').append(errorMessage + "<br/>");
            $('#site-visit-files').addClass('govuk-file-upload--error');
            $('#file-select-group').addClass('govuk-form-group--error');
        };

        // function to create a new metadata card for an added file
        var createCardTemplate = function (file) {
            var index = $docsCount - 1;
            var disabled = $isDisabled ? 'disabled="disabled"' : '';

            var link = $isDisabled ? '' : '<a class="govuk-link" id="remove-' + index + '" data-id="' + index + '" href="#site-visit-evidence-card-' + index + '">Remove file</a>';
            var $template = $('' +
                '<div id="site-visit-evidence-card-' + index + '" class="govuk-summary-card">' +
                '<input id="SiteVisitEvidenceMetadata_' + index + '__SupportingDocumentId" type="hidden" data-val="true" name="SiteVisitEvidenceMetadata[' + index + '].SupportingDocumentId">' +
                '<input id="SiteVisitEvidenceMetadata_' + index + '__FileName" type="hidden" data-val="true" name="SiteVisitEvidenceMetadata[' + index + '].FileName" value="' + file.name + '">' +
                '<input id="SiteVisitEvidenceMetadata_' + index + '__MarkedForDeletion" type="hidden" data-val="true" name="SiteVisitEvidenceMetadata[' + index + '].MarkedForDeletion" value="False">' +
                '<div class="govuk-summary-card__title-wrapper">' +
                '<h3 class="govuk-summary-card__title">File ' + $docsCount + ' of ' +
                '<span id="files-length-' + index + '">' + $docsCount + '</span></h3>' +
                '<ul class="govuk-summary-card__actions"><li class="govuk-summary-card__action">' + link +
                '</li></ul>' +
                '</div>' +
                '<div id="site-visit-evidence-content-' + index + '" class="govuk-summary-card__content">' +
                '<dl class="govuk-summary-list">' +
                '<div class="govuk-summary-card__row">' +
                '<dt class="govuk-summary-list__key">' + file.name + '</dt>' +
                '</div>' +
                '<div class="govuk-summary-card__row">' +
                '<dt class="govuk-summary-list__key">' +
                '<div>Label this file<p class="govuk-hint">Add a short title to identify this file in the summary.</p></div>' +
                '<input ' + disabled + ' class="govuk-input govuk-input--width-20" id="SiteVisitEvidenceMetadata_' + index + '__Label" type="text" name="SiteVisitEvidenceMetadata[' + index + '].Label" />' +
                '</dt>' +
                '</div>' +
                '<div class="govuk-summary-card__row">' +
                '<dt class="govuk-summary-list__key">' +
                '<div>Comment<p class="govuk-hint">Add any relevant details about this file. For example the location, what it shows, or why it\'s important.</p></div>' +
                '<div>' +
                '<textarea ' + disabled + ' class="govuk-textarea" rows="4" id="SiteVisitEvidenceMetadata_' + index + '__Comment" name="SiteVisitEvidenceMetadata[' + index + '].Comment"></textarea>' +
                '<div class="govuk-checkboxes govuk-checkboxes--small">' +
                '<div class="govuk-checkboxes__item">' +
                '<input ' + disabled + 'class="govuk-checkboxes__input" type="checkbox" data-val="true" id="SiteVisitEvidenceMetadata_' + index + '__VisibleToApplicants" name="SiteVisitEvidenceMetadata[' + index + '].VisibleToApplicants" value="false">' +
                '<label class="govuk-label govuk-checkboxes__label" for="SiteVisitEvidenceMetadata_' + index + '__VisibleToApplicants">Visible to applicants</label>' +
                '</div>' + //divs around first checkbox
                '<div class="govuk-checkboxes__item">' +
                '<input ' + disabled + 'class="govuk-checkboxes__input" type="checkbox" data-val="true" id="SiteVisitEvidenceMetadata_' + index + '__VisibleToConsultees" name="SiteVisitEvidenceMetadata[' + index + '].VisibleToConsultees" value="false">' +
                '<label class="govuk-label govuk-checkboxes__label" for="SiteVisitEvidenceMetadata_' + index + '__VisibleToConsultees">Visible to external consultees</label>' +
                '</div>' + //divs around second checkbox
                '</div>' +  //div for checkboxes group
                '</div>' +  //div containing textarea and checkboxes
                '</dt>' +
                '</div>' +
                '</div></div>');

            $template.find('.govuk-checkboxes__input').click(function() {
                
                if (this.checked) {
                    $(this).attr("checked", "checked");
                    $(this).attr("value", "true");
                } else {
                    $(this).removeAttr("checked");
                    $(this).attr("value", "false");
                }
            });

            $template.find('.govuk-link').click(toggleCard);

            return $template;
        }

        // function to create the hidden input controls for the checkboxes in a new file metadata entry
        var createInputsTemplate = function () {
            var index = $docsCount - 1;
            var $template = $('' +
                '<input name="SiteVisitEvidenceMetadata[' + index + '].VisibleToApplicants" type="hidden" value="false">' +
                '<input name="SiteVisitEvidenceMetadata[' + index + '].VisibleToConsultees" type="hidden" value="false">');

            return $template;
        }

        // function to update the file count spans in each metadata card
        var updateCounts = function () {
            $("span[id^='files-length-']").text($docsCount);
        }

        // function to add a new file to the selected files array, and add the metadata card and hidden input controls to the form
        var addFile = function (file) {
            selectedFiles.push(file);
            $docsCount++;
            $currentFilesCollectionSize += file.size;
            var $newTemplate = createCardTemplate(file);
            $('#file-metadata-cards').append($newTemplate);
            var $newInputs = createInputsTemplate();
            $('#add-site-evidence-form').append($newInputs);
            updateCounts();
        };

        // function to update the file upload control details and hidden input control after calculating which selected files to keep
        var updateInputDetails = function () {
            if (selectedFiles.length > 0) {

                var inputText = selectedFiles.length + ' files chosen';

                if (selectedFiles.length === 1) {
                    inputText = selectedFiles[0].name;
                }

                $(".govuk-file-upload-button__status").text(inputText);
            } else {
                $(".govuk-file-upload-button__status").text('No files chosen');
                $("#site-visit-files").addClass('govuk-file-upload-button--empty');
            }

            var data = new DataTransfer();
            $.each(selectedFiles, function (index, existingFile) {
                data.items.add(existingFile);
            });

            $('#site-visit-files-input').prop('files', data.files);
        }

        // change event for the file upload control
        $('#site-visit-files-input').on('change', function (e, data) {

            clearErrors();

            $.each(this.files, function(index, file) {

                var fileName = file.name, fileExists = false;
                var fileNameParts = file.name.split('.');
                var extension = fileNameParts[fileNameParts.length - 1].toLowerCase();

                if (extension !== "png" && extension !== "jpg" && extension !== "jpeg" && extension !== "pdf") {
                    addError(file.name + " - selected file not of the correct type");
                } else if ($currentFilesCollectionSize + file.size >= $maxFileUpload) {
                    addError("Only " +
                        $("#max-files-size-description").val() +
                        " in total of files can be uploaded at once, some files were deselected");
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
                        if (!fileExists) {
                            addFile(file);
                        }
                    }
                }

            });

            updateInputDetails();
        });
    });
});