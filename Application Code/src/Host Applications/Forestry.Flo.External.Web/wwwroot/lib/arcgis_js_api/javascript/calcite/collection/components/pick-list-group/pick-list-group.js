/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Fragment } from "@stencil/core";
import { CSS, SLOTS } from "./resources";
import { HEADING_LEVEL } from "./resources";
import { getSlotted } from "../../utils/dom";
import { Heading, constrainHeadingLevel } from "../functional/Heading";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding `calcite-pick-list-item` elements.
 */
export class PickListGroup {
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
  render() {
    var _a;
    const { el, groupTitle, headingLevel } = this;
    const hasParentItem = getSlotted(el, SLOTS.parentItem) !== null;
    const sectionClasses = {
      [CSS.container]: true,
      [CSS.indented]: hasParentItem
    };
    const title = groupTitle;
    const parentLevel = (_a = el.closest("calcite-pick-list")) === null || _a === void 0 ? void 0 : _a.headingLevel;
    const relativeLevel = parentLevel ? constrainHeadingLevel(parentLevel + 1) : null;
    const level = headingLevel || relativeLevel || HEADING_LEVEL;
    return (h(Fragment, null, title ? (h(Heading, { class: CSS.heading, level: level }, title)) : null, h("slot", { name: SLOTS.parentItem }), h("section", { class: sectionClasses }, h("slot", null))));
  }
  static get is() { return "calcite-pick-list-group"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["pick-list-group.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["pick-list-group.css"]
    };
  }
  static get properties() {
    return {
      "groupTitle": {
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
          "text": "Specifies the title for all nested `calcite-pick-list-item`s."
        },
        "attribute": "group-title",
        "reflect": true
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
      }
    };
  }
  static get elementRef() { return "el"; }
}
