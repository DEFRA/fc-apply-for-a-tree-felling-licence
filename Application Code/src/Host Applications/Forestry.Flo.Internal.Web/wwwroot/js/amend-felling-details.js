$(function () {
    $(document).ready(setFellingDetailsControlSet);
    const $operationSelect = $('#ConfirmedFellingRestockingDetails_ConfirmedFellingDetails_OperationType');
    const $isRestockingFormGroup = $('#IsRestocking');
    $operationSelect.on('change', setFellingDetailsControlSet);

    var $currentFellingOption = $operationSelect.find(":selected").val();

    function setFellingDetailsControlSet(){
        const selectedValue = $operationSelect.find(":selected").val();

        // hide restocking form group when thinning is selected as felling operation type
        if (selectedValue === 5 || selectedValue === '5') {
            $isRestockingFormGroup.hide();
        } else {
            $isRestockingFormGroup.show();
        }
    }

    $("#felling-add-tree-species-btn").click(function () {
        const $tableRows = $('#felling-species-list-table tr');
        const rowCount = $tableRows.length - 1;  
        if (rowCount > 0) {
            $(this).text("Add another species");
        } else {
            $(this).text("Add species");
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

    $('#FellingDetail_OperationType').change(function () {
        canCompleteSection();
    });

    document.querySelectorAll('[asp-for="FellingDetail.IsWithinConservationArea"], [asp-for="FellingDetail.IsPartOfTreePreservationOrder"]').forEach(radio => {
        radio.addEventListener('change', function (e) {
            let relatedElement = document.querySelector(`#${this.dataset.ariaControls}`);
            if (this.value === '@true') {
                relatedElement.classList.remove('govuk-radios__conditional--hidden');
            } else {
                relatedElement.classList.add('govuk-radios__conditional--hidden');
            }
        });
    });
});