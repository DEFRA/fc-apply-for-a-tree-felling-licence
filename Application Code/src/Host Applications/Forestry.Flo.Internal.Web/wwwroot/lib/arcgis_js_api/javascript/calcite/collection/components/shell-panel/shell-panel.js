/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, forceUpdate } from "@stencil/core";
import { CSS, SLOTS, TEXT } from "./resources";
import { getSlotted, getElementDir, isPrimaryPointerButton } from "../../utils/dom";
import { clamp } from "../../utils/math";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding content to the component.
 * @slot action-bar - A slot for adding a `calcite-action-bar` to the component.
 */
export class ShellPanel {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, hides the component's content area.
     */
    this.collapsed = false;
    /**
     * When `true`, the content area displays like a floating panel.
     */
    this.detached = false;
    /**
     * When `detached`, specifies the maximum height of the component.
     */
    this.detachedHeightScale = "l";
    /**
     * Specifies the width of the component's content area.
     */
    this.widthScale = "m";
    /**
     * Accessible name for the resize separator.
     *
     * @default "Resize"
     */
    this.intlResize = TEXT.resize;
    /**
     * When `true` and not `detached`, the component's content area is resizable.
     */
    this.resizable = false;
    this.contentWidth = null;
    this.initialContentWidth = null;
    this.initialClientX = null;
    this.contentWidthMax = null;
    this.contentWidthMin = null;
    this.step = 1;
    this.stepMultiplier = 10;
    this.storeContentEl = (contentEl) => {
      this.contentEl = contentEl;
    };
    this.getKeyAdjustedWidth = (event) => {
      const { key } = event;
      const { el, step, stepMultiplier, contentWidthMin, contentWidthMax, initialContentWidth, position } = this;
      const multipliedStep = step * stepMultiplier;
      const MOVEMENT_KEYS = [
        "ArrowUp",
        "ArrowDown",
        "ArrowLeft",
        "ArrowRight",
        "Home",
        "End",
        "PageUp",
        "PageDown"
      ];
      if (MOVEMENT_KEYS.indexOf(key) > -1) {
        event.preventDefault();
      }
      const dir = getElementDir(el);
      const directionKeys = ["ArrowLeft", "ArrowRight"];
      const directionFactor = dir === "rtl" && directionKeys.includes(key) ? -1 : 1;
      const increaseKeys = key === "ArrowUp" ||
        (position === "end" ? key === directionKeys[0] : key === directionKeys[1]);
      if (increaseKeys) {
        const stepValue = event.shiftKey ? multipliedStep : step;
        return initialContentWidth + directionFactor * stepValue;
      }
      const decreaseKeys = key === "ArrowDown" ||
        (position === "end" ? key === directionKeys[1] : key === directionKeys[0]);
      if (decreaseKeys) {
        const stepValue = event.shiftKey ? multipliedStep : step;
        return initialContentWidth - directionFactor * stepValue;
      }
      if (typeof contentWidthMin === "number" && key === "Home") {
        return contentWidthMin;
      }
      if (typeof contentWidthMax === "number" && key === "End") {
        return contentWidthMax;
      }
      if (key === "PageDown") {
        return initialContentWidth - multipliedStep;
      }
      if (key === "PageUp") {
        return initialContentWidth + multipliedStep;
      }
      return null;
    };
    this.separatorKeyDown = (event) => {
      this.setInitialContentWidth();
      const width = this.getKeyAdjustedWidth(event);
      if (typeof width === "number") {
        this.setContentWidth(width);
      }
    };
    this.separatorPointerMove = (event) => {
      event.preventDefault();
      const { el, initialContentWidth, position, initialClientX } = this;
      const offset = event.clientX - initialClientX;
      const dir = getElementDir(el);
      const adjustmentDirection = dir === "rtl" ? -1 : 1;
      const adjustedOffset = position === "end" ? -adjustmentDirection * offset : adjustmentDirection * offset;
      const width = initialContentWidth + adjustedOffset;
      this.setContentWidth(width);
    };
    this.separatorPointerUp = (event) => {
      if (!isPrimaryPointerButton(event)) {
        return;
      }
      event.preventDefault();
      document.removeEventListener("pointerup", this.separatorPointerUp);
      document.removeEventListener("pointermove", this.separatorPointerMove);
    };
    this.setInitialContentWidth = () => {
      var _a;
      this.initialContentWidth = (_a = this.contentEl) === null || _a === void 0 ? void 0 : _a.getBoundingClientRect().width;
    };
    this.separatorPointerDown = (event) => {
      if (!isPrimaryPointerButton(event)) {
        return;
      }
      event.preventDefault();
      const { separatorEl } = this;
      separatorEl && document.activeElement !== separatorEl && separatorEl.focus();
      this.setInitialContentWidth();
      this.initialClientX = event.clientX;
      document.addEventListener("pointerup", this.separatorPointerUp);
      document.addEventListener("pointermove", this.separatorPointerMove);
    };
    this.connectSeparator = (separatorEl) => {
      this.disconnectSeparator();
      this.separatorEl = separatorEl;
      separatorEl.addEventListener("pointerdown", this.separatorPointerDown);
    };
    this.disconnectSeparator = () => {
      var _a;
      (_a = this.separatorEl) === null || _a === void 0 ? void 0 : _a.removeEventListener("pointerdown", this.separatorPointerDown);
    };
  }
  watchHandler() {
    this.calciteShellPanelToggle.emit();
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
    this.disconnectSeparator();
  }
  componentDidLoad() {
    this.updateAriaValues();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderHeader() {
    const { el } = this;
    const hasHeader = getSlotted(el, SLOTS.header);
    return hasHeader ? (h("div", { class: CSS.contentHeader, key: "header" }, h("slot", { name: SLOTS.header }))) : null;
  }
  render() {
    const { collapsed, detached, position, initialContentWidth, contentWidth, contentWidthMax, contentWidthMin, intlResize, resizable } = this;
    const allowResizing = !detached && resizable;
    const contentNode = (h("div", { class: { [CSS.content]: true, [CSS.contentDetached]: detached }, hidden: collapsed, key: "content", ref: this.storeContentEl, style: allowResizing && contentWidth ? { width: `${contentWidth}px` } : null }, this.renderHeader(), h("div", { class: CSS.contentBody }, h("slot", null))));
    const separatorNode = allowResizing ? (h("div", { "aria-label": intlResize, "aria-orientation": "horizontal", "aria-valuemax": contentWidthMax, "aria-valuemin": contentWidthMin, "aria-valuenow": contentWidth !== null && contentWidth !== void 0 ? contentWidth : initialContentWidth, class: CSS.separator, key: "separator", onKeyDown: this.separatorKeyDown, ref: this.connectSeparator, role: "separator", tabIndex: 0, "touch-action": "none" })) : null;
    const actionBarNode = h("slot", { key: "action-bar", name: SLOTS.actionBar });
    const mainNodes = [actionBarNode, contentNode, separatorNode];
    if (position === "end") {
      mainNodes.reverse();
    }
    return h("div", { class: { [CSS.container]: true } }, mainNodes);
  }
  // --------------------------------------------------------------------------
  //
  //  private Methods
  //
  // --------------------------------------------------------------------------
  setContentWidth(width) {
    const { contentWidthMax, contentWidthMin } = this;
    const roundedWidth = Math.round(width);
    this.contentWidth =
      typeof contentWidthMax === "number" && typeof contentWidthMin === "number"
        ? clamp(roundedWidth, contentWidthMin, contentWidthMax)
        : roundedWidth;
  }
  updateAriaValues() {
    const { contentEl } = this;
    const computedStyle = contentEl && getComputedStyle(contentEl);
    if (!computedStyle) {
      return;
    }
    const max = parseInt(computedStyle.getPropertyValue("max-width"), 10);
    const min = parseInt(computedStyle.getPropertyValue("min-width"), 10);
    const valueNow = parseInt(computedStyle.getPropertyValue("width"), 10);
    if (typeof valueNow === "number" && !isNaN(valueNow)) {
      this.initialContentWidth = valueNow;
    }
    if (typeof max === "number" && !isNaN(max)) {
      this.contentWidthMax = max;
    }
    if (typeof min === "number" && !isNaN(min)) {
      this.contentWidthMin = min;
    }
    forceUpdate(this);
  }
  static get is() { return "calcite-shell-panel"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["shell-panel.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["shell-panel.css"]
    };
  }
  static get properties() {
    return {
      "collapsed": {
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
          "text": "When `true`, hides the component's content area."
        },
        "attribute": "collapsed",
        "reflect": true,
        "defaultValue": "false"
      },
      "detached": {
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
          "text": "When `true`, the content area displays like a floating panel."
        },
        "attribute": "detached",
        "reflect": true,
        "defaultValue": "false"
      },
      "detachedHeightScale": {
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
          "text": "When `detached`, specifies the maximum height of the component."
        },
        "attribute": "detached-height-scale",
        "reflect": true,
        "defaultValue": "\"l\""
      },
      "widthScale": {
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
          "text": "Specifies the width of the component's content area."
        },
        "attribute": "width-scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "position": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Position",
          "resolved": "\"end\" | \"start\"",
          "references": {
            "Position": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the component's position. Will be flipped when the element direction is right-to-left (`\"rtl\"`)."
        },
        "attribute": "position",
        "reflect": true
      },
      "intlResize": {
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
              "text": "\"Resize\""
            }],
          "text": "Accessible name for the resize separator."
        },
        "attribute": "intl-resize",
        "reflect": false,
        "defaultValue": "TEXT.resize"
      },
      "resizable": {
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
          "text": "When `true` and not `detached`, the component's content area is resizable."
        },
        "attribute": "resizable",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get states() {
    return {
      "contentWidth": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteShellPanelToggle",
        "name": "calciteShellPanelToggle",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use a `ResizeObserver` on the component to listen for changes to its size."
            }],
          "text": "Emitted when collapse has been toggled."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }];
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "collapsed",
        "methodName": "watchHandler"
      }];
  }
}
