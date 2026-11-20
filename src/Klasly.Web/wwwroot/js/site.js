// Forms with data-confirm="..." ask before submitting (used for Deactivate, Drop, Unlink).
// After a confirmed, valid POST the submit button is disabled and shows its data-pending-text (default "Saving…"),
// so a double click can't post twice. jQuery Validation cancels invalid submits before this runs (defaultPrevented).
document.addEventListener('submit', function (event) {
    var form = event.target;
    var message = form.dataset.confirm;
    if (message && !window.confirm(message)) event.preventDefault();
    if (event.defaultPrevented) return;

    form.dataset.submitted = 'true'; // tells the unsaved-changes guard below that leaving is intended
    if ((form.getAttribute('method') || 'get').toLowerCase() !== 'post') return;

    var button = event.submitter || form.querySelector('[type=submit]');
    if (!button || button.disabled) return;
    // Wait one tick so the browser has already collected the form data (a disabled button would be left out).
    setTimeout(function () {
        button.dataset.pendingOriginal = button.innerHTML;
        button.textContent = button.dataset.pendingText || 'Saving…';
        button.disabled = true;
    }, 0);
});

// Back/forward cache can restore a page with its buttons still disabled; put them back.
window.addEventListener('pageshow', function (event) {
    if (!event.persisted) return;
    document.querySelectorAll('[data-pending-original]').forEach(function (button) {
        button.innerHTML = button.dataset.pendingOriginal;
        button.disabled = false;
        delete button.dataset.pendingOriginal;
    });
    document.querySelectorAll('form[data-submitted]').forEach(function (form) {
        delete form.dataset.submitted;
    });
});

// <form data-warn-unsaved> asks before leaving the page with typed but unsaved changes (grade sheet, account form).
document.addEventListener('input', function (event) {
    var form = event.target.form;
    if (form && form.hasAttribute('data-warn-unsaved')) form.dataset.dirty = 'true';
});

window.addEventListener('beforeunload', function (event) {
    if (document.querySelector('form[data-warn-unsaved][data-dirty]:not([data-submitted])')) {
        event.preventDefault();
        event.returnValue = '';
    }
});
