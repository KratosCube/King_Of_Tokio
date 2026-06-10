(() => {
    let isDraggingDice = false;
    let dragStartedOnDiceRow = false;
    let dragStartButton = null;
    let isSyntheticDieClick = false;
    let suppressNativeClickUntil = 0;
    let clearSuppressionTimer = null;

    const faceIcons = new Map([
        ['ONE', '1'],
        ['TWO', '2'],
        ['THREE', '3'],
        ['ENERGY', '⚡'],
        ['HEAL', '❤'],
        ['HEART', '❤'],
        ['ATTACK', '💥'],
        ['CLAW', '💥']
    ]);

    const diceActionTexts = new Set([
        'ROLL DICE',
        'REROLL SELECTED DICE',
        'FINALIZE INSTEAD',
        'FINALIZE DICE'
    ]);

    const isSelectableDieButton = (element) =>
        element instanceof HTMLElement &&
        element.classList.contains('die-button') &&
        !element.disabled;

    const findDieButton = (event) =>
        event.target instanceof Element ? event.target.closest('.die-button') : null;

    const normalizeText = (value) => (value || '').replace(/\s+/g, ' ').trim().toUpperCase();

    const scheduleSuppressionClear = () => {
        window.clearTimeout(clearSuppressionTimer);
        clearSuppressionTimer = window.setTimeout(() => {
            suppressNativeClickUntil = 0;
        }, 180);
    };

    const suppressNativeClickBriefly = () => {
        suppressNativeClickUntil = performance.now() + 180;
        scheduleSuppressionClear();
    };

    const shouldSuppressNativeClick = () => suppressNativeClickUntil > performance.now();

    const normalizeDiceFaces = () => {
        for (const face of document.querySelectorAll('.die-face')) {
            const text = (face.textContent || '').trim();
            const icon = faceIcons.get(text.toUpperCase());
            if (icon && text !== icon) {
                face.textContent = icon;
            }
        }
    };

    const clickIfNotSelected = (button) => {
        if (!isSelectableDieButton(button) || button.classList.contains('selected')) {
            return false;
        }

        isSyntheticDieClick = true;
        try {
            button.click();
        } finally {
            isSyntheticDieClick = false;
        }

        return true;
    };

    const findDiceAnchor = () => document.querySelector('.dice-row') || document.querySelector('.placeholder-box');

    const collectDiceActions = () => {
        const actions = [];

        for (const button of document.querySelectorAll('.next-action-panel .actions button')) {
            const text = normalizeText(button.textContent);
            if (!diceActionTexts.has(text)) {
                continue;
            }

            actions.push({
                text: button.textContent.trim(),
                kind: button.classList.contains('btn-primary') ? 'primary' : 'secondary',
                disabled: button.disabled,
                source: button
            });
        }

        const clearButton = document.querySelector('.dice-row + .actions.compact-actions button');
        if (clearButton) {
            actions.push({
                text: clearButton.textContent.trim(),
                kind: 'secondary',
                disabled: clearButton.disabled,
                source: clearButton
            });
        }

        return actions;
    };

    const ensureDiceActionStrip = () => {
        const anchor = findDiceAnchor();
        const actions = collectDiceActions();
        const hasDiceActions = Boolean(anchor) && actions.some(action => diceActionTexts.has(normalizeText(action.text)) || normalizeText(action.text) === 'CLEAR DICE SELECTION');
        document.body.classList.toggle('dice-actions-mirrored', hasDiceActions);

        const existing = document.querySelector('.dice-action-strip');
        if (!hasDiceActions) {
            existing?.remove();
            return;
        }

        const signature = actions
            .map(action => `${normalizeText(action.text)}:${action.kind}:${action.disabled ? 'disabled' : 'enabled'}`)
            .join('|');

        let strip = existing;
        if (!strip) {
            strip = document.createElement('div');
            strip.className = 'dice-action-strip';
            anchor.insertAdjacentElement('afterend', strip);
        } else if (strip.previousElementSibling !== anchor) {
            anchor.insertAdjacentElement('afterend', strip);
        }

        if (strip.dataset.signature === signature) {
            return;
        }

        strip.dataset.signature = signature;
        strip.replaceChildren();

        for (const action of actions) {
            const proxy = document.createElement('button');
            proxy.type = 'button';
            proxy.className = `btn btn-${action.kind}`;
            proxy.textContent = action.text;
            proxy.disabled = action.disabled;
            proxy.addEventListener('click', () => {
                if (!action.source.disabled) {
                    action.source.click();
                }
            });
            strip.append(proxy);
        }
    };

    const refreshDiceUx = () => {
        normalizeDiceFaces();
        ensureDiceActionStrip();
    };

    document.addEventListener('click', (event) => {
        const button = findDieButton(event);
        if (!isSelectableDieButton(button)) {
            return;
        }

        if (isSyntheticDieClick) {
            return;
        }

        if (shouldSuppressNativeClick()) {
            event.preventDefault();
            event.stopImmediatePropagation();
            suppressNativeClickUntil = 0;
            window.clearTimeout(clearSuppressionTimer);
        }
    }, true);

    document.addEventListener('pointerdown', (event) => {
        refreshDiceUx();

        const button = findDieButton(event);
        if (!isSelectableDieButton(button)) {
            isDraggingDice = false;
            dragStartedOnDiceRow = false;
            dragStartButton = null;
            return;
        }

        isDraggingDice = true;
        dragStartedOnDiceRow = true;
        dragStartButton = button;
    });

    document.addEventListener('pointerover', (event) => {
        if (!isDraggingDice || !dragStartedOnDiceRow) {
            return;
        }

        const button = findDieButton(event);
        if (!isSelectableDieButton(button) || button === dragStartButton) {
            return;
        }

        const selectedStart = clickIfNotSelected(dragStartButton);
        const selectedCurrent = clickIfNotSelected(button);
        if (selectedStart || selectedCurrent) {
            suppressNativeClickBriefly();
        }
    });

    document.addEventListener('pointerup', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        dragStartButton = null;
        scheduleSuppressionClear();
        window.setTimeout(refreshDiceUx, 0);
    });

    document.addEventListener('pointercancel', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        dragStartButton = null;
        suppressNativeClickUntil = 0;
        isSyntheticDieClick = false;
        window.clearTimeout(clearSuppressionTimer);
        window.setTimeout(refreshDiceUx, 0);
    });

    refreshDiceUx();
    const observer = new MutationObserver(() => refreshDiceUx());
    observer.observe(document.body, {
        childList: true,
        subtree: true,
        characterData: true,
        attributes: true,
        attributeFilter: ['disabled', 'class']
    });
})();
