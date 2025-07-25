/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

const index = require('./index-b27b231c.js');
const guid = require('./guid-acbbb0e7.js');
const dom = require('./dom-2b919cb6.js');
const observers = require('./observers-664fbf90.js');
const interactive = require('./interactive-3d681fb9.js');
require('./resources-3fd6da1b.js');

const tabCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([selected]) section,:host([selected]) .container{display:block}:host{display:none;block-size:100%;inline-size:100%}:host([selected]){display:block;block-size:100%;inline-size:100%;overflow:auto}section,.container{display:none;block-size:100%;inline-size:100%}:host([scale=s]){padding-block:0.25rem;font-size:var(--calcite-font-size--2);line-height:1rem}:host([scale=m]){padding-block:0.5rem;font-size:var(--calcite-font-size--1);line-height:1rem}:host([scale=l]){padding-block:0.75rem;font-size:var(--calcite-font-size-0);line-height:1.25rem}";

const Tab = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteInternalTabRegister = index.createEvent(this, "calciteInternalTabRegister", 6);
    /**
     * When `true`, the component's contents are selected.
     *
     * Only one tab can be selected within the `calcite-tabs` parent.
     *
     * @deprecated Use `selected` instead.
     */
    this.active = false;
    /**
     * When `true`, the component's contents are selected.
     *
     * Only one tab can be selected within the `calcite-tabs` parent.
     */
    this.selected = false;
    /**
     * @internal
     */
    this.scale = "m";
    this.guid = `calcite-tab-title-${guid.guid()}`;
  }
  activeHandler(value) {
    this.selected = value;
  }
  selectedHandler(value) {
    this.active = value;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  render() {
    const id = this.el.id || this.guid;
    return (index.h(index.Host, { "aria-labelledby": this.labeledBy, id: id }, index.h("div", { class: "container", role: "tabpanel", tabIndex: this.selected ? 0 : -1 }, index.h("section", null, index.h("slot", null)))));
  }
  connectedCallback() {
    this.parentTabsEl = this.el.closest("calcite-tabs");
    const isSelected = this.selected || this.active;
    if (isSelected) {
      this.activeHandler(isSelected);
      this.selectedHandler(isSelected);
    }
  }
  componentDidLoad() {
    this.calciteInternalTabRegister.emit();
  }
  componentWillRender() {
    var _a;
    this.scale = (_a = this.parentTabsEl) === null || _a === void 0 ? void 0 : _a.scale;
  }
  disconnectedCallback() {
    var _a;
    // Dispatching to body in order to be listened by other elements that are still connected to the DOM.
    (_a = document.body) === null || _a === void 0 ? void 0 : _a.dispatchEvent(new CustomEvent("calciteTabUnregister", {
      detail: this.el
    }));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  internalTabChangeHandler(event) {
    const targetTabsEl = event
      .composedPath()
      .find((el) => el.tagName === "CALCITE-TABS");
    // to allow `<calcite-tabs>` to be nested we need to make sure this
    // `calciteTabChange` event was actually fired from a within the same
    // `<calcite-tabs>` that is the a parent of this tab.
    if (targetTabsEl !== this.parentTabsEl) {
      return;
    }
    if (this.tab) {
      this.selected = this.tab === event.detail.tab;
    }
    else {
      this.getTabIndex().then((index) => {
        this.selected = index === event.detail.tab;
      });
    }
    event.stopPropagation();
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /**
   * Returns the index of the component item within the tab array.
   */
  async getTabIndex() {
    return Array.prototype.indexOf.call(dom.nodeListToArray(this.el.parentElement.children).filter((el) => el.matches("calcite-tab")), this.el);
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  /**
   * @param tabIds
   * @param titleIds
   * @internal
   */
  async updateAriaInfo(tabIds = [], titleIds = []) {
    this.labeledBy = titleIds[tabIds.indexOf(this.el.id)] || null;
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "active": ["activeHandler"],
    "selected": ["selectedHandler"]
  }; }
};
Tab.style = tabCss;

const tabNavCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{position:relative;display:flex}:host([scale=s]){min-block-size:1.5rem}:host([scale=m]){min-block-size:2rem}:host([scale=l]){min-block-size:2.75rem}.tab-nav{display:flex;inline-size:100%;justify-content:flex-start;overflow:auto}.tab-nav-active-indicator-container{position:absolute;inset-inline:0px;inset-block-end:0px;block-size:0.125rem;inline-size:100%;overflow:hidden}.tab-nav-active-indicator{position:absolute;inset-block-end:0px;display:block;block-size:0.125rem;background-color:var(--calcite-ui-brand);transition-property:all;transition-timing-function:cubic-bezier(0.4, 0, 0.2, 1);transition-duration:150ms;transition-timing-function:cubic-bezier(0, 0, 0.2, 1)}:host([position=below]) .tab-nav-active-indicator{inset-block-end:unset;inset-block-start:0px}:host([position=bottom]) .tab-nav-active-indicator{inset-block-end:unset;inset-block-start:0px}:host([position=below]) .tab-nav-active-indicator-container{inset-block-start:0px;inset-block-end:unset}:host([position=bottom]) .tab-nav-active-indicator-container{inset-block-end:unset;inset-block-start:0px}:host([bordered]) .tab-nav-active-indicator-container{inset-block-end:unset}:host([bordered][position=below]) .tab-nav-active-indicator-container{inset-block-end:0;inset-block-start:unset}:host([bordered][position=bottom]) .tab-nav-active-indicator-container{inset-block-end:0;inset-block-start:unset}@media (forced-colors: active){.tab-nav-active-indicator{background-color:highlight}}";

const TabNav = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteTabChange = index.createEvent(this, "calciteTabChange", 6);
    this.calciteInternalTabChange = index.createEvent(this, "calciteInternalTabChange", 6);
    /**
     * @internal
     */
    this.scale = "m";
    /**
     * @internal
     */
    this.layout = "inline";
    /**
     * @internal
     */
    this.position = "bottom";
    /**
     * @internal
     */
    this.bordered = false;
    this.animationActiveDuration = 0.3;
    this.resizeObserver = observers.createObserver("resize", () => {
      // remove active indicator transition duration during resize to prevent wobble
      this.activeIndicatorEl.style.transitionDuration = "0s";
      this.updateActiveWidth();
      this.updateOffsetPosition();
    });
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.handleContainerScroll = () => {
      // remove active indicator transition duration while container is scrolling to prevent wobble
      this.activeIndicatorEl.style.transitionDuration = "0s";
      this.updateOffsetPosition();
    };
  }
  async selectedTabChanged() {
    if (localStorage &&
      this.storageId &&
      this.selectedTab !== undefined &&
      this.selectedTab !== null) {
      localStorage.setItem(`calcite-tab-nav-${this.storageId}`, JSON.stringify(this.selectedTab));
    }
    this.calciteInternalTabChange.emit({
      tab: this.selectedTab
    });
    this.selectedTabEl = await this.getTabTitleById(this.selectedTab);
  }
  selectedTabElChanged() {
    this.updateOffsetPosition();
    this.updateActiveWidth();
    // reset the animation time on tab selection
    this.activeIndicatorEl.style.transitionDuration = `${this.animationActiveDuration}s`;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    this.parentTabsEl = this.el.closest("calcite-tabs");
    (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el);
  }
  disconnectedCallback() {
    var _a;
    (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  componentWillLoad() {
    const storageKey = `calcite-tab-nav-${this.storageId}`;
    if (localStorage && this.storageId && localStorage.getItem(storageKey)) {
      const storedTab = JSON.parse(localStorage.getItem(storageKey));
      this.selectedTab = storedTab;
    }
  }
  componentWillRender() {
    const { parentTabsEl } = this;
    this.layout = parentTabsEl === null || parentTabsEl === void 0 ? void 0 : parentTabsEl.layout;
    this.position = parentTabsEl === null || parentTabsEl === void 0 ? void 0 : parentTabsEl.position;
    this.scale = parentTabsEl === null || parentTabsEl === void 0 ? void 0 : parentTabsEl.scale;
    this.bordered = parentTabsEl === null || parentTabsEl === void 0 ? void 0 : parentTabsEl.bordered;
    // fix issue with active tab-title not lining up with blue indicator
    if (this.selectedTabEl) {
      this.updateOffsetPosition();
    }
  }
  componentDidRender() {
    // if every tab title is active select the first tab.
    if (this.tabTitles.length &&
      this.tabTitles.every((title) => !title.active) &&
      !this.selectedTab) {
      this.tabTitles[0].getTabIdentifier().then((tab) => {
        this.calciteInternalTabChange.emit({
          tab
        });
      });
    }
  }
  render() {
    const dir = dom.getElementDir(this.el);
    const width = `${this.indicatorWidth}px`;
    const offset = `${this.indicatorOffset}px`;
    const indicatorStyle = dir !== "rtl" ? { width, left: offset } : { width, right: offset };
    return (index.h(index.Host, { role: "tablist" }, index.h("div", { class: "tab-nav", onScroll: this.handleContainerScroll, ref: (el) => (this.tabNavEl = el) }, index.h("div", { class: "tab-nav-active-indicator-container", ref: (el) => (this.activeIndicatorContainerEl = el) }, index.h("div", { class: "tab-nav-active-indicator", ref: (el) => (this.activeIndicatorEl = el), style: indicatorStyle })), index.h("slot", null))));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  focusPreviousTabHandler(event) {
    const currentIndex = this.getIndexOfTabTitle(event.target, this.enabledTabTitles);
    const previousTab = this.enabledTabTitles[currentIndex - 1] ||
      this.enabledTabTitles[this.enabledTabTitles.length - 1];
    previousTab === null || previousTab === void 0 ? void 0 : previousTab.focus();
    event.stopPropagation();
    event.preventDefault();
  }
  focusNextTabHandler(event) {
    const currentIndex = this.getIndexOfTabTitle(event.target, this.enabledTabTitles);
    const nextTab = this.enabledTabTitles[currentIndex + 1] || this.enabledTabTitles[0];
    nextTab === null || nextTab === void 0 ? void 0 : nextTab.focus();
    event.stopPropagation();
    event.preventDefault();
  }
  internalActivateTabHandler(event) {
    this.selectedTab = event.detail.tab
      ? event.detail.tab
      : this.getIndexOfTabTitle(event.target);
    event.stopPropagation();
    event.preventDefault();
  }
  activateTabHandler(event) {
    this.calciteTabChange.emit({
      tab: this.selectedTab
    });
    event.stopPropagation();
    event.preventDefault();
  }
  /**
   * Check for active tabs on register and update selected
   *
   * @param event
   */
  updateTabTitles(event) {
    if (event.target.active) {
      this.selectedTab = event.detail;
    }
  }
  globalInternalTabChangeHandler(event) {
    if (this.syncId &&
      event.target !== this.el &&
      event.target.syncId === this.syncId &&
      this.selectedTab !== event.detail.tab) {
      this.selectedTab = event.detail.tab;
    }
    event.stopPropagation();
  }
  iconStartChangeHandler() {
    this.updateActiveWidth();
  }
  updateOffsetPosition() {
    var _a, _b, _c, _d, _e;
    const dir = dom.getElementDir(this.el);
    const navWidth = (_a = this.activeIndicatorContainerEl) === null || _a === void 0 ? void 0 : _a.offsetWidth;
    const tabLeft = (_b = this.selectedTabEl) === null || _b === void 0 ? void 0 : _b.offsetLeft;
    const tabWidth = (_c = this.selectedTabEl) === null || _c === void 0 ? void 0 : _c.offsetWidth;
    const offsetRight = navWidth - (tabLeft + tabWidth);
    this.indicatorOffset =
      dir !== "rtl" ? tabLeft - ((_d = this.tabNavEl) === null || _d === void 0 ? void 0 : _d.scrollLeft) : offsetRight + ((_e = this.tabNavEl) === null || _e === void 0 ? void 0 : _e.scrollLeft);
  }
  updateActiveWidth() {
    var _a;
    this.indicatorWidth = (_a = this.selectedTabEl) === null || _a === void 0 ? void 0 : _a.offsetWidth;
  }
  getIndexOfTabTitle(el, tabTitles = this.tabTitles) {
    // In most cases, since these indexes correlate with tab contents, we want to consider all tab titles.
    // However, when doing relative index operations, it makes sense to pass in this.enabledTabTitles as the 2nd arg.
    return tabTitles.indexOf(el);
  }
  async getTabTitleById(id) {
    return Promise.all(this.tabTitles.map((el) => el.getTabIdentifier())).then((ids) => {
      return this.tabTitles[ids.indexOf(id)];
    });
  }
  get tabTitles() {
    return dom.filterDirectChildren(this.el, "calcite-tab-title");
  }
  get enabledTabTitles() {
    return dom.filterDirectChildren(this.el, "calcite-tab-title:not([disabled])");
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "selectedTab": ["selectedTabChanged"],
    "selectedTabEl": ["selectedTabElChanged"]
  }; }
};
TabNav.style = tabNavCss;

const tabTitleCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host([disabled]){pointer-events:none;cursor:default;-webkit-user-select:none;user-select:none;opacity:var(--calcite-ui-opacity-disabled)}:host{display:block;flex:0 1 auto;outline:2px solid transparent;outline-offset:2px;margin-inline-start:0px;margin-inline-end:1.25rem}:host([layout=center]){margin-block:0px;margin-inline:1.25rem;text-align:center;flex-basis:12rem;margin:auto}:host([position=below]) .container{border-block-end-width:0px;border-block-start-width:2px;border-block-start-color:transparent;border-block-start-style:solid}:host([position=bottom]) .container{border-block-end-width:0px;border-block-start-width:2px;border-block-start-color:transparent;border-block-start-style:solid}:host .container{outline-color:transparent}:host(:focus) .container{outline:2px solid var(--calcite-ui-brand);outline-offset:-2px}:host(:active) a,:host(:focus) a,:host(:hover) a{border-color:var(--calcite-ui-border-2);color:var(--calcite-ui-text-1);text-decoration-line:none}:host([selected]) .container{border-color:transparent;color:var(--calcite-ui-text-1)}:host([disabled]) .container{pointer-events:none;opacity:0.5}:host([disabled]) ::slotted([calcite-hydrated][disabled]),:host([disabled]) [calcite-hydrated][disabled]{opacity:1}:host([scale=s]){margin-inline-end:1rem}:host([scale=s]) .container{padding-block:0.25rem;font-size:var(--calcite-font-size--2);line-height:1rem}:host([scale=m]) .container{padding-block:0.5rem;font-size:var(--calcite-font-size--1);line-height:1rem}:host([scale=l]){margin-inline-end:1.5rem}:host([scale=l]) .container{padding-block:0.75rem;font-size:var(--calcite-font-size-0);line-height:1.25rem}.container{box-sizing:border-box;display:flex;block-size:100%;inline-size:100%;cursor:pointer;-webkit-appearance:none;appearance:none;justify-content:center;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;border-block-end-width:2px;padding-inline:0px;padding-block:0.5rem;font-size:var(--calcite-font-size--1);line-height:1rem;color:var(--calcite-ui-text-3);transition:all var(--calcite-animation-timing) ease-in-out 0s, outline 0s, outline-offset 0s;border-block-end-color:transparent;border-block-end-style:solid}.calcite-tab-title--icon{position:relative;margin:0px;display:inline-flex;align-self:center}.calcite-tab-title--icon svg{transition:all var(--calcite-animation-timing) ease-in-out 0s, outline 0s, outline-offset 0s}.container--has-text{padding:0.25rem}.container--has-text .calcite-tab-title--icon.icon-start{margin-inline-end:0.5rem}.container--has-text .calcite-tab-title--icon.icon-end{margin-inline-start:0.5rem}:host([icon-start][icon-end]) .calcite-tab-title--icon:first-child{margin-inline-end:0.5rem}:host([bordered]){margin-inline-end:0}:host([bordered][selected]){box-shadow:inset 0px -2px var(--calcite-ui-foreground-1)}:host([bordered][selected][position=below]){box-shadow:inset 0 2px 0 var(--calcite-ui-foreground-1)}:host([bordered][selected][position=bottom]){box-shadow:inset 0 2px 0 var(--calcite-ui-foreground-1)}:host([bordered]:hover) .container,:host([bordered]:focus) .container,:host([bordered]:active) .container{position:relative}:host([bordered]:hover) .container{background-color:var(--calcite-button-transparent-hover)}:host([bordered]) .container{border-block-end-style:unset;border-inline-start:1px solid transparent;border-inline-end:1px solid transparent}:host([bordered][position=below]) .container{border-block-start-style:unset}:host([bordered][position=bottom]) .container{border-block-start-style:unset}:host([selected][bordered]) .container{border-inline-start-color:var(--calcite-ui-border-1);border-inline-end-color:var(--calcite-ui-border-1)}:host([bordered]) .container{padding-inline:0.75rem}:host([bordered][scale=s]) .container{padding-inline:0.5rem}:host([bordered][scale=l]) .container{padding-inline:1rem}@media (forced-colors: active){:host{outline-width:0;outline-offset:0}:host(:focus) .container{outline-color:highlight}:host([bordered]) .container{border-block-end-style:solid}:host([bordered][position=below]) .container{border-block-start-style:solid}:host([bordered][position=bottom]) .container{border-block-start-style:solid}:host([bordered][selected]) .container{border-block-end-style:none}:host([bordered][position=below][selected]) .container{border-block-start-style:none}:host([bordered][position=bottom][selected]) .container{border-block-start-style:none}}";

const TabTitle = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    this.calciteTabsActivate = index.createEvent(this, "calciteTabsActivate", 6);
    this.calciteInternalTabsActivate = index.createEvent(this, "calciteInternalTabsActivate", 6);
    this.calciteInternalTabsFocusNext = index.createEvent(this, "calciteInternalTabsFocusNext", 6);
    this.calciteInternalTabsFocusPrevious = index.createEvent(this, "calciteInternalTabsFocusPrevious", 6);
    this.calciteInternalTabTitleRegister = index.createEvent(this, "calciteInternalTabTitleRegister", 6);
    this.calciteInternalTabIconChanged = index.createEvent(this, "calciteInternalTabIconChanged", 6);
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /**
     * When `true`, the component and its respective `calcite-tab` contents are selected.
     *
     * Only one tab can be selected within the `calcite-tabs` parent.
     *
     * @deprecated Use `selected` instead.
     */
    this.active = false;
    /**
     * When `true`, the component and its respective `calcite-tab` contents are selected.
     *
     * Only one tab can be selected within the `calcite-tabs` parent.
     */
    this.selected = false;
    /** When `true`, interaction is prevented and the component is displayed with lower opacity.  */
    this.disabled = false;
    /**
     * @internal
     */
    this.bordered = false;
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    /** watches for changing text content */
    this.mutationObserver = observers.createObserver("mutation", () => this.updateHasText());
    /** determine if there is slotted text for styling purposes */
    this.hasText = false;
    this.resizeObserver = observers.createObserver("resize", () => {
      this.calciteInternalTabIconChanged.emit();
    });
    this.guid = `calcite-tab-title-${guid.guid()}`;
  }
  activeHandler(value) {
    this.selected = value;
  }
  selectedHandler(value) {
    this.active = value;
    if (this.selected) {
      this.emitActiveTab(false);
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    const { selected, active } = this;
    if (selected) {
      this.active = selected;
    }
    else if (active) {
      this.activeHandler(active);
    }
    this.setupTextContentObserver();
    this.parentTabNavEl = this.el.closest("calcite-tab-nav");
    this.parentTabsEl = this.el.closest("calcite-tabs");
  }
  disconnectedCallback() {
    var _a, _b, _c;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    // Dispatching to body in order to be listened by other elements that are still connected to the DOM.
    (_b = document.body) === null || _b === void 0 ? void 0 : _b.dispatchEvent(new CustomEvent("calciteTabTitleUnregister", {
      detail: this.el
    }));
    (_c = this.resizeObserver) === null || _c === void 0 ? void 0 : _c.disconnect();
  }
  componentWillLoad() {
    {
      this.updateHasText();
    }
    if (this.tab && this.selected) {
      this.emitActiveTab(false);
    }
  }
  componentWillRender() {
    if (this.parentTabsEl) {
      this.layout = this.parentTabsEl.layout;
      this.position = this.parentTabsEl.position;
      this.scale = this.parentTabsEl.scale;
      this.bordered = this.parentTabsEl.bordered;
    }
    // handle case when tab-nav is only parent
    if (!this.parentTabsEl && this.parentTabNavEl) {
      this.position = dom.getElementProp(this.parentTabNavEl, "position", this.position);
      this.scale = dom.getElementProp(this.parentTabNavEl, "scale", this.scale);
    }
  }
  render() {
    const id = this.el.id || this.guid;
    const iconStartEl = (index.h("calcite-icon", { class: "calcite-tab-title--icon icon-start", flipRtl: this.iconFlipRtl === "start" || this.iconFlipRtl === "both", icon: this.iconStart, scale: "s" }));
    const iconEndEl = (index.h("calcite-icon", { class: "calcite-tab-title--icon icon-end", flipRtl: this.iconFlipRtl === "end" || this.iconFlipRtl === "both", icon: this.iconEnd, scale: "s" }));
    return (index.h(index.Host, { "aria-controls": this.controls, "aria-selected": dom.toAriaBoolean(this.selected), id: id, role: "tab", tabIndex: this.selected ? 0 : -1 }, index.h("div", { class: {
        container: true,
        "container--has-text": this.hasText
      }, ref: (el) => { var _a; return (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.observe(el); } }, this.iconStart ? iconStartEl : null, index.h("slot", null), this.iconEnd ? iconEndEl : null)));
  }
  async componentDidLoad() {
    this.calciteInternalTabTitleRegister.emit(await this.getTabIdentifier());
  }
  componentDidRender() {
    interactive.updateHostInteraction(this, () => {
      return this.selected;
    });
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  internalTabChangeHandler(event) {
    const targetTabsEl = event
      .composedPath()
      .find((el) => el.tagName === "CALCITE-TABS");
    if (targetTabsEl !== this.parentTabsEl) {
      return;
    }
    if (this.tab) {
      this.selected = this.tab === event.detail.tab;
    }
    else {
      this.getTabIndex().then((index) => {
        this.selected = index === event.detail.tab;
      });
    }
    event.stopPropagation();
  }
  onClick() {
    this.emitActiveTab();
  }
  keyDownHandler(event) {
    switch (event.key) {
      case " ":
      case "Enter":
        this.emitActiveTab();
        event.preventDefault();
        break;
      case "ArrowRight":
        event.preventDefault();
        if (dom.getElementDir(this.el) === "ltr") {
          this.calciteInternalTabsFocusNext.emit();
        }
        else {
          this.calciteInternalTabsFocusPrevious.emit();
        }
        break;
      case "ArrowLeft":
        event.preventDefault();
        if (dom.getElementDir(this.el) === "ltr") {
          this.calciteInternalTabsFocusPrevious.emit();
        }
        else {
          this.calciteInternalTabsFocusNext.emit();
        }
        break;
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /**
   * Returns the index of the title within the `calcite-tab-nav`.
   */
  async getTabIndex() {
    return Array.prototype.indexOf.call(this.el.parentElement.querySelectorAll("calcite-tab-title"), this.el);
  }
  /**
   * @internal
   */
  async getTabIdentifier() {
    return this.tab ? this.tab : this.getTabIndex();
  }
  /**
   * @param tabIds
   * @param titleIds
   * @internal
   */
  async updateAriaInfo(tabIds = [], titleIds = []) {
    this.controls = tabIds[titleIds.indexOf(this.el.id)] || null;
  }
  updateHasText() {
    this.hasText = this.el.textContent.trim().length > 0;
  }
  setupTextContentObserver() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
  }
  emitActiveTab(userTriggered = true) {
    if (this.disabled) {
      return;
    }
    const payload = { tab: this.tab };
    this.calciteInternalTabsActivate.emit(payload);
    if (userTriggered) {
      this.calciteTabsActivate.emit(payload);
    }
  }
  get el() { return index.getElement(this); }
  static get watchers() { return {
    "active": ["activeHandler"],
    "selected": ["selectedHandler"]
  }; }
};
TabTitle.style = tabTitleCss;

const SLOTS = {
  tabNav: "tab-nav"
};

const tabsCss = "@keyframes in{0%{opacity:0}100%{opacity:1}}@keyframes in-down{0%{opacity:0;transform:translate3D(0, -5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-up{0%{opacity:0;transform:translate3D(0, 5px, 0)}100%{opacity:1;transform:translate3D(0, 0, 0)}}@keyframes in-scale{0%{opacity:0;transform:scale3D(0.95, 0.95, 1)}100%{opacity:1;transform:scale3D(1, 1, 1)}}:root{--calcite-animation-timing:calc(150ms * var(--calcite-internal-duration-factor));--calcite-internal-duration-factor:var(--calcite-duration-factor, 1);--calcite-internal-animation-timing-fast:calc(100ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-medium:calc(200ms * var(--calcite-internal-duration-factor));--calcite-internal-animation-timing-slow:calc(300ms * var(--calcite-internal-duration-factor))}.calcite-animate{opacity:0;animation-fill-mode:both;animation-duration:var(--calcite-animation-timing)}.calcite-animate__in{animation-name:in}.calcite-animate__in-down{animation-name:in-down}.calcite-animate__in-up{animation-name:in-up}.calcite-animate__in-scale{animation-name:in-scale}@media (prefers-reduced-motion: reduce){:root{--calcite-internal-duration-factor:0.01}}:root{--calcite-floating-ui-transition:var(--calcite-animation-timing)}:host([hidden]){display:none}:host{display:flex;flex-direction:column}:host([bordered]){box-shadow:inset 0 1px 0 var(--calcite-ui-border-1);background-color:var(--calcite-ui-foreground-1)}:host([bordered]:not([position=below])) ::slotted(calcite-tab-nav){margin-block-end:-1px}:host([bordered]:not([position=bottom])) ::slotted(calcite-tab-nav){margin-block-end:-1px}:host([bordered][position=below]) ::slotted(calcite-tab-nav){margin-block-start:-1px}:host([bordered][position=below]){box-shadow:inset 0 1px 0 var(--calcite-ui-border-1), inset 0 -1px 0 var(--calcite-ui-border-1)}:host([bordered][position=bottom]){box-shadow:inset 0 1px 0 var(--calcite-ui-border-1), inset 0 -1px 0 var(--calcite-ui-border-1)}:host([bordered]) section{border-width:1px;border-style:solid;border-color:var(--calcite-ui-border-1)}:host([bordered][scale=s]) section{padding:0.75rem}:host([bordered][scale=m]) section{padding:0.5rem}:host([bordered][scale=l]) section{padding:1rem}:host([position=below]){flex-direction:column-reverse}:host([position=bottom]){flex-direction:column-reverse}section{display:flex;flex-grow:1;overflow:hidden;border-block-start-width:1px;border-block-start-color:var(--calcite-ui-border-1);border-block-start-style:solid}:host([position=below]) section{flex-direction:column-reverse;border-block-start-width:0px;border-block-end-width:1px;border-block-end-color:var(--calcite-ui-border-1)}:host([position=bottom]) section{flex-direction:column-reverse;border-block-start-width:0px;border-block-end-width:1px;border-block-end-color:var(--calcite-ui-border-1)}:host([position=below]:not([bordered])) section{border-block-end-style:solid}:host([position=bottom]:not([bordered])) section{border-block-end-style:solid}@media (forced-colors: active){:host([bordered]) section{border-block-start-width:0px;border-block-end-width:1px}:host([position=below][bordered]) section{border-block-start-width:1px;border-block-end-width:0px}:host([position=bottom][bordered]) section{border-block-start-width:1px;border-block-end-width:0px}}";

const Tabs = class {
  constructor(hostRef) {
    index.registerInstance(this, hostRef);
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**
     * Specifies the layout of the `calcite-tab-nav`, justifying the `calcite-tab-title`s to the start (`"inline"`), or across and centered (`"center"`).
     */
    this.layout = "inline";
    /**
     * Specifies the position of the component in relation to the `calcite-tab`s. The `"above"` and `"below"` values are deprecated.
     *
     */
    this.position = "top";
    /**
     * Specifies the size of the component.
     */
    this.scale = "m";
    /**
     * When `true`, the component will display with a folder style menu.
     */
    this.bordered = false;
    //--------------------------------------------------------------------------
    //
    //  Events
    //
    //--------------------------------------------------------------------------
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    /**
     *
     * Stores an array of ids of `<calcite-tab-titles>`s to match up ARIA
     * attributes.
     */
    this.titles = [];
    /**
     *
     * Stores an array of ids of `<calcite-tab>`s to match up ARIA attributes.
     */
    this.tabs = [];
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  render() {
    return (index.h(index.Fragment, null, index.h("slot", { name: SLOTS.tabNav }), index.h("section", null, index.h("slot", null))));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  /**
   * @param event
   * @internal
   */
  calciteInternalTabTitleRegister(event) {
    this.titles = [...this.titles, event.target];
    this.registryHandler();
    event.stopPropagation();
  }
  /**
   * @param event
   * @internal
   */
  calciteTabTitleUnregister(event) {
    this.titles = this.titles.filter((el) => el !== event.detail);
    this.registryHandler();
    event.stopPropagation();
  }
  /**
   * @param event
   * @internal
   */
  calciteInternalTabRegister(event) {
    this.tabs = [...this.tabs, event.target];
    this.registryHandler();
    event.stopPropagation();
  }
  /**
   * @param event
   * @internal
   */
  calciteTabUnregister(event) {
    this.tabs = this.tabs.filter((el) => el !== event.detail);
    this.registryHandler();
    event.stopPropagation();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  /**
   *
   * Matches up elements from the internal `tabs` and `titles` to automatically
   * update the ARIA attributes and link `<calcite-tab>` and
   * `<calcite-tab-title>` components.
   */
  async registryHandler() {
    let tabIds;
    let titleIds;
    // determine if we are using `tab` based or `index` based tab identifiers.
    if (this.tabs.some((el) => el.tab) || this.titles.some((el) => el.tab)) {
      // if we are using `tab` based identifiers sort by `tab` to account for
      // possible out of order tabs and get the id of each tab
      tabIds = this.tabs.sort((a, b) => a.tab.localeCompare(b.tab)).map((el) => el.id);
      titleIds = this.titles.sort((a, b) => a.tab.localeCompare(b.tab)).map((el) => el.id);
    }
    else {
      // if we are using index based tabs then the `<calcite-tab>` and
      // `<calcite-tab-title>` might have been rendered out of order so the
      // order of `this.tabs` and `this.titles` might not reflect the DOM state,
      // and might not match each other so we need to get the index of all the
      // tabs and titles in the DOM order to match them up as a source of truth
      const tabDomIndexes = await Promise.all(this.tabs.map((el) => el.getTabIndex()));
      const titleDomIndexes = await Promise.all(this.titles.map((el) => el.getTabIndex()));
      // once we have the DOM order as a source of truth we can build the
      // matching tabIds and titleIds arrays
      tabIds = tabDomIndexes.reduce((ids, indexInDOM, registryIndex) => {
        ids[indexInDOM] = this.tabs[registryIndex].id;
        return ids;
      }, []);
      titleIds = titleDomIndexes.reduce((ids, indexInDOM, registryIndex) => {
        ids[indexInDOM] = this.titles[registryIndex].id;
        return ids;
      }, []);
    }
    // pass all our new aria information to each `<calcite-tab>` and
    // `<calcite-tab-title>` which will check if they can update their internal
    // `controlled` or `labeledBy` states and re-render if necessary
    this.tabs.forEach((el) => el.updateAriaInfo(tabIds, titleIds));
    this.titles.forEach((el) => el.updateAriaInfo(tabIds, titleIds));
  }
  get el() { return index.getElement(this); }
};
Tabs.style = tabsCss;

exports.calcite_tab = Tab;
exports.calcite_tab_nav = TabNav;
exports.calcite_tab_title = TabTitle;
exports.calcite_tabs = Tabs;
