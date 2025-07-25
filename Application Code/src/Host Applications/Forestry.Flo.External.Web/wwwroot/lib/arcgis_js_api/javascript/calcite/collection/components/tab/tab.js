/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { guid } from "../../utils/guid";
import { nodeListToArray } from "../../utils/dom";
/**
 * @slot - A slot for adding content to the component.
 */
export class Tab {
  constructor() {
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
    this.guid = `calcite-tab-title-${guid()}`;
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
    return (h(Host, { "aria-labelledby": this.labeledBy, id: id }, h("div", { class: "container", role: "tabpanel", tabIndex: this.selected ? 0 : -1 }, h("section", null, h("slot", null)))));
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
    return Array.prototype.indexOf.call(nodeListToArray(this.el.parentElement.children).filter((el) => el.matches("calcite-tab")), this.el);
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
  static get is() { return "calcite-tab"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tab.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tab.css"]
    };
  }
  static get properties() {
    return {
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
          "text": "Specifies a unique name for the component.\n\nWhen specified, use the same value on the `calcite-tab-title`."
        },
        "attribute": "tab",
        "reflect": true
      },
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
          "text": "When `true`, the component's contents are selected.\n\nOnly one tab can be selected within the `calcite-tabs` parent."
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
          "text": "When `true`, the component's contents are selected.\n\nOnly one tab can be selected within the `calcite-tabs` parent."
        },
        "attribute": "selected",
        "reflect": true,
        "defaultValue": "false"
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
        "reflect": true,
        "defaultValue": "\"m\""
      }
    };
  }
  static get states() {
    return {
      "labeledBy": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteInternalTabRegister",
        "name": "calciteInternalTabRegister",
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
          "text": "Returns the index of the component item within the tab array.",
          "tags": []
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
      }];
  }
}
