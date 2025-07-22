$(function () {

    $(document).ready(function () {
        $('#confirm-felling-and-restocking-table').DataTable({
            fixedHeader: true, // Ensures the table header is fixed.
            paging: false, // Disables pagination.
            scrollCollapse: true, // Allows table height to adjust dynamically.
            scrollX: true, // Enables horizontal scrolling.
            scrollXInner: "100%", // Sets the inner width for scrolling.
            scrollY: 1000, // Sets the maximum height for vertical scrolling.
            searching: false,  // Disables search box
            fixedColumns: { leftColumns: 2 }, // Fixes the first two columns 
            info: false, // Removes the information like "Showing x of y entries".
        });
    });

    $('button[id^=add-felling-species-btn]').each(function() {
        $(this).click(addFellingSpecies); 
    });

    $('button[id^=delete-felling-species-btn]').each(function() {
        $(this).click(deleteFellingSpecies);
    });

    $('button[id^=add-restock-species-btn]').each(function () {
        $(this).click(addRestockSpecies);
    });

    $('button[id^=delete-restock-species-btn]').each(function () {
        $(this).click(deleteRestockSpecies);
    });

    $('select[id$=__OperationType]').each(function() {
        $(this).on('change', setAvailableRestockingOptions);

        $(this).trigger('change');
    });

    function addFellingSpecies(e) {

        e.preventDefault();

        var button = $(e.target);

        var nextSpeciesId = button.attr('data-id');
        var compartmentId = button.attr('data-cpt-id');
        var fellId = button.attr('data-fell-id');

        var divToAddSpeciesInto = $('#felling-species-list-div-' + compartmentId + '-' + fellId);

        var speciesListControl = $('#species-select').clone();

        speciesListControl.attr('id', 'Compartments_' + compartmentId + '-' + fellId + '__ConfirmedFellingSpecies_' + nextSpeciesId + '__Species');
        speciesListControl.attr('data-val', 'true');
        speciesListControl.attr('data-val-required', 'The Species field is required');
        speciesListControl.attr('name', 'Compartments[' + compartmentId + '].ConfirmedFellingDetails[' + fellId + '].ConfirmedFellingSpecies[' + nextSpeciesId + '].Species');

        var newContainer = $('<div>').attr('id', 'felling-container-' + compartmentId + '-' + fellId + '-' + nextSpeciesId).attr('class', 'form-control');

        newContainer.append($('<input />')
            .attr('type', 'hidden')
            .attr('id', 'Compartments_' + compartmentId + '__ConfirmedFellingDetails_' + fellId + '__ConfirmedFellingSpecies_' + nextSpeciesId + '__Deleted')
            .attr('data-val', 'true')
            .attr('name', 'Compartments[' + compartmentId + '].ConfirmedFellingDetails[' + fellId + '].ConfirmedFellingSpecies[' + nextSpeciesId + '].Deleted')
            .attr('value', 'False')
        );

        newContainer.append($('<button>-</button>')
            .attr('id', 'delete-felling-species-btn-' + compartmentId + '-' + fellId + '-' + nextSpeciesId)
            .attr('class', 'govuk-button govuk-button--secondary')
            .attr('data-cpt-id', compartmentId)
            .attr('data-fell-id', fellId)
            .attr('data-id', nextSpeciesId)
            .click(deleteFellingSpecies)
        );

        newContainer.append(speciesListControl);

        newContainer.append($('<input/>')
            .attr('id', 'Compartments_' + compartmentId + '__ConfirmedFellingDetails_' + fellId + '__ConfirmedFellingSpecies_' + nextSpeciesId + '__Percentage')
            .attr('class', 'govuk-input govuk-input--width-4')
            .attr('type', 'text')
            .attr('data-val', 'true')
            .attr('data-val-required', 'The Percentage field is required.')
            .attr('name', 'Compartments[' + compartmentId + '].ConfirmedFellingDetails[' + fellId + '].ConfirmedFellingSpecies[' + nextSpeciesId + '].Percentage')
        );

        newContainer.append($('<input/>')
            .attr('id', 'Compartments_' + compartmentId + '__ConfirmedFellingDetails_' + fellId + '__ConfirmedFellingSpecies_' + nextSpeciesId + '__Volume')
            .attr('class', 'govuk-input govuk-input--width-4')
            .attr('type', 'text')
            .attr('data-val', 'true')
            .attr('data-val-required', 'The Volume field is required.')
            .attr('name', 'Compartments[' + compartmentId + '].ConfirmedFellingDetails[' + fellId + '].ConfirmedFellingSpecies[' + nextSpeciesId + '].Volume')
        );

        newContainer.append($('<br/>'));

        divToAddSpeciesInto.append(newContainer);

        button.attr('data-id', (parseInt(nextSpeciesId) + 1));

    }

    function deleteFellingSpecies(e) {

        e.preventDefault();
        var button = $(e.target);
        var fellId = button.attr('data-fell-id');
        var speciesId = button.attr('data-id');
        var compartmentId = button.attr('data-cpt-id');

        $('#Compartments_' + compartmentId + '__ConfirmedFellingDetails_' + fellId + '__ConfirmedFellingSpecies_' + speciesId + '__Deleted')
            .attr('value', 'True');

        $('#felling-container-' + compartmentId + '-' + fellId + '-' + speciesId).hide();
    }

    function addRestockSpecies(e) {

        e.preventDefault();

        var button = $(e.target);
        var nextSpeciesId = button.attr('data-id');
        var compartmentId = button.attr('data-cpt-id');
        var fellId = button.attr('data-fell-id');
        var reId = button.attr('data-re-id');

        var divToAddSpeciesInto = $('#restock-species-list-div-' + compartmentId + '-' + fellId + '-' + reId);


        var speciesListControl = $('#species-select').clone();

        speciesListControl.attr('id', 'Compartments_' + compartmentId + '__ConfirmedFellingDetails_' + fellId + '__ConfirmedRestockingDetails_' + reId + '__ConfirmedRestockingSpecies_' + nextSpeciesId + '__Species');
        speciesListControl.attr('data-val', 'true');
        speciesListControl.attr('data-val-required', 'The Species field is required');
        speciesListControl.attr('name', 'Compartments[' + compartmentId + '].ConfirmedFellingDetails[' + fellId + '].ConfirmedRestockingDetails[' + reId + '].ConfirmedRestockingSpecies[' + nextSpeciesId + '].Species');

        var newContainer = $('<div>').attr('id', 'restock-container-' + compartmentId + '-' + fellId + '-' + reId + nextSpeciesId).attr('class', 'form-control');

        newContainer.append($('<input />')
            .attr('type', 'hidden')
            .attr('id', 'Compartments_' + compartmentId + '__ConfirmedFellingDetails_' + fellId + '__ConfirmedRestockingDetails_' + reId + '__ConfirmedRestockingSpecies_' + nextSpeciesId + '__Deleted')
            .attr('data-val', 'true')
            .attr('name', 'Compartments[' + compartmentId + '].ConfirmedFellingDetails[' + fellId + '].ConfirmedRestockingDetails[' + reId + '].ConfirmedRestockingSpecies[' + nextSpeciesId + '].Deleted')
            .attr('value', 'False')
        );

        newContainer.append($('<button>-</button>')
            .attr('id', 'delete-restock-species-btn-' + compartmentId + '-' + fellId + '-' + reId + '-' + nextSpeciesId)
            .attr('class', 'govuk-button govuk-button--secondary')
            .attr('data-cpt-id', compartmentId)
            .attr('data-fell-id', fellId)
            .attr('data-re-id', reId)
            .attr('data-id', nextSpeciesId)
            .click(deleteRestockSpecies)
        );

        newContainer.append(speciesListControl);

        newContainer.append($('<input/>')
            .attr('id', 'Compartments_' + compartmentId + '__ConfirmedFellingDetails_' + fellId + '__ConfirmedRestockingDetails_' + reId + '__ConfirmedRestockingSpecies_' + nextSpeciesId + '__Percentage')
            .attr('class', 'govuk-input govuk-input--width-4')
            .attr('type', 'text')
            .attr('data-val', 'true')
            .attr('data-val-required', 'The Percentage field is required.')
            .attr('name', 'Compartments[' + compartmentId + '].ConfirmedFellingDetails[' + fellId + '].ConfirmedRestockingDetails[' + reId + '].ConfirmedRestockingSpecies[' + nextSpeciesId + '].Percentage')
        );

        newContainer.append($('<br/>'));

        divToAddSpeciesInto.append(newContainer);

        button.attr('data-id', (parseInt(nextSpeciesId) + 1));

    }

    function deleteRestockSpecies(e) {

        e.preventDefault();

        var button = $(e.target);
        var speciesId = button.attr('data-id');
        var fellId = button.attr('data-fell-id');
        var reId = button.attr('data-re-id');
        var compartmentId = button.attr('data-cpt-id');

        $('#Compartments_' + compartmentId + '__ConfirmedFellingDetails_' + fellId + '__ConfirmedRestockingDetails_' + reId + '__ConfirmedRestockingSpecies_' + speciesId + '__Deleted')
            .attr('value', 'True');

        $('#restock-container-' + compartmentId + '-' + fellId + '-' + reId + '-' + speciesId).hide();
    }

    $('#visible-import-button').click(function() {
        Swal.fire({
            title: 'Do you wish to overwrite existing confirmed felling and restocking details?',
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
                $("#import-felling-restocking-form").submit();
            } 
        });
    });

    function setAvailableRestockingOptions(e) {
        var fellingOption = $(e.target);

        var cptId = fellingOption.attr('data-id');
        var selectedFellingOption = fellingOption.find(":selected").val();
        var currentRestockOption = $('#Compartments_' + cptId + '__RestockingProposal').find(':selected').val();

        $('#Compartments_' + cptId + '__RestockingProposal option').hide();

        switch(selectedFellingOption) {
            case '1':
                $('#Compartments_' + cptId + '__RestockingProposal option[value=0]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=3]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=5]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=6]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=7]').show();
                break;
            case '2':
                $('#Compartments_' + cptId + '__RestockingProposal option[value=0]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=2]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=3]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=4]').show();
                break;
            case '3':
                $('#Compartments_' + cptId + '__RestockingProposal option[value=0]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=5]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=6]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=7]').show();
                break;
            case '4':
                $('#Compartments_' + cptId + '__RestockingProposal option[value=0]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=2]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=4]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=6]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=7]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=8]').show();
                break;
            case '5':
                $('#Compartments_' + cptId + '__RestockingProposal option[value=0]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=1]').show();
                break;
            case '6':
                $('#Compartments_' + cptId + '__RestockingProposal option[value=0]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=2]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=5]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=6]').show();
                $('#Compartments_' + cptId + '__RestockingProposal option[value=7]').show();
                break;
            default:
                $('#Compartments_' + cptId + '__RestockingProposal option[value=0]').show();
                break;
        }

        var selectedRestockOptionState = $('#Compartments_' + cptId + '__RestockingProposal option[value=' + currentRestockOption + ']').attr('style');

        if (selectedRestockOptionState === 'display: none;') {
            $('#Compartments_' + cptId + '__RestockingProposal option[value=0]').prop('selected', true);
        }
    }
});