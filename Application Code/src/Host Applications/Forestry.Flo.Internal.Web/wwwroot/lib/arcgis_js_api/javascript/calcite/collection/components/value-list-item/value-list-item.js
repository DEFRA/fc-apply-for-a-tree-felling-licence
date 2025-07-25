/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { ICON_TYPES } from "../pick-list/resources";
import { guid } from "../../utils/guid";
import { CSS, SLOTS as PICK_LIST_SLOTS } from "../pick-list-item/resources";
import { ICONS, SLOTS } from "./resources";
import { getSlotted } from "../../utils/dom";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot actions-end - A slot for adding actions or content to the end side of the component.
 * @slot actions-start - A slot for adding actions or content to the start side of the component.
 */
export class ValueListItem {
  constructor() {
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * @internal
     */
    this.disableDeselect = false;
    /**
     * When `true`, prevents the content of the component from user interaction.
     */
    this.nonInteractive = false;
    /**
     * @internal
     */
    this.handleActivated = false;
    /**
     * Determines the icon SVG symbol that will be shown. Options are circle, square, grip or null.
     *
     * @see [ICON_TYPES](https://github.com/Esri/calcite-components/blob/master/src/components/pick-list/resources.ts#L5)
     */
    this.icon = null;
    /**
     * When `true`, adds an action to remove the component.
     */
    this.removable = false;
    /**
     * When `true`, the component is selected.
     */
    this.selected = false;
    this.pickListItem = null;
    this.guid = `calcite-value-list-item-${guid()}`;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.getPickListRef = (el) => (this.pickListItem = el);
    this.handleKeyDown = (event) => {
      if (event.key === " ") {
        this.handleActivated = !this.handleActivated;
      }
    };
    this.handleBlur = () => {
      this.handleActivated = false;
    };
    this.handleSelectChange = (event) => {
      this.selected = event.detail.selected;
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  componentDidRender() {
    updateHostInteraction(this, this.el.closest("calcite-value-list") ? "managed" : false);
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Toggle the selection state. By default this won't trigger an event.
   * The first argument allows the value to be coerced, rather than swapping values.
   *
   * @param coerce
   */
  async toggleSelected(coerce) {
    this.pickListItem.toggleSelected(coerce);
  }
  /** Set focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.pickListItem) === null || _a === void 0 ? void 0 : _a.setFocus();
  }
  calciteListItemChangeHandler(event) {
    // adjust item payload from wrapped item before bubbling
    event.detail.item = this.el;
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderActionsEnd() {
    const { el } = this;
    const hasActionsEnd = getSlotted(el, SLOTS.actionsEnd);
    return hasActionsEnd ? (h("slot", { name: SLOTS.actionsEnd, slot: PICK_LIST_SLOTS.actionsEnd })) : null;
  }
  renderActionsStart() {
    const { el } = this;
    const hasActionsStart = getSlotted(el, SLOTS.actionsStart);
    return hasActionsStart ? (h("slot", { name: SLOTS.actionsStart, slot: PICK_LIST_SLOTS.actionsStart })) : null;
  }
  renderHandle() {
    const { icon } = this;
    if (icon === ICON_TYPES.grip) {
      return (h("span", { class: {
          [CSS.handle]: true,
          [CSS.handleActivated]: this.handleActivated
        }, "data-js-handle": true, onBlur: this.handleBlur, onKeyDown: this.handleKeyDown, role: "button", tabindex: "0" }, h("calcite-icon", { icon: ICONS.drag, scale: "s" })));
    }
  }
  render() {
    return (h(Host, { id: this.el.id || this.guid }, this.renderHandle(), h("calcite-pick-list-item", { description: this.description, disableDeselect: this.disableDeselect, disabled: this.disabled, label: this.label, metadata: this.metadata, nonInteractive: this.nonInteractive, onCalciteListItemChange: this.handleSelectChange, ref: this.getPickListRef, removable: this.removable, selected: this.selected, value: this.value }, this.renderActionsStart(), this.renderActionsEnd())));
  }
  static get is() { return "calcite-value-list-item"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["value-list-item.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["value-list-item.css"]
    };
  }
  static get properties() {
    return {
      "description": {
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
          "text": "A description for the component that displays below the label text."
        },
        "attribute": "description",
        "reflect": true
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
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "disableDeselect": {
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
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "disable-deselect",
        "reflect": false,
        "defaultValue": "false"
      },
      "nonInteractive": {
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
          "text": "When `true`, prevents the content of the component from user interaction."
        },
        "attribute": "non-interactive",
        "reflect": true,
        "defaultValue": "false"
      },
      "handleActivated": {
        "type": "boolean",
        "mutable": true,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "handle-activated",
        "reflect": false,
        "defaultValue": "false"
      },
      "icon": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ICON_TYPES | null",
          "resolved": "ICON_TYPES.circle | ICON_TYPES.grip | ICON_TYPES.square",
          "references": {
            "ICON_TYPES": {
              "location": "import",
              "path": "../pick-list/resources"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "see",
              "text": "[ICON_TYPES](https://github.com/Esri/calcite-components/blob/master/src/components/pick-list/resources.ts#L5)"
            }],
          "text": "Determines the icon SVG symbol that will be shown. Options are circle, square, grip or null."
        },
        "attribute": "icon",
        "reflect": true,
        "defaultValue": "null"
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
          "text": "Label and accessible name for the component. Appears next to the icon."
        },
        "attribute": "label",
        "reflect": true
      },
      "metadata": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "Record<string, unknown>",
          "resolved": "{ [x: string]: unknown; }",
          "references": {
            "Record": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Provides additional metadata to the component. Primary use is for a filter on the parent list."
        }
      },
      "removable": {
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
          "text": "When `true`, adds an action to remove the component."
        },
        "attribute": "removable",
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
          "text": "When `true`, the component is selected."
        },
        "attribute": "selected",
        "reflect": true,
        "defaultValue": "false"
      },
      "value": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "any",
          "resolved": "any",
          "references": {}
        },
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The component's value."
        },
        "attribute": "value",
        "reflect": false
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteListItemRemove",
        "name": "calciteListItemRemove",
        "bubbles": true,
        "cancelable": true,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the remove button is pressed."
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
      "toggleSelected": {
        "complexType": {
          "signature": "(coerce?: boolean) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "coerce"
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
          "text": "Toggle the selection state. By default this won't trigger an event.\nThe first argument allows the value to be coerced, rather than swapping values.",
          "tags": [{
              "name": "param",
              "text": "coerce"
            }]
        }
      },
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
          "text": "Set focus on the component.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "calciteListItemChange",
        "method": "calciteListItemChangeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
