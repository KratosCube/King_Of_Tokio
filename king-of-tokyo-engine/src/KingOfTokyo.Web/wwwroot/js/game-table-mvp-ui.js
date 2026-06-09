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

    const duplicateDiceActionLabels = new Set([
        "Reroll selected dice"
    ]);

    function normalize(text) {
        return (text || "").replace(/\s+/g, " ").trim();
    }

    function installStableCleanupStyles() {
        if (document.getElementById("kot-mvp-ui-cleanup-style")) {
            return;
        }

        const style = document.createElement("style");
        style.id = "kot-mvp-ui-cleanup-style";
        style.textContent = `
            .pending-decision-banner {
                display: none !important;
            }
        `;
        document.head.appendChild(style);
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

    function cleanupDuplicateDiceActions() {
        for (const button of document.querySelectorAll(".game-grid .actions button")) {
            const label = normalize(button.textContent);
            if (!duplicateDiceActionLabels.has(label)) {
                continue;
            }

            if (button.closest(".next-action-panel") || button.closest(".developer-panel")) {
                continue;
            }

            button.hidden = true;
            button.setAttribute("aria-hidden", "true");
            button.setAttribute("data-mvp-duplicate-action-hidden", "true");
        }
    }

    function cleanupGameTable() {
        installStableCleanupStyles();
        cleanupNextActionPanel();
        cleanupDuplicateDiceActions();
    }

    function startObserver() {
        cleanupGameTable();

        const observer = new MutationObserver(() => cleanupGameTable());
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true
        });

        window.tokyoDebug?.log?.("game.mvp-ui.cleanup-loaded", {
            hiddenMainActionLabels: Array.from(hiddenMainActionLabels),
            renamedMainActionLabels: Array.from(renameMainActionLabels.entries()),
            duplicateDiceActionLabels: Array.from(duplicateDiceActionLabels),
            hidesPendingDecisionBanner: true
        });
    }

    installStableCleanupStyles();

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startObserver, { once: true });
    } else {
        startObserver();
    }
})();
