/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { CSS, SLOTS, TEXT } from "./resources";
import { StatusIcons } from "../alert/interfaces";
import { getSlotted, setRequestedIcon } from "../../utils/dom";
import { connectConditionalSlotComponent, disconnectConditionalSlotComponent } from "../../utils/conditionalSlot";
/**
 * Notices are intended to be used to present users with important-but-not-crucial contextual tips or copy. Because
 * notices are displayed inline, a common use case is displaying them on page-load to present users with short hints or contextual copy.
 * They are optionally dismissible - useful for keeping track of whether or not a user has dismissed the notice. You can also choose not
 * to display a notice on page load and set the "active" attribute as needed to contextually provide inline messaging to users.
 */
/**
 * @slot title - A slot for adding the title.
 * @slot message - A slot for adding the message.
 * @slot link - A slot for adding actions to take, such as: undo, try again, link to page, etc.
 * @slot actions-end - A slot for adding actions to the end of the component. It is recommended to use two or less actions.
 */
export class Notice {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //---------------------------------------------------------------------------
    /**
     * When `true`, the component is active.
     *
     * @deprecated Use `open` instead.
     */
    this.active = false;
    /** When `true`, the component is visible. */
    this.open = false;
    /** The color for the component's top border and icon. */
    this.color = "blue";
    /**
     * When `true`, a close button is added to the component.
     *
     * @deprecated use `closable` instead.
     */
    this.dismissible = false;
    /** When `true`, a close button is added to the component. */
    this.closable = false;
    /**
     * Accessible name for the close button.
     *
     * @default "Close"
     */
    this.intlClose = TEXT.close;
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Specifies the width of the component. */
    this.width = "auto";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.close = () => {
      this.open = false;
      this.calciteNoticeClose.emit();
    };
  }
  activeHandler(value) {
    this.open = value;
  }
  openHandler(value) {
    this.active = value;
  }
  handleDismissible(value) {
    this.closable = value;
  }
  handleClosable(value) {
    this.dismissible = value;
  }
  updateRequestedIcon() {
    this.requestedIcon = setRequestedIcon(StatusIcons, this.icon, this.color);
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    connectConditionalSlotComponent(this);
    const isOpen = this.active || this.open;
    if (isOpen) {
      this.activeHandler(isOpen);
      this.openHandler(isOpen);
    }
    if (this.dismissible) {
      this.handleDismissible(this.dismissible);
    }
    if (this.closable) {
      this.handleClosable(this.closable);
    }
  }
  disconnectedCallback() {
    disconnectConditionalSlotComponent(this);
  }
  componentWillLoad() {
    this.requestedIcon = setRequestedIcon(StatusIcons, this.icon, this.color);
  }
  render() {
    const { el } = this;
    const closeButton = (h("button", { "aria-label": this.intlClose, class: CSS.close, onClick: this.close, ref: (el) => (this.closeButton = el) }, h("calcite-icon", { icon: "x", scale: this.scale === "l" ? "m" : "s" })));
    const hasActionEnd = getSlotted(el, SLOTS.actionsEnd);
    return (h("div", { class: CSS.container }, this.requestedIcon ? (h("div", { class: CSS.icon }, h("calcite-icon", { icon: this.requestedIcon, scale: this.scale === "l" ? "m" : "s" }))) : null, h("div", { class: CSS.content }, h("slot", { name: SLOTS.title }), h("slot", { name: SLOTS.message }), h("slot", { name: SLOTS.link })), hasActionEnd ? (h("div", { class: CSS.actionsEnd }, h("slot", { name: SLOTS.actionsEnd }))) : null, this.closable ? closeButton : null));
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    const noticeLinkEl = this.el.querySelector("calcite-link");
    if (!this.closeButton && !noticeLinkEl) {
      return;
    }
    if (noticeLinkEl) {
      noticeLinkEl.setFocus();
    }
    else if (this.closeButton) {
      this.closeButton.focus();
    }
  }
  static get is() { return "calcite-notice"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["notice.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["notice.css"]
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
              "text": "Use `open` instead."
            }],
          "text": "When `true`, the component is active."
        },
        "attribute": "active",
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
          "text": "When `true`, the component is visible."
        },
        "attribute": "open",
        "reflect": true,
        "defaultValue": "false"
      },
      "color": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "StatusColor",
          "resolved": "\"blue\" | \"green\" | \"red\" | \"yellow\"",
          "references": {
            "StatusColor": {
              "location": "import",
              "path": "../alert/interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "The color for the component's top border and icon."
        },
        "attribute": "color",
        "reflect": true,
        "defaultValue": "\"blue\""
      },
      "dismissible": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [{
              "name": "deprecated",
              "text": "use `closable` instead."
            }],
          "text": "When `true`, a close button is added to the component."
        },
        "attribute": "dismissible",
        "reflect": true,
        "defaultValue": "false"
      },
      "closable": {
        "type": "boolean",
        "mutable": false,
        "complexType": {
          "original": "boolean",
          "resolved": "boolean",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "When `true`, a close button is added to the component."
        },
        "attribute": "closable",
        "reflect": true,
        "defaultValue": "false"
      },
      "icon": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "string | boolean",
          "resolved": "boolean | string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "When `true`, shows a default recommended icon. Alternatively, pass a Calcite UI Icon name to display a specific icon."
        },
        "attribute": "icon",
        "reflect": true
      },
      "intlClose": {
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
              "text": "\"Close\""
            }],
          "text": "Accessible name for the close button."
        },
        "attribute": "intl-close",
        "reflect": false,
        "defaultValue": "TEXT.close"
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
      "width": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "Width",
          "resolved": "\"auto\" | \"full\" | \"half\"",
          "references": {
            "Width": {
              "location": "import",
              "path": "../interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the width of the component."
        },
        "attribute": "width",
        "reflect": true,
        "defaultValue": "\"auto\""
      }
    };
  }
  static get events() {
    return [{
        "method": "calciteNoticeClose",
        "name": "calciteNoticeClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fired when the component is closed."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteNoticeOpen",
        "name": "calciteNoticeOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fired when the component is opened."
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
      "setFocus": {
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
          "text": "Sets focus on the component.",
          "tags": []
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
        "propName": "open",
        "methodName": "openHandler"
      }, {
        "propName": "dismissible",
        "methodName": "handleDismissible"
      }, {
        "propName": "closable",
        "methodName": "handleClosable"
      }, {
        "propName": "icon",
        "methodName": "updateRequestedIcon"
      }, {
        "propName": "color",
        "methodName": "updateRequestedIcon"
      }];
  }
}
