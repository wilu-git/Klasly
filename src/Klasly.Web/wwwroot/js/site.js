// Forms with data-confirm="..." ask before submitting (used for Deactivate, Drop, Unlink).
document.addEventListener('submit', function (event) {
    var message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) event.preventDefault();
});
