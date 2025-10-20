define(["require", "exports"], function (require, exports) {
    var mapSettings = /** @class */ (function () {
        function mapSettings() {
        }
        mapSettings.fontsUrl = "/assets/fonts";
        mapSettings.gridPrecision = 3;
        mapSettings.tolerance = 0.01;
        mapSettings.spatialReference = 27700,
            mapSettings.englandExtent = {
                xmin: 132406.2749467052,
                ymin: 3348.8733624143033,
                xmax: 668550.3805471654,
                ymax: 599274.2565042875,
                spatialReference: mapSettings.spatialReference,
            };
        mapSettings.requestParamsAPI = {
            mode: 'no-cors',
            cache: 'no-cache',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json'
            },
        };

        mapSettings.LabelText = {
            drawLine: "Click to begin line<br />click again to add new point.<br />Double click to finish",
            drawPoly: "Click to begin shape:<br />Click again to add new point.<br />You can also click and hold to draw freehand.<br />Double click to finish",
            drawPoint: "Click to add a point",
            cutPoly: "Cut a shape out of another shape:<br />Click to begin shape, click again to add new point.<br />You can also click and hold to draw freehand.<br />Double click to finish"
        },

            mapSettings.esriGeoServiceLocatorUrl = "http://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer";
        mapSettings.esriApiKey = "AAPK3dfe831d59354a7b97e58425b66f968eGwNWMzPWa6UrLRkrtTnpujN2iCjDkyXJegZabNGlyJ28aXl90McRXCw3qUF1IzLi";
        mapSettings.defaultBaseMap = "arcgis-topographic";
        mapSettings.baseMapForUK = "778b49e161db47aba6dd4f5034f9d52b";

        mapSettings.wmsLayer = {
            url: "https://www.getmapping.com/GmWMS/1499a302-3ea8-40c8-9480-c16beab54b1f/ApgbBng.wmsx",
            sublayers: [
                {
                    name: "APGB_Latest_UK_125mm",
                    title: "Indexing for 25cm Aerial Photography (2024)"
                }
            ],
            format: "image/png",
            version: "1.3.0",
            spatialReference: { wkid: mapSettings.spatialReference },
            opacity: 1.0
        };

        mapSettings.wmsLayerName = "Aerial Photography";



        // //Start polygon
        mapSettings.invalidPolygonSymbol = {
            type: "simple-fill",
            style: "diagonal-cross",
            color: [255, 0, 0, 1],
            outline: {
                color: [255, 0, 0, 1],
                width: 3,
            },
        };
        mapSettings.activePolygonSymbol = {
            type: "simple-fill",
            style: "solid",
            color: [191, 227, 247, 0.5],
            outline: {
                color: [0, 0, 0, 1],
                width: 1,
            },
        };
        mapSettings.otherPolygonSymbol = {
            type: "simple-fill",
            style: "solid",
            color: [191, 227, 247, 0.25],
            outline: {
                color: [0, 0, 0, 0.25],
                width: 1,
            },
        };
        mapSettings.selectedPolygonSymbol = {
            type: "simple-fill",
            style: "solid",
            color: [191, 227, 247, 0.5],
            outline: {
                style: "dash",
                color: [0, 0, 0, 1],
                width: 1,
            },
        };
        mapSettings.importShapeSelected = {
            type: "simple-fill",
            color: [255, 0, 0, 0.5],
            outline: {
                color: [255, 255, 0],
                width: 2
            }
        };
        mapSettings.importShape = {
            type: "simple-fill",
            color: [0, 0, 255, 0.5],
            outline: {
                color: [0, 0, 255],
                width: 1
            }
        };
        //end polygon
   
        mapSettings.activeTextSymbol = {
            type: "text",
            backgroundColor: [255, 255, 255, 0],
            borderLineColor: [255, 255, 255, 0],
            color: [24, 24, 255, 1],
            font: {
                decoration: "none",
                family: "Roboto",
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
        mapSettings.BlueSkyTextSymbol = {
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

        mapSettings.visualVariables = {
            size: [
                {
                    type: "size",
                    field: "zoom",
                    stops: [
                        { value: 0, size: 8 },
                        { value: 10, size: 12 },
                        { value: 20, size: 16 }
                    ]
                }
            ],
            textSize: [
                {
                    type: "size",
                    field: "zoom",
                    stops: [
                        { value: 0, size: 8 },
                        { value: 8, size: 8 },
                        { value: 20, size: 8 }
                    ]
                }
            ]
        };

        mapSettings.pointOffset = {
            xoffset: 15,
            yoffset: 15,
        };

        mapSettings.popup = {
            width: "500",
            height: "400"
        };
        return mapSettings;
    }());
    return mapSettings;
});
