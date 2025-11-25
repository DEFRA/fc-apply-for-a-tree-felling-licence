$(function() {
    const $neededYesRadio = $("#SiteVisitNeeded");
    const $neededNoRadio = $("#site-visit-needed-no");

    const $arrangmentsYesRadio = $("#SiteVisitArrangementsMade");
    const $arrangmentsNoRadio = $("#site-visit-arrangements-no");

    const $submitButton = $("#submit-button");
    const $notNeededReason = $("textarea").first();

    function handleNeededChangeYes(checked) {
        var readonly = $("#SiteVisitNeeded").attr("disabled") === "disabled";

        if (checked === true) {
            $("#not-needed-reason").addClass("govuk-visually-hidden");
            $("#not-needed-reason").attr("aria-hidden", "true");

            $('#not-needed-reason textarea').val(''); // Clear the reason text area
            $('#not-needed-reason input[type="checkbox"]').prop('checked', false); // Clear the reason text area checkboxes

            $("#preparation-notes").removeClass("govuk-visually-hidden");
            $("#preparation-notes").removeAttr("aria-hidden");

            if (!readonly) {
                $("#arrangement-notes").removeClass("govuk-visually-hidden");
                $("#arrangement-notes").removeAttr("aria-hidden");
            }

            if ($submitButton === null) {  // If the submit button is not found, exit early
                return;
            }

            $submitButton.html("Ready for site visit");
            handleArrangementsChange();
        }
    }

    function handleNeededChangeNo(checked) {
        var readonly = $("#SiteVisitNeeded").attr("disabled") === "disabled";

        if (checked === true) {
            if (!readonly) {
                $("#not-needed-reason").removeClass("govuk-visually-hidden");
                $("#not-needed-reason").removeAttr("aria-hidden");
            }

            $("#preparation-notes").addClass("govuk-visually-hidden");
            $("#preparation-notes").attr("aria-hidden", "true");

            $('#SiteVisitArrangementsMade').prop('checked', false);
            $('#site-visit-arrangements-no').prop('checked', false);

            $('#preparation-notes textarea').val(''); // Clear the prep notes text area
            $('#preparation-notes input[type="checkbox"]').prop('checked', false); // Clear the prep notes text area checkboxes

            if ($submitButton === null) {  // If the submit button is not found, exit early
                return;
            }

            $submitButton.html("Continue");
            handleReasonChange();
        }
    }

    function handleArrangementsChange() {
        if (!$neededYesRadio.prop("checked") || $submitButton === null) {
            return;
        }

        if ($arrangmentsYesRadio.prop("checked") || $arrangmentsNoRadio.prop("checked")) {
            $submitButton.removeAttr("disabled");
        } else {
            $submitButton.attr("disabled", "disabled");
        }
    }

    function handleReasonChange() {
        var text = $notNeededReason.val();

        if (text == null || text.trim() === '') {
            $submitButton.attr("disabled", "disabled");
        } else {
            $submitButton.removeAttr("disabled");
        }
    }

    function initialiseRadios() {
        handleNeededChangeYes($neededYesRadio.prop("checked"));
        handleNeededChangeNo($neededNoRadio.prop("checked"));
        handleArrangementsChange();
    }

    $neededYesRadio.on("change", function () {
        handleNeededChangeYes($(this).prop("checked"));
    });

    $neededNoRadio.on("change", function () {
        handleNeededChangeNo($(this).prop("checked"));
    });

    $arrangmentsYesRadio.on("change", handleArrangementsChange);

    $arrangmentsNoRadio.on("change", handleArrangementsChange);

    $notNeededReason.on('change keyup paste selectionchange', handleReasonChange);

    $(document).ready(initialiseRadios);
});