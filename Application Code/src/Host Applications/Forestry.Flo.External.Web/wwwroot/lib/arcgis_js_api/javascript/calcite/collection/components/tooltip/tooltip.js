/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Host, h } from "@stencil/core";
import { CSS, ARIA_DESCRIBED_BY } from "./resources";
import { guid } from "../../utils/guid";
import { connectFloatingUI, disconnectFloatingUI, defaultOffsetDistance, reposition, FloatingCSS, updateAfterClose } from "../../utils/floating-ui";
import { queryElementRoots, toAriaBoolean } from "../../utils/dom";
import TooltipManager from "./TooltipManager";
const manager = new TooltipManager();
/**
 * @slot - A slot for adding text.
 */
export class Tooltip {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /** Closes the component when the `referenceElement` is clicked. */
    this.closeOnClick = false;
    /**
     * Offset the position of the component away from the `referenceElement`.
     *
     * @default 6
     */
    this.offsetDistance = defaultOffsetDistance;
    /**
     * Offset the position of the component along the `referenceElement`.
     */
    this.offsetSkidding = 0;
    /**
     * When `true`, the component is open.
     */
    this.open = false;
    /**
     * Determines the type of positioning to use for the overlaid content.
     *
     * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
     *
     * The `"fixed"` value should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
     *
     */
    this.overlayPositioning = "absolute";
    /**
     * Determines where the component will be positioned relative to the `referenceElement`.
     *
     * @see [LogicalPlacement](https://github.com/Esri/calcite-components/blob/master/src/utils/floating-ui.ts#L25)
     */
    this.placement = "auto";
    this.guid = `calcite-tooltip-${guid()}`;
    this.hasLoaded = false;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.setUpReferenceElement = (warn = true) => {
      this.removeReferences();
      this.effectiveReferenceElement = this.getReferenceElement();
      connectFloatingUI(this, this.effectiveReferenceElement, this.el);
      const { el, referenceElement, effectiveReferenceElement } = this;
      if (warn && referenceElement && !effectiveReferenceElement) {
        console.warn(`${el.tagName}: reference-element id "${referenceElement}" was not found.`, {
          el
        });
      }
      this.addReferences();
    };
    this.getId = () => {
      return this.el.id || this.guid;
    };
    this.addReferences = () => {
      const { effectiveReferenceElement } = this;
      if (!effectiveReferenceElement) {
        return;
      }
      const id = this.getId();
      if ("setAttribute" in effectiveReferenceElement) {
        effectiveReferenceElement.setAttribute(ARIA_DESCRIBED_BY, id);
      }
      manager.registerElement(effectiveReferenceElement, this.el);
    };
    this.removeReferences = () => {
      const { effectiveReferenceElement } = this;
      if (!effectiveReferenceElement) {
        return;
      }
      if ("removeAttribute" in effectiveReferenceElement) {
        effectiveReferenceElement.removeAttribute(ARIA_DESCRIBED_BY);
      }
      manager.unregisterElement(effectiveReferenceElement);
    };
  }
  offsetDistanceOffsetHandler() {
    this.reposition(true);
  }
  offsetSkiddingHandler() {
    this.reposition(true);
  }
  openHandler(value) {
    if (value) {
      this.reposition(true);
    }
    else {
      updateAfterClose(this.el);
    }
  }
  overlayPositioningHandler() {
    this.reposition(true);
  }
  placementHandler() {
    this.reposition(true);
  }
  referenceElementHandler() {
    this.setUpReferenceElement();
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    this.setUpReferenceElement(this.hasLoaded);
  }
  componentDidLoad() {
    if (this.referenceElement && !this.effectiveReferenceElement) {
      this.setUpReferenceElement();
    }
    this.reposition(true);
    this.hasLoaded = true;
  }
  disconnectedCallback() {
    this.removeReferences();
    disconnectFloatingUI(this, this.effectiveReferenceElement, this.el);
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Updates the position of the component.
   *
   * @param delayed
   */
  async reposition(delayed = false) {
    const { el, effectiveReferenceElement, placement, overlayPositioning, offsetDistance, offsetSkidding, arrowEl } = this;
    return reposition(this, {
      floatingEl: el,
      referenceEl: effectiveReferenceElement,
      overlayPositioning,
      placement,
      offsetDistance,
      offsetSkidding,
      includeArrow: true,
      arrowEl,
      type: "tooltip"
    }, delayed);
  }
  getReferenceElement() {
    const { referenceElement, el } = this;
    return ((typeof referenceElement === "string"
      ? queryElementRoots(el, { id: referenceElement })
      : referenceElement) || null);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const { effectiveReferenceElement, label, open } = this;
    const displayed = effectiveReferenceElement && open;
    const hidden = !displayed;
    return (h(Host, { "aria-hidden": toAriaBoolean(hidden), "aria-label": label, "aria-live": "polite", "calcite-hydrated-hidden": hidden, id: this.getId(), role: "tooltip" }, h("div", { class: {
        [FloatingCSS.animation]: true,
        [FloatingCSS.animationActive]: displayed
      } }, h("div", { class: CSS.arrow, ref: (arrowEl) => (this.arrowEl = arrowEl) }), h("div", { class: CSS.container }, h("slot", null)))));
  }
  static get is() { return "calcite-tooltip"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tooltip.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tooltip.css"]
    };
  }
  static get properties() {
    return {
      "closeOnClick": {
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
          "text": "Closes the component when the `referenceElement` is clicked."
        },
        "attribute": "close-on-click",
        "reflect": true,
        "defaultValue": "false"
      },
      "label": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Accessible name for the component."
        },
        "attribute": "label",
        "reflect": false
      },
      "offsetDistance": {
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
          "tags": [{
              "name": "default",
              "text": "6"
            }],
          "text": "Offset the position of the component away from the `referenceElement`."
        },
        "attribute": "offset-distance",
        "reflect": true,
        "defaultValue": "defaultOffsetDistance"
      },
      "offsetSkidding": {
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
          "text": "Offset the position of the component along the `referenceElement`."
        },
        "attribute": "offset-skidding",
        "reflect": true,
        "defaultValue": "0"
      },
      "open": {
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
          "text": "When `true`, the component is open."
        },
        "attribute": "open",
        "reflect": true,
        "defaultValue": "false"
      },
      "overlayPositioning": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "OverlayPositioning",
          "resolved": "\"absolute\" | \"fixed\"",
          "references": {
            "OverlayPositioning": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Determines the type of positioning to use for the overlaid content.\n\nUsing `\"absolute\"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.\n\nThe `\"fixed\"` value should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `\"fixed\"`."
        },
        "attribute": "overlay-positioning",
        "reflect": true,
        "defaultValue": "\"absolute\""
      },
      "placement": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "LogicalPlacement",
          "resolved": "Placement | VariationPlacement | AutoPlacement | DeprecatedPlacement",
          "references": {
            "LogicalPlacement": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "see",
              "text": "[LogicalPlacement](https://github.com/Esri/calcite-components/blob/master/src/utils/floating-ui.ts#L25)"
            }],
          "text": "Determines where the component will be positioned relative to the `referenceElement`."
        },
        "attribute": "placement",
        "reflect": true,
        "defaultValue": "\"auto\""
      },
      "referenceElement": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ReferenceElement | string",
          "resolved": "Element | VirtualElement | string",
          "references": {
            "ReferenceElement": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The `referenceElement` to position the component according to its `\"placement\"` value.\n\nSetting to the `HTMLElement` is preferred so the component does not need to query the DOM for the `referenceElement`.\n\nHowever, a string ID of the reference element can be used."
        },
        "attribute": "reference-element",
        "reflect": false
      }
    };
  }
  static get states() {
    return {
      "effectiveReferenceElement": {}
    };
  }
  static get methods() {
    return {
      "reposition": {
        "complexType": {
          "signature": "(delayed?: boolean) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "delayed"
                }],
              "text": ""
            }],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Updates the position of the component.",
          "tags": [{
              "name": "param",
              "text": "delayed"
            }]
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "offsetDistance",
        "methodName": "offsetDistanceOffsetHandler"
      }, {
        "propName": "offsetSkidding",
        "methodName": "offsetSkiddingHandler"
      }, {
        "propName": "open",
        "methodName": "openHandler"
      }, {
        "propName": "overlayPositioning",
        "methodName": "overlayPositioningHandler"
      }, {
        "propName": "placement",
        "methodName": "placementHandler"
      }, {
        "propName": "referenceElement",
        "methodName": "referenceElementHandler"
      }];
  }
}
