/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Build, h, Host } from "@stencil/core";
import { CSS } from "./resources";
import { getElementDir, toAriaBoolean } from "../../utils/dom";
import { fetchIcon, scaleToPx } from "./utils";
import { createObserver } from "../../utils/observers";
export class Icon {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * The name of the icon to display. The value of this property must match the icon name from https://esri.github.io/calcite-ui-icons/.
     */
    this.icon = null;
    /**
     * When true, the icon will be flipped when the element direction is 'rtl'.
     */
    this.flipRtl = false;
    /**
     * Icon scale.
     */
    this.scale = "m";
    this.visible = false;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.waitUntilVisible(() => {
      this.visible = true;
      this.loadIconPathData();
    });
  }
  disconnectedCallback() {
    var _a;
    (_a = this.intersectionObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    this.intersectionObserver = null;
  }
  async componentWillLoad() {
    this.loadIconPathData();
  }
  render() {
    const { el, flipRtl, pathData, scale, textLabel } = this;
    const dir = getElementDir(el);
    const size = scaleToPx[scale];
    const semantic = !!textLabel;
    const paths = [].concat(pathData || "");
    return (h(Host, { "aria-hidden": toAriaBoolean(!semantic), "aria-label": semantic ? textLabel : null, role: semantic ? "img" : null }, h("svg", { class: {
        [CSS.flipRtl]: dir === "rtl" && flipRtl,
        svg: true
      }, fill: "currentColor", height: "100%", viewBox: `0 0 ${size} ${size}`, width: "100%", xmlns: "http://www.w3.org/2000/svg" }, paths.map((path) => typeof path === "string" ? (h("path", { d: path })) : (h("path", { d: path.d, opacity: "opacity" in path ? path.opacity : 1 }))))));
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  async loadIconPathData() {
    const { icon, scale, visible } = this;
    if (!Build.isBrowser || !icon || !visible) {
      return;
    }
    this.pathData = await fetchIcon({ icon, scale });
  }
  waitUntilVisible(callback) {
    this.intersectionObserver = createObserver("intersection", (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          this.intersectionObserver.disconnect();
          this.intersectionObserver = null;
          callback();
        }
      });
    }, { rootMargin: "50px" });
    if (!this.intersectionObserver) {
      callback();
      return;
    }
    this.intersectionObserver.observe(this.el);
  }
  static get is() { return "calcite-icon"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["icon.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["icon.css"]
    };
  }
  static get assetsDirs() { return ["assets"]; }
  static get properties() {
    return {
      "icon": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The name of the icon to display. The value of this property must match the icon name from https://esri.github.io/calcite-ui-icons/."
        },
        "attribute": "icon",
        "reflect": true,
        "defaultValue": "null"
      },
      "flipRtl": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When true, the icon will be flipped when the element direction is 'rtl'."
        },
        "attribute": "flip-rtl",
        "reflect": true,
        "defaultValue": "false"
      },
      "scale": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Scale",
          "resolved": "\"l\" | \"m\" | \"s\"",
          "references": {
            "Scale": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Icon scale."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "textLabel": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The icon label.\n\nIt is recommended to set this value if your icon is semantic."
        },
        "attribute": "text-label",
        "reflect": false
      }
    };
  }
  static get states() {
    return {
      "pathData": {},
      "visible": {}
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "icon",
        "methodName": "loadIconPathData"
      }, {
        "propName": "scale",
        "methodName": "loadIconPathData"
      }];
  }
}
