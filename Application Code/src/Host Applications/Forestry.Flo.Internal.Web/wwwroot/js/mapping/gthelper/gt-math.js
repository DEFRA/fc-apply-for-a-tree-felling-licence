define(["require", "exports", "tslib", "/js/mapping/gthelper/gt-osgb.js?v=" + Date.now(), "/js/mapping/gthelper/gt-wgs84.js?v=" + Date.now()], function (require, exports, tslib_1, GT_OSGB_1, GT_WGS84_1) {
    "use strict";
    GT_OSGB_1 = tslib_1.__importDefault(GT_OSGB_1);
    GT_WGS84_1 = tslib_1.__importDefault(GT_WGS84_1);
    var GT_Math = /** @class */ (function () {
        function GT_Math() {
        }
        GT_Math.E_N_to_Lat = function (East, North, a, b, e0, n0, f0, PHI0, LAM0) {
            //Un-project Transverse Mercator eastings and northings back to latitude.
            //Input: - _
            //eastings (East) and northings (North) in meters; _
            //ellipsoid axis dimensions (a & b) in meters; _
            //eastings (e0) and northings (n0) of false origin in meters; _
            //central meridian scale factor (f0) and _
            //latitude (PHI0) and longitude (LAM0) of false origin in decimal degrees.
            //'REQUIRES THE "Marc" AND "InitialLat" FUNCTIONS
            //Convert angle measures to radians
            var Pi = 3.14159265358979;
            var RadPHI0 = PHI0 * (Pi / 180);
            var RadLAM0 = LAM0 * (Pi / 180);
            //Compute af0, bf0, e squared (e2), n and Et
            var af0 = a * f0;
            var bf0 = b * f0;
            var e2 = (Math.pow(af0, 2) - Math.pow(bf0, 2)) / Math.pow(af0, 2);
            var n = (af0 - bf0) / (af0 + bf0);
            var Et = East - e0;
            //Compute initial value for latitude (PHI) in radians
            var PHId = this.InitialLat(North, n0, af0, RadPHI0, n, bf0);
            //Compute nu, rho and eta2 using value for PHId
            var nu = af0 / (Math.sqrt(1 - (e2 * (Math.pow(Math.sin(PHId), 2)))));
            var rho = (nu * (1 - e2)) / (1 - (e2 * Math.pow(Math.sin(PHId), 2)));
            var eta2 = (nu / rho) - 1;
            //Compute Latitude
            var VII = (Math.tan(PHId)) / (2 * rho * nu);
            var VIII = ((Math.tan(PHId)) / (24 * rho * Math.pow(nu, 3))) * (5 + (3 * (Math.pow(Math.tan(PHId), 2))) + eta2 - (9 * eta2 * (Math.pow(Math.tan(PHId), 2))));
            var IX = ((Math.tan(PHId)) / (720 * rho * Math.pow(nu, 5))) * (61 + (90 * ((Math.tan(PHId)) ^ 2)) + (45 * (Math.pow(Math.tan(PHId), 4))));
            var E_N_to_Lat = (180 / Pi) * (PHId - (Math.pow(Et, 2) * VII) + (Math.pow(Et, 4) * VIII) - ((Et ^ 6) * IX));
            return (E_N_to_Lat);
        };
        GT_Math.InitialLat = function (North, n0, afo, PHI0, n, bfo) {
            //Compute initial value for Latitude (PHI) IN RADIANS.
            //Input: - _
            //northing of point (North) and northing of false origin (n0) in meters; _
            //semi major axis multiplied by central meridian scale factor (af0) in meters; _
            //latitude of false origin (PHI0) IN RADIANS; _
            //n (computed from a, b and f0) and _
            //ellipsoid semi major axis multiplied by central meridian scale factor (bf0) in meters.
            //REQUIRES THE "Marc" FUNCTION
            //THIS FUNCTION IS CALLED BY THE "E_N_to_Lat", "E_N_to_Long" and "E_N_to_C" FUNCTIONS
            //THIS FUNCTION IS ALSO USED ON IT'S OWN IN THE  "Projection and Transformation Calculations.xls" SPREADSHEET
            //First PHI value (PHI1)
            var PHI1 = ((North - n0) / afo) + PHI0;
            //Calculate M
            var M = this.Marc(bfo, n, PHI0, PHI1);
            //Calculate new PHI value (PHI2)
            var PHI2 = ((North - n0 - M) / afo) + PHI1;
            //Iterate to get final value for InitialLat
            while (Math.abs(North - n0 - M) > 0.00001) {
                PHI2 = ((North - n0 - M) / afo) + PHI1;
                M = this.Marc(bfo, n, PHI0, PHI2);
                PHI1 = PHI2;
            }
            return PHI2;
        };
        GT_Math.Marc = function (bf0, n, PHI0, PHI) {
            //Compute meridional arc.
            //Input: - _
            // ellipsoid semi major axis multiplied by central meridian scale factor (bf0) in meters; _
            // n (computed from a, b and f0); _
            // lat of false origin (PHI0) and initial or final latitude of point (PHI) IN RADIANS.
            //THIS FUNCTION IS CALLED BY THE - _
            // "Lat_Long_to_North" and "InitialLat" FUNCTIONS
            // THIS FUNCTION IS ALSO USED ON IT'S OWN IN THE "Projection and Transformation Calculations.xls" SPREADSHEET
            return bf0 * (((1 + n + ((5 / 4) * Math.pow(n, 2)) + ((5 / 4) * Math.pow(n, 3))) * (PHI - PHI0)) - (((3 * n) + (3 * Math.pow(n, 2)) + ((21 / 8) * Math.pow(n, 3))) * (Math.sin(PHI - PHI0)) * (Math.cos(PHI + PHI0))) + ((((15 / 8) * Math.pow(n, 2)) + ((15 / 8) * Math.pow(n, 3))) * (Math.sin(2 * (PHI - PHI0))) * (Math.cos(2 * (PHI + PHI0)))) - (((35 / 24) * Math.pow(n, 3)) * (Math.sin(3 * (PHI - PHI0))) * (Math.cos(3 * (PHI + PHI0)))));
        };
        GT_Math.E_N_to_Long = function (East, North, a, b, e0, n0, f0, PHI0, LAM0) {
            //Un-project Transverse Mercator eastings and northings back to longitude.
            //Input: - _
            //eastings (East) and northings (North) in meters; _
            //ellipsoid axis dimensions (a & b) in meters; _
            //eastings (e0) and northings (n0) of false origin in meters; _
            //central meridian scale factor (f0) and _
            //latitude (PHI0) and longitude (LAM0) of false origin in decimal degrees.
            //REQUIRES THE "Marc" AND "InitialLat" FUNCTIONS
            //Convert angle measures to radians
            var Pi = 3.14159265358979;
            var RadPHI0 = PHI0 * (Pi / 180);
            var RadLAM0 = LAM0 * (Pi / 180);
            //Compute af0, bf0, e squared (e2), n and Et
            var af0 = a * f0;
            var bf0 = b * f0;
            var e2 = (Math.pow(af0, 2) - Math.pow(bf0, 2)) / Math.pow(af0, 2);
            var n = (af0 - bf0) / (af0 + bf0);
            var Et = East - e0;
            //Compute initial value for latitude (PHI) in radians
            var PHId = this.InitialLat(North, n0, af0, RadPHI0, n, bf0);
            //Compute nu, rho and eta2 using value for PHId
            var nu = af0 / (Math.sqrt(1 - (e2 * (Math.pow(Math.sin(PHId), 2)))));
            var rho = (nu * (1 - e2)) / (1 - (e2 * Math.pow(Math.sin(PHId), 2)));
            var eta2 = (nu / rho) - 1;
            //Compute Longitude
            var X = (Math.pow(Math.cos(PHId), -1)) / nu;
            var XI = ((Math.pow(Math.cos(PHId), -1)) / (6 * Math.pow(nu, 3))) * ((nu / rho) + (2 * (Math.pow(Math.tan(PHId), 2))));
            var XII = ((Math.pow(Math.cos(PHId), -1)) / (120 * Math.pow(nu, 5))) * (5 + (28 * (Math.pow(Math.tan(PHId), 2))) + (24 * (Math.pow(Math.tan(PHId), 4))));
            var XIIA = ((Math.pow(Math.cos(PHId), -1)) / (5040 * Math.pow(nu, 7))) * (61 + (662 * (Math.pow(Math.tan(PHId), 2))) + (1320 * (Math.pow(Math.tan(PHId), 4))) + (720 * (Math.pow(Math.tan(PHId), 6))));
            var E_N_to_Long = (180 / Pi) * (RadLAM0 + (Et * X) - (Math.pow(Et, 3) * XI) + (Math.pow(Et, 5) * XII) - (Math.pow(Et, 7) * XIIA));
            return E_N_to_Long;
        };
        GT_Math.Lat_Long_H_to_X = function (PHI, LAM, H, a, b) {
            // Convert geodetic coords lat (PHI), long (LAM) and height (H) to cartesian X coordinate.
            // Input: - _
            //    Latitude (PHI)& Longitude (LAM) both in decimal degrees; _
            //  Ellipsoidal height (H) and ellipsoid axis dimensions (a & b) all in meters.
            // Convert angle measures to radians
            var Pi = 3.14159265358979;
            var RadPHI = PHI * (Pi / 180);
            var RadLAM = LAM * (Pi / 180);
            // Compute eccentricity squared and nu
            var e2 = (Math.pow(a, 2) - Math.pow(b, 2)) / Math.pow(a, 2);
            var V = a / (Math.sqrt(1 - (e2 * (Math.pow(Math.sin(RadPHI), 2)))));
            // Compute X
            return (V + H) * (Math.cos(RadPHI)) * (Math.cos(RadLAM));
        };
        GT_Math.Lat_Long_H_to_Y = function (PHI, LAM, H, a, b) {
            // Convert geodetic coords lat (PHI), long (LAM) and height (H) to cartesian Y coordinate.
            // Input: - _
            // Latitude (PHI)& Longitude (LAM) both in decimal degrees; _
            // Ellipsoidal height (H) and ellipsoid axis dimensions (a & b) all in meters.
            // Convert angle measures to radians
            var Pi = 3.14159265358979;
            var RadPHI = PHI * (Pi / 180);
            var RadLAM = LAM * (Pi / 180);
            // Compute eccentricity squared and nu
            var e2 = (Math.pow(a, 2) - Math.pow(b, 2)) / Math.pow(a, 2);
            var V = a / (Math.sqrt(1 - (e2 * (Math.pow(Math.sin(RadPHI), 2)))));
            // Compute Y
            return (V + H) * (Math.cos(RadPHI)) * (Math.sin(RadLAM));
        };
        GT_Math.Lat_H_to_Z = function (PHI, H, a, b) {
            // Convert geodetic coord components latitude (PHI) and height (H) to cartesian Z coordinate.
            // Input: - _
            //    Latitude (PHI) decimal degrees; _
            // Ellipsoidal height (H) and ellipsoid axis dimensions (a & b) all in meters.
            // Convert angle measures to radians
            var Pi = 3.14159265358979;
            var RadPHI = PHI * (Pi / 180);
            // Compute eccentricity squared and nu
            var e2 = (Math.pow(a, 2) - Math.pow(b, 2)) / Math.pow(a, 2);
            var V = a / (Math.sqrt(1 - (e2 * (Math.pow(Math.sin(RadPHI), 2)))));
            // Compute X
            return ((V * (1 - e2)) + H) * (Math.sin(RadPHI));
        };
        GT_Math.Helmert_X = function (X, Y, Z, DX, Y_Rot, Z_Rot, s) {
            // (X, Y, Z, DX, Y_Rot, Z_Rot, s)
            // Computed Helmert transformed X coordinate.
            // Input: - _
            //    cartesian XYZ coords (X,Y,Z), X translation (DX) all in meters ; _
            // Y and Z rotations in seconds of arc (Y_Rot, Z_Rot) and scale in ppm (s).
            // Convert rotations to radians and ppm scale to a factor
            var Pi = 3.14159265358979;
            var sfactor = s * 0.000001;
            var RadY_Rot = (Y_Rot / 3600) * (Pi / 180);
            var RadZ_Rot = (Z_Rot / 3600) * (Pi / 180);
            //Compute transformed X coord
            return (X + (X * sfactor) - (Y * RadZ_Rot) + (Z * RadY_Rot) + DX);
        };
        GT_Math.Helmert_Y = function (X, Y, Z, DY, X_Rot, Z_Rot, s) {
            // (X, Y, Z, DY, X_Rot, Z_Rot, s)
            // Computed Helmert transformed Y coordinate.
            // Input: - _
            //    cartesian XYZ coords (X,Y,Z), Y translation (DY) all in meters ; _
            //  X and Z rotations in seconds of arc (X_Rot, Z_Rot) and scale in ppm (s).
            // Convert rotations to radians and ppm scale to a factor
            var Pi = 3.14159265358979;
            var sfactor = s * 0.000001;
            var RadX_Rot = (X_Rot / 3600) * (Pi / 180);
            var RadZ_Rot = (Z_Rot / 3600) * (Pi / 180);
            // Compute transformed Y coord
            return (X * RadZ_Rot) + Y + (Y * sfactor) - (Z * RadX_Rot) + DY;
        };
        GT_Math.Helmert_Z = function (X, Y, Z, DZ, X_Rot, Y_Rot, s) {
            // (X, Y, Z, DZ, X_Rot, Y_Rot, s)
            // Computed Helmert transformed Z coordinate.
            // Input: - _
            //    cartesian XYZ coords (X,Y,Z), Z translation (DZ) all in meters ; _
            // X and Y rotations in seconds of arc (X_Rot, Y_Rot) and scale in ppm (s).
            //
            // Convert rotations to radians and ppm scale to a factor
            var Pi = 3.14159265358979;
            var sfactor = s * 0.000001;
            var RadX_Rot = (X_Rot / 3600) * (Pi / 180);
            var RadY_Rot = (Y_Rot / 3600) * (Pi / 180);
            // Compute transformed Z coord
            return (-1 * X * RadY_Rot) + (Y * RadX_Rot) + Z + (Z * sfactor) + DZ;
        };
        GT_Math.XYZ_to_Lat = function (X, Y, Z, a, b) {
            // Convert XYZ to Latitude (PHI) in Dec Degrees.
            // Input: - _
            // XYZ cartesian coords (X,Y,Z) and ellipsoid axis dimensions (a & b), all in meters.
            // THIS FUNCTION REQUIRES THE "Iterate_XYZ_to_Lat" FUNCTION
            // THIS FUNCTION IS CALLED BY THE "XYZ_to_H" FUNCTION
            var RootXYSqr = Math.sqrt(Math.pow(X, 2) + Math.pow(Y, 2));
            var e2 = (Math.pow(a, 2) - Math.pow(b, 2)) / Math.pow(a, 2);
            var PHI1 = Math.atan2(Z, (RootXYSqr * (1 - e2)));
            var PHI = this.Iterate_XYZ_to_Lat(a, e2, PHI1, Z, RootXYSqr);
            var Pi = 3.14159265358979;
            return PHI * (180 / Pi);
        };
        GT_Math.Iterate_XYZ_to_Lat = function (a, e2, PHI1, Z, RootXYSqr) {
            // Iteratively computes Latitude (PHI).
            // Input: - _
            //    ellipsoid semi major axis (a) in meters; _
            //    eta squared (e2); _
            //    estimated value for latitude (PHI1) in radians; _
            //    cartesian Z coordinate (Z) in meters; _
            // RootXYSqr computed from X & Y in meters.
            // THIS FUNCTION IS CALLED BY THE "XYZ_to_PHI" FUNCTION
            // THIS FUNCTION IS ALSO USED ON IT'S OWN IN THE _
            // "Projection and Transformation Calculations.xls" SPREADSHEET
            var V = a / (Math.sqrt(1 - (e2 * Math.pow(Math.sin(PHI1), 2))));
            var PHI2 = Math.atan2((Z + (e2 * V * (Math.sin(PHI1)))), RootXYSqr);
            while (Math.abs(PHI1 - PHI2) > 0.000000001) {
                PHI1 = PHI2;
                V = a / (Math.sqrt(1 - (e2 * Math.pow(Math.sin(PHI1), 2))));
                PHI2 = Math.atan2((Z + (e2 * V * (Math.sin(PHI1)))), RootXYSqr);
            }
            return PHI2;
        };
        GT_Math.XYZ_to_Long = function (X, Y) {
            // Convert XYZ to Longitude (LAM) in Dec Degrees.
            // Input: - _
            // X and Y cartesian coords in meters.
            var Pi = 3.14159265358979;
            return Math.atan2(Y, X) * (180 / Pi);
        };
        GT_Math.Lat_Long_to_East = function (PHI, LAM, a, b, e0, f0, PHI0, LAM0) {
            //Project Latitude and longitude to Transverse Mercator eastings.
            //Input: - _
            //    Latitude (PHI) and Longitude (LAM) in decimal degrees; _
            //    ellipsoid axis dimensions (a & b) in meters; _
            //    eastings of false origin (e0) in meters; _
            //    central meridian scale factor (f0); _
            // latitude (PHI0) and longitude (LAM0) of false origin in decimal degrees.
            // Convert angle measures to radians
            var Pi = 3.14159265358979;
            var RadPHI = PHI * (Pi / 180);
            var RadLAM = LAM * (Pi / 180);
            var RadPHI0 = PHI0 * (Pi / 180);
            var RadLAM0 = LAM0 * (Pi / 180);
            var af0 = a * f0;
            var bf0 = b * f0;
            var e2 = (Math.pow(af0, 2) - Math.pow(bf0, 2)) / Math.pow(af0, 2);
            var n = (af0 - bf0) / (af0 + bf0);
            var nu = af0 / (Math.sqrt(1 - (e2 * Math.pow(Math.sin(RadPHI), 2))));
            var rho = (nu * (1 - e2)) / (1 - (e2 * Math.pow(Math.sin(RadPHI), 2)));
            var eta2 = (nu / rho) - 1;
            var p = RadLAM - RadLAM0;
            var IV = nu * (Math.cos(RadPHI));
            var V = (nu / 6) * (Math.pow(Math.cos(RadPHI), 3)) * ((nu / rho) - (Math.pow(Math.tan(RadPHI), 2)));
            var VI = (nu / 120) * (Math.pow(Math.cos(RadPHI), 5)) * (5 - (18 * (Math.pow(Math.tan(RadPHI), 2))) + (Math.pow(Math.tan(RadPHI), 4)) + (14 * eta2) - (58 * (Math.pow(Math.tan(RadPHI), 2)) * eta2));
            return e0 + (p * IV) + (Math.pow(p, 3) * V) + (Math.pow(p, 5) * VI);
        };
        GT_Math.Lat_Long_to_North = function (PHI, LAM, a, b, e0, n0, f0, PHI0, LAM0) {
            // Project Latitude and longitude to Transverse Mercator northings
            // Input: - _
            // Latitude (PHI) and Longitude (LAM) in decimal degrees; _
            // ellipsoid axis dimensions (a & b) in meters; _
            // eastings (e0) and northings (n0) of false origin in meters; _
            // central meridian scale factor (f0); _
            // latitude (PHI0) and longitude (LAM0) of false origin in decimal degrees.
            // REQUIRES THE "Marc" FUNCTION
            // Convert angle measures to radians
            var Pi = 3.14159265358979;
            var RadPHI = PHI * (Pi / 180);
            var RadLAM = LAM * (Pi / 180);
            var RadPHI0 = PHI0 * (Pi / 180);
            var RadLAM0 = LAM0 * (Pi / 180);
            var af0 = a * f0;
            var bf0 = b * f0;
            var e2 = (Math.pow(af0, 2) - Math.pow(bf0, 2)) / Math.pow(af0, 2);
            var n = (af0 - bf0) / (af0 + bf0);
            var nu = af0 / (Math.sqrt(1 - (e2 * Math.pow(Math.sin(RadPHI), 2))));
            var rho = (nu * (1 - e2)) / (1 - (e2 * Math.pow(Math.sin(RadPHI), 2)));
            var eta2 = (nu / rho) - 1;
            var p = RadLAM - RadLAM0;
            var M = this.Marc(bf0, n, RadPHI0, RadPHI);
            var I = M + n0;
            var II = (nu / 2) * (Math.sin(RadPHI)) * (Math.cos(RadPHI));
            var III = ((nu / 24) * (Math.sin(RadPHI)) * (Math.pow(Math.cos(RadPHI), 3))) * (5 - (Math.pow(Math.tan(RadPHI), 2)) + (9 * eta2));
            var IIIA = ((nu / 720) * (Math.sin(RadPHI)) * (Math.pow(Math.cos(RadPHI), 5))) * (61 - (58 * (Math.pow(Math.tan(RadPHI), 2))) + (Math.pow(Math.tan(RadPHI), 4)));
            return I + (Math.pow(p, 2) * II) + (Math.pow(p, 4) * III) + (Math.pow(p, 6) * IIIA);
        };
        GT_Math.WGS84_To_OSGB = function (item) {
            var osgb = new GT_OSGB_1.default();
            if (item.isGreatBritain()) {
                var height = 0;
                var x1 = this.Lat_Long_H_to_X(item.Latitude, item.Longitude, height, 6378137.00, 6356752.313);
                var y1 = this.Lat_Long_H_to_Y(item.Latitude, item.Longitude, height, 6378137.00, 6356752.313);
                var z1 = this.Lat_H_to_Z(item.Latitude, height, 6378137.00, 6356752.313);
                var x2 = this.Helmert_X(x1, y1, z1, -446.448, -0.2470, -0.8421, 20.4894);
                var y2 = this.Helmert_Y(x1, y1, z1, 125.157, -0.1502, -0.8421, 20.4894);
                var z2 = this.Helmert_Z(x1, y1, z1, -542.060, -0.1502, -0.2470, 20.4894);
                var latitude2 = this.XYZ_to_Lat(x2, y2, z2, 6377563.396, 6356256.910);
                var longitude2 = this.XYZ_to_Long(x2, y2);
                var e = this.Lat_Long_to_East(latitude2, longitude2, 6377563.396, 6356256.910, 400000, 0.999601272, 49.00000, -2.00000);
                var n = this.Lat_Long_to_North(latitude2, longitude2, 6377563.396, 6356256.910, 400000, -100000, 0.999601272, 49.00000, -2.00000);
                osgb.setGridCoordinates(Math.round(e), Math.round(n));
            }
            else {
                osgb.setError("Coordinate not within Great Britain");
            }
            return osgb;
        };
        GT_Math.OSGB_To_WGS84 = function (item) {
            var height = 0;
            var lat1 = this.E_N_to_Lat(item.Eastings, item.Northings, 6377563.396, 6356256.910, 400000, -100000, 0.999601272, 49.00000, -2.00000);
            var lon1 = this.E_N_to_Long(item.Eastings, item.Northings, 6377563.396, 6356256.910, 400000, -100000, 0.999601272, 49.00000, -2.00000);
            var x1 = this.Lat_Long_H_to_X(lat1, lon1, height, 6377563.396, 6356256.910);
            var y1 = this.Lat_Long_H_to_Y(lat1, lon1, height, 6377563.396, 6356256.910);
            var z1 = this.Lat_H_to_Z(lat1, height, 6377563.396, 6356256.910);
            var x2 = this.Helmert_X(x1, y1, z1, 446.448, 0.2470, 0.8421, -20.4894);
            var y2 = this.Helmert_Y(x1, y1, z1, -125.157, 0.1502, 0.8421, -20.4894);
            var z2 = this.Helmert_Z(x1, y1, z1, 542.060, 0.1502, 0.2470, -20.4894);
            var latitude = this.XYZ_to_Lat(x2, y2, z2, 6378137.000, 6356752.313);
            var longitude = this.XYZ_to_Long(x2, y2);
            return new GT_WGS84_1.default(latitude, longitude);
        };
        ;
        return GT_Math;
    }());
    return GT_Math;
});
