/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { CSS, HEADING_LEVEL, ICONS, SLOTS, TEXT } from "./resources";
import { getSlotted, toAriaBoolean } from "../../utils/dom";
import { Heading } from "../functional/Heading";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
import { updateHostInteraction } from "../../utils/interactive";
import { guid } from "../../utils/guid";
/**
 * @slot - A slot for adding content to the component.
 * @slot icon - A slot for adding a leading header icon with `calcite-icon`.
 * @slot control - A slot for adding a single HTML input element in a header.
 * @slot header-menu-actions - A slot for adding an overflow menu with `calcite-action`s inside a dropdown.
 */
export class Block {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Properties
    //
    // --------------------------------------------------------------------------
    /**
     * When `true`, the component is collapsible.
     */
    this.collapsible = false;
    /**
     * When `true`, interaction is prevented and the component is displayed with lower opacity.
     */
    this.disabled = false;
    /**
     * When `true`, displays a drag handle in the header.
     */
    this.dragHandle = false;
    /**
     * Accessible name for the component's collapse button.
     *
     * @default "Collapse"
     */
    this.intlCollapse = TEXT.collapse;
    /**
     * Accessible name for the component's expand button.
     *
     * @default "Expand"
     */
    this.intlExpand = TEXT.expand;
    /**
     * Accessible name when the component is loading.
     *
     * @default "Loading"
     */
    this.intlLoading = TEXT.loading;
    /**
     * Accessible name for the component's options button.
     *
     * @default "Options"
     */
    this.intlOptions = TEXT.options;
    /**
     * When `true`, a busy indicator is displayed.
     */
    this.loading = false;
    /**
     * When `true`, expands the component and its contents.
     */
    this.open = false;
    /**
     * When `true`, removes padding for the slotted content.
     *
     * @deprecated Use `--calcite-block-padding` CSS variable instead.
     */
    this.disablePadding = false;
    this.guid = guid();
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.onHeaderClick = () => {
      this.open = !this.open;
      this.calciteBlockToggle.emit();
    };
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  componentDidRender() {
    updateHostInteraction(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  // --------------------------------------------------------------------------
  //
  //  Render Methods
  //
  // --------------------------------------------------------------------------
  renderScrim() {
    const { loading } = this;
    const defaultSlot = h("slot", null);
    return [loading ? h("calcite-scrim", { loading: loading }) : null, defaultSlot];
  }
  renderIcon() {
    const { el, status } = this;
    const showingLoadingStatus = this.loading && !this.open;
    const statusIcon = showingLoadingStatus ? ICONS.refresh : ICONS[status];
    const hasIcon = getSlotted(el, SLOTS.icon) || statusIcon;
    const iconEl = !statusIcon ? (h("slot", { key: "icon-slot", name: SLOTS.icon })) : (h("calcite-icon", { class: {
        [CSS.statusIcon]: true,
        [CSS.valid]: status == "valid",
        [CSS.invalid]: status == "invalid",
        [CSS.loading]: showingLoadingStatus
      }, icon: statusIcon, scale: "m" }));
    return hasIcon ? h("div", { class: CSS.icon }, iconEl) : null;
  }
  renderTitle() {
    const { heading, headingLevel, summary, description } = this;
    return heading || summary || description ? (h("div", { class: CSS.title }, h(Heading, { class: CSS.heading, level: headingLevel || HEADING_LEVEL }, heading), summary || description ? (h("div", { class: CSS.description }, summary || description)) : null)) : null;
  }
  render() {
    const { collapsible, el, intlCollapse, intlExpand, loading, open, intlLoading } = this;
    const toggleLabel = open ? intlCollapse || TEXT.collapse : intlExpand || TEXT.expand;
    const headerContent = (h("header", { class: CSS.header }, this.renderIcon(), this.renderTitle()));
    const hasControl = !!getSlotted(el, SLOTS.control);
    const hasMenuActions = !!getSlotted(el, SLOTS.headerMenuActions);
    const collapseIcon = open ? ICONS.opened : ICONS.closed;
    const { guid } = this;
    const regionId = `${guid}-region`;
    const buttonId = `${guid}-button`;
    const headerNode = (h("div", { class: CSS.headerContainer }, this.dragHandle ? h("calcite-handle", null) : null, collapsible ? (h("button", { "aria-controls": regionId, "aria-expanded": collapsible ? toAriaBoolean(open) : null, "aria-label": toggleLabel, class: CSS.toggle, id: buttonId, onClick: this.onHeaderClick, title: toggleLabel }, headerContent, !hasControl && !hasMenuActions ? (h("calcite-icon", { "aria-hidden": "true", class: CSS.toggleIcon, icon: collapseIcon, scale: "s" })) : null)) : (headerContent), loading ? (h("calcite-loader", { inline: true, "is-active": true, label: intlLoading })) : hasControl ? (h("div", { class: CSS.controlContainer }, h("slot", { name: SLOTS.control }))) : null, hasMenuActions ? (h("calcite-action-menu", { label: this.intlOptions || TEXT.options }, h("slot", { name: SLOTS.headerMenuActions }))) : null));
    return (h(Host, null, h("article", { "aria-busy": toAriaBoolean(loading), class: {
        [CSS.container]: true
      } }, headerNode, h("section", { "aria-expanded": toAriaBoolean(open), "aria-labelledby": buttonId, class: {
        [CSS.content]: true,
        [CSS.contentSpaced]: !this.disablePadding
      }, hidden: !open, id: regionId }, this.renderScrim()))));
  }
  static get is() { return "calcite-block"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["block.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["block.css"]
    };
  }
  static get properties() {
    return {
      "collapsible": {
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
          "text": "When `true`, the component is collapsible."
        },
        "attribute": "collapsible",
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
      "dragHandle": {
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
          "text": "When `true`, displays a drag handle in the header."
        },
        "attribute": "drag-handle",
        "reflect": true,
        "defaultValue": "false"
      },
      "heading": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": true,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The component header text."
        },
        "attribute": "heading",
        "reflect": false
      },
      "headingLevel": {
        "type": "number",
        "mutable": false,
        "complexType": {
          "original": "HeadingLevel",
          "resolved": "1 | 2 | 3 | 4 | 5 | 6",
          "references": {
            "HeadingLevel": {
              "location": "import",
              "path": "../functional/Heading"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the number at which section headings should start."
        },
        "attribute": "heading-level",
        "reflect": true
      },
      "intlCollapse": {
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
          "tags": [{
              "name": "default",
              "text": "\"Collapse\""
            }],
          "text": "Accessible name for the component's collapse button."
        },
        "attribute": "intl-collapse",
        "reflect": false,
        "defaultValue": "TEXT.collapse"
      },
      "intlExpand": {
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
          "tags": [{
              "name": "default",
              "text": "\"Expand\""
            }],
          "text": "Accessible name for the component's expand button."
        },
        "attribute": "intl-expand",
        "reflect": false,
        "defaultValue": "TEXT.expand"
      },
      "intlLoading": {
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
          "tags": [{
              "name": "default",
              "text": "\"Loading\""
            }],
          "text": "Accessible name when the component is loading."
        },
        "attribute": "intl-loading",
        "reflect": false,
        "defaultValue": "TEXT.loading"
      },
      "intlOptions": {
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
          "tags": [{
              "name": "default",
              "text": "\"Options\""
            }],
          "text": "Accessible name for the component's options button."
        },
        "attribute": "intl-options",
        "reflect": false,
        "defaultValue": "TEXT.options"
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
          "text": "When `true`, a busy indicator is displayed."
        },
        "attribute": "loading",
        "reflect": true,
        "defaultValue": "false"
      },
      "open": {
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
          "text": "When `true`, expands the component and its contents."
        },
        "attribute": "open",
        "reflect": true,
        "defaultValue": "false"
      },
      "status": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Status",
          "resolved": "\"idle\" | \"invalid\" | \"valid\"",
          "references": {
            "Status": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Displays a status-related indicator icon."
        },
        "attribute": "status",
        "reflect": true
      },
      "summary": {
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
          "tags": [{
              "name": "deprecated",
              "text": "use `description` instead"
            }],
          "text": "A description for the component, which displays below the heading."
        },
        "attribute": "summary",
        "reflect": false
      },
      "description": {
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
          "text": "A description for the component, which displays below the heading."
        },
        "attribute": "description",
        "reflect": false
      },
      "disablePadding": {
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
              "text": "Use `--calcite-block-padding` CSS variable instead."
            }],
          "text": "When `true`, removes padding for the slotted content."
        },
        "attribute": "disable-padding",
        "reflect": true,
        "defaultValue": "false"
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteBlockToggle",
        "name": "calciteBlockToggle",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Emits when the component's header is clicked."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }];
  }
  static get elementRef() { return "el"; }
}
