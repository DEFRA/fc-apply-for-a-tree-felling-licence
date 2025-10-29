$(function () {
    const $correctFormsRadio = $('input[name="AreTheFormsCorrect"]');
    const $submitEiaCheck = $('#submit-eia-check');
    const $sendReminderContainer = $('#send-reminder-container');
    const $inLineCheck = $('input[name="EiaProcessInLineWithCode"]');
    const $trackerReference = $('input[name="EiaTrackerReferenceNumber"]');

    function setHidden($element) {
        $($element).hide();
        $($element).attr("aria-hidden", "true");
    }

    function setVisible($element) {
        $($element).show();
        $($element).removeAttr("aria-hidden");
    }

    function setRadioState($radio, enabled) {
        $radio.prop('disabled', !enabled);

        $radio.prop('hidden', !enabled);
        if (!enabled) {
            $radio.prop('checked', false);
        }
    }
    function handleCorrectFormsChanged(value, reset) {
        const yesSelected = value === 'True';

        setHidden($submitEiaCheck);
        setHidden($sendReminderContainer);

        if (reset === true) {
            $inLineCheck.prop('checked', false);
            $trackerReference.val("");
        }

        if (yesSelected) {
            setVisible($submitEiaCheck);
        } else {
            setVisible($sendReminderContainer);
        }
    }

    function initialiseRadios() {
        handleCorrectFormsChanged($correctFormsRadio.filter(':checked').val(), false);
    }

    $correctFormsRadio.on('change', function () {
        handleCorrectFormsChanged($(this).val(), true);
    });

    $(document).ready(initialiseRadios);
});