// Small page helpers. Every page still works without them.

// Shows or hides the role-specific fields on the register page.
document.querySelectorAll("[data-role-fields]").forEach(form => {
    const update = () => {
        const role = form.querySelector("input[name='Role']:checked")?.value;
        form.querySelectorAll("[data-for-role]").forEach(group => {
            group.hidden = group.dataset.forRole !== role;
        });
    };
    form.querySelectorAll("input[name='Role']").forEach(input => input.addEventListener("change", update));
    update();
});

// Shows a preview of an image before it is uploaded.
document.querySelectorAll("input[type='file'][data-preview]").forEach(input => {
    input.addEventListener("change", () => {
        const preview = document.getElementById(input.dataset.preview);
        const file = input.files?.[0];
        if (!preview || !file) return;
        preview.src = URL.createObjectURL(file);
        preview.hidden = false;
    });
});

// Asks before a delete form is submitted.
document.querySelectorAll("form[data-confirm]").forEach(form => {
    form.addEventListener("submit", event => {
        if (!window.confirm(form.dataset.confirm)) event.preventDefault();
    });
});
