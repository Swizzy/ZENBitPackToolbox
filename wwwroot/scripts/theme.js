window.SetTheme = function (currentTheme) {
    console.log("Current Theme: " + currentTheme);
    const classList = document.body.classList;
    if (currentTheme == "dark") {
        classList.add("dark-theme");
        classList.remove("light-theme");

    } else if (currentTheme == "light") {
        classList.add("light-theme");
        classList.remove("dark-theme");
    }
};
SetTheme(localStorage.getItem("theme") || (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light"));