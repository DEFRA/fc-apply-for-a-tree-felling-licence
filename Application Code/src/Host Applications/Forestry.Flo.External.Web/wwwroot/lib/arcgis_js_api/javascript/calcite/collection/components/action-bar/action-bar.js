/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Host, h } from "@stencil/core";
import { ExpandToggle, toggleChildActionText } from "../functional/ExpandToggle";
import { CSS, SLOTS, TEXT } from "./resources";
import { getSlotted, focusElement } from "../../utils/dom";
import { geActionDimensions, getOverflowCount, overflowActions, queryActions, overflowActionsDebounceInMs } from "./utils";
import { createObserver } from "../../utils/observers";
import { debounce } from "lodash-es";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * @slot - A slot for adding `calcite-action`s that will appear at the top of the action bar.
 * @slot bottom-actions - A slot for adding `calcite-action`s that will appear at the bottom of the action bar, above the collapse/expand button.
 * @slot expand-tooltip - Used to set the tooltip for the expand toggle.
 */
export class ActionBar {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the expand-toggling behavior is disabled.
     */
    this.expandDisabled = false;
    /**
     * When `true`, the component is expanded.
     */
    this.expanded = false;
    /**
     *  The layout direction of the actions.
     */
    this.layout = "vertical";
    /**
     * Disables automatically overflowing `calcite-action`s that won't fit into menus.
     */
    this.overflowActionsDisabled = false;
    this.mutationObserver = createObserver("mutation", () => {
      const { el, expanded } = this;
      toggleChildActionText({ parent: el, expanded });
      this.conditionallyOverflowActions();
    });
    this.resizeObserver = createObserver("resize", (entries) => this.resizeHandlerEntries(entries));
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.actionMenuOpenChangeHandler = (event) => {
      if (event.detail) {
        const composedPath = event.composedPath();
        Array.from(this.el.querySelectorAll("calcite-action-group")).forEach((group) => {
          if (!composedPath.includes(group)) {
            group.menuOpen = false;
          }
        });
      }
    };
    this.resizeHandlerEntries = (entries) => {
      entries.forEach(this.resizeHandler);
    };
    this.resizeHandler = (entry) => {
      const { width, height } = entry.contentRect;
      this.resize({ width, height });
    };
    this.resize = debounce(({ width, height }) => {
      const { el, expanded, expandDisabled, layout } = this;
      if ((layout === "vertical" && !height) || (layout === "horizontal" && !width)) {
        return;
      }
      const actions = queryActions(el);
      const actionCount = expandDisabled ? actions.length : actions.length + 1;
      const actionGroups = Array.from(el.querySelectorAll("calcite-action-group"));
      const groupCount = getSlotted(el, SLOTS.bottomActions) || !expandDisabled
        ? actionGroups.length + 1
        : actionGroups.length;
      const { actionHeight, actionWidth } = geActionDimensions(actions);
      const overflowCount = getOverflowCount({
        layout,
        actionCount,
        actionHeight,
        actionWidth,
        height,
        width,
        groupCount
      });
      overflowActions({
        actionGroups,
        expanded,
        overflowCount
      });
    }, overflowActionsDebounceInMs);
    this.conditionallyOverflowActions = () => {
      if (!this.overflowActionsDisabled) {
        this.overflowActions();
      }
    };
    this.toggleExpand = () => {
      this.expanded = !this.expanded;
      this.calciteActionBarToggle.emit();
    };
    this.setExpandToggleRef = (el) => {
      this.expandToggleEl = el;
    };
  }
  expandHandler() {
    this.conditionallyOverflowActions();
  }
  expandedHandler(expanded) {
    toggleChildActionText({ parent: this.el, expanded });
    this.conditionallyOverflowActions();
  }
  overflowDisabledHandler(overflowActionsDisabled) {
    overflowActionsDisabled
      ? this.resizeObserver.disconnect()
      : this.resizeObserver.observe(this.el);
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  componentDidLoad() {
    this.conditionallyOverflowActions();
  }
  connectedCallback() {
    var _a, _b;
    const { el, expanded } = this;
    toggleChildActionText({ parent: el, expanded });
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(el, { childList: true, subtree: true });
    if (!this.overflowActionsDisabled) {
      (_b = this.resizeObserver) === null || _b === void 0 ? void 0 : _b.observe(el);
    }
    this.conditionallyOverflowActions();
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    var _a, _b;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    (_b = this.resizeObserver) === null || _b === void 0 ? void 0 : _b.disconnect();
    disconnectConditionalSlotComponent(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Overflows actions that won't fit into menus.
   *
   * @internal
   */
  async overflowActions() {
    this.resize({ width: this.el.clientWidth, height: this.el.clientHeight });
  }
  /**
   * Sets focus on the component.
   *
   * @param focusId
   */
  async setFocus(focusId) {
    var _a;
    if (focusId === "expand-toggle") {
      await focusElement(this.expandToggleEl);
      return;
    }
    (_a = this.el) === null || _a === void 0 ? void 0 : _a.focus();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderBottomActionGroup() {
    const { expanded, expandDisabled, intlExpand, intlCollapse, el, position, toggleExpand, scale, layout } = this;
    const tooltip = getSlotted(el, SLOTS.expandTooltip);
    const expandLabel = intlExpand || TEXT.expand;
    const collapseLabel = intlCollapse || TEXT.collapse;
    const expandToggleNode = !expandDisabled ? (h(ExpandToggle, { el: el, expanded: expanded, intlCollapse: collapseLabel, intlExpand: expandLabel, position: position, ref: this.setExpandToggleRef, scale: scale, toggle: toggleExpand, tooltip: tooltip })) : null;
    return getSlotted(el, SLOTS.bottomActions) || expandToggleNode ? (h("calcite-action-group", { class: CSS.actionGroupBottom, layout: layout, scale: scale }, h("slot", { name: SLOTS.bottomActions }), h("slot", { name: SLOTS.expandTooltip }), expandToggleNode)) : null;
  }
  render() {
    return (h(Host, { onCalciteActionMenuOpenChange: this.actionMenuOpenChangeHandler }, h("slot", null), this.renderBottomActionGroup()));
  }
  static get is() { return "calcite-action-bar"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["action-bar.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["action-bar.css"]
    };
  }
  static get properties() {
    return {
      "expandDisabled": {
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
          "text": "When `true`, the expand-toggling behavior is disabled."
        },
        "attribute": "expand-disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "expanded": {
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
          "text": "When `true`, the component is expanded."
        },
        "attribute": "expanded",
        "reflect": true,
        "defaultValue": "false"
      },
      "intlExpand": {
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
          "text": "Specifies the label of the expand icon when the component is collapsed."
        },
        "attribute": "intl-expand",
        "reflect": false
      },
      "intlCollapse": {
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
          "text": "Specifies the label of the collapse icon when the component is expanded."
        },
        "attribute": "intl-collapse",
        "reflect": false
      },
      "layout": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Extract<\"horizontal\" | \"vertical\", Layout>",
          "resolved": "\"horizontal\" | \"vertical\"",
          "references": {
            "Extract": {
              "location": "global"
            },
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
          "text": "The layout direction of the actions."
        },
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"vertical\""
      },
      "overflowActionsDisabled": {
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
          "text": "Disables automatically overflowing `calcite-action`s that won't fit into menus."
        },
        "attribute": "overflow-actions-disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "position": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Position",
          "resolved": "\"end\" | \"start\"",
          "references": {
            "Position": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Arranges the component depending on the element's `dir` property."
        },
        "attribute": "position",
        "reflect": true
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
          "text": "Specifies the size of the expand `calcite-action`."
        },
        "attribute": "scale",
        "reflect": true
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteActionBarToggle",
        "name": "calciteActionBarToggle",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits when the `expanded` property is toggled."
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
      "overflowActions": {
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
          "text": "Overflows actions that won't fit into menus.",
          "tags": [{
              "name": "internal",
              "text": undefined
            }]
        }
      },
      "setFocus": {
        "complexType": {
          "signature": "(focusId?: \"expand-toggle\") => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "focusId"
                }],
              "text": ""
            }],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Sets focus on the component.",
          "tags": [{
              "name": "param",
              "text": "focusId"
            }]
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "expandDisabled",
        "methodName": "expandHandler"
      }, {
        "propName": "expanded",
        "methodName": "expandedHandler"
      }, {
        "propName": "overflowActionsDisabled",
        "methodName": "overflowDisabledHandler"
      }];
  }
}
