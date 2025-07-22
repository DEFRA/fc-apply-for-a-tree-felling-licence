const adminName = document.getElementById('name-field-input').value;
const address = document.getElementById('address-field-input').value;
const managerName = document.getElementById('admin-hub-managers').value;

$(function () {
    $(document).ready(setEditMode);
});

function setEditMode() {
    $('#cancel-edit-options').hide();

    $('#edit-name-field').hide();
    $('#edit-manager-field').hide();

    $('#save-admin-hub-button').hide();
    var editOptions = document.getElementById("enable-edit-options");
    editOptions.addEventListener("click",
        function (event) {
            $(this).hide();
            $('#cancel-edit-options').show();
            $('#edit-name-field').show();
            $('#name-field').hide();
            $('#edit-address-field').show();
            $('#address-field').hide();

            var adminHubInput = document.querySelector('#name-field-input');
            adminHubInput.addEventListener('change', (event) => {
                enableSaveAdminHubDetails();
            });

            var addressInput = document.querySelector('#address-field-input');
            addressInput.addEventListener('change', (event) => {
                enableSaveAdminHubDetails();
            });

            $('#edit-manager-field').show();
            $('#manager-field').hide();
            var managerSelect = document.querySelector('#admin-hub-managers');
            managerSelect.addEventListener('change', (event) => {
                enableSaveAdminHubDetails();
            });

            $('#save-admin-hub-button').show();
            $("#save-admin-hub-button").attr("disabled", true);
        }, false);
}

function enableSaveAdminHubDetails() {
    if (adminName !== document.getElementById('name-field-input').value
        || address !== document.getElementById('address-field-input').value
        || managerName !== document.getElementById('admin-hub-managers').value) {
        $("#save-admin-hub-button").attr("disabled", false);
    }
    else {
        $("#save-admin-hub-button").attr("disabled", true);
    }
}

function filterTableByAdminHub() {
    var table = document.getElementById("user-list-table");

    var value = '';
    $('button.selected').each(function () {
        value += $(this).attr('data-id') + "|";
    });

    // Loop through all table rows, and hide those who don't match the search query
    for (var i = 1, row; row = table.rows[i]; i++) {
        var adminHubCell = row.cells[3];
        if (value == "") {
            row.style = "";
        }
        else if (adminHubCell) {
            if (value.includes(adminHubCell.firstChild.data)) {
                row.style.display = "";
            }
            else {
                row.style.display = "none";
            }
        }
    }
};

$('button.filter').each(function () {
    $(this).on('click', function () {
        if ($(this).hasClass('selected')) {
            $(this).removeClass('selected');
            $(this).addClass('unselected');
        } else {
            $(this).removeClass('unselected');
            $(this).addClass('selected');
        }
        filterTableByAdminHub();
    });
});