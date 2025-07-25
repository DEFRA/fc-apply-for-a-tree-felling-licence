/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { focusElement, getRootNode, nodeListToArray } from "../../utils/dom";
import { TreeSelectionMode } from "./interfaces";
import { getEnabledSiblingItem } from "./utils";
/**
 * @slot - A slot for `calcite-tree-item` elements.
 */
export class Tree {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** Displays indentation guide lines. */
    this.lines = false;
    /**
     * Display input
     *
     * @deprecated Use `selectionMode="ancestors"` for checkbox input.
     */
    this.inputEnabled = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    /**
     * Customize how the component's selection works.
     *
     * @default "single"
     * @see [TreeSelectionMode](https://github.com/Esri/calcite-components/blob/master/src/components/tree/interfaces.ts#L5)
     */
    this.selectionMode = TreeSelectionMode.Single;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentWillRender() {
    var _a;
    const parent = (_a = this.el.parentElement) === null || _a === void 0 ? void 0 : _a.closest("calcite-tree");
    this.lines = parent ? parent.lines : this.lines;
    this.scale = parent ? parent.scale : this.scale;
    this.selectionMode = parent ? parent.selectionMode : this.selectionMode;
    this.child = !!parent;
  }
  render() {
    return (h(Host, { "aria-multiselectable": this.child
        ? undefined
        : (this.selectionMode === TreeSelectionMode.Multi ||
          this.selectionMode === TreeSelectionMode.Multiple ||
          this.selectionMode === TreeSelectionMode.MultiChildren).toString(), role: !this.child ? "tree" : undefined, tabIndex: this.getRootTabIndex() }, h("slot", null)));
  }
  //--------------------------------------------------------------------------
  //
  //  Event Listeners
  //
  //--------------------------------------------------------------------------
  onFocus() {
    if (!this.child) {
      const focusTarget = this.el.querySelector("calcite-tree-item[selected]:not([disabled])") || this.el.querySelector("calcite-tree-item:not([disabled])");
      focusElement(focusTarget);
    }
  }
  onFocusIn(event) {
    const focusedFromRootOrOutsideTree = event.relatedTarget === this.el || !this.el.contains(event.relatedTarget);
    if (focusedFromRootOrOutsideTree) {
      // gives user the ability to tab into external elements (modifying tabindex property will not work in firefox)
      this.el.removeAttribute("tabindex");
    }
  }
  onFocusOut(event) {
    const willFocusOutsideTree = !this.el.contains(event.relatedTarget);
    if (willFocusOutsideTree) {
      this.el.tabIndex = this.getRootTabIndex();
    }
  }
  onClick(event) {
    const target = event.target;
    const childItems = nodeListToArray(target.querySelectorAll("calcite-tree-item"));
    if (this.child) {
      return;
    }
    if (!this.child) {
      event.preventDefault();
      event.stopPropagation();
    }
    if (this.selectionMode === TreeSelectionMode.Ancestors && !this.child) {
      this.updateAncestorTree(event);
      return;
    }
    const isNoneSelectionMode = this.selectionMode === TreeSelectionMode.None;
    const shouldSelect = this.selectionMode !== null &&
      (!target.hasChildren ||
        (target.hasChildren &&
          (this.selectionMode === TreeSelectionMode.Children ||
            this.selectionMode === TreeSelectionMode.MultiChildren)));
    const shouldModifyToCurrentSelection = !isNoneSelectionMode &&
      event.detail.modifyCurrentSelection &&
      (this.selectionMode === TreeSelectionMode.Multi ||
        this.selectionMode === TreeSelectionMode.Multiple ||
        this.selectionMode === TreeSelectionMode.MultiChildren);
    const shouldSelectChildren = this.selectionMode === TreeSelectionMode.MultiChildren ||
      this.selectionMode === TreeSelectionMode.Children;
    const shouldClearCurrentSelection = !shouldModifyToCurrentSelection &&
      (((this.selectionMode === TreeSelectionMode.Single ||
        this.selectionMode === TreeSelectionMode.Multi ||
        this.selectionMode === TreeSelectionMode.Multiple) &&
        childItems.length <= 0) ||
        this.selectionMode === TreeSelectionMode.Children ||
        this.selectionMode === TreeSelectionMode.MultiChildren);
    const shouldExpandTarget = this.selectionMode === TreeSelectionMode.Children ||
      this.selectionMode === TreeSelectionMode.MultiChildren;
    if (!this.child) {
      const targetItems = [];
      if (shouldSelect) {
        targetItems.push(target);
      }
      if (shouldSelectChildren) {
        childItems.forEach((treeItem) => {
          targetItems.push(treeItem);
        });
      }
      if (shouldClearCurrentSelection) {
        const selectedItems = nodeListToArray(this.el.querySelectorAll("calcite-tree-item[selected]"));
        selectedItems.forEach((treeItem) => {
          if (!targetItems.includes(treeItem)) {
            treeItem.selected = false;
          }
        });
      }
      if (shouldExpandTarget && !event.detail.forceToggle) {
        target.expanded = true;
      }
      if (shouldModifyToCurrentSelection) {
        window.getSelection().removeAllRanges();
      }
      if ((shouldModifyToCurrentSelection && target.selected) ||
        (shouldSelectChildren && event.detail.forceToggle)) {
        targetItems.forEach((treeItem) => {
          if (!treeItem.disabled) {
            treeItem.selected = false;
          }
        });
      }
      else if (!isNoneSelectionMode) {
        targetItems.forEach((treeItem) => {
          if (!treeItem.disabled) {
            treeItem.selected = true;
          }
        });
      }
    }
    const selected = isNoneSelectionMode
      ? [target]
      : nodeListToArray(this.el.querySelectorAll("calcite-tree-item")).filter((i) => i.selected);
    this.calciteTreeSelect.emit({ selected });
    event.stopPropagation();
  }
  keyDownHandler(event) {
    var _a;
    const root = this.el.closest("calcite-tree:not([child])");
    const target = event.target;
    if (!(root === this.el && target.tagName === "CALCITE-TREE-ITEM" && this.el.contains(target))) {
      return;
    }
    if (event.key === "ArrowDown") {
      const next = getEnabledSiblingItem(target.nextElementSibling, "down");
      if (next) {
        next.focus();
        event.preventDefault();
      }
      return;
    }
    if (event.key === "ArrowUp") {
      const previous = getEnabledSiblingItem(target.previousElementSibling, "up");
      if (previous) {
        previous.focus();
        event.preventDefault();
      }
    }
    if (event.key === "ArrowLeft" && !target.disabled) {
      // When focus is on an open node, closes the node.
      if (target.hasChildren && target.expanded) {
        target.expanded = false;
        event.preventDefault();
        return;
      }
      // When focus is on a child node that is also either an end node or a closed node, moves focus to its parent node.
      const parentItem = target.parentElement.closest("calcite-tree-item");
      if (parentItem && (!target.hasChildren || target.expanded === false)) {
        parentItem.focus();
        event.preventDefault();
        return;
      }
      // When focus is on a root node that is also either an end node or a closed node, does nothing.
      return;
    }
    if (event.key === "ArrowRight" && !target.disabled) {
      if (target.hasChildren) {
        if (target.expanded && getRootNode(this.el).activeElement === target) {
          // When focus is on an open node, moves focus to the first child node.
          (_a = getEnabledSiblingItem(target.querySelector("calcite-tree-item"), "down")) === null || _a === void 0 ? void 0 : _a.focus();
          event.preventDefault();
        }
        else {
          // When focus is on a closed node, opens the node; focus does not move.
          target.expanded = true;
          event.preventDefault();
        }
      }
      return;
    }
  }
  updateAncestorTree(event) {
    const item = event.target;
    if (item.disabled) {
      return;
    }
    const ancestors = [];
    let parent = item.parentElement.closest("calcite-tree-item");
    while (parent) {
      ancestors.push(parent);
      parent = parent.parentElement.closest("calcite-tree-item");
    }
    const childItems = Array.from(item.querySelectorAll("calcite-tree-item:not([disabled])"));
    const childItemsWithNoChildren = childItems.filter((child) => !child.hasChildren);
    const childItemsWithChildren = childItems.filter((child) => child.hasChildren);
    const futureSelected = item.hasChildren
      ? !(item.selected || item.indeterminate)
      : !item.selected;
    childItemsWithNoChildren.forEach((el) => {
      el.selected = futureSelected;
      el.indeterminate = false;
    });
    function updateItemState(childItems, item) {
      const selected = childItems.filter((child) => child.selected);
      const unselected = childItems.filter((child) => !child.selected);
      item.selected = selected.length === childItems.length;
      item.indeterminate = selected.length > 0 && unselected.length > 0;
    }
    childItemsWithChildren.forEach((el) => {
      const directChildItems = Array.from(el.querySelectorAll(":scope > calcite-tree > calcite-tree-item"));
      updateItemState(directChildItems, el);
    });
    if (item.hasChildren) {
      updateItemState(childItems, item);
    }
    else {
      item.selected = futureSelected;
      item.indeterminate = false;
    }
    ancestors.forEach((ancestor) => {
      const descendants = nodeListToArray(ancestor.querySelectorAll("calcite-tree-item"));
      const activeDescendants = descendants.filter((el) => el.selected);
      if (activeDescendants.length === 0) {
        ancestor.selected = false;
        ancestor.indeterminate = false;
        return;
      }
      const indeterminate = activeDescendants.length < descendants.length;
      ancestor.indeterminate = indeterminate;
      ancestor.selected = !indeterminate;
    });
    this.calciteTreeSelect.emit({
      selected: nodeListToArray(this.el.querySelectorAll("calcite-tree-item")).filter((i) => i.selected)
    });
  }
  // --------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  getRootTabIndex() {
    return !this.child ? 0 : -1;
  }
  static get is() { return "calcite-tree"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tree.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tree.css"]
    };
  }
  static get properties() {
    return {
      "lines": {
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
          "text": "Displays indentation guide lines."
        },
        "attribute": "lines",
        "reflect": true,
        "defaultValue": "false"
      },
      "inputEnabled": {
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
              "text": "Use `selectionMode=\"ancestors\"` for checkbox input."
            }],
          "text": "Display input"
        },
        "attribute": "input-enabled",
        "reflect": false,
        "defaultValue": "false"
      },
      "child": {
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
        "attribute": "child",
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
          "tags": [],
          "text": "Specifies the size of the component."
        },
        "attribute": "scale",
        "reflect": true,
        "defaultValue": "\"m\""
      },
      "selectionMode": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "TreeSelectionMode",
          "resolved": "TreeSelectionMode.Ancestors | TreeSelectionMode.Children | TreeSelectionMode.Multi | TreeSelectionMode.MultiChildren | TreeSelectionMode.Multiple | TreeSelectionMode.None | TreeSelectionMode.Single",
          "references": {
            "TreeSelectionMode": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "default",
              "text": "\"single\""
            }, {
              "name": "see",
              "text": "[TreeSelectionMode](https://github.com/Esri/calcite-components/blob/master/src/components/tree/interfaces.ts#L5)"
            }],
          "text": "Customize how the component's selection works."
        },
        "attribute": "selection-mode",
        "reflect": true,
        "defaultValue": "TreeSelectionMode.Single"
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteTreeSelect",
        "name": "calciteTreeSelect",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "see",
              "text": "[TreeSelectDetail](https://github.com/Esri/calcite-components/blob/master/src/components/tree/interfaces.ts#L1)"
            }],
          "text": "Fires when the user selects/deselects `calcite-tree-items`. An object including an array of selected items will be passed in the event's `detail` property."
        },
        "complexType": {
          "original": "TreeSelectDetail",
          "resolved": "TreeSelectDetail",
          "references": {
            "TreeSelectDetail": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        }
      }];
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "focus",
        "method": "onFocus",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "focusin",
        "method": "onFocusIn",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "focusout",
        "method": "onFocusOut",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalTreeItemSelect",
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
