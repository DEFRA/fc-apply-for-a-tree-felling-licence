/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { focusElement } from "../../utils/dom";
/**
 * @slot - A slot for adding `calcite-stepper-item`s.
 */
export class Stepper {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /** When `true`, displays a status icon in the `calcite-stepper-item` heading. */
    this.icon = false;
    /** Defines the layout of the component. */
    this.layout = "horizontal";
    /** When `true`, displays the step number in the `calcite-stepper-item` heading. */
    this.numbered = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    this.itemMap = new Map();
    /** list of sorted Stepper items */
    this.items = [];
    /** list of enabled Stepper items */
    this.enabledItems = [];
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidLoad() {
    // if no stepper items are set as active, default to the first one
    if (typeof this.currentPosition !== "number") {
      this.calciteInternalStepperItemChange.emit({
        position: 0
      });
    }
  }
  render() {
    return (h("slot", { onSlotchange: (event) => {
        const items = event.currentTarget
          .assignedElements()
          .filter((el) => (el === null || el === void 0 ? void 0 : el.tagName) === "CALCITE-STEPPER-ITEM");
        const spacing = Array(items.length).fill("1fr").join(" ");
        this.el.style.gridTemplateAreas = spacing;
        this.el.style.gridTemplateColumns = spacing;
      } }));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  calciteInternalStepperItemKeyEvent(event) {
    const item = event.detail.item;
    const itemToFocus = event.target;
    const isFirstItem = this.itemIndex(itemToFocus) === 0;
    const isLastItem = this.itemIndex(itemToFocus) === this.enabledItems.length - 1;
    switch (item.key) {
      case "ArrowDown":
      case "ArrowRight":
        if (isLastItem) {
          this.focusFirstItem();
        }
        else {
          this.focusNextItem(itemToFocus);
        }
        break;
      case "ArrowUp":
      case "ArrowLeft":
        if (isFirstItem) {
          this.focusLastItem();
        }
        else {
          this.focusPrevItem(itemToFocus);
        }
        break;
      case "Home":
        this.focusFirstItem();
        break;
      case "End":
        this.focusLastItem();
        break;
    }
    event.stopPropagation();
  }
  registerItem(event) {
    const item = event.target;
    const { content, position } = event.detail;
    this.itemMap.set(item, { position, content });
    this.items = this.sortItems();
    this.enabledItems = this.filterItems();
    event.stopPropagation();
  }
  updateItem(event) {
    const { position } = event.detail;
    if (typeof position === "number") {
      this.currentPosition = position;
    }
    this.calciteInternalStepperItemChange.emit({
      position
    });
  }
  handleUserRequestedStepperItemSelect(event) {
    const { position } = event.detail;
    this.calciteStepperItemChange.emit({
      position
    });
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Set the next `calcite-stepper-item` as active. */
  async nextStep() {
    const enabledStepIndex = this.getEnabledStepIndex(this.currentPosition + 1, "next");
    if (typeof enabledStepIndex !== "number") {
      return;
    }
    this.updateStep(enabledStepIndex);
  }
  /** Set the previous `calcite-stepper-item` as active. */
  async prevStep() {
    const enabledStepIndex = this.getEnabledStepIndex(this.currentPosition - 1, "previous");
    if (typeof enabledStepIndex !== "number") {
      return;
    }
    this.updateStep(enabledStepIndex);
  }
  /**
   * Set a specified `calcite-stepper-item` as active.
   *
   * @param step
   */
  async goToStep(step) {
    const position = step - 1;
    if (this.currentPosition !== position) {
      this.updateStep(position);
    }
  }
  /** Set the first `calcite-stepper-item` as active. */
  async startStep() {
    const enabledStepIndex = this.getEnabledStepIndex(0, "next");
    if (typeof enabledStepIndex !== "number") {
      return;
    }
    this.updateStep(enabledStepIndex);
  }
  /** Set the last `calcite-stepper-item` as active. */
  async endStep() {
    const enabledStepIndex = this.getEnabledStepIndex(this.items.length - 1, "previous");
    if (typeof enabledStepIndex !== "number") {
      return;
    }
    this.updateStep(enabledStepIndex);
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  getEnabledStepIndex(startIndex, direction = "next") {
    var _a;
    const { items, currentPosition } = this;
    let newIndex = startIndex;
    while ((_a = items[newIndex]) === null || _a === void 0 ? void 0 : _a.disabled) {
      newIndex = newIndex + (direction === "previous" ? -1 : 1);
    }
    return newIndex !== currentPosition && newIndex < items.length && newIndex >= 0
      ? newIndex
      : null;
  }
  updateStep(position) {
    this.currentPosition = position;
    this.calciteInternalStepperItemChange.emit({
      position
    });
  }
  focusFirstItem() {
    const firstItem = this.enabledItems[0];
    focusElement(firstItem);
  }
  focusLastItem() {
    const lastItem = this.enabledItems[this.enabledItems.length - 1];
    focusElement(lastItem);
  }
  focusNextItem(el) {
    const index = this.itemIndex(el);
    const nextItem = this.enabledItems[index + 1] || this.enabledItems[0];
    focusElement(nextItem);
  }
  focusPrevItem(el) {
    const index = this.itemIndex(el);
    const prevItem = this.enabledItems[index - 1] || this.enabledItems[this.enabledItems.length - 1];
    focusElement(prevItem);
  }
  itemIndex(el) {
    return this.enabledItems.indexOf(el);
  }
  sortItems() {
    const { itemMap } = this;
    return Array.from(itemMap.keys()).sort((a, b) => itemMap.get(a).position - itemMap.get(b).position);
  }
  filterItems() {
    return this.items.filter((item) => !item.disabled);
  }
  static get is() { return "calcite-stepper"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["stepper.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["stepper.css"]
    };
  }
  static get properties() {
    return {
      "icon": {
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
          "text": "When `true`, displays a status icon in the `calcite-stepper-item` heading."
        },
        "attribute": "icon",
        "reflect": true,
        "defaultValue": "false"
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
          "text": "Defines the layout of the component."
        },
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"horizontal\""
      },
      "numbered": {
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
          "text": "When `true`, displays the step number in the `calcite-stepper-item` heading."
        },
        "attribute": "numbered",
        "reflect": true,
        "defaultValue": "false"
      },
      "numberingSystem": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "NumberingSystem",
          "resolved": "\"arab\" | \"arabext\" | \"bali\" | \"beng\" | \"deva\" | \"fullwide\" | \"gujr\" | \"guru\" | \"hanidec\" | \"khmr\" | \"knda\" | \"laoo\" | \"latn\" | \"limb\" | \"mlym\" | \"mong\" | \"mymr\" | \"orya\" | \"tamldec\" | \"telu\" | \"thai\" | \"tibt\"",
          "references": {
            "NumberingSystem": {
              "location": "import",
              "path": "../../utils/locale"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the Unicode numeral system used by the component for localization."
        },
        "attribute": "numbering-system",
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
          "text": "Specifies the size of the component."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteStepperItemChange",
        "name": "calciteStepperItemChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the active `calcite-stepper-item` changes."
        },
        "complexType": {
          "original": "StepperItemChangeEventDetail",
          "resolved": "StepperItemChangeEventDetail",
          "references": {
            "StepperItemChangeEventDetail": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalStepperItemChange",
        "name": "calciteInternalStepperItemChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Fires when the active `calcite-stepper-item` changes."
        },
        "complexType": {
          "original": "StepperItemChangeEventDetail",
          "resolved": "StepperItemChangeEventDetail",
          "references": {
            "StepperItemChangeEventDetail": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        }
      }];
  }
  static get methods() {
    return {
      "nextStep": {
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
          "text": "Set the next `calcite-stepper-item` as active.",
          "tags": []
        }
      },
      "prevStep": {
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
          "text": "Set the previous `calcite-stepper-item` as active.",
          "tags": []
        }
      },
      "goToStep": {
        "complexType": {
          "signature": "(step: number) => Promise<void>",
          "parameters": [{
              "tags": [{
                  "name": "param",
                  "text": "step"
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
          "text": "Set a specified `calcite-stepper-item` as active.",
          "tags": [{
              "name": "param",
              "text": "step"
            }]
        }
      },
      "startStep": {
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
          "text": "Set the first `calcite-stepper-item` as active.",
          "tags": []
        }
      },
      "endStep": {
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
          "text": "Set the last `calcite-stepper-item` as active.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "calciteInternalStepperItemKeyEvent",
        "method": "calciteInternalStepperItemKeyEvent",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalStepperItemRegister",
        "method": "registerItem",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalStepperItemSelect",
        "method": "updateItem",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalUserRequestedStepperItemSelect",
        "method": "handleUserRequestedStepperItemSelect",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
