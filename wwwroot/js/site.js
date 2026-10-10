(function () {
    const form = document.getElementById("bill-form");
    if (!form) {
        return;
    }

    const regionInput = document.getElementById("Region");
    const detail = document.getElementById("region-detail");
    const title = document.getElementById("selected-company");
    const keNote = document.getElementById("ke-note");
    const pitcFields = document.getElementById("pitc-fields");
    const number = document.getElementById("Number");
    const ruRow = document.getElementById("ru-row");
    const lookup = document.getElementById("lookup-panel");

    function visibleChecked() {
        return document.querySelector('[data-region-panel]:not(.is-out) input[name="Company"]:checked');
    }

    function syncCompany() {
        const checked = visibleChecked();
        if (!checked) {
            title.textContent = "Select a company";
            keNote.hidden = true;
            pitcFields.hidden = false;
            return;
        }

        title.textContent = (checked.dataset.short || "Company") + " bill";
        const isPitc = checked.dataset.pitc === "true";
        keNote.hidden = isPitc;
        pitcFields.hidden = !isPitc;
    }

    function syncSearch() {
        const by = form.querySelector('input[name="SearchBy"]:checked');
        const customer = by && by.value === "appno";
        ruRow.hidden = !!customer;
        number.placeholder = customer ? "10 digit customer ID" : "14 digit reference number";
    }

    function showRegion(key) {
        regionInput.value = key;
        document.querySelectorAll("[data-region-btn]").forEach(function (button) {
            const on = button.dataset.regionBtn === key;
            button.classList.toggle("is-on", on);
            button.setAttribute("aria-pressed", on ? "true" : "false");
        });
        document.querySelectorAll("[data-region-panel]").forEach(function (panel) {
            const on = panel.dataset.regionPanel === key;
            panel.classList.toggle("is-out", !on);
            if (on && detail) {
                detail.textContent = panel.dataset.detail || "";
            }
        });

        const checked = document.querySelector('input[name="Company"]:checked');
        if (checked) {
            const same = document.querySelector('[data-region-panel]:not(.is-out) input[name="Company"][value="' + checked.value + '"]');
            if (same && same !== checked) {
                checked.checked = false;
                same.checked = true;
            } else if (!same) {
                checked.checked = false;
            }
        }

        syncCompany();
    }

    document.querySelectorAll("[data-region-btn]").forEach(function (button) {
        button.addEventListener("click", function () {
            showRegion(button.dataset.regionBtn);
        });
    });

    form.addEventListener("change", function (event) {
        if (event.target.name === "Company") {
            syncCompany();
            lookup.scrollIntoView({ behavior: "smooth", block: "nearest" });
        }
        if (event.target.name === "SearchBy") {
            syncSearch();
        }
    });

    form.addEventListener("submit", function (event) {
        const checked = visibleChecked();
        let alertBox = form.querySelector(".alert");
        function warn(message) {
            event.preventDefault();
            if (!alertBox) {
                alertBox = document.createElement("div");
                alertBox.className = "alert";
                alertBox.setAttribute("role", "alert");
                lookup.insertBefore(alertBox, lookup.firstChild);
            }
            alertBox.textContent = message;
        }

        if (!checked) {
            warn("Select a company first.");
            return;
        }

        if (checked.dataset.pitc !== "true") {
            return;
        }

        const digits = number.value.replace(/\D/g, "");
        const by = form.querySelector('input[name="SearchBy"]:checked').value;
        const needed = by === "appno" ? 8 : 14;
        if (digits.length < needed) {
            warn(by === "appno" ? "Enter a customer ID." : "Enter the 14-digit reference number.");
            return;
        }

        const button = document.getElementById("open-bill");
        button.disabled = true;
        button.textContent = "Opening bill...";
    });

    syncSearch();
    syncCompany();
})();
