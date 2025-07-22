define(["require", "exports"], function (require, exports) {
    "use strict";
    var GT_WGS84 = /** @class */ (function () {
        function GT_WGS84(latitude, longitude) {
            this._latitude = latitude !== null && latitude !== void 0 ? latitude : 0;
            this._longitude = longitude !== null && longitude !== void 0 ? longitude : 0;
        }
        Object.defineProperty(GT_WGS84.prototype, "Latitude", {
            get: function () {
                return this._latitude;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(GT_WGS84.prototype, "Longitude", {
            get: function () {
                return this._longitude;
            },
            enumerable: false,
            configurable: true
        });
        GT_WGS84.prototype.setDegrees = function (latitude, longitude) {
            this._latitude = latitude;
            this._longitude = longitude;
        };
        GT_WGS84.prototype.isGreatBritain = function () {
            return this._latitude > 49 &&
                this._latitude < 62 &&
                this._longitude > -9.5 &&
                this._longitude < 2.3;
        };
        GT_WGS84.prototype.isIreland = function () {
            return this._latitude > 51.2 &&
                this._latitude < 55.73 &&
                this._longitude > -12.2 &&
                this._longitude < -4.8;
        };
        GT_WGS84.prototype.parseString = function (text) {
            var ok = false;
            var str = new String(text);
            //N 51° 53.947 W 000° 10.018
            var pattern = /([ns])\s*(\d+)[°\s]+(\d+\.\d+)\s+([we])\s*(\d+)[°\s]+(\d+\.\d+)/i;
            var matches = str.match(pattern);
            if (matches) {
                ok = true;
                var latsign = (matches[1] == 's' || matches[1] == 'S') ? -1 : 1;
                var longsign = (matches[4] == 'w' || matches[4] == 'W') ? -1 : 1;
                var d1 = parseFloat(matches[2]);
                var m1 = parseFloat(matches[3]);
                var d2 = parseFloat(matches[5]);
                var m2 = parseFloat(matches[6]);
                this._latitude = latsign * (d1 + (m1 / 60.0));
                this._longitude = longsign * (d2 + (m2 / 60.0));
            }
            return ok;
        };
        return GT_WGS84;
    }());
    return GT_WGS84;
});
