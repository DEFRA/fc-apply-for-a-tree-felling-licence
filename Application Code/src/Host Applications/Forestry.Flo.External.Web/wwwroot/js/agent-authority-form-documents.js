$(function () {

    document.getElementsByName('remove-agent-authority-form').forEach(e => {
        e.addEventListener('click', function (e) {
            $('input[name=agentAuthorityFormId]').val(e.target.dataset.id);
        });
    })
});
