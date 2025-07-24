define(["require", "exports"], function (require, exports) {
    "use strict";
    var GT_OSGB = /** @class */ (function () {
        function GT_OSGB(northings, eastings) {
            this._prefixes = new Array(new Array("SV", "SW", "SX", "SY", "SZ", "TV", "TW"), new Array("SQ", "SR", "SS", "ST", "SU", "TQ", "TR"), new Array("SL", "SM", "SN", "SO", "SP", "TL", "TM"), new Array("SF", "SG", "SH", "SJ", "SK", "TF", "TG"), new Array("SA", "SB", "SC", "SD", "SE", "TA", "TB"), new Array("NV", "NW", "NX", "NY", "NZ", "OV", "OW"), new Array("NQ", "NR", "NS", "NT", "NU", "OQ", "OR"), new Array("NL", "NM", "NN", "NO", "NP", "OL", "OM"), new Array("NF", "NG", "NH", "NJ", "NK", "OF", "OG"), new Array("NA", "NB", "NC", "ND", "NE", "OA", "OB"), new Array("HV", "HW", "HX", "HY", "HZ", "JV", "JW"), new Array("HQ", "HR", "HS", "HT", "HU", "JQ", "JR"), new Array("HL", "HM", "HN", "HO", "HP", "JL", "JM"));
            this.setError = function (msg) {
                this._status = msg;
            };
            this._northings = northings !== null && northings !== void 0 ? northings : 0;
            this._eastings = eastings !== null && eastings !== void 0 ? eastings : 0;
            this._status = "Undefined";
        }
        Object.defineProperty(GT_OSGB.prototype, "Northings", {
            get: function () {
                return this._northings;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(GT_OSGB.prototype, "Eastings", {
            get: function () {
                return this._eastings;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(GT_OSGB.prototype, "Status", {
            get: function () {
                return this._status;
            },
            enumerable: false,
            configurable: true
        });
        GT_OSGB.prototype.setGridCoordinates = function (eastings, northings) {
            this._northings = northings;
            this._eastings = eastings;
            this._status = "OK";
        };
        GT_OSGB.prototype.zeropad = function (num, len) {
            var str = new String(num);
            while (str.length < len) {
                str = '0' + str;
            }
            return str;
        };
        GT_OSGB.prototype.getGridRef = function (precision) {
            if (precision < 0)
                precision = 0;
            if (precision > 5)
                precision = 5;
            var e;
            var n;
            if (precision > 0) {
                var y = Math.floor(this._northings / 100000);
                var x = Math.floor(this._eastings / 100000);
                var e = Math.round(this._eastings % 100000);
                var n = Math.round(this._northings % 100000);
                var div = (5 - precision);
                e = Math.round(e / Math.pow(10, div));
                n = Math.round(n / Math.pow(10, div));
            }
            if (y <= 0 || y >= this._prefixes.length) {
                return "undefined";
            }
            if (x <= 0 || x >= this._prefixes[y].length) {
                return "undefined";
            }
            var prefix = this._prefixes[y][x];
            return prefix + " " + this.zeropad(e, precision) + " " + this.zeropad(n, precision);
        };
        GT_OSGB.prototype.getGridRefForEngland = function (precision) {
            if (precision < 0)
                precision = 0;
            if (precision > 5)
                precision = 5;
            var e;
            var n;
            if (precision > 0) {
                var y = Math.floor(this._northings / 100000);
                var x = Math.floor(this._eastings / 100000);
                var e = Math.round(this._eastings % 100000);
                var n = Math.round(this._northings % 100000);
                var div = (5 - precision);
                e = Math.round(e / Math.pow(10, div));
                n = Math.round(n / Math.pow(10, div));
            }
            if (y <= 0 || y >= this._prefixes.length) {
                return "undefined";
            }
            if (x <= 0 || x >= this._prefixes[y].length) {
                return "undefined";
            }
            var prefix = this._prefixes[y][x];
            return prefix + " " + this.zeropad(e, precision) + " " + this.zeropad(n, precision);
        };
        GT_OSGB.prototype.parseGridRef = function (landranger) {
            var ok = false;
            this._northings = 0;
            this._eastings = 0;
            var precision;
            for (precision = 5; precision >= 1; precision--) {
                var pattern = new RegExp("^([A-Z]{2})\\s*(\\d{" + precision + "})\\s*(\\d{" + precision + "})$", "i");
                var gridRef = landranger.match(pattern);
                if (gridRef) {
                    var gridSheet = gridRef[1];
                    var gridEast = 0;
                    var gridNorth = 0;
                    //5x1 4x10 3x100 2x1000 1x10000
                    if (precision > 0) {
                        var mult = Math.pow(10, 5 - precision);
                        gridEast = parseInt(gridRef[2], 10) * mult;
                        gridNorth = parseInt(gridRef[3], 10) * mult;
                    }
                    var x, y;
                    search: for (y = 0; y < this._prefixes.length; y++) {
                        for (x = 0; x < this._prefixes[y].length; x++)
                            if (this._prefixes[y][x] == gridSheet) {
                                this._eastings = (x * 100000) + gridEast;
                                this._northings = (y * 100000) + gridNorth;
                                ok = true;
                                break search;
                            }
                    }
                }
            }
            return ok;
        };
        ;
        GT_OSGB.prototype.parseGridRefForEngland = function (landranger) {
            var ok = false;
            this._northings = 0;
            this._eastings = 0;
            var precision;
            for (precision = 5; precision >= 1; precision--) {
                var pattern = new RegExp("^([A-Z]{2})\\s*(\\d{" + precision + "})\\s*(\\d{" + precision + "})$", "i");
                var gridRef = landranger.match(pattern);
                if (gridRef) {
                    var gridSheet = gridRef[1];
                    var gridEast = 0;
                    var gridNorth = 0;
                    //5x1 4x10 3x100 2x1000 1x10000
                    if (precision > 0) {
                        var mult = Math.pow(10, 5 - precision);
                        gridEast = parseInt(gridRef[2], 10) * mult;
                        gridNorth = parseInt(gridRef[3], 10) * mult;
                    }
                    var x, y;
                    search: for (y = 0; y < this._prefixes.length; y++) {
                        for (x = 0; x < this._prefixes[y].length; x++)
                            if (this._prefixes[y][x] == gridSheet) {
                                this._eastings = (x * 100000) + gridEast;
                                this._northings = (y * 100000) + gridNorth;
                                ok = true;
                                break search;
                            }
                    }
                }
            }
            return ok;
        };
        ;
        return GT_OSGB;
    }());
    return GT_OSGB;
});
