/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
/**
 * @slot - A slot for adding `calcite-accordion-item`s. `calcite-accordion` cannot be nested, however `calcite-accordion-item`s can.
 */
export class Accordion {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the appearance of the component. */
    this.appearance = "solid";
    /** Specifies the placement of the icon in the header. */
    this.iconPosition = "end";
    /** Specifies the type of the icon in the header. */
    this.iconType = "chevron";
    /** Specifies the size of the component. */
    this.scale = "m";
    /**
     * Specifies the selection mode - "multiple" (allow any number of open items), "single" (allow one open item),
     * or "single-persist" (allow and require one open item).
     */
    this.selectionMode = "multi";
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    /** created list of Accordion items */
    this.items = [];
    /** keep track of whether the items have been sorted so we don't re-sort */
    this.sorted = false;
    this.sortItems = (items) => items.sort((a, b) => a.position - b.position).map((a) => a.item);
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidLoad() {
    if (!this.sorted) {
      this.items = this.sortItems(this.items);
      this.sorted = true;
    }
  }
  render() {
    const transparent = this.appearance === "transparent";
    const minimal = this.appearance === "minimal";
    return (h("div", { class: {
        "accordion--transparent": transparent,
        "accordion--minimal": minimal,
        accordion: !transparent && !minimal
      } }, h("slot", null)));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  calciteInternalAccordionItemKeyEvent(event) {
    const item = event.detail.item;
    const parent = event.detail.parent;
    if (this.el === parent) {
      const { key } = item;
      const itemToFocus = event.target;
      const isFirstItem = this.itemIndex(itemToFocus) === 0;
      const isLastItem = this.itemIndex(itemToFocus) === this.items.length - 1;
      switch (key) {
        case "ArrowDown":
          if (isLastItem) {
            this.focusFirstItem();
          }
          else {
            this.focusNextItem(itemToFocus);
          }
          break;
        case "ArrowUp":
          if (isFirstItem) {
            this.focusLastItem();
          }
          else {
            this.focusPrevItem(itemToFocus);
          }
          break;
        case "Home":
          this.focusFirstItem();
          break;
        case "End":
          this.focusLastItem();
          break;
      }
    }
    event.stopPropagation();
  }
  registerCalciteAccordionItem(event) {
    const item = {
      item: event.target,
      parent: event.detail.parent,
      position: event.detail.position
    };
    if (this.el === item.parent) {
      this.items.push(item);
    }
    event.stopPropagation();
  }
  updateActiveItemOnChange(event) {
    this.requestedAccordionItem = event.detail.requestedAccordionItem;
    this.calciteInternalAccordionChange.emit({
      requestedAccordionItem: this.requestedAccordionItem
    });
    event.stopPropagation();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  focusFirstItem() {
    const firstItem = this.items[0];
    this.focusElement(firstItem);
  }
  focusLastItem() {
    const lastItem = this.items[this.items.length - 1];
    this.focusElement(lastItem);
  }
  focusNextItem(el) {
    const index = this.itemIndex(el);
    const nextItem = this.items[index + 1] || this.items[0];
    this.focusElement(nextItem);
  }
  focusPrevItem(el) {
    const index = this.itemIndex(el);
    const prevItem = this.items[index - 1] || this.items[this.items.length - 1];
    this.focusElement(prevItem);
  }
  itemIndex(el) {
    return this.items.indexOf(el);
  }
  focusElement(item) {
    const target = item;
    target === null || target === void 0 ? void 0 : target.focus();
  }
  static get is() { return "calcite-accordion"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["accordion.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["accordion.css"]
    };
  }
  static get properties() {
    return {
      "appearance": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "AccordionAppearance",
          "resolved": "\"default\" | \"minimal\" | \"solid\" | \"transparent\"",
          "references": {
            "AccordionAppearance": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the appearance of the component."
        },
        "attribute": "appearance",
        "reflect": true,
        "defaultValue": "\"solid\""
      },
      "iconPosition": {
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
          "text": "Specifies the placement of the icon in the header."
        },
        "attribute": "icon-position",
        "reflect": true,
        "defaultValue": "\"end\""
      },
      "iconType": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "\"chevron\" | \"caret\" | \"plus-minus\"",
          "resolved": "\"caret\" | \"chevron\" | \"plus-minus\"",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the type of the icon in the header."
        },
        "attribute": "icon-type",
        "reflect": true,
        "defaultValue": "\"chevron\""
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
      "selectionMode": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "AccordionSelectionMode",
          "resolved": "\"multi\" | \"multiple\" | \"single\" | \"single-persist\"",
          "references": {
            "AccordionSelectionMode": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the selection mode - \"multiple\" (allow any number of open items), \"single\" (allow one open item),\nor \"single-persist\" (allow and require one open item)."
        },
        "attribute": "selection-mode",
        "reflect": true,
        "defaultValue": "\"multi\""
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteInternalAccordionChange",
        "name": "calciteInternalAccordionChange",
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
      }];
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "calciteInternalAccordionItemKeyEvent",
        "method": "calciteInternalAccordionItemKeyEvent",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalAccordionItemRegister",
        "method": "registerCalciteAccordionItem",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalAccordionItemSelect",
        "method": "updateActiveItemOnChange",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
