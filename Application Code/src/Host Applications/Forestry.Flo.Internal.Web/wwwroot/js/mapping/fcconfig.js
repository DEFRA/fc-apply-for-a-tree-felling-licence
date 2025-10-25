define(["require", "exports"], function (require, exports) {
    var fcconfig = /** @class */ (function () {
        function fcconfig() {
        }
        fcconfig.gridPrecision = 3;
        fcconfig.tolerance = 0.01;
        fcconfig.spatialReference = 27700,
            fcconfig.englandExtent = {
                xmin: 132406.2749467052,
                ymin: 3348.8733624143033,
                xmax: 668550.3805471654,
                ymax: 599274.2565042875,
                spatialReference: fcconfig.spatialReference,
            };
        fcconfig.requestParamsAPI = {
            mode: 'no-cors',
            cache: 'no-cache',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json'
            },
        };

        fcconfig.LabelText = {
            drawLine: "Click to begin line<br />click again to add new point.<br />Double click to finish",
            drawPoly: "Click to begin shape<br />click again to add new point.<br />Double click to finish",
            drawPoint: "Click to add a point",
            cutPoly: "Cut a shape out of another shape:<br />click to begin shape, click again to add new point.<br />Double click to finish"
        },

            fcconfig.esriGeoServiceLocatorUrl = "http://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer";
        fcconfig.esriApiKey = "AAPK3dfe831d59354a7b97e58425b66f968eGwNWMzPWa6UrLRkrtTnpujN2iCjDkyXJegZabNGlyJ28aXl90McRXCw3qUF1IzLi";
        fcconfig.defaultBaseMap = "arcgis-topographic";
        fcconfig.baseMapForUK = "778b49e161db47aba6dd4f5034f9d52b";

        fcconfig.wmsLayer = {
            url: "https://www.getmapping.com/GmWMS/1499a302-3ea8-40c8-9480-c16beab54b1f/ApgbBng.wmsx",
            sublayers: [
                {
                    name: "APGB_Latest_UK_125mm",
                    title: "Indexing for 25cm Aerial Photography (2024)"
                }
            ],
            format: "image/png",
            version: "1.3.0",
            spatialReference: { wkid: fcconfig.spatialReference },
            opacity: 1.0
        };

        fcconfig.wmsLayerName = "Aerial Photography";


        // //Start polygon
        fcconfig.invalidPolygonSymbol = {
            type: "simple-fill",
            style: "diagonal-cross",
            color: [255, 0, 0, 1],
            outline: {
                color: [255, 0, 0, 1],
                width: 3,
            },
        };
        fcconfig.activePolygonSymbol = {
            type: "simple-fill",
            style: "solid",
            color: [191, 227, 247, 0.5],
            outline: {
                color: [0, 0, 0, 1],
                width: 1,
            },
        };
        fcconfig.otherPolygonSymbol = {
            type: "simple-fill",
            style: "solid",
            color: [191, 227, 247, 0.25],
            outline: {
                color: [0, 0, 0, 0.25],
                width: 1,
            },
        };
        fcconfig.selectedPolygonSymbol = {
            type: "simple-fill",
            style: "solid",
            color: [191, 227, 247, 0.5],
            outline: {
                style: "dash",
                color: [0, 0, 0, 1],
                width: 1,
            },
        };
        //end polygon

        fcconfig.BlueSkyTextSymbol = {
            type: "text",
            color: "rgba(128, 128, 128, 0.3)",
            text: "\u00A9 Bluesky International Limited",
            xoffset: 3,
            yoffset: 3,
            font: {
                size: 16,
                family: "Roboto",
                weight: "bold",
            },
            angle: -45
        };

        fcconfig.activeTextSymbol = {
            type: "text",
            backgroundColor: [255, 255, 255, 0],
            borderLineColor: [255, 255, 255, 0],
            color: [24, 24, 255, 1],
            font: {
                decoration: "none",
                family: "Arial",
                size: 8,
                style: "normal",
                weight: "bold"
            },
            haloColor: [255, 255, 255, 1],
            haloSize: 1,
            horizontalAlignment: "center",
            kerning: false,
            lineWidth: 192,
            rotated: false,
            verticalAlignment: "baseline",
            xoffset: 0,
            yoffset: 0
        };
        fcconfig.pointOffset = {
            xoffset: 15,
            yoffset: 15,
        };

        fcconfig.popup = {
            width: "500",
            height: "400"
        };
        return fcconfig;
    }());
    return fcconfig;
});
