/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { CSS } from "./resources";
import { createObserver } from "../../utils/observers";
/**
 * @slot - A slot for adding `calcite-flow-item` or `calcite-panel`s (deprecated) to the flow.
 */
export class Flow {
  constructor() {
    this.flowDirection = null;
    this.itemCount = 0;
    this.items = [];
    this.itemMutationObserver = createObserver("mutation", () => this.updateFlowProps());
    this.getFlowDirection = (oldFlowItemCount, newFlowItemCount) => {
      const allowRetreatingDirection = oldFlowItemCount > 1;
      const allowAdvancingDirection = oldFlowItemCount && newFlowItemCount > 1;
      if (!allowAdvancingDirection && !allowRetreatingDirection) {
        return null;
      }
      return newFlowItemCount < oldFlowItemCount ? "retreating" : "advancing";
    };
    this.updateFlowProps = () => {
      const { el, items } = this;
      const newItems = Array.from(el.querySelectorAll("calcite-flow-item, calcite-panel")).filter((flowItem) => !flowItem.matches("calcite-flow-item calcite-flow-item, calcite-panel calcite-panel"));
      const oldItemCount = items.length;
      const newItemCount = newItems.length;
      const activeItem = newItems[newItemCount - 1];
      const previousItem = newItems[newItemCount - 2];
      if (newItemCount && activeItem) {
        newItems.forEach((itemNode) => {
          itemNode.showBackButton = itemNode === activeItem && newItemCount > 1;
          itemNode.hidden = itemNode !== activeItem;
        });
      }
      if (previousItem) {
        previousItem.menuOpen = false;
      }
      this.items = newItems;
      if (oldItemCount !== newItemCount) {
        const flowDirection = this.getFlowDirection(oldItemCount, newItemCount);
        this.itemCount = newItemCount;
        this.flowDirection = flowDirection;
      }
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  /**
   * Removes the currently active `calcite-flow-item` or `calcite-panel`.
   */
  async back() {
    const { items } = this;
    const lastItem = items[items.length - 1];
    if (!lastItem) {
      return;
    }
    const beforeBack = lastItem.beforeBack
      ? lastItem.beforeBack
      : () => Promise.resolve();
    return beforeBack.call(lastItem).then(() => {
      lastItem.remove();
      return lastItem;
    });
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    var _a;
    (_a = this.itemMutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
    this.updateFlowProps();
  }
  disconnectedCallback() {
    var _a;
    (_a = this.itemMutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  // --------------------------------------------------------------------------
  handleItemBackClick() {
    this.back();
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  render() {
    const { flowDirection } = this;
    const frameDirectionClasses = {
      [CSS.frame]: true,
      [CSS.frameAdvancing]: flowDirection === "advancing",
      [CSS.frameRetreating]: flowDirection === "retreating"
    };
    return (h("div", { class: frameDirectionClasses }, h("slot", null)));
  }
  static get is() { return "calcite-flow"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["flow.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["flow.css"]
    };
  }
  static get states() {
    return {
      "flowDirection": {},
      "itemCount": {},
      "items": {}
    };
  }
  static get methods() {
    return {
      "back": {
        "complexType": {
          "signature": "() => Promise<HTMLCalciteFlowItemElement>",
          "parameters": [],
          "references": {
            "Promise": {
              "location": "global"
            },
            "HTMLCalciteFlowItemElement": {
              "location": "global"
            }
          },
          "return": "Promise<HTMLCalciteFlowItemElement>"
        },
        "docs": {
          "text": "Removes the currently active `calcite-flow-item` or `calcite-panel`.",
          "tags": []
        }
      }
    };
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "calciteFlowItemBackClick",
        "method": "handleItemBackClick",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calcitePanelBackClick",
        "method": "handleItemBackClick",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
