(() => {
    /**
     * Main class.
     */
    class Base {

        /**
         * Initializes a new instance of Base.
         */
        constructor() {
            $(document).ready(() => {
                this.onPageReady();
            });
        }

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            this.setupBindings();
        }

        /**
         * Setup all basic bindings for all modules.
         */
        setupBindings() {
            // Determine global position of all tooltips
            $(".info-link").each(function () {
                if ($(this).offset().left > ($(window).width() / 2)) {
                    $(this).addClass("right");
                }
            });
        }
    }

    // Initialize the Base class and make one instance of it globally available.
    window.base = new Base();
})();