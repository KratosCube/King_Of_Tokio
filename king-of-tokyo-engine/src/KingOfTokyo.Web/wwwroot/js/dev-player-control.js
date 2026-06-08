(() => {
    const storageKey = 'king-of-tokyo.client-session';

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

    const postJson = async (url, payload) => {
        const response = await fetch(url, {
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
                    if (!gameId || controlledPlayerId is null) {
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
    };

    const observer = new MutationObserver(installDevHelpers);
    observer.observe(document.documentElement, { childList: true, subtree: true });

    document.addEventListener('DOMContentLoaded', installDevHelpers);
    installDevHelpers();
})();
