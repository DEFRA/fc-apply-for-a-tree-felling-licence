/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { getSlotted, toAriaBoolean } from "../../utils/dom";
import { CSS, SLOTS, TEXT } from "./resources";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * Cards do not include a grid or bounding container
 * - cards will expand to fit the width of their container
 */
/**
 * @slot - A slot for adding subheader/description content.
 * @slot thumbnail - A slot for adding a thumbnail to the component.
 * @slot title - A slot for adding a title.
 * @slot subtitle - A slot for adding a subtitle or short summary.
 * @slot footer-leading - A slot for adding a leading footer.
 * @slot footer-trailing - A slot for adding a trailing footer.
 */
export class Card {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Public Properties
    //
    //--------------------------------------------------------------------------
    /**  When `true`, a busy indicator is displayed. */
    this.loading = false;
    /** When `true`, the component is selected. */
    this.selected = false;
    /** When `true`, the component is selectable. */
    this.selectable = false;
    /**
     * Accessible name when the component is loading.
     *
     * @default "Loading"
     */
    this.intlLoading = TEXT.loading;
    /**
     * When `selectable` is `true`, the accessible name for the component's checkbox for selection.
     *
     * @default "Select"
     */
    this.intlSelect = TEXT.select;
    /**
     * When `selectable` is `true`, the accessible name for the component's checkbox for deselection.
     *
     * @default "Deselect"
     */
    this.intlDeselect = TEXT.deselect;
    /** Sets the placement of the thumbnail defined in the `thumbnail` slot. */
    this.thumbnailPosition = "block-start";
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.cardSelectClick = () => {
      this.selectCard();
    };
    this.cardSelectKeyDown = (event) => {
      switch (event.key) {
        case " ":
        case "Enter":
          this.selectCard();
          event.preventDefault();
          break;
      }
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  // --------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
  }
  disonnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  render() {
    const thumbnailInline = this.thumbnailPosition.startsWith("inline");
    const thumbnailStart = this.thumbnailPosition.endsWith("start");
    return (h("div", { class: { "calcite-card-container": true, inline: thumbnailInline } }, this.loading ? (h("div", { class: "calcite-card-loader-container" }, h("calcite-loader", { active: true, label: this.intlLoading }))) : null, thumbnailStart && this.renderThumbnail(), h("section", { "aria-busy": toAriaBoolean(this.loading), class: { [CSS.container]: true } }, this.selectable ? this.renderCheckbox() : null, this.renderHeader(), h("div", { class: "card-content" }, h("slot", null)), this.renderFooter()), !thumbnailStart && this.renderThumbnail()));
  }
  selectCard() {
    this.selected = !this.selected;
    this.calciteCardSelect.emit();
  }
  renderThumbnail() {
    return getSlotted(this.el, SLOTS.thumbnail) ? (h("section", { class: CSS.thumbnailWrapper }, h("slot", { name: SLOTS.thumbnail }))) : null;
  }
  renderCheckbox() {
    const checkboxLabel = this.selected ? this.intlDeselect : this.intlSelect;
    return (h("calcite-label", { class: CSS.checkboxWrapper, onClick: this.cardSelectClick, onKeyDown: this.cardSelectKeyDown }, h("calcite-checkbox", { checked: this.selected, label: checkboxLabel })));
  }
  renderHeader() {
    const { el } = this;
    const title = getSlotted(el, SLOTS.title);
    const subtitle = getSlotted(el, SLOTS.subtitle);
    const hasHeader = title || subtitle;
    return hasHeader ? (h("header", { class: CSS.header }, h("slot", { name: SLOTS.title }), h("slot", { name: SLOTS.subtitle }))) : null;
  }
  renderFooter() {
    const { el } = this;
    const leadingFooter = getSlotted(el, SLOTS.footerLeading);
    const trailingFooter = getSlotted(el, SLOTS.footerTrailing);
    const hasFooter = leadingFooter || trailingFooter;
    return hasFooter ? (h("footer", { class: CSS.footer }, h("slot", { name: SLOTS.footerLeading }), h("slot", { name: SLOTS.footerTrailing }))) : null;
  }
  static get is() { return "calcite-card"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["card.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["card.css"]
    };
  }
  static get properties() {
    return {
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
          "text": "When `true`, the component is selected."
        },
        "attribute": "selected",
        "reflect": true,
        "defaultValue": "false"
      },
      "selectable": {
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
          "text": "When `true`, the component is selectable."
        },
        "attribute": "selectable",
        "reflect": true,
        "defaultValue": "false"
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
      "intlSelect": {
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
              "name": "default",
              "text": "\"Select\""
            }],
          "text": "When `selectable` is `true`, the accessible name for the component's checkbox for selection."
        },
        "attribute": "intl-select",
        "reflect": false,
        "defaultValue": "TEXT.select"
      },
      "intlDeselect": {
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
              "name": "default",
              "text": "\"Deselect\""
            }],
          "text": "When `selectable` is `true`, the accessible name for the component's checkbox for deselection."
        },
        "attribute": "intl-deselect",
        "reflect": false,
        "defaultValue": "TEXT.deselect"
      },
      "thumbnailPosition": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "LogicalFlowPosition",
          "resolved": "\"block-end\" | \"block-start\" | \"inline-end\" | \"inline-start\"",
          "references": {
            "LogicalFlowPosition": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Sets the placement of the thumbnail defined in the `thumbnail` slot."
        },
        "attribute": "thumbnail-position",
        "reflect": true,
        "defaultValue": "\"block-start\""
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteCardSelect",
        "name": "calciteCardSelect",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when `selectable` is `true` and the component is selected."
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
