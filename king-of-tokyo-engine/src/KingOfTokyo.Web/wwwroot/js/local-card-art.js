(() => {
    const explicitCardIdsByName = new Map([
        ["we're only making it stronger", "card-were-only-making-it-stronger"]
    ]);

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

    function createArtFrame(cardName, cardType, cost, cardId) {
        const frame = document.createElement("div");
        frame.className = "card-art-frame market-card-art";
        frame.dataset.cardId = cardId;

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

    function startObserver() {
        enhanceMarketCards();

        const observer = new MutationObserver(() => enhanceMarketCards());
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });

        window.tokyoDebug?.log?.("card-art.enhancer-loaded", {
            format: "jpg",
            pathTemplate: "images/cards/{card-id}.jpg"
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startObserver, { once: true });
    } else {
        startObserver();
    }
})();
