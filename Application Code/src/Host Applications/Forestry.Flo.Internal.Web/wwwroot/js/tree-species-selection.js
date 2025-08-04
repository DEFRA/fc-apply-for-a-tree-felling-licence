$(function () {

    
    const fellingTreeSpeciesSelections = $('#felling-species-list-table tr td:first-child input').map(function () {
        return $(this).val();
    }).get();
    const restockingTreeSpeciesSelections = $('#restocking-species-list-table tr td:first-child input').map(function(){
        return $(this).val();
    }).get();

    $('#felling-add-tree-species-btn').click({formPrefix: "felling"}, addSpecies);
    $('#restocking-add-tree-species-btn').click({formPrefix: "restocking"}, addSpecies);

    $(".felling-remove-tree-species").click({ formPrefix: "felling" }, removeSpecies);
    $(".restocking-remove-tree-species").click({ formPrefix: "restocking" }, removeSpecies);

    var selectElement1 = document.querySelector('#felling-tree-species-select');
    var selectElement2 = document.querySelector('#restocking-tree-species-select');


    if (selectElement1 !== null) {
        accessibleAutocomplete.enhanceSelectElement({
            selectElement: selectElement1,
            minLength: 3,
            name: 'Species selection'
        });

        $('#felling-tree-species-select').val('');

        var speciesError = $('.govuk-error-summary a[href="#Species"]');
        if (speciesError !== null) {
            speciesError.attr('href', '#felling-tree-species-select');
        }
    }

    if (selectElement2 !== null) {
        accessibleAutocomplete.enhanceSelectElement({
            selectElement: selectElement2,
            minLength: 3,
            name: 'Species and percentages selection'
        });
        $('#restocking-tree-species-select').val('');

        var speciesError = $('.govuk-error-summary a[href="#Species"]');
        if (speciesError !== null) {
            speciesError.attr('href', '#restocking-tree-species-select');
        }
    }
    function addSpecies(e) {
        e.preventDefault(); // Prevent form submission
        const formPrefix = e.data.formPrefix;
        hideTreeSpeciesSelectionError(formPrefix);
        const $selectedSpecies = $(`#${formPrefix}-tree-species-select`);
        const $speciesList = $(`#${formPrefix}-tree-species-select`);

        const selectedText = $selectedSpecies.val();
        // Find row of table for the selected and take the value of the 1st cell as the Species Code
        let $row = $('tr[data-id="' + selectedText + '"]');
        const selectedValue = $row.find('td:first').attr('id');

        let selectedSpecies;
        let idFieldPrefix;
        let nameFieldPrefix;
        if(formPrefix === "felling"){
            selectedSpecies = fellingTreeSpeciesSelections;
            idFieldPrefix = "Species";
            nameFieldPrefix = "Species";
        }
        else{
            selectedSpecies = restockingTreeSpeciesSelections;
            idFieldPrefix = "Species";
            nameFieldPrefix = "Species";
        }
        if (selectedValue === undefined) {
            // No selection
            showTreeSpeciesSelectionError('You must select a species from the list before adding it', formPrefix);
        }
        else if (selectedSpecies.filter(e => e === selectedValue).length > 0) {
            // Tree species already exists in treeSpeciesSelections
            showTreeSpeciesSelectionError(selectedText + ' has already been selected', formPrefix);
        }
        else {
            // Add the selection to the array and redraw
            selectedSpecies.push(selectedValue);
            appendSpeciesHtml(selectedText, selectedValue, formPrefix, idFieldPrefix, nameFieldPrefix);

        }
        $speciesList.get(0).selectedIndex = 0;
        $(`#${formPrefix}-tree-species-select`).val('');
    }
    
    function removeSpecies(e) {
        e.preventDefault();
        const formPrefix = e.data.formPrefix;

        hideTreeSpeciesSelectionError(formPrefix);

        const species = $(this).closest("tr").first().children("td:first").children("input").val();
        let selectedSpecies;
        if(formPrefix === "felling"){
            selectedSpecies = fellingTreeSpeciesSelections;
        }
        else{
            selectedSpecies = restockingTreeSpeciesSelections;
        }
        const index = selectedSpecies.indexOf(species);
        if (index > -1) { // only splice array when item is found
            selectedSpecies.splice(index, 1); // 2nd parameter means remove one item only
        }
        $(this).closest("tr").remove();

        const $tableRows = $(`#${formPrefix}-species-list-table tr`);
        const rowCount = $tableRows.length - 1;
        if (rowCount > 0) {
            $(`#${formPrefix}-add-tree-species-btn`).text("Add another species");
        } else {
            $(`#${formPrefix}-add-tree-species-btn`).text("Add species");
        }
    }
    
    function appendSpeciesHtml(selectedText, selectedValue, formPrefix, idFieldPrefix, nameFieldPrefix){
        const $speciesRow = $("<tr/>").addClass("govuk-table__row");        
        $speciesRow.append($("<td/>")
            .addClass("govuk-table__cell")
            .attr("data-order", selectedText)
            .text(selectedText).append($("<input/>").attr("type", "hidden")
                .attr("data-val","true")
                .attr("id",`${idFieldPrefix}_${selectedValue}__Species`)
                .attr("name",`${nameFieldPrefix}[${selectedValue}].Species`)
                .val(selectedValue))
            .append($("<input/>").attr("type", "hidden")
                .attr("data-val","true")
                .attr("id",`${idFieldPrefix}_${selectedValue}__SpeciesName`)
                .attr("name",`${nameFieldPrefix}[${selectedValue}].SpeciesName`)
                .val(selectedText))
        );
        if (formPrefix === "restocking") {
            $speciesRow.append($("<td/>").addClass("govuk-table__cell")
                .append($("<input/>")
                    .addClass("govuk-input govuk-date-input__input govuk-input--width-4")
                    .attr("type", "number")
                    .attr("step", "0.01")
                    .attr("id", `${idFieldPrefix}_${selectedValue}__Percentage`)
                    .attr("name", `${nameFieldPrefix}[${selectedValue}].Percentage`)
                    .val(selectedValue)));
        }
        $speciesRow.append($("<td/>").addClass("govuk-table__cell")
            .append($("<a/>").addClass(`govuk-link ${formPrefix}-remove-tree-species`)
            .attr("href", "#")
            .text("Remove")
                .click({ formPrefix: formPrefix }, removeSpecies)
            .append($("<span/>").addClass('govuk-visually-hidden').html('&nbsp;' + selectedText.toLowerCase())))
        );
        $speciesRow.appendTo(`#${formPrefix}-species-list-table tbody`);
    }

    function showTreeSpeciesSelectionError(errorMessage, formPrefix) {
        $(`#${formPrefix}-tree-species-selection-form-group`).addClass('govuk-form-group--error');
        $(`#${formPrefix}-tree-species-selection-error`).text('');

        $(`#${formPrefix}-tree-species-selection-error`)
            .append($("<span/>").addClass("govuk-visually-hidden").text("Error:"));

        $(`#${formPrefix}-tree-species-selection-error`).append(errorMessage);

        $(`#${formPrefix}-tree-species-error`).remove();
    }

    function hideTreeSpeciesSelectionError(formPrefix) {
        $(`#${formPrefix}-tree-species-selection-form-group`).removeClass('govuk-form-group--error');
        $(`#${formPrefix}-tree-species-selection-error`).text('');
        $(`#${formPrefix}-tree-species-selection-error`)
            .append($("<span/>").addClass("govuk-visually-hidden").attr("aria-hidden", "true").text('Error:'));

        $(`#${formPrefix}-tree-species-error`).remove();
    }

});

