/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { getElementProp, toAriaBoolean } from "../../utils/dom";
import { updateHostInteraction } from "../../utils/interactive";
import { numberStringFormatter, disconnectLocalized, connectLocalized } from "../../utils/locale";
/**
 * @slot - A slot for adding custom content.
 */
export class StepperItem {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     *  When `true`, the component is selected.
     *
     * @deprecated Use `selected` instead.
     */
    this.active = false;
    /**
     * When `true`, the component is selected.
     */
    this.selected = false;
    /** When `true`, the step has been completed. */
    this.complete = false;
    /** When `true`, the component contains an error that requires resolution from the user. */
    this.error = false;
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    // internal props inherited from wrapping calcite-stepper
    /** Defines the layout of the component. */
    /** @internal */
    this.layout = "horizontal";
    /** When `true`, displays a status icon in the component's heading. */
    /** @internal */
    this.icon = false;
    /** When `true`, displays the step number in the component's heading. */
    /** @internal */
    this.numbered = false;
    /** Specifies the size of the component. */
    /** @internal */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  Internal State/Props
    //
    //--------------------------------------------------------------------------
    this.effectiveLocale = "";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.keyDownHandler = (event) => {
      if (!this.disabled && event.target === this.el) {
        switch (event.key) {
          case " ":
          case "Enter":
            this.emitUserRequestedItem();
            event.preventDefault();
            break;
          case "ArrowUp":
          case "ArrowDown":
          case "ArrowLeft":
          case "ArrowRight":
          case "Home":
          case "End":
            this.calciteInternalStepperItemKeyEvent.emit({ item: event });
            event.preventDefault();
            break;
        }
      }
    };
    this.handleItemClick = (event) => {
      if (this.layout === "horizontal" &&
        event
          .composedPath()
          .some((el) => { var _a; return (_a = el.classList) === null || _a === void 0 ? void 0 : _a.contains("stepper-item-content"); })) {
        return;
      }
      this.emitUserRequestedItem();
    };
    this.emitUserRequestedItem = () => {
      this.emitRequestedItem();
      if (!this.disabled) {
        const position = this.itemPosition;
        this.calciteInternalUserRequestedStepperItemSelect.emit({
          position
        });
      }
    };
    this.emitRequestedItem = () => {
      if (!this.disabled) {
        const position = this.itemPosition;
        this.calciteInternalStepperItemSelect.emit({
          position
        });
      }
    };
  }
  activeHandler(value) {
    this.selected = value;
  }
  selectedHandler(value) {
    this.active = value;
    if (this.selected) {
      this.emitRequestedItem();
    }
  }
  // watch for removal of disabled to register step
  disabledWatcher() {
    this.registerStepperItem();
  }
  effectiveLocaleWatcher(locale) {
    var _a;
    numberStringFormatter.numberFormatOptions = {
      locale,
      numberingSystem: (_a = this.parentStepperEl) === null || _a === void 0 ? void 0 : _a.numberingSystem,
      useGrouping: false
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    connectLocalized(this);
    const { selected, active } = this;
    if (selected) {
      this.active = selected;
    }
    else if (active) {
      this.selected = active;
    }
  }
  componentWillLoad() {
    var _a;
    this.icon = getElementProp(this.el, "icon", false);
    this.numbered = getElementProp(this.el, "numbered", false);
    this.layout = getElementProp(this.el, "layout", false);
    this.scale = getElementProp(this.el, "scale", "m");
    this.parentStepperEl = this.el.parentElement;
    this.itemPosition = this.getItemPosition();
    this.registerStepperItem();
    if (this.selected) {
      this.emitRequestedItem();
    }
    numberStringFormatter.numberFormatOptions = {
      locale: this.effectiveLocale,
      numberingSystem: (_a = this.parentStepperEl) === null || _a === void 0 ? void 0 : _a.numberingSystem,
      useGrouping: false
    };
  }
  componentDidRender() {
    updateHostInteraction(this, true);
  }
  disconnectedCallback() {
    disconnectLocalized(this);
  }
  render() {
    return (h(Host, { "aria-expanded": toAriaBoolean(this.active), onClick: this.handleItemClick, onKeyDown: this.keyDownHandler }, h("div", { class: "container" }, h("div", { class: "stepper-item-header", ref: (el) => (this.headerEl = el), tabIndex: 
      /* additional tab index logic needed because of display: contents */
      this.layout === "horizontal" && !this.disabled ? 0 : null }, this.icon ? this.renderIcon() : null, this.numbered ? (h("div", { class: "stepper-item-number" }, numberStringFormatter.numberFormatter.format(this.itemPosition + 1), ".")) : null, h("div", { class: "stepper-item-header-text" }, h("span", { class: "stepper-item-heading" }, this.heading || this.itemTitle), h("span", { class: "stepper-item-description" }, this.description || this.itemSubtitle))), h("div", { class: "stepper-item-content" }, h("slot", null)))));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  updateActiveItemOnChange(event) {
    if (event.target === this.parentStepperEl ||
      event.composedPath().includes(this.parentStepperEl)) {
      this.selectedPosition = event.detail.position;
      this.determineSelectedItem();
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  async setFocus() {
    var _a;
    (_a = (this.layout === "vertical" ? this.el : this.headerEl)) === null || _a === void 0 ? void 0 : _a.focus();
  }
  renderIcon() {
    const path = this.selected
      ? "circleF"
      : this.error
        ? "exclamationMarkCircleF"
        : this.complete
          ? "checkCircleF"
          : "circle";
    return h("calcite-icon", { class: "stepper-item-icon", icon: path, scale: "s" });
  }
  determineSelectedItem() {
    this.selected = !this.disabled && this.itemPosition === this.selectedPosition;
  }
  registerStepperItem() {
    this.calciteInternalStepperItemRegister.emit({
      position: this.itemPosition
    });
  }
  getItemPosition() {
    var _a;
    return Array.from((_a = this.parentStepperEl) === null || _a === void 0 ? void 0 : _a.querySelectorAll("calcite-stepper-item")).indexOf(this.el);
  }
  static get is() { return "calcite-stepper-item"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["stepper-item.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["stepper-item.css"]
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
              "text": "Use `selected` instead."
            }],
          "text": "When `true`, the component is selected."
        },
        "attribute": "active",
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
      "complete": {
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
          "text": "When `true`, the step has been completed."
        },
        "attribute": "complete",
        "reflect": true,
        "defaultValue": "false"
      },
      "error": {
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
          "text": "When `true`, the component contains an error that requires resolution from the user."
        },
        "attribute": "error",
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
      "itemTitle": {
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
              "name": "deprecated",
              "text": "use `heading` instead."
            }],
          "text": "The component header text."
        },
        "attribute": "item-title",
        "reflect": false
      },
      "heading": {
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
          "text": "The component header text."
        },
        "attribute": "heading",
        "reflect": false
      },
      "itemSubtitle": {
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
              "name": "deprecated",
              "text": "use `description` instead."
            }],
          "text": "A description for the component. Displays below the header text."
        },
        "attribute": "item-subtitle",
        "reflect": false
      },
      "description": {
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
          "text": "A description for the component. Displays below the header text."
        },
        "attribute": "description",
        "reflect": false
      },
      "layout": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "Extract<\"horizontal\" | \"vertical\", Layout>",
          "resolved": "\"horizontal\" | \"vertical\"",
          "references": {
            "Extract": {
              "location": "global"
            },
            "Layout": {
              "location": "import",
              "path": "../interfaces"
            }
          }
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
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"horizontal\""
      },
      "icon": {
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
          "text": ""
        },
        "attribute": "icon",
        "reflect": false,
        "defaultValue": "false"
      },
      "numbered": {
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
          "text": ""
        },
        "attribute": "numbered",
        "reflect": false,
        "defaultValue": "false"
      },
      "scale": {
        "type": "string",
        "mutable": true,
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
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      }
    };
  }
  static get states() {
    return {
      "effectiveLocale": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteInternalStepperItemKeyEvent",
        "name": "calciteInternalStepperItemKeyEvent",
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
          "original": "StepperItemKeyEventDetail",
          "resolved": "StepperItemKeyEventDetail",
          "references": {
            "StepperItemKeyEventDetail": {
              "location": "import",
              "path": "../stepper/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalStepperItemSelect",
        "name": "calciteInternalStepperItemSelect",
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
          "original": "StepperItemEventDetail",
          "resolved": "StepperItemEventDetail",
          "references": {
            "StepperItemEventDetail": {
              "location": "import",
              "path": "../stepper/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalUserRequestedStepperItemSelect",
        "name": "calciteInternalUserRequestedStepperItemSelect",
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
          "original": "StepperItemChangeEventDetail",
          "resolved": "StepperItemChangeEventDetail",
          "references": {
            "StepperItemChangeEventDetail": {
              "location": "import",
              "path": "../stepper/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalStepperItemRegister",
        "name": "calciteInternalStepperItemRegister",
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
          "original": "StepperItemEventDetail",
          "resolved": "StepperItemEventDetail",
          "references": {
            "StepperItemEventDetail": {
              "location": "import",
              "path": "../stepper/interfaces"
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
          "text": "",
          "tags": []
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
        "propName": "selected",
        "methodName": "selectedHandler"
      }, {
        "propName": "disabled",
        "methodName": "disabledWatcher"
      }, {
        "propName": "effectiveLocale",
        "methodName": "effectiveLocaleWatcher"
      }];
  }
  static get listeners() {
    return [{
        "name": "calciteInternalStepperItemChange",
        "method": "updateActiveItemOnChange",
        "target": "body",
        "capture": false,
        "passive": false
      }];
  }
}
