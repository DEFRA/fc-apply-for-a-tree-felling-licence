/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { CSS } from "./resources";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding `calcite-dropdown` content.
 */
export class SplitButton {
  constructor() {
    /** Specifies the appearance style of the component. */
    this.appearance = "solid";
    /** Specifies the color of the component. */
    this.color = "blue";
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * When `true`, the component is active.
     *
     * @internal
     */
    this.active = false;
    /** Specifies the icon used for the dropdown menu. */
    this.dropdownIconType = "chevron";
    /**
      When `true`, a busy indicator is displayed on the primary button.
     */
    this.loading = false;
    /**
     * Determines the type of positioning to use for the overlaid content.
     *
     * Using `"absolute"` will work for most cases. The component will be positioned inside of overflowing parent containers and will affect the container's layout.
     *
     * `"fixed"` should be used to escape an overflowing parent container, or when the reference element's `position` CSS property is `"fixed"`.
     *
     */
    this.overlayPositioning = "absolute";
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Specifies the width of the component. */
    this.width = "auto";
    this.calciteSplitButtonPrimaryClickHandler = (event) => this.calciteSplitButtonPrimaryClick.emit(event);
    this.calciteSplitButtonSecondaryClickHandler = (event) => this.calciteSplitButtonSecondaryClick.emit(event);
  }
  handleDisabledChange(value) {
    if (!value) {
      this.active = false;
    }
  }
  activeHandler() {
    if (this.disabled) {
      this.active = false;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidRender() {
    updateHostInteraction(this);
  }
  render() {
    const widthClasses = {
      [CSS.container]: true,
      [CSS.widthAuto]: this.width === "auto",
      [CSS.widthHalf]: this.width === "half",
      [CSS.widthFull]: this.width === "full"
    };
    const buttonWidth = this.width === "auto" ? "auto" : "full";
    return (h("div", { class: widthClasses }, h("calcite-button", { appearance: this.appearance, color: this.color, disabled: this.disabled, "icon-end": this.primaryIconEnd ? this.primaryIconEnd : null, "icon-start": this.primaryIconStart ? this.primaryIconStart : null, iconFlipRtl: this.primaryIconFlipRtl ? this.primaryIconFlipRtl : null, label: this.primaryLabel, loading: this.loading, onClick: this.calciteSplitButtonPrimaryClickHandler, scale: this.scale, splitChild: "primary", type: "button", width: buttonWidth }, this.primaryText), h("div", { class: CSS.dividerContainer }, h("div", { class: CSS.divider })), h("calcite-dropdown", { active: this.active, disabled: this.disabled, onClick: this.calciteSplitButtonSecondaryClickHandler, overlayPositioning: this.overlayPositioning, placement: "bottom-end", scale: this.scale, width: this.scale }, h("calcite-button", { appearance: this.appearance, color: this.color, disabled: this.disabled, "icon-start": this.dropdownIcon, label: this.dropdownLabel, scale: this.scale, slot: "dropdown-trigger", splitChild: "secondary", type: "button" }), h("slot", null))));
  }
  get dropdownIcon() {
    return this.dropdownIconType === "chevron"
      ? "chevronDown"
      : this.dropdownIconType === "caret"
        ? "caretDown"
        : this.dropdownIconType === "ellipsis"
          ? "ellipsis"
          : "handle-vertical";
  }
  static get is() { return "calcite-split-button"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["split-button.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["split-button.css"]
    };
  }
  static get properties() {
    return {
      "appearance": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ButtonAppearance",
          "resolved": "\"clear\" | \"minimal\" | \"outline\" | \"solid\" | \"transparent\"",
          "references": {
            "ButtonAppearance": {
              "location": "import",
              "path": "../button/interfaces"
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
          "original": "ButtonColor",
          "resolved": "\"blue\" | \"inverse\" | \"neutral\" | \"red\"",
          "references": {
            "ButtonColor": {
              "location": "import",
              "path": "../button/interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the color of the component."
        },
        "attribute": "color",
        "reflect": true,
        "defaultValue": "\"blue\""
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
              "name": "internal",
              "text": undefined
            }],
          "text": "When `true`, the component is active."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "dropdownIconType": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "DropdownIconType",
          "resolved": "\"caret\" | \"chevron\" | \"ellipsis\" | \"overflow\"",
          "references": {
            "DropdownIconType": {
              "location": "import",
              "path": "../button/interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the icon used for the dropdown menu."
        },
        "attribute": "dropdown-icon-type",
        "reflect": true,
        "defaultValue": "\"chevron\""
      },
      "dropdownLabel": {
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
          "text": "Accessible name for the dropdown menu."
        },
        "attribute": "dropdown-label",
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
          "text": "When `true`, a busy indicator is displayed on the primary button."
        },
        "attribute": "loading",
        "reflect": true,
        "defaultValue": "false"
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
      "primaryIconEnd": {
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
          "text": "Specifies an icon to display at the end of the primary button."
        },
        "attribute": "primary-icon-end",
        "reflect": true
      },
      "primaryIconFlipRtl": {
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
          "text": "When `true`, the primary button icon will be flipped when the element direction is right-to-left (`\"rtl\"`)."
        },
        "attribute": "primary-icon-flip-rtl",
        "reflect": true
      },
      "primaryIconStart": {
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
          "text": "Specifies an icon to display at the start of the primary button."
        },
        "attribute": "primary-icon-start",
        "reflect": true
      },
      "primaryLabel": {
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
          "text": "Accessible name for the primary button."
        },
        "attribute": "primary-label",
        "reflect": true
      },
      "primaryText": {
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
          "text": "Text displayed in the primary button."
        },
        "attribute": "primary-text",
        "reflect": true
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
      "width": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Width",
          "resolved": "\"auto\" | \"full\" | \"half\"",
          "references": {
            "Width": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the width of the component."
        },
        "attribute": "width",
        "reflect": true,
        "defaultValue": "\"auto\""
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteSplitButtonPrimaryClick",
        "name": "calciteSplitButtonPrimaryClick",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the primary button is clicked.\n\n**Note:** The event payload is deprecated, use separate mouse event listeners to get info about click."
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
      }, {
        "method": "calciteSplitButtonSecondaryClick",
        "name": "calciteSplitButtonSecondaryClick",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the dropdown menu is clicked.\n\n**Note:** The event payload is deprecated, use separate mouse event listeners to get info about click."
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
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "disabled",
        "methodName": "handleDisabledChange"
      }, {
        "propName": "active",
        "methodName": "activeHandler"
      }];
  }
}
