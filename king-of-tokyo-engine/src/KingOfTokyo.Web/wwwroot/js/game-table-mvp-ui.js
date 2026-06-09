(() => {
    const hiddenMainActionLabels = new Set([
        "Initialize game",
        "Begin turn",
        "Advance player",
        "End turn only"
    ]);

    const renameMainActionLabels = new Map([
        ["End turn & advance", "End turn"]
    ]);

    function normalize(text) {
        return (text || "").replace(/\s+/g, " ").trim();
    }

    function cleanupNextActionPanel() {
        const panel = document.querySelector(".next-action-panel");
        if (!panel) {
            return;
        }

        for (const button of panel.querySelectorAll("button")) {
            const label = normalize(button.textContent);

            if (renameMainActionLabels.has(label)) {
                button.textContent = renameMainActionLabels.get(label);
                button.setAttribute("data-mvp-label", "true");
                continue;
            }

            if (hiddenMainActionLabels.has(label)) {
                button.hidden = true;
                button.setAttribute("aria-hidden", "true");
                button.setAttribute("data-mvp-hidden", "true");
            }
        }
    }

    function startObserver() {
        cleanupNextActionPanel();

        const observer = new MutationObserver(() => cleanupNextActionPanel());
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true
        });

        window.tokyoDebug?.log?.("game.mvp-ui.cleanup-loaded", {
            hiddenMainActionLabels: Array.from(hiddenMainActionLabels),
            renamedMainActionLabels: Array.from(renameMainActionLabels.entries())
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startObserver, { once: true });
    } else {
        startObserver();
    }
})();
