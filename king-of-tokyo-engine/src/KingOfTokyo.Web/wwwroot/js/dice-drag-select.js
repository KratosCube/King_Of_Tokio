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

    const isSelectableDieButton = (element) =>
        element instanceof HTMLElement &&
        element.classList.contains('die-button') &&
        !element.disabled;

    const findDieButton = (event) =>
        event.target instanceof Element ? event.target.closest('.die-button') : null;

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
        normalizeDiceFaces();

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
    });

    document.addEventListener('pointercancel', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        dragStartButton = null;
        suppressNativeClickUntil = 0;
        isSyntheticDieClick = false;
        window.clearTimeout(clearSuppressionTimer);
    });

    normalizeDiceFaces();
    const observer = new MutationObserver(() => normalizeDiceFaces());
    observer.observe(document.body, {
        childList: true,
        subtree: true,
        characterData: true
    });
})();
