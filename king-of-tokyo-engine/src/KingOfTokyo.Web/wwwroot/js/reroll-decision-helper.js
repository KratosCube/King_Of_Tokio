(() => {
    const enableRerollButtonWhenDiceDecisionIsActive = () => {
        const bodyText = document.body?.innerText ?? "";
        if (!bodyText.includes("SelectDiceToReroll")) {
            return;
        }

        const selectedDice = document.querySelectorAll(".die-button.selected");
        if (selectedDice.length === 0) {
            return;
        }

        const buttons = Array.from(document.querySelectorAll("button"));
        const rerollButton = buttons.find(button => button.textContent?.trim() === "Reroll selected dice");
        if (!rerollButton) {
            return;
        }

        rerollButton.removeAttribute("disabled");
        rerollButton.classList.remove("disabled");
    };

    const observer = new MutationObserver(enableRerollButtonWhenDiceDecisionIsActive);

    window.addEventListener("load", () => {
        observer.observe(document.body, {
            attributes: true,
            childList: true,
            subtree: true,
            characterData: true
        });
        enableRerollButtonWhenDiceDecisionIsActive();
    });
})();
