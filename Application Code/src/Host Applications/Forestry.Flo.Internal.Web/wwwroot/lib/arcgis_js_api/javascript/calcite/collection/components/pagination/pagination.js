/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Fragment } from "@stencil/core";
import { connectLocalized, disconnectLocalized, numberStringFormatter } from "../../utils/locale";
import { CSS, TEXT } from "./resources";
const maxPagesDisplayed = 5;
export class Pagination {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, number values are displayed with a group separator corresponding to the language and country format.
     */
    this.groupSeparator = false;
    /** Specifies the number of items per page. */
    this.num = 20;
    /** Specifies the starting item number. */
    this.start = 1;
    /** Specifies the total number of items. */
    this.total = 0;
    /**
     * Accessible name for the component's next button.
     *
     * @default "Next"
     */
    this.textLabelNext = TEXT.nextLabel;
    /**
     * Accessible name for the component's previous button.
     *
     * @default "Previous"
     */
    this.textLabelPrevious = TEXT.previousLabel;
    /** Specifies the size of the component. */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  State
    //
    //--------------------------------------------------------------------------
    this.effectiveLocale = "";
    this.previousClicked = () => {
      this.previousPage().then();
      this.emitUpdate();
    };
    this.nextClicked = () => {
      this.nextPage();
      this.emitUpdate();
    };
    /**
     * Returns a string representing the localized label value based on groupSeparator prop being on or off.
     *
     * @param value
     */
    this.determineGroupSeparator = (value) => {
      numberStringFormatter.numberFormatOptions = {
        locale: this.effectiveLocale,
        numberingSystem: this.numberingSystem,
        useGrouping: this.groupSeparator
      };
      return this.groupSeparator
        ? numberStringFormatter.localize(value.toString())
        : value.toString();
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectLocalized(this);
  }
  disconnectedCallback() {
    disconnectLocalized(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Go to the next page of results. */
  async nextPage() {
    this.start = Math.min(this.getLastStart(), this.start + this.num);
  }
  /** Go to the previous page of results. */
  async previousPage() {
    this.start = Math.max(1, this.start - this.num);
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  getLastStart() {
    const { total, num } = this;
    const lastStart = total % num === 0 ? total - num : Math.floor(total / num) * num;
    return lastStart + 1;
  }
  showLeftEllipsis() {
    return Math.floor(this.start / this.num) > 3;
  }
  showRightEllipsis() {
    return (this.total - this.start) / this.num > 3;
  }
  emitUpdate() {
    const changePayload = {
      start: this.start,
      total: this.total,
      num: this.num
    };
    this.calcitePaginationChange.emit(changePayload);
    this.calcitePaginationUpdate.emit(changePayload);
  }
  //--------------------------------------------------------------------------
  //
  //  Render Methods
  //
  //--------------------------------------------------------------------------
  renderPages() {
    const lastStart = this.getLastStart();
    let end;
    let nextStart;
    // if we don't need ellipses render the whole set
    if (this.total / this.num <= maxPagesDisplayed) {
      nextStart = 1 + this.num;
      end = lastStart - this.num;
    }
    else {
      // if we're within max pages of page 1
      if (this.start / this.num < maxPagesDisplayed - 1) {
        nextStart = 1 + this.num;
        end = 1 + 4 * this.num;
      }
      else {
        // if we're within max pages of last page
        if (this.start + 3 * this.num >= this.total) {
          nextStart = lastStart - 4 * this.num;
          end = lastStart - this.num;
        }
        else {
          nextStart = this.start - this.num;
          end = this.start + this.num;
        }
      }
    }
    const pages = [];
    while (nextStart <= end) {
      pages.push(nextStart);
      nextStart = nextStart + this.num;
    }
    return pages.map((page) => this.renderPage(page));
  }
  renderPage(start) {
    const page = Math.floor(start / this.num) + (this.num === 1 ? 0 : 1);
    const displayedPage = this.determineGroupSeparator(page);
    return (h("button", { class: {
        [CSS.page]: true,
        [CSS.selected]: start === this.start
      }, onClick: () => {
        this.start = start;
        this.emitUpdate();
      } }, displayedPage));
  }
  renderLeftEllipsis() {
    if (this.total / this.num > maxPagesDisplayed && this.showLeftEllipsis()) {
      return h("span", { class: `${CSS.ellipsis} ${CSS.ellipsisStart}` }, "\u2026");
    }
  }
  renderRightEllipsis() {
    if (this.total / this.num > maxPagesDisplayed && this.showRightEllipsis()) {
      return h("span", { class: `${CSS.ellipsis} ${CSS.ellipsisEnd}` }, "\u2026");
    }
  }
  render() {
    const { total, num, start } = this;
    const prevDisabled = num === 1 ? start <= num : start < num;
    const nextDisabled = num === 1 ? start + num > total : start + num > total;
    return (h(Fragment, null, h("button", { "aria-label": this.textLabelPrevious, class: {
        [CSS.previous]: true,
        [CSS.disabled]: prevDisabled
      }, disabled: prevDisabled, onClick: this.previousClicked }, h("calcite-icon", { flipRtl: true, icon: "chevronLeft", scale: "s" })), total > num ? this.renderPage(1) : null, this.renderLeftEllipsis(), this.renderPages(), this.renderRightEllipsis(), this.renderPage(this.getLastStart()), h("button", { "aria-label": this.textLabelNext, class: {
        [CSS.next]: true,
        [CSS.disabled]: nextDisabled
      }, disabled: nextDisabled, onClick: this.nextClicked }, h("calcite-icon", { flipRtl: true, icon: "chevronRight", scale: "s" }))));
  }
  static get is() { return "calcite-pagination"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["pagination.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["pagination.css"]
    };
  }
  static get properties() {
    return {
      "groupSeparator": {
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
          "text": "When `true`, number values are displayed with a group separator corresponding to the language and country format."
        },
        "attribute": "group-separator",
        "reflect": true,
        "defaultValue": "false"
      },
      "num": {
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
          "text": "Specifies the number of items per page."
        },
        "attribute": "num",
        "reflect": true,
        "defaultValue": "20"
      },
      "numberingSystem": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "NumberingSystem",
          "resolved": "\"arab\" | \"arabext\" | \"bali\" | \"beng\" | \"deva\" | \"fullwide\" | \"gujr\" | \"guru\" | \"hanidec\" | \"khmr\" | \"knda\" | \"laoo\" | \"latn\" | \"limb\" | \"mlym\" | \"mong\" | \"mymr\" | \"orya\" | \"tamldec\" | \"telu\" | \"thai\" | \"tibt\"",
          "references": {
            "NumberingSystem": {
              "location": "import",
              "path": "../../utils/locale"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the Unicode numeral system used by the component for localization."
        },
        "attribute": "numbering-system",
        "reflect": false
      },
      "start": {
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
          "text": "Specifies the starting item number."
        },
        "attribute": "start",
        "reflect": true,
        "defaultValue": "1"
      },
      "total": {
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
          "text": "Specifies the total number of items."
        },
        "attribute": "total",
        "reflect": true,
        "defaultValue": "0"
      },
      "textLabelNext": {
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
              "text": "\"Next\""
            }],
          "text": "Accessible name for the component's next button."
        },
        "attribute": "text-label-next",
        "reflect": false,
        "defaultValue": "TEXT.nextLabel"
      },
      "textLabelPrevious": {
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
              "text": "\"Previous\""
            }],
          "text": "Accessible name for the component's previous button."
        },
        "attribute": "text-label-previous",
        "reflect": false,
        "defaultValue": "TEXT.previousLabel"
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
        "method": "calcitePaginationUpdate",
        "name": "calcitePaginationUpdate",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use calcitePaginationChange instead"
            }],
          "text": "Emits when the selected page changes."
        },
        "complexType": {
          "original": "PaginationDetail",
          "resolved": "PaginationDetail",
          "references": {
            "PaginationDetail": {
              "location": "local"
            }
          }
        }
      }, {
        "method": "calcitePaginationChange",
        "name": "calcitePaginationChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "see",
              "text": "[PaginationDetail](https://github.com/Esri/calcite-components/blob/master/src/components/pagination/pagination.tsx#L23)"
            }],
          "text": "Emits when the selected page changes."
        },
        "complexType": {
          "original": "PaginationDetail",
          "resolved": "PaginationDetail",
          "references": {
            "PaginationDetail": {
              "location": "local"
            }
          }
        }
      }];
  }
  static get methods() {
    return {
      "nextPage": {
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
          "text": "Go to the next page of results.",
          "tags": []
        }
      },
      "previousPage": {
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
          "text": "Go to the previous page of results.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
}
