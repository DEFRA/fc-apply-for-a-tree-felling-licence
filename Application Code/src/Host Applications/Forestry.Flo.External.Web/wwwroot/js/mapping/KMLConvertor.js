define(["require", "exports"], function (require, exports) {
    'use strict';
    var toGeoJSON = (function () {
        function toGeoJSON() {
        }
        toGeoJSON.removeSpace = /\s*/g;
        toGeoJSON.trimSpace = /^\s*|\s*$/g;
        toGeoJSON.splitSpace = /\s+/;

        // generate a short, numeric hash of a string
        toGeoJSON.okhash = function (x) {
            if (!x || !x.length) return 0;
            for (var i = 0, h = 0; i < x.length; i++) {
                h = ((h << 5) - h) + x.charCodeAt(i) | 0;
            } return h;
        };

        // all Y children of X
        toGeoJSON.get = function (x, y) {
            const namespace = "http://www.opengis.net/kml/2.2";
            const elements = x.getElementsByTagNameNS(namespace, y);
            return elements.length ? elements : x.getElementsByTagName(y);
        }

        toGeoJSON.attr = function attr(x, y) { return x.getAttribute(y); }
        toGeoJSON.attrf = function (x, y) { return parseFloat(attr(x, y)); }
        // one Y child of X, if any, otherwise null
        toGeoJSON.get1 = function (x, y) { var n = this.get(x, y); return n.length ? n[0] : null; }

        // https://developer.mozilla.org/en-US/docs/Web/API/Node.normalize
        toGeoJSON.norm = function (el) { if (el.normalize) { el.normalize(); } return el; }

        // cast array x into numbers
        toGeoJSON.numarray = function (x) {
            for (var j = 0, o = []; j < x.length; j++) { o[j] = parseFloat(x[j]); }
            return o;
        }
        // get the content of a text node, if any
        toGeoJSON.nodeVal = function (x) {
            if (x) { this.norm(x); }
            return (x && x.textContent) || '';
        }
        // get the contents of multiple text nodes, if present
        toGeoJSON.getMulti = function (x, ys) {
            var o = {}, n, k;
            for (k = 0; k < ys.length; k++) {
                n = this.get1(x, ys[k]);
                if (n) o[ys[k]] = this.nodeVal(n);
            }
            return o;
        }
        // add properties of Y to X, overwriting if present in both
        toGeoJSON.extend = function (x, y) { for (var k in y) x[k] = y[k]; }

        // get one coordinate from a coordinate array, if any
        toGeoJSON.coord1 = function (v) { return this.numarray(v.replace(this.removeSpace, '').split(',')); }

        // get all coordinates from a coordinate array as [[],[]]
        toGeoJSON.coord = function (v) {
            var coords = v.replace(this.trimSpace, '').split(this.splitSpace),
                o = [];
            for (var i = 0; i < coords.length; i++) {
                o.push(this.coord1(coords[i]));
            }
            return o;
        }
        toGeoJSON.coordPair = function (x) {
            var ll = [attrf(x, 'lon'), attrf(x, 'lat')],
                ele = this.get1(x, 'ele'),
                // handle namespaced attribute in browser
                heartRate = this.get1(x, 'gpxtpx:hr') || this.get1(x, 'hr'),
                time = this.get1(x, 'time'),
                e;
            if (ele) {
                e = parseFloat(this.nodeVal(ele));
                if (!isNaN(e)) {
                    ll.push(e);
                }
            }
            return {
                coordinates: ll,
                time: time ? this.nodeVal(time) : null,
                heartRate: heartRate ? parseFloat(this.nodeVal(heartRate)) : null
            };
        }

        // create a new feature collection parent object
        function fc() {
            return {
                type: 'FeatureCollection',
                features: []
            };
        }

        var serializer;
        if (typeof XMLSerializer !== 'undefined') {
            /* istanbul ignore next */
            serializer = new XMLSerializer();
        } else {
            var isNodeEnv = (typeof process === 'object' && !process.browser);
            var isTitaniumEnv = (typeof Titanium === 'object');
            if (typeof exports === 'object' && (isNodeEnv || isTitaniumEnv)) {
                serializer = new (require('xmldom').XMLSerializer)();
            } else {
                throw new Error('Unable to initialize serializer');
            }
        }
        function xml2str(str) {
            // IE9 will create a new XMLSerializer but it'll crash immediately.
            // This line is ignored because we don't run coverage tests in IE9
            /* istanbul ignore next */
            if (str.xml !== undefined) return str.xml;
            return serializer.serializeToString(str);
        }

        toGeoJSON.kml = function (doc) {
            var that = this;
            var gj = fc(),
                // styleindex keeps track of hashed styles in order to match features
                styleIndex = {}, styleByHash = {},
                // stylemapindex keeps track of style maps to expose in properties
                styleMapIndex = {},
                // atomic geospatial types supported by KML - MultiGeometry is
                // handled separately
                geotypes = ['Polygon', 'LineString', 'Point', 'Track', 'gx:Track'],
                // all root placemarks in the file
                placemarks = this.get(doc, 'Placemark'),
                styles = this.get(doc, 'Style'),
                styleMaps = this.get(doc, 'StyleMap');

            for (var k = 0; k < styles.length; k++) {
                var hash = this.okhash(xml2str(styles[k])).toString(16);
                styleIndex['#' + this.attr(styles[k], 'id')] = hash;
                styleByHash[hash] = styles[k];
            }
            for (var l = 0; l < styleMaps.length; l++) {
                styleIndex['#' + this.attr(styleMaps[l], 'id')] = this.okhash(xml2str(styleMaps[l])).toString(16);
                var pairs = this.get(styleMaps[l], 'Pair');
                var pairsMap = {};
                for (var m = 0; m < pairs.length; m++) {
                    pairsMap[this.nodeVal(this.get1(pairs[m], 'key'))] = this.nodeVal(this.get1(pairs[m], 'styleUrl'));
                }
                styleMapIndex['#' + this.attr(styleMaps[l], 'id')] = pairsMap;

            }
            for (var j = 0; j < placemarks.length; j++) {
                gj.features = gj.features.concat(getPlacemark(placemarks[j]));
            }
            function kmlColor(v) {
                var color, opacity;
                v = v || '';
                if (v.substr(0, 1) === '#') { v = v.substr(1); }
                if (v.length === 6 || v.length === 3) { color = v; }
                if (v.length === 8) {
                    opacity = parseInt(v.substr(0, 2), 16) / 255;
                    color = '#' + v.substr(6, 2) +
                        v.substr(4, 2) +
                        v.substr(2, 2);
                }
                return [color, isNaN(opacity) ? undefined : opacity];
            }
            function gxCoord(v) { return numarray(v.split(' ')); }
            function gxCoords(root) {
                var elems = get(root, 'coord', 'gx'), coords = [], times = [];
                if (elems.length === 0) elems = get(root, 'gx:coord');
                for (var i = 0; i < elems.length; i++) coords.push(gxCoord(this.nodeVal(elems[i])));
                var timeElems = get(root, 'when');
                for (var j = 0; j < timeElems.length; j++) times.push(this.nodeVal(timeElems[j]));
                return {
                    coords: coords,
                    times: times
                };
            }
            function getGeometry(root) {
                var geomNode, geomNodes, i, j, k, geoms = [], coordTimes = [];
                if (that.get1(root, 'MultiGeometry')) { return getGeometry(that.get1(root, 'MultiGeometry')); }
                if (that.get1(root, 'MultiTrack')) { return getGeometry(that.get1(root, 'MultiTrack')); }
                if (that.get1(root, 'gx:MultiTrack')) { return getGeometry(that.get1(root, 'gx:MultiTrack')); }
                for (i = 0; i < geotypes.length; i++) {
                    geomNodes = that.get(root, geotypes[i]);
                    if (geomNodes) {
                        for (j = 0; j < geomNodes.length; j++) {
                            geomNode = geomNodes[j];
                            if (geotypes[i] === 'Point') {
                                geoms.push({
                                    type: 'Point',
                                    coordinates: that.coord1(that.nodeVal(that.get1(geomNode, 'coordinates')))
                                });
                            } else if (geotypes[i] === 'LineString') {
                                geoms.push({
                                    type: 'LineString',
                                    coordinates: that.coord(that.nodeVal(that.get1(geomNode, 'coordinates')))
                                });
                            } else if (geotypes[i] === 'Polygon') {
                                var rings = that.get(geomNode, 'LinearRing'),
                                    coords = [];
                                for (k = 0; k < rings.length; k++) {
                                    coords.push(that.coord(that.nodeVal(that.get1(rings[k], 'coordinates'))));
                                }
                                geoms.push({
                                    type: 'Polygon',
                                    coordinates: coords
                                });
                            } else if (geotypes[i] === 'Track' ||
                                geotypes[i] === 'gx:Track') {
                                var track = gxCoords(geomNode);
                                geoms.push({
                                    type: 'LineString',
                                    coordinates: track.coords
                                });
                                if (track.times.length) coordTimes.push(track.times);
                            }
                        }
                    }
                }
                return {
                    geoms: geoms,
                    coordTimes: coordTimes
                };
            }
            function getPlacemark(root) {
                var geomsAndTimes = getGeometry(root), i, properties = {},
                    name = that.nodeVal(that.get1(root, 'name')),
                    address = that.nodeVal(that.get1(root, 'address')),
                    styleUrl = that.nodeVal(that.get1(root, 'styleUrl')),
                    description = that.nodeVal(that.get1(root, 'description')),
                    timeSpan = that.get1(root, 'TimeSpan'),
                    timeStamp = that.get1(root, 'TimeStamp'),
                    extendedData = that.get1(root, 'ExtendedData'),
                    lineStyle = that.get1(root, 'LineStyle'),
                    polyStyle = that.get1(root, 'PolyStyle'),
                    visibility = that.get1(root, 'visibility');

                if (!geomsAndTimes.geoms.length) return [];
                if (name) properties.name = name;
                if (address) properties.address = address;
                if (styleUrl) {
                    if (styleUrl[0] !== '#') {
                        styleUrl = '#' + styleUrl;
                    }

                    properties.styleUrl = styleUrl;
                    if (styleIndex[styleUrl]) {
                        properties.styleHash = styleIndex[styleUrl];
                    }
                    if (styleMapIndex[styleUrl]) {
                        properties.styleMapHash = styleMapIndex[styleUrl];
                        properties.styleHash = styleIndex[styleMapIndex[styleUrl].normal];
                    }
                    // Try to populate the lineStyle or polyStyle since we got the style hash
                    var style = styleByHash[properties.styleHash];
                    if (style) {
                        if (!lineStyle) lineStyle = that.get1(style, 'LineStyle');
                        if (!polyStyle) polyStyle = that.get1(style, 'PolyStyle');
                        var iconStyle = that.get1(style, 'IconStyle');
                        if (iconStyle) {
                            var icon = that.get1(iconStyle, 'Icon');
                            if (icon) {
                                var href = that.nodeVal(that.get1(icon, 'href'));
                                if (href) properties.icon = href;
                            }
                        }
                    }
                }
                if (description) properties.description = description;
                if (timeSpan) {
                    var begin = that.nodeVal(that.get1(timeSpan, 'begin'));
                    var end = that.nodeVal(that.get1(timeSpan, 'end'));
                    properties.timespan = { begin: begin, end: end };
                }
                if (timeStamp) {
                    properties.timestamp = that.nodeVal(that.get1(timeStamp, 'when'));
                }
                if (lineStyle) {
                    var linestyles = kmlColor(that.nodeVal(that.get1(lineStyle, 'color'))),
                        color = linestyles[0],
                        opacity = linestyles[1],
                        width = parseFloat(that.nodeVal(that.get1(lineStyle, 'width')));
                    if (color) properties.stroke = color;
                    if (!isNaN(opacity)) properties['stroke-opacity'] = opacity;
                    if (!isNaN(width)) properties['stroke-width'] = width;
                }
                if (polyStyle) {
                    var polystyles = kmlColor(that.nodeVal(that.get1(polyStyle, 'color'))),
                        pcolor = polystyles[0],
                        popacity = polystyles[1],
                        fill = that.nodeVal(that.get1(polyStyle, 'fill')),
                        outline = that.nodeVal(that.get1(polyStyle, 'outline'));
                    if (pcolor) properties.fill = pcolor;
                    if (!isNaN(popacity)) properties['fill-opacity'] = popacity;
                    if (fill) properties['fill-opacity'] = fill === '1' ? properties['fill-opacity'] || 1 : 0;
                    if (outline) properties['stroke-opacity'] = outline === '1' ? properties['stroke-opacity'] || 1 : 0;
                }
                if (extendedData) {
                    var datas = that.get(extendedData, 'Data'),
                        simpleDatas = that.get(extendedData, 'SimpleData');

                    for (i = 0; i < datas.length; i++) {
                        properties[datas[i].getAttribute('name')] = that.nodeVal(that.get1(datas[i], 'value'));
                    }
                    for (i = 0; i < simpleDatas.length; i++) {
                        properties[simpleDatas[i].getAttribute('name')] = that.nodeVal(simpleDatas[i]);
                    }
                }
                if (visibility) {
                    properties.visibility = that.nodeVal(visibility);
                }
                if (geomsAndTimes.coordTimes.length) {
                    properties.coordTimes = (geomsAndTimes.coordTimes.length === 1) ?
                        geomsAndTimes.coordTimes[0] : geomsAndTimes.coordTimes;
                }
                var feature = {
                    type: 'Feature',
                    geometry: (geomsAndTimes.geoms.length === 1) ? geomsAndTimes.geoms[0] : {
                        type: 'GeometryCollection',
                        geometries: geomsAndTimes.geoms
                    },
                    properties: properties
                };
                if (that.attr(root, 'id')) feature.id = that.attr(root, 'id');
                return [feature];
            }
            return gj;
        }
        return toGeoJSON;
    }());
    return toGeoJSON;
});
