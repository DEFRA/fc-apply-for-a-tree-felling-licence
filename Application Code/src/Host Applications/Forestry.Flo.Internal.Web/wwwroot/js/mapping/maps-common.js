define(["require", "exports", "esri/Graphic"], function (require, exports, Graphic) {
    "use strict";
    var EsriHelper = /** @class */ (function () {
        function EsriHelper(view) {
            this._view = view;
        }

        EsriHelper.prototype.disableZooming = function () {
            this._view.popup.actions = [];
            function stopEvtPropagation(evt) {
                evt.stopPropagation();
            }
            // exlude the zoom widget from the default UI
            this._view.ui.components = ["attribution"];
            // disable mouse wheel scroll zooming on the view
            this._view.on("mouse-wheel", stopEvtPropagation);
            // disable zooming via double-click on the view
            this._view.on("double-click", stopEvtPropagation);
            // disable zooming out via double-click + Control on the view
            this._view.on("double-click", ["Control"], stopEvtPropagation);
            // disables pinch-zoom and panning on the view
            this._view.on("drag", stopEvtPropagation);
            // disable the view's zoom box to prevent the Shift + drag
            // and Shift + Control + drag zoom gestures.
            this._view.on("drag", ["Shift"], stopEvtPropagation);
            this._view.on("drag", ["Shift", "Control"], stopEvtPropagation);
            // prevents zooming with the + and - keys
            this._view.on("key-down", function (evt) {
                var prohibitedKeys = [
                    "+",
                    "-",
                    "Shift",
                    "_",
                    "=",
                    "ArrowUp",
                    "ArrowDown",
                    "ArrowRight",
                    "ArrowLeft",
                ];
                var keyPressed = evt.key;
                if (prohibitedKeys.indexOf(keyPressed) !== -1) {
                    evt.stopPropagation();
                }
            });
        };

        return EsriHelper;
    }());
    return EsriHelper;
});
