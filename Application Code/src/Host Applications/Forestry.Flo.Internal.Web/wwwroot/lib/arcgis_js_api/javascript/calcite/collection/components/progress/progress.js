/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
export class Progress {
  constructor() {
    /**
     * Specifies the component type.
     *
     * Use `"indeterminate"` if finding actual progress value is impossible.
     *
     */
    this.type = "determinate";
    /** The component's progress value, with a range of 0.0 - 1.0. */
    this.value = 0;
    /** When `true` and for `"indeterminate"` progress bars, reverses the animation direction. */
    this.reversed = false;
  }
  render() {
    const isDeterminate = this.type === "determinate";
    const barStyles = isDeterminate ? { width: `${this.value * 100}%` } : {};
    return (h("div", { "aria-label": this.label || this.text, "aria-valuemax": 1, "aria-valuemin": 0, "aria-valuenow": this.value, role: "progressbar" }, h("div", { class: "track" }, h("div", { class: {
        bar: true,
        indeterminate: this.type === "indeterminate",
        reversed: this.reversed
      }, style: barStyles })), this.text ? h("div", { class: "text" }, this.text) : null));
  }
  static get is() { return "calcite-progress"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["progress.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["progress.css"]
    };
  }
  static get properties() {
    return {
      "type": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "\"indeterminate\" | \"determinate\"",
          "resolved": "\"determinate\" | \"indeterminate\"",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the component type.\n\nUse `\"indeterminate\"` if finding actual progress value is impossible."
        },
        "attribute": "type",
        "reflect": true,
        "defaultValue": "\"determinate\""
      },
      "value": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The component's progress value, with a range of 0.0 - 1.0."
        },
        "attribute": "value",
        "reflect": false,
        "defaultValue": "0"
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
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Accessible name for the component."
        },
        "attribute": "label",
        "reflect": false
      },
      "text": {
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
          "text": "Text that displays under the component's indicator."
        },
        "attribute": "text",
        "reflect": false
      },
      "reversed": {
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
          "text": "When `true` and for `\"indeterminate\"` progress bars, reverses the animation direction."
        },
        "attribute": "reversed",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get elementRef() { return "el"; }
}
