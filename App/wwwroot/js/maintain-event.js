(() => {
    const form = document.querySelector("[data-event-form]");
    const confirmation = form?.querySelector("#ConfirmOverlap");
    if (!confirmation) return;

    const resetConfirmation = event => {
        if (event.target.name !== "ConfirmOverlap") confirmation.checked = false;
    };
    form.addEventListener("input", resetConfirmation);
    form.addEventListener("change", resetConfirmation);
})();
