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

    function createArtFrame(cardName, cardType, cost, cardId) {
        const frame = document.createElement("button");
        frame.className = "card-art-frame market-card-art";
        frame.type = "button";
        frame.dataset.cardId = cardId;
        frame.dataset.cardName = cardName;
        frame.setAttribute("aria-label", `Show ${cardName} card detail`);
        wireDetailButton(frame);

        const image = document.createElement("img");
        image.src = `images/cards/${cardId}.jpg`;
        image.alt = `${cardName} card art`;
        image.loading = "lazy";
        image.addEventListener("error", () => {
            frame.closest(".card-art-surface")?.classList.add("missing-card-art");
            image.remove();
        }, { once: true });

        const fallback = document.createElement("div");
        fallback.className = "card-art-fallback";

        const title = document.createElement("strong");
        title.textContent = cardName;

        const meta = document.createElement("small");
        meta.textContent = `${cardType || "Card"}${cost ? ` • Cost ${cost}` : ""}`;

        fallback.append(title, meta);
        frame.append(image, fallback);
        return frame;
    }

    function enhanceMarketCards() {
        for (const card of document.querySelectorAll(".market-card")) {
            if (card.dataset.localCardArtEnhanced === "true") {
                continue;
            }

            const title = card.querySelector(".market-card-header h3");
            if (!title) {
                card.dataset.localCardArtEnhanced = "true";
                continue;
            }

            const cardName = normalize(title.textContent);
            const cardId = slugifyCardName(cardName);
            if (!cardId) {
                card.dataset.localCardArtEnhanced = "true";
                continue;
            }

            const cardType = normalize(card.querySelector(".market-card-type")?.textContent);
            const costText = normalize(card.querySelector(".market-cost")?.textContent).replace(/^⚡\s*/, "");
            const frame = createArtFrame(cardName, cardType, costText, cardId);

            card.classList.add("card-art-surface");
            card.prepend(frame);
            card.dataset.localCardArtEnhanced = "true";
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
            subtree: true
        });

        window.tokyoDebug?.log?.("card-art.enhancer-loaded", {
            format: "jpg",
            pathTemplate: "images/cards/{card-id}.jpg",
            detailModal: true,
            keepCards: true
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startObserver, { once: true });
    } else {
        startObserver();
    }
})();
