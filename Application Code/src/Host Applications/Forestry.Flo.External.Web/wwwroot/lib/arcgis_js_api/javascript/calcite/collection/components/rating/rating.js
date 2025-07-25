/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Fragment, h } from "@stencil/core";
import { guid } from "../../utils/guid";
import { connectLabel, disconnectLabel } from "../../utils/label";
import { connectForm, disconnectForm, HiddenFormInputSlot } from "../../utils/form";
import { TEXT } from "./resources";
import { updateHostInteraction } from "../../utils/interactive";
import { isActivationKey } from "../../utils/key";
export class Rating {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /** Specifies the size of the component. */
    this.scale = "m";
    /** The component's value. */
    this.value = 0;
    /** When `true`, the component's value can be read, but cannot be modified. */
    this.readOnly = false;
    /** When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /** When `true`, and if available, displays the `average` and/or `count` data summary in a `calcite-chip`. */
    this.showChip = false;
    /**
     * Accessible name for the component.
     *
     * @default "Rating"
     */
    this.intlRating = TEXT.rating;
    /**
     * Accessible name for each star. The `${num}` in the string will be replaced by the number.
     *
     * @default "Stars: ${num}"
     */
    this.intlStars = TEXT.stars;
    /**
     * When `true`, the component must have a value in order for the form to submit.
     *
     * @internal
     */
    this.required = false;
    this.onKeyboardPressed = (event) => {
      if (!this.required && isActivationKey(event.key)) {
        event.preventDefault();
        this.updateValue(0);
      }
    };
    this.onFocusChange = (selectedRatingValue) => {
      this.hasFocus = true;
      if (!this.required && this.focusValue === selectedRatingValue) {
        this.updateValue(0);
      }
      else {
        this.focusValue = selectedRatingValue;
      }
    };
    this.guid = `calcite-ratings-${guid()}`;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    connectLabel(this);
    connectForm(this);
  }
  disconnectedCallback() {
    disconnectLabel(this);
    disconnectForm(this);
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  blurHandler() {
    this.hasFocus = false;
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderStars() {
    return [1, 2, 3, 4, 5].map((i) => {
      const selected = this.value >= i;
      const average = this.average && !this.value && i <= this.average;
      const hovered = i <= this.hoverValue;
      const fraction = this.average && this.average + 1 - i;
      const partial = !this.value && !hovered && fraction > 0 && fraction < 1;
      const focused = this.hasFocus && this.focusValue === i;
      return (h("span", { class: { wrapper: true } }, h("label", { class: { star: true, focused, selected, average, hovered, partial }, htmlFor: `${this.guid}-${i}`, onPointerOver: () => {
          this.hoverValue = i;
        } }, h("calcite-icon", { "aria-hidden": "true", class: "icon", icon: selected || average || this.readOnly ? "star-f" : "star", scale: this.scale }), partial && (h("div", { class: "fraction", style: { width: `${fraction * 100}%` } }, h("calcite-icon", { icon: "star-f", scale: this.scale }))), h("span", { class: "visually-hidden" }, this.intlStars.replace("${num}", `${i}`))), h("input", { checked: i === this.value, class: "visually-hidden", disabled: this.disabled || this.readOnly, id: `${this.guid}-${i}`, name: this.guid, onChange: () => this.updateValue(i), onClick: (event) => 
        // click is fired from the component's label, so we treat this as an internal event
        event.stopPropagation(), onFocus: () => this.onFocusChange(i), onKeyDown: this.onKeyboardPressed, ref: (el) => (i === 1 || i === this.value) && (this.inputFocusRef = el), type: "radio", value: i })));
    });
  }
  render() {
    const { disabled, intlRating, showChip, scale, count, average } = this;
    return (h(Fragment, null, h("fieldset", { class: "fieldset", disabled: disabled, onBlur: () => (this.hoverValue = null), onPointerLeave: () => (this.hoverValue = null), onTouchEnd: () => (this.hoverValue = null) }, h("legend", { class: "visually-hidden" }, intlRating), this.renderStars()), (count || average) && showChip ? (h("calcite-chip", { scale: scale, value: count === null || count === void 0 ? void 0 : count.toString() }, !!average && h("span", { class: "number--average" }, average.toString()), !!count && h("span", { class: "number--count" }, "(", count === null || count === void 0 ? void 0 :
      count.toString(), ")"))) : null, h(HiddenFormInputSlot, { component: this })));
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  onLabelClick() {
    this.setFocus();
  }
  updateValue(value) {
    this.value = value;
    this.calciteRatingChange.emit({ value });
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.inputFocusRef) === null || _a === void 0 ? void 0 : _a.focus();
  }
  static get is() { return "calcite-rating"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["rating.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["rating.css"]
    };
  }
  static get properties() {
    return {
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
        "type": "number",
        "mutable": true,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The component's value."
        },
        "attribute": "value",
        "reflect": true,
        "defaultValue": "0"
      },
      "readOnly": {
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
          "text": "When `true`, the component's value can be read, but cannot be modified."
        },
        "attribute": "read-only",
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
      "showChip": {
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
          "text": "When `true`, and if available, displays the `average` and/or `count` data summary in a `calcite-chip`."
        },
        "attribute": "show-chip",
        "reflect": true,
        "defaultValue": "false"
      },
      "count": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the number of previous ratings to display."
        },
        "attribute": "count",
        "reflect": true
      },
      "average": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies a cumulative average from previous ratings to display."
        },
        "attribute": "average",
        "reflect": true
      },
      "name": {
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
          "text": "Specifies the name of the component on form submission."
        },
        "attribute": "name",
        "reflect": true
      },
      "intlRating": {
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
              "text": "\"Rating\""
            }],
          "text": "Accessible name for the component."
        },
        "attribute": "intl-rating",
        "reflect": false,
        "defaultValue": "TEXT.rating"
      },
      "intlStars": {
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
              "text": "\"Stars: ${num}\""
            }],
          "text": "Accessible name for each star. The `${num}` in the string will be replaced by the number."
        },
        "attribute": "intl-stars",
        "reflect": false,
        "defaultValue": "TEXT.stars"
      },
      "required": {
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
          "text": "When `true`, the component must have a value in order for the form to submit."
        },
        "attribute": "required",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get states() {
    return {
      "hoverValue": {},
      "focusValue": {},
      "hasFocus": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteRatingChange",
        "name": "calciteRatingChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component's value changes."
        },
        "complexType": {
          "original": "{ value: number }",
          "resolved": "{ value: number; }",
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
  static get listeners() {
    return [{
        "name": "blur",
        "method": "blurHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
