/*
    Juice Javascript Library
    Core library functions

    Note:
        In the library "core" is used as the main component.
        At the end this is attached to the window as set in setting "libraryName"


    Used abbrivations:
    Fn = Function
    Anon = Anonymous
*/

/*jshint esversion: 6 */

/**
 * Self running function to declarate and attach library
 */
; (function () {
    /**
     * Published library version (Major.Minor.Revision)
     * Every Minor and above needs to be publised. Increase version and lastEditDate on every change when done.
     */
    const version = "1.1.3";
    const lastEditDate = "2018-07-05";

    /**
     * Name of the library as it is attached to the window
     */
    const libraryName = "jjl";

    /**
     * Settings used throughout the library.
     * Placed in core.settings
     */
    const settings = {
        /**
         * Service URL, this is used as default for requests like getJson or getTemplate
         */
        serviceUrl: "",
        doNotDisableScroll: false,
        enableCache: false,
        cacheFilter: "",
        cacheWrapHtmlTemplates: true,
        languageId: 0,
        disableFnStorage: true,
        useHtml5Sql: false
    };

    /**
     * Anonymous function storage.
     * This (array) is used to store anonymous functions, this to use them as reference so they can be unbinded(removed). (normally this is not possible)
     */
    const anonFuncStorage = [];

    let indexedDbStorage = null;

    /**
     * Boolean if the library is currently in a scrolling event, preventing double scrolling events
     */
    let isScrolling = false;

    /**
     * If the DOM and code is ready
     */
    let isReady = false;

    /**
     * Whether debugging is enabled and showing debug logs
     */
    let debuggingEnabled = false;


    let cacheReady = false;

    /**
     * Inner var to set if the popups are enabled on the site. To prevent double initiation
     */
    let popupEnabled = false;

    /**
     * Internal functions, these are used only in the library itself or can be used by its plugins or components
     */
    const internal = {
        /**
         * Stores an anonymous function or increments its usage count (cnt)
         * We want to store anonymous functions so we can reference to them so we can remove the listeners.
         * Bonus is that if we use more than one of the same anonFn we do not store them all but reference to 1.
         * @param {any} fn Anonymous function: ex: function(){console.log(this);}
         */
        storeAnonFn(fn) {
            const existIdx = internal.checkAnonFn(fn);
            if (existIdx !== -1) {
                anonFuncStorage[existIdx].cnt++;
            } else {
                const fnStr = fn.toString();
                anonFuncStorage.push({ key: fnStr, val: fn, cnt: 1 });
            }
        },

        /**
         * Check whether the anonFn(anonymous function) exists and return the index of the AnonFn  
         * @param {any} fn Anonymous function: ex: function(){console.log(this);}
         */
        checkAnonFn(fn) {
            const fnStr = fn.toString();

            for (let i = 0; i < anonFuncStorage.length; i++) {
                if (fnStr === anonFuncStorage[i].key) {
                    return i;
                }
            }
            return -1;
        },

        /**
         * Removes an anonymous function from the anonFn array or decreases count
         * @param {any} fn Anonymous function: ex: function(){console.log(this);}
         */
        removeAnonFn(fn) {
            const existIdx = internal.checkAnonFn(fn);
            if (existIdx !== -1) {
                // Nothing here
                return;
            } else {
                if (!anonFuncStorage[existIdx]) {
                    return;
                }
                anonFuncStorage[existIdx].cnt--;
                if (anonFuncStorage[existIdx].cnt < 1) {
                    delete anonFuncStorage[existIdx];
                }
            }
        },

        /**
         * Locally prevent default to inshure no default is run,or use as a default negate for a listner
         * ex: Following line disables all events for scrolling.
         * window.addEventListener('DOMMouseScroll', internal.preventDefault, false)
         * @param {any} e Eventreturn || window.event
         */
        preventDefault(e = window.event) {
            if (e.preventDefault)
                e.preventDefault();
            e.returnValue = false;
        },

        addGlobalEvents() {
            const timeoutInMs = 750;
            let timeout = false;

            window.addEventListener("resize", () => {
                if (timeout) return;

                timeout = true;
                setTimeout(() => {
                    window.dispatchEvent(new CustomEvent("jjlResize"));
                    timeout = false;
                }, timeoutInMs);
            });


        },

        attachOnclicks() {
            const runActions = function (event) {
                const targetElement = event.currentTarget;
                const actions = targetElement.dataset["jjlOnclick"].split(";");

                for (let i = 0; i < actions.length; i++) {
                    const action = actions[i].split("=")[0];
                    const param = (actions[i].length > 1) ? actions[i].split("=")[1] : "";

                    switch (action) {
                        case "toggleClass":
                            core(targetElement).toggleClass(param);
                            break;
                        case "addClass":
                            core(targetElement).addClass(param);
                            break;
                        case "removeClass":
                            core(targetElement).removeClass(param);
                            break;
                    }
                }
            };

            // existing elements
            core("[data-jjl-onclick]").on("click", runActions);

            // new elements
            core.events.onElementWithDataAttrAdded("jjl-onclick", (collection) => {
                if (collection.length > 0) {
                    collection.on("click", runActions);
                }
            });



        }
    };


    /**
     * Creates CORE element and Function to create a new JllObject, based on string or object given in selector. core(selector);
     * @param {any} selector The selector to create the object from (queryselectorAll call, object, array, html element etc)
     */
    var core = (selector) => {
        if (!selector) {
            //console.warn("Nothing selected.. Selector: is nothing or null");
            return new Collection([], "");
        }
        let elements;
        if (typeof selector === "string") {
            try {
                elements = document.querySelectorAll(selector);
            } catch (err) {
                console.error("Wrong selector! '" + selector + "'", err);
            };
        } else if (selector.length) {
            elements = selector;
        } else {
            elements = [selector];
        }

        if (elements.length === 0) {
            core.debug.warn(`Nothing selected.. Selector: '${selector}'`);
        }

        return new Collection(elements, selector);
    }


    /**
     * Global Settings used throughout the library, get from global var on top;
     */
    core.settings = settings;

    /**
     * Enumerations:
     */
    core.enums = {
        /**
         * Data types, these types can be distinguished with the ".getType" method.
         */
        types: Object.freeze({
            NULL: "NULL",
            STRING: "STRING",
            NUMBER: "NUMBER",
            ARRAY: "ARRAY",
            OBJECT: "OBJECT",
            UNKNOWN: "UNKNOWN",
            UNDEFINED: "UNDEFINED",
            BOOLEAN: "BOOLEAN",
            SYMBOL: "symbol",
            FUNCTION: "FUNCTION",
            HTML_NODE: "HTML_NODE",
            HTML_ELEMENT: "HTML_ELEMENT",
            HTML_INPUT: "HTML_INPUT"
        }),
        /**
         * Data types to use on a XmlHttpRequest, GetData
         */
        xhrDataTypes: Object.freeze({
            RAW: "RAW",
            JSON: "JSON",
            TEMPLATE: "TEMPLATE",
            WEBPAGE: "WEBPAGE",
            API: "API",
            WEBMETHOD: "WEBMETHOD",
            FORM_DATA: "FORM_DATA"
        }),
        browsers: Object.freeze({
            INTERNET_EXPLORER: "INTERNET_EXPLORER",
            CHROME: "CHROME",
            FIREFOX: "FIREFOX",
            SAFARI: "SAFARI",
            OPERA: "OPERA",
            EDGE: "EDGE",
            UNKNOWN: "UNKNOWN"
        }),
        validationTypes: Object.freeze({
            PASSWORD: "PASSWORD",
            TEXT: "TEXT",
            EMAIL: "EMAIL",
            PHONE: "PHONE"
        })
    };

    /**
     * Models:
     * Preconfigured models to use throughout the library
     */
    core.models = {
        /**
         * Response model for a Xhr request | GetJson, GetTemplate, GetData etc..
         */
        XhrResponseModel: class {
            constructor() {
                this.response = "";
                this.responseCode = 0;
                this.isSuccess = "";
                this.value = "";
                this.errorMessage = "";
                this.errorExplain = "";
            }
        },
        /**
         * Request model for XhrRequest, used in GetJson, GetTemplate, GetData etc..
         */
        XhrRequestModel: class {
            constructor() {
                this.url = "";
                this.method = "POST";
                this.identifier = 0;
                this.parameters = {};
                this.requestHeader = "";
                this.withCredentials = false;
                this.parseParamsAs = "auto";
                this.onFailContent = null; // Can be string
                this.onLoadContent = null; // Can be string
                this.onSuccess = core.utils.noop;
                this.onError = core.utils.noop;
                this.onDone = core.utils.noop;
            }
        }
    };

    /**
     * == Plugins ==
     * Component for handling plugin type actions
     */
    core.plugins = {
        /**
         * Adding a plugin to the library, the plugin is attached with classname but first letter lowercase "Class" is attached as "core.class"
         * @param {any} pluginClass Class to implement as plugin
         */
        add(pluginClass, name) {
            const PluginClass = pluginClass;
            let fnName = "";

            if (PluginClass.name) {
                fnName = PluginClass.name.substr(0, 1).toLowerCase() + PluginClass.name.substr(1);
            } else {
                fnName = PluginClass.toString().match(/^function\s*([^\s(]+)/)[1];
                fnName = fnName.substr(0, 1).toLowerCase() + fnName.substr(1);
            }

            if (name && typeof name === "string") {
                fnName = name;
            }

            if (core[fnName]) {
                console.warn(`[${libraryName}] Add Plugin: ERROR name${fnName} already attached..`);
                return false;
            }

            core[fnName] = new PluginClass();
            // Inject core and internal
            core[fnName].core = core;
            core[fnName].internal = internal;

            // Add extensions if there are any

            if (core[fnName].extensions /*&& typeof core[fnName].extensions === "object"*/) Collection.prototype[fnName] = core[fnName].extensions;

            return true;
        }
    }

    // easter egg (if you found this, silently add your answer!)
    core.getCoffee = function () {
        const answers = [
            "Good idea!\n\nOnly i am too busy controlling everything to do that! \nBut if you're going please get me some! ;)",
            "Not now, \n\nI am too busy keeping it all in control.",
            "Again?!\n\nStop joking and get back to work!",
            "I think i should keep serving code, not coffee"
        ];

        alert(answers[Math.floor(Math.random() * answers.length)]);
    }

    core.ready = (onReady = core.utils.noop) => {
        if (isReady) {
            onReady();
        } else {
            document.addEventListener("DOMContentLoaded", onReady);
        }
    };

    /**
     * Debugging methods
     */
    core.debug = {
        log() {
            if (debuggingEnabled) {
                for (let arg of arguments) {
                    console.log(`[${libraryName} debug]`, arg);
                }
            }
        },
        warn() {
            if (debuggingEnabled) {
                for (let arg of arguments) {
                    console.warn(`[${libraryName} debug]`, arg);
                }
            }
        },
        error() {
            if (debuggingEnabled) {
                for (let arg of arguments) {
                    console.error(`[${libraryName} debug]`, arg);
                }
            }
        },
        enable() {
            debuggingEnabled = true;
            console.log(`[${libraryName} debug] enabled`);
        },
        disable() {
            debuggingEnabled = false;
            console.log(`[${libraryName} debug] disabled`);
        },
        isEnabled() {
            return debuggingEnabled;
        }
    };

    /**
     * == Utils ==:
     * Basic utilities
     */
    core.utils = {
        /**
         * No Operation, empty function. note (function(){} == function(){}) IS FALSE! and (core.utils.noop == core.utils.noop) IS TRUE!
         * @returns {} void
         */
        noop() { },

        sleep(sleepTimeInMs = 1000) {
            return new Promise((resolve) => {
                setTimeout(resolve, sleepTimeInMs);
            });
        },

        assert(assertion = true, message = "", resultItem = "[![UnSeT]!]") {
            if (!assertion) {
                if (resultItem !== "[![UnSeT]!]") {
                    console.warn(`Assertion fail: ${message ? `"${message}"` : ""} | Result:`, resultItem);
                } else {
                    console.warn(`Assertion fail: ${message ? `"${message}"` : ""}`);
                }
            }
        },

        removeDuplicatesFromObjectArray(objectArray, identifier) {
            const unique = objectArray
                .map(e => e[identifier])

                // store the keys of the unique objects
                .map((e, i, final) => final.indexOf(e) === i && i)

                // eliminate the dead keys & store unique objects
                .filter(e => objectArray[e]).map(e => objectArray[e]);

            return unique;
        },

        isEmptyObject(obj) {
            for (let name in obj) {
                return false;
            }
            return true;
        },

        /**
         * Loop through items, or do a function on each element
         * @param {element} elements Elements to loop through
         * @param {fn} callback Callback fn to run
         */
        each(elements, callback) {
            if ((elements instanceof Collection) && elements.length === 0) return; // Empty collection
            let setSingle = false;
            if ((elements instanceof Collection) && elements.length === 1) {
                elements = elements[0];
                setSingle = true;
            } // Single element, 

            if (this.isEmptyObject(elements)) return;

            if (core.utils.getType(elements) === core.enums.types.ARRAY && elements.length === 0) return;

            if (!elements.length || setSingle) {
                if (core.utils.getType(elements) === core.enums.types.OBJECT && core.utils.count(elements) > 0) {
                    for (const key in elements) {
                        if (elements.hasOwnProperty(key)) {
                            callback.call(elements[key], key, elements[key]);
                        }
                    }
                } else {
                    callback.call(elements, 0, elements);
                }
                //callback.call(elements, 0, elements);
            } else {
                for (let i = 0; i < elements.length; i++) {
                    callback.call(elements[i], i, elements[i]);
                }
            }
        },

        /**
         * Check whether element is an array
         * @param {element} element
         * @return boolean
         */
        isArray(element) {
            if (Array.isArray) {
                if (Array.isArray(element)) {
                    return true;
                } else {
                    return false;
                }
            } else {
                if (element instanceof Array) {
                    return true;
                } else {
                    return false;
                }
            }
        },

        /**
         * Check if element is html node
         * @param {element} element
         * @return boolean
         */
        isHtmlNode(element) {
            return (
                typeof Node === "object" ? element instanceof Node : element && typeof element === "object" && typeof element.nodeType === "number" && typeof element.nodeName === "string"
            );
        },

        /**
         * Check if element is HTML element
         * @param {element} element
         * @return boolean
         */
        isHtmlElement(element) {
            return (
                typeof HTMLElement === "object" ? element instanceof HTMLElement : //DOM2
                    element && typeof element === "object" && element.nodeType === 1 && typeof element.nodeName === "string"
            );
        },

        /**
         * Check if element is html object
         * @param {element} element
         * @return boolean
         */
        isHtml(element) {
            return (core.utils.isHtmlElement(element) || core.utils.isHtmlNode(element));
        },

        isValidJson(json) {
            try {
                return typeof JSON.parse(json) === "object";
            } catch (err) {
                return false;
            }
        },

        /**
         * Return the element type
         * @param {element} element
         * @return {core.enums.types} Type of the element
         */
        getType(element) {
            const baseType = typeof element;
            switch (baseType) {
                case "undefined":
                    return core.enums.types.UNDEFINED;
                case "boolean":
                    return core.enums.types.BOOLEAN;
                case "number":
                    return core.enums.types.NUMBER;
                case "string":
                    return core.enums.types.STRING;
                case "symbol":
                    return core.enums.types.SYMBOL;
                case "function":
                    return core.enums.types.FUNCTION;
                case "object":
                    if (core.utils.isArray(element)) {
                        return core.enums.types.ARRAY;
                    } else {
                        if (core.utils.isHtmlNode(element)) {
                            if (element.nodeName === undefined) {
                                return core.enums.types.HTML_NODE;
                            }

                            switch (element.nodeName) {
                                case "INPUT":
                                    return core.enums.types.HTML_INPUT;
                                default:
                                    return core.enums.types.HTML_NODE;
                            }
                        } else if (core.utils.isHtmlElement(element)) {
                            return core.enums.types.HTML_ELEMENT;
                        }
                        return core.enums.types.OBJECT;
                    }
                default:
                    return core.enums.types.UNKNOWN;
            }
        },

        /**
         * Counts the elements, object or array, returns number.
         * forceCount param forces to count and not use length
         * @param {any} element
         * @param {boolean} countObjectsOnly Whether to count objects only or (default) count all including properties
         * @return {number} number of elements
         */
        count(element, countObjectsOnly = false) {
            // If object is array, just return lenght
            if (core.utils.isArray(element)) {
                if (!element.length) {
                    console.warn(`[${libraryName}:count] Array is not countable!`);
                    return 0;
                }

                if (!countObjectsOnly) {
                    return element.length;
                }

                let cnt = 0;
                for (let i = 0; i < element.length; i++) {
                    if (typeof element[i] === "object") {
                        ++cnt;
                    }
                }
                return cnt;
            }

            // Else lets count ;)
            if (core.utils.getType(element) === core.enums.types.OBJECT) {
                let count = 0;

                for (const prop in element) {
                    if (element.hasOwnProperty(prop)) {
                        if (!(countObjectsOnly && typeof element[prop] !== "object"))
                            count++;
                    }
                }

                return count;
            } else {
                console.warn("[jcl:count] Wrong type, this is not countable!");
                return 0;
            }
        },

        /**
         * Counts the elements, object or array, returns number.
         * forceCount param forces to count and not use length
         * @param {any} element
         * @return {number} number of elements
         */
        countType(element, enumType) {
            if (!(enumType in core.enums.types)) {
                console.log(`[${libraryName}:count] Please use 'jjl.enum.types'`);
                return 0;
            }

            // If object is array, just return lenght
            if (core.utils.isArray(element)) {
                if (!element.length) {
                    console.warn(`[${libraryName}:count] Array is not countable!`);
                    return 0;
                }

                let cnt = 0;
                for (let i = 0; i < element.length; i++) {
                    if (core.utils.getType(element[i]) === enumType) {
                        ++cnt;
                    }
                }
                return cnt;
            }

            // Else lets count ;)
            if (core.utils.getType(element) === core.enums.types.OBJECT) {
                let count = 0;

                for (const prop in element) {
                    if (element.hasOwnProperty(prop)) {
                        if (core.utils.getType(element[prop]) === enumType)
                            count++;
                    }
                }

                return count;
            } else {
                console.warn(`[${libraryName}:count] Wrong type, this is not countable!`);
                return 0;
            }
        },

        /**
         * Checks if element has a class
         * @param {element} element Element to check
         * @param {string} className Classname to check 
         * @param {boolean} strict If true all elements in collection must have this class for a positive result. False (default) any element with class returns true
         * @return {boolean} Result whether elements have the class
         */
        hasClass(element, className, strict = false) {
            if (typeof element === "string") {
                element = core(element);
            }

            let hasClass = false;
            let strictFail = false;

            core.utils.each(element, function () {
                if (this.classList) {
                    if (this.classList.contains(className)) {
                        hasClass = true;
                    } else {
                        if (strict) {
                            strictFail = true;
                        }
                    }
                } else {
                    if (this.className && !!this.className.match(new RegExp(`(\\s|^)${className}(\\s|$)`))) {
                        hasClass = true;
                    } else {
                        if (strict) {
                            strictFail = true;
                        }
                    }
                }
            });
            return (strictFail) ? false : hasClass;
        },

        /**
         * Adding a class to one or more elements
         * @param {any} element the element
         * @param {any} className The classname to add
         */
        addClass(element, className) {
            if (typeof element === "string") {
                element = core(element);
            }

            core.utils.each(element, function () {
                const classes = className.split(" ");

                for (let i = 0; i < classes.length; i++) {
                    if (this.classList)
                        this.classList.add(classes[i]);
                    else if (!core.utils.hasClass(this, classes[i])) classes[i] += ` ${classes[i]}`;
                }
            });
        },

        /**
         * Remove a class from an element or elements
         * @param {any} element The element to check 
         * @param {any} className The classname to remove
         */
        removeClass(element, className) {
            if (typeof element === "string") {
                element = core(element);
            }

            core.utils.each(element, function () {
                const classes = className.split(" ");

                for (let i = 0; i < classes.length; i++) {
                    if (this.classList)
                        this.classList.remove(classes[i]);
                    else if (this.hasClass && this.hasClass(this, classes[i])) {
                        const reg = new RegExp(`(\\s|^)${classes[i]}(\\s|$)`);
                        this.className = this.className.replace(reg, " ");
                    }
                }
            });
        },

        toggleClass(element, className) {
            if (typeof element === "string") {
                element = core(element);
            }

            core.utils.each(element, function () {
                const cElement = core(this);
                if (!cElement.hasClass(className)) {
                    cElement.addClass(className);
                } else {
                    cElement.removeClass(className);
                }
            });
        },

        clone(obj = {}) {
            const clonedObject = {};

            const handle = (ref = {}, newObj = {}) => {
                //const reference = newObj;
                for (const key in ref) {
                    if (ref.hasOwnProperty(key)) {
                        const value = ref[key];
                        if (typeof value !== "object") {
                            console.log("addKey");
                            newObj[key] = value;
                            continue;
                        }
                        console.log("go deep");
                        // for objects, go deep
                        const tempObj = handle(value, ref);
                        console.log("->", tempObj);
                    }
                }

                console.log("pass back: ", newObj);
                return newObj;
            };

            handle(obj);
            return clonedObject;
        },

        /**
         * Extend an object with another object
         * @argument {objects} Objects to extend  
         * @return {object} new extended object
         */
        extend() {
            for (let i = 1; i < arguments.length; i++)
                for (const key in arguments[i])
                    if (arguments[i].hasOwnProperty(key))
                        arguments[0][key] = arguments[i][key];
            return arguments[0];
        },

        /**
         * Extends an object with another but only replaces properties that already exist in base model
         *  @argument {objects} Objects to extend
         * @return {object} new extended object
         */
        extendStrict() {
            for (let i = 1; i < arguments.length; i++) {
                for (const key in arguments[i]) {
                    if (arguments[i].hasOwnProperty(key) && arguments[0].hasOwnProperty(key)) {
                        arguments[0][key] = arguments[i][key];
                    }
                }
            }
            return arguments[0];
        },

        /**
         * Extend an object with another object
         * @argument {objects} Objects to extend  
         * @return {object} new extended object
         */
        extendInsensitive() {
            for (let i = 1; i < arguments.length; i++)
                for (const key in arguments[i])
                    if (arguments[i].hasOwnProperty(key)) {
                        const keys = Object.keys(arguments[0]);
                        const matchedKey = keys.find(k => k.toLowerCase() === key.toLowerCase());
                        let oriKey = key;

                        if (matchedKey !== undefined && typeof matchedKey === "string") {
                            oriKey = matchedKey;
                        }
                        arguments[0][oriKey] = arguments[i][key];
                    }
            return arguments[0];
        },
        /**
         * Extends an object with another but only replaces properties that already exist in base model
         *  @argument {objects} Objects to extend
         * @return {object} new extended object
         */
        extendStrictInsensitive() {
            for (let i = 1; i < arguments.length; i++) {
                for (const key in arguments[i]) {
                    if (arguments[i].hasOwnProperty(key)) {
                        const keys = Object.keys(arguments[0]);
                        const matchedKey = keys.find(k => k.toLowerCase() === key.toLowerCase());
                        let oriKey = key;
                        if (arguments[0].hasOwnProperty(key)) {
                            arguments[0][oriKey] = arguments[i][key];
                        } else if (matchedKey !== undefined && typeof matchedKey === "string") {
                            oriKey = matchedKey;
                            arguments[0][oriKey] = arguments[i][key];
                        }
                    }
                }
            }
            return arguments[0];
        },

        /**
         * Fill element with content. Ex. inputs, htmlDOM elements etc
         * @param {element} element Element or Identifier
         * @param {string} content Content to fill element with
         */
        fill(element, content) {
            if (typeof element === "string") {
                element = core(element);
            }

            core.utils.each(element, function () {
                switch (core.utils.getType(this)) {
                    case core.enums.types.HTML_INPUT:
                        if (this.value !== undefined) {
                            try {
                                this.value = content;
                            } catch (err) {
                                core.debug.warn("Fill: Unable to fill element..", err);
                            }
                        } else {
                            core.debug.warn("Fill: Unable to fill element..");
                        }
                        break;
                    default:
                        if (this.innerHTML !== undefined) {
                            try {
                                this.innerHTML = content;
                            } catch (err) {
                                core.debug.warn("Fill: Unable to fill element..", err);
                            }
                        } else {
                            core.debug.warn("Fill: Unable to fill element..");
                        }
                }
            });
        },

        /**
         * Fill an element or elements with a Wiser template Use (element, identifier, parameters) or (element, XhrRequestModel)
         * @param {elements} element Element(s) to fill
         * @param {core.models.XhrRequestModel} identifierOrxhrRequestModel core.models.XhrRequestModel
         * @param {object} parameters Object with parameters 
         */
        fillWithTemplate(element, identifierOrxhrRequestModel, parameters, onDone = core.utils.noop) {
            if (typeof element === "string") {
                element = core(element);
            }

            if (identifierOrxhrRequestModel instanceof core.models.XhrRequestModel || core.utils.getType(identifierOrxhrRequestModel) === core.enums.types.OBJECT) {
                if (identifierOrxhrRequestModel.onLoadContent !== false && identifierOrxhrRequestModel.onLoadContent !== null) {
                    core.utils.fill(element, identifierOrxhrRequestModel.onLoadContent);
                    onDone();
                }
            }

            core.data.getDataByType(core.enums.xhrDataTypes.TEMPLATE, identifierOrxhrRequestModel, parameters, function () {
                const template = this;
                core.utils.fill(element, template);
                onDone();
            });
        },

        /**
         * Fill an html element with a webpage from wiser
         * @param {any} element Element to fill
         * @param {any} identifierOrxhrRequestModel webpage identifier of xhrmodel
         */
        fillWithWebpage(element, identifierOrxhrRequestModel, onDone = core.utils.noop) {
            if (typeof element === "string") {
                element = core(element);
            }

            if (identifierOrxhrRequestModel instanceof core.models.XhrRequestModel || core.utils.getType(identifierOrxhrRequestModel) === core.enums.types.OBJECT) {
                if (identifierOrxhrRequestModel.onLoadContent !== false) {
                    core.utils.fill(element, identifierOrxhrRequestModel.onLoadContent);
                    onDone();
                }
            }

            core.data.getDataByType(core.enums.xhrDataTypes.WEBPAGE, identifierOrxhrRequestModel, null, function () {
                const template = this;
                core.utils.fill(element, template);
                onDone();
            });
        },

        /**
         * Get the first element or empty object
         * @param {elements} element Elements
         * @returns {element} the first element
         */
        first(element) {
            if (typeof element === "string") {
                element = core(element);
            }

            if (element.length === 0) {
                core.debug.warn("First: There are no elements..");
                return {};
            }
            return element[0];
        },
        /**
         * Get the last element or empty object
         * @param {elements} element Elements
         * @returns {element} the last element
         */
        last(element) {
            if (typeof element === "string") {
                element = core(element);
            }

            if (element.length === 0) {
                core.debug.warn("Last: There are no elements..");
                return {};
            };
            return element[element.length - 1];
        },

        /**
         * Returns if the collection has an element
         * @param {collection} element collection
         * @param {elememnt} subElement the element
         * @returns {boolean} whether element is in collection
         */
        contains(element, subElement) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            let match = false;

            let search = (element.length === 1) ? element[0] : element;

            // If only string is given and no elements
            if (search instanceof Collection && search.length === 0) {
                search = element.selector || "";
            }


            if (typeof search === "string" && typeof subElement === "string") {
                match = new RegExp(subElement).test(search);
            } else {
                core.utils.each(search, (key, val) => {
                    core.debug.log("contains: " + val + " === " + subElement);
                    if (val === subElement) {
                        match = true;
                        return;
                    }
                });
            }
            return match;
        },

        /**
         * Returns if the collection has an element (inversed with contains)
         * @param {element} element the element
         * @param {collection} subElements collection
         * @returns {boolean} whether element is in collection
         */
        in(element, subElements) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            let result = false;
            core.utils.each(element, (key, val) => {
                if (core.utils.contains(subElements, val)) {
                    result = true;
                }
            });
            return result;
        },

        /**
         * Check if an element exists
         * @param {any} identifier identifier or element
         * @returns {boolean} Whether the element exists
         */
        exists(identifier) {
            return (document.querySelectorAll(identifier).length !== 0);
        },

        /**
         * Appends content to an element
         * @param {any} element base element
         * @param {any} contentToAppend Content to append
         */
        append(element, contentToAppend) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            if (typeof contentToAppend === "string") {
                element.each(function () {
                    this.insertAdjacentHTML("beforeend", contentToAppend);
                });
            } else {
                element.each(function () {
                    this.appendChild(contentToAppend.cloneNode(true));
                });
            }
        },

        /**
         * Gets the content of an object/element, if more elements it returns an array of contents
         * @param {elements} element Element or collection
         * @param {boolean} forceSingle Only get content of first object
         * @returns Element content 
         */
        content(element, forceSingle = false) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            let content = "";
            let isMultiple = false;

            if (element.length > 1) {
                content = [];
                isMultiple = true;
            }

            element.each(function () {
                switch (core.utils.getType(this)) {
                    case core.enums.types.HTML_INPUT:
                        if (isMultiple) {
                            content.push(this.value);
                        } else {
                            content = this.value;
                        }
                        break;
                    default:
                        if (isMultiple) {
                            content.push(this.innerHTML);
                        } else {
                            content = this.innerHTML;
                        }
                }
            });

            if (forceSingle && isMultiple && content.length > 0) {
                content = content[0];
            }

            return content;
        },

        /**
         * Show element (removes style to hide element)
         * @param {any} element Element to show
         */
        show(element) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            core.utils.each(element, function (key, val) {
                if (this.style) {
                    this.style.display = "";
                }
            });
        },

        /**
         * Show element (adds style to hide element)
         * @param {any} element Element to hide
         */
        hide(element) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            core.utils.each(element, function (key, val) {
                if (this.style) {
                    this.style.display = "none";
                }
            });
        },

        /**
         * Removes element(s) from DOM
         * @param {any} element Element(s) to remove
         */
        remove(element) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            core.utils.each(element, function (key, val) {
                const parent = this.parentElement;
                if (!parent) {
                    return;
                }
                parent.removeChild(this);
            });
        },

        /**
         * Scroll to element
         * @param {element} element to scroll to
         * @param {number} duration Duration of the scroll
         * @param {element} baseElement Top element || document.documentElement
         * @param {boolean} forced Do event even if another scroll event is busy
         */
        scroll(element, duration, baseElement, forced = false) {
            if (isScrolling && !forced) {
                core.debug.log("Scroll: Already in scroll action..");
                return;
            }
            isScrolling = true;

            if (!core.settings.doNotDisableScroll) {
                core.utils.disableScroll();
            }
            baseElement = baseElement || document.documentElement;
            duration = duration || 50;
            if (!(element instanceof Collection)) {
                element = core(element);
            }
            if (!(baseElement instanceof Collection)) {
                baseElement = core(baseElement);
            }

            const elementItem = element.first();
            const baseElementItem = baseElement.first();

            if (duration <= 0) {
                isScrolling = false;
                core.utils.enableScroll();
                return;
            }
            const difference = elementItem.offsetTop - baseElementItem.scrollTop;
            const perTick = difference / duration * 10;

            if (!elementItem || !baseElementItem) {
                isScrolling = false;
                core.utils.enableScroll();
                return;
            }

            setTimeout(() => {
                baseElementItem.scrollTop = baseElementItem.scrollTop + perTick;
                if (baseElementItem.scrollTop === elementItem.offsetTop) {
                    isScrolling = false;
                    core.utils.enableScroll();
                    return;
                }
                core.utils.scroll(element, duration - 10, baseElement, true);
            }, 10);
        },

        /**
         * Disables all scrolling
         */
        disableScroll() {
            if (window.addEventListener) // older FF
                window.addEventListener("DOMMouseScroll", internal.preventDefault, false);
            window.onwheel = internal.preventDefault; // modern standard
            window.onmousewheel = document.onmousewheel = internal.preventDefault; // older browsers, IE
            window.ontouchmove = internal.preventDefault; // mobile
        },

        /**
         * Enables scrolling
         */
        enableScroll() {
            if (window.removeEventListener)
                window.removeEventListener("DOMMouseScroll", internal.preventDefault, false);
            window.onmousewheel = document.onmousewheel = null;
            window.onwheel = null;
            window.ontouchmove = null;
        },

        /**
         * 
         */
        stripNulls(obj) {
            const returnObject = core.utils.extend({}, obj);
            for (const key in returnObject) {
                if (returnObject.hasOwnProperty(key)) {
                    if (!returnObject[key]) {
                        delete returnObject[key];
                    }
                }
            }
            return returnObject;
        },

        /**
         * Get data attributes as object with optionally prefixed
         * @param {any} prefix
         */
        getDataAttrAsObj(collectionOrElement, prefix, removePrefix = false) {
            if (!(collectionOrElement instanceof Collection)) {
                collectionOrElement = core(collectionOrElement);
            }

            const dataAttrObj = {};

            core.utils.each(collectionOrElement, function (k, v) {
                const domStringMap = v.dataset;
                for (let dataAttr in domStringMap) {
                    if (domStringMap.hasOwnProperty(dataAttr)) {
                        let dName = dataAttr;
                        const dValue = v.dataset[dName];
                        dName = core.convert.toCamelCase(dName);

                        if (prefix && typeof prefix === "string") {
                            // Prefix to camelCase
                            prefix = core.convert.toCamelCase(prefix);
                            prefix = prefix.replace(/-/g, "");
                            if (!dName.startsWith(prefix)) {
                                continue;
                            }

                            if (removePrefix) {
                                dName = dName.substr(prefix.length);
                                dName = dName.charAt(0).toLowerCase() + dName.slice(1);
                            }
                        }

                        dataAttrObj[dName] = dValue;
                    }
                }
            });
            return dataAttrObj;
        },

        /**
         * 
         * @param {any} content content of the file
         * @param {any} filename the filename
         * @param {any} type the content type
         */
        download(content = "", filename = "file.txt", type = "text/plain") {
            const element = document.createElement("a");
            element.setAttribute("href", `data:${type};charset=utf-8,` + encodeURIComponent(content));
            element.setAttribute("download", filename);

            element.style.display = "none";
            document.body.appendChild(element);
            element.click();
            document.body.removeChild(element);
        }
    };// end Utils

    /**
     * == EVENTS ==
     * Methods related to events
     */
    core.events = {
        /**
         * Clear all listeners of element(s)
         * @param {elements} element Element(s) to clear
         */
        clearListeners(element) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            element.each(function () {
                const oldElement = this;
                if (!oldElement.cloneNode) {
                    return;
                }
                const newElement = oldElement.cloneNode(true);
                oldElement.parentNode.replaceChild(newElement, oldElement);
            });
        },

        attachDoubleClick(element, callback = core.utils.noop, timeOutInMs = 500, useMouseClick = false) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            const timers = [];

            class TimerElement {
                constructor(target) {
                    this.element = target;
                    this.timer = setTimeout(this.remove.bind(this), timeOutInMs);
                }

                remove() {
                    clearTimeout(this.timer);
                    this.timer = null;
                    timers.splice(timers.findIndex(t => t.element === this.element));
                }
            }

            const getTimer = (target) => {
                return timers.find(timers => timers.element === target);
            };

            const attach = (event) => {
                const target = event.currentTarget;
                if (getTimer(target)) {
                    getTimer(target).remove();
                    callback(target, event);
                } else {
                    timers.push(new TimerElement(target));
                }
            };


            element.each(function () {
                this.addEventListener(useMouseClick ? "click" : "mousedown", attach);
            });
        },

        /**
         * Add an eventlistener, usefull with anonFn with option to remove them
         * @param {elements} element Element(s) to set eventListener to
         * @param {any} eventType Event type to attach
         * @param {any} fn Function to execute/bind
         * @param {any} useCapture useCapture
         */
        on(element, eventType, fn, useCapture = false) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            if (/(^function\(.*\)\{.*\}$)|(^\(.*\)\=\>\{.*\})/g.test(fn.toString().replace(/ /g, "").replace(/\n|\r/g, "")) && !core.settings.disableFnStorage) {
                internal.storeAnonFn(fn);
                fn = anonFuncStorage[internal.checkAnonFn(fn)].val;
                console.log("As anon");
            }

            element.each(function () {
                if (this.addEventListener) {
                    this.addEventListener(eventType, fn, useCapture);
                } else if (this.attachEvent) {
                    this.attachEvent("on" + eventType, fn);
                } else {
                    core.debug.warn(`failed to add eventlistener. Event type: '${eventType}' element:`, element);
                }
            });
        },

        /**
         * Remove an eventlistener set with .on, usefull with anonFn with option to remove them
         * @param {elements} element Element(s) to set eventListener to
         * @param {any} eventType Event type to attach
         * @param {any} fn Function to execute/bind
         * @param {any} useCapture useCapture
         */
        off(element, eventType, fn, useCapture = false) {
            if (!(element instanceof Collection)) {
                element = core(element);
            }

            if (!fn.name && !core.settings.disableFnStorage) {
                if (internal.checkAnonFn(fn) === -1) {
                    return;
                }
                internal.removeAnonFn(fn);
                fn = anonFuncStorage[internal.checkAnonFn(fn)].val;
            }

            element.each(function () {
                if (!this || !this.removeEventListener) return;
                this.removeEventListener(eventType, fn, useCapture);
            });
        },

        /**
         * If the element or content is changed
         * @param {any} element
         * @param {any} callback
         * @param {any} runInitially
         */
        onElementChanged(element, callback = this.utils.noop, runInitially = false, customParams = { childList: true, subtree: true }) {
            const observer = new MutationObserver(callback);
            observer.observe(element, customParams);
            if (runInitially) callback();
        },

        observe(filter, callbackFn = () => { }, excludeChildren = false, fromThisElement = document.documentElement) {
            if (!filter) filter = () => { return true; };
            const validNodeTypes = [1];

            const evalNode = (node) => {
                if (validNodeTypes.indexOf(node.nodeType) === -1) return;

                if (typeof filter === "function") {
                    console.log("onFn");
                    if (filter(node)) callbackFn(node);
                } else {
                    const tempDiv = document.createElement("div");
                    tempDiv.innerHTML = node.outerHTML;
                    if (tempDiv.childNodes && tempDiv.childNodes.length > 0) tempDiv.childNodes[0].innerHTML = "";
                    if (tempDiv.querySelector(filter)) {
                        callbackFn(node);
                    }
                }
            };

            const loopNodes = (node) => {
                evalNode(node);

                if (excludeChildren) return;

                // loop children
                for (let n = 0; n < node.childNodes.length; n++) {
                    loopNodes(node.childNodes[n]);
                }
            };

            const observer = new MutationObserver((mutationRecords) => {
                for (let recordId in mutationRecords) {
                    if (mutationRecords.hasOwnProperty(recordId)) {
                        const currentRecord = mutationRecords[recordId];
                        if (currentRecord.addedNodes.length > 0) {
                            const addedNodes = currentRecord.addedNodes;

                            for (let addedNodeId in addedNodes) {
                                if (addedNodes.hasOwnProperty(addedNodeId)) {
                                    const node = addedNodes[addedNodeId];
                                    loopNodes(node);
                                }
                            }
                        }
                    }
                }
            });

            observer.observe(fromThisElement, {
                childList: true,
                subtree: true
            });
        },

        /**
         * If an element with class is added to dom, it runs the callback fn and returns the collection of elements.
         * @param {any} cljjassName
         * @param {any} fn
         */
        onElementOfTypeAdded(typeAr = [], fn = this.utils.noop) {
            const observer = new MutationObserver((mutationRecords) => {
                for (let recordId in mutationRecords) {
                    if (mutationRecords.hasOwnProperty(recordId)) {
                        let currentRecord = mutationRecords[recordId];

                        if (currentRecord.addedNodes.length > 0) {
                            let addedNodes = currentRecord.addedNodes;

                            for (let addedNodeId in addedNodes) {
                                if (addedNodes.hasOwnProperty(addedNodeId)) {
                                    let node = addedNodes[addedNodeId];
                                    let firstChild = core(node).first();
                                    //core(core(node).first().getElementsByClassName("jpopup")).on("click", () => { alert("it worked!") });
                                    if (firstChild && firstChild.nodeType === 1) {
                                        let collection = core(firstChild.getElementsByClassName(className));
                                        if (collection.length > 0 && !(collection.length === 1 && collection[0] && collection[0].length === 0)) {
                                            fn(collection);
                                        } else {
                                            // Single element
                                            var el = core(firstChild);

                                            if (el.hasClass(className)) {
                                                fn(el);
                                            } else {
                                                // Added item but NOT with the right class, so ignore.
                                            }


                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            observer.observe(document.documentElement, {
                childList: true,
                subtree: true
            });
        },

        /**
         * If an element with class is added to dom, it runs the callback fn and returns the collection of elements.
         * @param {any} cljjassName
         * @param {any} fn
         */
        onElementWithClassAdded(className, fn = this.utils.noop) {
            const observer = new MutationObserver((mutationRecords) => {
                for (let recordId in mutationRecords) {
                    if (mutationRecords.hasOwnProperty(recordId)) {
                        let currentRecord = mutationRecords[recordId];

                        if (currentRecord.addedNodes.length > 0) {
                            let addedNodes = currentRecord.addedNodes;

                            for (let addedNodeId in addedNodes) {
                                if (addedNodes.hasOwnProperty(addedNodeId)) {
                                    let node = addedNodes[addedNodeId];
                                    let firstChild = core(node).first();
                                    //core(core(node).first().getElementsByClassName("jpopup")).on("click", () => { alert("it worked!") });
                                    if (firstChild && firstChild.nodeType === 1) {
                                        let collection = core(firstChild.getElementsByClassName(className));
                                        if (collection.length > 0 && !(collection.length === 1 && collection[0] && collection[0].length === 0)) {
                                            fn(collection);
                                        } else {
                                            // Single element
                                            var el = core(firstChild);

                                            if (el.hasClass(className)) {
                                                fn(el);
                                            } else {
                                                // Added item but NOT with the right class, so ignore.
                                            }


                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            observer.observe(document.documentElement, {
                childList: true,
                subtree: true
            });
        },
        /**
         * If an element with class is added to dom, it runs the callback fn and returns the collection of elements.
         * @param {any} dataName x
         * @param {any} fn x
         */
        onElementWithDataAttrAdded(dataName, fn = this.utils.noop) {
            const observer = new MutationObserver((mutationRecords) => {
                for (let recordId in mutationRecords) {
                    if (mutationRecords.hasOwnProperty(recordId)) {
                        let currentRecord = mutationRecords[recordId];
                        if (currentRecord.addedNodes.length > 0) {
                            let addedNodes = currentRecord.addedNodes;
                            for (let addedNodeId in addedNodes) {
                                if (addedNodes.hasOwnProperty(addedNodeId)) {
                                    let node = addedNodes[addedNodeId];
                                    let firstChild = core(node).first();

                                    if (firstChild && firstChild.nodeType === 1) {
                                        let collection = core(firstChild.querySelectorAll("[data-" + dataName + "]"));// core(firstChild.getElementsByClassName(className));
                                        if (collection.length > 0 && !(collection.length === 1 && collection[0] && collection[0].length === 0)) {
                                            fn(collection);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            observer.observe(document.documentElement, {
                childList: true,
                subtree: true
            });
        },

        /**
         * Add hot-key to an element like a textarea or body
         * Also possible to forward an event Usage: onClick = addHotKey.bind(this, element, .... etc) just dont fill event
         * Attaching to an event is useful to just check if the keypress was correct. (only attachable to Keyboard event of course)
          * @param {any} element HTML element to target
          * @param {any} actionFn The method to execute on keycode
          * @param {any} mainKey The main key, like M,q,w,e,r,t,y
          * @param {any} optionKey (optional) like CTRL,ALT
          * @param {any} extraOptionKey (optional) like CTRL,ALT to combine
          * @param {any} event Do not fill manually, this is used when attached to an event. (When attached to event, leave element null)
          */
        addHotKey(element = null, actionFn = () => { }, mainKey, optionKey = null, extraOptionKey = null, event = null) {
            const optionKeys = [null, "CTRL", "ALT" /*, "ALTGR", "BOTH_ALT"*/, "SHIFT"];

            if (optionKeys.indexOf(optionKey) === -1 || optionKeys.indexOf(extraOptionKey) == -1) {
                console.warn("Option keys alowed are" + optionKeys.join(", "));
                return;
            }

            const needsAltGrKey = ([optionKey, extraOptionKey].indexOf("ALTGR") !== -1);
            const needsAltKey = ([optionKey, extraOptionKey].indexOf("ALT") !== -1);
            const needsAltBothKey = ([optionKey, extraOptionKey].indexOf("BOTH_ALT") !== -1);
            const needsCtrlKey = ([optionKey, extraOptionKey].indexOf("CTRL") !== -1);
            const needsShiftKey = ([optionKey, extraOptionKey].indexOf("SHIFT") !== -1);

            const onKeyPress = (innerEvent, isDirect = false) => {
                if (mainKey !== innerEvent.key) return;

                // Pass on option keys
                if (!(needsAltGrKey === false || (needsAltGrKey && innerEvent.key === "AltGraph"))) return;
                if (!(needsAltKey === false || (needsAltKey && innerEvent.altKey))) return;
                if (!(needsAltBothKey === false || (needsAltBothKey && (innerEvent.keyCode === 18 || innerEvent.altKey)))) return;
                if (!(needsCtrlKey === false || (needsCtrlKey && innerEvent.ctrlKey))) return;
                if (!(needsShiftKey === false || (needsShiftKey && innerEvent.shiftKey))) return;

                actionFn(innerEvent, isDirect);
            };

            if (element) {
                element.addEventListener("keyup", onKeyPress);
            } else if (event) {
                onKeyPress(event, true);
            } else {
                console.warn("Input element OR set element as NULL and run through event");
            }

        },

        // sub Element for dataLayer events
        dataLayer: {
            enabled: false,
            autoFormat: true,
            locationFn: r => "",
            enable(autoFormat = true) {
                if (!window.dataLayer && false) {
                    console.warn("[events.dataLayer]: dataLayer not set, no window.dataLayer present");
                    return;
                }
                if (this.enabled) {
                    console.warn("[events.dataLayer]: already enabled");
                    return;
                }

                this.autoFormat = autoFormat;

                // set eventListerer for elements
                const attachListener = (elements) => {
                    if (elements && !elements.length) elements = [elements];

                    for (let i = 0; i < elements.length; i++) {
                        const element = elements[i];
                        let eventType = element instanceof HTMLAnchorElement ? "mousedown" : "click";

                        // or change type
                        if (element.type && element.type === "checkbox") eventType = "change";

                        elements[i].addEventListener(eventType, this.onHandleElement.bind(this, eventType));
                    }
                };

                jjl.events.onElementWithClassAdded("jjl-datalayer", attachListener.bind(this));

                if (this.autoFormat) {
                    const qsSelector = `input[type="radio"],input[type="checkbox"],a`;
                    core.events.observe(qsSelector, attachListener.bind(this));

                    const targetElements = document.querySelectorAll(qsSelector);
                    for (let i = 0; i < targetElements.length; i++) {
                        attachListener(targetElements[i]);
                    }
                }



                this.enabled = true;
            },

            onHandleElement(eventType, event) {
                const element = event.currentTarget;
                eventType = eventType || event.type;
                const dlObject = {
                    event: eventType
                };

                // get from autoFormat
                if (this.autoFormat) {
                    let elementType = element.type;

                    if (!elementType) {
                        if (element instanceof HTMLAnchorElement) elementType = "anchor";
                    }

                    Object.assign(dlObject, {
                        type: elementType
                    });

                    switch (elementType) {
                        case "anchor":
                            Object.assign(dlObject, {
                                key: element.getAttribute("href") && element.getAttribute("href") !== "#" ? element.href : element.innerText,
                                value: "clicked!"
                            });
                            break;
                        case "checkbox":
                            Object.assign(dlObject, {
                                key: element.name || element.innerText,
                                value: element.checked ? "checked" : "unchecked"
                            });
                            break;
                        case "radio":
                            Object.assign(dlObject, {
                                key: element.name || element.innerText,
                                value: element.value
                            });
                            break;
                        default:
                            console.log("unknown type: " + elementType);
                    }

                }

                // Combine with custom
                if (element.dataset.jjlDatalayerObject) {
                    Object.assign(dlObject, core.convert.jsonToObject(`{${core.convert.invertQuotes(element.dataset.jjlDatalayerObject)}}`));
                }

                this.push(dlObject);
            },

            getLocation() {
                const location = this.locationFn();
                if (!location) return {};

                return { "location": location };
            },

            setLocation(fn) {
                if (fn === undefined) {
                    this.locationFn = r => window.location.search;
                } else if (typeof fn === "function") {
                    this.locationFn = fn;
                } else {
                    console.warn("[events.dataLayer]: Only none or function is possible, to get value: 'r => window.myVar'");
                }
            },

            push(obj = {}) {
                const dataLayerObject = obj;
                Object.assign(dataLayerObject, this.getLocation());


                console.log("Push to dataLayer: ", dataLayerObject);
                if (!window.dataLayer) {
                    console.warn("[events.dataLayer]: dataLayer not set, no window.dataLayer present");
                    return;
                }

                window.dataLayer.push(dataLayerObject);
            }

        }
    }; // end events

    /**
     * == Convert ==
     * Methods related to converts
     */
    core.convert = {
        /**
         * Replace multiple items in a string or value with an object.
         * Please note that object given is like {replacement:searchStrOrRx, replacement: needle}
         * @param {string} string haystack 
         * @param {object} replacements {replacement: needle}
         * @returns {string} new string 
         */
        multiReplace(string, replacements) {
            if (core.utils.getType(string) !== core.enums.types.STRING) {
                return string;
            }
            replacements = replacements || {};

            for (const key in replacements) {
                if (replacements.hasOwnProperty(key)) {
                    if (replacements[key] !== undefined) {
                        string = string.replace(replacements[key], key);
                    }
                }
            }

            /*for (var i = 0; i < replacements.length; i++) {
                string = string.replace(Object.keys(replacements[i])[0], replacements[i][Object.keys(replacements[i])[0]]);
            }*/
            return string;
        },
        invertQuotes(str = "") {
            return str.replace(/[\'\"]/g, e => e === '"' ? "'" : '"');
        },
        decodeHtml(html) {
            const txt = document.createElement("textarea");
            txt.innerHTML = html;
            return txt.value;
        },

        encodeHtml(text) {
            const txt = document.createElement("textarea");
            txt.innerText = text;
            return txt.innerHTML;
        },

        jsFileToEs5WithBabelApi(fileLocation, download = true, options = {}, callbackEs5 = core.utils.noop) {

            const onFileConverted = (result) => {
                if (download)
                    core.utils.download(result, "ConvertedCode.txt");
                else {
                    callbackEs5(result);
                    if (callbackEs5 === core.utils.noop) {
                        console.log(result);
                    }
                }
            };

            const onFileDownloaded = (xhrResult) => {
                this.jsToEs5WithBabelApi(xhrResult.value, options, onFileConverted);
            };

            // Bug: does not work on edge
            //jjl.data.getRawData({ ...new jjl.models.XhrRequestModel(), method: "GET", url: fileLocation, onDone: onFileDownloaded });
            jjl.data.getRawData(core.utils.extend(new jjl.models.XhrRequestModel(), { method: "GET", url: fileLocation, onDone: onFileDownloaded }));

        },

        jsToEs5WithBabelApi(javascriptEs6String, options = {}, callbackEs5 = core.utils.noop) {
            core.data.getApi("https://api.juicebv.nl/api/v1/babel", { content: javascriptEs6String, options: options }, (xhrResult) => {

                var resultContent = xhrResult.value && xhrResult.value.content ? xhrResult.value.content : javascriptEs6String;
                callbackEs5(resultContent);

                if (callbackEs5 === core.utils.noop) {
                    console.log(resultContent);
                }
            });
        },

        /**
         * 
         * @param {any} string ...
         * @returns {any}  ...
         */
        toUpperStartOfString(string) {
            if (core.utils.getType(string) === core.enums.types.STRING) {
                return string.charAt(0).toUpperCase() + string.slice(1);
            }
            return string;
        },
        toQueryString(obj, prependQuestionMarkOnData = false) {
            let returnString = "";
            let cnt = 0;
            for (const key in obj) {
                if (obj.hasOwnProperty(key)) {
                    if (obj[key] !== undefined) {
                        if (cnt > 0) {
                            returnString += "&";
                        }
                        cnt++;
                        returnString += `${key}=${obj[key]}`;
                    }
                }
            }
            return returnString.length === 0 ? "" : `${prependQuestionMarkOnData ? "?" : ""}${returnString}`;
        },

        toHashString(obj) {
            let returnString = "";
            let cnt = 0;
            for (const key in obj) {
                if (obj.hasOwnProperty(key)) {
                    if (obj[key] !== undefined) {
                        if (cnt > 0) {
                            returnString += "&";
                        }
                        cnt++;
                        returnString += `${key}=${obj[key]}`;
                    }
                }
            }
            return returnString;
        },
        jsonToObject(json, onInvalid = {}) {
            // if invalid, return empty obj
            if (typeof json !== "string") return onInvalid;
            if ((json.substr(0, 1) !== "{" || json.substr(-1, 1) !== "}") && (json.substr(0, 1) !== "[" || json.substr(-1, 1) !== "]")) return onInvalid;

            try {
                return JSON.parse(json);
            } catch (err) {
                console.warn("Unable to parse JSON to object: ", json);
                return onInvalid;
            }
        },

        toPrettyPrice(price, useThousandSteps, useZeros, numDecimals) {
            if (!(/^\d+\.\d+\,\d+|\d+\,\d+$|^\d+\,\d+\.\d+|\d+\.\d+$|^\d+$/.test(price))) {
                return price;
            }
            numDecimals = numDecimals || 2;
            useZeros = useZeros || false;
            useThousandSteps = useThousandSteps || false;

            // Validate if it is possible
            price = price.toString();
            let prettyPrice = price;
            // Check format and correct to decimal with ,
            if (/f/.test(price)) {
                // decimalSign = , (NL)
                // remove thousands
                prettyPrice = price.replace(/\./g, "");
            } else {
                // decimalSign = . (EN)
                // remove thousands
                prettyPrice = price.replace(/\,/g, "");
                // Convert decimal sign to ,
                prettyPrice = prettyPrice.replace(/\./g, ",");
            }

            if (useThousandSteps) {
                prettyPrice += "";
                const x = prettyPrice.split(",");
                let x1 = x[0];
                const x2 = x.length > 1 ? `,${x[1]}` : "";
                const rgx = /(\d+)(\d{3})/;
                while (rgx.test(x1)) {
                    x1 = x1.replace(rgx, "$1" + "." + "$2");
                }
                prettyPrice = x1 + x2;
            }

            // Correct decimals
            let decimalPart = prettyPrice.split(",")[1];
            if (!decimalPart || decimalPart.length === 0) {
                prettyPrice += ",00";
                decimalPart = "00";
            }

            if (decimalPart && decimalPart.length < numDecimals) {
                let adZeros = "";
                for (let zi = 0; zi < (numDecimals - decimalPart.length); zi++) {
                    adZeros += "0";
                }
                prettyPrice += adZeros;
            }

            if (decimalPart && decimalPart.length > numDecimals) {
                const newDecimal = (parseFloat("0." + decimalPart).toFixed(numDecimals)).toString().replace("0.", "");
                prettyPrice = prettyPrice.substring(0, prettyPrice.lastIndexOf(",") + 1) + newDecimal;
                //(.987465).toFixed(2)
            }

            // Convert ,00 to ,-
            if (!useZeros) {
                prettyPrice = prettyPrice.replace(/\,00$/g, ",-");
            }
            return prettyPrice;
        },
        /**
         * Converts a string or number to a boolean value
         * @param {string} stringOrNumber String or numeric value
         */
        toBool(stringOrNumber) {
            var falseValues = [
                "false",
                false,
                "0",
                "",
                null,
                undefined
            ];

            return (falseValues.indexOf(stringOrNumber) === -1) ? true : false;
        },
        toCamelCase(string) {
            if (!string) return string;
            string = string.replace(/[-| |_]([a-z0-9])/g, (g) => { return g[1].toUpperCase(); });
            string = string.charAt(0).toLowerCase() + string.slice(1);
            return string;
        },
        toSeperated(string, separator = "-") {
            console.warn("This method is obsolete (wrong spelling), please use 'toSeparated' instead.");
            return this.toSeparated(string, separator);
        },
        toSeparated(string, separator = "-") {
            if (!string) {
                return string;
            }
            const upperChars = string.match(/([A-Z])/g);
            if (!upperChars) {
                return string;
            }

            var str = string.toString();
            for (var i = 0, n = upperChars.length; i < n; i++) {
                str = str.replace(new RegExp(upperChars[i]), separator + upperChars[i].toLowerCase());
            }

            // remove all else (non a-z or separator)
            str = str.replace(new RegExp("[^a-z|\\" + separator + "]", "g"), "");

            // remove doubles
            str = str.replace(new RegExp("\\" + separator + "{2,}", "g"), separator);

            // remove if first char is separator
            if (str.slice(0, 1) === separator) {
                str = str.slice(1);
            }

            return str;
        },

        dateToString(dateTime, formatString = "%d/%m/%Y %H:%i:%s", useUtc = true) {
            if (!dateTime || !(dateTime instanceof Date) || !formatString) {
                console.log('Format as in MySQL, https://www.w3schools.com/sql/func_mysql_date_format.asp \nIf option not present, add in jjl');
                return;
            }

            // Prefix with 0 if length 1
            const forceDoubleNumbers = (entrance, prependZerosUntilLength = 2) => entrance.toString().length < prependZerosUntilLength ? `0${entrance}` : entrance;

            formatString = formatString.replace("%H", forceDoubleNumbers(dateTime.getHours()));
            formatString = formatString.replace("%i", forceDoubleNumbers(dateTime.getMinutes()));
            formatString = formatString.replace("%s", forceDoubleNumbers(dateTime.getSeconds()));

            formatString = formatString.replace("%d", forceDoubleNumbers(useUtc ? dateTime.getUTCDate() : dateTime.getDate()));
            formatString = formatString.replace("%m", forceDoubleNumbers(useUtc ? dateTime.getUTCMonth() + 1 : dateTime.getMonth() + 1));
            formatString = formatString.replace("%Y", forceDoubleNumbers(useUtc ? dateTime.getUTCFullYear() : dateTime.getFullYear(), 4));

            return formatString;
        },

        /**
         * Creates correct formats from strings "400" --> 400 && "true" == true
         */
        autoFormat(inputStringOrObject) {
            const aFormat = (original) => {
                // return the original if it is not a string
                if (typeof original !== "string") return original;

                // force to lowercase string first
                var str = original.toString().toLocaleLowerCase();

                // check startswith 0
                if (/^0\d+$/.test(str)) return original;
                // check for number
                if (/^\d+$/.test(str)) return parseInt(str);
                // check float
                if (/^\d+\.\d+$/.test(str)) return parseFloat(str);
                // check bool
                if (/^(true|false)$/.test(str)) return core.convert.toBool(str);

                // check json
                if (core.utils.isValidJson(original)) return core.convert.jsonToObject(original);

                return original;
            };

            if (typeof inputStringOrObject === "string") {
                return aFormat(inputStringOrObject);
            } else if (typeof inputStringOrObject === "object") {

                for (let prop in inputStringOrObject) {
                    if (inputStringOrObject.hasOwnProperty(prop)) {
                        if (typeof inputStringOrObject[prop] === "string") {
                            inputStringOrObject[prop] = aFormat(inputStringOrObject[prop]);
                        }
                    }
                }
                return inputStringOrObject;
            }

            return inputStringOrObject;
        }
    }; // End convert

    core.data = {
        getDataByType(datatype = core.enums.xhrDataTypes.RAW, identifierOrxhrRequestModel, parameters, callback) {
            // Switch callback to function if no params
            if (core.utils.getType(parameters) === core.enums.types.FUNCTION && callback === undefined) {
                callback = parameters;
                parameters = {};
            }

            parameters = parameters || {};
            callback = callback || core.utils.noop;


            // create correct model
            let xhrRequestSettings;
            if (identifierOrxhrRequestModel instanceof core.models.XhrRequestModel || core.utils.getType(identifierOrxhrRequestModel) === core.enums.types.OBJECT) {
                xhrRequestSettings = core.utils.extendStrict(new core.models.XhrRequestModel(), identifierOrxhrRequestModel);
            } else {
                xhrRequestSettings = new core.models.XhrRequestModel();
                if (typeof identifierOrxhrRequestModel === "string" && identifierOrxhrRequestModel.indexOf("http") === 0) {
                    xhrRequestSettings.url = identifierOrxhrRequestModel;
                } else {
                    xhrRequestSettings.identifier = identifierOrxhrRequestModel;
                }
                xhrRequestSettings.parameters = parameters;
            }

            if (datatype !== core.enums.xhrDataTypes.FORM_DATA) {
                xhrRequestSettings.parameters = core.utils.extend({}, xhrRequestSettings.parameters);
            }

            switch (datatype) {
                case core.enums.xhrDataTypes.TEMPLATE:
                    if (xhrRequestSettings.parameters.ombouw === undefined) {
                        xhrRequestSettings.parameters.ombouw = false;
                    }
                    if (xhrRequestSettings.parameters.ombouw === null) {
                        delete xhrRequestSettings.parameters.ombouw;
                    }
                case core.enums.xhrDataTypes.JSON:
                    if (xhrRequestSettings.parameters.trace === undefined) {
                        xhrRequestSettings.parameters.trace = false;
                    }
                    if (xhrRequestSettings.parameters.trace === null) {
                        delete xhrRequestSettings.parameters.trace;
                    }
                    break;
                case core.enums.xhrDataTypes.WEBPAGE:
                    xhrRequestSettings.parameters.trace = false;
                    xhrRequestSettings.parameters.ombouw = false;
                    break;
                case core.enums.xhrDataTypes.WEBMETHOD:
                    //xhrRequestSettings.parameters.trace = false;
                    //xhrRequestSettings.parameters.ombouw = false;
                    break;
            }


            let templateType = "";

            switch (datatype) {
                case core.enums.xhrDataTypes.TEMPLATE:
                case core.enums.xhrDataTypes.JSON:
                    templateType = (core.utils.getType(xhrRequestSettings.identifier) === core.enums.types.NUMBER) ? "templateid" : "templatename";
                    break;
                case core.enums.xhrDataTypes.WEBPAGE:
                    templateType = (core.utils.getType(xhrRequestSettings.identifier) === core.enums.types.NUMBER) ? "jclcmspageid" : "jclcmspagepath";
                    break;
            }


            let url = (xhrRequestSettings.url) ? xhrRequestSettings.url : core.settings.serviceUrl;

            const baseUrl = xhrRequestSettings.url || core.settings.serviceUrl;

            switch (datatype) {
                case core.enums.xhrDataTypes.TEMPLATE:
                    url = `${baseUrl}/template.jcl`;
                    break;
                case core.enums.xhrDataTypes.WEBPAGE:
                    url = `${baseUrl}/cmspage.jcl`;
                    break;
                case core.enums.xhrDataTypes.JSON:
                    url = `${baseUrl}/json.jcl`;
                    break;
                case core.enums.xhrDataTypes.API:
                    //url += "?ts=" + new Date().getTime();
                    break;
                case core.enums.xhrDataTypes.WEBMETHOD:
                    url += `/${xhrRequestSettings.identifier}`;
                    break;
            }

            // On GET extend url
            if (xhrRequestSettings.method === "GET") {
                if (core.utils.count(xhrRequestSettings.parameters) > 0) {
                    url += `?${templateType}=${xhrRequestSettings.identifier}&${core.convert.toQueryString(xhrRequestSettings.parameters)}`; //(line fails in some browsers) Object.keys(settings.data).map((i) => i + "=" + settings.data[i]).join("&");
                }
            } else {
                switch (datatype) {
                    case core.enums.xhrDataTypes.TEMPLATE:
                    case core.enums.xhrDataTypes.WEBPAGE:
                    case core.enums.xhrDataTypes.JSON:
                        xhrRequestSettings.parameters[templateType] = xhrRequestSettings.identifier;
                        break;
                }

            }

            const request = new XMLHttpRequest();
            const response = new core.models.XhrResponseModel();
            response.value = {};
            response.isSuccess = false;
            request.onreadystatechange = () => {
                if (request.readyState === XMLHttpRequest.DONE) {
                    response.responseCode = request.status;
                    if (request.status === 200) {
                        response.response = request.responseText;
                        try {
                            switch (datatype) {
                                case core.enums.xhrDataTypes.WEBMETHOD:
                                    try {
                                        const tempObj = JSON.parse(request.responseText);
                                        response.value = (tempObj.d === null || tempObj.d === undefined) ? tempObj : tempObj.d;
                                    } catch (er) {
                                        response.value = request.responseText;
                                    }
                                    break;
                                case core.enums.xhrDataTypes.TEMPLATE:
                                case core.enums.xhrDataTypes.WEBPAGE:
                                    response.value = request.responseText;
                                    break;
                                case core.enums.xhrDataTypes.JSON:
                                case core.enums.xhrDataTypes.API:
                                    response.value = JSON.parse(request.responseText);
                                    break;
                                default:
                                    response.value = request.responseText;
                            }

                            response.isSuccess = true;
                            xhrRequestSettings.onSuccess.call(response.value, response);
                        } catch (exc) {
                            console.warn(`[${libraryName}:getJson] Unable to parse JSON content to object!`, exc);

                            if (!xhrRequestSettings.onFailContent) {
                                response.value = xhrRequestSettings.onFailContent;
                            }
                            xhrRequestSettings.onError.call(response.value, response);
                        }
                    }
                    else {
                        response.response = request.responseText;
                        if (!xhrRequestSettings.onFailContent) {
                            response.value = xhrRequestSettings.onFailContent;
                        }

                        // try to set errortext
                        const rxServerException = "\<b\> Exception Details: \<\/b\>(.*?)<"; //\<b\> Description: \<\/b\>(.*?)[<|\n]
                        const rxServerExceptionDesc = "\<b\> Description: \<\/b\>(.*?)[<|\r|\n]";

                        const rxIssException = '<div id="header"><h1>(.*?)<';
                        const rxIssExceptionDesc = '<h2>(.*?)<\/h2>\r\n  <h3>(.*?)<';

                        let parsedJsonResult = null;
                        try {
                            parsedJsonResult = JSON.parse(response.response);
                        } catch (err) {
                            // Do nothing. Result is no JSON
                        }

                        if (response.response.match(rxServerException) && response.response.match(rxServerException).length > 1) {
                            // Server error with exception:
                            response.errorMessage = response.response.match(rxServerException)[1];
                            response.errorExplain = (response.response.match(rxServerExceptionDesc) && response.response.match(rxServerExceptionDesc).length > 1) ? response.response.match(rxServerExceptionDesc)[1] : "";
                        } else if (response.response.match(rxIssException) && response.response.match(rxIssException).length > 1) {
                            response.errorMessage = response.response.match(rxIssException)[1];
                            response.errorExplain = (response.response.match(rxIssExceptionDesc) && response.response.match(rxIssExceptionDesc).length > 2) ? `${response.response.match(rxIssExceptionDesc)[1]} \n\n${response.response.match(rxIssExceptionDesc)[2]}` : "";
                        } else if (parsedJsonResult && parsedJsonResult.Message) {
                            response.errorMessage = `${parsedJsonResult.Message} (${parsedJsonResult.ExceptionType || ""})`;
                            response.errorExplain = parsedJsonResult.StackTrace || "";
                        }

                        if (response.errorMessage && response.errorExplain) {
                            console.warn(`[${libraryName}:getData] XHR Error!\n\n\n${response.errorMessage}\n\n"${response.errorExplain}"`);
                        } else {
                            console.warn(`[${libraryName}:getData] XHR Error!\n\n\n${response.response}`);
                        }

                        xhrRequestSettings.onError.call(response.value, response);
                    }
                    xhrRequestSettings.onDone.call(response.value, response);
                    callback.call(response.value, response);

                    const noop = core.utils.noop;
                    if (callback === noop && xhrRequestSettings.onDone === noop && xhrRequestSettings.onError === noop && xhrRequestSettings.onSuccess === noop) {
                        console.log(response.value);
                    }

                }
            };
            request.open(xhrRequestSettings.method, url, true);

            if (xhrRequestSettings.requestHeader === "") {
                switch (datatype) {
                    case core.enums.xhrDataTypes.WEBMETHOD:
                        request.setRequestHeader("Content-type", "application/json; charset=UTF-8");
                        break;
                    case core.enums.xhrDataTypes.API:
                        request.setRequestHeader("Content-type", "application/json; charset=UTF-8");

                        //request.setRequestHeader("Content-type", "text/plain; charset=UTF-8");
                        break;
                    case core.enums.xhrDataTypes.FORM_DATA:
                        break;
                    default:
                        request.setRequestHeader("Content-type", "application/x-www-form-urlencoded; charset=utf-8");
                }
            } else {
                request.setRequestHeader("Content-type", xhrRequestSettings.requestHeader);
            }


            if (xhrRequestSettings.withCredentials) {
                request.withCredentials = true;
            }


            if ((core.utils.count(xhrRequestSettings.parameters) === 0 || xhrRequestSettings.method === "GET") && datatype !== core.enums.xhrDataTypes.FORM_DATA) {
                request.send();
            } else {
                const parseParamsAs = xhrRequestSettings.parseParamsAs;

                if (parseParamsAs.toLowerCase() === "json") {
                    request.send(JSON.stringify(xhrRequestSettings.parameters));
                } else if (parseParamsAs.toLowerCase() === "formdata") {
                    request.send(core.convert.toQueryString(xhrRequestSettings.parameters));
                } else {
                    switch (datatype) {
                        case core.enums.xhrDataTypes.WEBMETHOD:
                            request.send(JSON.stringify(xhrRequestSettings.parameters));
                            break;
                        case core.enums.xhrDataTypes.API:
                            request.send(JSON.stringify(xhrRequestSettings.parameters));
                            break;
                        case core.enums.xhrDataTypes.FORM_DATA:
                            request.send(xhrRequestSettings.parameters);
                            break;
                        default:
                            request.send(core.convert.toQueryString(xhrRequestSettings.parameters));
                    }
                }
            }
        },

        // Get address information based on a specified zipcode and hoursenumber
        getAddressByZipcode(zipcode, nr, callback) {
            const reqSettings = new core.models.XhrRequestModel();

            reqSettings.method = "GET";
            reqSettings.url = "/addressinfo.jcl";
            reqSettings.parameters = {
                "zipcode": zipcode,
                "housenumber": nr,
                "trace": false,
                "ombouw": false
            };

            core.data.getRawData(reqSettings, {}, function (resp) {
                callback(JSON.parse(resp.response));
            });
        },

        /**
         * Get Json data
         * @param {XhrRequestModel} identifierOrxhrRequestModel The model
         * @param {object} parameters Params
         * @param {function} callback Callback
         */
        getJson(identifierOrxhrRequestModel, parameters, callback) {
            core.data.getDataByType(core.enums.xhrDataTypes.JSON, identifierOrxhrRequestModel, parameters, callback);
        },

        async getJsonAsync(identifierOrXhrRequestModel, parameters, onlyResolveValue = false) {
            return new Promise((resolve) => {
                core.data.getDataByType(core.enums.xhrDataTypes.JSON, identifierOrXhrRequestModel, parameters, (xhrResult) => {
                    resolve(onlyResolveValue ? xhrResult.value || [] : xhrResult);
                });
            });
        },
        getWiserData(requestSettings = {}, dataSelectorObject = {}, callback = core.utils.noop) {
            // Todo: Maybe create request object for wiser? (and extend)
            // ...

            const baseUrl = requestSettings.url || core.settings.serviceUrl;
            const requestMethod = Object.keys(dataSelectorObject).length === 0 ? "GET" : "POST";

            // exclude trace if not set
            if (requestSettings.trace === undefined) requestSettings.trace = false;

            const url = `${baseUrl}/get_items.jcl${core.convert.toQueryString(requestSettings, true)}`;

            const request = new XMLHttpRequest();
            const response = new core.models.XhrResponseModel();
            response.value = [];
            response.isSuccess = false;

            request.onreadystatechange = () => {
                if (request.readyState === XMLHttpRequest.DONE) {
                    response.responseCode = request.status;
                    if (request.status === 200) {
                        response.response = request.responseText;
                        try {
                            response.value = jjl.convert.jsonToObject(request.responseText, []);
                        } catch (exc) {
                            console.warn(`[${libraryName}:getWiserData] Unable to parse JSON content to object!`, exc);
                            response.value = [];
                        }
                    } else {
                        // Must be an error
                        response.response = request.responseText;

                        // try to set errortext
                        const rxServerException = "\<b\> Exception Details: \<\/b\>(.*?)<"; //\<b\> Description: \<\/b\>(.*?)[<|\n]
                        const rxServerExceptionDesc = "\<b\> Description: \<\/b\>(.*?)[<|\r|\n]";

                        const rxIssException = '<div id="header"><h1>(.*?)<';
                        const rxIssExceptionDesc = '<h2>(.*?)<\/h2>\r\n  <h3>(.*?)<';

                        let parsedJsonResult = null;
                        try {
                            parsedJsonResult = JSON.parse(response.response);
                        } catch (err) {
                            // Do nothing. Result is no JSON
                        }

                        if (response.response.match(rxServerException) && response.response.match(rxServerException).length > 1) {
                            // Server error with exception:
                            response.errorMessage = response.response.match(rxServerException)[1];
                            response.errorExplain = (response.response.match(rxServerExceptionDesc) && response.response.match(rxServerExceptionDesc).length > 1) ? response.response.match(rxServerExceptionDesc)[1] : "";
                        } else if (response.response.match(rxIssException) && response.response.match(rxIssException).length > 1) {
                            response.errorMessage = response.response.match(rxIssException)[1];
                            response.errorExplain = (response.response.match(rxIssExceptionDesc) && response.response.match(rxIssExceptionDesc).length > 2) ? `${response.response.match(rxIssExceptionDesc)[1]} \n\n${response.response.match(rxIssExceptionDesc)[2]}` : "";
                        } else if (parsedJsonResult && parsedJsonResult.Message) {
                            response.errorMessage = `${parsedJsonResult.Message} (${parsedJsonResult.ExceptionType || ""})`;
                            response.errorExplain = parsedJsonResult.StackTrace || "";
                        }

                        if (response.errorMessage && response.errorExplain) {
                            console.warn(`[${libraryName}:getData] XHR Error!\n\n\n${response.errorMessage}\n\n"${response.errorExplain}"`);
                        } else {
                            console.warn(`[${libraryName}:getData] XHR Error!\n\n\n${response.response}`);
                        }
                    }
                    callback.call(response.value, response);

                    const noop = core.utils.noop;
                    if (callback === noop && xhrRequestSettings.onDone === noop && xhrRequestSettings.onError === noop && xhrRequestSettings.onSuccess === noop) {
                        console.log(response.value);
                    }
                }
            };

            request.open(requestMethod, url, true);
            request.setRequestHeader("Content-type", "application/x-www-form-urlencoded; charset=utf-8");
            if (requestMethod === "GET") {
                request.send();
            } else {
                request.send(core.convert.toQueryString({
                    dataselectorjson: encodeURIComponent(JSON.stringify(dataSelectorObject))
                }));

            }
        },
        getWiserDataAsync(RequestSettings = {}, dataSelectorObject = {}, resolveFullXhrResult = false) {
            return new Promise((resolve) => {
                core.data.getWiserData(RequestSettings, dataSelectorObject, (xhrResult) => {
                    resolve(!resolveFullXhrResult ? xhrResult.value || [] : xhrResult);
                });
            });
        },

        /**
         * Get a template
         */
        getTemplate(identifierOrxhrRequestModel, parameters, callback) {
            core.data.getDataByType(core.enums.xhrDataTypes.TEMPLATE, identifierOrxhrRequestModel, parameters, callback);
        },
        async getTemplateAsync(identifierOrXhrRequestModel, parameters, onlyResolveTemplateContent = false) {
            return new Promise((resolve) => {
                core.data.getDataByType(core.enums.xhrDataTypes.TEMPLATE, identifierOrXhrRequestModel, parameters, (xhrResult) => {
                    resolve(onlyResolveTemplateContent ? xhrResult.value || "" : xhrResult);
                });
            });
        },
        getApi(identifierOrxhrRequestModel, parameters, callback) {
            core.data.getDataByType(core.enums.xhrDataTypes.API, identifierOrxhrRequestModel, parameters, callback);
        },
        getWebpage(identifierOrxhrRequestModel, callback) {
            core.data.getDataByType(core.enums.xhrDataTypes.WEBPAGE, identifierOrxhrRequestModel, null, callback);
        },
        getRawData(urlOrxhrRequestModel, parameters, callback) {
            if (typeof urlOrxhrRequestModel === "string") {
                const url = urlOrxhrRequestModel;
                urlOrxhrRequestModel = new core.models.XhrRequestModel();
                urlOrxhrRequestModel.url = url;
                urlOrxhrRequestModel.parameters = parameters;
            }
            core.data.getDataByType(core.enums.xhrDataTypes.RAW, urlOrxhrRequestModel, parameters, callback);
        },
        getWebMethod(identifierOrxhrRequestModel, parameters, callback) {
            core.data.getDataByType(core.enums.xhrDataTypes.WEBMETHOD, identifierOrxhrRequestModel, parameters, callback);
        },
        /**
        * Call url(s) and redirect to target url
        */
        callUrlAndRedirect(urls, target) {
            var urlsSuccess = true;
            var splitValue = urls.split(";");
            var responseCount = 1;

            //New request model
            const xhrReq = new jjl.models.XhrRequestModel();
            xhrReq.method = "GET";
            xhrReq.parameters = "";

            //Loop trough all request urls
            jjl.utils.each(splitValue, function (key, value) {
                xhrReq.url = value;

                jjl.data.getDataByType(jjl.enums.xhrDataTypes.RAW, xhrReq, { trace: false }, function (response) {
                    if (response.isSuccess === false) {
                        urlsSuccess = false;
                    }

                    if (responseCount === splitValue.length) {
                        if (urlsSuccess === true) {
                            // Redirect the user to the target url, if all request are successful
                            window.location.href = target;
                        } else {
                            console.error("No redirect to target url, not all url requests are successful");
                        }
                    } else {
                        responseCount++;
                    }
                });
            });
        }
    };

    core.hashString = {
        get() {
            return window.location.hash;
        },

        getAsObject(alternateQueryString) {
            const hashString = alternateQueryString || core.hashString.get().replace('#', '');

            if (hashString === "") {
                return {};
            }

            const hsSplits = hashString.split('&');
            const hsObject = {};
            for (let i = 0; i < hsSplits.length; i++) {
                const key = hsSplits[i].split('=')[0];
                const value = hsSplits[i].split('=')[1];

                hsObject[key] = decodeURIComponent(value.replace(/\+/g, "%20"));
            }
            return hsObject;
        },

        getValue(key) {
            const result = null;
            const v = window.location.hash.substring(1).split("&");

            for (let i = 0; i < v.length; i++) {
                const p = v[i].split("=");
                if (decodeURIComponent(p["0"].replace(/\+/g, "%20")) == key) {
                    return decodeURIComponent(p["1"].replace(/\+/g, "%20"));
                }
            }
            return result;
        },

        alter(valueObject, onlyReplace) {
            const hsObject = core.hashString.getAsObject();

            for (const key in valueObject) {
                if (valueObject.hasOwnProperty(key)) {
                    const value = valueObject[key];
                    if (value === null) {
                        delete hsObject[key];
                    } else {
                        hsObject[key] = value;
                    }
                }
            }
            core.hashString.set(hsObject, onlyReplace);
        },

        set(valueObject, onlyReplace = false) {
            const hs = core.convert.toHashString(valueObject);
            const url = location.protocol + "//" + location.host + location.pathname;
            const fullNew = `${url}${core.querystring.get()}#${hs}`;
            if (fullNew !== location.href) {
                if (onlyReplace) {
                    history.replaceState(null, null, fullNew);
                } else {
                    history.pushState(null, null, fullNew);
                }
                window.dispatchEvent(new CustomEvent("popstate"));
            } else {
                // console.warn("[qsEdit:setQs] Nothing to alter, querystring is the same..");
            }
        },

        follow() {
            location.replace(location.href);
        },

        load(valueObject) {
            const hs = core.convert.toHashString(valueObject);
            const url = location.protocol + "//" + location.host + location.pathname;
            const fullNew = `${url}#${hs}`;
            location.replace(fullNew);
        },

        /**
         * Check if specific values are set in querystring ex. checkIfSet(["width", "height"])
         * @param {} keyAr Array with keys to check
         * @returns {} false if there is a key thats not there
         */
        checkIfSet(keyAr = []) {
            if (typeof keyAr === "string") {
                keyAr = [keyAr];
            }
            for (let i = 0; i < keyAr.length; i++) {
                if (core.hashString.getValue(keyAr[i]) === null) {
                    return false;
                }
            }
            return true;
        },
        clear(onlyReplace) {
            const url = location.origin + location.pathname;
            const fullNew = url + core.querystring.get();
            if (fullNew !== location.href) {
                if (onlyReplace) {
                    history.replaceState(null, null, fullNew);
                } else {
                    history.pushState(null, null, fullNew);
                }
                window.dispatchEvent(new CustomEvent("popstate"));
            }
        }
    }; // end QueryString

    core.querystring = {
        get() {
            return window.location.search;
        },

        getAsObject(alternateQueryString) {
            const queryString = alternateQueryString || core.querystring.get().replace('?', '');

            if (queryString === "") {
                return {};
            }

            const qsSplits = queryString.split('&');
            const qsObject = {};
            for (let i = 0; i < qsSplits.length; i++) {
                const key = qsSplits[i].split('=')[0];
                const value = qsSplits[i].split('=')[1];

                qsObject[key] = decodeURIComponent(value.replace(/\+/g, "%20"));
            }
            return qsObject;
        },

        getValue(key) {
            const result = null;
            const v = window.location.search.substring(1).split("&");

            for (let i = 0; i < v.length; i++) {
                const p = v[i].split("=");
                if (decodeURIComponent(p["0"].replace(/\+/g, "%20")) == key) {
                    return decodeURIComponent(p["1"].replace(/\+/g, "%20"));
                }
            }
            return result;
        },

        alter(valueObject, onlyReplace) {
            const qsObject = core.querystring.getAsObject();

            for (const key in valueObject) {
                if (valueObject.hasOwnProperty(key)) {
                    const value = valueObject[key];
                    if (value === null) {
                        delete qsObject[key];
                    } else {
                        qsObject[key] = value;
                    }
                }
            }
            core.querystring.set(qsObject, onlyReplace);
        },

        set(valueObject, onlyReplace = false) {
            const qs = core.convert.toQueryString(valueObject);
            const url = location.protocol + "//" + location.host + location.pathname;
            const fullNew = `${url}?${qs}${core.hashString.get()}`;
            if (fullNew !== location.href) {
                if (onlyReplace) {
                    history.replaceState(null, null, fullNew);
                } else {
                    history.pushState(null, null, fullNew);
                }
                window.dispatchEvent(new CustomEvent("popstate"));
            } else {
                // console.warn("[qsEdit:setQs] Nothing to alter, querystring is the same..");
            }
        },

        follow() {
            location.replace(location.href);
        },

        load(valueObject) {
            const qs = core.convert.toQueryString(valueObject);
            const url = location.protocol + "//" + location.host + location.pathname;
            const fullNew = `${url}?${qs}`;
            location.replace(fullNew);
        },

        /**
         * Check if specific values are set in querystring ex. checkIfSet(["width", "height"])
         * @param {} keyAr Array with keys to check
         * @returns {} false if there is a key thats not there
         */
        checkIfSet(keyAr = []) {
            if (typeof keyAr === "string") {
                keyAr = [keyAr];
            }
            for (let i = 0; i < keyAr.length; i++) {
                if (core.querystring.getValue(keyAr[i]) === null) {
                    return false;
                }
            }
            return true;
        },
        clear(onlyReplace) {
            const url = location.origin + location.pathname;
            const fullNew = url + core.hashString.get();
            if (fullNew !== location.href) {
                if (onlyReplace) {
                    history.replaceState(null, null, fullNew);
                } else {
                    history.pushState(null, null, fullNew);
                }
                window.dispatchEvent(new CustomEvent("popstate"));
            }
        }
    }; // end QueryString

    core.kvk = {
        apiKey: "",
        search(searchString = "", page = 1) {
            return new Promise((resolve, reject) => {
                const reqSettings = new core.models.XhrRequestModel();

                reqSettings.method = "GET";
                reqSettings.url = "https://api.kvk.nl/api/v2/search/companies";
                reqSettings.parameters = {
                    "q": searchString,
                    "user_key": this.apiKey,
                    "startPage": page
                };

                core.data.getRawData(reqSettings, {}, function (resp) {
                    resolve({ kvkData: JSON.parse(resp.response) });
                });
            });
        }
    },

    core.shop = {
        /**
         * Calculate the final digit of a GTIN-13 EAN number.
         * @param {string} ean The GTIN-13 EAN number to calculate the last digit for.
         * @returns {integer} the final digit of the EAN.
         */
        calculateGtn13ValidationDigit(ean) {
            if (!ean || typeof ean !== "string" || ean.length !== 12) {
                throw "Invalid EAN. EAN must be a 12 character string.";
            }

            let calcSum = 0;

            ean.split("").map((number, index) => {
                number = parseInt(number, 10);
                if (index % 2 === 0) {
                    calcSum += number;
                } else {
                    calcSum += number * 3;
                }
            });

            return ((10 - calcSum % 10) % 10);
        },

        /**
         * Calculate the final digit of a GTIN-13 EAN number and add it to the given EAN.
         * @param {string} ean The GTIN-13 EAN number to calculate the last digit for.
         * @returns {string} The full EAN, including the final digit.
         */
        addGtn13ValidationDigit(ean) {
            return ean + this.calculateGtn13ValidationDigit(ean).toString();
        }
    }; // end shop

    /**
     * Storage related methods
     */
    core.storage = {
        indexedDb: {
            get(key, onSuccess = core.utils.noop, onError = core.utils.noop) {
                const doGet = () => {
                    indexedDbStorage.get(key, (result) => {
                        if (result && result.target && result.target.result && result.target.result["value"]) {
                            onSuccess(result.target.result["value"]);
                        } else {
                            onSuccess(null);
                        }
                    });
                }

                if (indexedDbStorage === null) {
                    core.storage.indexedDb.createDb("jjlSettings", "key", ["key"], ["value"], (db) => {
                        indexedDbStorage = db;
                        doGet();
                    }, onError);

                } else {
                    doGet();
                }
            },
            set(key, value, onSuccess = core.utils.noop, onError = core.utils.noop) {
                const doSet = () => {
                    indexedDbStorage.update([{ key: key, value: value }], onSuccess);
                }

                if (indexedDbStorage === null) {
                    core.storage.indexedDb.createDb("jjlSettings", "key", ["key"], ["value"], (db) => {
                        indexedDbStorage = db;
                        doSet();
                    }, onError);

                } else {
                    doSet();
                }
            },
            delete() {

            },
            createDb(name, primaryKey, uniqueIndexesAr, indexesAr, onSuccess = core.utils.noop, onError = core.utils.noop) {
                const base = this;
                const databaseSettings = {
                    databaseName: name,
                    databaseVersion: 3,
                    tableName: name,
                    primaryKey: primaryKey,
                    indexes: indexesAr,
                    uniqueIndexes: uniqueIndexesAr
                }

                // Create local class
                class JuiceIndexedDb {
                    constructor(databaseSettings) {
                        this.databaseSettings = databaseSettings;

                        // This works on all devices/browsers, and uses IndexedDBShim as a final fallback 
                        this.database = window.indexedDB || window.mozIndexedDB || window.webkitIndexedDB || window.msIndexedDB || window.shimIndexedDB;
                    }

                    request(onSuccess = core.utils.noop, onError = core.utils.noop) {
                        // Open (or create) the database
                        var request = this.database.open(this.databaseSettings.databaseName, this.databaseSettings.databaseVersion); // name, version
                        request.onerror = onError;
                        request.onsuccess = onSuccess;
                        request.onupgradeneeded = () => { this.onUpgradeNeeded(request); };
                    }

                    onUpgradeNeeded(request) {
                        const db = request.result;
                        const store = db.createObjectStore(this.databaseSettings.tableName, { keyPath: this.databaseSettings.primaryKey, autoIncrement: true });

                        this.storage = store;

                        // Create the indexes
                        for (let ui = 0; ui < this.databaseSettings.uniqueIndexes.length; ui++) {
                            const uKeyName = this.databaseSettings.uniqueIndexes[ui];
                            store.createIndex(uKeyName, uKeyName, { unique: true });
                        }
                        for (let i = 0; i < this.databaseSettings.indexes.length; i++) {
                            const keyName = this.databaseSettings.indexes[i];
                            store.createIndex(keyName, keyName, { unique: false });
                        }
                    }

                    update(dbObjectAr, onDone = core.utils.noop) {
                        this.request(
                            (event) => {
                                var db = (event.target) ? event.target.result : event.srcElement.result;
                                var transaction = db.transaction(this.databaseSettings.tableName, "readwrite").objectStore(this.databaseSettings.tableName);

                                let loadAr = [];

                                for (var oid in dbObjectAr) {
                                    if (dbObjectAr.hasOwnProperty(oid)) {
                                        loadAr.push(oid);
                                        var result = transaction.put(dbObjectAr[oid]);

                                        result.onerror = function () {
                                            if (this.error.code === 20) return;
                                            console.log("Error updating data[" + this.error.code + "]: ", this.error.message, dbObjectAr[oid]);
                                            console.log(this.error);
                                            console.log(this);
                                        };

                                        result.transaction.oncomplete = function () {
                                            onDone(transaction);
                                        };
                                    }
                                }
                            },
                            function () {
                                console.log("jDb: Failed to set (Unable to open DB)");
                            });
                    }

                    get(primaryKeyId, onDone = core.utils.noop) {
                        this.request(
                            (event) => {

                                var db = (event.target) ? event.target.result : event.srcElement.result;
                                var transaction = db.transaction(this.databaseSettings.tableName).objectStore(this.databaseSettings.tableName);
                                //transaction.onerror = function (err) { alert(err); };

                                var result = transaction.get(primaryKeyId);
                                result.onerror = function () { if (this.error.code === 20) return; console.log("Error updating data[" + this.error.code + "]: ", this.error.message) };

                                result.onsuccess = onDone;

                            },
                            () => {
                                console.log("jDb: Failed to set (Unable to open DB)");
                            });
                    }
                }

                const newDb = new JuiceIndexedDb(databaseSettings);

                // Empty database request to prepare db;
                newDb.request(() => {
                    console.log("Database Init: init '" + name + "' OK");

                    onSuccess(newDb);
                }, function () {
                    console.warn("Database Init: '" + name + "' Fail");
                    onError();
                });
            }
        },
        cookie: {
            get(cookieName) {
                const name = `${cookieName}=`;
                const decodedCookie = decodeURIComponent(document.cookie);
                const ca = decodedCookie.split(";");

                for (let c of ca) {
                    while (c.charAt(0) === " ") {
                        c = c.substring(1);
                    }
                    if (c.indexOf(name) === 0) {
                        return decodeURIComponent(c.substring(name.length, c.length));
                    }
                }
                return "";
            },
            set(cookieName, cookieValue, expirationDays) {
                expirationDays = expirationDays || 3;
                const d = new Date();
                d.setTime(d.getTime() + (expirationDays * 24 * 60 * 60 * 1000));

                // Infinite January 19th, 2038
                if (expirationDays === -1) {
                    d.setTime(2147483646 * 1000);
                }
                const expires = `expires=${d.toUTCString()}`;
                document.cookie = `${cookieName}=${encodeURIComponent(cookieValue)};${expires};path=/`;
            },
            delete(cookieName) {
                const d = new Date();
                d.setTime(d.getTime() - 1);
                const expires = `expires=${d.toUTCString()}`;
                document.cookie = `${cookieName}=;${expires};path=/`;
            }
        } // End cookie
    } // End storage

    /**
     * Pure browser related methods
     */
    core.browser = {
        /**
         * Returns browser, version
         * @param {any} getBrowserVersionOrBoth
         */
        get(getBrowserVersionOrBoth = "BOTH") {
            var browser, position, end, version;

            //Detect browser and write the corresponding name

            if (window.navigator.userAgent.indexOf("Edge") > -1) {
                browser = core.enums.browsers.EDGE;
                position = navigator.userAgent.search("Edge") + 5;
                end = navigator.userAgent.length;
                version = navigator.userAgent.substring(position, end);

                switch (getBrowserVersionOrBoth.toLowerCase()) {
                    case "browser":
                        return browser;
                    case "version":
                        return version;
                    default:
                        return browser + " (" + version + ")";
                }
            }
            else if (navigator.userAgent.search("MSIE") >= 0) {
                browser = core.enums.browsers.INTERNET_EXPLORER;
                position = navigator.userAgent.search("MSIE") + 5;
                end = navigator.userAgent.search("; Windows");
                version = navigator.userAgent.substring(position, end);

                switch (getBrowserVersionOrBoth.toLowerCase()) {
                    case "browser":
                        return browser;
                    case "version":
                        return version;
                    default:
                        return browser + " (" + version + ")";
                }
            }
            else if (navigator.userAgent.search("Chrome") >= 0) {
                browser = core.enums.browsers.CHROME;
                position = navigator.userAgent.search("Chrome") + 7;
                end = navigator.userAgent.search(" Safari");
                version = navigator.userAgent.substring(position, end);

                switch (getBrowserVersionOrBoth.toLowerCase()) {
                    case "browser":
                        return browser;
                    case "version":
                        return version;
                    default:
                        return browser + " (" + version + ")";
                }
            }
            else if (navigator.userAgent.search("Firefox") >= 0) {
                browser = core.enums.browsers.FIREFOX;
                position = navigator.userAgent.search("Firefox") + 8;
                version = navigator.userAgent.substring(position);

                switch (getBrowserVersionOrBoth.toLowerCase()) {
                    case "browser":
                        return browser;
                    case "version":
                        return version;
                    default:
                        return browser + " (" + version + ")";
                }
            }
            else if (navigator.userAgent.search("Safari") >= 0 && navigator.userAgent.search("Chrome") < 0) {//<< Here
                browser = core.enums.browsers.SAFARI;
                position = navigator.userAgent.search("Version") + 8;
                end = navigator.userAgent.search(" Safari");
                version = navigator.userAgent.substring(position, end);

                switch (getBrowserVersionOrBoth.toLowerCase()) {
                    case "browser":
                        return browser;
                    case "version":
                        return version;
                    default:
                        return browser + " (" + version + ")";
                }
            }
            else if (navigator.userAgent.search("Opera") >= 0) {
                browser = core.enums.browsers.OPERA;
                position = navigator.userAgent.search("Version") + 8;
                version = navigator.userAgent.substring(position);

                switch (getBrowserVersionOrBoth.toLowerCase()) {
                    case "browser":
                        return browser;
                    case "version":
                        return version;
                    default:
                        return browser + " (" + version + ")";
                }
            }
            else {
                browser = core.enums.browsers.UNKNOWN;

                switch (getBrowserVersionOrBoth.toLowerCase()) {
                    case "browser":
                        return browser;
                    case "version":
                        return "0";
                    default:
                        return browser + " (0)";
                }
            }
        },
        getBrowser() {
            return core.browser.get("BROWSER");
        },
        getVersion() {
            return core.browser.get("VERSION");
        },
        isBrowser(browsersAr = []) {
            if (browsersAr !== "string" && browsersAr.length === 1) {
                browsersAr = browsersAr[0];
            }
            const currentBrowser = core.browser.get("BROWSER");
            return core.utils.contains(browsersAr, currentBrowser);
        }
    }

    /**
     * Security en encryption related methods
     */
    core.security = {
        sha512(str) {
            function Int64(msint32, lsint32) {
                this.highOrder = msint32;
                this.lowOrder = lsint32;
            }

            const H = [new Int64(0x6a09e667, 0xf3bcc908), new Int64(0xbb67ae85, 0x84caa73b),
            new Int64(0x3c6ef372, 0xfe94f82b), new Int64(0xa54ff53a, 0x5f1d36f1),
            new Int64(0x510e527f, 0xade682d1), new Int64(0x9b05688c, 0x2b3e6c1f),
            new Int64(0x1f83d9ab, 0xfb41bd6b), new Int64(0x5be0cd19, 0x137e2179)];

            const K = [new Int64(0x428a2f98, 0xd728ae22), new Int64(0x71374491, 0x23ef65cd),
            new Int64(0xb5c0fbcf, 0xec4d3b2f), new Int64(0xe9b5dba5, 0x8189dbbc),
            new Int64(0x3956c25b, 0xf348b538), new Int64(0x59f111f1, 0xb605d019),
            new Int64(0x923f82a4, 0xaf194f9b), new Int64(0xab1c5ed5, 0xda6d8118),
            new Int64(0xd807aa98, 0xa3030242), new Int64(0x12835b01, 0x45706fbe),
            new Int64(0x243185be, 0x4ee4b28c), new Int64(0x550c7dc3, 0xd5ffb4e2),
            new Int64(0x72be5d74, 0xf27b896f), new Int64(0x80deb1fe, 0x3b1696b1),
            new Int64(0x9bdc06a7, 0x25c71235), new Int64(0xc19bf174, 0xcf692694),
            new Int64(0xe49b69c1, 0x9ef14ad2), new Int64(0xefbe4786, 0x384f25e3),
            new Int64(0x0fc19dc6, 0x8b8cd5b5), new Int64(0x240ca1cc, 0x77ac9c65),
            new Int64(0x2de92c6f, 0x592b0275), new Int64(0x4a7484aa, 0x6ea6e483),
            new Int64(0x5cb0a9dc, 0xbd41fbd4), new Int64(0x76f988da, 0x831153b5),
            new Int64(0x983e5152, 0xee66dfab), new Int64(0xa831c66d, 0x2db43210),
            new Int64(0xb00327c8, 0x98fb213f), new Int64(0xbf597fc7, 0xbeef0ee4),
            new Int64(0xc6e00bf3, 0x3da88fc2), new Int64(0xd5a79147, 0x930aa725),
            new Int64(0x06ca6351, 0xe003826f), new Int64(0x14292967, 0x0a0e6e70),
            new Int64(0x27b70a85, 0x46d22ffc), new Int64(0x2e1b2138, 0x5c26c926),
            new Int64(0x4d2c6dfc, 0x5ac42aed), new Int64(0x53380d13, 0x9d95b3df),
            new Int64(0x650a7354, 0x8baf63de), new Int64(0x766a0abb, 0x3c77b2a8),
            new Int64(0x81c2c92e, 0x47edaee6), new Int64(0x92722c85, 0x1482353b),
            new Int64(0xa2bfe8a1, 0x4cf10364), new Int64(0xa81a664b, 0xbc423001),
            new Int64(0xc24b8b70, 0xd0f89791), new Int64(0xc76c51a3, 0x0654be30),
            new Int64(0xd192e819, 0xd6ef5218), new Int64(0xd6990624, 0x5565a910),
            new Int64(0xf40e3585, 0x5771202a), new Int64(0x106aa070, 0x32bbd1b8),
            new Int64(0x19a4c116, 0xb8d2d0c8), new Int64(0x1e376c08, 0x5141ab53),
            new Int64(0x2748774c, 0xdf8eeb99), new Int64(0x34b0bcb5, 0xe19b48a8),
            new Int64(0x391c0cb3, 0xc5c95a63), new Int64(0x4ed8aa4a, 0xe3418acb),
            new Int64(0x5b9cca4f, 0x7763e373), new Int64(0x682e6ff3, 0xd6b2b8a3),
            new Int64(0x748f82ee, 0x5defb2fc), new Int64(0x78a5636f, 0x43172f60),
            new Int64(0x84c87814, 0xa1f0ab72), new Int64(0x8cc70208, 0x1a6439ec),
            new Int64(0x90befffa, 0x23631e28), new Int64(0xa4506ceb, 0xde82bde9),
            new Int64(0xbef9a3f7, 0xb2c67915), new Int64(0xc67178f2, 0xe372532b),
            new Int64(0xca273ece, 0xea26619c), new Int64(0xd186b8c7, 0x21c0c207),
            new Int64(0xeada7dd6, 0xcde0eb1e), new Int64(0xf57d4f7f, 0xee6ed178),
            new Int64(0x06f067aa, 0x72176fba), new Int64(0x0a637dc5, 0xa2c898a6),
            new Int64(0x113f9804, 0xbef90dae), new Int64(0x1b710b35, 0x131c471b),
            new Int64(0x28db77f5, 0x23047d84), new Int64(0x32caab7b, 0x40c72493),
            new Int64(0x3c9ebe0a, 0x15c9bebc), new Int64(0x431d67c4, 0x9c100d4c),
            new Int64(0x4cc5d4be, 0xcb3e42b6), new Int64(0x597f299c, 0xfc657e2a),
            new Int64(0x5fcb6fab, 0x3ad6faec), new Int64(0x6c44198c, 0x4a475817)];

            const W = new Array(64);
            const charsize = 8;
            let a, b, c, d, e, f, g, h, T1, T2;
            var i, j;

            function utf8_encode(str) {
                return unescape(encodeURIComponent(str));
            }

            function str2binb(str) {
                const bin = [];
                const mask = (1 << charsize) - 1;
                const len = str.length * charsize;

                for (let i = 0; i < len; i += charsize) {
                    bin[i >> 5] |= (str.charCodeAt(i / charsize) & mask) << (32 - charsize - (i % 32));
                }

                return bin;
            }

            function binb2hex(binarray) {
                const hex_tab = "0123456789abcdef";
                let str = "";
                const length = binarray.length * 4;
                let srcByte;

                for (let i = 0; i < length; i += 1) {
                    srcByte = binarray[i >> 2] >> ((3 - (i % 4)) * 8);
                    str += hex_tab.charAt((srcByte >> 4) & 0xF) + hex_tab.charAt(srcByte & 0xF);
                }

                return str;
            }

            function safe_add_2(x, y) {
                let lsw;
                let msw;
                let lowOrder;
                let highOrder;

                lsw = (x.lowOrder & 0xFFFF) + (y.lowOrder & 0xFFFF);
                msw = (x.lowOrder >>> 16) + (y.lowOrder >>> 16) + (lsw >>> 16);
                lowOrder = ((msw & 0xFFFF) << 16) | (lsw & 0xFFFF);

                lsw = (x.highOrder & 0xFFFF) + (y.highOrder & 0xFFFF) + (msw >>> 16);
                msw = (x.highOrder >>> 16) + (y.highOrder >>> 16) + (lsw >>> 16);
                highOrder = ((msw & 0xFFFF) << 16) | (lsw & 0xFFFF);

                return new Int64(highOrder, lowOrder);
            }

            function safe_add_4(a, b, c, d) {
                let lsw;
                let msw;
                let lowOrder;
                let highOrder;

                lsw = (a.lowOrder & 0xFFFF) + (b.lowOrder & 0xFFFF) + (c.lowOrder & 0xFFFF) + (d.lowOrder & 0xFFFF);
                msw = (a.lowOrder >>> 16) + (b.lowOrder >>> 16) + (c.lowOrder >>> 16) + (d.lowOrder >>> 16) + (lsw >>> 16);
                lowOrder = ((msw & 0xFFFF) << 16) | (lsw & 0xFFFF);

                lsw = (a.highOrder & 0xFFFF) + (b.highOrder & 0xFFFF) + (c.highOrder & 0xFFFF) + (d.highOrder & 0xFFFF) + (msw >>> 16);
                msw = (a.highOrder >>> 16) + (b.highOrder >>> 16) + (c.highOrder >>> 16) + (d.highOrder >>> 16) + (lsw >>> 16);
                highOrder = ((msw & 0xFFFF) << 16) | (lsw & 0xFFFF);

                return new Int64(highOrder, lowOrder);
            }

            function safe_add_5(a, b, c, d, e) {
                let lsw;
                let msw;
                let lowOrder;
                let highOrder;

                lsw = (a.lowOrder & 0xFFFF) + (b.lowOrder & 0xFFFF) + (c.lowOrder & 0xFFFF) + (d.lowOrder & 0xFFFF) + (e.lowOrder & 0xFFFF);
                msw = (a.lowOrder >>> 16) + (b.lowOrder >>> 16) + (c.lowOrder >>> 16) + (d.lowOrder >>> 16) + (e.lowOrder >>> 16) + (lsw >>> 16);
                lowOrder = ((msw & 0xFFFF) << 16) | (lsw & 0xFFFF);

                lsw = (a.highOrder & 0xFFFF) + (b.highOrder & 0xFFFF) + (c.highOrder & 0xFFFF) + (d.highOrder & 0xFFFF) + (e.highOrder & 0xFFFF) + (msw >>> 16);
                msw = (a.highOrder >>> 16) + (b.highOrder >>> 16) + (c.highOrder >>> 16) + (d.highOrder >>> 16) + (e.highOrder >>> 16) + (lsw >>> 16);
                highOrder = ((msw & 0xFFFF) << 16) | (lsw & 0xFFFF);

                return new Int64(highOrder, lowOrder);
            }

            function maj(x, y, z) {
                return new Int64(
                    (x.highOrder & y.highOrder) ^ (x.highOrder & z.highOrder) ^ (y.highOrder & z.highOrder),
                    (x.lowOrder & y.lowOrder) ^ (x.lowOrder & z.lowOrder) ^ (y.lowOrder & z.lowOrder)
                );
            }

            function ch(x, y, z) {
                return new Int64(
                    (x.highOrder & y.highOrder) ^ (~x.highOrder & z.highOrder),
                    (x.lowOrder & y.lowOrder) ^ (~x.lowOrder & z.lowOrder)
                );
            }

            function rotr(x, n) {
                if (n <= 32) {
                    return new Int64(
                        (x.highOrder >>> n) | (x.lowOrder << (32 - n)),
                        (x.lowOrder >>> n) | (x.highOrder << (32 - n))
                    );
                } else {
                    return new Int64(
                        (x.lowOrder >>> n) | (x.highOrder << (32 - n)),
                        (x.highOrder >>> n) | (x.lowOrder << (32 - n))
                    );
                }
            }

            function sigma0(x) {
                const rotr28 = rotr(x, 28);
                const rotr34 = rotr(x, 34);
                const rotr39 = rotr(x, 39);

                return new Int64(
                    rotr28.highOrder ^ rotr34.highOrder ^ rotr39.highOrder,
                    rotr28.lowOrder ^ rotr34.lowOrder ^ rotr39.lowOrder
                );
            }

            function sigma1(x) {
                const rotr14 = rotr(x, 14);
                const rotr18 = rotr(x, 18);
                const rotr41 = rotr(x, 41);

                return new Int64(
                    rotr14.highOrder ^ rotr18.highOrder ^ rotr41.highOrder,
                    rotr14.lowOrder ^ rotr18.lowOrder ^ rotr41.lowOrder
                );
            }

            function gamma0(x) {
                const rotr1 = rotr(x, 1);
                const rotr8 = rotr(x, 8);
                const shr7 = shr(x, 7);

                return new Int64(
                    rotr1.highOrder ^ rotr8.highOrder ^ shr7.highOrder,
                    rotr1.lowOrder ^ rotr8.lowOrder ^ shr7.lowOrder
                );
            }

            function gamma1(x) {
                const rotr19 = rotr(x, 19);
                const rotr61 = rotr(x, 61);
                const shr6 = shr(x, 6);

                return new Int64(
                    rotr19.highOrder ^ rotr61.highOrder ^ shr6.highOrder,
                    rotr19.lowOrder ^ rotr61.lowOrder ^ shr6.lowOrder
                );
            }

            function shr(x, n) {
                if (n <= 32) {
                    return new Int64(
                        x.highOrder >>> n,
                        x.lowOrder >>> n | (x.highOrder << (32 - n))
                    );
                } else {
                    return new Int64(
                        0,
                        x.highOrder << (32 - n)
                    );
                }
            }

            str = utf8_encode(str);
            strlen = str.length * charsize;
            str = str2binb(str);

            str[strlen >> 5] |= 0x80 << (24 - strlen % 32);
            str[(((strlen + 128) >> 10) << 5) + 31] = strlen;

            for (var i = 0; i < str.length; i += 32) {
                a = H[0];
                b = H[1];
                c = H[2];
                d = H[3];
                e = H[4];
                f = H[5];
                g = H[6];
                h = H[7];

                for (var j = 0; j < 80; j++) {
                    if (j < 16) {
                        W[j] = new Int64(str[j * 2 + i], str[j * 2 + i + 1]);
                    } else {
                        W[j] = safe_add_4(gamma1(W[j - 2]), W[j - 7], gamma0(W[j - 15]), W[j - 16]);
                    }

                    T1 = safe_add_5(h, sigma1(e), ch(e, f, g), K[j], W[j]);
                    T2 = safe_add_2(sigma0(a), maj(a, b, c));
                    h = g;
                    g = f;
                    f = e;
                    e = safe_add_2(d, T1);
                    d = c;
                    c = b;
                    b = a;
                    a = safe_add_2(T1, T2);
                }

                H[0] = safe_add_2(a, H[0]);
                H[1] = safe_add_2(b, H[1]);
                H[2] = safe_add_2(c, H[2]);
                H[3] = safe_add_2(d, H[3]);
                H[4] = safe_add_2(e, H[4]);
                H[5] = safe_add_2(f, H[5]);
                H[6] = safe_add_2(g, H[6]);
                H[7] = safe_add_2(h, H[7]);
            }

            const binarray = [];
            for (var i = 0; i < H.length; i++) {
                binarray.push(H[i].highOrder);
                binarray.push(H[i].lowOrder);
            }
            return binb2hex(binarray).toUpperCase();
        }
    }

    /**
     * Caching related, html5SQL
     */
    core.caching = {
        onCacheReady(onReady = core.utils.noop, onFail = core.utils.noop) {
            if (cacheReady) {
                onReady();
                return true;
            }

            const onError = () => {
                console.warn("[jjl:caching] Action aborted! Cache not ready due to error!");
                onFail();
            }

            // Test for main library
            if (!window.html5sql) {
                console.warn("[jjl:Caching] Unable to use caching! \n    Unable to load 'html5sql' library. \n    Please load 'html5sql.js' ");
                onError();
                return false;
            };

            // Setup databases
            // Open database
            html5sql.openDatabase("JuiceCache", "JuiceCache", 40 * 1024 * 1024);

            // create table if not exists
            const queryArray = [
                {
                    sql:
                        "CREATE TABLE IF NOT EXISTS cached_templates ( `ID` INTEGER PRIMARY KEY ASC, name varchar(255) UNIQUE, content TEXT,type varchar(255), timestamp varchar(255));" // No enters!
                }, {
                    sql: "CREATE TABLE IF NOT EXISTS cache_settings ( `ID` INTEGER PRIMARY KEY ASC, name varchar(255) UNIQUE, value TEXT);"
                }
            ];

            const onSuccess = () => {
                cacheReady = true;
                onReady();
            }

            // Start processing
            html5sql.process(queryArray, onSuccess, onError);


            return false;
        },

        // get latest templates from Wiser and put in html5 storage
        update(onDone = core.utils.noop) {
            const onFail = () => { console.warn("[jjl:caching] Unable to update caching!"); };
            core.caching.onCacheReady(() => {
                console.log("Start updating cache...");

                core.caching.getSettingsObject((templateSettings) => {
                    var xhrReq = new core.models.XhrRequestModel();
                    xhrReq.method = "GET";
                    xhrReq.url = core.settings.serviceUrl + "/mobile_update.jcl";
                    xhrReq.parameters = { trace: false, minify: true, timestamp: templateSettings.last_update || "", languageid: core.settings.languageId, filter: core.settings.cacheFilter };
                    core.data.getApi(xhrReq, (xhrResult) => {
                        const updateSettings = xhrResult.value || {};
                        const templatesToCache = updateSettings.template || [];
                        if (templatesToCache.length === 0) {
                            onDone(xhrResult.value);
                            return;
                        }

                        let query = "INSERT OR REPLACE INTO cached_templates (name, type, content, timestamp) VALUES (?, ?, ?, ?)";
                        let data = [];
                        let cnt = 0;
                        for (let i = 0; i < templatesToCache.length; i++) {
                            const template = templatesToCache[i];

                            if (template.type === "QUERY") {
                                continue;
                            }


                            const additionRow = ", (?, ?, ?, ?)";
                            let additions = "";
                            if (cnt > 0) {
                                query += additionRow;
                            }

                            data.push(template.name);
                            data.push(template.type);
                            data.push(template.data);
                            data.push(xhrResult.value.timestamp);
                            cnt++;
                        }
                        core.caching.setSetting("last_update", xhrResult.value.timestamp);
                        html5sql.process([{ sql: query, data: data }], () => { onDone(xhrResult.value); });
                    });
                });
            });
        },


        setSetting(settingName, settingValue, callback = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query = "INSERT OR REPLACE INTO cache_settings (name, value) VALUES (?, ?)";
                const data = [settingName, settingValue];

                html5sql.process([{ sql: query, data: data }], callback);
            });
        },

        getSetting(settingName, callback = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query = "SELECT value FROM cache_settings WHERE name=?";
                const data = [settingName];

                html5sql.process([{ sql: query, data: data }], (trans, result) => {
                    var returnResult = null;
                    if (result && result.rows && result.rows.length > 0 && result.rows[0].value) {
                        returnResult = result.rows[0].value;
                    }
                    if (callback === core.utils.noop) {
                        console.log(returnResult);
                    } else {
                        callback(returnResult);
                    }
                });
            });
        },

        getSettingsObject(callback = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query = "SELECT name, value FROM cache_settings";

                html5sql.process([{ sql: query }], (trans, result) => {
                    const cSettings = {};
                    if (result && result.rows && result.rows) {
                        for (let i = 0; i < result.rows.length; i++) {
                            cSettings[result.rows[i].name] = result.rows[i].value;
                        }
                    }

                    if (callback === core.utils.noop) {
                        console.log(cSettings);
                    } else {
                        callback(cSettings);
                    }
                });
            });
        },

        deleteSetting(settingName, callback = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query = "DELETE FROM cache_settings WHERE name=?";
                const data = [settingName];

                html5sql.process([{ sql: query, data: data }], callback);
            });
        },

        setTemplate(templateName, type, content, expires, callback = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query = "INSERT OR REPLACE INTO cached_templates (name, type, content, timestamp) VALUES (?, ?, ?, ?)";
                const data = [templateName, type, content, expires];

                html5sql.process([{ sql: query, data: data }], callback);
            });
        },

        getTemplate(templateIdOrName, callback = core.utils.noop) {
            core.caching.onCacheReady(() => {
                let query = "SELECT content FROM cached_templates WHERE name=?";

                if (typeof templateIdOrName === "number") {
                    query = "SELECT content FROM cached_templates WHERE ID=?";
                }

                const data = [templateIdOrName];

                html5sql.process([{ sql: query, data: data }], (trans, result) => {
                    var returnResult = null;
                    if (result && result.rows && result.rows[0].content) {
                        returnResult = result.rows[0].content;
                    }
                    if (callback === core.utils.noop) {
                        console.log(returnResult);
                    } else {
                        callback(returnResult);
                    }
                });
            });
        },

        getTemplatesObject(callback = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query = "SELECT ID, name, `type`, content, expires FROM cached_templates";

                html5sql.process([{ sql: query }], (trans, result) => {
                    const templates = {};
                    class Template {
                        constructor() {
                            this.id = null;
                            this.name = null;
                            this.type = null;
                            this.content = null;
                            this.expires = null;
                        }
                    }
                    if (result && result.rows && result.rows) {
                        for (let i = 0; i < result.rows.length; i++) {
                            const newTemplate = new Template();

                            newTemplate.id = result.rows[i].ID;
                            newTemplate.name = result.rows[i].name;
                            newTemplate.type = result.rows[i].type;
                            newTemplate.content = result.rows[i].content;
                            newTemplate.expires = result.rows[i].expires;

                            templates[result.rows[i].name] = newTemplate;
                        }
                    }

                    if (callback === core.utils.noop) {
                        console.log(templates);
                    } else {
                        callback(templates);
                    }
                });
            });
        },

        deleteTemplate(templateIdOrName, callback = core.utils.noop) {
            core.caching.onCacheReady(() => {
                let query = "DELETE FROM cache_settings WHERE name=?";

                if (typeof templateIdOrName === "number") {
                    query = "DELETE FROM cache_settings WHERE ID=?";
                }

                const data = [templateIdOrName];

                html5sql.process([{ sql: query, data: data }], callback);
            });
        },

        reloadAll(onDone = core.utils.noop) {
            core.caching.onCacheReady(() => {
                core.caching.setSetting("last_update", "", () => {
                    core.caching.update(onDone);
                });
            });
        },
        attachScripts(onDone = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query = "SELECT name, content FROM cached_templates WHERE type = 'SCRIPTS'";

                html5sql.process([{ sql: query }], (trans, result) => {
                    if (result && result.rows && result.rows.length > 0) {
                        for (let i = 0; i < result.rows.length; i++) {
                            const identifier = `JCacheScript-${result.rows[i].name}`;
                            const content = result.rows[i].content;
                            // Remove from body
                            core(`#${identifier}`).remove();

                            const script = document.createElement("script");
                            script.setAttribute("class", "JCacheScript");
                            script.id = identifier;
                            script.type = "text/javascript";
                            script.innerHTML = content;

                            document.body.appendChild(script);
                        }

                        onDone();
                    } else {
                        onDone();
                    }
                });
            });
        },
        clear(onDone = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query1 = "DELETE FROM cached_templates";
                const query2 = "DELETE FROM cache_settings";

                html5sql.process([{ sql: query1 }, { sql: query2 }], () => {
                    onDone();
                });
            });
        },

        attachHtml(idOfTargetDomElement, onDone = core.utils.noop) {
            core.caching.onCacheReady(() => {
                const query = "SELECT name, content FROM cached_templates WHERE type = 'HTML'";

                if (core(idOfTargetDomElement).length < 1) {
                    console.warn("[jjl:caching] Unable to attach html, invalid or nonexisting identifier");
                    return;
                }

                var targetElement = core(idOfTargetDomElement).first();

                html5sql.process([{ sql: query }], (trans, result) => {
                    if (result && result.rows && result.rows.length > 0) {
                        for (let i = 0; i < result.rows.length; i++) {
                            let content = result.rows[i].content;

                            if (core.settings.cacheWrapHtmlTemplates) {
                                const identifier = `JCacheHtml-${result.rows[i].name}`;
                                content = `<div class="JCacheHtml" id="${identifier}" style="display:none">${content}</div>`;

                                // Remove the old one 
                                core(`#${identifier}`).remove();
                            }

                            targetElement.insertAdjacentHTML("beforeEnd", content);
                        }

                        onDone();
                    } else {
                        onDone();
                    }
                });
            });
        },
        attachAll(htmlTarget = "body", onDone = core.utils.noop) {
            core.caching.onCacheReady(() => {
                core.caching.attachHtml(htmlTarget, () => {
                    core.caching.attachScripts(onDone);
                });
            });
        }
    } // End Caching


    class JProcess {
        constructor(processId, isBlocking) {
            this.processId = processId;
            this.isBlocking = isBlocking;
            this.created = Date.now();
            this.createdDateTime = new Date().toLocaleString();
        }
    }

    /*core.processing = {
        currentProcesses: {},
        onIdleFns: [],
        tempFn: function () {
        },

        addProcess(processId, isBlocking = true) {
            if (core.processing.currentProcesses[processId]) {
                console.warn("Cannot add process, id already exists");
                return;
            }
            // todo: send busy event
            core.debug.log("Event: processing.Busy");
            document.dispatchEvent(new CustomEvent("processing.Busy"));
            core.processing.currentProcesses[processId] = new JProcess(processId, isBlocking);
        },

        removeProcess(processId) {
            if (!core.processing.currentProcesses[processId]) {
                console.warn("Cannot remove process, id does not exist");
                return;
            }

            delete core.processing.currentProcesses[processId];

            if (Object.keys(core.processing.currentProcesses).length < 1) {
                core.debug.log("Event: processing.Idle");
                document.dispatchEvent(new CustomEvent("processing.Idle"));
            }
        },

        busy(showActiveProcesses = false) {
            if (Object.keys(core.processing.currentProcesses).length > 0) {
                if (showActiveProcesses) this.showProcesses();
                return true;
            } else {
                return false;
            }
        },

        showProcesses() {
            console.log("Active processes: " + Object.keys(core.processing.currentProcesses).length + ":",
                core.processing.currentProcesses);
        },

        clearProcesses() {
            core.processing.currentProcesses = {};
        },

        onDone(fn) {
            core.processing.onIdleFns.push(fn);

            if (core.processing.busy()) {
                document.addEventListener("processing.Idle", core.processing.runOnIdleFns);
            } else {
                core.processing.runOnIdleFns();
            }
        },

        runOnIdleFns() {
            for (let i = 0; i < core.processing.onIdleFns.length; i++) {
                core.processing.onIdleFns[i]();
            }

            core.processing.onIdleFns = [];
        }
    };*/

    class JjlFormInput {
        constructor(inputElement, forceInstantValidate = null) {
            this.elements = [inputElement];
            this.identifier = this.elements[0].name;
            this.value = (this.elements[0].type === "checkbox") ? "" : this.elements[0].value;

            this.settings = core.convert.autoFormat(core([inputElement]).getDataAttrAsObj("jjlform", true));
            this.settings.instantvalidate = this.settings.instantvalidate || forceInstantValidate || false;

            // bind onchange if instantValidate
            if (this.settings.instantvalidate) {
                this.elements[0].addEventListener("change", this.validate.bind(this));
                this.elements[0].addEventListener("keyup", this.validate.bind(this));
                // this.elements[0].addEventListener("click", this.validate.bind(this));
            }

            this.update();
        }

        addElement(inputElement) {
            this.elements.push(inputElement);

            // bind onchange if instantValidate
            if (this.settings.instantvalidate) {
                inputElement.addEventListener("change", this.validate.bind(this));
                inputElement.addEventListener("keyup", this.validate.bind(this));
                //inputElement.addEventListener("click", this.validate.bind(this));

            }
        }

        update() {
            const valueAr = []; //this.value.toString().indexOf(",") === -1 ? [] : this.value.split(",");

            console.log(this.elements);

            for (let i = 0; i < this.elements.length; i++) {
                const element = this.elements[i];

                const elementExistsOnDom = document.body.contains(element);

                // Only handle active elements
                if (!elementExistsOnDom) continue;

                if (element.type === "checkbox" || element.type === "radio") {
                    if (element.value === "on" || element.value === undefined) {
                        // Checkbox without value
                        valueAr.push(element.checked);
                    } else {
                        if (element.checked) {
                            if (valueAr.indexOf(element.value) === -1) valueAr.push(element.value);
                        } else {
                            if (valueAr.indexOf(element.value) !== -1) {
                                valueAr.splice(valueAr.indexOf(element.value), 1);
                            }
                        }
                    }
                } else {
                    if (valueAr.indexOf(element.value) === -1) valueAr.push(element.value);
                }
            }

            const valueString = valueAr.toString();
            this.value = (valueString === "true" || valueString === "false") ? core.convert.toBool(valueString) : (valueAr.length === 1) ? valueAr[0] : (valueAr.length === 0) ? "" : valueAr; //.toString();
        }

        validate(event) {
            this.update();
            if (!this.settings.validate) return true;
            if (core(this.elements).hasClass("noValidate")) {
                return true;
            }

            if (core(this.elements).hasClass("optional") && this.value !== undefined && this.value === "") {
                return true;
            }


            const validationType = this.settings.validate;

            // remove errors to start
            core(this.elements).removeClass("invalid");
            core(this.elements).removeClass("valid");

            // never check empty inputs on event, but return false
            if (this.value === "" && event && validationType !== "multiselect") return false;

            // check for elements that depend on specific values
            const dependsOnElement = this.settings.validateDependsonElement;
            const dependsOnElementValue = core("input[name='" + dependsOnElement + "']:checked").first().value;

            if (dependsOnElement && (dependsOnElementValue !== this.settings.validateDependsonValue.toString())) {
                // skip
                return true;
            }

            // first check if it is a regex
            if (validationType.substring(validationType.length - 1) === "/" && validationType.substring(-1, 1) === "/") {
                const rx = validationType.substring(1, validationType.length - 1);
                if (new RegExp(rx).test(this.value)) {
                    core(this.elements).addClass("valid");
                    return true;
                } else {
                    core(this.elements).addClass("invalid");
                    return false;
                }
            }

            const validateByRx = (rx, val = this.value, setClass = true) => {
                if (rx.test(val)) {
                    if (setClass) core(this.elements).addClass("valid");
                    return true;
                } else {
                    if (setClass) core(this.elements).addClass("invalid");
                    return false;
                }

            }

            // Check other types
            switch (validationType) {
                case "password":
                    return true;

                case "select":
                    if (this.value.length > 0) {
                        core(this.elements).addClass("valid");
                        return true;
                    } else {
                        core(this.elements).addClass("invalid");
                        return false;
                    }

                case "compare":
                    var compareFieldName = this.settings.validateOption;
                    var compareFieldValue = core(`input[name='${compareFieldName}']`).first().value;
                    var currentFieldValue = core(this.elements).first().value;

                    if (compareFieldValue === currentFieldValue) {
                        core(this.elements).addClass("valid");
                        core(".passwordCompareMessage").hide();
                        return true;
                    } else {
                        core(this.elements).addClass("invalid");
                        core(".passwordCompareMessage").show();
                        return false;
                    }

                case "multiselect":
                    var numSelected = (this.value.length && typeof this.value === "object") ? this.value.length : (this.value !== "") ? 1 : 0;// (this.value !== "" ? this.value.split(",") : []).length;
                    var result = (numSelected >= (this.settings.multiselectMin || -1) && numSelected <= (this.settings.multiselectMax || 99999));
                    // on Max reached undo click
                    if (numSelected > (this.settings.multiselectMax || 99999)) {
                        if (this.settings.multiselectMaxmessage) alert(this.settings.multiselectMaxmessage);
                        if (event) {
                            console.log(event.currentTarget);
                            event.currentTarget.checked = !event.currentTarget.checked;
                            this.update();
                        }
                    }
                    // re-check
                    numSelected = (this.value.length && typeof this.value === "object") ? this.value.length : (this.value !== "") ? 1 : 0; //(this.value !== "" ? this.value.split(",") : []).length;
                    result = (numSelected >= (this.settings.multiselectMin || -1) && numSelected <= (this.settings.multiselectMax || 99999));
                    core(this.elements).addClass((result ? "valid" : "invalid"));
                    return result;
                case "text":
                    return validateByRx(/.{2,}/);
                case "longtext":
                    return validateByRx(/.{10,}/);
                case "email":
                    return validateByRx(/^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/);
                case "phone":
                    return validateByRx(/^([\+]?)([0-9]{2,4})([ |-]?)(\(0\))?([ ]?)([0-9]{4,9})$/);
                case "numeric":
                    return validateByRx(/\d+/);
                case "housenumber":
                    return validateByRx(/^\d{1,} ?\w*$/);
                case "zipcode":
                case "postcode":
                    if (validateByRx(/^\d{4} ?\w{2}$/, this.value, false) || validateByRx(/^[1-9]\d{3}$/, this.value, false) || validateByRx(/^\d{5}$/, this.value, false) || validateByRx(/^([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9][A-Za-z]?))))\s?[0-9][A-Za-z]{2})$/, this.value, false)) {
                        core(this.elements).addClass("valid");
                        return true;
                    } else {
                        core(this.elements).addClass("invalid");
                        return false;
                    }
                case "vatnumber":
                    return validateByRx(/^(NL)?[0-9]{9}B[0-9]{2}$/);
                case "zipcode_nl":
                case "postcode_nl":
                    return validateByRx(/^\d{4} ?\w{2}$/);
                case "date":
                    if (validateByRx(/^((19|20)([0-9]{2}))-((10|11|12)|(0[0-9]{1}))-(([0-2]{1}[0-9]{1})|(30|31))$/, this.value, false) || validateByRx(/^(([0-2]{1}[0-9]{1})|(30|31))-((10|11|12)|(0[0-9]{1}))-((19|20)([0-9]{2}))$/, this.value, false)) {
                        core(this.elements).addClass("valid");
                        return true;
                    } else {
                        core(this.elements).addClass("invalid");
                        return false;
                    }
                case "selected":
                    var resultSelected = (this.value && this.value !== "") ? true : false;
                    core(this.elements).addClass((resultSelected ? "valid" : "invalid"));
                    return resultSelected;
                case "check":
                    core(this.elements).addClass((this.value ? "valid" : "invalid"));
                    return this.value;
                default:
                    console.log("invalid validationtype '" + validationType + "', valid types: (regex, text, longtext, email, phone, numeric)");
                    return false;
            }
        }
    }

    class JjlForm {
        constructor(formElement) {
            this.settings = core.convert.autoFormat(core([formElement]).getDataAttrAsObj("jjlform", true));
            this.element = formElement;
            this.identifier = this.element.id;
            this.inputs = {};

            this.update();
        }

        update() {
            // get all elements
            const inputs = document.querySelectorAll(`#${this.identifier} input,#${this.identifier} textarea,#${this.identifier} select`);

            // Return on no inputs
            if (inputs.length === 0) return;


            // Loop through to init and add if not exists
            for (let i = 0; i < inputs.length; i++) {
                const inputElement = inputs[i];

                const validElements = [HTMLInputElement, HTMLTextAreaElement, HTMLSelectElement];

                const checkType = (input) => {
                    for (let ii = 0; ii < validElements.length; ii++) {
                        if (input instanceof validElements[ii]) return true;
                    }
                    return false;
                };


                // If no correct element or no id, skip
                if (!(inputElement.id || inputElement.name) || !checkType(inputElement)) {
                    if (!core.forms.suppressWarnings) console.warn("[jjl-form] Unable to init inputelement '.jjl-form' name not set or no valid input element!", inputElement);
                    if (!core.forms.suppressWarnings) console.log(inputElement.name);
                    continue;
                }

                // if already exists, just update the form
                if (this.inputs[inputElement.name]) {
                    if (this.inputs[inputElement.name].elements.indexOf(inputElement) === -1) {
                        // Add element
                        this.inputs[inputElement.name].addElement(inputElement);
                    } else {
                        // Already exists, update
                        this.inputs[inputElement.name].update();
                    }
                    continue;
                }

                let instantValidate = null;
                if (this.settings.instantvalidate === true) {
                    instantValidate = true;
                }

                // New form to add
                this.inputs[inputElement.name] = new JjlFormInput(inputElement, instantValidate);
            }
        }

        /**
         * Validates the form and returns true of success and fail on false
         */
        validate() {
            // loop throuch form elements

            let isValid = true;

            // Check inputs
            for (let inputId in this.inputs) {
                if (this.inputs.hasOwnProperty(inputId)) {
                    if (this.inputs[inputId].validate() === false) {
                        isValid = false;
                    }
                }
            }

            // no invalid inputs so must be all good
            return isValid;
        }
    }


    core.forms = {
        suppressWarnings: false,
        forms: {},

        initForms() {
            const forms = document.getElementsByClassName("jjl-form");

            // Return on no forms
            if (forms.length === 0) return;


            // Loop through to init and add if not exists
            for (let i = 0; i < forms.length; i++) {
                const formElement = forms[i];

                // If no correct element or no id, skip
                if (!formElement.id || !(formElement instanceof HTMLFormElement)) {
                    if (!core.forms.suppressWarnings) console.warn("[jjl-form] Unable to init form '.jjl-form' id not set or no form element!");
                    continue;
                }

                // if already exists, just update the form
                if (this.forms[formElement.id]) {
                    this.forms[formElement.id].update();
                    continue;
                }

                // New form to add
                this.forms[formElement.id] = new JjlForm(formElement);
            }

        },

        validateForm(formElement) {
            if (!this.forms[formElement.id]) {
                console.warn("Form not found!");
                return false;
            }

            return this.forms[formElement.id].validate();
        },

        validatePassword(password, repeat, options) {


        },

        /**
         * 
         * @param {string} value The value that needs to be checked
         * @param {string} validationType Validation type to be checked
         * @param {object} settings validation type settings
         * @returns {boolean} validated
         */
        validateValue(value, validationType, settings = {}) {
            // never check empty inputs on event, but return false
            if (value === "" && validationType !== "multiselect") return false;

            // first check if it is a regex
            if (validationType.substring(validationType.length - 1) === "/" && validationType.substring(-1, 1) === "/") {
                const rx = validationType.substring(1, validationType.length - 1);
                if (new RegExp(rx).test(value)) {
                    return true;
                } else {
                    return false;
                }
            }

            const validateByRx = (rx, val = value, setClass = true) => {
                if (rx.test(val)) {
                    return true;
                } else {
                    return false;
                }
            };

            // Check other types
            switch (validationType) {
                case "multiselect":
                    var numSelected = (value.length && typeof value === "object") ? value.length : (value !== "") ? 1 : 0;
                    var result = (numSelected >= (settings.multiselectMin || -1) && numSelected <= (settings.multiselectMax || 99999));
                    // on Max reached undo click
                    if (numSelected > (settings.multiselectMax || 99999)) {
                        if (settings.multiselectMaxmessage) alert(settings.multiselectMaxmessage);
                    }
                    // re-check
                    numSelected = (value.length && typeof value === "object") ? value.length : (value !== "") ? 1 : 0; //(this.value !== "" ? this.value.split(",") : []).length;
                    result = numSelected >= (settings.multiselectMin || -1) && numSelected <= (settings.multiselectMax || 99999);
                    return result;
                case "text":
                    return validateByRx(/.{2,}/);
                case "longtext":
                    return validateByRx(/.{10,}/);
                case "email":
                    return validateByRx(/^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/);
                case "phone":
                    return validateByRx(/^([\+]?)([0-9]{2,4})([ |-]?)(\(0\))?([ ]?)([0-9]{4,9})$/);
                case "numeric":
                    return validateByRx(/\d+/);
                case "housenumber":
                    return validateByRx(/^\d{1,} ?\w*$/);
                case "zipcode":
                case "postcode":
                    if (validateByRx(/^\d{4} ?\w{2}$/, value, false) || validateByRx(/^[1-9]\d{3}$/, value, false) || validateByRx(/^\d{5}$/, value, false) || validateByRx(/^([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9][A-Za-z]?))))\s?[0-9][A-Za-z]{2})$/, this.value, false)) {
                        return true;
                    } else {
                        return false;
                    }
                case "zipcode_nl":
                case "postcode_nl":
                    return validateByRx(/^\d{4} ?\w{2}$/);
                case "date":
                    if (validateByRx(/^((19|20)([0-9]{2}))-((10|11|12)|(0[0-9]{1}))-(([0-2]{1}[0-9]{1})|(30|31))$/, this.value, false) || validateByRx(/^(([0-2]{1}[0-9]{1})|(30|31))-((10|11|12)|(0[0-9]{1}))-((19|20)([0-9]{2}))$/, this.value, false)) {
                        return true;
                    } else {
                        return false;
                    }
                case "selected":
                    var resultSelected = (value && value !== "") ? true : false;
                    return resultSelected;
                case "check":
                    return this.value;
                default:
                    console.log("invalid validationtype '" + validationType + "', valid types: (regex, text, longtext, email, phone, numeric)");
                    return false;
            }
        }
    }

    core.interactive = {
        enabled: false,

        enable() {
            this.enabled = true;

            // attach base listeners
            core.events.onElementWithClassAdded("jjl-interactive-click", (collection) => {
                console.log(collection);
                collection.on("click", this.onClick);
            });
            if (core(".jjl-interactive-click").length > 0) core(".jjl-interactive-click").on("click", this.onClick);
        },

        onClick(event) {
            const target = event.currentTarget;

            if (!core(target).hasClass("jjl-interactive-click")) {
                console.log("Type is not of class 'jjl_interactive'");
            }

            if (target.dataset.jjlInteractiveAction === undefined || target.dataset.jjlInteractiveKey === undefined || target.dataset.jjlInteractiveValue === undefined) {
                console.warn("[jjl-interactive-click] Cannot run actions. following must be set: (data-jjl-interactive-action, data-jjl-interactive-key, data-jjl-interactive-value)\nTarget:", target);
                return;
            }

            const actions = target.dataset.jjlInteractiveAction.replace("|", ";").split(";");
            const keys = target.dataset.jjlInteractiveKey.replace("|", ";").split(";");
            const values = target.dataset.jjlInteractiveValue.replace("|", ";").split(";");

            if (actions.length !== keys.length && keys.length !== values.length) {
                console.warn("[jjl-interactive-click] (item mismatch) Cannot run actions. actions, keys and values must contain same number of items.");
                return;
            }

            for (let i = 0; i < actions.length; i++) {
                const action = actions[i];
                let key = keys[i];
                const value = values[i];
                let qsObj = {};
                const evt = new CustomEvent('jjlInteractive', {
                    detail: {
                        type: "click",
                        action: action,
                        key: key,
                        value: value,
                        element: target
                    }
                });

                // Functions >>
                const qsToggle = () => {
                    let currentValues = core.querystring.checkIfSet(key) ? core.querystring.getValue(key).split(",") : [];
                    if (currentValues.indexOf(value) === -1) {
                        currentValues.push(value);
                    } else {
                        currentValues.splice(currentValues.indexOf(value), 1);
                    }

                    qsObj[key] = (currentValues.length === 0) ? null : currentValues.toString();
                    core.querystring.alter(qsObj);
                }
                // << End functions

                // catch event types
                switch (action) {
                    case "toggleclass":
                        key = (key === "this") ? core(target) : core(key);
                        key.toggleClass(value);
                        break;
                    case "qsset":
                        qsObj[key] = (value === "") ? null : value;
                        core.querystring.alter(qsObj);
                        break;
                    case "qstoggle":
                        qsToggle();
                        break;
                    case "setInnerText":
                        core(key).fill(value);
                        break;
                    case "favorite":
                        core.favorites.doAction(target, key, JSON.parse(value));
                        break;
                    case "redirect":
                        // code/ function call
                        console.log("Alex!!");
                        break;
                    case "count":
                        qsObj[key] = parseInt(core.querystring.getValue(key) || 0) + 1;//(value === "") ? null : value;
                        core.querystring.alter(qsObj);
                        break;
                    default:
                        document.dispatchEvent(evt);
                }
            }
        }
    }

    core.favorites = {
        favoriteClass: "favorite",
        doAction(target, key, settings) {
            const self = this,
                parameters = {
                    mode: (core(target).hasClass(this.favoriteClass) ? "remove" : "add"),
                    trace: false
                };

            parameters[key] = settings.id;

            const xhrReq = new core.models.XhrRequestModel();
            xhrReq.method = "GET";
            xhrReq.url = core.settings.serviceUrl + "/favorites.jcl";
            xhrReq.parameters = parameters;

            core.data.getApi(xhrReq,
                (data) => {
                    const r = JSON.parse(data.response);
                    if (data.isSuccess && r.success === true) {
                        if (parameters.mode === "add") {
                            core(target).addClass(self.favoriteClass);
                        } else if (parameters.mode === "remove") {
                            core(target).removeClass(self.favoriteClass);
                        }

                        if (typeof settings.callback === "string") {
                            var cbArr = settings.callback.split(".");
                            if (cbArr.length === 1) {
                                var _function = cbArr[0];
                                window[_function](target, parameters);
                            } else if (cbArr.length === 2) {
                                var _class = cbArr[0],
                                    _function = cbArr[1];
                                window[_class][_function](target, parameters);
                            }
                        }
                    } else {
                        //console.log("Test");
                    }
                });
        },
        enable() {
            core(".jjl_favorite[data-jjl-favorite-autofill=true]").each(function (i, target) {
                core.favorites.autofill(target);
            });
        },
        autofill(target) {
            const self = this,
                key = target.dataset.jjlInteractiveKey.replace("|", ";").split(";")[0],
                value = JSON.parse(target.dataset.jjlInteractiveValue.replace("|", ";").split(";")[0]),
                parameters = {
                    mode: "status",
                    trace: false
                };

            parameters[key] = value.id;

            const xhrReq = new core.models.XhrRequestModel();
            xhrReq.method = "GET";
            xhrReq.url = core.settings.serviceUrl + "/favorites.jcl";
            xhrReq.parameters = parameters;

            core.data.getApi(xhrReq,
                (data) => {
                    var r = JSON.parse(data.response);
                    if (data.isSuccess && r.isFavorite === true) {
                        jjl(target).addClass(self.favoriteClass);
                    }
                });
        }
    };

    core.filters = {
        identifiers: {
            productBlockId: "products",
            filterBlockClass: "filter",
            clearSelectionBtnClass: "removeSelection"
        },

        parameters: {

        },

        templateId: 0,

        target: "#filter-wrapper",

        onDone: core.utils.noop,

        enable() {
            // Only when page is ready
            core.ready(() => {
                /* Initialize filter-blocks when shown  and those already on page */
                core.events.onElementWithClassAdded(this.identifiers.filterBlockClass, this.initFilterBlock.bind(this));
                this.initFilterBlock(core(`.${this.identifiers.filterBlockClass}`));
            });
        },

        initFilterBlock(filterBlocks) {
            if (filterBlocks.length === 0) return;

            // Apply init to each block in collection
            for (let i = 0; i < filterBlocks.length; i++) {
                const filterBlock = filterBlocks[i];

                /* Init Event listeners */
                // Clear filter button
                core(filterBlock.getElementsByClassName(this.identifiers.clearSelectionBtnClass)).on("click", this.clearSelectionBlock.bind(this, filterBlock));
                core(filterBlock.querySelectorAll("input")).on("change", this.onChangeFilterChoice.bind(this, filterBlock));
            }
        },

        onChangeFilterChoice(event) {
            this.applyFilters();
        },

        clearSelectionBlock(filterBlock, event) {
            event.preventDefault();
            const clickedBtn = event.currentTarget;

            // Loop through input elements of the filter group
            const filterBlockFilters = filterBlock.querySelectorAll(".filterOptions input");
            for (let i = 0; i < filterBlockFilters.length; i++) {
                const filter = filterBlockFilters[i];
                if (filter.type === "checkbox") filter.checked = false;
            }

            this.applyFilters();
        },

        applyFilters() {
            const xhrRequest = new core.models.XhrRequestModel();
            xhrRequest.method = "GET";
            xhrRequest.identifier = this.templateId;
            xhrRequest.parameters = {};

            // Add chosen filters
            const chosenValues = {};
            const filterBlocks = document.getElementsByClassName(this.identifiers.filterBlockClass);
            for (let i = 0; i < filterBlocks.length; i++) {
                // Hint: Maybe only necessary to loop through only one block
                const filterBlock = filterBlocks[i];

                // Loop through the inputs
                const selections = filterBlock.querySelectorAll("input:checked");
                for (let ii = 0; ii < selections.length; ii++) {
                    const selectionValue = selections[ii].value;
                    chosenValues[filterBlock.dataset.filter] = (chosenValues[filterBlock.dataset.filter]) ? `${chosenValues[filterBlock.dataset.filter]},${selectionValue}` : selectionValue;
                }
            }

            console.log(chosenValues);
            // Add chosen values to params
            core.utils.extend(xhrRequest.parameters, this.parameters, chosenValues);

            xhrRequest.onDone = this.onDone;

            // Do request
            jjl(this.target).fillWithTemplate(xhrRequest);
        }

    }

	/**
	 * Cookie notification functionality
	 */
    core.cookieNotification = {
        enable() {
            //console.log("cookieNotification enabled!");

            // On load of page
            core.ready(() => {
                // Check if user is navigated or notification is turned off
                if (core.storage.cookie.get("notification") === "off" || core.storage.cookie.get("navigation") === "set") {
                    core("#cookieNotification").removeClass("show");
                } else {
                    core("#cookieNotification").addClass("show");
                }

                // Set navigation cookie
                core.storage.cookie.set("navigation", "set");

                // Set close action
                core("#closeCN").on("click", this.onCloseButtonClick.bind(this));
            });
        },
        onCloseButtonClick(event) {
            // Turn notification off
            core.storage.cookie.set("notification", "off", 30);
            core("#cookieNotification").removeClass("show");
            core("#cookieNotification").hide();
        }
    }

    /**
	 * Cookie notification functionality
	 */
    core.popup = {
        enable() {
            // Do not allow to init more than once
            if (popupEnabled) return;

            // set popup Enabled to true, so it can be only ran once
            popupEnabled = true;

            const clearPopup = () => core("#jpopup-wrapper").remove();
            const onClose = (delay = 0, event) => {
                console.log("close popup");
                event.preventDefault();
                const target = event.currentTarget;
                setTimeout(clearPopup, delay);
            };

            const onClickPopup = event => {
                event.preventDefault();
                const target = event.currentTarget;
                const settings = core.convert.autoFormat(core(target).getDataAttrAsObj("jpopup-", true));
                const parameters = (settings.datastring) ? core.convert.autoFormat(core.querystring.getAsObject(settings.datastring)) : {};
                const delay = settings.delay || 0;
                if (!settings.template) {
                    console.warn("jjl.popup: Unable to find attribute 'data-jpopup-template'");
                    return;
                }

                // Place template, but first make sure there is no old popup on DOM
                clearPopup();

                // Generate new component, and add to body
                const htmlPopupWrapper = document.createElement("div");
                htmlPopupWrapper.id = "jpopup-wrapper";

                const htmlPopupClose = document.createElement("div");
                htmlPopupClose.setAttribute("class", "jpopup-close close");

                const htmlPopup = document.createElement("div");
                htmlPopup.setAttribute("class", "popup");

                htmlPopupWrapper.appendChild(htmlPopupClose);
                htmlPopupWrapper.appendChild(htmlPopup);
                document.body.appendChild(htmlPopupWrapper);

                // Fill the wrapper with the correct template
                core("#jpopup-wrapper .popup").fillWithTemplate(settings.template, parameters, () => {
                    // Add active class if delay is set
                    if (delay) setTimeout(() => jjl("#jpopup-wrapper").addClass("active"), 0);

                    // Attach the onClose functionality
                    core(".jpopup-close").on("click", event => {
                        // Add active class if delay is set
                        if (delay) setTimeout(() => jjl("#jpopup-wrapper").addClass("active"), 0);

                        onClose(delay, event);
                    });
                });

            };

            // if new popup elements are added to page, re-bind them
            core.events.onElementWithClassAdded("jpopup", targets => { core(targets).on("click", onClickPopup); });

            // Attach on currently available items
            core(".jpopup").on("click", onClickPopup);
        }
    };


	/**
	 * The main object used in the library is the Collection type, it is instantiated here:
	 */
    class Collection {
        constructor(elements, selector) {
            for (let i = 0; i < elements.length; i++) {
                this[i] = elements[i];
            }
            this.length = elements.length;
            this.selector = selector;
        }

        /* * * * * * * * * * * * * * * * * * * * * * * * *
         *   PROTOTYPE FUNCTIONS (element specific):     *
         *       Functions runned on selector items      *
         *       "this" is the selected object           *
         *       Example:                                *
         *           Core("body").addClass("className"); *
         * * * * * * * * * * * * * * * * * * * * * * * * */

        /**
         * Counts number of items in object
         * @param {} enumType enumerated type
         * @returns {} numeric value of items counted
         */
        count(enumType) {
            if (enumType) {
                if (!(enumType in core.enums.types)) {
                    console.log("[jcl:count] Please use 'core.enum.types'");
                    return 0;
                }

                return core.utils.countType(this, enumType);
            }

            if (this instanceof Collection) {
                return core.utils.count(this, true);
            } else {
                return core.utils.count(this);
            }
        }

        /**
         * Loop through items
         */
        each(callback = () => { }) {
            if (this.length === 0) return;
            core.utils.each(this, callback);
        }

        /**
         *  Adding a class to an element or elements
         */
        addClass(className) {
            core.utils.addClass(this, className);
        }

        /**
         * Check if an element has a class
         */
        hasClass(className, strict) {
            return core.utils.hasClass(this, className, strict);
        }

        /**
         *  Adding a class to an element or elements
         */
        removeClass(className) {
            core.utils.removeClass(this, className);
        }

        /**
         *  Adding a class to an element or elements
         */
        toggleClass(className) {
            core.utils.toggleClass(this, className);
        }

        /**
         *  Selects the first element of the object. returns empty object if none.
         */
        first() {
            return core.utils.first(this);
        }

        /**
         *  Selects the first element of the object. returns empty object if none.
         */
        last() {
            return core.utils.last(this);
        }

        fill(content) {
            core.utils.fill(this, content);
        }

        /**
         * Fill the component(s) with result
         */
        fillWithTemplate(identifierOrxhrRequestModel, parameters, onDone) {
            core.utils.fillWithTemplate(this, identifierOrxhrRequestModel, parameters, onDone);
        }

        fillWithWebpage(identifierOrxhrRequestModel, onDone) {
            core.utils.fillWithWebpage(this, identifierOrxhrRequestModel, onDone);
        }

        contains(elementToCheck) {
            return core.utils.contains(this, elementToCheck);
        }

        in(elementsToCheck) {
            return core.utils.in(this, elementsToCheck);
        }

        content(forceSingle) {
            return core.utils.content(this, forceSingle);
        }

        append(contentToAppend) {
            return core.utils.append(this, contentToAppend);
        }

        clearListeners() {
            return core.events.clearListeners(this);
        }

        on(eventType, fn, useCapture) {
            return core.events.on(this, eventType, fn, useCapture);
        }

        attachDoubleClick(callback = core.utils.noop, timeOutInMs = 500, useMouseClick = false) {
            return core.events.attachDoubleClick(this, callback, timeOutInMs, useMouseClick);
        }

        off(eventType, fn, useCapture) {
            return core.events.off(this, eventType, fn, useCapture);
        }

        show() {
            return core.utils.show(this);
        }

        hide() {
            return core.utils.hide(this);
        }

        remove() {
            return core.utils.remove(this);
        }

        scroll(duration, baseElement) {
            return core.utils.scroll(this, duration, baseElement);
        }

        getDataAttrAsObj(prefix, removePrefix) {
            return core.utils.getDataAttrAsObj(this, prefix, removePrefix);
        }
    }

    // Handle finishing stuff after library internals are ready
    internal.attachOnclicks();
    internal.addGlobalEvents();

    // Attach to window with name "jcl"
    window[libraryName] = core;

    // Set eventlistener to ready
    document.addEventListener("DOMContentLoaded", () => {
        isReady = true;
        core.debug.log("Ready ;)");
    });

    console.log("Juice Javascript Library Loaded..");
})();
