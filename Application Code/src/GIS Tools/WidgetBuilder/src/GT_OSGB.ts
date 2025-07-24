class GT_OSGB {
    private _northings: number;
    private _eastings: number;
    private _status: GT_OSGBStatus;

    get Northings(): number {
        return this._northings;
    }

    get Eastings(): number {
        return this._eastings;
    }

    get Status():string {
        return this._status;
    }

    private _prefixes = new Array(
        new Array("SV", "SW", "SX", "SY", "SZ", "TV", "TW"),
        new Array("SQ", "SR", "SS", "ST", "SU", "TQ", "TR"),
        new Array("SL", "SM", "SN", "SO", "SP", "TL", "TM"),
        new Array("SF", "SG", "SH", "SJ", "SK", "TF", "TG"),
        new Array("SA", "SB", "SC", "SD", "SE", "TA", "TB"),
        new Array("NV", "NW", "NX", "NY", "NZ", "OV", "OW"),
        new Array("NQ", "NR", "NS", "NT", "NU", "OQ", "OR"),
        new Array("NL", "NM", "NN", "NO", "NP", "OL", "OM"),
        new Array("NF", "NG", "NH", "NJ", "NK", "OF", "OG"),
        new Array("NA", "NB", "NC", "ND", "NE", "OA", "OB"),
        new Array("HV", "HW", "HX", "HY", "HZ", "JV", "JW"),
        new Array("HQ", "HR", "HS", "HT", "HU", "JQ", "JR"),
        new Array("HL", "HM", "HN", "HO", "HP", "JL", "JM"));

    private _englandprefixes = new Array(
        "NX", "NY", "NZ",
        "SC", "SD", "SE", "TA",
        "SH", "SJ", "SK", "TF", "TG",
        "SO", "SP", "TL", "TM",
        "SS", "ST", "SU", "TR",
        "SW", "SX", "SY", "SZ", "TV"
    );

    constructor(northings?: number, eastings?: number) {
        this._northings = northings ?? 0;
        this._eastings = eastings ?? 0;
        this._status = "Undefined";
    }

    public setError = function (msg: string) {
        this._status = msg;
    }


    public setGridCoordinates(eastings, northings) {
        this._northings = northings;
        this._eastings = eastings;
        this._status = "OK";
    }

    private zeropad(num: string, len: number) {
        var str = new String(num);
        while (str.length < len) {
            str = '0' + str;
        }
        return str;
    }

    public getGridRef(precision: number) {
        if (precision < 0)
            precision = 0;
        if (precision > 5)
            precision = 5;

        var e: number;
        var n: number;

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

        return prefix + " " + this.zeropad((e as any), precision) + " " + this.zeropad((n as any), precision);
    }

    public getGridRefForEngland(precision: number) {
        if (precision < 0)
            precision = 0;
        if (precision > 5)
            precision = 5;

        var e: number;
        var n: number;

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

        if (this._englandprefixes.indexOf(prefix) === -1) {
            return "undefined";
        }

        return prefix + " " + this.zeropad((e as any), precision) + " " + this.zeropad((n as any), precision);
    }

    public parseGridRef(landranger: string) {
        var ok = false;
        this._northings = 0;
        this._eastings = 0;

        var precision;

        for (precision = 5; precision >= 1; precision--) {
            var pattern = new RegExp("^([A-Z]{2})\\s*(\\d{" + precision + "})\\s*(\\d{" + precision + "})$", "i")
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

                var x: number, y: number;
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

    public parseGridRefForEngland(landranger: string) {
        var ok = false;
        this._northings = 0;
        this._eastings = 0;

        var precision;

        for (precision = 5; precision >= 1; precision--) {
            var pattern = new RegExp("^([A-Z]{2})\\s*(\\d{" + precision + "})\\s*(\\d{" + precision + "})$", "i")
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

                var x: number, y: number;
                search: for (y = 0; y < this._prefixes.length; y++) {
                    for (x = 0; x < this._prefixes[y].length; x++)
                        if (this._prefixes[y][x] == gridSheet && this._englandprefixes.indexOf(gridSheet) !== -1) {
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
}
export = GT_OSGB;

type GT_OSGBStatus = "Undefined" | "OK" | "Error" | "Coordinate not within Great Britain";