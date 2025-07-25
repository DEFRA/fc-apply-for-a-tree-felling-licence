/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { forceUpdate, Host, h } from "@stencil/core";
import { CSS, ARIA_CONTROLS, ARIA_EXPANDED, HEADING_LEVEL, TEXT, defaultPopoverPlacement } from "./resources";
import { FloatingCSS, connectFloatingUI, disconnectFloatingUI, defaultOffsetDistance, filterComputedPlacements, reposition, updateAfterClose } from "../../utils/floating-ui";
import { guid } from "../../utils/guid";
import { queryElementRoots, toAriaBoolean } from "../../utils/dom";
import { connectOpenCloseComponent, disconnectOpenCloseComponent } from "../../utils/openCloseComponent";
import { Heading } from "../functional/Heading";
import PopoverManager from "./PopoverManager";
const manager = new PopoverManager();
/**
 * @slot - A slot for adding custom content.
 */
export class Popover {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, clicking outside of the component automatically closes open `calcite-popover`s.
     */
    this.autoClose = false;
    /**
     * When `true`, a close button is added to the component.
     *
     * @deprecated use dismissible instead.
     */
    this.closeButton = false;
    /**
     * When `true`, a close button is added to the component.
     *
     * @deprecated use `closable` instead.
     */
    this.dismissible = false;
    /** When `true`, display a close button within the component. */
    this.closable = false;
    /**
     * When `true`, prevents flipping the component's placement when overlapping its `referenceElement`.
     */
    this.disableFlip = false;
    /**
     * When `true`, removes the caret pointer.
     */
    this.disablePointer = false;
    /**
     * Offsets the position of the component away from the `referenceElement`.
     *
     * @default 6
     */
    this.offsetDistance = defaultOffsetDistance;
    /**
     * Offsets the position of the component along the `referenceElement`.
     */
    this.offsetSkidding = 0;
    /**
     * When `true`, displays and positions the component.
     */
    this.open = false;
    /**
     * Determines the type of positioning to use for the overlaid content.
     *
     * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
     *
     * `"fixed"` value should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
     *
     */
    this.overlayPositioning = "absolute";
    /**
     * Determines where the component will be positioned relative to the `referenceElement`.
     *
     * @see [LogicalPlacement](https://github.com/Esri/calcite-components/blob/master/src/utils/floating-ui.ts#L25)
     */
    this.placement = defaultPopoverPlacement;
    /** Specifies the size of the component. */
    this.scale = "m";
    /**
     * When `true`, disables automatically toggling the component when its `referenceElement` has been triggered.
     *
     * This property can be set to `true` to manage when the component is open.
     */
    this.triggerDisabled = false;
    /**
     * Accessible name for the component's close button.
     *
     * @default "Close"
     */
    this.intlClose = TEXT.close;
    this.guid = `calcite-popover-${guid()}`;
    this.openTransitionProp = "opacity";
    this.hasLoaded = false;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.setTransitionEl = (el) => {
      this.transitionEl = el;
      connectOpenCloseComponent(this);
    };
    this.setFilteredPlacements = () => {
      const { el, flipPlacements } = this;
      this.filteredFlipPlacements = flipPlacements
        ? filterComputedPlacements(flipPlacements, el)
        : null;
    };
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
    this.setExpandedAttr = () => {
      const { effectiveReferenceElement, open } = this;
      if (!effectiveReferenceElement) {
        return;
      }
      if ("setAttribute" in effectiveReferenceElement) {
        effectiveReferenceElement.setAttribute(ARIA_EXPANDED, toAriaBoolean(open));
      }
    };
    this.addReferences = () => {
      const { effectiveReferenceElement } = this;
      if (!effectiveReferenceElement) {
        return;
      }
      const id = this.getId();
      if ("setAttribute" in effectiveReferenceElement) {
        effectiveReferenceElement.setAttribute(ARIA_CONTROLS, id);
      }
      manager.registerElement(effectiveReferenceElement, this.el);
      this.setExpandedAttr();
    };
    this.removeReferences = () => {
      const { effectiveReferenceElement } = this;
      if (!effectiveReferenceElement) {
        return;
      }
      if ("removeAttribute" in effectiveReferenceElement) {
        effectiveReferenceElement.removeAttribute(ARIA_CONTROLS);
        effectiveReferenceElement.removeAttribute(ARIA_EXPANDED);
      }
      manager.unregisterElement(effectiveReferenceElement);
    };
    this.hide = () => {
      this.open = false;
    };
    this.storeArrowEl = (el) => {
      this.arrowEl = el;
      this.reposition(true);
    };
  }
  handleDismissible(value) {
    this.closable = value;
  }
  handleClosable(value) {
    this.dismissible = value;
  }
  flipPlacementsHandler() {
    this.setFilteredPlacements();
    this.reposition(true);
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
    this.setExpandedAttr();
  }
  overlayPositioningHandler() {
    this.reposition(true);
  }
  placementHandler() {
    this.reposition(true);
  }
  referenceElementHandler() {
    this.setUpReferenceElement();
    this.reposition(true);
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    this.setFilteredPlacements();
    connectOpenCloseComponent(this);
    const closable = this.closable || this.dismissible;
    if (closable) {
      this.handleDismissible(closable);
    }
    if (closable) {
      this.handleClosable(closable);
    }
    this.setUpReferenceElement(this.hasLoaded);
  }
  componentDidLoad() {
    if (this.referenceElement && !this.effectiveReferenceElement) {
      this.setUpReferenceElement();
    }
    this.reposition();
    this.hasLoaded = true;
  }
  disconnectedCallback() {
    this.removeReferences();
    disconnectFloatingUI(this, this.effectiveReferenceElement, this.el);
    disconnectOpenCloseComponent(this);
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
    const { el, effectiveReferenceElement, placement, overlayPositioning, disableFlip, filteredFlipPlacements, offsetDistance, offsetSkidding, arrowEl } = this;
    return reposition(this, {
      floatingEl: el,
      referenceEl: effectiveReferenceElement,
      overlayPositioning,
      placement,
      disableFlip,
      flipPlacements: filteredFlipPlacements,
      offsetDistance,
      offsetSkidding,
      includeArrow: !this.disablePointer,
      arrowEl,
      type: "popover"
    }, delayed);
  }
  /**
   * Sets focus on the component.
   *
   * @param focusId
   */
  async setFocus(focusId) {
    var _a;
    const { closeButtonEl } = this;
    if (focusId === "close-button" && closeButtonEl) {
      forceUpdate(closeButtonEl);
      closeButtonEl.setFocus();
      return;
    }
    (_a = this.el) === null || _a === void 0 ? void 0 : _a.focus();
  }
  /**
   * Toggles the component's open property.
   *
   * @param value
   */
  async toggle(value = !this.open) {
    this.open = value;
  }
  getReferenceElement() {
    const { referenceElement, el } = this;
    return ((typeof referenceElement === "string"
      ? queryElementRoots(el, { id: referenceElement })
      : referenceElement) || null);
  }
  onBeforeOpen() {
    this.calcitePopoverBeforeOpen.emit();
  }
  onOpen() {
    this.calcitePopoverOpen.emit();
  }
  onBeforeClose() {
    this.calcitePopoverBeforeClose.emit();
  }
  onClose() {
    this.calcitePopoverClose.emit();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderCloseButton() {
    const { closeButton, intlClose, closable } = this;
    return closable || closeButton ? (h("div", { class: CSS.closeButtonContainer }, h("calcite-action", { class: CSS.closeButton, onClick: this.hide, ref: (closeButtonEl) => (this.closeButtonEl = closeButtonEl), scale: this.scale, text: intlClose }, h("calcite-icon", { icon: "x", scale: this.scale === "l" ? "m" : this.scale })))) : null;
  }
  renderHeader() {
    const { heading, headingLevel } = this;
    const headingNode = heading ? (h(Heading, { class: CSS.heading, level: headingLevel || HEADING_LEVEL }, heading)) : null;
    return headingNode ? (h("div", { class: CSS.header }, headingNode, this.renderCloseButton())) : null;
  }
  render() {
    const { effectiveReferenceElement, heading, label, open, disablePointer } = this;
    const displayed = effectiveReferenceElement && open;
    const hidden = !displayed;
    const arrowNode = !disablePointer ? h("div", { class: CSS.arrow, ref: this.storeArrowEl }) : null;
    return (h(Host, { "aria-hidden": toAriaBoolean(hidden), "aria-label": label, "aria-live": "polite", "calcite-hydrated-hidden": hidden, id: this.getId(), role: "dialog" }, h("div", { class: {
        [FloatingCSS.animation]: true,
        [FloatingCSS.animationActive]: displayed
      }, ref: this.setTransitionEl }, arrowNode, h("div", { class: {
        [CSS.hasHeader]: !!heading,
        [CSS.container]: true
      } }, this.renderHeader(), h("div", { class: CSS.content }, h("slot", null)), !heading ? this.renderCloseButton() : null))));
  }
  static get is() { return "calcite-popover"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["popover.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["popover.css"]
    };
  }
  static get properties() {
    return {
      "autoClose": {
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
          "text": "When `true`, clicking outside of the component automatically closes open `calcite-popover`s."
        },
        "attribute": "auto-close",
        "reflect": true,
        "defaultValue": "false"
      },
      "closeButton": {
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
          "tags": [{
              "name": "deprecated",
              "text": "use dismissible instead."
            }],
          "text": "When `true`, a close button is added to the component."
        },
        "attribute": "close-button",
        "reflect": true,
        "defaultValue": "false"
      },
      "dismissible": {
        "type": "boolean",
        "mutable": true,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `closable` instead."
            }],
          "text": "When `true`, a close button is added to the component."
        },
        "attribute": "dismissible",
        "reflect": true,
        "defaultValue": "false"
      },
      "closable": {
        "type": "boolean",
        "mutable": true,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, display a close button within the component."
        },
        "attribute": "closable",
        "reflect": true,
        "defaultValue": "false"
      },
      "disableFlip": {
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
          "text": "When `true`, prevents flipping the component's placement when overlapping its `referenceElement`."
        },
        "attribute": "disable-flip",
        "reflect": true,
        "defaultValue": "false"
      },
      "disablePointer": {
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
          "text": "When `true`, removes the caret pointer."
        },
        "attribute": "disable-pointer",
        "reflect": true,
        "defaultValue": "false"
      },
      "flipPlacements": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "EffectivePlacement[]",
          "resolved": "Placement[]",
          "references": {
            "EffectivePlacement": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Defines the available placements that can be used when a flip occurs."
        }
      },
      "heading": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "The component header text."
        },
        "attribute": "heading",
        "reflect": false
      },
      "headingLevel": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "HeadingLevel",
          "resolved": "1 | 2 | 3 | 4 | 5 | 6",
          "references": {
            "HeadingLevel": {
              "location": "import",
              "path": "../functional/Heading"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the number at which section headings should start."
        },
        "attribute": "heading-level",
        "reflect": true
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
          "text": "Offsets the position of the component away from the `referenceElement`."
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
          "text": "Offsets the position of the component along the `referenceElement`."
        },
        "attribute": "offset-skidding",
        "reflect": true,
        "defaultValue": "0"
      },
      "open": {
        "type": "boolean",
        "mutable": true,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, displays and positions the component."
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
          "text": "Determines the type of positioning to use for the overlaid content.\n\nUsing `\"absolute\"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.\n\n`\"fixed\"` value should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `\"fixed\"`."
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
        "defaultValue": "defaultPopoverPlacement"
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
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The `referenceElement` used to position the component according to its `placement` value. Setting to an `HTMLElement` is preferred so the component does not need to query the DOM. However, a string `id` of the reference element can also be used."
        },
        "attribute": "reference-element",
        "reflect": false
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
          "text": "Specifies the size of the component."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "triggerDisabled": {
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
          "text": "When `true`, disables automatically toggling the component when its `referenceElement` has been triggered.\n\nThis property can be set to `true` to manage when the component is open."
        },
        "attribute": "trigger-disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "intlClose": {
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
          "tags": [{
              "name": "default",
              "text": "\"Close\""
            }],
          "text": "Accessible name for the component's close button."
        },
        "attribute": "intl-close",
        "reflect": false,
        "defaultValue": "TEXT.close"
      }
    };
  }
  static get states() {
    return {
      "effectiveReferenceElement": {}
    };
  }
  static get events() {
    return [{
        "method": "calcitePopoverBeforeClose",
        "name": "calcitePopoverBeforeClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is requested to be closed and before the closing transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calcitePopoverClose",
        "name": "calcitePopoverClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is closed and animation is complete."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calcitePopoverBeforeOpen",
        "name": "calcitePopoverBeforeOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is added to the DOM but not rendered, and before the opening transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calcitePopoverOpen",
        "name": "calcitePopoverOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is open and animation is complete."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }];
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
      },
      "setFocus": {
        "complexType": {
          "signature": "(focusId?: \"close-button\") => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "focusId"
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
          "text": "Sets focus on the component.",
          "tags": [{
              "name": "param",
              "text": "focusId"
            }]
        }
      },
      "toggle": {
        "complexType": {
          "signature": "(value?: boolean) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "value"
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
          "text": "Toggles the component's open property.",
          "tags": [{
              "name": "param",
              "text": "value"
            }]
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "dismissible",
        "methodName": "handleDismissible"
      }, {
        "propName": "closable",
        "methodName": "handleClosable"
      }, {
        "propName": "flipPlacements",
        "methodName": "flipPlacementsHandler"
      }, {
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
