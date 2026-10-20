(function () {
    var STORAGE_KEY = "wapda_lang";
    var dict = null;
    var current = "en";

    function normalize(lang) {
        return lang === "ur" ? "ur" : "en";
    }

    function readStored() {
        try {
            return normalize(localStorage.getItem(STORAGE_KEY) || "en");
        } catch (e) {
            return "en";
        }
    }

    function writeStored(lang) {
        try {
            localStorage.setItem(STORAGE_KEY, lang);
        } catch (e) { /* ignore */ }
    }

    function applyDocumentLang(lang) {
        var html = document.documentElement;
        html.setAttribute("lang", lang);
        html.setAttribute("dir", lang === "ur" ? "rtl" : "ltr");
        html.classList.toggle("lang-ur", lang === "ur");
        html.classList.toggle("lang-en", lang === "en");
    }

    function textFor(key) {
        if (!dict) return null;
        var pack = dict[current] || dict.en || {};
        if (pack[key] != null) return pack[key];
        if (dict.en && dict.en[key] != null) return dict.en[key];
        return null;
    }

    function applyTranslations() {
        document.querySelectorAll("[data-i18n]").forEach(function (el) {
            var key = el.getAttribute("data-i18n");
            if (!key) return;
            var value = textFor(key);
            if (value == null) return;
            el.textContent = value;
        });
        document.querySelectorAll("[data-i18n-html]").forEach(function (el) {
            var key = el.getAttribute("data-i18n-html");
            var value = textFor(key);
            if (value == null) return;
            el.innerHTML = value;
        });
        document.querySelectorAll("[data-i18n-placeholder]").forEach(function (el) {
            var key = el.getAttribute("data-i18n-placeholder");
            var value = textFor(key);
            if (value == null) return;
            el.setAttribute("placeholder", value);
        });
        document.querySelectorAll("[data-i18n-aria]").forEach(function (el) {
            var key = el.getAttribute("data-i18n-aria");
            var value = textFor(key);
            if (value == null) return;
            el.setAttribute("aria-label", value);
        });
        document.querySelectorAll("[data-i18n-title]").forEach(function (el) {
            var key = el.getAttribute("data-i18n-title");
            var value = textFor(key);
            if (value == null) return;
            el.setAttribute("title", value);
        });

        syncSwitcherUi();
        syncBillSubmitLabel();
    }

    function syncSwitcherUi() {
        document.querySelectorAll("[data-lang-switch]").forEach(function (root) {
            root.querySelectorAll("[data-set-lang]").forEach(function (btn) {
                var lang = normalize(btn.getAttribute("data-set-lang"));
                var on = lang === current;
                btn.setAttribute("aria-pressed", on ? "true" : "false");
                btn.classList.toggle("is-on", on);
            });
        });
    }

    function syncBillSubmitLabel() {
        var btn = document.getElementById("open-bill");
        var companySelect = document.getElementById("Company");
        if (!btn || !companySelect) return;
        if (btn.disabled) {
            var checking = textFor("home.checking");
            if (checking) btn.textContent = checking;
            return;
        }
        var option = companySelect.options[companySelect.selectedIndex];
        var code = option && option.value ? option.value : "";
        var isPitc = !code || (option && option.dataset.pitc === "true");
        var key = !code || isPitc ? "home.checkBill" : "home.openKe";
        var label = textFor(key);
        if (label) btn.textContent = label;
    }

    function setLanguage(lang, persist) {
        current = normalize(lang);
        if (persist !== false) writeStored(current);
        applyDocumentLang(current);
        applyTranslations();
        document.dispatchEvent(new CustomEvent("wapda:langchange", { detail: { lang: current } }));
    }

    function bindSwitchers() {
        document.querySelectorAll("[data-set-lang]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                setLanguage(btn.getAttribute("data-set-lang"), true);
            });
        });
    }

    function boot(data) {
        dict = data || { en: {}, ur: {} };
        current = readStored();
        applyDocumentLang(current);
        applyTranslations();
        bindSwitchers();
        window.WapdaI18n = {
            getLang: function () { return current; },
            setLang: function (lang) { setLanguage(lang, true); },
            t: function (key) { return textFor(key) || key; },
            refresh: applyTranslations
        };
    }

    current = readStored();
    applyDocumentLang(current);

    var url = "/i18n/translations.json";
    fetch(url, { credentials: "same-origin" })
        .then(function (res) { return res.ok ? res.json() : Promise.reject(); })
        .then(boot)
        .catch(function () { boot({ en: {}, ur: {} }); });
})();
