(() => {
    let isDraggingDice = false;
    let dragStartedOnDiceRow = false;
    let suppressNextDieClick = false;
    let dragStartButton = null;
    let draggedAcrossDice = false;

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

        button.click();
        return true;
    };

    document.addEventListener('click', (event) => {
        const button = findDieButton(event);
        if (!isSelectableDieButton(button)) {
            return;
        }

        if (suppressNextDieClick || draggedAcrossDice) {
            event.preventDefault();
            event.stopImmediatePropagation();
            suppressNextDieClick = false;
            draggedAcrossDice = false;
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

        suppressNextDieClick = clickIfNotSelected(button);
    });

    document.addEventListener('pointerover', (event) => {
        if (!isDraggingDice || !dragStartedOnDiceRow) {
            return;
        }

        const button = findDieButton(event);
        if (button !== dragStartButton && isSelectableDieButton(button)) {
            draggedAcrossDice = true;
        }

        clickIfNotSelected(button);
    });

    document.addEventListener('pointerup', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        dragStartButton = null;
    });

    document.addEventListener('pointercancel', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
        suppressNextDieClick = false;
        dragStartButton = null;
        draggedAcrossDice = false;
    });
})();
