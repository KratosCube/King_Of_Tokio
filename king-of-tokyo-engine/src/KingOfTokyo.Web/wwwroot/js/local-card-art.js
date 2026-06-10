(() => {
    const explicitCardIdsByName = new Map([
        ["we're only making it stronger", "card-were-only-making-it-stronger"]
    ]);

    let modal;
    let modalImage;
    let modalTitle;

    function normalize(text) {
        return (text || "").replace(/\s+/g, " ").trim();
    }

    function slugifyCardName(name) {
        const normalized = normalize(name).toLowerCase();
        if (explicitCardIdsByName.has(normalized)) {
            return explicitCardIdsByName.get(normalized);
        }

        const slug = normalized
            .replace(/[’']/g, "")
            .replace(/&/g, " and ")
            .replace(/[^a-z0-9]+/g, "-")
            .replace(/^-+|-+$/g, "");

        return slug ? `card-${slug}` : "";
    }

    function ensureModal() {
        if (modal) {
            return modal;
        }

        modal = document.createElement("div");
        modal.className = "card-art-modal";
        modal.setAttribute("aria-hidden", "true");

        const backdrop = document.createElement("button");
        backdrop.className = "card-art-modal-backdrop";
        backdrop.type = "button";
        backdrop.setAttribute("aria-label", "Close card detail");
        backdrop.addEventListener("click", closeModal);

        const dialog = document.createElement("section");
        dialog.className = "card-art-modal-dialog";
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        dialog.setAttribute("aria-labelledby", "card-art-modal-title");

        const closeButton = document.createElement("button");
        closeButton.className = "card-art-modal-close";
        closeButton.type = "button";
        closeButton.textContent = "×";
        closeButton.setAttribute("aria-label", "Close card detail");
        closeButton.addEventListener("click", closeModal);

        modalTitle = document.createElement("h2");
        modalTitle.id = "card-art-modal-title";

        modalImage = document.createElement("img");
        modalImage.alt = "Card detail";

        dialog.append(closeButton, modalTitle, modalImage);
        modal.append(backdrop, dialog);
        document.body.append(modal);

        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && modal?.classList.contains("open")) {
                closeModal();
            }
        });

        return modal;
    }

    function openModal(cardName, cardId) {
        ensureModal();
        modalTitle.textContent = cardName;
        modalImage.src = `images/cards/${cardId}.jpg`;
        modalImage.alt = `${cardName} card detail`;
        modal.classList.add("open");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("card-art-modal-open");
    }

    function closeModal() {
        if (!modal) {
            return;
        }

        modal.classList.remove("open");
        modal.setAttribute("aria-hidden", "true");
        document.body.classList.remove("card-art-modal-open");
    }

    function wireDetailButton(button) {
        if (!button || button.dataset.cardArtDetailWired === "true") {
            return;
        }

        const cardId = button.dataset.cardId;
        const cardName = button.dataset.cardName || button.getAttribute("aria-label") || cardId;
        if (!cardId) {
            return;
        }

        button.addEventListener("click", () => openModal(cardName, cardId));
        button.dataset.cardArtDetailWired = "true";
    }

    function wireMarketCard(card) {
        if (card.dataset.cardArtDetailWired === "true") {
            return;
        }

        card.addEventListener("click", event => {
            if (event.target?.closest?.("button, a, input, select, textarea")) {
                return;
            }

            const cardId = card.dataset.cardId;
            const cardName = card.dataset.cardName;
            if (cardId && cardName) {
                openModal(cardName, cardId);
            }
        });

        card.dataset.cardArtDetailWired = "true";
    }

    function enhanceMarketCards() {
        for (const card of document.querySelectorAll(".market-card")) {
            const title = card.querySelector(".market-card-header h3");
            if (!title) {
                card.classList.remove("card-art-surface");
                card.style.removeProperty("--card-art-url");
                delete card.dataset.cardId;
                delete card.dataset.cardName;
                continue;
            }

            const cardName = normalize(title.textContent);
            const cardId = slugifyCardName(cardName);
            if (!cardId) {
                continue;
            }

            card.classList.add("card-art-surface");
            card.style.setProperty("--card-art-url", `url("../images/cards/${cardId}.jpg")`);
            card.dataset.cardId = cardId;
            card.dataset.cardName = cardName;
            wireMarketCard(card);
        }
    }

    function enhanceKeepCards() {
        for (const button of document.querySelectorAll(".keep-card-art[data-card-id]")) {
            wireDetailButton(button);
        }
    }

    function enhanceAllCards() {
        enhanceMarketCards();
        enhanceKeepCards();
    }

    function startObserver() {
        ensureModal();
        enhanceAllCards();

        const observer = new MutationObserver(() => enhanceAllCards());
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true
        });

        window.tokyoDebug?.log?.("card-art.enhancer-loaded", {
            format: "jpg",
            pathTemplate: "images/cards/{card-id}.jpg",
            detailModal: true,
            keepCards: true,
            mutatesBlazorMarketDom: false
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startObserver, { once: true });
    } else {
        startObserver();
    }
})();
