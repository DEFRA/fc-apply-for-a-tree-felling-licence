/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, forceUpdate } from "@stencil/core";
import { guid } from "../../utils/guid";
import { area, range, translate } from "./util";
import { createObserver } from "../../utils/observers";
export class Graph {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * Array of tuples describing a single data point ([x, y])
     * These data points should be sorted by x-axis value
     */
    this.data = [];
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    this.graphId = `calcite-graph-${guid()}`;
    this.resizeObserver = createObserver("resize", () => forceUpdate(this));
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el);
  }
  disconnectedCallback() {
    var _a;
    (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  render() {
    const { data, colorStops, el, highlightMax, highlightMin, min, max } = this;
    const id = this.graphId;
    const { clientHeight: height, clientWidth: width } = el;
    // if we have no data, return empty svg
    if (!data || data.length === 0) {
      return (h("svg", { class: "svg", height: height, preserveAspectRatio: "none", viewBox: `0 0 ${width} ${height}`, width: width }));
    }
    const { min: rangeMin, max: rangeMax } = range(data);
    let currentMin = rangeMin;
    let currentMax = rangeMax;
    if (min < rangeMin[0] || min > rangeMin[0]) {
      currentMin = [min, 0];
    }
    if (max > rangeMax[0] || max < rangeMax[0]) {
      currentMax = [max, rangeMax[1]];
    }
    const t = translate({ min: currentMin, max: currentMax, width, height });
    const [hMinX] = t([highlightMin, currentMax[1]]);
    const [hMaxX] = t([highlightMax, currentMax[1]]);
    const areaPath = area({ data, min: rangeMin, max: rangeMax, t });
    const fill = colorStops ? `url(#linear-gradient-${id})` : undefined;
    return (h("svg", { class: "svg", height: height, preserveAspectRatio: "none", viewBox: `0 0 ${width} ${height}`, width: width }, colorStops ? (h("defs", null, h("linearGradient", { id: `linear-gradient-${id}`, x1: "0", x2: "1", y1: "0", y2: "0" }, colorStops.map(({ offset, color, opacity }) => (h("stop", { offset: `${offset * 100}%`, "stop-color": color, "stop-opacity": opacity })))))) : null, highlightMin !== undefined ? ([
      h("mask", { height: "100%", id: `${id}1`, width: "100%", x: "0%", y: "0%" }, h("path", { d: `
            M 0,0
            L ${hMinX - 1},0
            L ${hMinX - 1},${height}
            L 0,${height}
            Z
          `, fill: "white" })),
      h("mask", { height: "100%", id: `${id}2`, width: "100%", x: "0%", y: "0%" }, h("path", { d: `
            M ${hMinX + 1},0
            L ${hMaxX - 1},0
            L ${hMaxX - 1},${height}
            L ${hMinX + 1}, ${height}
            Z
          `, fill: "white" })),
      h("mask", { height: "100%", id: `${id}3`, width: "100%", x: "0%", y: "0%" }, h("path", { d: `
                M ${hMaxX + 1},0
                L ${width},0
                L ${width},${height}
                L ${hMaxX + 1}, ${height}
                Z
              `, fill: "white" })),
      h("path", { class: "graph-path", d: areaPath, fill: fill, mask: `url(#${id}1)` }),
      h("path", { class: "graph-path--highlight", d: areaPath, fill: fill, mask: `url(#${id}2)` }),
      h("path", { class: "graph-path", d: areaPath, fill: fill, mask: `url(#${id}3)` })
    ]) : (h("path", { class: "graph-path", d: areaPath, fill: fill }))));
  }
  static get is() { return "calcite-graph"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["graph.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["graph.css"]
    };
  }
  static get properties() {
    return {
      "data": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "DataSeries",
          "resolved": "Point[]",
          "references": {
            "DataSeries": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Array of tuples describing a single data point ([x, y])\nThese data points should be sorted by x-axis value"
        },
        "defaultValue": "[]"
      },
      "colorStops": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "ColorStop[]",
          "resolved": "ColorStop[]",
          "references": {
            "ColorStop": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Array of values describing a single color stop ([offset, color, opacity])\nThese color stops should be sorted by offset value"
        }
      },
      "highlightMin": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Start of highlight color if highlighting range"
        },
        "attribute": "highlight-min",
        "reflect": false
      },
      "highlightMax": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "End of highlight color if highlighting range"
        },
        "attribute": "highlight-max",
        "reflect": false
      },
      "min": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Lowest point of the range"
        },
        "attribute": "min",
        "reflect": true
      },
      "max": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Highest point of the range"
        },
        "attribute": "max",
        "reflect": true
      }
    };
  }
  static get elementRef() { return "el"; }
}
