/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Host, h } from "@stencil/core";
import { closestElementCrossShadowBoundary, getElementDir } from "../../utils/dom";
import { CSS_UTILITY } from "../../utils/resources";
import { updateHostInteraction } from "../../utils/interactive";
import { isActivationKey } from "../../utils/key";
import { numberStringFormatter } from "../../utils/locale";
export class DatePickerDay {
  constructor() {
    /** Date is outside of range and can't be selected */
    this.disabled = false;
    /** Date is in the current month. */
    this.currentMonth = false;
    /** Date is the current selected date of the picker */
    this.selected = false;
    /** Date is currently highlighted as part of the range */
    this.highlighted = false;
    /** Showing date range */
    this.range = false;
    /** Date is the start of date range */
    this.startOfRange = false;
    /** Date is the end of date range */
    this.endOfRange = false;
    /** Date is being hovered and within the set range */
    this.rangeHover = false;
    /** Date is actively in focus for keyboard navigation */
    this.active = false;
    //--------------------------------------------------------------------------
    //
    //  Event Listeners
    //
    //--------------------------------------------------------------------------
    this.onClick = () => {
      !this.disabled && this.calciteDaySelect.emit();
    };
    this.keyDownHandler = (event) => {
      if (isActivationKey(event.key)) {
        !this.disabled && this.calciteDaySelect.emit();
        event.preventDefault();
      }
    };
  }
  mouseoverHandler() {
    this.calciteInternalDayHover.emit();
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentWillLoad() {
    this.parentDatePickerEl = closestElementCrossShadowBoundary(this.el, "calcite-date-picker");
  }
  render() {
    if (this.parentDatePickerEl) {
      const { numberingSystem, lang: locale } = this.parentDatePickerEl;
      numberStringFormatter.numberFormatOptions = {
        useGrouping: false,
        ...(numberingSystem && { numberingSystem }),
        ...(locale && { locale })
      };
    }
    const formattedDay = numberStringFormatter.localize(String(this.day));
    const dir = getElementDir(this.el);
    return (h(Host, { onClick: this.onClick, onKeyDown: this.keyDownHandler, role: "gridcell" }, h("div", { class: { "day-v-wrapper": true, [CSS_UTILITY.rtl]: dir === "rtl" } }, h("div", { class: "day-wrapper" }, h("span", { class: "day" }, h("span", { class: "text" }, formattedDay))))));
  }
  componentDidRender() {
    updateHostInteraction(this, this.isTabbable);
  }
  isTabbable() {
    return this.active;
  }
  static get is() { return "calcite-date-picker-day"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["date-picker-day.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["date-picker-day.css"]
    };
  }
  static get properties() {
    return {
      "day": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "number",
          "resolved": "number",
          "references": {}
        },
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Day of the month to be shown."
        },
        "attribute": "day",
        "reflect": false
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
          "text": "Date is outside of range and can't be selected"
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "currentMonth": {
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
          "text": "Date is in the current month."
        },
        "attribute": "current-month",
        "reflect": true,
        "defaultValue": "false"
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
          "text": "Date is the current selected date of the picker"
        },
        "attribute": "selected",
        "reflect": true,
        "defaultValue": "false"
      },
      "highlighted": {
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
          "text": "Date is currently highlighted as part of the range"
        },
        "attribute": "highlighted",
        "reflect": true,
        "defaultValue": "false"
      },
      "range": {
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
          "text": "Showing date range"
        },
        "attribute": "range",
        "reflect": true,
        "defaultValue": "false"
      },
      "startOfRange": {
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
          "text": "Date is the start of date range"
        },
        "attribute": "start-of-range",
        "reflect": true,
        "defaultValue": "false"
      },
      "endOfRange": {
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
          "text": "Date is the end of date range"
        },
        "attribute": "end-of-range",
        "reflect": true,
        "defaultValue": "false"
      },
      "rangeHover": {
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
          "text": "Date is being hovered and within the set range"
        },
        "attribute": "range-hover",
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
          "text": "Date is actively in focus for keyboard navigation"
        },
        "attribute": "active",
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
          "text": "specify the scale of the date picker"
        },
        "attribute": "scale",
        "reflect": true
      },
      "value": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Date value for the day."
        }
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteDaySelect",
        "name": "calciteDaySelect",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emitted when user selects day"
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalDayHover",
        "name": "calciteInternalDayHover",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Emitted when user hovers over a day"
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }];
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "pointerover",
        "method": "mouseoverHandler",
        "target": undefined,
        "capture": false,
        "passive": true
      }];
  }
}
