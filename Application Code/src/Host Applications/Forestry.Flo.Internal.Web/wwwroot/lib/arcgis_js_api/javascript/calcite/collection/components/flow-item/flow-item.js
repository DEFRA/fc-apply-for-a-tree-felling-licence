/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { getElementDir } from "../../utils/dom";
import { CSS, ICONS, TEXT, SLOTS } from "./resources";
import { SLOTS as PANEL_SLOTS } from "../panel/resources";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding custom content.
 * @slot header-actions-start - A slot for adding actions or content to the start side of the header.
 * @slot header-actions-end - A slot for adding actions or content to the end side of the header.
 * @slot header-content - A slot for adding custom content to the header.
 * @slot header-menu-actions - A slot for adding an overflow menu with actions inside a `calcite-dropdown`.
 * @slot fab - A slot for adding a `calcite-fab` (floating action button) to perform an action.
 * @slot footer-actions - A slot for adding buttons to the footer.
 * @slot footer - A slot for adding custom content to the footer.
 */
export class FlowItem {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /** When true, displays a close button in the trailing side of the header */
    this.closable = false;
    /** When true, flow-item will be hidden */
    this.closed = false;
    /**
     *  When true, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When true, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * When true, the action menu items in the `header-menu-actions` slot are open.
     */
    this.menuOpen = false;
    /**
     * When true, displays a back button in the header.
     */
    this.showBackButton = false;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.handlePanelScroll = (event) => {
      event.stopPropagation();
      this.calciteFlowItemScroll.emit();
    };
    this.handlePanelClose = (event) => {
      event.stopPropagation();
      this.calciteFlowItemClose.emit();
    };
    this.backButtonClick = () => {
      this.calciteFlowItemBackClick.emit();
      this.calciteFlowItemBack.emit();
    };
    this.setBackRef = (node) => {
      this.backButtonEl = node;
    };
    this.setContainerRef = (node) => {
      this.containerEl = node;
    };
    this.getBackLabel = () => {
      return this.intlBack || TEXT.back;
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidRender() {
    updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Sets focus on the component.
   */
  async setFocus() {
    const { backButtonEl, containerEl } = this;
    if (backButtonEl) {
      backButtonEl.setFocus();
      return;
    }
    containerEl === null || containerEl === void 0 ? void 0 : containerEl.setFocus();
  }
  /**
   * Scrolls the component's content to a specified set of coordinates.
   *
   * @example
   * myCalciteFlowItem.scrollContentTo({
   *   left: 0, // Specifies the number of pixels along the X axis to scroll the window or element.
   *   top: 0, // Specifies the number of pixels along the Y axis to scroll the window or element
   *   behavior: "auto" // Specifies whether the scrolling should animate smoothly (smooth), or happen instantly in a single jump (auto, the default value).
   * });
   * @param options
   */
  async scrollContentTo(options) {
    var _a;
    await ((_a = this.containerEl) === null || _a === void 0 ? void 0 : _a.scrollContentTo(options));
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderBackButton() {
    const { el } = this;
    const rtl = getElementDir(el) === "rtl";
    const { showBackButton, backButtonClick } = this;
    const label = this.getBackLabel();
    const icon = rtl ? ICONS.backRight : ICONS.backLeft;
    return showBackButton ? (h("calcite-action", { "aria-label": label, class: CSS.backButton, icon: icon, key: "flow-back-button", onClick: backButtonClick, ref: this.setBackRef, scale: "s", slot: "header-actions-start", text: label })) : null;
  }
  render() {
    const { closable, closed, description, disabled, heading, headingLevel, heightScale, intlBack, intlClose, intlOptions, loading, menuOpen, widthScale, backButtonEl } = this;
    const label = this.getBackLabel();
    return (h(Host, null, h("calcite-panel", { closable: closable, closed: closed, description: description, disabled: disabled, heading: heading, headingLevel: headingLevel, heightScale: heightScale, intlBack: intlBack, intlClose: intlClose, intlOptions: intlOptions, loading: loading, menuOpen: menuOpen, onCalcitePanelClose: this.handlePanelClose, onCalcitePanelScroll: this.handlePanelScroll, ref: this.setContainerRef, widthScale: widthScale }, this.renderBackButton(), h("slot", { name: SLOTS.headerActionsStart, slot: PANEL_SLOTS.headerActionsStart }), h("slot", { name: SLOTS.headerActionsEnd, slot: PANEL_SLOTS.headerActionsEnd }), h("slot", { name: SLOTS.headerContent, slot: PANEL_SLOTS.headerContent }), h("slot", { name: SLOTS.headerMenuActions, slot: PANEL_SLOTS.headerMenuActions }), h("slot", { name: SLOTS.fab, slot: PANEL_SLOTS.fab }), h("slot", { name: SLOTS.footerActions, slot: PANEL_SLOTS.footerActions }), h("slot", { name: SLOTS.footer, slot: PANEL_SLOTS.footer }), h("slot", null)), backButtonEl ? (h("calcite-tooltip", { label: label, placement: "auto", referenceElement: backButtonEl }, label)) : null));
  }
  static get is() { return "calcite-flow-item"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["flow-item.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["flow-item.css"]
    };
  }
  static get properties() {
    return {
      "closable": {
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
          "text": "When true, displays a close button in the trailing side of the header"
        },
        "attribute": "closable",
        "reflect": true,
        "defaultValue": "false"
      },
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
          "text": "When true, flow-item will be hidden"
        },
        "attribute": "closed",
        "reflect": true,
        "defaultValue": "false"
      },
      "beforeBack": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "() => Promise<void>",
          "resolved": "() => Promise<void>",
          "references": {
            "Promise": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "When provided, this method will be called before it is removed from the parent flow."
        }
      },
      "description": {
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
          "text": "A description for the component."
        },
        "attribute": "description",
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
          "text": "When true, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
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
      "heightScale": {
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
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the maximum height of the component."
        },
        "attribute": "height-scale",
        "reflect": true
      },
      "intlBack": {
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
          "text": "Accessible name for the component's back button. The back button will only be shown when 'showBackButton' is true."
        },
        "attribute": "intl-back",
        "reflect": false
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
          "text": "Accessible name for the component's close button. The close button will only be shown when 'dismissible' is true."
        },
        "attribute": "intl-close",
        "reflect": false
      },
      "intlOptions": {
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
          "text": "Accessible name for the component's actions menu."
        },
        "attribute": "intl-options",
        "reflect": false
      },
      "loading": {
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
          "text": "When true, a busy indicator is displayed."
        },
        "attribute": "loading",
        "reflect": true,
        "defaultValue": "false"
      },
      "menuOpen": {
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
          "text": "When true, the action menu items in the `header-menu-actions` slot are open."
        },
        "attribute": "menu-open",
        "reflect": true,
        "defaultValue": "false"
      },
      "showBackButton": {
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
          "text": "When true, displays a back button in the header."
        },
        "attribute": "show-back-button",
        "reflect": true,
        "defaultValue": "false"
      },
      "widthScale": {
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
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the width of the component."
        },
        "attribute": "width-scale",
        "reflect": true
      }
    };
  }
  static get states() {
    return {
      "backButtonEl": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteFlowItemBack",
        "name": "calciteFlowItemBack",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the back button is clicked."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteFlowItemBackClick",
        "name": "calciteFlowItemBackClick",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use calciteFlowItemBack instead."
            }],
          "text": "Fires when the back button is clicked."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteFlowItemScroll",
        "name": "calciteFlowItemScroll",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the content is scrolled."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteFlowItemClose",
        "name": "calciteFlowItemClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the close button is clicked."
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
      },
      "scrollContentTo": {
        "complexType": {
          "signature": "(options?: ScrollToOptions) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "options"
                }],
              "text": ""
            }],
          "references": {
            "Promise": {
              "location": "global"
            },
            "ScrollToOptions": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Scrolls the component's content to a specified set of coordinates.",
          "tags": [{
              "name": "example",
              "text": "myCalciteFlowItem.scrollContentTo({\n  left: 0, // Specifies the number of pixels along the X axis to scroll the window or element.\n  top: 0, // Specifies the number of pixels along the Y axis to scroll the window or element\n  behavior: \"auto\" // Specifies whether the scrolling should animate smoothly (smooth), or happen instantly in a single jump (auto, the default value).\n});"
            }, {
              "name": "param",
              "text": "options"
            }]
        }
      }
    };
  }
  static get elementRef() { return "el"; }
}
