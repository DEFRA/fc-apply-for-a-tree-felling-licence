/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { getElementProp, toAriaBoolean } from "../../utils/dom";
import { CSS } from "./resources";
/**
 * @slot - A slot for adding text.
 */
export class DropdownItem {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * Indicates whether the item is active.
     *
     * @deprecated Use selected instead.
     */
    this.active = false;
    /** When true, item is selected  */
    this.selected = false;
  }
  activeHandler(value) {
    this.selected = value;
  }
  selectedHandler(value) {
    this.active = value;
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.el) === null || _a === void 0 ? void 0 : _a.focus();
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentWillLoad() {
    this.initialize();
  }
  connectedCallback() {
    const isSelected = this.selected || this.active;
    if (isSelected) {
      this.activeHandler(isSelected);
      this.selectedHandler(isSelected);
    }
    this.initialize();
  }
  render() {
    const scale = getElementProp(this.el, "scale", "m");
    const iconStartEl = (h("calcite-icon", { class: "dropdown-item-icon-start", flipRtl: this.iconFlipRtl === "start" || this.iconFlipRtl === "both", icon: this.iconStart, scale: "s" }));
    const contentNode = (h("span", { class: "dropdown-item-content" }, h("slot", null)));
    const iconEndEl = (h("calcite-icon", { class: "dropdown-item-icon-end", flipRtl: this.iconFlipRtl === "end" || this.iconFlipRtl === "both", icon: this.iconEnd, scale: "s" }));
    const slottedContent = this.iconStart && this.iconEnd
      ? [iconStartEl, contentNode, iconEndEl]
      : this.iconStart
        ? [iconStartEl, h("slot", null)]
        : this.iconEnd
          ? [contentNode, iconEndEl]
          : contentNode;
    const contentEl = !this.href ? (slottedContent) : (h("a", { "aria-label": this.label, class: "dropdown-link", href: this.href, ref: (el) => (this.childLink = el), rel: this.rel, target: this.target }, slottedContent));
    const itemRole = this.href
      ? null
      : this.selectionMode === "single"
        ? "menuitemradio"
        : this.selectionMode === "multiple" || this.selectionMode === "multi"
          ? "menuitemcheckbox"
          : "menuitem";
    const itemAria = this.selectionMode !== "none" ? toAriaBoolean(this.selected) : null;
    return (h(Host, { "aria-checked": itemAria, role: itemRole, tabindex: "0" }, h("div", { class: {
        container: true,
        [CSS.containerLink]: !!this.href,
        [CSS.containerSmall]: scale === "s",
        [CSS.containerMedium]: scale === "m",
        [CSS.containerLarge]: scale === "l",
        [CSS.containerMulti]: this.selectionMode === "multiple" || this.selectionMode === "multi",
        [CSS.containerSingle]: this.selectionMode === "single",
        [CSS.containerNone]: this.selectionMode === "none"
      } }, this.selectionMode !== "none" ? (h("calcite-icon", { class: "dropdown-item-icon", icon: this.selectionMode === "multiple" || this.selectionMode === "multi"
        ? "check"
        : "bullet-point", scale: "s" })) : null, contentEl)));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  onClick() {
    this.emitRequestedItem();
  }
  keyDownHandler(event) {
    switch (event.key) {
      case " ":
      case "Enter":
        this.emitRequestedItem();
        if (this.href) {
          this.childLink.click();
        }
        event.preventDefault();
        break;
      case "Escape":
        this.calciteInternalDropdownCloseRequest.emit();
        event.preventDefault();
        break;
      case "Tab":
        this.calciteInternalDropdownItemKeyEvent.emit({ keyboardEvent: event });
        break;
      case "ArrowUp":
      case "ArrowDown":
      case "Home":
      case "End":
        event.preventDefault();
        this.calciteInternalDropdownItemKeyEvent.emit({ keyboardEvent: event });
        break;
    }
  }
  updateActiveItemOnChange(event) {
    const parentEmittedChange = event.composedPath().includes(this.parentDropdownGroupEl);
    if (parentEmittedChange) {
      this.requestedDropdownGroup = event.detail.requestedDropdownGroup;
      this.requestedDropdownItem = event.detail.requestedDropdownItem;
      this.determineActiveItem();
    }
    event.stopPropagation();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  initialize() {
    this.selectionMode = getElementProp(this.el, "selection-mode", "single");
    this.parentDropdownGroupEl = this.el.closest("calcite-dropdown-group");
    if (this.selectionMode === "none") {
      this.selected = false;
    }
  }
  determineActiveItem() {
    switch (this.selectionMode) {
      case "multi":
      case "multiple":
        if (this.el === this.requestedDropdownItem) {
          this.selected = !this.selected;
        }
        break;
      case "single":
        if (this.el === this.requestedDropdownItem) {
          this.selected = true;
        }
        else if (this.requestedDropdownGroup === this.parentDropdownGroupEl) {
          this.selected = false;
        }
        break;
      case "none":
        this.selected = false;
        break;
    }
  }
  emitRequestedItem() {
    this.calciteInternalDropdownItemSelect.emit({
      requestedDropdownItem: this.el,
      requestedDropdownGroup: this.parentDropdownGroupEl
    });
  }
  static get is() { return "calcite-dropdown-item"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["dropdown-item.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["dropdown-item.css"]
    };
  }
  static get properties() {
    return {
      "active": {
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
              "text": "Use selected instead."
            }],
          "text": "Indicates whether the item is active."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "selected": {
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
          "text": "When true, item is selected"
        },
        "attribute": "selected",
        "reflect": true,
        "defaultValue": "false"
      },
      "iconFlipRtl": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "FlipContext",
          "resolved": "\"both\" | \"end\" | \"start\"",
          "references": {
            "FlipContext": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "When true, the icon will be flipped when the element direction is right-to-left (`\"rtl\"`)."
        },
        "attribute": "icon-flip-rtl",
        "reflect": true
      },
      "iconStart": {
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
          "text": "Specifies an icon to display at the start of the component."
        },
        "attribute": "icon-start",
        "reflect": true
      },
      "iconEnd": {
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
          "text": "Specifies an icon to display at the end of the component."
        },
        "attribute": "icon-end",
        "reflect": true
      },
      "href": {
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
          "text": "optionally pass a href - used to determine if the component should render as anchor"
        },
        "attribute": "href",
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
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Applies to the aria-label attribute on the button or hyperlink"
        },
        "attribute": "label",
        "reflect": false
      },
      "rel": {
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
          "text": "The rel attribute to apply to the hyperlink"
        },
        "attribute": "rel",
        "reflect": true
      },
      "target": {
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
          "text": "The target attribute to apply to the hyperlink"
        },
        "attribute": "target",
        "reflect": true
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteInternalDropdownItemSelect",
        "name": "calciteInternalDropdownItemSelect",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "complexType": {
          "original": "RequestedItem",
          "resolved": "RequestedItem",
          "references": {
            "RequestedItem": {
              "location": "import",
              "path": "../dropdown-group/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalDropdownItemKeyEvent",
        "name": "calciteInternalDropdownItemKeyEvent",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "complexType": {
          "original": "ItemKeyboardEvent",
          "resolved": "ItemKeyboardEvent",
          "references": {
            "ItemKeyboardEvent": {
              "location": "import",
              "path": "../dropdown/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalDropdownCloseRequest",
        "name": "calciteInternalDropdownCloseRequest",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
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
      "setFocus": {
        "complexType": {
          "signature": "() => Promise<void>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Sets focus on the component.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "active",
        "methodName": "activeHandler"
      }, {
        "propName": "selected",
        "methodName": "selectedHandler"
      }];
  }
  static get listeners() {
    return [{
        "name": "click",
        "method": "onClick",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "keydown",
        "method": "keyDownHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalDropdownItemChange",
        "method": "updateActiveItemOnChange",
        "target": "body",
        "capture": false,
        "passive": false
      }];
  }
}
