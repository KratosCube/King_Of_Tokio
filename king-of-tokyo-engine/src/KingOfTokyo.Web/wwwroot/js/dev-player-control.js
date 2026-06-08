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
            button.textContent = session.PlayerId === playerId ? 'Controlling' : 'Control';
            button.disabled = session.PlayerId === playerId;
        };

        updateText();
        button.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();

            const session = readSession();
            session.PlayerId = playerId;
            writeSession(session);
            window.location.reload();
        });

        card.appendChild(button);
    };

    const installControlButtons = () => {
        document.querySelectorAll('.monster-card.display-card')
            .forEach(installControlButton);
    };

    const observer = new MutationObserver(installControlButtons);
    observer.observe(document.documentElement, { childList: true, subtree: true });

    document.addEventListener('DOMContentLoaded', installControlButtons);
    installControlButtons();
})();
