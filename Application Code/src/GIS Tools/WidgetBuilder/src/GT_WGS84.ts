class GT_WGS84 {
    private _latitude: number;
    private _longitude: number;

    constructor(latitude?: number, longitude?: number) {
        this._latitude = latitude ?? 0;
        this._longitude = longitude ?? 0;
    }

    get Latitude(): number {
        return this._latitude;
    }

    get Longitude(): number {
        return this._longitude;
    }


    public setDegrees(latitude: number, longitude: number) {
        this._latitude = latitude;
        this._longitude = longitude;
    }

    public isGreatBritain() {
        return this._latitude > 49 &&
            this._latitude < 62 &&
            this._longitude > -9.5 &&
            this._longitude < 2.3;
    }
  
    public isIreland () {
        return this._latitude > 51.2 &&
            this._latitude < 55.73 &&
            this._longitude > -12.2 &&
            this._longitude < -4.8;
    }


    public parseString(text: string) {
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
    }
}
export = GT_WGS84;