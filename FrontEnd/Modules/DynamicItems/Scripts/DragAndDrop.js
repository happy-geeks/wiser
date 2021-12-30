require("@progress/kendo-ui/js/kendo.sortable.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

/**
 * Class for drag and drop functionality, such as resizing or moving fields around.
 */
export class DragAndDrop {

    /**
     * Initializes a new instance of the DragAndDrop class.
     * @param {DynamicItems} base An instance of the base class (DynamicItems).
     */
    constructor(base) {
        this.base = base;
        this.elementBeingDragged = null;
    }

    /**
     * Do all initializations for the DragAndDrop class, such as adding bindings.
     */
    initialize() {
        // While changing the width of a field, update the width so the user can always see the current width.
        $("body").on("mousemove", ".item", (event) => {
            if (!$("#widthToggle").prop("checked")) {
                return;
            }

            this.showElementWidth($(event.currentTarget));
        });

        // This is done to remember what element the user started dragging, so that we change the width of this element no matter where the user releases their mouse button.
        // Otherwise if the user starts dragging a certain element and then releases their mouse button while hovering over a different element, the change to the original element would not be saved.
        $("body").on("mousedown", ".item", (event) => {
            if (!$("#widthToggle").prop("checked")) {
                return;
            }

            this.elementBeingDragged = $(event.currentTarget);
        });

        // When releasing the mouse button, round the width to the nearest 5%, snap it to that width and save the new width.
        $("body").on("mouseup", ".item", (event) => {
            if (!$("#widthToggle").prop("checked")) {
                return;
            }

            this.snapFieldContainer($(event.currentTarget));
        });

        // Calculate the width of all field containers once the option has been clicked to enable resizing.
        $("#widthToggle").change((event) => {
            if (!event.currentTarget.checked) {
                return;
            }

            $(".item").each((index, element) => {
                this.showElementWidth($(element));
            });
        });

        // Enable or disable moving of fields based on what option was selected.
        $("input.edit").on("change", (event) => {
            $("input.edit").not(event.currentTarget).prop("checked", false);

            if ($("#editToggle").prop("checked") === true) {
                this.turnSortingOn();
            } else {
                this.turnSortingOff();
            }
        });
    }

    /**
     * Turn the possibility of moving fields around on.
     */
    turnSortingOn() {
        $(".formview").kendoSortable({
            handler: ".handler",
            ignore: "input, textarea, #panelbar",
            placeholder: "<div class='placeholder'>Sleep naar hier</div>",
            hint: (element) => {
                return element.clone().addClass("hint");
            }
        });
    }

    /**
     * Turn the possibility of moving fields around off.
     */
    turnSortingOff() {
        $(".formview").each((index, element) => {
            const kendoSortable = $(element).data("kendoSortable");
            if (!kendoSortable) {
                return;
            }
            kendoSortable.destroy();
        });
    }

    /**
     * Show the width of an element in a little box on the top right of that element, while changing the width.
     * @param {any} element The element to show the width of.
     */
    showElementWidth(element) {
        const parentOuterWidth = element.parent().outerWidth();
        const elementOuterWidth = element.outerWidth();

        if (parentOuterWidth <= 0 || elementOuterWidth <= 0) {
            return;
        }

        const width = Math.round(elementOuterWidth / (parentOuterWidth / 100));
        let widthElement = element.find(".width-text");

        if (!widthElement.length) {
            widthElement = $("<div class='width-text'/>").appendTo(element);
        }

        widthElement.text(width + "%");
    }

    /**
     * Snap a field container to the nearest 5% width interval.
     * @param {any} eventElement The field container
     */
    async snapFieldContainer(eventElement) {
        const element = this.elementBeingDragged || eventElement;
        const previousWidth = parseInt(element.data("width"));
        const parentOuterWidth = element.parent().outerWidth();
        const elementOuterWidth = element.outerWidth();

        if (parentOuterWidth <= 0 || elementOuterWidth <= 0) {
            return;
        }

        const width = elementOuterWidth / (parentOuterWidth / 100);
        const newWidth = Math.round(width / 5) * 5;
        let widthElement = element.find(".width-text");

        if (!widthElement.length) {
            widthElement = $("<div class='width-text'/>").appendTo(element);
        }

        widthElement.text(newWidth + "%");
        element.data("width", newWidth).css("width", newWidth + "%");

        if (!newWidth || previousWidth === newWidth) {
            return;
        }

        try {
            await this.base.fields.updateWidth(element.data("propertyId"), newWidth);
        } catch (exception) {
            console.error(exception);
            kendo.alert(`Er is iets fout gegaan met het opslaan van de nieuwe breedte (${exception.statusText}). Probeer het a.u.b. nogmaals of neem contact op met ons.`);
        }
    }
}