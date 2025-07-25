/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import Sortable from "sortablejs";
import { h } from "@stencil/core";
import { CSS, ICON_TYPES } from "./resources";
import { calciteListFocusOutHandler, calciteListItemChangeHandler, calciteInternalListItemValueChangeHandler, cleanUpObserver, deselectSiblingItems, deselectRemovedItems, getItemData, handleFilter, initialize, initializeObserver, keyDownHandler, mutationObserverCallback, removeItem, selectSiblings, setFocus, setUpItems, moveItemIndex } from "../pick-list/shared-list-logic";
import List from "../pick-list/shared-list-render";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
import { getHandleAndItemElement, getScreenReaderText } from "./utils";
/**
 * @slot - A slot for adding `calcite-value-list-item` elements. List items are displayed as a vertical list.
 * @slot menu-actions - A slot for adding a button and menu combination for performing actions, such as sorting.
 */
export class ValueList {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `true`, `calcite-value-list-item`s are sortable via a draggable button.
     */
    this.dragEnabled = false;
    /**
     * When `true`, an input appears at the top of the component that can be used by end users to filter list items.
     */
    this.filterEnabled = false;
    /**
     * When `true`, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * Similar to standard radio buttons and checkboxes.
     * When `true`, a user can select multiple `calcite-value-list-item`s at a time.
     * When `false`, only a single `calcite-value-list-item` can be selected at a time,
     * and a new selection will deselect previous selections.
     */
    this.multiple = false;
    /**
     * When `true` and single-selection is enabled, the selection changes when navigating `calcite-value-list-item`s via keyboard.
     */
    this.selectionFollowsFocus = false;
    // --------------------------------------------------------------------------
    //
    //  Private Properties
    //
    // --------------------------------------------------------------------------
    this.selectedValues = new Map();
    this.dataForFilter = [];
    this.lastSelectedItem = null;
    this.mutationObserver = createObserver("mutation", mutationObserverCallback.bind(this));
    this.setFilterEl = (el) => {
      this.filterEl = el;
    };
    this.deselectRemovedItems = deselectRemovedItems.bind(this);
    this.deselectSiblingItems = deselectSiblingItems.bind(this);
    this.selectSiblings = selectSiblings.bind(this);
    this.handleFilter = handleFilter.bind(this);
    this.getItemData = getItemData.bind(this);
    this.keyDownHandler = (event) => {
      if (event.defaultPrevented) {
        return;
      }
      const { handle, item } = getHandleAndItemElement(event);
      if (handle && !item.handleActivated && event.key === " ") {
        this.updateScreenReaderText(getScreenReaderText(item, "commit", this));
      }
      if (!handle || !item.handleActivated) {
        keyDownHandler.call(this, event);
        return;
      }
      const { items } = this;
      if (event.key === " ") {
        this.updateScreenReaderText(getScreenReaderText(item, "active", this));
      }
      if ((event.key !== "ArrowUp" && event.key !== "ArrowDown") || items.length <= 1) {
        return;
      }
      event.preventDefault();
      const { el } = this;
      const nextIndex = moveItemIndex(this, item, event.key === "ArrowUp" ? "up" : "down");
      if (nextIndex === items.length - 1) {
        el.appendChild(item);
      }
      else {
        const itemAtNextIndex = el.children[nextIndex];
        const insertionReferenceItem = itemAtNextIndex === item.nextElementSibling
          ? itemAtNextIndex.nextElementSibling
          : itemAtNextIndex;
        el.insertBefore(item, insertionReferenceItem);
      }
      this.items = this.getItems();
      this.calciteListOrderChange.emit(this.items.map(({ value }) => value));
      requestAnimationFrame(() => handle === null || handle === void 0 ? void 0 : handle.focus());
      item.handleActivated = true;
      this.updateHandleAriaLabel(handle, getScreenReaderText(item, "change", this));
    };
    this.storeAssistiveEl = (el) => {
      this.assistiveTextEl = el;
    };
    this.handleFocusIn = (event) => {
      const { handle, item } = getHandleAndItemElement(event);
      if (!(item === null || item === void 0 ? void 0 : item.handleActivated) && item && handle) {
        this.updateHandleAriaLabel(handle, getScreenReaderText(item, "idle", this));
      }
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    initialize.call(this);
    initializeObserver.call(this);
  }
  componentDidLoad() {
    this.setUpDragAndDrop();
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  disconnectedCallback() {
    cleanUpObserver.call(this);
    this.cleanUpDragAndDrop();
  }
  calciteListFocusOutHandler(event) {
    calciteListFocusOutHandler.call(this, event);
  }
  calciteListItemRemoveHandler(event) {
    removeItem.call(this, event);
  }
  calciteListItemChangeHandler(event) {
    calciteListItemChangeHandler.call(this, event);
  }
  calciteInternalListItemPropsChangeHandler(event) {
    event.stopPropagation();
    this.setUpFilter();
  }
  calciteInternalListItemValueChangeHandler(event) {
    calciteInternalListItemValueChangeHandler.call(this, event);
    event.stopPropagation();
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  getItems() {
    return Array.from(this.el.querySelectorAll("calcite-value-list-item"));
  }
  setUpItems() {
    setUpItems.call(this, "calcite-value-list-item");
  }
  setUpFilter() {
    if (this.filterEnabled) {
      this.dataForFilter = this.getItemData();
    }
  }
  setUpDragAndDrop() {
    this.cleanUpDragAndDrop();
    if (!this.dragEnabled) {
      return;
    }
    this.sortable = Sortable.create(this.el, {
      dataIdAttr: "id",
      handle: `.${CSS.handle}`,
      draggable: "calcite-value-list-item",
      group: this.group,
      onSort: () => {
        this.items = Array.from(this.el.querySelectorAll("calcite-value-list-item"));
        const values = this.items.map((item) => item.value);
        this.calciteListOrderChange.emit(values);
      }
    });
  }
  cleanUpDragAndDrop() {
    var _a;
    (_a = this.sortable) === null || _a === void 0 ? void 0 : _a.destroy();
    this.sortable = null;
  }
  handleBlur() {
    if (this.dragEnabled) {
      this.updateScreenReaderText("");
    }
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Returns the currently selected items */
  async getSelectedItems() {
    return this.selectedValues;
  }
  /**
   * Sets focus on the component.
   *
   * @param focusId
   */
  async setFocus(focusId) {
    return setFocus.call(this, focusId);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  getIconType() {
    let type = null;
    if (this.dragEnabled) {
      type = ICON_TYPES.grip;
    }
    return type;
  }
  updateScreenReaderText(text) {
    this.assistiveTextEl.textContent = text;
  }
  updateHandleAriaLabel(handleElement, text) {
    handleElement.ariaLabel = text;
  }
  render() {
    return (h(List, { onBlur: this.handleBlur, onFocusin: this.handleFocusIn, onKeyDown: this.keyDownHandler, props: this }));
  }
  static get is() { return "calcite-value-list"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["value-list.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["value-list.css"]
    };
  }
  static get properties() {
    return {
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
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "dragEnabled": {
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
          "text": "When `true`, `calcite-value-list-item`s are sortable via a draggable button."
        },
        "attribute": "drag-enabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "filterEnabled": {
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
          "text": "When `true`, an input appears at the top of the component that can be used by end users to filter list items."
        },
        "attribute": "filter-enabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "filterPlaceholder": {
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
          "text": "Placeholder text for the filter's input field."
        },
        "attribute": "filter-placeholder",
        "reflect": true
      },
      "group": {
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
          "text": "The component's group identifier.\n\nTo drag elements from one list into another, both lists must have the same group value."
        },
        "attribute": "group",
        "reflect": true
      },
      "loading": {
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
          "text": "When `true`, a busy indicator is displayed."
        },
        "attribute": "loading",
        "reflect": true,
        "defaultValue": "false"
      },
      "multiple": {
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
          "text": "Similar to standard radio buttons and checkboxes.\nWhen `true`, a user can select multiple `calcite-value-list-item`s at a time.\nWhen `false`, only a single `calcite-value-list-item` can be selected at a time,\nand a new selection will deselect previous selections."
        },
        "attribute": "multiple",
        "reflect": true,
        "defaultValue": "false"
      },
      "selectionFollowsFocus": {
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
          "text": "When `true` and single-selection is enabled, the selection changes when navigating `calcite-value-list-item`s via keyboard."
        },
        "attribute": "selection-follows-focus",
        "reflect": true,
        "defaultValue": "false"
      },
      "intlDragHandleIdle": {
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
          "text": "When `dragEnabled` is `true` and active, specifies accessible context to the `calcite-value-list-item`s initial position.\n\nUse \"`${position}` of `${total}`\" as a placeholder for displaying indices and `${item.label}` as a placeholder for displaying the `calcite-value-list-item` label."
        },
        "attribute": "intl-drag-handle-idle",
        "reflect": false
      },
      "intlDragHandleActive": {
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
          "text": "When `dragEnabled` is `true` and active, specifies accessible context to the component.\n\nUse \"`${position}` of `${total}`\" as a placeholder for displaying indices and `${item.label}` as a placeholder for displaying the `calcite-value-list-item` label."
        },
        "attribute": "intl-drag-handle-active",
        "reflect": false
      },
      "intlDragHandleChange": {
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
          "text": "When `dragEnabled` is `true` and active, specifies accessible context to the `calcite-value-list-item`s new position.\n\nUse \"`${position}` of `${total}`\" as a placeholder for displaying indices and `${item.label}` as a placeholder for displaying the `calcite-value-list-item` label."
        },
        "attribute": "intl-drag-handle-change",
        "reflect": false
      },
      "intlDragHandleCommit": {
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
          "text": "When `dragEnabled` is `true` and active, specifies accessible context to the `calcite-value-list-item`s current position after commit.\n\nUse \"`${position}` of `${total}`\" as a placeholder for displaying indices and `${item.label}` as a placeholder for displaying the `calcite-value-list-item` label."
        },
        "attribute": "intl-drag-handle-commit",
        "reflect": false
      }
    };
  }
  static get states() {
    return {
      "selectedValues": {},
      "dataForFilter": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteListChange",
        "name": "calciteListChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits when any of the list item selections have changed."
        },
        "complexType": {
          "original": "Map<string, HTMLCalciteValueListItemElement>",
          "resolved": "Map<string, HTMLCalciteValueListItemElement>",
          "references": {
            "Map": {
              "location": "global"
            },
            "HTMLCalciteValueListItemElement": {
              "location": "global"
            }
          }
        }
      }, {
        "method": "calciteListOrderChange",
        "name": "calciteListOrderChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits when the order of the list has changed."
        },
        "complexType": {
          "original": "any[]",
          "resolved": "any[]",
          "references": {}
        }
      }];
  }
  static get methods() {
    return {
      "getSelectedItems": {
        "complexType": {
          "signature": "() => Promise<Map<string, HTMLCalciteValueListItemElement>>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            },
            "Map": {
              "location": "global"
            },
            "HTMLCalciteValueListItemElement": {
              "location": "global"
            }
          },
          "return": "Promise<Map<string, HTMLCalciteValueListItemElement>>"
        },
        "docs": {
          "text": "Returns the currently selected items",
          "tags": []
        }
      },
      "setFocus": {
        "complexType": {
          "signature": "(focusId?: ListFocusId) => Promise<void>",
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
            },
            "ListFocusId": {
              "location": "import",
              "path": "../pick-list/shared-list-logic"
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
      }
    };
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "focusout",
        "method": "calciteListFocusOutHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteListItemRemove",
        "method": "calciteListItemRemoveHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteListItemChange",
        "method": "calciteListItemChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalListItemPropsChange",
        "method": "calciteInternalListItemPropsChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalListItemValueChange",
        "method": "calciteInternalListItemValueChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
