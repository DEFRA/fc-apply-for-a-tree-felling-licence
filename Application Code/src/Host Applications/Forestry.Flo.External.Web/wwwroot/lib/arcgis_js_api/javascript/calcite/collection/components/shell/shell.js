/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Fragment } from "@stencil/core";
import { CSS, SLOTS } from "./resources";
import { getSlotted } from "../../utils/dom";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding content to the component. This content will appear between any leading and trailing panels added to the component, such as a map.
 * @slot header - A slot for adding header content. This content will be positioned at the top of the component.
 * @slot footer - A slot for adding footer content. This content will be positioned at the bottom of the component.
 * @slot panel-start - A slot for adding the starting `calcite-shell-panel`.
 * @slot panel-end - A slot for adding the ending `calcite-shell-panel`.
 * @slot primary-panel - [DEPRECATED] A slot for adding the leading `calcite-shell-panel`.
 * @slot contextual-panel - [DEPRECATED] A slot for adding the trailing `calcite-shell-panel`.
 * @slot center-row - A slot for adding content to the center row.
 */
export class Shell {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * Positions the center content behind any `calcite-shell-panel`s.
     */
    this.contentBehind = false;
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
    const hasHeader = !!getSlotted(this.el, SLOTS.header);
    return hasHeader ? h("slot", { key: "header", name: SLOTS.header }) : null;
  }
  renderContent() {
    const defaultSlotNode = h("slot", { key: "default-slot" });
    const centerRowSlotNode = h("slot", { key: "center-row-slot", name: SLOTS.centerRow });
    const contentContainerKey = "content-container";
    const content = !!this.contentBehind
      ? [
        h("div", { class: {
            [CSS.content]: true,
            [CSS.contentBehind]: true
          }, key: contentContainerKey }, defaultSlotNode),
        centerRowSlotNode
      ]
      : [
        h("div", { class: CSS.content, key: contentContainerKey }, defaultSlotNode, centerRowSlotNode)
      ];
    return content;
  }
  renderFooter() {
    const hasFooter = !!getSlotted(this.el, SLOTS.footer);
    return hasFooter ? (h("div", { class: CSS.footer, key: "footer" }, h("slot", { name: SLOTS.footer }))) : null;
  }
  renderMain() {
    const primaryPanel = getSlotted(this.el, SLOTS.primaryPanel);
    const mainClasses = {
      [CSS.main]: true,
      [CSS.mainReversed]: (primaryPanel === null || primaryPanel === void 0 ? void 0 : primaryPanel.position) === "end"
    };
    return (h("div", { class: mainClasses }, h("slot", { name: SLOTS.primaryPanel }), h("slot", { name: SLOTS.panelStart }), this.renderContent(), h("slot", { name: SLOTS.panelEnd }), h("slot", { name: SLOTS.contextualPanel })));
  }
  render() {
    return (h(Fragment, null, this.renderHeader(), this.renderMain(), this.renderFooter()));
  }
  static get is() { return "calcite-shell"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["shell.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["shell.css"]
    };
  }
  static get properties() {
    return {
      "contentBehind": {
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
          "text": "Positions the center content behind any `calcite-shell-panel`s."
        },
        "attribute": "content-behind",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get elementRef() { return "el"; }
}
