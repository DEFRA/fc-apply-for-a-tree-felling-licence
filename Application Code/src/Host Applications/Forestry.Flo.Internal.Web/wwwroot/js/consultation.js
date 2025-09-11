$(function() {
    const $neededYesRadio = $("#consultation-needed-yes");
    const $neededNoRadio = $("#consultation-needed-no");
    const $submitButton = $("#submit-button");

    function handleNeededChangeYes(checked) {
        if (checked === true) {
            $("#invite-section").removeClass("govuk-visually-hidden");
            $("#invite-section").removeAttr("aria-hidden");

            if ($submitButton === null) {  // If the submit button is not found, exit early
                return;
            }

            $submitButton.html("Finish this task");
        }
    }

    function handleNeededChangeNo(checked) {
        if (checked === true) {
            $("#invite-section").addClass("govuk-visually-hidden");
            $("#invite-section").attr("aria-hidden", "true");

            if ($submitButton === null) {  // If the submit button is not found, exit early
                return;
            }

            $submitButton.html("Save and continue");
        }
    }

    function initialiseRadios() {
        handleNeededChangeYes($neededYesRadio.prop("checked"));
        handleNeededChangeNo($neededNoRadio.prop("checked"));
    }

    $neededYesRadio.on("change", function () {
        handleNeededChangeYes($(this).prop("checked"));
    });

    $neededNoRadio.on("change", function () {
        handleNeededChangeNo($(this).prop("checked"));
    });

    $(document).ready(initialiseRadios);
});