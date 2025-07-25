/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { CSS, ICONS, TEXT, HEADING_LEVEL } from "./resources";
import { getElementDir, toAriaBoolean } from "../../utils/dom";
import { Heading } from "../functional/Heading";
import { createObserver } from "../../utils/observers";
/**
 * @slot - A slot for adding `calcite-tip`s.
 */
export class TipManager {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, does not display or position the component.
     */
    this.closed = false;
    this.mutationObserver = createObserver("mutation", () => this.setUpTips());
    this.hideTipManager = () => {
      this.closed = true;
      this.calciteTipManagerToggle.emit();
      this.calciteTipManagerClose.emit();
    };
    this.previousClicked = () => {
      this.previousTip();
    };
    this.nextClicked = () => {
      this.nextTip();
    };
    this.tipManagerKeyDownHandler = (event) => {
      if (event.target !== this.container) {
        return;
      }
      switch (event.key) {
        case "ArrowRight":
          event.preventDefault();
          this.nextTip();
          break;
        case "ArrowLeft":
          event.preventDefault();
          this.previousTip();
          break;
        case "Home":
          event.preventDefault();
          this.selectedIndex = 0;
          break;
        case "End":
          event.preventDefault();
          this.selectedIndex = this.total - 1;
          break;
      }
    };
    this.storeContainerRef = (el) => {
      this.container = el;
    };
  }
  closedChangeHandler() {
    this.direction = null;
    this.calciteTipManagerToggle.emit();
  }
  selectedChangeHandler() {
    this.showSelectedTip();
    this.updateGroupTitle();
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    this.setUpTips();
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /** Selects the next `calcite-tip` to display. */
  async nextTip() {
    this.direction = "advancing";
    const nextIndex = this.selectedIndex + 1;
    this.selectedIndex = (nextIndex + this.total) % this.total;
  }
  /** Selects the previous `calcite-tip` to display. */
  async previousTip() {
    this.direction = "retreating";
    const previousIndex = this.selectedIndex - 1;
    this.selectedIndex = (previousIndex + this.total) % this.total;
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  setUpTips() {
    const tips = Array.from(this.el.querySelectorAll("calcite-tip"));
    this.total = tips.length;
    if (this.total === 0) {
      return;
    }
    const selectedTip = this.el.querySelector("calcite-tip[selected]");
    this.tips = tips;
    this.selectedIndex = selectedTip ? tips.indexOf(selectedTip) : 0;
    tips.forEach((tip) => {
      tip.nonDismissible = true;
    });
    this.showSelectedTip();
    this.updateGroupTitle();
  }
  showSelectedTip() {
    this.tips.forEach((tip, index) => {
      const isSelected = this.selectedIndex === index;
      tip.selected = isSelected;
      tip.hidden = !isSelected;
    });
  }
  updateGroupTitle() {
    const selectedTip = this.tips[this.selectedIndex];
    const tipParent = selectedTip.closest("calcite-tip-group");
    this.groupTitle = (tipParent === null || tipParent === void 0 ? void 0 : tipParent.groupTitle) || this.intlDefaultTitle || TEXT.defaultGroupTitle;
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderPagination() {
    const dir = getElementDir(this.el);
    const { selectedIndex, tips, total, intlNext, intlPrevious, intlPaginationLabel } = this;
    const nextLabel = intlNext || TEXT.next;
    const previousLabel = intlPrevious || TEXT.previous;
    const paginationLabel = intlPaginationLabel || TEXT.defaultPaginationLabel;
    return tips.length > 1 ? (h("footer", { class: CSS.pagination }, h("calcite-action", { class: CSS.pagePrevious, icon: dir === "ltr" ? ICONS.chevronLeft : ICONS.chevronRight, onClick: this.previousClicked, scale: "m", text: previousLabel }), h("span", { class: CSS.pagePosition }, `${paginationLabel} ${selectedIndex + 1}/${total}`), h("calcite-action", { class: CSS.pageNext, icon: dir === "ltr" ? ICONS.chevronRight : ICONS.chevronLeft, onClick: this.nextClicked, scale: "m", text: nextLabel }))) : null;
  }
  render() {
    const { closed, direction, headingLevel, groupTitle, selectedIndex, intlClose, total } = this;
    const closeLabel = intlClose || TEXT.close;
    if (total === 0) {
      return null;
    }
    return (h("section", { "aria-hidden": toAriaBoolean(closed), class: CSS.container, hidden: closed, onKeyDown: this.tipManagerKeyDownHandler, ref: this.storeContainerRef, tabIndex: 0 }, h("header", { class: CSS.header }, h(Heading, { class: CSS.heading, level: headingLevel || HEADING_LEVEL }, groupTitle), h("calcite-action", { class: CSS.close, onClick: this.hideTipManager, scale: "m", text: closeLabel }, h("calcite-icon", { icon: ICONS.close, scale: "m" }))), h("div", { class: {
        [CSS.tipContainer]: true,
        [CSS.tipContainerAdvancing]: !closed && direction === "advancing",
        [CSS.tipContainerRetreating]: !closed && direction === "retreating"
      }, key: selectedIndex, tabIndex: 0 }, h("slot", null)), this.renderPagination()));
  }
  static get is() { return "calcite-tip-manager"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tip-manager.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tip-manager.css"]
    };
  }
  static get properties() {
    return {
      "closed": {
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
          "text": "When `true`, does not display or position the component."
        },
        "attribute": "closed",
        "reflect": true,
        "defaultValue": "false"
      },
      "headingLevel": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "HeadingLevel",
          "resolved": "1 | 2 | 3 | 4 | 5 | 6",
          "references": {
            "HeadingLevel": {
              "location": "import",
              "path": "../functional/Heading"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the number at which section headings should start."
        },
        "attribute": "heading-level",
        "reflect": true
      },
      "intlClose": {
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
          "text": "Accessible name for the component's close button."
        },
        "attribute": "intl-close",
        "reflect": false
      },
      "intlDefaultTitle": {
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
          "text": "Accessible name for the `calcite-tip-group` title."
        },
        "attribute": "intl-default-title",
        "reflect": false
      },
      "intlNext": {
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
          "text": "Accessible name for navigating to the next tip."
        },
        "attribute": "intl-next",
        "reflect": false
      },
      "intlPaginationLabel": {
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
          "text": "Text that accompanies the component's pagination."
        },
        "attribute": "intl-pagination-label",
        "reflect": false
      },
      "intlPrevious": {
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
          "text": "Accessible name for navigating to the previous tip."
        },
        "attribute": "intl-previous",
        "reflect": false
      }
    };
  }
  static get states() {
    return {
      "selectedIndex": {},
      "tips": {},
      "total": {},
      "direction": {},
      "groupTitle": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteTipManagerToggle",
        "name": "calciteTipManagerToggle",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `calciteTipManagerClose` instead."
            }],
          "text": "Emits when the component has been open or closed."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteTipManagerClose",
        "name": "calciteTipManagerClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits when the component has been closed."
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
      "nextTip": {
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
          "text": "Selects the next `calcite-tip` to display.",
          "tags": []
        }
      },
      "previousTip": {
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
          "text": "Selects the previous `calcite-tip` to display.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "closed",
        "methodName": "closedChangeHandler"
      }, {
        "propName": "selectedIndex",
        "methodName": "selectedChangeHandler"
      }];
  }
}
