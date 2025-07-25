/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { ICONS, SLOTS, TEXT } from "./resources";
import { Fragment } from "@stencil/core/internal";
import { getSlotted } from "../../utils/dom";
import { SLOTS as ACTION_MENU_SLOTS } from "../action-menu/resources";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding a group of `calcite-action`s.
 * @slot menu-actions - A slot for adding an overflow menu with `calcite-action`s inside a `calcite-dropdown`.
 * @slot menu-tooltip - A slot for adding a `calcite-tooltip` for the menu.
 */
export class ActionGroup {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the component is expanded.
     */
    this.expanded = false;
    /**
     * Indicates the layout of the component.
     */
    this.layout = "vertical";
    /**
     * When `true`, the `calcite-action-menu` is open.
     */
    this.menuOpen = false;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.setMenuOpen = (event) => {
      this.menuOpen = !!event.detail;
    };
  }
  expandedHandler() {
    this.menuOpen = false;
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
  //  Component Methods
  //
  // --------------------------------------------------------------------------
  renderTooltip() {
    const { el } = this;
    const hasTooltip = getSlotted(el, SLOTS.menuTooltip);
    return hasTooltip ? h("slot", { name: SLOTS.menuTooltip, slot: ACTION_MENU_SLOTS.tooltip }) : null;
  }
  renderMenu() {
    const { el, expanded, intlMore, menuOpen, scale, layout } = this;
    const hasMenuItems = getSlotted(el, SLOTS.menuActions);
    return hasMenuItems ? (h("calcite-action-menu", { expanded: expanded, flipPlacements: ["left", "right"], label: intlMore || TEXT.more, onCalciteActionMenuOpenChange: this.setMenuOpen, open: menuOpen, placement: layout === "horizontal" ? "bottom-leading" : "leading-start", scale: scale }, h("calcite-action", { icon: ICONS.menu, scale: scale, slot: ACTION_MENU_SLOTS.trigger, text: intlMore || TEXT.more, textEnabled: expanded }), h("slot", { name: SLOTS.menuActions }), this.renderTooltip())) : null;
  }
  render() {
    return (h(Fragment, null, h("slot", null), this.renderMenu()));
  }
  static get is() { return "calcite-action-group"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["action-group.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["action-group.css"]
    };
  }
  static get properties() {
    return {
      "expanded": {
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
          "text": "When `true`, the component is expanded."
        },
        "attribute": "expanded",
        "reflect": true,
        "defaultValue": "false"
      },
      "layout": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Layout",
          "resolved": "\"grid\" | \"horizontal\" | \"vertical\"",
          "references": {
            "Layout": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Indicates the layout of the component."
        },
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"vertical\""
      },
      "columns": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "Columns",
          "resolved": "1 | 2 | 3 | 4 | 5 | 6",
          "references": {
            "Columns": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Indicates number of columns."
        },
        "attribute": "columns",
        "reflect": true
      },
      "intlMore": {
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
          "text": "Specifies a text string for the `calcite-action-menu`."
        },
        "attribute": "intl-more",
        "reflect": false
      },
      "menuOpen": {
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
          "text": "When `true`, the `calcite-action-menu` is open."
        },
        "attribute": "menu-open",
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
          "text": "Specifies the size of the `calcite-action-menu`."
        },
        "attribute": "scale",
        "reflect": true
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "expanded",
        "methodName": "expandedHandler"
      }];
  }
}
