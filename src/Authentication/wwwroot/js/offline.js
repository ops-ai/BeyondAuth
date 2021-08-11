window.addEventListener("load", () => {
    function handleNetworkChange(event) {
        if (navigator.onLine) {
            location.reload();
        }
    }

    window.addEventListener("online", handleNetworkChange);
    window.addEventListener("offline", handleNetworkChange);
});