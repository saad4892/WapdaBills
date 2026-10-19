(function () {
    const header = document.getElementById("site-header");
    const toggle = document.getElementById("nav-toggle");
    const drawer = document.getElementById("mobile-drawer");
    const backdrop = document.getElementById("drawer-backdrop");
    const closeBtn = document.getElementById("drawer-close");
    const navItems = Array.from(document.querySelectorAll("[data-nav-item]"));
    let lastFocus = null;

    function setScrolled() {
        if (!header) return;
        header.classList.toggle("is-scrolled", window.scrollY > 4);
    }

    function closeDesktopMenus(except) {
        navItems.forEach(function (item) {
            if (item === except) return;
            const trigger = item.querySelector(".nav-trigger");
            const panel = item.querySelector(".nav-panel");
            if (!trigger || !panel) return;
            trigger.setAttribute("aria-expanded", "false");
            panel.classList.remove("is-open");
            panel.hidden = true;
        });
    }

    function openDesktopMenu(item) {
        closeDesktopMenus(item);
        const trigger = item.querySelector(".nav-trigger");
        const panel = item.querySelector(".nav-panel");
        if (!trigger || !panel) return;
        trigger.setAttribute("aria-expanded", "true");
        panel.hidden = false;
        requestAnimationFrame(function () {
            panel.classList.add("is-open");
        });
    }

    function isDesktop() {
        return window.matchMedia("(min-width: 961px)").matches;
    }

    navItems.forEach(function (item) {
        const trigger = item.querySelector(".nav-trigger");
        const panel = item.querySelector(".nav-panel");
        if (!trigger || !panel) return;

        let hoverTimer = null;

        item.addEventListener("mouseenter", function () {
            if (!isDesktop()) return;
            clearTimeout(hoverTimer);
            openDesktopMenu(item);
        });

        item.addEventListener("mouseleave", function () {
            if (!isDesktop()) return;
            hoverTimer = setTimeout(function () {
                closeDesktopMenus();
            }, 120);
        });

        trigger.addEventListener("click", function (event) {
            event.preventDefault();
            const open = trigger.getAttribute("aria-expanded") === "true";
            if (open) {
                closeDesktopMenus();
            } else {
                openDesktopMenu(item);
                const first = panel.querySelector("a");
                if (first) first.focus();
            }
        });

        trigger.addEventListener("keydown", function (event) {
            if (event.key === "ArrowDown") {
                event.preventDefault();
                openDesktopMenu(item);
                const first = panel.querySelector("a");
                if (first) first.focus();
            }
            if (event.key === "Escape") {
                closeDesktopMenus();
                trigger.focus();
            }
        });

        panel.addEventListener("keydown", function (event) {
            const links = Array.from(panel.querySelectorAll("a"));
            const index = links.indexOf(document.activeElement);
            if (event.key === "Escape") {
                event.preventDefault();
                closeDesktopMenus();
                trigger.focus();
            }
            if (event.key === "ArrowDown" && index > -1) {
                event.preventDefault();
                links[(index + 1) % links.length].focus();
            }
            if (event.key === "ArrowUp" && index > -1) {
                event.preventDefault();
                links[(index - 1 + links.length) % links.length].focus();
            }
        });
    });

    function focusables(root) {
        return Array.from(root.querySelectorAll('a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])'))
            .filter(function (el) { return !el.hasAttribute("hidden") && el.offsetParent !== null; });
    }

    function openDrawer() {
        if (!drawer || !toggle || !backdrop) return;
        lastFocus = document.activeElement;
        drawer.hidden = false;
        backdrop.hidden = false;
        requestAnimationFrame(function () {
            drawer.classList.add("is-open");
        });
        toggle.setAttribute("aria-expanded", "true");
        toggle.setAttribute("aria-label", "Close menu");
        document.body.classList.add("nav-open");
        const first = focusables(drawer)[0] || closeBtn;
        if (first) first.focus();
    }

    function closeDrawer() {
        if (!drawer || !toggle || !backdrop) return;
        drawer.classList.remove("is-open");
        toggle.setAttribute("aria-expanded", "false");
        toggle.setAttribute("aria-label", "Open menu");
        document.body.classList.remove("nav-open");
        setTimeout(function () {
            if (!drawer.classList.contains("is-open")) {
                drawer.hidden = true;
                backdrop.hidden = true;
            }
        }, 220);
        if (lastFocus && typeof lastFocus.focus === "function") {
            lastFocus.focus();
        } else {
            toggle.focus();
        }
    }

    if (toggle) {
        toggle.addEventListener("click", function () {
            if (toggle.getAttribute("aria-expanded") === "true") {
                closeDrawer();
            } else {
                openDrawer();
            }
        });
    }
    if (closeBtn) closeBtn.addEventListener("click", closeDrawer);
    if (backdrop) backdrop.addEventListener("click", closeDrawer);

    document.querySelectorAll("[data-accordion]").forEach(function (block) {
        const trigger = block.querySelector(".mobile-accordion__trigger");
        const panel = block.querySelector(".mobile-accordion__panel");
        if (!trigger || !panel) return;
        trigger.addEventListener("click", function () {
            const open = trigger.getAttribute("aria-expanded") === "true";
            trigger.setAttribute("aria-expanded", open ? "false" : "true");
            panel.hidden = open;
        });
    });

    if (drawer) {
        drawer.addEventListener("keydown", function (event) {
            if (event.key !== "Tab") return;
            const items = focusables(drawer);
            if (items.length === 0) return;
            const first = items[0];
            const last = items[items.length - 1];
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        });
    }

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeDesktopMenus();
            if (toggle && toggle.getAttribute("aria-expanded") === "true") {
                closeDrawer();
            }
        }
    });

    document.addEventListener("click", function (event) {
        if (!isDesktop()) return;
        if (!event.target.closest("[data-nav-item]")) {
            closeDesktopMenus();
        }
    });

    window.addEventListener("resize", function () {
        if (isDesktop()) {
            closeDrawer();
        } else {
            closeDesktopMenus();
        }
    });

    window.addEventListener("scroll", setScrolled, { passive: true });
    setScrolled();

    // Footer mobile accordions + back-to-top
    const footerCols = Array.from(document.querySelectorAll("[data-footer-col]"));
    const backTop = document.getElementById("back-to-top");

    function footerMobile() {
        return window.matchMedia("(max-width: 767px)").matches;
    }

    function resetFooterAccordions() {
        footerCols.forEach(function (col) {
            const trigger = col.querySelector(".footer-col__trigger");
            const panel = col.querySelector(".footer-col__panel");
            if (!trigger || !panel) return;
            if (!footerMobile()) {
                trigger.setAttribute("aria-expanded", "false");
                panel.classList.remove("is-open");
                panel.hidden = false;
            } else if (!panel.classList.contains("is-open")) {
                panel.hidden = true;
            }
        });
    }

    footerCols.forEach(function (col) {
        const trigger = col.querySelector(".footer-col__trigger");
        const panel = col.querySelector(".footer-col__panel");
        if (!trigger || !panel) return;
        trigger.addEventListener("click", function () {
            if (!footerMobile()) return;
            const open = trigger.getAttribute("aria-expanded") === "true";
            trigger.setAttribute("aria-expanded", open ? "false" : "true");
            panel.classList.toggle("is-open", !open);
            panel.hidden = open;
        });
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") return;
        const active = document.activeElement && document.activeElement.closest("[data-footer-col]");
        if (!active) return;
        const trigger = active.querySelector(".footer-col__trigger");
        const panel = active.querySelector(".footer-col__panel");
        if (!trigger || !panel) return;
        trigger.setAttribute("aria-expanded", "false");
        panel.classList.remove("is-open");
        panel.hidden = true;
        trigger.focus();
    });

    function syncBackTop() {
        if (!backTop) return;
        const show = window.scrollY > 480;
        backTop.classList.toggle("is-visible", show);
        backTop.hidden = false;
        backTop.setAttribute("aria-hidden", show ? "false" : "true");
        backTop.tabIndex = show ? 0 : -1;
    }

    if (backTop) {
        backTop.addEventListener("click", function () {
            const target = document.getElementById("main") || document.getElementById("site-header") || document.body;
            window.scrollTo({ top: 0, behavior: "smooth" });
            if (target && typeof target.focus === "function") {
                if (!target.hasAttribute("tabindex")) {
                    target.setAttribute("tabindex", "-1");
                }
                setTimeout(function () { target.focus({ preventScroll: true }); }, 350);
            }
        });
        window.addEventListener("scroll", syncBackTop, { passive: true });
        syncBackTop();
    }

    window.addEventListener("resize", resetFooterAccordions);
    resetFooterAccordions();
})();
