/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import "form-request-submit-polyfill/form-request-submit-polyfill";
import { h, Build } from "@stencil/core";
import { CSS, TEXT } from "./resources";
import { closestElementCrossShadowBoundary } from "../../utils/dom";
import { connectLabel, disconnectLabel, getLabelText } from "../../utils/label";
import { createObserver } from "../../utils/observers";
import { updateHostInteraction } from "../../utils/interactive";
import { submitForm, resetForm } from "../../utils/form";
/** Passing a 'href' will render an anchor link, instead of a button. Role will be set to link, or button, depending on this. */
/** It is the consumers responsibility to add aria information, rel, target, for links, and any button attributes for form submission */
/** @slot - A slot for adding text. */
export class Button {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the alignment of the component's elements. */
    this.alignment = "center";
    /** Specifies the appearance style of the component. */
    this.appearance = "solid";
    /** Specifies the color of the component. */
    this.color = "blue";
    /**  When `true`, interaction is prevented and the component is displayed with lower opacity. */
    this.disabled = false;
    /**
     * Accessible name when the component is loading.
     *
     * @default "Loading"
     */
    this.intlLoading = TEXT.loading;
    /**
     * When `true`, a busy indicator is displayed and interaction is disabled.
     */
    this.loading = false;
    /** When `true`, adds a round style to the component. */
    this.round = false;
    /** Specifies the size of the component. */
    this.scale = "m";
    /** Specifies if the component is a child of a `calcite-split-button`. */
    this.splitChild = false;
    /**
     * Specifies the default behavior of the button.
     *
     * @mdn [type](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/button#attr-type)
     */
    this.type = "button";
    /** Specifies the width of the component. */
    this.width = "auto";
    /** watches for changing text content */
    this.mutationObserver = createObserver("mutation", () => this.updateHasContent());
    /** determine if there is slotted content for styling purposes */
    this.hasContent = false;
    /** determine if loader present for styling purposes */
    this.hasLoader = false;
    // act on a requested or nearby form based on type
    this.handleClick = () => {
      const { type } = this;
      if (this.href) {
        return;
      }
      // this.type refers to type attribute, not child element type
      if (type === "submit") {
        submitForm(this);
      }
      else if (type === "reset") {
        resetForm(this);
      }
    };
  }
  loadingChanged(newValue, oldValue) {
    if (!!newValue && !oldValue) {
      this.hasLoader = true;
    }
    if (!newValue && !!oldValue) {
      window.setTimeout(() => {
        this.hasLoader = false;
      }, 300);
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    this.hasLoader = this.loading;
    this.setupTextContentObserver();
    connectLabel(this);
    this.formEl = closestElementCrossShadowBoundary(this.el, this.form ? `#${this.form}` : "form");
  }
  disconnectedCallback() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.disconnect();
    disconnectLabel(this);
    this.formEl = null;
  }
  componentWillLoad() {
    if (Build.isBrowser) {
      this.updateHasContent();
    }
  }
  componentDidRender() {
    updateHostInteraction(this);
  }
  render() {
    const childElType = this.href ? "a" : "button";
    const Tag = childElType;
    const loaderNode = this.hasLoader ? (h("div", { class: CSS.buttonLoader }, h("calcite-loader", { active: true, class: this.loading ? CSS.loadingIn : CSS.loadingOut, inline: true, label: this.intlLoading, scale: this.scale === "l" ? "m" : "s" }))) : null;
    const iconStartEl = (h("calcite-icon", { class: { [CSS.icon]: true, [CSS.iconStart]: true }, flipRtl: this.iconFlipRtl === "start" || this.iconFlipRtl === "both", icon: this.iconStart, scale: this.scale === "l" ? "m" : "s" }));
    const iconEndEl = (h("calcite-icon", { class: { [CSS.icon]: true, [CSS.iconEnd]: true }, flipRtl: this.iconFlipRtl === "end" || this.iconFlipRtl === "both", icon: this.iconEnd, scale: this.scale === "l" ? "m" : "s" }));
    const contentEl = (h("span", { class: CSS.content }, h("slot", null)));
    return (h(Tag, { "aria-label": getLabelText(this), class: {
        [CSS.contentSlotted]: this.hasContent,
        [CSS.iconStartEmpty]: !this.iconStart,
        [CSS.iconEndEmpty]: !this.iconEnd
      }, disabled: this.disabled || this.loading, href: childElType === "a" && this.href, name: childElType === "button" && this.name, onClick: this.handleClick, ref: (el) => (this.childEl = el), rel: childElType === "a" && this.rel, tabIndex: this.disabled || this.loading ? -1 : null, target: childElType === "a" && this.target, type: childElType === "button" && this.type }, loaderNode, this.iconStart ? iconStartEl : null, this.hasContent ? contentEl : null, this.iconEnd ? iconEndEl : null));
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    var _a;
    (_a = this.childEl) === null || _a === void 0 ? void 0 : _a.focus();
  }
  updateHasContent() {
    var _a, _b;
    const slottedContent = this.el.textContent.trim().length > 0 || this.el.childNodes.length > 0;
    this.hasContent =
      this.el.childNodes.length === 1 && ((_a = this.el.childNodes[0]) === null || _a === void 0 ? void 0 : _a.nodeName) === "#text"
        ? ((_b = this.el.textContent) === null || _b === void 0 ? void 0 : _b.trim().length) > 0
        : slottedContent;
  }
  setupTextContentObserver() {
    var _a;
    (_a = this.mutationObserver) === null || _a === void 0 ? void 0 : _a.observe(this.el, { childList: true, subtree: true });
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  onLabelClick() {
    this.handleClick();
    this.setFocus();
  }
  static get is() { return "calcite-button"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["button.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["button.css"]
    };
  }
  static get properties() {
    return {
      "alignment": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ButtonAlignment",
          "resolved": "\"center\" | \"end\" | \"icon-end-space-between\" | \"icon-start-space-between\" | \"space-between\" | \"start\"",
          "references": {
            "ButtonAlignment": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies the alignment of the component's elements."
        },
        "attribute": "alignment",
        "reflect": true,
        "defaultValue": "\"center\""
      },
      "appearance": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ButtonAppearance",
          "resolved": "\"clear\" | \"minimal\" | \"outline\" | \"solid\" | \"transparent\"",
          "references": {
            "ButtonAppearance": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the appearance style of the component."
        },
        "attribute": "appearance",
        "reflect": true,
        "defaultValue": "\"solid\""
      },
      "label": {
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
          "text": "Accessible name for the component."
        },
        "attribute": "label",
        "reflect": false
      },
      "color": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "ButtonColor",
          "resolved": "\"blue\" | \"inverse\" | \"neutral\" | \"red\"",
          "references": {
            "ButtonColor": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the color of the component."
        },
        "attribute": "color",
        "reflect": true,
        "defaultValue": "\"blue\""
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
      "href": {
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
          "text": "Specifies the URL of the linked resource, which can be set as an absolute or relative path."
        },
        "attribute": "href",
        "reflect": true
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
          "text": "When `true`, a busy indicator is displayed and interaction is disabled."
        },
        "attribute": "loading",
        "reflect": true,
        "defaultValue": "false"
      },
      "name": {
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
          "text": "Specifies the name of the component on form submission."
        },
        "attribute": "name",
        "reflect": true
      },
      "rel": {
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
              "name": "mdn",
              "text": "[rel](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/rel)"
            }],
          "text": "Defines the relationship between the `href` value and the current document."
        },
        "attribute": "rel",
        "reflect": true
      },
      "form": {
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
              "name": "deprecated",
              "text": "\u2013 The property is no longer needed if the component is placed inside a form."
            }],
          "text": "The form ID to associate with the component."
        },
        "attribute": "form",
        "reflect": false
      },
      "round": {
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
          "text": "When `true`, adds a round style to the component."
        },
        "attribute": "round",
        "reflect": true,
        "defaultValue": "false"
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
      "splitChild": {
        "type": "any",
        "mutable": false,
        "complexType": {
          "original": "\"primary\" | \"secondary\" | false",
          "resolved": "\"primary\" | \"secondary\" | boolean",
          "references": {}
        },
        "required": false,
        "optional": true,
        "docs": {
          "tags": [],
          "text": "Specifies if the component is a child of a `calcite-split-button`."
        },
        "attribute": "split-child",
        "reflect": true,
        "defaultValue": "false"
      },
      "target": {
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
              "name": "mdn",
              "text": "[target](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/a#attr-target)"
            }],
          "text": "Specifies where to open the linked document defined in the `href` property."
        },
        "attribute": "target",
        "reflect": true
      },
      "type": {
        "type": "string",
        "mutable": true,
        "complexType": {
          "original": "string",
          "resolved": "string",
          "references": {}
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [{
              "name": "mdn",
              "text": "[type](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/button#attr-type)"
            }],
          "text": "Specifies the default behavior of the button."
        },
        "attribute": "type",
        "reflect": true,
        "defaultValue": "\"button\""
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
  static get states() {
    return {
      "hasContent": {},
      "hasLoader": {}
    };
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
        "propName": "loading",
        "methodName": "loadingChanged"
      }];
  }
}
