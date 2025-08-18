function toggleRestockingInputs() {
    var proposal = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingProposal');
    var densityGroup = document.getElementById('restocking-density-group');
    var treesGroup = document.getElementById('number-of-trees-group');
    var compartmentGroup = document.getElementById('RestockingCompartmentId');
    var speciesGroup = document.getElementById('restocking-tree-species-selection-form-group');
    var compartmentSelect = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingCompartmentId');
    if (!proposal || !densityGroup || !treesGroup || !compartmentGroup || !speciesGroup) return;
    var value = proposal.value;
    if (value === 'CreateDesignedOpenGround') {
        densityGroup.style.display = 'none';
        treesGroup.style.display = 'none';
        speciesGroup.style.display = 'none';
    } else if (value === 'PlantAnAlternativeAreaWithIndividualTrees' || value === 'RestockWithIndividualTrees') {
        densityGroup.style.display = 'none';
        treesGroup.style.display = '';
        speciesGroup.style.display = '';
    } else {
        densityGroup.style.display = '';
        treesGroup.style.display = 'none';
        speciesGroup.style.display = '';
    }
    if (value === 'PlantAnAlternativeAreaWithIndividualTrees' || value === 'PlantAnAlternativeArea' || value === 'NaturalColonisation') {
        compartmentGroup.style.display = '';
    } else {
        compartmentGroup.style.display = 'none';
        // Set select to initial compartment id
        if (ConfirmedFellingRestockingDetails_SubmittedFlaPropertyCompartmentId) {
            compartmentSelect.value = ConfirmedFellingRestockingDetails_SubmittedFlaPropertyCompartmentId.value;
            if (typeof updateGrossSizeDisplay === 'function') updateGrossSizeDisplay();
            if (typeof updateRestockingCompartmentNumber === 'function') updateRestockingCompartmentNumber();
        }
    }
}
function updateRestockingCompartmentNumber() {
    var select = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingCompartmentId');
    var hidden = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingCompartmentNumber');
    if (!select || !hidden) return;
    var selectedOption = select.options[select.selectedIndex];
    hidden.value = selectedOption ? selectedOption.text : '';
}
document.addEventListener('DOMContentLoaded', function () {
    var proposal = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingProposal');
    var compartmentSelect = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingCompartmentId');
    if (proposal) {
        proposal.addEventListener('change', toggleRestockingInputs);
        toggleRestockingInputs();
    }
    if (compartmentSelect) {
        compartmentSelect.addEventListener('change', updateRestockingCompartmentNumber);
        updateRestockingCompartmentNumber();
    }
});

$("#restocking-add-tree-species-btn").click(function () {
    const $tableRows = $('#restocking-species-list-table tr');
    const rowCount = $tableRows.length - 1;
    if (rowCount > 0) {
        $(this).text("Add another species");
    } else {
        $(this).text("Add species");
    }
    $(this).text("Add another species");
});

// Function to format number to 2 decimal places
function formatDoubleForDisplay(value) {
    if (value == null || isNaN(value)) return '';
    return parseFloat(value).toFixed(2);
}

function updateGrossSizeDisplay() {
    var select = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingCompartmentId');
    var original = document.getElementById('ConfirmedFellingRestockingDetails_SubmittedFlaPropertyCompartmentId');
    var selectedId = select.value.length > 0 ? select.value : original.value;

    const hectares = compartmentHectares[selectedId] ? compartmentHectares[selectedId] : '';
    document.getElementById('compartment-gross-size').textContent = formatDoubleForDisplay(hectares);
    document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingCompartmentTotalHectares').value = formatDoubleForDisplay(hectares);

    // Update hidden input for RestockingTotalHectares
    var totalHectaresInput = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingTotalHectares');
    if (totalHectaresInput) {
        totalHectaresInput.value = hectares;
    }

    // Update PercentOpenSpace
    var restockAreaInput = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockArea');
    var percentOpenSpaceInput = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_PercentOpenSpace');
    if (restockAreaInput && percentOpenSpaceInput) {
        var restockArea = parseFloat(restockAreaInput.value);
        var gross = parseFloat(hectares);
        var percent = (gross > 0 && !isNaN(restockArea)) ? ((restockArea / gross) * 100) : '';
        percentOpenSpaceInput.value = percent !== '' ? formatDoubleForDisplay(percent) : '';
    }
}

// Initial update on page load
document.addEventListener('DOMContentLoaded', function () {
    updateGrossSizeDisplay();
    var select = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingCompartmentId');
    if (select) {
        select.addEventListener('change', updateGrossSizeDisplay);
    }

    var areaField = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockArea');
    if (areaField) {
        areaField.addEventListener('change', updateGrossSizeDisplay);
    }
});


$('#delete-restocking-details-button').click(function () {
    Swal.fire({
        title: 'Do you wish to delete this restocking operation?',
        icon: 'warning',
        confirmButtonText: "Yes",
        cancelButtonText: "No",
        showCancelButton: true,
        focusConfirm: true,
        customClass: {
            title: 'govuk-heading-s',
            confirmButton: 'confirm-popup-button'
        }

    }).then((result) => {
        if (result.isConfirmed) {
            $("#delete-restocking-details").submit();
        }
    });
});