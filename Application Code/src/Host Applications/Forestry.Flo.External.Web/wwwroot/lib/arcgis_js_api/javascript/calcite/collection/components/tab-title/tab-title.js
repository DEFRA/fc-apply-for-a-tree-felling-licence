/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { Build, h, Host } from "@stencil/core";
import { guid } from "../../utils/guid";
import { getElementDir, getElementProp, toAriaBoolean } from "../../utils/dom";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding text.
 */
export class TabTitle {
  constructor() {
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
    this.mutationObserver = createObserver("mutation", () => this.updateHasText());
    /** determine if there is slotted text for styling purposes */
    this.hasText = false;
    this.resizeObserver = createObserver("resize", () => {
      this.calciteInternalTabIconChanged.emit();
    });
    this.guid = `calcite-tab-title-${guid()}`;
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
    if (Build.isBrowser) {
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
      this.position = getElementProp(this.parentTabNavEl, "position", this.position);
      this.scale = getElementProp(this.parentTabNavEl, "scale", this.scale);
    }
  }
  render() {
    const id = this.el.id || this.guid;
    const iconStartEl = (h("calcite-icon", { class: "calcite-tab-title--icon icon-start", flipRtl: this.iconFlipRtl === "start" || this.iconFlipRtl === "both", icon: this.iconStart, scale: "s" }));
    const iconEndEl = (h("calcite-icon", { class: "calcite-tab-title--icon icon-end", flipRtl: this.iconFlipRtl === "end" || this.iconFlipRtl === "both", icon: this.iconEnd, scale: "s" }));
    return (h(Host, { "aria-controls": this.controls, "aria-selected": toAriaBoolean(this.selected), id: id, role: "tab", tabIndex: this.selected ? 0 : -1 }, h("div", { class: {
        container: true,
        "container--has-text": this.hasText
      }, ref: (el) => { var _a; return (_a = this.resizeObserver) === null || _a === void 0 ? void 0 : _a.observe(el); } }, this.iconStart ? iconStartEl : null, h("slot", null), this.iconEnd ? iconEndEl : null)));
  }
  async componentDidLoad() {
    this.calciteInternalTabTitleRegister.emit(await this.getTabIdentifier());
  }
  componentDidRender() {
    updateHostInteraction(this, () => {
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
        if (getElementDir(this.el) === "ltr") {
          this.calciteInternalTabsFocusNext.emit();
        }
        else {
          this.calciteInternalTabsFocusPrevious.emit();
        }
        break;
      case "ArrowLeft":
        event.preventDefault();
        if (getElementDir(this.el) === "ltr") {
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
  static get is() { return "calcite-tab-title"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tab-title.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tab-title.css"]
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
              "text": "Use `selected` instead."
            }],
          "text": "When `true`, the component and its respective `calcite-tab` contents are selected.\n\nOnly one tab can be selected within the `calcite-tabs` parent."
        },
        "attribute": "active",
        "reflect": true,
        "defaultValue": "false"
      },
      "selected": {
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
          "text": "When `true`, the component and its respective `calcite-tab` contents are selected.\n\nOnly one tab can be selected within the `calcite-tabs` parent."
        },
        "attribute": "selected",
        "reflect": true,
        "defaultValue": "false"
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
      "iconEnd": {
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
          "text": "Specifies an icon to display at the end of the component."
        },
        "attribute": "icon-end",
        "reflect": true
      },
      "iconFlipRtl": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "FlipContext",
          "resolved": "\"both\" | \"end\" | \"start\"",
          "references": {
            "FlipContext": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "When `true`, the icon will be flipped when the element direction is right-to-left (`\"rtl\"`)."
        },
        "attribute": "icon-flip-rtl",
        "reflect": true
      },
      "iconStart": {
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
          "text": "Specifies an icon to display at the start of the component."
        },
        "attribute": "icon-start",
        "reflect": true
      },
      "layout": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "TabLayout",
          "resolved": "\"center\" | \"inline\"",
          "references": {
            "TabLayout": {
              "location": "import",
              "path": "../tabs/interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "layout",
        "reflect": true
      },
      "position": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "TabPosition",
          "resolved": "\"above\" | \"below\" | \"bottom\" | \"top\"",
          "references": {
            "TabPosition": {
              "location": "import",
              "path": "../tabs/interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "position",
        "reflect": true
      },
      "scale": {
        "type": "string",
        "mutable": true,
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
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "scale",
        "reflect": true
      },
      "bordered": {
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
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "attribute": "bordered",
        "reflect": true,
        "defaultValue": "false"
      },
      "tab": {
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
          "text": "Specifies a unique name for the component.\n\nWhen specified, use the same value on the `calcite-tab`."
        },
        "attribute": "tab",
        "reflect": true
      }
    };
  }
  static get states() {
    return {
      "controls": {},
      "hasText": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteTabsActivate",
        "name": "calciteTabsActivate",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "see",
              "text": "[TabChangeEventDetail](https://github.com/Esri/calcite-components/blob/master/src/components/tab/interfaces.ts#L1)"
            }],
          "text": "Fires when a `calcite-tab` is selected. Emits the `tab` property, or the index position."
        },
        "complexType": {
          "original": "TabChangeEventDetail",
          "resolved": "TabChangeEventDetail",
          "references": {
            "TabChangeEventDetail": {
              "location": "import",
              "path": "../tab/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalTabsActivate",
        "name": "calciteInternalTabsActivate",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "see",
              "text": "[TabChangeEventDetail](https://github.com/Esri/calcite-components/blob/master/src/components/tab/interfaces.ts#L1)"
            }, {
              "name": "internal",
              "text": undefined
            }],
          "text": "Fires when a `calcite-tab` is selected (`event.details`)."
        },
        "complexType": {
          "original": "TabChangeEventDetail",
          "resolved": "TabChangeEventDetail",
          "references": {
            "TabChangeEventDetail": {
              "location": "import",
              "path": "../tab/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalTabsFocusNext",
        "name": "calciteInternalTabsFocusNext",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalTabsFocusPrevious",
        "name": "calciteInternalTabsFocusPrevious",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalTabTitleRegister",
        "name": "calciteInternalTabTitleRegister",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
        },
        "complexType": {
          "original": "TabID",
          "resolved": "number | string",
          "references": {
            "TabID": {
              "location": "import",
              "path": "../tabs/interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalTabIconChanged",
        "name": "calciteInternalTabIconChanged",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": ""
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
      "getTabIndex": {
        "complexType": {
          "signature": "() => Promise<number>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            }
          },
          "return": "Promise<number>"
        },
        "docs": {
          "text": "Returns the index of the title within the `calcite-tab-nav`.",
          "tags": []
        }
      },
      "getTabIdentifier": {
        "complexType": {
          "signature": "() => Promise<TabID>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            },
            "TabID": {
              "location": "import",
              "path": "../tabs/interfaces"
            }
          },
          "return": "Promise<TabID>"
        },
        "docs": {
          "text": "",
          "tags": [{
              "name": "internal",
              "text": undefined
            }]
        }
      },
      "updateAriaInfo": {
        "complexType": {
          "signature": "(tabIds?: string[], titleIds?: string[]) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "tabIds"
                }],
              "text": ""
            }, {
              "tags": [{
                  "name": "param",
                  "text": "titleIds"
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
          "text": "",
          "tags": [{
              "name": "param",
              "text": "tabIds"
            }, {
              "name": "param",
              "text": "titleIds"
            }, {
              "name": "internal",
              "text": undefined
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
        "propName": "selected",
        "methodName": "selectedHandler"
      }];
  }
  static get listeners() {
    return [{
        "name": "calciteInternalTabChange",
        "method": "internalTabChangeHandler",
        "target": "body",
        "capture": false,
        "passive": false
      }, {
        "name": "click",
        "method": "onClick",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "keydown",
        "method": "keyDownHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
