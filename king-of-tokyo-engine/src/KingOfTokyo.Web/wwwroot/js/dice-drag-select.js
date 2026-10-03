(() => {
    let isDraggingDice = false;
    let dragStartedOnDiceRow = false;
    let dragStartButton = null;
    let suppressNextNativeClick = false;
    let isSyntheticDieClick = false;

    const isSelectableDieButton = (element) =>
        element instanceof HTMLElement &&
        element.classList.contains('die-button') &&
        !element.disabled;

    const findDieButton = (event) =>
        event.target instanceof Element ? event.target.closest('.die-button') : null;

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

        if (suppressNextNativeClick) {
            event.preventDefault();
            event.stopImmediatePropagation();
            suppressNextNativeClick = false;
        }
    }, true);

    document.addEventListener('pointerdown', (event) => {
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

        clickIfNotSelected(dragStartButton);
        clickIfNotSelected(button);
        suppressNextNativeClick = true;
    });

    document.addEventListener('pointerup', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        dragStartButton = null;
        // A drag can end outside a die, so the browser may never dispatch a click.
        // Clear suppression after this pointer gesture instead of eating a later click.
        setTimeout(() => { suppressNextNativeClick = false; }, 0);
    });

    document.addEventListener('pointercancel', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        dragStartButton = null;
        suppressNextNativeClick = false;
        isSyntheticDieClick = false;
    });
})();
