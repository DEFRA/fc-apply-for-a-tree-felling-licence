/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Fragment, h } from "@stencil/core";
import { CSS, ICONS, SLOTS, TEXT } from "./resources";
import { ICON_TYPES } from "../pick-list/resources";
import { getSlotted, toAriaBoolean } from "../../utils/dom";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot actions-end - A slot for adding `calcite-action`s or content to the end side of the component.
 * @slot actions-start - A slot for adding `calcite-action`s or content to the start side of the component.
 */
export class PickListItem {
  constructor() {
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `false`, the component cannot be deselected by user interaction.
     */
    this.disableDeselect = false;
    /**
     * @internal
     */
    this.nonInteractive = false;
    /**
     * Determines the icon SVG symbol that will be shown. Options are `"circle"`, `"square"`, `"grip"` or `null`.
     *
     * @see [ICON_TYPES](https://github.com/Esri/calcite-components/blob/master/src/components/pick-list/resources.ts#L5)
     */
    this.icon = null;
    /**
     * When `true`, displays a remove action that removes the item from the list.
     */
    this.removable = false;
    /**
     * When `true`, selects an item. Toggles when an item is checked/unchecked.
     */
    this.selected = false;
    /**
     * When `removable` is `true`, the accessible name for the component's remove button.
     *
     * @default "Remove"
     */
    this.intlRemove = TEXT.remove;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.pickListClickHandler = (event) => {
      if (this.disabled || (this.disableDeselect && this.selected) || this.nonInteractive) {
        return;
      }
      this.shiftPressed = event.shiftKey;
      this.selected = !this.selected;
    };
    this.pickListKeyDownHandler = (event) => {
      if (event.key === " ") {
        event.preventDefault();
        if ((this.disableDeselect && this.selected) || this.nonInteractive) {
          return;
        }
        this.selected = !this.selected;
      }
    };
    this.removeClickHandler = () => {
      this.calciteListItemRemove.emit();
    };
  }
  descriptionWatchHandler() {
    this.calciteInternalListItemPropsChange.emit();
  }
  labelWatchHandler() {
    this.calciteInternalListItemPropsChange.emit();
  }
  metadataWatchHandler() {
    this.calciteInternalListItemPropsChange.emit();
  }
  selectedWatchHandler() {
    this.calciteListItemChange.emit({
      item: this.el,
      value: this.value,
      selected: this.selected,
      shiftPressed: this.shiftPressed
    });
    this.shiftPressed = false;
  }
  valueWatchHandler(newValue, oldValue) {
    this.calciteInternalListItemValueChange.emit({ oldValue, newValue });
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
    updateHostInteraction(this, this.el.closest("calcite-pick-list") ? "managed" : false);
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Toggles the selection state. By default this won't trigger an event.
   * The first argument allows the value to be coerced, rather than swapping values.
   *
   * @param coerce
   */
  async toggleSelected(coerce) {
    this.selected = typeof coerce === "boolean" ? coerce : !this.selected;
  }
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.focusEl) === null || _a === void 0 ? void 0 : _a.focus();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderIcon() {
    const { icon } = this;
    if (!icon) {
      return null;
    }
    return (h("span", { class: {
        [CSS.icon]: true,
        [CSS.iconDot]: icon === ICON_TYPES.circle
      }, onClick: this.pickListClickHandler }, icon === ICON_TYPES.square ? h("calcite-icon", { icon: ICONS.checked, scale: "s" }) : null));
  }
  renderRemoveAction() {
    return this.removable ? (h("calcite-action", { class: CSS.remove, icon: ICONS.remove, onCalciteActionClick: this.removeClickHandler, slot: SLOTS.actionsEnd, text: this.intlRemove })) : null;
  }
  renderActionsStart() {
    const { el } = this;
    const hasActionsStart = getSlotted(el, SLOTS.actionsStart);
    return hasActionsStart ? (h("div", { class: { [CSS.actions]: true, [CSS.actionsStart]: true } }, h("slot", { name: SLOTS.actionsStart }))) : null;
  }
  renderActionsEnd() {
    const { el, removable } = this;
    const hasActionsEnd = getSlotted(el, SLOTS.actionsEnd);
    return hasActionsEnd || removable ? (h("div", { class: { [CSS.actions]: true, [CSS.actionsEnd]: true } }, h("slot", { name: SLOTS.actionsEnd }), this.renderRemoveAction())) : null;
  }
  render() {
    const { description, label } = this;
    return (h(Fragment, null, this.renderIcon(), this.renderActionsStart(), h("label", { "aria-label": label, class: CSS.label, onClick: this.pickListClickHandler, onKeyDown: this.pickListKeyDownHandler, ref: (focusEl) => (this.focusEl = focusEl), tabIndex: 0 }, h("div", { "aria-checked": toAriaBoolean(this.selected), class: CSS.textContainer, role: "menuitemcheckbox" }, h("span", { class: CSS.title }, label), description ? h("span", { class: CSS.description }, description) : null)), this.renderActionsEnd()));
  }
  static get is() { return "calcite-pick-list-item"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["pick-list-item.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["pick-list-item.css"]
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
          "tags": [],
          "text": "When `false`, the component cannot be deselected by user interaction."
        },
        "attribute": "disable-deselect",
        "reflect": true,
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
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "non-interactive",
        "reflect": true,
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
          "text": "Determines the icon SVG symbol that will be shown. Options are `\"circle\"`, `\"square\"`, `\"grip\"` or `null`."
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
        "optional": true,
        "docs": {
          "tags": [],
          "text": "When `true`, displays a remove action that removes the item from the list."
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
          "text": "When `true`, selects an item. Toggles when an item is checked/unchecked."
        },
        "attribute": "selected",
        "reflect": true,
        "defaultValue": "false"
      },
      "intlRemove": {
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
              "text": "\"Remove\""
            }],
          "text": "When `removable` is `true`, the accessible name for the component's remove button."
        },
        "attribute": "intl-remove",
        "reflect": true,
        "defaultValue": "TEXT.remove"
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
        "method": "calciteListItemChange",
        "name": "calciteListItemChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is selected or unselected."
        },
        "complexType": {
          "original": "{\n    item: HTMLCalcitePickListItemElement;\n    value: any;\n    selected: boolean;\n    shiftPressed: boolean;\n  }",
          "resolved": "{ item: HTMLCalcitePickListItemElement; value: any; selected: boolean; shiftPressed: boolean; }",
          "references": {
            "HTMLCalcitePickListItemElement": {
              "location": "global"
            }
          }
        }
      }, {
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
      }, {
        "method": "calciteInternalListItemPropsChange",
        "name": "calciteInternalListItemPropsChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Emits when the component's label, description, value, or metadata properties are modified."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalListItemValueChange",
        "name": "calciteInternalListItemValueChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Emits when the component's value property is modified."
        },
        "complexType": {
          "original": "{\n    oldValue: any;\n    newValue: any;\n  }",
          "resolved": "{ oldValue: any; newValue: any; }",
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
          "text": "Toggles the selection state. By default this won't trigger an event.\nThe first argument allows the value to be coerced, rather than swapping values.",
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
          "text": "Sets focus on the component.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "description",
        "methodName": "descriptionWatchHandler"
      }, {
        "propName": "label",
        "methodName": "labelWatchHandler"
      }, {
        "propName": "metadata",
        "methodName": "metadataWatchHandler"
      }, {
        "propName": "selected",
        "methodName": "selectedWatchHandler"
      }, {
        "propName": "value",
        "methodName": "valueWatchHandler"
      }];
  }
}
