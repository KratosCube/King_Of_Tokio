window.tokyoDebug = {
    enabled: window.localStorage.getItem("kot.debug") === "1",
    setEnabled(value) {
        this.enabled = !!value;
        if (this.enabled) {
            window.localStorage.setItem("kot.debug", "1");
            console.info("[KOT DEBUG] enabled");
        } else {
            window.localStorage.removeItem("kot.debug");
            console.info("[KOT DEBUG] disabled");
        }
    },
    log(scope, payload) {
        if (!this.enabled) {
            return;
        }

        const time = new Date().toISOString();
        console.log(`[KOT DEBUG][${time}][${scope}]`, payload);
    },
    dumpStorage() {
        const key = "king-of-tokyo.client-session";
        const snapshot = {
            localStorageSession: window.localStorage.getItem(key),
            sessionStorageSession: window.sessionStorage.getItem(key),
            debugEnabled: this.enabled,
            location: window.location.href
        };
        console.table(snapshot);
        return snapshot;
    },
    clearSession() {
        const key = "king-of-tokyo.client-session";
        window.sessionStorage.removeItem(key);
        console.info("[KOT DEBUG] cleared sessionStorage identity. Reload the page.");
    },
    clearLegacyLocalSession() {
        const key = "king-of-tokyo.client-session";
        window.localStorage.removeItem(key);
        console.info("[KOT DEBUG] cleared legacy localStorage identity.");
    }
};

console.info("[KOT DEBUG] helper loaded. Run tokyoDebug.setEnabled(true), then reload. Use tokyoDebug.dumpStorage() to inspect identity.");
