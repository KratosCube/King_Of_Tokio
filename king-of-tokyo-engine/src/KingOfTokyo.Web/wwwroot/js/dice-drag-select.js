(() => {
    let isDraggingDice = false;
    let dragStartedOnDiceRow = false;

    const isSelectableDieButton = (element) =>
        element instanceof HTMLElement &&
        element.classList.contains('die-button') &&
        !element.disabled;

    const clickIfNotSelected = (button) => {
        if (!isSelectableDieButton(button) || button.classList.contains('selected')) {
            return;
        }

        button.click();
    };

    document.addEventListener('pointerdown', (event) => {
        const button = event.target instanceof Element ? event.target.closest('.die-button') : null;
        if (!isSelectableDieButton(button)) {
            isDraggingDice = false;
            dragStartedOnDiceRow = false;
            return;
        }

        isDraggingDice = true;
        dragStartedOnDiceRow = true;
    });

    document.addEventListener('pointerover', (event) => {
        if (!isDraggingDice || !dragStartedOnDiceRow) {
            return;
        }

        const button = event.target instanceof Element ? event.target.closest('.die-button') : null;
        clickIfNotSelected(button);
    });

    document.addEventListener('pointerup', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
    });

    document.addEventListener('pointercancel', () => {
        isDraggingDice = false;
        dragStartedOnDiceRow = false;
    });
})();
