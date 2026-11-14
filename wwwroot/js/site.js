document.addEventListener("DOMContentLoaded", () => {
    const menuButton = document.querySelector("[data-menu-toggle]");
    const navigation = document.querySelector("[data-main-nav]");

    if (menuButton && navigation) {
        menuButton.addEventListener("click", () => {
            navigation.classList.toggle("open");
            menuButton.setAttribute("aria-expanded", navigation.classList.contains("open").toString());
        });
    }
});
