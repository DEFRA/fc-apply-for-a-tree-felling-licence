/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { createObserver } from "../../utils/observers";
export class Option {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    this.mutationObserver = createObserver("mutation", () => {
      this.ensureTextContentDependentProps();
      this.calciteInternalOptionChange.emit();
    });
  }
  handlePropChange(_newValue, _oldValue, propName) {
    if (propName === "label" || propName === "value") {
      this.ensureTextContentDependentProps();
    }
    this.calciteInternalOptionChange.emit();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  ensureTextContentDependentProps() {
    const { el: { textContent } } = this;
    if (!this.label || this.label === this.internallySetLabel) {
      this.label = textContent;
      this.internallySetLabel = textContent;
    }
    if (!this.value || this.value === this.internallySetValue) {
      this.value = textContent;
      this.internallySetValue = textContent;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    this.ensureTextContentDependentProps();
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, {
      attributeFilter: ["label", "value"],
      characterData: true,
      childList: true,
      subtree: true
    });
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  //--------------------------------------------------------------------------
  //
  //  Render Methods
  //
  //--------------------------------------------------------------------------
  render() {
    return h("slot", null, this.label);
  }
  static get is() { return "calcite-option"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["option.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["option.css"]
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
      "label": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Accessible name for the component."
        },
        "attribute": "label",
        "reflect": false
      },
      "selected": {
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
          "text": "When `true`, the component is selected."
        },
        "attribute": "selected",
        "reflect": true
      },
      "value": {
        "type": "any",
        "mutable": true,
        "complexType": {
          "original": "any",
          "resolved": "any",
          "references": {}
        },
        "required": false,
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
        "method": "calciteInternalOptionChange",
        "name": "calciteInternalOptionChange",
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
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "disabled",
        "methodName": "handlePropChange"
      }, {
        "propName": "label",
        "methodName": "handlePropChange"
      }, {
        "propName": "selected",
        "methodName": "handlePropChange"
      }, {
        "propName": "value",
        "methodName": "handlePropChange"
      }];
  }
}
