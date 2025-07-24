$(function () {
    const fellingDetailsSectionCheckbox = $("#SectionsToReview_FellingAndRestockingDetails_");
    const compartmentCheckboxes = $("#compartment-selection");

    $(document).ready(toggleCompartmentVisibility);

    $(fellingDetailsSectionCheckbox).on('change', toggleCompartmentVisibility);

    function toggleCompartmentVisibility() {
        const visible = $(fellingDetailsSectionCheckbox).is(":checked");
        $(compartmentCheckboxes).css('display', visible ? 'block' : 'none');
    }
});