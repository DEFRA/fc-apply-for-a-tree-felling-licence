$(function () {
    var signaturePadElement = document.getElementById('signature-pad');

    var signaturePad = new SignaturePad(signaturePadElement, {
        backgroundColor: 'rgba(230, 230, 230, 0)',
        penColor: 'rgb(0, 0, 0)'
    });

    var parent = signaturePadElement.parentNode,
        styles = getComputedStyle(parent),
        w = parseInt(styles.getPropertyValue("width"), 10),
        h = parseInt(styles.getPropertyValue("height"), 10);

    signaturePadElement.width = w;
    signaturePadElement.height = h;

    var cancelButton = document.getElementById('clear');
    var approveButton = $('#approve-button');
    var declineButton = $('#decline-button');

    $(approveButton).prop('disabled', true);
    $(declineButton).prop('disabled', true);

    cancelButton.addEventListener('click', function (event) {
        signaturePad.clear();
        $(approveButton).prop('disabled', true);
        $(declineButton).prop('disabled', true);
        $("#SignatureImageData").val("");
    });

    signaturePad.addEventListener("endStroke", () => {
        var imageData = signaturePad.toDataURL();
        var disabledApprovalButton = signaturePad.isEmpty();

        imageData = imageData.replace('data:image/png;base64,', '');
        $("#SignatureImageData").val(imageData);
        $(approveButton).prop('disabled', disabledApprovalButton);
        $(declineButton).prop('disabled', disabledApprovalButton);
    });
});