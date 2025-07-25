/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Fragment } from "@stencil/core";
import { SLOTS } from "./resources";
/**
 * @slot - A slot for adding `calcite-tab`s.
 * @slot tab-nav - A slot for adding a `calcite-tab-nav`.
 */
export class Tabs {
  constructor() {
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
    return (h(Fragment, null, h("slot", { name: SLOTS.tabNav }), h("section", null, h("slot", null))));
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
  static get is() { return "calcite-tabs"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["tabs.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["tabs.css"]
    };
  }
  static get properties() {
    return {
      "layout": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "TabLayout",
          "resolved": "\"center\" | \"inline\"",
          "references": {
            "TabLayout": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the layout of the `calcite-tab-nav`, justifying the `calcite-tab-title`s to the start (`\"inline\"`), or across and centered (`\"center\"`)."
        },
        "attribute": "layout",
        "reflect": true,
        "defaultValue": "\"inline\""
      },
      "position": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "TabPosition",
          "resolved": "\"above\" | \"below\" | \"bottom\" | \"top\"",
          "references": {
            "TabPosition": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the position of the component in relation to the `calcite-tab`s. The `\"above\"` and `\"below\"` values are deprecated."
        },
        "attribute": "position",
        "reflect": true,
        "defaultValue": "\"top\""
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
          "tags": [],
          "text": "When `true`, the component will display with a folder style menu."
        },
        "attribute": "bordered",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get states() {
    return {
      "titles": {},
      "tabs": {}
    };
  }
  static get elementRef() { return "el"; }
  static get listeners() {
    return [{
        "name": "calciteInternalTabTitleRegister",
        "method": "calciteInternalTabTitleRegister",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteTabTitleUnregister",
        "method": "calciteTabTitleUnregister",
        "target": "body",
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalTabRegister",
        "method": "calciteInternalTabRegister",
        "target": undefined,
        "capture": false,
        "passive": false
      }, {
        "name": "calciteTabUnregister",
        "method": "calciteTabUnregister",
        "target": "body",
        "capture": false,
        "passive": false
      }];
  }
}
