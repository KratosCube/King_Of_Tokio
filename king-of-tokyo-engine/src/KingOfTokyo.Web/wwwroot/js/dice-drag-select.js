(() => {
    let isDraggingDice = false;
    let dragStartedOnDiceRow = false;
    let dragStartButton = null;
    let draggedAcrossDice = false;
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
            draggedAcrossDice = false;
            return;
        }

        isDraggingDice = true;
        dragStartedOnDiceRow = true;
        dragStartButton = button;
        draggedAcrossDice = false;
    });

    document.addEventListener('pointerover', (event) => {
        if (!isDraggingDice || !dragStartedOnDiceRow) {
            return;
        }

        const button = findDieButton(event);
        if (!isSelectableDieButton(button) || button === dragStartButton) {
            return;
        }

        draggedAcrossDice = true;
        clickIfNotSelected(dragStartButton);
        clickIfNotSelected(button);
        suppressNextNativeClick = true;
    });

    document.addEventListener('pointerup', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        dragStartButton = null;
        draggedAcrossDice = false;
    });

    document.addEventListener('pointercancel', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        dragStartButton = null;
        draggedAcrossDice = false;
        suppressNextNativeClick = false;
        isSyntheticDieClick = false;
    });
})();
