/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Fragment } from "@stencil/core";
import { CSS, ICONS, SLOTS, TEXT, HEADING_LEVEL } from "./resources";
import { getSlotted } from "../../utils/dom";
import { Heading, constrainHeadingLevel } from "../functional/Heading";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding text and a hyperlink.
 * @slot thumbnail - A slot for adding an HTML image element.
 */
export class Tip {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the component does not display.
     */
    this.dismissed = false;
    /**
     * When `true`, the close button is not present on the component.
     */
    this.nonDismissible = false;
    /**
     * When `true`, the component is selected if it has a parent `calcite-tip-manager`.
     *
     * Only one tip can be selected within the `calcite-tip-manager` parent.
     */
    this.selected = false;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.hideTip = () => {
      this.dismissed = true;
      this.calciteTipDismiss.emit();
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderHeader() {
    var _a;
    const { heading, headingLevel, el } = this;
    const parentLevel = (_a = el.closest("calcite-tip-manager")) === null || _a === void 0 ? void 0 : _a.headingLevel;
    const relativeLevel = parentLevel ? constrainHeadingLevel(parentLevel + 1) : null;
    const level = headingLevel || relativeLevel || HEADING_LEVEL;
    return heading ? (h("header", { class: CSS.header }, h(Heading, { class: CSS.heading, level: level }, heading))) : null;
  }
  renderDismissButton() {
    const { nonDismissible, hideTip, intlClose } = this;
    const text = intlClose || TEXT.close;
    return !nonDismissible ? (h("calcite-action", { class: CSS.close, icon: ICONS.close, onClick: hideTip, scale: "l", text: text })) : null;
  }
  renderImageFrame() {
    const { el } = this;
    return getSlotted(el, SLOTS.thumbnail) ? (h("div", { class: CSS.imageFrame, key: "thumbnail" }, h("slot", { name: SLOTS.thumbnail }))) : null;
  }
  renderInfoNode() {
    return (h("div", { class: CSS.info }, h("slot", null)));
  }
  renderContent() {
    return (h("div", { class: CSS.content }, this.renderImageFrame(), this.renderInfoNode()));
  }
  render() {
    return (h(Fragment, null, h("article", { class: CSS.container }, this.renderHeader(), this.renderContent()), this.renderDismissButton()));
  }
  static get is() { return "calcite-tip"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tip.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tip.css"]
    };
  }
  static get properties() {
    return {
      "dismissed": {
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
          "text": "When `true`, the component does not display."
        },
        "attribute": "dismissed",
        "reflect": true,
        "defaultValue": "false"
      },
      "nonDismissible": {
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
          "text": "When `true`, the close button is not present on the component."
        },
        "attribute": "non-dismissible",
        "reflect": true,
        "defaultValue": "false"
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
          "text": "When `true`, the component is selected if it has a parent `calcite-tip-manager`.\n\nOnly one tip can be selected within the `calcite-tip-manager` parent."
        },
        "attribute": "selected",
        "reflect": true,
        "defaultValue": "false"
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
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteTipDismiss",
        "name": "calciteTipDismiss",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits when the component has been dismissed."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }];
  }
  static get elementRef() { return "el"; }
}
