if (!window.customPolyfillAdded) {
    // CustomEvent
    (function() {
        window.customPolyfillAdded = true;
        if (typeof window.CustomEvent === "function") return false;

        function CustomEvent(event, params) {
            params = params || { bubbles: false, cancelable: false, detail: undefined };
            var evt = document.createEvent('CustomEvent');
            evt.initCustomEvent(event, params.bubbles, params.cancelable, params.detail);
            return evt;
        }

        CustomEvent.prototype = window.Event.prototype;

        window.CustomEvent = CustomEvent;
    })();

    // StartsWith
    if (!String.prototype.startsWith) {
        String.prototype.startsWith = function(searchString, position) {
            position = position || 0;
            return this.indexOf(searchString, position) === position;
        };
    }

    // String.includes
    if (!String.prototype.includes) {
        String.prototype.includes = function(search, start) {
            'use strict';
            if (typeof start !== 'number') {
                start = 0;
            }

            if (start + search.length > this.length) {
                return false;
            } else {
                return this.indexOf(search, start) !== -1;
            }
        };
    }

    // Array.includes
    // https://tc39.github.io/ecma262/#sec-array.prototype.includes
    if (!Array.prototype.includes) {
        Object.defineProperty(Array.prototype, 'includes', {
            value: function(searchElement, fromIndex) {

                // 1. Let O be ? ToObject(this value).
                if (this == null) {
                    throw new TypeError('"this" is null or not defined');
                }

                var o = Object(this);

                // 2. Let len be ? ToLength(? Get(O, "length")).
                var len = o.length >>> 0;

                // 3. If len is 0, return false.
                if (len === 0) {
                    return false;
                }

                // 4. Let n be ? ToInteger(fromIndex).
                //    (If fromIndex is undefined, this step produces the value 0.)
                var n = fromIndex | 0;

                // 5. If n ≥ 0, then
                //  a. Let k be n.
                // 6. Else n < 0,
                //  a. Let k be len + n.
                //  b. If k < 0, let k be 0.
                var k = Math.max(n >= 0 ? n : len - Math.abs(n), 0);

                // 7. Repeat, while k < len
                while (k < len) {
                    // a. Let elementK be the result of ? Get(O, ! ToString(k)).
                    // b. If SameValueZero(searchElement, elementK) is true, return true.
                    // c. Increase k by 1.
                    // NOTE: === provides the correct "SameValueZero" comparison needed here.
                    if (o[k] === searchElement) {
                        return true;
                    }
                    k++;
                }

                // 8. Return false
                return false;
            }
        });
    }

    // Prepend - Source: https://github.com/jserz/js_piece/blob/master/DOM/ParentNode/prepend()/prepend().md
    (function(arr) {
        arr.forEach(function(item) {
            if (item.hasOwnProperty('prepend')) {
                return;
            }
            Object.defineProperty(item, 'prepend', {
                configurable: true,
                enumerable: true,
                writable: true,
                value: function prepend() {
                    var argArr = Array.prototype.slice.call(arguments),
                        docFrag = document.createDocumentFragment();

                    argArr.forEach(function(argItem) {
                        var isNode = argItem instanceof Node;
                        docFrag.appendChild(isNode ? argItem : document.createTextNode(String(argItem)));
                    });

                    this.insertBefore(docFrag, this.firstChild);
                }
            });
        });
    })([Element.prototype, Document.prototype, DocumentFragment.prototype]);

    // ReplaceWith - Source: https://developer.mozilla.org/en-US/docs/Web/API/ChildNode/replaceWith
    function ReplaceWith(Ele) {
        'use-strict'; // For safari, and IE > 10
        var parent = this.parentNode,
            i = arguments.length,
            firstIsNode = +(parent && typeof Ele === 'object');
        if (!parent) return;

        while (i-- > firstIsNode) {
            if (parent && typeof arguments[i] !== 'object') {
                arguments[i] = document.createTextNode(arguments[i]);
            }
            if (!parent && arguments[i].parentNode) {
                arguments[i].parentNode.removeChild(arguments[i]);
                continue;
            }
            parent.insertBefore(this.previousSibling, arguments[i]);
        }
        if (firstIsNode) parent.replaceChild(Ele, this);
    }

    if (!Element.prototype.replaceWith)
        Element.prototype.replaceWith = ReplaceWith;
    if (!CharacterData.prototype.replaceWith)
        CharacterData.prototype.replaceWith = ReplaceWith;
    if (!DocumentType.prototype.replaceWith)
        DocumentType.prototype.replaceWith = ReplaceWith;

    // getElementsByClassName - Source: https://stackoverflow.com/questions/7410949/javascript-document-getelementsbyclassname-compatibility-with-ie
    if (!document.getElementsByClassName) {
        document.getElementsByClassName = function(className) {
            return this.querySelectorAll("." + className);
        };
        Element.prototype.getElementsByClassName = document.getElementsByClassName;
    }
}