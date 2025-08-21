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

    $('#delete-felling-details-button').click(function () {
        Swal.fire({
            title: 'Do you wish to delete this felling operation and associated restocking operations?',
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
                $("#delete-felling-details").submit();
            }
        });
    });
});