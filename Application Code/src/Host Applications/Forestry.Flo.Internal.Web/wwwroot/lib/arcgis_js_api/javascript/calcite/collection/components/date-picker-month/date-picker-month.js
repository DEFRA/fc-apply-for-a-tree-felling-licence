/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Host, h } from "@stencil/core";
import { inRange, sameDate, dateFromRange } from "../../utils/date";
export class DatePickerMonth {
  constructor() {
    /** Date currently active.*/
    this.activeDate = new Date();
    //--------------------------------------------------------------------------
    //
    //  Event Listeners
    //
    //--------------------------------------------------------------------------
    this.keyDownHandler = (event) => {
      if (event.defaultPrevented) {
        return;
      }
      const isRTL = this.el.dir === "rtl";
      switch (event.key) {
        case "ArrowUp":
          event.preventDefault();
          this.addDays(-7);
          break;
        case "ArrowRight":
          event.preventDefault();
          this.addDays(isRTL ? -1 : 1);
          break;
        case "ArrowDown":
          event.preventDefault();
          this.addDays(7);
          break;
        case "ArrowLeft":
          event.preventDefault();
          this.addDays(isRTL ? 1 : -1);
          break;
        case "PageUp":
          event.preventDefault();
          this.addMonths(-1);
          break;
        case "PageDown":
          event.preventDefault();
          this.addMonths(1);
          break;
        case "Home":
          event.preventDefault();
          this.activeDate.setDate(1);
          this.addDays();
          break;
        case "End":
          event.preventDefault();
          this.activeDate.setDate(new Date(this.activeDate.getFullYear(), this.activeDate.getMonth() + 1, 0).getDate());
          this.addDays();
          break;
        case "Enter":
        case " ":
          event.preventDefault();
          break;
        case "Tab":
          this.activeFocus = false;
      }
    };
    /**
     * Once user is not interacting via keyboard,
     * disable auto focusing of active date
     */
    this.disableActiveFocus = () => {
      this.activeFocus = false;
    };
    this.dayHover = (event) => {
      const target = event.target;
      if (target.disabled) {
        this.calciteInternalDatePickerMouseOut.emit();
      }
      else {
        this.calciteInternalDatePickerHover.emit(target.value);
      }
      event.stopPropagation();
    };
    this.daySelect = (event) => {
      const target = event.target;
      this.calciteDatePickerSelect.emit(target.value);
    };
  }
  mouseoutHandler() {
    this.calciteInternalDatePickerMouseOut.emit();
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  render() {
    const month = this.activeDate.getMonth();
    const year = this.activeDate.getFullYear();
    const startOfWeek = this.localeData.weekStart % 7;
    const { abbreviated, short, narrow } = this.localeData.days;
    const weekDays = this.scale === "s" ? narrow || short || abbreviated : short || abbreviated || narrow;
    const adjustedWeekDays = [...weekDays.slice(startOfWeek, 7), ...weekDays.slice(0, startOfWeek)];
    const curMonDays = this.getCurrentMonthDays(month, year);
    const prevMonDays = this.getPrevMonthdays(month, year, startOfWeek);
    const nextMonDays = this.getNextMonthDays(month, year, startOfWeek);
    const days = [
      ...prevMonDays.map((day) => {
        const date = new Date(year, month - 1, day);
        return this.renderDateDay(false, day, date);
      }),
      ...curMonDays.map((day) => {
        const date = new Date(year, month, day);
        const active = sameDate(date, this.activeDate);
        return this.renderDateDay(active, day, date, true, true);
      }),
      ...nextMonDays.map((day) => {
        const date = new Date(year, month + 1, day);
        return this.renderDateDay(false, day, date);
      })
    ];
    const weeks = [];
    for (let i = 0; i < days.length; i += 7) {
      weeks.push(days.slice(i, i + 7));
    }
    return (h(Host, { onFocusOut: this.disableActiveFocus, onKeyDown: this.keyDownHandler }, h("div", { class: "calender", role: "grid" }, h("div", { class: "week-headers", role: "row" }, adjustedWeekDays.map((weekday) => (h("span", { class: "week-header", role: "columnheader" }, weekday)))), weeks.map((days) => (h("div", { class: "week-days", role: "row" }, days))))));
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  /**
   * Add n months to the current month
   *
   * @param step
   */
  addMonths(step) {
    const nextDate = new Date(this.activeDate);
    nextDate.setMonth(this.activeDate.getMonth() + step);
    this.calciteDatePickerActiveDateChange.emit(dateFromRange(nextDate, this.min, this.max));
    this.activeFocus = true;
  }
  /**
   * Add n days to the current date
   *
   * @param step
   */
  addDays(step = 0) {
    const nextDate = new Date(this.activeDate);
    nextDate.setDate(this.activeDate.getDate() + step);
    this.calciteDatePickerActiveDateChange.emit(dateFromRange(nextDate, this.min, this.max));
    this.activeFocus = true;
  }
  /**
   * Get dates for last days of the previous month
   *
   * @param month
   * @param year
   * @param startOfWeek
   */
  getPrevMonthdays(month, year, startOfWeek) {
    const lastDate = new Date(year, month, 0);
    const date = lastDate.getDate();
    const day = lastDate.getDay();
    const days = [];
    if (day - 6 === startOfWeek) {
      return days;
    }
    for (let i = lastDate.getDay() - startOfWeek; i >= 0; i--) {
      days.push(date - i);
    }
    return days;
  }
  /**
   * Get dates for the current month
   *
   * @param month
   * @param year
   */
  getCurrentMonthDays(month, year) {
    const num = new Date(year, month + 1, 0).getDate();
    const days = [];
    for (let i = 0; i < num; i++) {
      days.push(i + 1);
    }
    return days;
  }
  /**
   * Get dates for first days of the next month
   *
   * @param month
   * @param year
   * @param startOfWeek
   */
  getNextMonthDays(month, year, startOfWeek) {
    const endDay = new Date(year, month + 1, 0).getDay();
    const days = [];
    if (endDay === (startOfWeek + 6) % 7) {
      return days;
    }
    for (let i = 0; i < (6 - (endDay - startOfWeek)) % 7; i++) {
      days.push(i + 1);
    }
    return days;
  }
  /**
   * Determine if the date is in between the start and end dates
   *
   * @param date
   */
  betweenSelectedRange(date) {
    return !!(this.startDate &&
      this.endDate &&
      date > this.startDate &&
      date < this.endDate &&
      !this.isRangeHover(date));
  }
  /**
   * Determine if the date should be in selected state
   *
   * @param date
   */
  isSelected(date) {
    return !!(sameDate(date, this.selectedDate) ||
      (this.startDate && sameDate(date, this.startDate)) ||
      (this.endDate && sameDate(date, this.endDate)));
  }
  /**
   * Determine if the date is the start of the date range
   *
   * @param date
   */
  isStartOfRange(date) {
    return !!(this.startDate &&
      !sameDate(this.startDate, this.endDate) &&
      sameDate(this.startDate, date) &&
      !this.isEndOfRange(date));
  }
  isEndOfRange(date) {
    return !!((this.endDate && !sameDate(this.startDate, this.endDate) && sameDate(this.endDate, date)) ||
      (!this.endDate &&
        this.hoverRange &&
        sameDate(this.startDate, this.hoverRange.end) &&
        sameDate(date, this.hoverRange.end)));
  }
  /**
   * Render calcite-date-picker-day
   *
   * @param active
   * @param day
   * @param date
   * @param currentMonth
   * @param ref
   */
  renderDateDay(active, day, date, currentMonth, ref) {
    var _a;
    const isFocusedOnStart = this.isFocusedOnStart();
    const isHoverInRange = this.isHoverInRange() ||
      (!this.endDate && this.hoverRange && sameDate((_a = this.hoverRange) === null || _a === void 0 ? void 0 : _a.end, this.startDate));
    return (h("calcite-date-picker-day", { active: active, class: {
        "hover--inside-range": this.startDate && isHoverInRange,
        "hover--outside-range": this.startDate && !isHoverInRange,
        "focused--start": isFocusedOnStart,
        "focused--end": !isFocusedOnStart
      }, currentMonth: currentMonth, day: day, disabled: !inRange(date, this.min, this.max), endOfRange: this.isEndOfRange(date), highlighted: this.betweenSelectedRange(date), key: date.toDateString(), onCalciteDaySelect: this.daySelect, onCalciteInternalDayHover: this.dayHover, range: !!this.startDate && !!this.endDate && !sameDate(this.startDate, this.endDate), rangeHover: this.isRangeHover(date), ref: (el) => {
        // when moving via keyboard, focus must be updated on active date
        if (ref && active && this.activeFocus) {
          el === null || el === void 0 ? void 0 : el.focus();
        }
      }, scale: this.scale, selected: this.isSelected(date), startOfRange: this.isStartOfRange(date), value: date }));
  }
  isFocusedOnStart() {
    var _a;
    return ((_a = this.hoverRange) === null || _a === void 0 ? void 0 : _a.focused) === "start";
  }
  isHoverInRange() {
    if (!this.hoverRange) {
      return false;
    }
    const { start, end } = this.hoverRange;
    return !!((!this.isFocusedOnStart() && this.startDate && (!this.endDate || end < this.endDate)) ||
      (this.isFocusedOnStart() && this.startDate && start > this.startDate));
  }
  isRangeHover(date) {
    if (!this.hoverRange) {
      return false;
    }
    const { start, end } = this.hoverRange;
    const isStart = this.isFocusedOnStart();
    const insideRange = this.isHoverInRange();
    const cond1 = insideRange &&
      ((!isStart && date > this.startDate && (date < end || sameDate(date, end))) ||
        (isStart && date < this.endDate && (date > start || sameDate(date, start))));
    const cond2 = !insideRange &&
      ((!isStart && date >= this.endDate && (date < end || sameDate(date, end))) ||
        (isStart &&
          (date < this.startDate || (this.endDate && sameDate(date, this.startDate))) &&
          (date > start || sameDate(date, start))));
    return cond1 || cond2;
  }
  static get is() { return "calcite-date-picker-month"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["date-picker-month.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["date-picker-month.css"]
    };
  }
  static get properties() {
    return {
      "selectedDate": {
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
          "text": "Already selected date."
        }
      },
      "activeDate": {
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
          "text": "Date currently active."
        },
        "defaultValue": "new Date()"
      },
      "startDate": {
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
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Start date currently active."
        }
      },
      "endDate": {
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
        "optional": true,
        "docs": {
          "tags": [],
          "text": "End date currently active"
        }
      },
      "min": {
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
          "text": "Minimum date of the calendar below which is disabled."
        }
      },
      "max": {
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
          "text": "Maximum date of the calendar above which is disabled."
        }
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
      "localeData": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "DateLocaleData",
          "resolved": "DateLocaleData",
          "references": {
            "DateLocaleData": {
              "location": "import",
              "path": "../date-picker/utils"
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
          "text": "CLDR locale data for current locale"
        }
      },
      "hoverRange": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "HoverRange",
          "resolved": "HoverRange",
          "references": {
            "HoverRange": {
              "location": "import",
              "path": "../../utils/date"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The range of dates currently being hovered"
        }
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteDatePickerSelect",
        "name": "calciteDatePickerSelect",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Event emitted when user selects the date."
        },
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        }
      }, {
        "method": "calciteInternalDatePickerHover",
        "name": "calciteInternalDatePickerHover",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Event emitted when user hovers the date."
        },
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        }
      }, {
        "method": "calciteDatePickerActiveDateChange",
        "name": "calciteDatePickerActiveDateChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Active date for the user keyboard access."
        },
        "complexType": {
          "original": "Date",
          "resolved": "Date",
          "references": {
            "Date": {
              "location": "global"
            }
          }
        }
      }, {
        "method": "calciteInternalDatePickerMouseOut",
        "name": "calciteInternalDatePickerMouseOut",
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
  static get listeners() {
    return [{
        "name": "pointerout",
        "method": "mouseoutHandler",
        "target": undefined,
        "capture": false,
        "passive": true
      }];
  }
}
