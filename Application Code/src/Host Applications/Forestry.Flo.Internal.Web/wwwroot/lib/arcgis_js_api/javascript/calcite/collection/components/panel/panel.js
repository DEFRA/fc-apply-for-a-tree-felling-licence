/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Fragment } from "@stencil/core";
import { CSS, HEADING_LEVEL, ICONS, SLOTS, TEXT } from "./resources";
import { getElementDir, toAriaBoolean } from "../../utils/dom";
import { Heading } from "../functional/Heading";
import { SLOTS as ACTION_MENU_SLOTS } from "../action-menu/resources";
import { updateHostInteraction } from "../../utils/interactive";
import { createObserver } from "../../utils/observers";
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
export class Panel {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, hides the component.
     *
     * @deprecated use `closed` instead.
     */
    this.dismissed = false;
    /** When `true`, the component will be hidden. */
    this.closed = false;
    /**
     *  When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `true`, a close button is added to the component.
     *
     * @deprecated use `closable` instead
     */
    this.dismissible = false;
    /** When `true`, displays a close button in the trailing side of the header. */
    this.closable = false;
    /**
     * When `true`, displays a back button in the header.
     *
     * @deprecated use `calcite-flow-item` instead.
     */
    this.showBackButton = false;
    /**
     * When `true`, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * When `true`, the action menu items in the `header-menu-actions` slot are open.
     */
    this.menuOpen = false;
    this.resizeObserver = createObserver("resize", () => this.resizeHandler());
    this.hasStartActions = false;
    this.hasEndActions = false;
    this.hasMenuItems = false;
    this.hasHeaderContent = false;
    this.hasFooterContent = false;
    this.hasFooterActions = false;
    this.hasFab = false;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.resizeHandler = () => {
      const { panelScrollEl } = this;
      if (!panelScrollEl ||
        typeof panelScrollEl.scrollHeight !== "number" ||
        typeof panelScrollEl.offsetHeight !== "number") {
        return;
      }
      panelScrollEl.tabIndex = panelScrollEl.scrollHeight > panelScrollEl.offsetHeight ? 0 : -1;
    };
    this.setContainerRef = (node) => {
      this.containerEl = node;
    };
    this.setCloseRef = (node) => {
      this.closeButtonEl = node;
    };
    this.setBackRef = (node) => {
      this.backButtonEl = node;
    };
    this.panelKeyDownHandler = (event) => {
      if (this.closable && event.key === "Escape" && !event.defaultPrevented) {
        this.close();
        event.preventDefault();
      }
    };
    this.close = () => {
      this.closed = true;
      this.calcitePanelDismiss.emit();
      this.calcitePanelClose.emit();
    };
    this.panelScrollHandler = () => {
      this.calcitePanelScroll.emit();
    };
    this.backButtonClick = () => {
      this.calcitePanelBackClick.emit();
    };
    this.handleHeaderActionsStartSlotChange = (event) => {
      const elements = event.target.assignedElements({
        flatten: true
      });
      this.hasStartActions = !!elements.length;
    };
    this.handleHeaderActionsEndSlotChange = (event) => {
      const elements = event.target.assignedElements({
        flatten: true
      });
      this.hasEndActions = !!elements.length;
    };
    this.handleHeaderMenuActionsSlotChange = (event) => {
      const elements = event.target.assignedElements({
        flatten: true
      });
      this.hasMenuItems = !!elements.length;
    };
    this.handleHeaderContentSlotChange = (event) => {
      const elements = event.target.assignedElements({
        flatten: true
      });
      this.hasHeaderContent = !!elements.length;
    };
    this.handleFooterSlotChange = (event) => {
      const elements = event.target.assignedElements({
        flatten: true
      });
      this.hasFooterContent = !!elements.length;
    };
    this.handleFooterActionsSlotChange = (event) => {
      const elements = event.target.assignedElements({
        flatten: true
      });
      this.hasFooterActions = !!elements.length;
    };
    this.handleFabSlotChange = (event) => {
      const elements = event.target.assignedElements({
        flatten: true
      });
      this.hasFab = !!elements.length;
    };
    this.setPanelScrollEl = (el) => {
      var _a, _b;
      this.panelScrollEl = el;
      (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
      if (el) {
        (_b = this.resizeObserver) === null || _b === void 0 ? void 0 : _b.observe(el);
        this.resizeHandler();
      }
    };
  }
  dismissedHandler(value) {
    this.closed = value;
    this.calcitePanelDismissedChange.emit();
  }
  closedHandler(value) {
    this.dismissed = value;
  }
  dismissibleHandler(value) {
    this.closable = value;
  }
  closableHandler(value) {
    this.dismissible = value;
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
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    const isClosed = this.dismissed || this.closed;
    const isClosable = this.dismissible || this.closable;
    if (isClosed) {
      this.dismissedHandler(isClosed);
      this.closedHandler(isClosed);
    }
    if (isClosable) {
      this.dismissibleHandler(isClosable);
      this.closableHandler(isClosable);
    }
  }
  disconnectedCallback() {
    var _a;
    (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  // --------------------------------------------------------------------------
  //
  //  Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Sets focus on the component.
   *
   * @param focusId
   */
  async setFocus(focusId) {
    const { backButtonEl, closeButtonEl, containerEl } = this;
    if (focusId === "back-button") {
      backButtonEl === null || backButtonEl === void 0 ? void 0 : backButtonEl.setFocus();
      return;
    }
    if (focusId === "dismiss-button") {
      closeButtonEl === null || closeButtonEl === void 0 ? void 0 : closeButtonEl.setFocus();
      return;
    }
    if (backButtonEl) {
      backButtonEl.setFocus();
      return;
    }
    if (closeButtonEl) {
      closeButtonEl.setFocus();
      return;
    }
    containerEl === null || containerEl === void 0 ? void 0 : containerEl.focus();
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
    (_a = this.panelScrollEl) === null || _a === void 0 ? void 0 : _a.scrollTo(options);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderBackButton() {
    const { el } = this;
    const rtl = getElementDir(el) === "rtl";
    const { showBackButton, intlBack, backButtonClick } = this;
    const label = intlBack || TEXT.back;
    const icon = rtl ? ICONS.backRight : ICONS.backLeft;
    return showBackButton ? (h("calcite-action", { "aria-label": label, class: CSS.backButton, icon: icon, key: "back-button", onClick: backButtonClick, ref: this.setBackRef, scale: "s", slot: SLOTS.headerActionsStart, text: label })) : null;
  }
  renderHeaderContent() {
    const { heading, headingLevel, summary, description, hasHeaderContent } = this;
    const headingNode = heading ? (h(Heading, { class: CSS.heading, level: headingLevel || HEADING_LEVEL }, heading)) : null;
    const descriptionNode = description || summary ? h("span", { class: CSS.description }, description || summary) : null;
    return !hasHeaderContent && (headingNode || descriptionNode) ? (h("div", { class: CSS.headerContent, key: "header-content" }, headingNode, descriptionNode)) : null;
  }
  /**
   * Allows user to override the entire header-content node.
   */
  renderHeaderSlottedContent() {
    return (h("div", { class: CSS.headerContent, hidden: !this.hasHeaderContent, key: "slotted-header-content" }, h("slot", { name: SLOTS.headerContent, onSlotchange: this.handleHeaderContentSlotChange })));
  }
  renderHeaderStartActions() {
    const { hasStartActions } = this;
    return (h("div", { class: { [CSS.headerActionsStart]: true, [CSS.headerActions]: true }, hidden: !hasStartActions, key: "header-actions-start" }, h("slot", { name: SLOTS.headerActionsStart, onSlotchange: this.handleHeaderActionsStartSlotChange })));
  }
  renderHeaderActionsEnd() {
    const { close, hasEndActions, intlClose, closable } = this;
    const text = intlClose || TEXT.close;
    const closableNode = closable ? (h("calcite-action", { "aria-label": text, icon: ICONS.close, onClick: close, ref: this.setCloseRef, text: text })) : null;
    const slotNode = (h("slot", { name: SLOTS.headerActionsEnd, onSlotchange: this.handleHeaderActionsEndSlotChange }));
    const showContainer = hasEndActions || closableNode;
    return (h("div", { class: { [CSS.headerActionsEnd]: true, [CSS.headerActions]: true }, hidden: !showContainer, key: "header-actions-end" }, slotNode, closableNode));
  }
  renderMenu() {
    const { hasMenuItems, intlOptions, menuOpen } = this;
    return (h("calcite-action-menu", { flipPlacements: ["top", "bottom"], hidden: !hasMenuItems, key: "menu", label: intlOptions || TEXT.options, open: menuOpen, placement: "bottom-end" }, h("calcite-action", { icon: ICONS.menu, slot: ACTION_MENU_SLOTS.trigger, text: intlOptions || TEXT.options }), h("slot", { name: SLOTS.headerMenuActions, onSlotchange: this.handleHeaderMenuActionsSlotChange })));
  }
  renderHeaderNode() {
    const { showBackButton, hasHeaderContent, hasStartActions, hasEndActions, closable, hasMenuItems } = this;
    const headerContentNode = this.renderHeaderContent();
    const showHeader = showBackButton ||
      hasHeaderContent ||
      headerContentNode ||
      hasStartActions ||
      hasEndActions ||
      closable ||
      hasMenuItems;
    return (h("header", { class: CSS.header, hidden: !showHeader }, this.renderBackButton(), this.renderHeaderStartActions(), this.renderHeaderSlottedContent(), headerContentNode, this.renderHeaderActionsEnd(), this.renderMenu()));
  }
  renderFooterNode() {
    const { hasFooterContent, hasFooterActions } = this;
    const showFooter = hasFooterContent || hasFooterActions;
    return (h("footer", { class: CSS.footer, hidden: !showFooter }, h("slot", { key: "footer-slot", name: SLOTS.footer, onSlotchange: this.handleFooterSlotChange }), h("slot", { key: "footer-actions-slot", name: SLOTS.footerActions, onSlotchange: this.handleFooterActionsSlotChange })));
  }
  renderContent() {
    const { hasFab } = this;
    const defaultSlotNode = h("slot", { key: "default-slot" });
    const containerNode = hasFab ? (h("section", { class: CSS.contentContainer }, defaultSlotNode)) : (defaultSlotNode);
    return (h("div", { class: {
        [CSS.contentWrapper]: true,
        [CSS.contentContainer]: !hasFab,
        [CSS.contentHeight]: hasFab
      }, onScroll: this.panelScrollHandler, ref: this.setPanelScrollEl }, containerNode, this.renderFab()));
  }
  renderFab() {
    return (h("div", { class: CSS.fabContainer, hidden: !this.hasFab }, h("slot", { name: SLOTS.fab, onSlotchange: this.handleFabSlotChange })));
  }
  render() {
    const { loading, panelKeyDownHandler, closed, closable } = this;
    const panelNode = (h("article", { "aria-busy": toAriaBoolean(loading), class: CSS.container, hidden: closed, onKeyDown: panelKeyDownHandler, ref: this.setContainerRef, tabIndex: closable ? 0 : -1 }, this.renderHeaderNode(), this.renderContent(), this.renderFooterNode()));
    return (h(Fragment, null, loading ? h("calcite-scrim", { loading: loading }) : null, panelNode));
  }
  static get is() { return "calcite-panel"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["panel.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["panel.css"]
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
          "tags": [{
              "name": "deprecated",
              "text": "use `closed` instead."
            }],
          "text": "When `true`, hides the component."
        },
        "attribute": "dismissed",
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
          "text": "When `true`, the component will be hidden."
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
          "tags": [{
              "name": "deprecated",
              "text": "use `calcite-flow-item` instead."
            }],
          "text": "When provided, this method will be called before it is removed from the parent flow."
        }
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
          "text": "When `true`, interaction is prevented and the component is displayed with lower opacity."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
      },
      "dismissible": {
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
          "tags": [{
              "name": "deprecated",
              "text": "use `closable` instead"
            }],
          "text": "When `true`, a close button is added to the component."
        },
        "attribute": "dismissible",
        "reflect": true,
        "defaultValue": "false"
      },
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
          "text": "When `true`, displays a close button in the trailing side of the header."
        },
        "attribute": "closable",
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
          "tags": [{
              "name": "deprecated",
              "text": "use `calcite-flow-item` instead."
            }],
          "text": "When `true`, displays a back button in the header."
        },
        "attribute": "show-back-button",
        "reflect": true,
        "defaultValue": "false"
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
          "tags": [{
              "name": "deprecated",
              "text": "use `calcite-flow-item` instead."
            }],
          "text": "Accessible name for the component's back button. The back button will only be shown when `showBackButton` is `true`."
        },
        "attribute": "intl-back",
        "reflect": false
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
          "text": "When `true`, a busy indicator is displayed."
        },
        "attribute": "loading",
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
          "text": "Accessible name for the component's close button. The close button will only be shown when `closeable` is `true`."
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
      "summary": {
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
          "tags": [{
              "name": "deprecated",
              "text": "use `description` instead."
            }],
          "text": "Summary text. A description displayed underneath the heading."
        },
        "attribute": "summary",
        "reflect": false
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
          "text": "When `true`, the action menu items in the `header-menu-actions` slot are open."
        },
        "attribute": "menu-open",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get states() {
    return {
      "hasStartActions": {},
      "hasEndActions": {},
      "hasMenuItems": {},
      "hasHeaderContent": {},
      "hasFooterContent": {},
      "hasFooterActions": {},
      "hasFab": {}
    };
  }
  static get events() {
    return [{
        "method": "calcitePanelClose",
        "name": "calcitePanelClose",
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
      }, {
        "method": "calcitePanelDismiss",
        "name": "calcitePanelDismiss",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `calcitePanelClose` instead."
            }],
          "text": "Fires when the close button is clicked."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calcitePanelDismissedChange",
        "name": "calcitePanelDismissedChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `calcitePanelClose` instead."
            }],
          "text": "Fires when there is a change to the `dismissed` property value ."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calcitePanelScroll",
        "name": "calcitePanelScroll",
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
        "method": "calcitePanelBackClick",
        "name": "calcitePanelBackClick",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `calcite-flow-item` instead."
            }],
          "text": "Fires when the back button is clicked."
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
          "signature": "(focusId?: \"back-button\" | \"dismiss-button\") => Promise<void>",
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
  static get watchers() {
    return [{
        "propName": "dismissed",
        "methodName": "dismissedHandler"
      }, {
        "propName": "closed",
        "methodName": "closedHandler"
      }, {
        "propName": "dismissible",
        "methodName": "dismissibleHandler"
      }, {
        "propName": "closable",
        "methodName": "closableHandler"
      }];
  }
}
