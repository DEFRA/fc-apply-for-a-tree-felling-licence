/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { r as registerInstance, c as createEvent, h, g as getElement } from './index-7b536c47.js';
import { S as Sortable } from './sortable.esm-de934228.js';
import { c as createObserver } from './observers-513bffd3.js';
import { u as updateHostInteraction } from './interactive-fc7d4437.js';

const CSS = {
  sortItem: "sort-item",
  container: "container",
  containerHorizontal: "container--horizontal",
  containerVertical: "container--vertical"
};

const sortableListCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:flex}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}.container{display:flex;flex:1 1 auto}.container--vertical{flex-direction:column}.container--horizontal{flex-direction:row}";

const SortableList = class {
  constructor(hostRef) {
    registerInstance(this, hostRef);
    this.calciteListOrderChange = createEvent(this, "calciteListOrderChange", 6);
    /**
     * The selector for the handle elements.
     */
    this.handleSelector = "calcite-handle";
    /**
     * Indicates the horizontal or vertical orientation of the component.
     */
    this.layout = "vertical";
    /**
     * When true, disabled prevents interaction. This state shows items with lower opacity/grayed.
     */
    this.disabled = false;
    /**
     * When true, content is waiting to be loaded. This state shows a busy indicator.
     */
    this.loading = false;
    this.handleActivated = false;
    this.items = [];
    this.mutationObserver = createObserver("mutation", () => {
      this.cleanUpDragAndDrop();
      this.items = Array.from(this.el.children);
      this.setUpDragAndDrop();
    });
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    this.items = Array.from(this.el.children);
    this.setUpDragAndDrop();
    this.beginObserving();
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    this.cleanUpDragAndDrop();
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  calciteHandleNudgeHandler(event) {
    var _a;
    const sortItem = this.items.find((item) => {
      return item.contains(event.detail.handle) || event.composedPath().includes(item);
    });
    const lastIndex = this.items.length - 1;
    const startingIndex = this.items.indexOf(sortItem);
    let appendInstead = false;
    let buddyIndex;
    switch (event.detail.direction) {
      case "up":
        event.preventDefault();
        if (startingIndex === 0) {
          appendInstead = true;
        }
        else {
          buddyIndex = startingIndex - 1;
        }
        break;
      case "down":
        event.preventDefault();
        if (startingIndex === lastIndex) {
          buddyIndex = 0;
        }
        else if (startingIndex === lastIndex - 1) {
          appendInstead = true;
        }
        else {
          buddyIndex = startingIndex + 2;
        }
        break;
      default:
        return;
    }
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    if (appendInstead) {
      sortItem.parentElement.appendChild(sortItem);
    }
    else {
      sortItem.parentElement.insertBefore(sortItem, this.items[buddyIndex]);
    }
    this.items = Array.from(this.el.children);
    event.detail.handle.activated = true;
    event.detail.handle.setFocus();
    this.beginObserving();
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  setUpDragAndDrop() {
    this.cleanUpDragAndDrop();
    const options = {
      dataIdAttr: "id",
      group: this.group,
      handle: this.handleSelector,
      // Changed sorting within list
      onUpdate: () => {
        this.items = Array.from(this.el.children);
        this.calciteListOrderChange.emit();
      },
      // Element dragging started
      onStart: () => {
        var _a;
        (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
      },
      // Element dragging ended
      onEnd: () => {
        this.beginObserving();
      }
    };
    if (this.dragSelector) {
      options.draggable = this.dragSelector;
    }
    this.sortable = Sortable.create(this.el, options);
  }
  cleanUpDragAndDrop() {
    var _a;
    (_a = this.sortable) === null || _a === void 0 ? void 0 : _a.destroy();
    this.sortable = null;
  }
  beginObserving() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const { layout } = this;
    const horizontal = layout === "horizontal" || false;
    return (h("div", { class: {
        [CSS.container]: true,
        [CSS.containerVertical]: !horizontal,
        [CSS.containerHorizontal]: horizontal
      } }, h("slot", null)));
  }
  get el() { return getElement(this); }
};
SortableList.style = sortableListCss;

export { SortableList as calcite_sortable_list };
