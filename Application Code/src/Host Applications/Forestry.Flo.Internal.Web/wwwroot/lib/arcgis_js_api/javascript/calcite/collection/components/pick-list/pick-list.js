/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { ICON_TYPES } from "./resources";
import { calciteListItemChangeHandler, calciteInternalListItemValueChangeHandler, cleanUpObserver, deselectSiblingItems, deselectRemovedItems, getItemData, handleFilter, calciteListFocusOutHandler, initialize, initializeObserver, mutationObserverCallback, selectSiblings, setUpItems, keyDownHandler, setFocus, removeItem } from "./shared-list-logic";
import List from "./shared-list-render";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding `calcite-pick-list-item` or `calcite-pick-list-group` elements. Items are displayed as a vertical list.
 * @slot menu-actions - A slot for adding a button and menu combination for performing actions, such as sorting.
 */
export class PickList {
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
     * When `true`, an input appears at the top of the list that can be used by end users to filter items in the list.
     */
    this.filterEnabled = false;
    /**
     * When `true`, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * Similar to standard radio buttons and checkboxes.
     * When `true`, a user can select multiple `calcite-pick-list-item`s at a time.
     * When `false`, only a single `calcite-pick-list-item` can be selected at a time,
     * and a new selection will deselect previous selections.
     */
    this.multiple = false;
    /**
     * When `true` and single selection is enabled, the selection changes when navigating `calcite-pick-list-item`s via keyboard.
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
    this.keyDownHandler = keyDownHandler.bind(this);
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
  disconnectedCallback() {
    cleanUpObserver.call(this);
  }
  componentDidRender() {
    updateHostInteraction(this);
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
  calciteListFocusOutHandler(event) {
    calciteListFocusOutHandler.call(this, event);
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  setUpItems() {
    setUpItems.call(this, "calcite-pick-list-item");
  }
  setUpFilter() {
    if (this.filterEnabled) {
      this.dataForFilter = this.getItemData();
    }
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Returns the component's selected `calcite-pick-list-item`s. */
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
    return this.multiple ? ICON_TYPES.square : ICON_TYPES.circle;
  }
  render() {
    return h(List, { onKeyDown: this.keyDownHandler, props: this });
  }
  static get is() { return "calcite-pick-list"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["pick-list.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["pick-list.css"]
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
          "text": "When `true`, an input appears at the top of the list that can be used by end users to filter items in the list."
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
          "text": "Placeholder text for the filter input field."
        },
        "attribute": "filter-placeholder",
        "reflect": true
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
          "text": "Similar to standard radio buttons and checkboxes.\nWhen `true`, a user can select multiple `calcite-pick-list-item`s at a time.\nWhen `false`, only a single `calcite-pick-list-item` can be selected at a time,\nand a new selection will deselect previous selections."
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
          "text": "When `true` and single selection is enabled, the selection changes when navigating `calcite-pick-list-item`s via keyboard."
        },
        "attribute": "selection-follows-focus",
        "reflect": true,
        "defaultValue": "false"
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
          "text": "Emits when any of the `calcite-pick-list-item` selections have changed."
        },
        "complexType": {
          "original": "Map<string, HTMLCalcitePickListItemElement>",
          "resolved": "Map<string, HTMLCalcitePickListItemElement>",
          "references": {
            "Map": {
              "location": "global"
            },
            "HTMLCalcitePickListItemElement": {
              "location": "global"
            }
          }
        }
      }];
  }
  static get methods() {
    return {
      "getSelectedItems": {
        "complexType": {
          "signature": "() => Promise<Map<string, HTMLCalcitePickListItemElement>>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            },
            "Map": {
              "location": "global"
            },
            "HTMLCalcitePickListItemElement": {
              "location": "global"
            }
          },
          "return": "Promise<Map<string, HTMLCalcitePickListItemElement>>"
        },
        "docs": {
          "text": "Returns the component's selected `calcite-pick-list-item`s.",
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
              "path": "./shared-list-logic"
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
      }, {
        "name": "focusout",
        "method": "calciteListFocusOutHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
