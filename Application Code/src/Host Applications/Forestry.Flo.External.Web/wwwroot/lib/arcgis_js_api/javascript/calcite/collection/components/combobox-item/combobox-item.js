/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Host, h } from "@stencil/core";
import { getElementProp, getSlotted } from "../../utils/dom";
import { CSS } from "./resources";
import { guid } from "../../utils/guid";
import { getAncestors, getDepth } from "../combobox/utils";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding nested `calcite-combobox-item`s.
 */
export class ComboboxItem {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * When `true`, the component is selected.
     */
    this.selected = false;
    /** When `true`, the component is active. */
    this.active = false;
    /** The `id` attribute of the component. When omitted, a globally unique identifier is used. */
    this.guid = guid();
    this.scale = "m";
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.itemClickHandler = (event) => {
      event.preventDefault();
      if (this.disabled) {
        return;
      }
      this.selected = !this.selected;
    };
  }
  selectedWatchHandler() {
    this.calciteComboboxItemChange.emit(this.el);
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    this.ancestors = getAncestors(this.el);
    this.scale = getElementProp(this.el, "scale", this.scale);
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Used to toggle the selection state. By default this won't trigger an event.
   * The first argument allows the value to be coerced, rather than swapping values.
   *
   * @param coerce
   */
  async toggleSelected(coerce) {
    if (this.disabled) {
      return;
    }
    this.selected = typeof coerce === "boolean" ? coerce : !this.selected;
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderIcon(isSingle) {
    const { icon, disabled, selected } = this;
    const level = `${CSS.icon}--indent`;
    const defaultIcon = isSingle ? "dot" : "check";
    const iconPath = disabled ? "circle-disallowed" : defaultIcon;
    const showDot = isSingle && !icon && !disabled;
    return showDot ? (h("span", { class: {
        [CSS.icon]: true,
        [CSS.dot]: true,
        [level]: true
      } })) : (h("calcite-icon", { class: {
        [CSS.icon]: !icon,
        [CSS.custom]: !!icon,
        [CSS.iconActive]: icon && selected,
        [level]: true
      }, icon: icon || iconPath, scale: "s" }));
  }
  renderChildren() {
    if (getSlotted(this.el)) {
      return (h("ul", { key: "default-slot-container" }, h("slot", null)));
    }
    return null;
  }
  render() {
    const isSingleSelect = getElementProp(this.el, "selection-mode", "multi") === "single";
    const classes = {
      [CSS.label]: true,
      [CSS.selected]: this.selected,
      [CSS.active]: this.active,
      [CSS.single]: isSingleSelect
    };
    const depth = getDepth(this.el);
    return (h(Host, { "aria-hidden": "true" }, h("div", { class: `container scale--${this.scale}`, style: { "--calcite-combobox-item-spacing-indent-multiplier": `${depth}` } }, h("li", { class: classes, id: this.guid, onClick: this.itemClickHandler }, this.renderIcon(isSingleSelect), h("span", { class: CSS.title }, this.textLabel)), this.renderChildren())));
  }
  static get is() { return "calcite-combobox-item"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["combobox-item.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["combobox-item.css"]
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
      "active": {
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
          "text": "When `true`, the component is active."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "ancestors": {
        "type": "unknown",
        "mutable": true,
        "complexType": {
          "original": "ComboboxChildElement[]",
          "resolved": "ComboboxChildElement[]",
          "references": {
            "ComboboxChildElement": {
              "location": "import",
              "path": "../combobox/interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the parent and grandparent items, which are set on `calcite-combobox`."
        }
      },
      "guid": {
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
          "text": "The `id` attribute of the component. When omitted, a globally unique identifier is used."
        },
        "attribute": "guid",
        "reflect": true,
        "defaultValue": "guid()"
      },
      "icon": {
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
          "text": "Specifies an icon to display."
        },
        "attribute": "icon",
        "reflect": true
      },
      "textLabel": {
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
          "text": "The component's text."
        },
        "attribute": "text-label",
        "reflect": true
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
      },
      "constant": {
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
              "text": "use `filterDisabled` instead."
            }],
          "text": "When `true`, omits the component from the `calcite-combobox` filtered search results."
        },
        "attribute": "constant",
        "reflect": true
      },
      "filterDisabled": {
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
          "text": "When `true`, omits the component from the `calcite-combobox` filtered search results."
        },
        "attribute": "filter-disabled",
        "reflect": true
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteComboboxItemChange",
        "name": "calciteComboboxItemChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits whenever the component is selected or unselected.\n\n**Note:**: The event's payload is deprecated, please use the event's `target`/`currentTarget` instead"
        },
        "complexType": {
          "original": "DeprecatedEventPayload",
          "resolved": "any",
          "references": {
            "DeprecatedEventPayload": {
              "location": "import",
              "path": "../interfaces"
            }
          }
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
          "text": "Used to toggle the selection state. By default this won't trigger an event.\nThe first argument allows the value to be coerced, rather than swapping values.",
          "tags": [{
              "name": "param",
              "text": "coerce"
            }]
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "selected",
        "methodName": "selectedWatchHandler"
      }];
  }
}
