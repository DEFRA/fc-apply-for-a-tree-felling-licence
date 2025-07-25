/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { focusElement, isPrimaryPointerButton, toAriaBoolean } from "../../utils/dom";
import { FloatingCSS, connectFloatingUI, disconnectFloatingUI, defaultMenuPlacement, filterComputedPlacements, reposition, updateAfterClose } from "../../utils/floating-ui";
import { SLOTS } from "./resources";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
import { connectOpenCloseComponent, disconnectOpenCloseComponent } from "../../utils/openCloseComponent";
import { guid } from "../../utils/guid";
import { isActivationKey } from "../../utils/key";
/**
 * @slot - A slot for adding `calcite-dropdown-group` components. Every `calcite-dropdown-item` must have a parent `calcite-dropdown-group`, even if the `groupTitle` property is not set.
 * @slot dropdown-trigger - A slot for the element that triggers the `calcite-dropdown`.
 */
export class Dropdown {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * Opens or closes the dropdown
     *
     * @deprecated use open instead.
     */
    this.active = false;
    /** When true, opens the dropdown */
    this.open = false;
    /**
     allow the dropdown to remain open after a selection is made
     if the selection-mode of the selected item's containing group is "none", the dropdown will always close
     */
    this.disableCloseOnSelect = false;
    /** is the dropdown disabled  */
    this.disabled = false;
    /**
     specify the maximum number of calcite-dropdown-items to display before showing the scroller, must be greater than 0 -
     this value does not include groupTitles passed to calcite-dropdown-group
     */
    this.maxItems = 0;
    /**
     * Determines the type of positioning to use for the overlaid content.
     *
     * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
     *
     * `"fixed"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
     *
     */
    this.overlayPositioning = "absolute";
    /**
     * Determines where the dropdown will be positioned relative to the button.
     *
     * @default "bottom-start"
     */
    this.placement = defaultMenuPlacement;
    /** specify the scale of dropdown, defaults to m */
    this.scale = "m";
    /**
     * **read-only** The currently selected items
     *
     * @readonly
     */
    this.selectedItems = [];
    /** specify whether the dropdown is opened by hover or click of a trigger element */
    this.type = "click";
    this.items = [];
    this.groups = [];
    this.mutationObserver = createObserver("mutation", () => this.updateItems());
    this.resizeObserver = createObserver("resize", (entries) => this.resizeObserverCallback(entries));
    this.openTransitionProp = "opacity";
    this.guid = `calcite-dropdown-${guid()}`;
    this.defaultAssignedElements = [];
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.slotChangeHandler = (event) => {
      this.defaultAssignedElements = event.target.assignedElements({
        flatten: true
      });
      this.updateItems();
    };
    this.setFilteredPlacements = () => {
      const { el, flipPlacements } = this;
      this.filteredFlipPlacements = flipPlacements
        ? filterComputedPlacements(flipPlacements, el)
        : null;
    };
    this.updateTriggers = (event) => {
      this.triggers = event.target.assignedElements({
        flatten: true
      });
      this.reposition(true);
    };
    this.updateItems = () => {
      this.items = this.groups
        .map((group) => Array.from(group === null || group === void 0 ? void 0 : group.querySelectorAll("calcite-dropdown-item")))
        .reduce((previousValue, currentValue) => [...previousValue, ...currentValue], []);
      this.updateSelectedItems();
      this.reposition(true);
    };
    this.updateGroups = (event) => {
      const groups = event.target
        .assignedElements({ flatten: true })
        .filter((el) => el === null || el === void 0 ? void 0 : el.matches("calcite-dropdown-group"));
      this.groups = groups;
      this.updateItems();
    };
    this.resizeObserverCallback = (entries) => {
      entries.forEach((entry) => {
        const { target } = entry;
        if (target === this.referenceEl) {
          this.setDropdownWidth();
        }
        else if (target === this.scrollerEl) {
          this.setMaxScrollerHeight();
        }
      });
    };
    this.setDropdownWidth = () => {
      const { referenceEl, scrollerEl } = this;
      const referenceElWidth = referenceEl === null || referenceEl === void 0 ? void 0 : referenceEl.clientWidth;
      if (!referenceElWidth || !scrollerEl) {
        return;
      }
      scrollerEl.style.minWidth = `${referenceElWidth}px`;
    };
    this.setMaxScrollerHeight = () => {
      const { scrollerEl } = this;
      if (!scrollerEl) {
        return;
      }
      this.reposition(true);
      const maxScrollerHeight = this.getMaxScrollerHeight();
      scrollerEl.style.maxHeight = maxScrollerHeight > 0 ? `${maxScrollerHeight}px` : "";
      this.reposition(true);
    };
    this.setScrollerAndTransitionEl = (el) => {
      this.resizeObserver.observe(el);
      this.scrollerEl = el;
      this.transitionEl = el;
      connectOpenCloseComponent(this);
    };
    this.setReferenceEl = (el) => {
      this.referenceEl = el;
      connectFloatingUI(this, this.referenceEl, this.floatingEl);
      this.resizeObserver.observe(el);
    };
    this.setFloatingEl = (el) => {
      this.floatingEl = el;
      connectFloatingUI(this, this.referenceEl, this.floatingEl);
    };
    this.keyDownHandler = (event) => {
      const target = event.target;
      if (target !== this.referenceEl) {
        return;
      }
      const { defaultPrevented, key } = event;
      if (defaultPrevented) {
        return;
      }
      if (this.open) {
        if (key === "Escape") {
          this.closeCalciteDropdown();
          event.preventDefault();
          return;
        }
        else if (event.shiftKey && key === "Tab") {
          this.closeCalciteDropdown();
          event.preventDefault();
          return;
        }
      }
      if (isActivationKey(key)) {
        this.openCalciteDropdown();
        event.preventDefault();
      }
      else if (key === "Escape") {
        this.closeCalciteDropdown();
        event.preventDefault();
      }
    };
    this.focusOnFirstActiveOrFirstItem = () => {
      this.getFocusableElement(this.items.find((item) => item.selected) || this.items[0]);
    };
    this.toggleOpenEnd = () => {
      this.focusOnFirstActiveOrFirstItem();
      this.el.removeEventListener("calciteDropdownOpen", this.toggleOpenEnd);
    };
    this.openCalciteDropdown = () => {
      this.open = !this.open;
      if (this.open) {
        this.el.addEventListener("calciteDropdownOpen", this.toggleOpenEnd);
      }
    };
  }
  activeHandler(value) {
    this.open = value;
  }
  openHandler(value) {
    if (!this.disabled) {
      if (value) {
        this.reposition(true);
      }
      else {
        updateAfterClose(this.floatingEl);
      }
      this.active = value;
      return;
    }
    if (!value) {
      updateAfterClose(this.floatingEl);
    }
    this.open = false;
  }
  handleDisabledChange(value) {
    if (!value) {
      this.open = false;
    }
  }
  flipPlacementsHandler() {
    this.setFilteredPlacements();
    this.reposition(true);
  }
  maxItemsHandler() {
    this.setMaxScrollerHeight();
  }
  overlayPositioningHandler() {
    this.reposition(true);
  }
  placementHandler() {
    this.reposition(true);
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
    this.setFilteredPlacements();
    this.reposition(true);
    if (this.open) {
      this.openHandler(this.open);
    }
    if (this.active) {
      this.activeHandler(this.active);
    }
    connectOpenCloseComponent(this);
  }
  componentDidLoad() {
    this.reposition(true);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  disconnectedCallback() {
    var _a, _b;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    disconnectFloatingUI(this, this.referenceEl, this.floatingEl);
    (_b = this.resizeObserver) === null || _b === void 0 ? void 0 : _b.disconnect();
    disconnectOpenCloseComponent(this);
  }
  render() {
    const { open, guid } = this;
    return (h(Host, null, h("div", { class: "calcite-dropdown-trigger-container", id: `${guid}-menubutton`, onClick: this.openCalciteDropdown, onKeyDown: this.keyDownHandler, ref: this.setReferenceEl }, h("slot", { "aria-controls": `${guid}-menu`, "aria-expanded": toAriaBoolean(open), "aria-haspopup": "menu", name: SLOTS.dropdownTrigger, onSlotchange: this.updateTriggers })), h("div", { "aria-hidden": toAriaBoolean(!open), class: "calcite-dropdown-wrapper", ref: this.setFloatingEl }, h("div", { "aria-labelledby": `${guid}-menubutton`, class: {
        ["calcite-dropdown-content"]: true,
        [FloatingCSS.animation]: true,
        [FloatingCSS.animationActive]: open
      }, id: `${guid}-menu`, ref: this.setScrollerAndTransitionEl, role: "menu" }, h("slot", { onSlotchange: this.updateGroups })))));
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /**
   * Updates the position of the component.
   *
   * @param delayed
   */
  async reposition(delayed = false) {
    const { floatingEl, referenceEl, placement, overlayPositioning, filteredFlipPlacements } = this;
    return reposition(this, {
      floatingEl,
      referenceEl,
      overlayPositioning,
      placement,
      flipPlacements: filteredFlipPlacements,
      type: "menu"
    }, delayed);
  }
  closeCalciteDropdownOnClick(event) {
    if (!isPrimaryPointerButton(event) || !this.open || event.composedPath().includes(this.el)) {
      return;
    }
    this.closeCalciteDropdown(false);
  }
  closeCalciteDropdownOnEvent(event) {
    this.closeCalciteDropdown();
    event.stopPropagation();
  }
  closeCalciteDropdownOnOpenEvent(event) {
    if (event.composedPath().includes(this.el)) {
      return;
    }
    this.open = false;
  }
  mouseEnterHandler() {
    if (this.type === "hover") {
      this.openCalciteDropdown();
    }
  }
  mouseLeaveHandler() {
    if (this.type === "hover") {
      this.closeCalciteDropdown();
    }
  }
  calciteInternalDropdownItemKeyEvent(event) {
    const { keyboardEvent } = event.detail;
    // handle edge
    const target = keyboardEvent.target;
    const itemToFocus = target.nodeName !== "A" ? target : target.parentNode;
    const isFirstItem = this.itemIndex(itemToFocus) === 0;
    const isLastItem = this.itemIndex(itemToFocus) === this.items.length - 1;
    switch (keyboardEvent.key) {
      case "Tab":
        if (isLastItem && !keyboardEvent.shiftKey) {
          this.closeCalciteDropdown();
        }
        else if (isFirstItem && keyboardEvent.shiftKey) {
          this.closeCalciteDropdown();
        }
        else if (keyboardEvent.shiftKey) {
          this.focusPrevItem(itemToFocus);
        }
        else {
          this.focusNextItem(itemToFocus);
        }
        break;
      case "ArrowDown":
        this.focusNextItem(itemToFocus);
        break;
      case "ArrowUp":
        this.focusPrevItem(itemToFocus);
        break;
      case "Home":
        this.focusFirstItem();
        break;
      case "End":
        this.focusLastItem();
        break;
    }
    event.stopPropagation();
  }
  handleItemSelect(event) {
    this.updateSelectedItems();
    event.stopPropagation();
    this.calciteDropdownSelect.emit({
      item: event.detail.requestedDropdownItem
    });
    if (!this.disableCloseOnSelect ||
      event.detail.requestedDropdownGroup.selectionMode === "none") {
      this.closeCalciteDropdown();
    }
    event.stopPropagation();
  }
  onBeforeOpen() {
    this.calciteDropdownBeforeOpen.emit();
  }
  onOpen() {
    this.calciteDropdownOpen.emit();
  }
  onBeforeClose() {
    this.calciteDropdownBeforeClose.emit();
  }
  onClose() {
    this.calciteDropdownClose.emit();
  }
  updateSelectedItems() {
    this.selectedItems = this.items.filter((item) => item.selected);
  }
  getMaxScrollerHeight() {
    const { maxItems, items } = this;
    let itemsToProcess = 0;
    let maxScrollerHeight = 0;
    let groupHeaderHeight;
    this.groups.forEach((group) => {
      if (maxItems > 0 && itemsToProcess < maxItems) {
        Array.from(group.children).forEach((item, index) => {
          if (index === 0) {
            if (isNaN(groupHeaderHeight)) {
              groupHeaderHeight = item.offsetTop;
            }
            maxScrollerHeight += groupHeaderHeight;
          }
          if (itemsToProcess < maxItems) {
            maxScrollerHeight += item.offsetHeight;
            itemsToProcess += 1;
          }
        });
      }
    });
    return items.length > maxItems ? maxScrollerHeight : 0;
  }
  closeCalciteDropdown(focusTrigger = true) {
    this.open = false;
    if (focusTrigger) {
      focusElement(this.triggers[0]);
    }
  }
  focusFirstItem() {
    const firstItem = this.items[0];
    this.getFocusableElement(firstItem);
  }
  focusLastItem() {
    const lastItem = this.items[this.items.length - 1];
    this.getFocusableElement(lastItem);
  }
  focusNextItem(el) {
    const index = this.itemIndex(el);
    const nextItem = this.items[index + 1] || this.items[0];
    this.getFocusableElement(nextItem);
  }
  focusPrevItem(el) {
    const index = this.itemIndex(el);
    const prevItem = this.items[index - 1] || this.items[this.items.length - 1];
    this.getFocusableElement(prevItem);
  }
  itemIndex(el) {
    return this.items.indexOf(el);
  }
  getFocusableElement(item) {
    if (!item) {
      return;
    }
    const target = item.attributes.isLink
      ? item.shadowRoot.querySelector("a")
      : item;
    focusElement(target);
  }
  static get is() { return "calcite-dropdown"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["dropdown.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["dropdown.css"]
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
              "text": "use open instead."
            }],
          "text": "Opens or closes the dropdown"
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
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
          "text": "When true, opens the dropdown"
        },
        "attribute": "open",
        "reflect": true,
        "defaultValue": "false"
      },
      "disableCloseOnSelect": {
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
          "text": "allow the dropdown to remain open after a selection is made\nif the selection-mode of the selected item's containing group is \"none\", the dropdown will always close"
        },
        "attribute": "disable-close-on-select",
        "reflect": true,
        "defaultValue": "false"
      },
      "disabled": {
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
          "text": "is the dropdown disabled"
        },
        "attribute": "disabled",
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
      "maxItems": {
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
          "text": "specify the maximum number of calcite-dropdown-items to display before showing the scroller, must be greater than 0 -\nthis value does not include groupTitles passed to calcite-dropdown-group"
        },
        "attribute": "max-items",
        "reflect": true,
        "defaultValue": "0"
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
          "text": "Determines the type of positioning to use for the overlaid content.\n\nUsing `\"absolute\"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.\n\n`\"fixed\"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `\"fixed\"`."
        },
        "attribute": "overlay-positioning",
        "reflect": true,
        "defaultValue": "\"absolute\""
      },
      "placement": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "MenuPlacement",
          "resolved": "\"bottom\" | \"bottom-end\" | \"bottom-leading\" | \"bottom-start\" | \"bottom-trailing\" | \"top\" | \"top-end\" | \"top-leading\" | \"top-start\" | \"top-trailing\"",
          "references": {
            "MenuPlacement": {
              "location": "import",
              "path": "../../utils/floating-ui"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"bottom-start\""
            }],
          "text": "Determines where the dropdown will be positioned relative to the button."
        },
        "attribute": "placement",
        "reflect": true,
        "defaultValue": "defaultMenuPlacement"
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
          "text": "specify the scale of dropdown, defaults to m"
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "selectedItems": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "HTMLCalciteDropdownItemElement[]",
          "resolved": "HTMLCalciteDropdownItemElement[]",
          "references": {
            "HTMLCalciteDropdownItemElement": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "readonly",
              "text": undefined
            }],
          "text": "**read-only** The currently selected items"
        },
        "defaultValue": "[]"
      },
      "type": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "\"hover\" | \"click\"",
          "resolved": "\"click\" | \"hover\"",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "specify whether the dropdown is opened by hover or click of a trigger element"
        },
        "attribute": "type",
        "reflect": true,
        "defaultValue": "\"click\""
      },
      "width": {
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
        "optional": true,
        "docs": {
          "tags": [],
          "text": "specify the width of dropdown"
        },
        "attribute": "width",
        "reflect": true
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteDropdownSelect",
        "name": "calciteDropdownSelect",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "fires when a dropdown item has been selected or deselected"
        },
        "complexType": {
          "original": "Selection",
          "resolved": "Selection",
          "references": {
            "Selection": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        }
      }, {
        "method": "calciteDropdownBeforeClose",
        "name": "calciteDropdownBeforeClose",
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
        "method": "calciteDropdownClose",
        "name": "calciteDropdownClose",
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
        "method": "calciteDropdownBeforeOpen",
        "name": "calciteDropdownBeforeOpen",
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
        "method": "calciteDropdownOpen",
        "name": "calciteDropdownOpen",
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
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "active",
        "methodName": "activeHandler"
      }, {
        "propName": "open",
        "methodName": "openHandler"
      }, {
        "propName": "disabled",
        "methodName": "handleDisabledChange"
      }, {
        "propName": "flipPlacements",
        "methodName": "flipPlacementsHandler"
      }, {
        "propName": "maxItems",
        "methodName": "maxItemsHandler"
      }, {
        "propName": "overlayPositioning",
        "methodName": "overlayPositioningHandler"
      }, {
        "propName": "placement",
        "methodName": "placementHandler"
      }];
  }
  static get listeners() {
    return [{
        "name": "pointerdown",
        "method": "closeCalciteDropdownOnClick",
        "target": "window",
        "capture": false,
        "passive": true
      }, {
        "name": "calciteInternalDropdownCloseRequest",
        "method": "closeCalciteDropdownOnEvent",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteDropdownOpen",
        "method": "closeCalciteDropdownOnOpenEvent",
        "target": "window",
        "capture": false,
        "passive": false
      }, {
        "name": "pointerenter",
        "method": "mouseEnterHandler",
        "target": undefined,
        "capture": false,
        "passive": true
      }, {
        "name": "pointerleave",
        "method": "mouseLeaveHandler",
        "target": undefined,
        "capture": false,
        "passive": true
      }, {
        "name": "calciteInternalDropdownItemKeyEvent",
        "method": "calciteInternalDropdownItemKeyEvent",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalDropdownItemSelect",
        "method": "handleItemSelect",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
