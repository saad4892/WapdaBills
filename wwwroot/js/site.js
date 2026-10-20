(function () {
    const form = document.getElementById("bill-form");
    if (!form) {
        return;
    }

    const regionInput = document.getElementById("Region");
    const companySelect = document.getElementById("Company");
    const searchByInput = document.getElementById("SearchBy");
    const number = document.getElementById("Number");
    const ruRow = document.getElementById("ru-row");
    const keLine = document.getElementById("ke-line");
    const pitcFields = document.getElementById("pitc-fields");
    const pitcMeta = document.getElementById("pitc-meta");
    const submitBtn = document.getElementById("open-bill");
    const advancedToggle = document.getElementById("advanced-toggle");
    const advancedPanel = document.getElementById("advanced-panel");

    const modal = document.getElementById("ref-help-modal");
    const openHelp = document.getElementById("ref-help-open");
    const closeHelp = document.getElementById("ref-help-close");
    const dismissHelp = document.getElementById("ref-help-dismiss");
    let lastHelpFocus = null;

    function t(key, fallback) {
        if (window.WapdaI18n && typeof window.WapdaI18n.t === "function") {
            const value = window.WapdaI18n.t(key);
            if (value && value !== key) return value;
        }
        return fallback;
    }

    function selectedOption() {
        if (!companySelect) return null;
        return companySelect.options[companySelect.selectedIndex] || null;
    }

    function digitCount(value) {
        return String(value || "").replace(/\D/g, "").length;
    }

    function syncSearchBy() {
        if (!searchByInput || !number) return;
        const digits = digitCount(number.value);
        const mode = digits > 0 && digits < 14 ? "appno" : "refno";
        searchByInput.value = mode;
        if (ruRow) {
            ruRow.hidden = mode === "appno";
        }
    }

    function syncCompany() {
        const option = selectedOption();
        const code = option && option.value ? option.value : "";
        const isPitc = !code || (option && option.dataset.pitc === "true");
        const isKe = !!code && !isPitc;

        if (regionInput && option && option.dataset.region) {
            regionInput.value = option.dataset.region;
        }

        if (pitcFields) {
            pitcFields.hidden = isKe;
        }
        if (keLine) {
            keLine.hidden = !isKe;
        }
        if (pitcMeta) {
            pitcMeta.hidden = isKe;
        }
        if (advancedToggle) {
            advancedToggle.hidden = isKe;
            if (isKe && advancedPanel && !advancedPanel.hidden) {
                setAdvancedOpen(false);
            }
        }
        if (submitBtn && !submitBtn.disabled) {
            if (!code) {
                submitBtn.textContent = t("home.checkBill", "Check Bill");
            } else if (isKe) {
                submitBtn.textContent = t("home.openKe", "Open K-Electric");
            } else {
                submitBtn.textContent = t("home.checkBill", "Check Bill");
            }
        }
        syncSearchBy();
    }

    function setAdvancedOpen(open) {
        if (!advancedToggle || !advancedPanel) return;
        advancedToggle.setAttribute("aria-expanded", open ? "true" : "false");
        advancedPanel.hidden = !open;
        advancedToggle.classList.toggle("is-open", open);
    }

    if (advancedToggle && advancedPanel) {
        setAdvancedOpen(false);
        advancedToggle.addEventListener("click", function () {
            const open = advancedToggle.getAttribute("aria-expanded") !== "true";
            setAdvancedOpen(open);
        });
    }

    function ensureAlert(message) {
        let alertBox = form.querySelector(".alert");
        if (!alertBox) {
            alertBox = document.createElement("div");
            alertBox.className = "alert";
            alertBox.setAttribute("role", "alert");
            const grid = form.querySelector(".bill-tool__grid");
            if (grid) {
                form.insertBefore(alertBox, grid);
            } else {
                form.prepend(alertBox);
            }
        }
        alertBox.textContent = message;
        alertBox.hidden = false;
        return alertBox;
    }

    if (companySelect) {
        companySelect.addEventListener("change", syncCompany);
    }
    if (number) {
        number.addEventListener("input", syncSearchBy);
        number.addEventListener("change", syncSearchBy);
    }

    form.addEventListener("submit", function (event) {
        const option = selectedOption();
        const code = option && option.value ? option.value : "";
        if (!code) {
            event.preventDefault();
            ensureAlert("Select a distribution company first.");
            companySelect.focus();
            return;
        }

        if (option.dataset.pitc !== "true") {
            return;
        }

        const digits = digitCount(number.value);
        syncSearchBy();
        const mode = searchByInput.value;
        const needed = mode === "appno" ? 8 : 14;
        if (digits < needed) {
            event.preventDefault();
            ensureAlert(mode === "appno"
                ? "Enter a customer ID (at least 8 digits)."
                : "Enter the 14-digit reference number.");
            number.focus();
            return;
        }

        submitBtn.disabled = true;
        submitBtn.textContent = t("home.checking", "Checking bill...");
    });

    function openModal() {
        if (!modal) return;
        lastHelpFocus = document.activeElement;
        if (typeof modal.showModal === "function") {
            modal.showModal();
        } else {
            modal.setAttribute("open", "");
        }
        const focusTarget = closeHelp || dismissHelp;
        if (focusTarget) focusTarget.focus();
    }

    function closeModal() {
        if (!modal) return;
        if (typeof modal.close === "function") {
            modal.close();
        } else {
            modal.removeAttribute("open");
        }
        if (lastHelpFocus && typeof lastHelpFocus.focus === "function") {
            lastHelpFocus.focus();
        }
    }

    if (openHelp) openHelp.addEventListener("click", openModal);
    if (closeHelp) closeHelp.addEventListener("click", closeModal);
    if (dismissHelp) dismissHelp.addEventListener("click", closeModal);
    if (modal) {
        modal.addEventListener("click", function (event) {
            if (event.target === modal) closeModal();
        });
        modal.addEventListener("cancel", function (event) {
            event.preventDefault();
            closeModal();
        });
    }

    // Honor /?company=&region= deep-links from SEO landings / nav CTAs.
    (function applyDeepLink() {
        const params = new URLSearchParams(window.location.search);
        const company = (params.get("company") || "").toLowerCase();
        if (company && companySelect) {
            const match = Array.from(companySelect.options).find(function (opt) {
                return opt.value.toLowerCase() === company;
            });
            if (match) {
                companySelect.value = match.value;
            }
        }
        const region = params.get("region");
        if (region && regionInput) {
            regionInput.value = region;
        }
        if (window.location.hash === "#bill-form" || company) {
            form.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    })();

    document.addEventListener("wapda:langchange", syncCompany);
    syncCompany();
})();
