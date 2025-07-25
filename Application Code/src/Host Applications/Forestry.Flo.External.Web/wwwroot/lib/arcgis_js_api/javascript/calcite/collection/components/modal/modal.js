/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { ensureId, focusElement, getSlotted, isCalciteFocusable } from "../../utils/dom";
import { queryShadowRoot } from "@a11y/focus-trap/shadow";
import { isFocusable, isHidden } from "@a11y/focus-trap/focusable";
import { CSS, ICONS, SLOTS, TEXT } from "./resources";
import { createObserver } from "../../utils/observers";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
import { connectOpenCloseComponent, disconnectOpenCloseComponent } from "../../utils/openCloseComponent";
const isFocusableExtended = (el) => {
  return isCalciteFocusable(el) || isFocusable(el);
};
const getFocusableElements = (el) => {
  return queryShadowRoot(el, isHidden, isFocusableExtended);
};
/**
 * @slot header - A slot for adding header text.
 * @slot content - A slot for adding the component's content.
 * @slot primary - A slot for adding a primary button.
 * @slot secondary - A slot for adding a secondary button.
 * @slot back - A slot for adding a back button.
 */
export class Modal {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, the component is active.
     *
     * @deprecated use `open` instead.
     */
    this.active = false;
    /** When `true`, displays and positions the component.  */
    this.open = false;
    /** Passes a function to run before the component closes. */
    this.beforeClose = () => Promise.resolve();
    /** When `true`, disables the component's close button. */
    this.disableCloseButton = false;
    /** When `true`, disables the closing of the component when clicked outside. */
    this.disableOutsideClose = false;
    /** Accessible name for the component's close button. */
    this.intlClose = TEXT.close;
    /** When `true`, disables the default close on escape behavior. */
    this.disableEscape = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Specifies the width of the component. Can use scale sizes or pass a number (displays in pixels). */
    this.width = "m";
    /** Sets the background color of the component's content. */
    this.backgroundColor = "white";
    /**
     * When `true`, disables spacing to the content area slot.
     *
     * @deprecated  Use `--calcite-modal-padding` CSS variable instead.
     */
    this.noPadding = false;
    //--------------------------------------------------------------------------
    //
    //  Variables
    //
    //--------------------------------------------------------------------------
    this.hasFooter = true;
    /**
     * We use internal variable to make sure initially open modal can transition from closed state when rendered
     *
     * @private
     */
    this.isOpen = false;
    this.mutationObserver = createObserver("mutation", () => this.updateFooterVisibility());
    this.openTransitionProp = "opacity";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.setTransitionEl = (el) => {
      this.transitionEl = el;
      connectOpenCloseComponent(this);
    };
    this.openEnd = () => {
      this.setFocus();
      this.el.removeEventListener("calciteModalOpen", this.openEnd);
    };
    this.handleOutsideClose = () => {
      if (this.disableOutsideClose) {
        return;
      }
      this.close();
    };
    /** Close the modal, first running the `beforeClose` method */
    this.close = () => {
      return this.beforeClose(this.el).then(() => {
        this.open = false;
        this.isOpen = false;
        focusElement(this.previousActiveElement);
        this.removeOverflowHiddenClass();
      });
    };
    this.focusFirstElement = () => {
      focusElement(this.disableCloseButton ? getFocusableElements(this.el)[0] : this.closeButtonEl);
    };
    this.focusLastElement = () => {
      const focusableElements = getFocusableElements(this.el).filter((el) => !el.getAttribute("data-focus-fence"));
      if (focusableElements.length > 0) {
        focusElement(focusableElements[focusableElements.length - 1]);
      }
      else {
        focusElement(this.closeButtonEl);
      }
    };
    this.updateFooterVisibility = () => {
      this.hasFooter = !!getSlotted(this.el, [SLOTS.back, SLOTS.primary, SLOTS.secondary]);
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentWillLoad() {
    // when modal initially renders, if active was set we need to open as watcher doesn't fire
    if (this.open) {
      requestAnimationFrame(() => this.openModal());
    }
  }
  connectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
    this.updateFooterVisibility();
    connectConditionalSlotComponent(this);
    connectOpenCloseComponent(this);
    if (this.open) {
      this.active = this.open;
    }
    if (this.active) {
      this.activeHandler(this.active);
    }
  }
  disconnectedCallback() {
    var _a;
    this.removeOverflowHiddenClass();
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    disconnectConditionalSlotComponent(this);
    disconnectOpenCloseComponent(this);
  }
  render() {
    return (h(Host, { "aria-describedby": this.contentId, "aria-labelledby": this.titleId, "aria-modal": "true", role: "dialog" }, h("calcite-scrim", { class: CSS.scrim, onClick: this.handleOutsideClose }), this.renderStyle(), h("div", { class: {
        [CSS.modal]: true,
        [CSS.modalOpen]: this.isOpen
      }, ref: this.setTransitionEl }, h("div", { "data-focus-fence": true, onFocus: this.focusLastElement, tabindex: "0" }), h("div", { class: CSS.header }, this.renderCloseButton(), h("header", { class: CSS.title }, h("slot", { name: CSS.header }))), h("div", { class: {
        content: true,
        "content--spaced": !this.noPadding,
        "content--no-footer": !this.hasFooter
      }, ref: (el) => (this.modalContent = el) }, h("slot", { name: SLOTS.content })), this.renderFooter(), h("div", { "data-focus-fence": true, onFocus: this.focusFirstElement, tabindex: "0" }))));
  }
  renderFooter() {
    return this.hasFooter ? (h("div", { class: CSS.footer, key: "footer" }, h("span", { class: CSS.back }, h("slot", { name: SLOTS.back })), h("span", { class: CSS.secondary }, h("slot", { name: SLOTS.secondary })), h("span", { class: CSS.primary }, h("slot", { name: SLOTS.primary })))) : null;
  }
  renderCloseButton() {
    return !this.disableCloseButton ? (h("button", { "aria-label": this.intlClose, class: CSS.close, key: "button", onClick: this.close, ref: (el) => (this.closeButtonEl = el), title: this.intlClose }, h("calcite-icon", { icon: ICONS.close, scale: this.scale === "s" ? "s" : this.scale === "m" ? "m" : this.scale === "l" ? "l" : null }))) : null;
  }
  renderStyle() {
    const hasCustomWidth = !isNaN(parseInt(`${this.width}`));
    return hasCustomWidth ? (h("style", null, `
        .${CSS.modal} {
          max-width: ${this.width}px !important;
        }
        @media screen and (max-width: ${this.width}px) {
          .${CSS.modal} {
            height: 100% !important;
            max-height: 100% !important;
            width: 100% !important;
            max-width: 100% !important;
            margin: 0 !important;
            border-radius: 0 !important;
          }
          .content {
            flex: 1 1 auto !important;
            max-height: unset !important;
          }
        }
      `)) : null;
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  handleEscape(event) {
    if (this.open && !this.disableEscape && event.key === "Escape" && !event.defaultPrevented) {
      this.close();
      event.preventDefault();
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /**
   * Focus the first interactive element.
   *
   * @param el
   * @deprecated use `setFocus` instead.
   */
  async focusElement(el) {
    if (el) {
      el.focus();
    }
    return this.setFocus();
  }
  /**
   * Sets focus on the component.
   *
   * By default, tries to focus on focusable content. If there is none, it will focus on the close button.
   * To focus on the close button, use the `close-button` focus ID.
   *
   * @param focusId
   */
  async setFocus(focusId) {
    const closeButton = this.closeButtonEl;
    return focusElement(focusId === "close-button" ? closeButton : getFocusableElements(this.el)[0] || closeButton);
  }
  /**
   * Sets the scroll top of the component's content.
   *
   * @param top
   * @param left
   */
  async scrollContent(top = 0, left = 0) {
    if (this.modalContent) {
      if (this.modalContent.scrollTo) {
        this.modalContent.scrollTo({ top, left, behavior: "smooth" });
      }
      else {
        this.modalContent.scrollTop = top;
        this.modalContent.scrollLeft = left;
      }
    }
  }
  onBeforeOpen() {
    this.transitionEl.classList.add(CSS.openingActive);
    this.calciteModalBeforeOpen.emit();
  }
  onOpen() {
    this.transitionEl.classList.remove(CSS.openingIdle, CSS.openingActive);
    this.calciteModalOpen.emit();
  }
  onBeforeClose() {
    this.transitionEl.classList.add(CSS.closingActive);
    this.calciteModalBeforeClose.emit();
  }
  onClose() {
    this.transitionEl.classList.remove(CSS.closingIdle, CSS.closingActive);
    this.calciteModalClose.emit();
  }
  activeHandler(value) {
    this.open = value;
  }
  async toggleModal(value) {
    var _a, _b;
    this.active = value;
    if (value) {
      (_a = this.transitionEl) === null || _a === void 0 ? void 0 : _a.classList.add(CSS.openingIdle);
      this.openModal();
    }
    else {
      (_b = this.transitionEl) === null || _b === void 0 ? void 0 : _b.classList.add(CSS.closingIdle);
      this.close();
    }
  }
  /** Open the modal */
  openModal() {
    this.previousActiveElement = document.activeElement;
    this.el.addEventListener("calciteModalOpen", this.openEnd);
    this.open = true;
    this.isOpen = true;
    const titleEl = getSlotted(this.el, SLOTS.header);
    const contentEl = getSlotted(this.el, SLOTS.content);
    this.titleId = ensureId(titleEl);
    this.contentId = ensureId(contentEl);
    document.documentElement.classList.add(CSS.overflowHidden);
  }
  removeOverflowHiddenClass() {
    document.documentElement.classList.remove(CSS.overflowHidden);
  }
  static get is() { return "calcite-modal"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["modal.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["modal.css"]
    };
  }
  static get properties() {
    return {
      "active": {
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
              "text": "use `open` instead."
            }],
          "text": "When `true`, the component is active."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "open": {
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
          "text": "When `true`, displays and positions the component."
        },
        "attribute": "open",
        "reflect": true,
        "defaultValue": "false"
      },
      "beforeClose": {
        "type": "unknown",
        "mutable": false,
        "complexType": {
          "original": "(el: HTMLElement) => Promise<void>",
          "resolved": "(el: HTMLElement) => Promise<void>",
          "references": {
            "HTMLElement": {
              "location": "global"
            },
            "Promise": {
              "location": "global"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Passes a function to run before the component closes."
        },
        "defaultValue": "() => Promise.resolve()"
      },
      "disableCloseButton": {
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
          "text": "When `true`, disables the component's close button."
        },
        "attribute": "disable-close-button",
        "reflect": true,
        "defaultValue": "false"
      },
      "disableOutsideClose": {
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
          "text": "When `true`, disables the closing of the component when clicked outside."
        },
        "attribute": "disable-outside-close",
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
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Accessible name for the component's close button."
        },
        "attribute": "intl-close",
        "reflect": false,
        "defaultValue": "TEXT.close"
      },
      "docked": {
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
          "text": "When `true`, prevents the component from expanding to the entire screen on mobile devices."
        },
        "attribute": "docked",
        "reflect": true
      },
      "disableEscape": {
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
          "text": "When `true`, disables the default close on escape behavior."
        },
        "attribute": "disable-escape",
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
          "text": "Specifies the size of the component."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "width": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "Scale | number",
          "resolved": "\"l\" | \"m\" | \"s\" | number",
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
          "text": "Specifies the width of the component. Can use scale sizes or pass a number (displays in pixels)."
        },
        "attribute": "width",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "fullscreen": {
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
          "text": "Sets the component to always be fullscreen (overrides `width`)."
        },
        "attribute": "fullscreen",
        "reflect": true
      },
      "color": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "\"red\" | \"blue\"",
          "resolved": "\"blue\" | \"red\"",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Adds a color bar to the top of component for visual impact.\nUse color to add importance to destructive or workflow dialogs."
        },
        "attribute": "color",
        "reflect": true
      },
      "backgroundColor": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ModalBackgroundColor",
          "resolved": "\"grey\" | \"white\"",
          "references": {
            "ModalBackgroundColor": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Sets the background color of the component's content."
        },
        "attribute": "background-color",
        "reflect": true,
        "defaultValue": "\"white\""
      },
      "noPadding": {
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
              "text": "Use `--calcite-modal-padding` CSS variable instead."
            }],
          "text": "When `true`, disables spacing to the content area slot."
        },
        "attribute": "no-padding",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get states() {
    return {
      "hasFooter": {},
      "isOpen": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteModalBeforeClose",
        "name": "calciteModalBeforeClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is requested to be closed and before the closing transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteModalClose",
        "name": "calciteModalClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is closed and animation is complete."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteModalBeforeOpen",
        "name": "calciteModalBeforeOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is added to the DOM but not rendered, and before the opening transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteModalOpen",
        "name": "calciteModalOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is open and animation is complete."
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
      "focusElement": {
        "complexType": {
          "signature": "(el?: HTMLElement) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "el"
                }],
              "text": ""
            }],
          "references": {
            "Promise": {
              "location": "global"
            },
            "HTMLElement": {
              "location": "global"
            }
          },
          "return": "Promise<void>"
        },
        "docs": {
          "text": "Focus the first interactive element.",
          "tags": [{
              "name": "param",
              "text": "el"
            }, {
              "name": "deprecated",
              "text": "use `setFocus` instead."
            }]
        }
      },
      "setFocus": {
        "complexType": {
          "signature": "(focusId?: \"close-button\") => Promise<void>",
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
          "text": "Sets focus on the component.\n\nBy default, tries to focus on focusable content. If there is none, it will focus on the close button.\nTo focus on the close button, use the `close-button` focus ID.",
          "tags": [{
              "name": "param",
              "text": "focusId"
            }]
        }
      },
      "scrollContent": {
        "complexType": {
          "signature": "(top?: number, left?: number) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "top"
                }],
              "text": ""
            }, {
              "tags": [{
                  "name": "param",
                  "text": "left"
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
          "text": "Sets the scroll top of the component's content.",
          "tags": [{
              "name": "param",
              "text": "top"
            }, {
              "name": "param",
              "text": "left"
            }]
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get watchers() {
    return [{
        "propName": "active",
        "methodName": "activeHandler"
      }, {
        "propName": "open",
        "methodName": "toggleModal"
      }];
  }
  static get listeners() {
    return [{
        "name": "keydown",
        "method": "handleEscape",
        "target": "window",
        "capture": false,
        "passive": false
      }];
  }
}
