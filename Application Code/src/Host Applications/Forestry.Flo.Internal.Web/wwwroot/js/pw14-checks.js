$(function () {
    const $tpoRadios = $('input[name="Pw14Checks.TpoOrCaDeclared"]');
    const $interestDeclared = $('input[name="Pw14Checks.InterestDeclared"]');
    const $eiaThresholdExceeded = $('input[name="Pw14Checks.EiaThresholdExceeded"]');

    const $eiaTrackerRadio = $('#eia-tracker-radio');
    const $eiaChecklistRadio = $('#eia-checklist-radio');
    const $recommendationsEnactedRadio = $('#recommendation-enacted-radio');
    const $interestComplianceRadio = $('#interest-compliance-radio');
    const $authorityConsultedRadio = $('#authority-consulted-radio');

    function setRadioState($radio, enabled) {
        $radio.prop('disabled', !enabled);

        $radio.prop('hidden', !enabled);
        if (!enabled) {
            $radio.prop('checked', false);
        }
    }

    function handleTpoChange(value) {
        setRadioState($authorityConsultedRadio, value === 'true');
    }

    function handleInterestDeclaredChange(value) {
        const enabled = value === 'true';
        setRadioState($recommendationsEnactedRadio, enabled);
        setRadioState($interestComplianceRadio, enabled);
    }

    function handleEiaThresholdExceededChange(value) {
        const enabled = value === 'true';
        setRadioState($eiaTrackerRadio, enabled);
        setRadioState($eiaChecklistRadio, enabled);
    }

    function initialiseRadios() {
        handleTpoChange($tpoRadios.filter(':checked').val());
        handleInterestDeclaredChange($interestDeclared.filter(':checked').val());
        handleEiaThresholdExceededChange($eiaThresholdExceeded.filter(':checked').val());
    }

    $tpoRadios.on('change', function () {
        handleTpoChange($(this).val());
    });

    $interestDeclared.on('change', function () {
        handleInterestDeclaredChange($(this).val());
    });

    $eiaThresholdExceeded.on('change', function () {
        handleEiaThresholdExceededChange($(this).val());
    });

    $(document).ready(initialiseRadios);
});