function disableFormElements($formSelector) {

    var $combinedSelector = $formSelector +
        ' input, ' +
        $formSelector +
        ' select, ' +
        $formSelector +
        ' button, ' +
        $formSelector +
        ' textarea';

    $($combinedSelector).attr('disabled', 'disabled');
}