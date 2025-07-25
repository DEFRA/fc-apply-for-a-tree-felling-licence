/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { getElementDir, getElementProp, getSlotted, toAriaBoolean } from "../../utils/dom";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
import { CSS_UTILITY } from "../../utils/resources";
import { SLOTS, CSS } from "./resources";
/**
 * @slot - A slot for adding custom content, including nested `calcite-accordion-item`s.
 */
export class AccordionItem {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, the component is active.
     *
     * @deprecated use `expanded` instead.
     */
    this.active = false;
    /** When `true`, the component is expanded. */
    this.expanded = false;
    /** what icon position does the parent accordion specify */
    this.iconPosition = "end";
    /** handle clicks on item header */
    this.itemHeaderClickHandler = () => this.emitRequestedItem();
  }
  activeHandler(value) {
    this.expanded = value;
  }
  expandedHandler(value) {
    this.active = value;
  }
  iconHandler(value) {
    this.iconStart = value;
  }
  iconStartHandler(value) {
    this.icon = value;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.parent = this.el.parentElement;
    this.selectionMode = getElementProp(this.el, "selection-mode", "multi");
    this.iconType = getElementProp(this.el, "icon-type", "chevron");
    this.iconPosition = getElementProp(this.el, "icon-position", this.iconPosition);
    const isExpanded = this.active || this.expanded;
    if (isExpanded) {
      this.activeHandler(isExpanded);
      this.expandedHandler(isExpanded);
    }
    if (this.iconStart) {
      this.icon = this.iconStart;
    }
    else if (this.icon) {
      this.iconStart = this.icon;
    }
    connectConditionalSlotComponent(this);
  }
  componentDidLoad() {
    this.itemPosition = this.getItemPosition();
    this.calciteInternalAccordionItemRegister.emit({
      parent: this.parent,
      position: this.itemPosition
    });
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderActionsStart() {
    const { el } = this;
    return getSlotted(el, SLOTS.actionsStart) ? (h("div", { class: CSS.actionsStart }, h("slot", { name: SLOTS.actionsStart }))) : null;
  }
  renderActionsEnd() {
    const { el } = this;
    return getSlotted(el, SLOTS.actionsEnd) ? (h("div", { class: CSS.actionsEnd }, h("slot", { name: SLOTS.actionsEnd }))) : null;
  }
  render() {
    const dir = getElementDir(this.el);
    const iconStartEl = this.iconStart ? (h("calcite-icon", { class: CSS.iconStart, icon: this.iconStart, key: "icon-start", scale: "s" })) : null;
    const iconEndEl = this.iconEnd ? (h("calcite-icon", { class: CSS.iconEnd, icon: this.iconEnd, key: "icon-end", scale: "s" })) : null;
    const description = this.description || this.itemSubtitle;
    return (h(Host, null, h("div", { class: {
        [`icon-position--${this.iconPosition}`]: true,
        [`icon-type--${this.iconType}`]: true
      } }, h("div", { class: { [CSS.header]: true, [CSS_UTILITY.rtl]: dir === "rtl" } }, this.renderActionsStart(), h("div", { "aria-expanded": toAriaBoolean(this.active || this.expanded), class: CSS.headerContent, onClick: this.itemHeaderClickHandler, role: "button", tabindex: "0" }, h("div", { class: CSS.headerContainer }, iconStartEl, h("div", { class: CSS.headerText }, h("span", { class: CSS.heading }, this.heading || this.itemTitle), description ? h("span", { class: CSS.description }, description) : null), iconEndEl), h("calcite-icon", { class: CSS.expandIcon, icon: this.iconType === "chevron"
        ? "chevronDown"
        : this.iconType === "caret"
          ? "caretDown"
          : this.expanded || this.active
            ? "minus"
            : "plus", scale: "s" })), this.renderActionsEnd()), h("div", { class: CSS.content }, h("slot", null)))));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  keyDownHandler(event) {
    if (event.target === this.el) {
      switch (event.key) {
        case " ":
        case "Enter":
          this.emitRequestedItem();
          event.preventDefault();
          break;
        case "ArrowUp":
        case "ArrowDown":
        case "Home":
        case "End":
          this.calciteInternalAccordionItemKeyEvent.emit({
            parent: this.parent,
            item: event
          });
          event.preventDefault();
          break;
      }
    }
  }
  updateActiveItemOnChange(event) {
    this.requestedAccordionItem = event.detail
      .requestedAccordionItem;
    if (this.el.parentNode !== this.requestedAccordionItem.parentNode) {
      return;
    }
    this.determineActiveItem();
    event.stopPropagation();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  determineActiveItem() {
    switch (this.selectionMode) {
      case "multi":
      case "multiple":
        if (this.el === this.requestedAccordionItem) {
          this.expanded = !this.expanded;
        }
        break;
      case "single":
        this.expanded = this.el === this.requestedAccordionItem ? !this.expanded : false;
        break;
      case "single-persist":
        this.expanded = this.el === this.requestedAccordionItem;
        break;
    }
  }
  emitRequestedItem() {
    this.calciteInternalAccordionItemSelect.emit({
      requestedAccordionItem: this.el
    });
  }
  getItemPosition() {
    return Array.prototype.indexOf.call(this.parent.querySelectorAll("calcite-accordion-item"), this.el);
  }
  static get is() { return "calcite-accordion-item"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["accordion-item.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["accordion-item.css"]
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
              "text": "use `expanded` instead."
            }],
          "text": "When `true`, the component is active."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "expanded": {
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
          "text": "When `true`, the component is expanded."
        },
        "attribute": "expanded",
        "reflect": true,
        "defaultValue": "false"
      },
      "itemTitle": {
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
          "tags": [{
              "name": "deprecated",
              "text": "Use `heading` instead."
            }],
          "text": "Specifies a title for the component."
        },
        "attribute": "item-title",
        "reflect": false
      },
      "itemSubtitle": {
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
          "tags": [{
              "name": "deprecated",
              "text": "Use `description` instead."
            }],
          "text": "Specifies a subtitle for the component."
        },
        "attribute": "item-subtitle",
        "reflect": false
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
          "text": "Specifies heading text for the component."
        },
        "attribute": "heading",
        "reflect": false
      },
      "description": {
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
          "text": "Specifies a description for the component."
        },
        "attribute": "description",
        "reflect": false
      },
      "icon": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `iconStart` or `iconEnd` instead."
            }],
          "text": "Specifies an icon to display."
        },
        "attribute": "icon",
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
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteInternalAccordionItemKeyEvent",
        "name": "calciteInternalAccordionItemKeyEvent",
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
          "original": "ItemKeyEvent",
          "resolved": "ItemKeyEvent",
          "references": {
            "ItemKeyEvent": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalAccordionItemSelect",
        "name": "calciteInternalAccordionItemSelect",
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
              "path": "./interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalAccordionItemClose",
        "name": "calciteInternalAccordionItemClose",
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
      }, {
        "method": "calciteInternalAccordionItemRegister",
        "name": "calciteInternalAccordionItemRegister",
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
          "original": "RegistryEntry",
          "resolved": "RegistryEntry",
          "references": {
            "RegistryEntry": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        }
      }];
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "active",
        "methodName": "activeHandler"
      }, {
        "propName": "expanded",
        "methodName": "expandedHandler"
      }, {
        "propName": "icon",
        "methodName": "iconHandler"
      }, {
        "propName": "iconStart",
        "methodName": "iconStartHandler"
      }];
  }
  static get listeners() {
    return [{
        "name": "keydown",
        "method": "keyDownHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalAccordionChange",
        "method": "updateActiveItemOnChange",
        "target": "body",
        "capture": false,
        "passive": false
      }];
  }
}
