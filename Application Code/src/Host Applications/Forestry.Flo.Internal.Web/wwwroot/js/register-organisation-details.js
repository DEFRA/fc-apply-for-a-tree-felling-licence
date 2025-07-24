$(function () {
    const line1 = $('#ContactAddress_Line1');
    const line2 = $('#ContactAddress_Line2');
    const line3 = $('#ContactAddress_Line3');
    const line4 = $('#ContactAddress_Line4');
    const postalCode = $('#ContactAddress_PostalCode');
    const addressesMatchCheckBox = $('#ContactAddressMatchesOrganisationAddress');
    const contactAddressContainer = $('#contact-address-container');

    const orgLine1 = $('#OrganisationAddress_Line1');
    const orgLine2 = $('#OrganisationAddress_Line2');
    const orgLine3 = $('#OrganisationAddress_Line3');
    const orgLine4 = $('#OrganisationAddress_Line4');
    const orgPostalCode = $('#OrganisationAddress_PostalCode');

    addressesMatchCheckBox.on('change', showHideSection);

    $(document).ready(showHideSection);

    function showHideSection() {

        var isChecked = addressesMatchCheckBox.prop('checked');

        if (isChecked) {
            line1.val(orgLine1.val());
            line2.val(orgLine2.val());
            line3.val(orgLine3.val());
            line4.val(orgLine4.val());
            postalCode.val(orgPostalCode.val());
            contactAddressContainer.addClass('govuk-visually-hidden');

            $("li:contains('contact address')").hide();

        } else {
            line1.val('');
            line1.removeClass('govuk-visually-hidden');
            line2.val('');
            line3.val('');
            line4.val('');
            postalCode.val('');
            contactAddressContainer.removeClass('govuk-visually-hidden');
        }
    }

    function copyAddressIfChecked() {
        var isChecked = addressesMatchCheckBox.prop('checked');

        if (isChecked) {
            line1.val(orgLine1.val());
            line2.val(orgLine2.val());
            line3.val(orgLine3.val());
            line4.val(orgLine4.val());
            postalCode.val(orgPostalCode.val());
        }
    }

    orgLine1.on('input change keyup', copyAddressIfChecked);
    orgLine2.on('input change keyup', copyAddressIfChecked);
    orgLine3.on('input change keyup', copyAddressIfChecked);
    orgLine4.on('input change keyup', copyAddressIfChecked);
    orgPostalCode.on('input change keyup', copyAddressIfChecked);
});