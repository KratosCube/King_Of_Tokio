(() => {
    const storageKey = 'king-of-tokyo.client-session';
    const apiBaseUrl = 'http://localhost:5000';

    const readSession = () => {
        try {
            const raw = localStorage.getItem(storageKey);
            return raw ? JSON.parse(raw) : {};
        } catch {
            return {};
        }
    };

    const readCurrentPlayerId = (session) => {
        const value = session.PlayerId ?? session.playerId;
        return Number.isInteger(value) ? value : null;
    };

    const writeSession = (session) => {
        localStorage.setItem(storageKey, JSON.stringify(session));
    };

    const readPlayerId = (card) => {
        const details = Array.from(card.querySelectorAll('small'))
            .map((element) => element.textContent ?? '')
            .find((text) => text.includes('Player #'));

        const match = details?.match(/Player #(\d+)/);
        return match ? Number.parseInt(match[1], 10) : null;
    };

    const readGameId = () => {
        const match = window.location.pathname.match(/\/games\/([0-9a-fA-F-]{36})/);
        return match ? match[1] : null;
    };

    const readActivePlayerIdFromPage = () => {
        const currentTurnText = Array.from(document.querySelectorAll('.game-hero p'))
            .map((element) => element.textContent ?? '')
            .find((text) => text.includes('Current turn:'));

        if (currentTurnText) {
            const monsterName = currentTurnText
                .replace(/^.*Current turn:\s*/i, '')
                .split('•')[0]
                .trim()
                .toLowerCase();

            const matchingCard = Array.from(document.querySelectorAll('.monster-card.display-card'))
                .find((card) => {
                    const name = card.querySelector('strong')?.textContent?.trim().toLowerCase();
                    return name && name === monsterName;
                });

            if (matchingCard) {
                return readPlayerId(matchingCard);
            }
        }

        return null;
    };

    const postJson = async (url, payload) => {
        const absoluteUrl = url.startsWith('http') ? url : `${apiBaseUrl}${url}`;
        const response = await fetch(absoluteUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload ?? {})
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        return response.json();
    };

    const reloadWithCacheBust = () => {
        const url = new URL(window.location.href);
        url.searchParams.set('controlPlayerRefresh', Date.now().toString());
        window.location.replace(url.toString());
    };

    const setBadge = (card, className, text) => {
        let badge = card.querySelector(`.${className}`);
        if (!badge) {
            badge = document.createElement('span');
            badge.className = `dev-monster-badge ${className}`;
            card.insertBefore(badge, card.firstChild);
        }

        badge.textContent = text;
    };

    const clearBadge = (card, className) => {
        card.querySelector(`.${className}`)?.remove();
    };

    const updateMonsterStateIndicators = () => {
        const controlledPlayerId = readCurrentPlayerId(readSession());
        const activePlayerId = readActivePlayerIdFromPage();

        document.querySelectorAll('.monster-card.display-card')
            .forEach((card) => {
                const playerId = readPlayerId(card);
                card.classList.toggle('dev-controlled-monster', playerId === controlledPlayerId);
                card.classList.toggle('dev-active-turn-monster', playerId === activePlayerId);

                if (playerId === controlledPlayerId) {
                    setBadge(card, 'dev-controlled-badge', '🎮 You control');
                } else {
                    clearBadge(card, 'dev-controlled-badge');
                }

                if (playerId === activePlayerId) {
                    setBadge(card, 'dev-active-turn-badge', '⚡ Active turn');
                } else {
                    clearBadge(card, 'dev-active-turn-badge');
                }
            });
    };

    const installControlButton = (card) => {
        if (!(card instanceof HTMLElement) || card.dataset.controlButtonInstalled === 'true') {
            return;
        }

        const playerId = readPlayerId(card);
        if (!Number.isInteger(playerId)) {
            return;
        }

        card.dataset.controlButtonInstalled = 'true';
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-secondary dev-control-player-button';

        const updateText = () => {
            const session = readSession();
            const currentPlayerId = readCurrentPlayerId(session);
            button.textContent = currentPlayerId === playerId ? 'Controlling' : 'Control';
            button.disabled = currentPlayerId === playerId;
        };

        updateText();
        button.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();

            const session = readSession();
            session.PlayerId = playerId;
            session.playerId = playerId;
            writeSession(session);
            reloadWithCacheBust();
        });

        card.appendChild(button);
    };

    const installControlButtons = () => {
        document.querySelectorAll('.monster-card.display-card')
            .forEach(installControlButton);
    };

    const shouldAutoHandleAdvance = (button) =>
        button instanceof HTMLButtonElement &&
        button.textContent?.trim().toLowerCase() === 'advance player' &&
        !button.disabled;

    const installAdvanceHelper = () => {
        document.querySelectorAll('button')
            .forEach((button) => {
                if (!(button instanceof HTMLButtonElement) || button.dataset.devAdvanceHelperInstalled === 'true') {
                    return;
                }

                if (button.textContent?.trim().toLowerCase() !== 'advance player') {
                    return;
                }

                button.dataset.devAdvanceHelperInstalled = 'true';
                button.addEventListener('click', async (event) => {
                    if (!shouldAutoHandleAdvance(button)) {
                        return;
                    }

                    const gameId = readGameId();
                    const controlledPlayerId = readCurrentPlayerId(readSession());
                    if (!gameId || controlledPlayerId === null) {
                        return;
                    }

                    event.preventDefault();
                    event.stopImmediatePropagation();
                    button.disabled = true;
                    button.textContent = 'Advancing...';

                    try {
                        const advanceResult = await postJson(`/api/games/${gameId}/commands/advance-player`, { actorPlayerId: controlledPlayerId });
                        const currentPlayerIndex = advanceResult?.gameState?.currentPlayerIndex;
                        const currentTurnPhase = advanceResult?.gameState?.currentTurn?.phase;
                        const canBeginControlledTurn = currentPlayerIndex === controlledPlayerId &&
                            (!currentTurnPhase || currentTurnPhase === 'Finished');

                        if (canBeginControlledTurn) {
                            await postJson(`/api/games/${gameId}/commands/begin-turn`, { actorPlayerId: controlledPlayerId });
                        }
                    } finally {
                        reloadWithCacheBust();
                    }
                }, true);
            });
    };

    const installDevHelpers = () => {
        installControlButtons();
        installAdvanceHelper();
        updateMonsterStateIndicators();
    };

    const observer = new MutationObserver(installDevHelpers);
    observer.observe(document.documentElement, { childList: true, subtree: true });

    document.addEventListener('DOMContentLoaded', installDevHelpers);
    installDevHelpers();
})();
