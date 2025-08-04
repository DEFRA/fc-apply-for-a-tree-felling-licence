function toggleRestockingInputs() {
    var proposal = document.getElementById('ConfirmedFellingRestockingDetails_ConfirmedRestockingDetails_RestockingProposal');
    var densityGroup = document.getElementById('restocking-density-group');
    var treesGroup = document.getElementById('number-of-trees-group');
    var compartmentGroup = document.getElementById('RestockingCompartmentId');
    var speciesGroup = document.getElementById('restocking-tree-species-selection-form-group');
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
    if (value === 'PlantAnAlternativeAreaWithIndividualTrees' || value === 'PlantAnAlternativeArea') {
        compartmentGroup.style.display = '';
    } else {
        compartmentGroup.style.display = 'none';
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