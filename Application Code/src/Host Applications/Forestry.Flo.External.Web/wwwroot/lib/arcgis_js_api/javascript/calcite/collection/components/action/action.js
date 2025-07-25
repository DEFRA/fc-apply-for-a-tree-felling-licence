/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Host, h, forceUpdate } from "@stencil/core";
import { CSS, TEXT, SLOTS } from "./resources";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
import { toAriaBoolean } from "../../utils/dom";
/**
 * @slot - A slot for adding a `calcite-icon`.
 */
export class Action {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the component is highlighted.
     */
    this.active = false;
    /** Specifies the appearance of the component. */
    this.appearance = "solid";
    /**
     * When `true`, the side padding of the component is reduced. Compact mode is used internally by components, e.g. `calcite-block-section`.
     */
    this.compact = false;
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `true`, indicates unread changes.
     */
    this.indicator = false;
    /**
     * Specifies the text label to display while loading.
     *
     * @default "Loading"
     */
    this.intlLoading = TEXT.loading;
    /**
     * When `true`, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * Specifies the size of the component.
     */
    this.scale = "m";
    /**
     * Indicates whether the text is displayed.
     */
    this.textEnabled = false;
    this.mutationObserver = createObserver("mutation", () => forceUpdate(this));
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.handleTooltipSlotChange = (event) => {
      const tooltips = event.target
        .assignedElements({
        flatten: true
      })
        .filter((el) => el === null || el === void 0 ? void 0 : el.matches("calcite-tooltip"));
      const tooltip = tooltips[0];
      if (tooltip) {
        tooltip.referenceElement = this.buttonEl;
      }
    };
    this.calciteActionClickHandler = () => {
      if (!this.disabled) {
        this.calciteActionClick.emit();
      }
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Methods
  //
  // --------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.buttonEl) === null || _a === void 0 ? void 0 : _a.focus();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderTextContainer() {
    const { text, textEnabled } = this;
    const textContainerClasses = {
      [CSS.textContainer]: true,
      [CSS.textContainerVisible]: textEnabled
    };
    return text ? (h("div", { class: textContainerClasses, key: "text-container" }, text)) : null;
  }
  renderIconContainer() {
    var _a;
    const { loading, icon, scale, el, intlLoading } = this;
    const iconScale = scale === "l" ? "m" : "s";
    const loaderScale = scale === "l" ? "l" : "m";
    const calciteLoaderNode = loading ? (h("calcite-loader", { active: true, inline: true, label: intlLoading, scale: loaderScale })) : null;
    const calciteIconNode = icon ? h("calcite-icon", { icon: icon, scale: iconScale }) : null;
    const iconNode = calciteLoaderNode || calciteIconNode;
    const hasIconToDisplay = iconNode || ((_a = el.children) === null || _a === void 0 ? void 0 : _a.length);
    const slotContainerNode = (h("div", { class: {
        [CSS.slotContainer]: true,
        [CSS.slotContainerHidden]: loading
      } }, h("slot", null)));
    return hasIconToDisplay ? (h("div", { "aria-hidden": "true", class: CSS.iconContainer, key: "icon-container" }, iconNode, slotContainerNode)) : null;
  }
  render() {
    const { compact, disabled, loading, textEnabled, label, text } = this;
    const ariaLabel = label || text;
    const buttonClasses = {
      [CSS.button]: true,
      [CSS.buttonTextVisible]: textEnabled,
      [CSS.buttonCompact]: compact
    };
    return (h(Host, { onClick: this.calciteActionClickHandler }, h("button", { "aria-busy": toAriaBoolean(loading), "aria-disabled": toAriaBoolean(disabled), "aria-label": ariaLabel, class: buttonClasses, disabled: disabled, ref: (buttonEl) => (this.buttonEl = buttonEl) }, this.renderIconContainer(), this.renderTextContainer()), h("slot", { name: SLOTS.tooltip, onSlotchange: this.handleTooltipSlotChange })));
  }
  static get is() { return "calcite-action"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["action.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["action.css"]
    };
  }
  static get properties() {
    return {
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
          "text": "When `true`, the component is highlighted."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "alignment": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Alignment",
          "resolved": "\"center\" | \"end\" | \"start\"",
          "references": {
            "Alignment": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the horizontal alignment of button elements with text content."
        },
        "attribute": "alignment",
        "reflect": true
      },
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
          "text": "Specifies the appearance of the component."
        },
        "attribute": "appearance",
        "reflect": true,
        "defaultValue": "\"solid\""
      },
      "compact": {
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
          "text": "When `true`, the side padding of the component is reduced. Compact mode is used internally by components, e.g. `calcite-block-section`."
        },
        "attribute": "compact",
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
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
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
        "reflect": false
      },
      "indicator": {
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
          "text": "When `true`, indicates unread changes."
        },
        "attribute": "indicator",
        "reflect": true,
        "defaultValue": "false"
      },
      "intlLoading": {
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
              "text": "\"Loading\""
            }],
          "text": "Specifies the text label to display while loading."
        },
        "attribute": "intl-loading",
        "reflect": false,
        "defaultValue": "TEXT.loading"
      },
      "label": {
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
          "text": "Specifies the label of the component. If no label is provided, the label inherits what's provided for the `text` prop."
        },
        "attribute": "label",
        "reflect": false
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
      "text": {
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
          "text": "Specifies text that accompanies the icon."
        },
        "attribute": "text",
        "reflect": false
      },
      "textEnabled": {
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
          "text": "Indicates whether the text is displayed."
        },
        "attribute": "text-enabled",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteActionClick",
        "name": "calciteActionClick",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `onClick` instead."
            }],
          "text": "Emits when the component has been clicked."
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
}
