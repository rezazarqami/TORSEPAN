// Native modal dialogs contain keyboard focus and restore it to the opener.
const initialized = new WeakSet();
export function setOpen(dialog, open) {
    if (!initialized.has(dialog)) {
        // Escape and Android Back must also update the Blazor open state.
        dialog.addEventListener("cancel", event => {
            event.preventDefault();
            dialog.querySelector("[data-navigation-close]").click();
        });
        initialized.add(dialog);
    }
    if (open && !dialog.open) dialog.showModal();
    else if (!open && dialog.open) dialog.close();
}
