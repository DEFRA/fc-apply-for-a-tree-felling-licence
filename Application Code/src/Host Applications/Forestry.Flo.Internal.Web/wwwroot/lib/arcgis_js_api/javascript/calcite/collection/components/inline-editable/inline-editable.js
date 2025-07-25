/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { getElementProp, getSlotted } from "../../utils/dom";
import { TEXT, CSS } from "./resources";
import { connectLabel, disconnectLabel, getLabelText } from "../../utils/label";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding a `calcite-input`.
 */
export class InlineEditable {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Props
    //
    //--------------------------------------------------------------------------
    /** specify whether editing can be enabled */
    this.disabled = false;
    /** specify whether the wrapped input element is editable, defaults to false */
    this.editingEnabled = false;
    /** specify whether the confirm button should display a loading state, defaults to false */
    this.loading = false;
    /** specify whether save/cancel controls should be displayed when editingEnabled is true, defaults to false */
    this.controls = false;
    /**
     * specify text to be user for the enable editing button's aria-label, defaults to `Click to edit`
     *
     * @default "Click to edit"
     */
    this.intlEnableEditing = TEXT.intlEnablingEditing;
    /**
     * specify text to be user for the cancel editing button's aria-label, defaults to `Cancel`
     *
     * @default "Cancel"
     */
    this.intlCancelEditing = TEXT.intlCancelEditing;
    /**
     * specify text to be user for the confirm changes button's aria-label, defaults to `Save`
     *
     * @default "Save"
     */
    this.intlConfirmChanges = TEXT.intlConfirmChanges;
    this.mutationObserver = createObserver("mutation", () => this.mutationObserverCallback());
    this.enableEditing = () => {
      var _a, _b;
      this.valuePriorToEditing = (_a = this.inputElement) === null || _a === void 0 ? void 0 : _a.value;
      this.editingEnabled = true;
      (_b = this.inputElement) === null || _b === void 0 ? void 0 : _b.setFocus();
      this.calciteInternalInlineEditableEnableEditingChange.emit();
    };
    this.disableEditing = () => {
      this.editingEnabled = false;
    };
    this.cancelEditing = () => {
      if (this.inputElement) {
        this.inputElement.value = this.valuePriorToEditing;
      }
      this.disableEditing();
      this.enableEditingButton.setFocus();
      if (!this.editingEnabled && !!this.shouldEmitCancel) {
        this.calciteInlineEditableEditCancel.emit();
      }
    };
    this.escapeKeyHandler = async (event) => {
      var _a;
      if (event.defaultPrevented) {
        return;
      }
      if (event.key === "Escape") {
        event.preventDefault();
        this.cancelEditing();
      }
      if (event.key === "Tab" && this.shouldShowControls) {
        if (!event.shiftKey && event.target === this.inputElement) {
          event.preventDefault();
          this.cancelEditingButton.setFocus();
        }
        if (!!event.shiftKey && event.target === this.cancelEditingButton) {
          event.preventDefault();
          (_a = this.inputElement) === null || _a === void 0 ? void 0 : _a.setFocus();
        }
      }
    };
    this.cancelEditingHandler = async (event) => {
      event.preventDefault();
      this.cancelEditing();
    };
    this.enableEditingHandler = async (event) => {
      if (this.disabled ||
        event.target === this.cancelEditingButton ||
        event.target === this.confirmEditingButton) {
        return;
      }
      event.preventDefault();
      if (!this.editingEnabled) {
        this.enableEditing();
      }
    };
    this.confirmChangesHandler = async (event) => {
      event.preventDefault();
      this.calciteInlineEditableEditConfirm.emit();
      try {
        if (this.afterConfirm) {
          this.loading = true;
          await this.afterConfirm();
          this.disableEditing();
          this.enableEditingButton.setFocus();
        }
      }
      catch (error) {
      }
      finally {
        this.loading = false;
      }
    };
  }
  disabledWatcher(disabled) {
    if (this.inputElement) {
      this.inputElement.disabled = disabled;
    }
  }
  editingEnabledWatcher(newValue, oldValue) {
    if (this.inputElement) {
      this.inputElement.editingEnabled = newValue;
    }
    if (!newValue && !!oldValue) {
      this.shouldEmitCancel = true;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    connectLabel(this);
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true });
    this.mutationObserverCallback();
  }
  disconnectedCallback() {
    var _a;
    disconnectLabel(this);
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  render() {
    return (h("div", { class: CSS.wrapper, onClick: this.enableEditingHandler, onKeyDown: this.escapeKeyHandler }, h("div", { class: CSS.inputWrapper }, h("slot", null)), h("div", { class: CSS.controlsWrapper }, h("calcite-button", { appearance: "transparent", class: CSS.enableEditingButton, color: "neutral", disabled: this.disabled, iconStart: "pencil", label: this.intlEnableEditing, onClick: this.enableEditingHandler, ref: (el) => (this.enableEditingButton = el), scale: this.scale, style: {
        opacity: this.editingEnabled ? "0" : "1",
        width: this.editingEnabled ? "0" : "inherit"
      }, type: "button" }), this.shouldShowControls && [
      h("div", { class: CSS.cancelEditingButtonWrapper }, h("calcite-button", { appearance: "transparent", class: CSS.cancelEditingButton, color: "neutral", disabled: this.disabled, iconStart: "x", label: this.intlCancelEditing, onClick: this.cancelEditingHandler, ref: (el) => (this.cancelEditingButton = el), scale: this.scale, type: "button" })),
      h("calcite-button", { appearance: "solid", class: CSS.confirmChangesButton, color: "blue", disabled: this.disabled, iconStart: "check", label: this.intlConfirmChanges, loading: this.loading, onClick: this.confirmChangesHandler, ref: (el) => (this.confirmEditingButton = el), scale: this.scale, type: "button" })
    ])));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  blurHandler() {
    if (!this.controls) {
      this.disableEditing();
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  async setFocus() {
    var _a, _b;
    if (this.editingEnabled) {
      (_a = this.inputElement) === null || _a === void 0 ? void 0 : _a.setFocus();
    }
    else {
      (_b = this.enableEditingButton) === null || _b === void 0 ? void 0 : _b.setFocus();
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  mutationObserverCallback() {
    var _a;
    this.updateSlottedInput();
    this.scale =
      this.scale || ((_a = this.inputElement) === null || _a === void 0 ? void 0 : _a.scale) || getElementProp(this.el, "scale", undefined);
  }
  onLabelClick() {
    this.setFocus();
  }
  updateSlottedInput() {
    const inputElement = getSlotted(this.el, {
      matches: "calcite-input"
    });
    this.inputElement = inputElement;
    if (!inputElement) {
      return;
    }
    this.inputElement.disabled = this.disabled;
    this.inputElement.label = this.inputElement.label || getLabelText(this);
  }
  get shouldShowControls() {
    return this.editingEnabled && this.controls;
  }
  static get is() { return "calcite-inline-editable"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["inline-editable.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["inline-editable.css"]
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
          "text": "specify whether editing can be enabled"
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "editingEnabled": {
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
          "text": "specify whether the wrapped input element is editable, defaults to false"
        },
        "attribute": "editing-enabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "loading": {
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
          "text": "specify whether the confirm button should display a loading state, defaults to false"
        },
        "attribute": "loading",
        "reflect": true,
        "defaultValue": "false"
      },
      "controls": {
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
          "text": "specify whether save/cancel controls should be displayed when editingEnabled is true, defaults to false"
        },
        "attribute": "controls",
        "reflect": true,
        "defaultValue": "false"
      },
      "intlEnableEditing": {
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
              "text": "\"Click to edit\""
            }],
          "text": "specify text to be user for the enable editing button's aria-label, defaults to `Click to edit`"
        },
        "attribute": "intl-enable-editing",
        "reflect": true,
        "defaultValue": "TEXT.intlEnablingEditing"
      },
      "intlCancelEditing": {
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
              "text": "\"Cancel\""
            }],
          "text": "specify text to be user for the cancel editing button's aria-label, defaults to `Cancel`"
        },
        "attribute": "intl-cancel-editing",
        "reflect": true,
        "defaultValue": "TEXT.intlCancelEditing"
      },
      "intlConfirmChanges": {
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
              "text": "\"Save\""
            }],
          "text": "specify text to be user for the confirm changes button's aria-label, defaults to `Save`"
        },
        "attribute": "intl-confirm-changes",
        "reflect": true,
        "defaultValue": "TEXT.intlConfirmChanges"
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
        "optional": true,
        "docs": {
          "tags": [],
          "text": "specify the scale of the inline-editable component, defaults to the scale of the wrapped calcite-input or the scale of the closest wrapping component with a set scale"
        },
        "attribute": "scale",
        "reflect": true
      },
      "afterConfirm": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "() => Promise<void>",
          "resolved": "() => Promise<void>",
          "references": {
            "Promise": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "when controls, specify a callback to be executed prior to disabling editing. when provided, loading state will be handled automatically."
        }
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteInlineEditableEditCancel",
        "name": "calciteInlineEditableEditCancel",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emitted when the cancel button gets clicked."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInlineEditableEditConfirm",
        "name": "calciteInlineEditableEditConfirm",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emitted when the check button gets clicked."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalInlineEditableEnableEditingChange",
        "name": "calciteInternalInlineEditableEnableEditingChange",
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
          "text": "",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "disabled",
        "methodName": "disabledWatcher"
      }, {
        "propName": "editingEnabled",
        "methodName": "editingEnabledWatcher"
      }];
  }
  static get listeners() {
    return [{
        "name": "calciteInternalInputBlur",
        "method": "blurHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
