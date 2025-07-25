/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import Sortable from "sortablejs";
import { h } from "@stencil/core";
import { createObserver } from "../../utils/observers";
import { CSS } from "./resources";
import { updateHostInteraction } from "../../utils/interactive";
/**
 * @slot - A slot for adding sortable items.
 */
export class SortableList {
  constructor() {
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
  static get is() { return "calcite-sortable-list"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["sortable-list.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["sortable-list.css"]
    };
  }
  static get properties() {
    return {
      "dragSelector": {
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
          "text": "Specifies which items inside the element should be draggable."
        },
        "attribute": "drag-selector",
        "reflect": true
      },
      "group": {
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
          "text": "The list's group identifier.\n\nTo drag elements from one list into another, both lists must have the same group value."
        },
        "attribute": "group",
        "reflect": true
      },
      "handleSelector": {
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
          "text": "The selector for the handle elements."
        },
        "attribute": "handle-selector",
        "reflect": true,
        "defaultValue": "\"calcite-handle\""
      },
      "layout": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Layout",
          "resolved": "\"grid\" | \"horizontal\" | \"vertical\"",
          "references": {
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
          "text": "Indicates the horizontal or vertical orientation of the component."
        },
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"vertical\""
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
          "text": "When true, disabled prevents interaction. This state shows items with lower opacity/grayed."
        },
        "attribute": "disabled",
        "reflect": true,
        "defaultValue": "false"
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
          "text": "When true, content is waiting to be loaded. This state shows a busy indicator."
        },
        "attribute": "loading",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get states() {
    return {
      "handleActivated": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteListOrderChange",
        "name": "calciteListOrderChange",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emitted when the order of the list has changed."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }];
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "calciteHandleNudge",
        "method": "calciteHandleNudgeHandler",
        "target": undefined,
        "capture": false,
        "passive": false
      }];
  }
}
