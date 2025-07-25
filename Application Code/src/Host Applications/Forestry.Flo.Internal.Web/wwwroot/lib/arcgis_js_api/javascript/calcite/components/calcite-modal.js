/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { proxyCustomElement, HTMLElement, createEvent, h, Host } from '@stencil/core/internal/client/index.js';
import { f as focusElement, b as getSlotted, h as ensureId, j as isCalciteFocusable } from './dom.js';
import { c as createObserver } from './observers.js';
import { c as connectConditionalSlotComponent, d as disconnectConditionalSlotComponent } from './conditionalSlot.js';
import { c as connectOpenCloseComponent, d as disconnectOpenCloseComponent } from './openCloseComponent.js';
import { d as defineCustomElement$4 } from './icon.js';
import { d as defineCustomElement$3 } from './loader.js';
import { d as defineCustomElement$2 } from './scrim.js';

/**
 * Traverses the slots of the open shadowroots and returns all children matching the query.
 * @param {ShadowRoot | HTMLElement} root
 * @param skipNode
 * @param isMatch
 * @param {number} maxDepth
 * @param {number} depth
 * @returns {HTMLElement[]}
 */
function queryShadowRoot(root, skipNode, isMatch, maxDepth = 20, depth = 0) {
    let matches = [];
    // If the depth is above the max depth, abort the searching here.
    if (depth >= maxDepth) {
        return matches;
    }
    // Traverses a slot element
    const traverseSlot = ($slot) => {
        // Only check nodes that are of the type Node.ELEMENT_NODE
        // Read more here https://developer.mozilla.org/en-US/docs/Web/API/Node/nodeType
        const assignedNodes = $slot.assignedNodes().filter(node => node.nodeType === 1);
        if (assignedNodes.length > 0) {
            return queryShadowRoot(assignedNodes[0].parentElement, skipNode, isMatch, maxDepth, depth + 1);
        }
        return [];
    };
    // Go through each child and continue the traversing if necessary
    // Even though the typing says that children can't be undefined, Edge 15 sometimes gives an undefined value.
    // Therefore we fallback to an empty array if it is undefined.
    const children = Array.from(root.children || []);
    for (const $child of children) {
        // Check if the node and its descendants should be skipped
        if (skipNode($child)) {
            continue;
        }
        // If the child matches we always add it
        if (isMatch($child)) {
            matches.push($child);
        }
        if ($child.shadowRoot != null) {
            matches.push(...queryShadowRoot($child.shadowRoot, skipNode, isMatch, maxDepth, depth + 1));
        }
        else if ($child.tagName === "SLOT") {
            matches.push(...traverseSlot($child));
        }
        else {
            matches.push(...queryShadowRoot($child, skipNode, isMatch, maxDepth, depth + 1));
        }
    }
    return matches;
}

/**
 * Returns whether the element is hidden.
 * @param $elem
 */
function isHidden($elem) {
    return $elem.hasAttribute("hidden")
        || ($elem.hasAttribute("aria-hidden") && $elem.getAttribute("aria-hidden") !== "false")
        // A quick and dirty way to check whether the element is hidden.
        // For a more fine-grained check we could use "window.getComputedStyle" but we don't because of bad performance.
        // If the element has visibility set to "hidden" or "collapse", display set to "none" or opacity set to "0" through CSS
        // we won't be able to catch it here. We accept it due to the huge performance benefits.
        || $elem.style.display === `none`
        || $elem.style.opacity === `0`
        || $elem.style.visibility === `hidden`
        || $elem.style.visibility === `collapse`;
    // If offsetParent is null we can assume that the element is hidden
    // https://stackoverflow.com/questions/306305/what-would-make-offsetparent-null
    //|| $elem.offsetParent == null;
}
/**
 * Returns whether the element is disabled.
 * @param $elem
 */
function isDisabled($elem) {
    return $elem.hasAttribute("disabled")
        || ($elem.hasAttribute("aria-disabled") && $elem.getAttribute("aria-disabled") !== "false");
}
/**
 * Determines whether an element is focusable.
 * Read more here: https://stackoverflow.com/questions/1599660/which-html-elements-can-receive-focus/1600194#1600194
 * Or here: https://stackoverflow.com/questions/18261595/how-to-check-if-a-dom-element-is-focusable
 * @param $elem
 */
function isFocusable($elem) {
    // Discard elements that are removed from the tab order.
    if ($elem.getAttribute("tabindex") === "-1" || isHidden($elem) || isDisabled($elem)) {
        return false;
    }
    return (
    // At this point we know that the element can have focus (eg. won't be -1) if the tabindex attribute exists
    $elem.hasAttribute("tabindex")
        // Anchor tags or area tags with a href set
        || ($elem instanceof HTMLAnchorElement || $elem instanceof HTMLAreaElement) && $elem.hasAttribute("href")
        // Form elements which are not disabled
        || ($elem instanceof HTMLButtonElement
            || $elem instanceof HTMLInputElement
            || $elem instanceof HTMLTextAreaElement
            || $elem instanceof HTMLSelectElement)
        // IFrames
        || $elem instanceof HTMLIFrameElement);
}

const CSS = {
  modal: "modal",
  modalOpen: "modal--open",
  title: "title",
  header: "header",
  footer: "footer",
  scrim: "scrim",
  back: "back",
  close: "close",
  secondary: "secondary",
  primary: "primary",
  overflowHidden: "overflow-hidden",
  // these classes help apply the animation in phases to only set transform on open/close
  // this helps avoid a positioning issue for any floating-ui-owning children
  openingIdle: "modal--opening-idle",
  openingActive: "modal--opening-active",
  closingIdle: "modal--closing-idle",
  closingActive: "modal--closing-active"
};
const ICONS = {
  close: "x"
};
const SLOTS = {
  content: "content",
  header: "header",
  back: "back",
  secondary: "secondary",
  primary: "primary"
};
const TEXT = {
  close: "Close"
};

const modalCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{position:fixed;inset:0px;z-index:700;display:flex;align-items:center;justify-content:center;overflow-y:hidden;color:var(--calcite-ui-text-2);opacity:0;visibility:hidden !important;transition:visibility 0ms linear var(--calcite-internal-animation-timing-slow), opacity var(--calcite-internal-animation-timing-slow) cubic-bezier(0.215, 0.44, 0.42, 0.88)}:host([scale=s]){--calcite-modal-padding:0.75rem;--calcite-modal-padding-large:1rem;--calcite-modal-title-text:var(--calcite-font-size-1);--calcite-modal-content-text:var(--calcite-font-size--1);--calcite-modal-padding-internal:0.75rem;--calcite-modal-padding-large-internal:1rem;--calcite-modal-title-text-internal:var(--calcite-font-size-1);--calcite-modal-content-text-internal:var(--calcite-font-size--1)}:host([scale=m]){--calcite-modal-padding:1rem;--calcite-modal-padding-large:1.25rem;--calcite-modal-title-text:var(--calcite-font-size-2);--calcite-modal-content-text:var(--calcite-font-size-0);--calcite-modal-padding-internal:1rem;--calcite-modal-padding-large-internal:1.25rem;--calcite-modal-title-text-internal:var(--calcite-font-size-2);--calcite-modal-content-text-internal:var(--calcite-font-size-0)}:host([scale=l]){--calcite-modal-padding:1.25rem;--calcite-modal-padding-large:1.5rem;--calcite-modal-title-text:var(--calcite-font-size-3);--calcite-modal-content-text:var(--calcite-font-size-1);--calcite-modal-padding-internal:1.25rem;--calcite-modal-padding-large-internal:1.5rem;--calcite-modal-title-text-internal:var(--calcite-font-size-3);--calcite-modal-content-text-internal:var(--calcite-font-size-1)}.scrim{--calcite-scrim-background:rgba(0, 0, 0, 0.75);position:fixed;inset:0px;display:flex;overflow-y:hidden}.modal{pointer-events:none;z-index:800;float:none;margin:1.5rem;box-sizing:border-box;display:flex;inline-size:100%;flex-direction:column;overflow:hidden;border-radius:0.25rem;background-color:var(--calcite-ui-foreground-1);opacity:0;--tw-shadow:0 2px 12px -4px rgba(0, 0, 0, 0.2), 0 2px 4px -2px rgba(0, 0, 0, 0.16);--tw-shadow-colored:0 2px 12px -4px var(--tw-shadow-color), 0 2px 4px -2px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow);-webkit-overflow-scrolling:touch;visibility:hidden;transition:transform var(--calcite-internal-animation-timing-slow) cubic-bezier(0.215, 0.44, 0.42, 0.88), visibility 0ms linear var(--calcite-internal-animation-timing-slow), opacity var(--calcite-internal-animation-timing-slow) cubic-bezier(0.215, 0.44, 0.42, 0.88);--calcite-modal-hidden-position:translate3d(0, 20px, 0);--calcite-modal-shown-position:translate3d(0, 0, 0)}.modal--opening-idle{transform:var(--calcite-modal-hidden-position)}.modal--opening-active{transform:var(--calcite-modal-shown-position)}.modal--closing-idle{transform:var(--calcite-modal-shown-position)}.modal--closing-active{transform:var(--calcite-modal-hidden-position)}:host([open]){opacity:1;visibility:visible !important;transition-delay:0ms}:host([open]) .modal--open{pointer-events:auto;visibility:visible;opacity:1;transition:transform var(--calcite-internal-animation-timing-slow) cubic-bezier(0.215, 0.44, 0.42, 0.88), visibility 0ms linear, opacity var(--calcite-internal-animation-timing-slow) cubic-bezier(0.215, 0.44, 0.42, 0.88), max-inline-size var(--calcite-internal-animation-timing-slow) cubic-bezier(0.215, 0.44, 0.42, 0.88), max-block-size var(--calcite-internal-animation-timing-slow) cubic-bezier(0.215, 0.44, 0.42, 0.88);transition-delay:0ms}.header{z-index:400;display:flex;min-inline-size:0px;max-inline-size:100%;border-start-start-radius:0.25rem;border-start-end-radius:0.25rem;border-width:0px;border-block-end-width:1px;border-style:solid;border-color:var(--calcite-ui-border-3);background-color:var(--calcite-ui-foreground-1);flex:0 0 auto}.close{order:2;margin:0px;cursor:pointer;-webkit-appearance:none;appearance:none;border-style:none;background-color:transparent;color:var(--calcite-ui-text-3);outline-color:transparent;transition:all var(--calcite-animation-timing) ease-in-out 0s, outline 0s, outline-offset 0s;border-start-end-radius:0.25rem;padding-block:var(--calcite-modal-padding, var(--calcite-modal-padding-internal));padding-inline:var(--calcite-modal-padding, var(--calcite-modal-padding-internal));flex:0 0 auto}.close calcite-icon{pointer-events:none;vertical-align:-2px}.close:focus{outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}.close:hover,.close:focus,.close:active{background-color:var(--calcite-ui-foreground-2);color:var(--calcite-ui-text-1)}.title{order:1;display:flex;min-inline-size:0px;align-items:center;flex:1 1 auto;padding-block:var(--calcite-modal-padding, var(--calcite-model-padding-internal));padding-inline:var(--calcite-modal-padding-large, var(--calcite-modal-padding-large-internal))}slot[name=header]::slotted(*),*::slotted([slot=header]){margin:0px;font-weight:var(--calcite-font-weight-normal);color:var(--calcite-ui-text-1);font-size:var(--calcite-modal-title-text, var(--calcite-modal-title-text-internal))}.content{position:relative;box-sizing:border-box;display:block;block-size:100%;overflow:auto;background-color:var(--calcite-ui-foreground-1);padding:0px;max-block-size:calc(100vh - 12rem)}.content--spaced{padding:var(--calcite-modal-padding)}.content--no-footer{border-end-end-radius:0.25rem;border-end-start-radius:0.25rem}slot[name=content]::slotted(*),*::slotted([slot=content]){font-size:var(--calcite-modal-content-text, var(--calcite-modal-context-text-internal))}:host([background-color=grey]) .content{background-color:var(--calcite-ui-background)}.footer{z-index:400;margin-block-start:auto;box-sizing:border-box;display:flex;inline-size:100%;justify-content:space-between;border-end-end-radius:0.25rem;border-end-start-radius:0.25rem;border-width:0px;border-block-start-width:1px;border-style:solid;border-color:var(--calcite-ui-border-3);background-color:var(--calcite-ui-foreground-1);flex:0 0 auto;padding-block:var(--calcite-modal-padding, var(--calcite-modal-padding-internal));padding-inline:var(--calcite-modal-padding-large, var(--calcite-modal-padding-large-internal))}.footer--hide-back .back,.footer--hide-secondary .secondary{display:none}.back{display:block;margin-inline-end:auto}.secondary{margin-inline:0.25rem;display:block}slot[name=primary]{display:block}:host([width=small]) .modal{inline-size:auto}:host([width=s]) .modal{max-inline-size:32rem}@media screen and (max-width: 35rem){:host([width=s]) .modal{margin:0px;block-size:100%;max-block-size:100%;inline-size:100%;max-inline-size:100%;border-radius:0px}:host([width=s]) .content{flex:1 1 auto;max-block-size:unset}:host([width=s][docked]){align-items:flex-end}}:host([width=m]) .modal{max-inline-size:48rem}@media screen and (max-width: 51rem){:host([width=m]) .modal{margin:0px;block-size:100%;max-block-size:100%;inline-size:100%;max-inline-size:100%;border-radius:0px}:host([width=m]) .content{flex:1 1 auto;max-block-size:unset}:host([width=m][docked]){align-items:flex-end}}:host([width=l]) .modal{max-inline-size:94rem}@media screen and (max-width: 97rem){:host([width=l]) .modal{margin:0px;block-size:100%;max-block-size:100%;inline-size:100%;max-inline-size:100%;border-radius:0px}:host([width=l]) .content{flex:1 1 auto;max-block-size:unset}:host([width=l][docked]){align-items:flex-end}}:host([fullscreen]){background-color:transparent}:host([fullscreen]) .modal{margin:0px;block-size:100%;max-block-size:100%;inline-size:100%;max-inline-size:100%;--calcite-modal-hidden-position:translate3D(0, 20px, 0) scale(0.95);--calcite-modal-shown-position:translate3D(0, 0, 0) scale(1)}:host([fullscreen]) .content{max-block-size:100%;flex:1 1 auto}:host([open][fullscreen]) .header{border-radius:0}:host([open][fullscreen]) .footer{border-radius:0}:host([docked]) .modal{block-size:auto}:host([docked]) .content{block-size:auto;flex:1 1 auto}@media screen and (max-width: 860px){:host([docked]) .modal{border-radius:var(--calcite-border-radius) var(--calcite-border-radius) 0 0}:host([docked]) .close{border-start-end-radius:var(--calcite-border-radius)}}:host([color=red]) .modal{border-color:var(--calcite-ui-danger)}:host([color=blue]) .modal{border-color:var(--calcite-ui-info)}:host([color=red]) .modal,:host([color=blue]) .modal{border-width:0px;border-block-start-width:4px;border-style:solid}:host([color=red]) .header,:host([color=blue]) .header{border-radius:0.25rem;border-end-end-radius:0px;border-end-start-radius:0px}@media screen and (max-width: 860px){slot[name=header]::slotted(*),*::slotted([slot=header]){font-size:var(--calcite-font-size-1)}.footer{position:sticky;inset-block-end:0px}}@media screen and (max-width: 480px){.footer{flex-direction:column}.back,.secondary{margin:0px;margin-block-end:0.25rem}}";

const isFocusableExtended = (el) => {
  return isCalciteFocusable(el) || isFocusable(el);
};
const getFocusableElements = (el) => {
  return queryShadowRoot(el, isHidden, isFocusableExtended);
};
const Modal = /*@__PURE__*/ proxyCustomElement(class extends HTMLElement {
  constructor() {
    super();
    this.__registerHost();
    this.__attachShadow();
    this.calciteModalBeforeClose = createEvent(this, "calciteModalBeforeClose", 6);
    this.calciteModalClose = createEvent(this, "calciteModalClose", 6);
    this.calciteModalBeforeOpen = createEvent(this, "calciteModalBeforeOpen", 6);
    this.calciteModalOpen = createEvent(this, "calciteModalOpen", 6);
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
  get el() { return this; }
  static get watchers() { return {
    "active": ["activeHandler"],
    "open": ["toggleModal"]
  }; }
  static get style() { return modalCss; }
}, [1, "calcite-modal", {
    "active": [1540],
    "open": [1540],
    "beforeClose": [16],
    "disableCloseButton": [516, "disable-close-button"],
    "disableOutsideClose": [516, "disable-outside-close"],
    "intlClose": [1, "intl-close"],
    "docked": [516],
    "disableEscape": [516, "disable-escape"],
    "scale": [513],
    "width": [520],
    "fullscreen": [516],
    "color": [513],
    "backgroundColor": [513, "background-color"],
    "noPadding": [516, "no-padding"],
    "hasFooter": [32],
    "isOpen": [32],
    "focusElement": [64],
    "setFocus": [64],
    "scrollContent": [64]
  }, [[8, "keydown", "handleEscape"]]]);
function defineCustomElement$1() {
  if (typeof customElements === "undefined") {
    return;
  }
  const components = ["calcite-modal", "calcite-icon", "calcite-loader", "calcite-scrim"];
  components.forEach(tagName => { switch (tagName) {
    case "calcite-modal":
      if (!customElements.get(tagName)) {
        customElements.define(tagName, Modal);
      }
      break;
    case "calcite-icon":
      if (!customElements.get(tagName)) {
        defineCustomElement$4();
      }
      break;
    case "calcite-loader":
      if (!customElements.get(tagName)) {
        defineCustomElement$3();
      }
      break;
    case "calcite-scrim":
      if (!customElements.get(tagName)) {
        defineCustomElement$2();
      }
      break;
  } });
}
defineCustomElement$1();

const CalciteModal = Modal;
const defineCustomElement = defineCustomElement$1;

export { CalciteModal, defineCustomElement };
