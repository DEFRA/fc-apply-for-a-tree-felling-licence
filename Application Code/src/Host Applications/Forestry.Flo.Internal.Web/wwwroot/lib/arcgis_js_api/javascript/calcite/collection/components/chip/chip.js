/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { getSlotted } from "../../utils/dom";
import { guid } from "../../utils/guid";
import { CSS, TEXT, SLOTS, ICONS } from "./resources";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding text.
 * @slot image - A slot for adding an image.
 */
export class Chip {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the appearance style of the component. */
    this.appearance = "solid";
    /** Specifies the color for the component. */
    this.color = "grey";
    /**
     * When `true`, a close button is added to the component.
     *
     * @deprecated use `closable` instead.
     */
    this.dismissible = false;
    /** When `true`, a close button is added to the component. */
    this.closable = false;
    /**
     * Accessible name for the component's close button.
     *
     * @default "Close"
     */
    this.dismissLabel = TEXT.close;
    /** When `true`, the icon will be flipped when the element direction is right-to-left (`"rtl"`). */
    this.iconFlipRtl = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    /** When `true`, hides the component. */
    this.closed = false;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.closeClickHandler = (event) => {
      event.preventDefault();
      this.calciteChipDismiss.emit(this.el);
      this.closed = true;
    };
    this.guid = guid();
  }
  handleDismissible(value) {
    this.closable = value;
  }
  handleClosable(value) {
    this.dismissible = value;
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
    if (this.dismissible) {
      this.handleDismissible(this.dismissible);
    }
    if (this.closable) {
      this.handleClosable(this.closable);
    }
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.closeButton) === null || _a === void 0 ? void 0 : _a.focus();
  }
  //--------------------------------------------------------------------------
  //
  //  Render Methods
  //
  //--------------------------------------------------------------------------
  renderChipImage() {
    const { el } = this;
    const hasChipImage = getSlotted(el, SLOTS.image);
    return hasChipImage ? (h("div", { class: CSS.imageContainer, key: "image" }, h("slot", { name: SLOTS.image }))) : null;
  }
  render() {
    const iconEl = (h("calcite-icon", { class: CSS.chipIcon, flipRtl: this.iconFlipRtl, icon: this.icon, scale: "s" }));
    const closeButton = (h("button", { "aria-describedby": this.guid, "aria-label": this.dismissLabel, class: CSS.close, onClick: this.closeClickHandler, ref: (el) => (this.closeButton = el) }, h("calcite-icon", { class: CSS.closeIcon, icon: ICONS.close, scale: "s" })));
    return (h("div", { class: "container" }, this.renderChipImage(), this.icon ? iconEl : null, h("span", { class: CSS.title, id: this.guid }, h("slot", null)), this.closable ? closeButton : null));
  }
  static get is() { return "calcite-chip"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["chip.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["chip.css"]
    };
  }
  static get properties() {
    return {
      "appearance": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Extract<\"solid\" | \"clear\", Appearance>",
          "resolved": "\"clear\" | \"solid\"",
          "references": {
            "Extract": {
              "location": "global"
            },
            "Appearance": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the appearance style of the component."
        },
        "attribute": "appearance",
        "reflect": true,
        "defaultValue": "\"solid\""
      },
      "color": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ChipColor",
          "resolved": "\"blue\" | \"green\" | \"grey\" | \"red\" | \"yellow\"",
          "references": {
            "ChipColor": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the color for the component."
        },
        "attribute": "color",
        "reflect": true,
        "defaultValue": "\"grey\""
      },
      "dismissible": {
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
              "text": "use `closable` instead."
            }],
          "text": "When `true`, a close button is added to the component."
        },
        "attribute": "dismissible",
        "reflect": true,
        "defaultValue": "false"
      },
      "closable": {
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
          "text": "When `true`, a close button is added to the component."
        },
        "attribute": "closable",
        "reflect": true,
        "defaultValue": "false"
      },
      "dismissLabel": {
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
              "name": "default",
              "text": "\"Close\""
            }],
          "text": "Accessible name for the component's close button."
        },
        "attribute": "dismiss-label",
        "reflect": false,
        "defaultValue": "TEXT.close"
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
      "iconFlipRtl": {
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
          "text": "When `true`, the icon will be flipped when the element direction is right-to-left (`\"rtl\"`)."
        },
        "attribute": "icon-flip-rtl",
        "reflect": true,
        "defaultValue": "false"
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
      "closed": {
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
          "text": "When `true`, hides the component."
        },
        "attribute": "closed",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteChipDismiss",
        "name": "calciteChipDismiss",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the dismiss button is clicked.\n\n**Note:**: The `el` event payload props is deprecated, please use the event's `target`/`currentTarget` instead."
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
        "propName": "dismissible",
        "methodName": "handleDismissible"
      }, {
        "propName": "closable",
        "methodName": "handleClosable"
      }];
  }
}
