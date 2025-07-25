/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h, Host } from "@stencil/core";
import { getSlotted, setRequestedIcon, toAriaBoolean } from "../../utils/dom";
import { DURATIONS, SLOTS, TEXT } from "./resources";
import { StatusIcons } from "./interfaces";
import { connectOpenCloseComponent, disconnectOpenCloseComponent } from "../../utils/openCloseComponent";
import { connectLocalized, disconnectLocalized, numberStringFormatter } from "../../utils/locale";
/**
 * Alerts are meant to provide a way to communicate urgent or important information to users, frequently as a result of an action they took in your app. Alerts are positioned
 * at the bottom of the page. Multiple opened alerts will be added to a queue, allowing users to dismiss them in the order they are provided.
 */
/**
 * @slot title - A slot for optionally adding a title to the component.
 * @slot message - A slot for adding main text to the component.
 * @slot link - A slot for optionally adding an action to take from the alert (undo, try again, link to page, etc.)
 */
export class Alert {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //---------------------------------------------------------------------------
    /**
     * When `true`, displays and positions the component.
     *
     * @deprecated use `open` instead.
     */
    this.active = false;
    /** When `true`, displays and positions the component. */
    this.open = false;
    /** When `true`, the component closes automatically (recommended for passive, non-blocking alerts). */
    this.autoDismiss = false;
    /** Specifies the duration before the component automatically closes (only use with `autoDismiss`). */
    this.autoDismissDuration = this.autoDismiss ? "medium" : null;
    /** Specifies the color for the component (will apply to top border and icon). */
    this.color = "blue";
    /**
     * Specifies the text label for the close button.
     *
     * @default "Close"
     */
    this.intlClose = TEXT.intlClose;
    /** Specifies the placement of the component */
    this.placement = "bottom";
    /** Specifies the size of the component. */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    this.effectiveLocale = "";
    /** the list of queued alerts */
    this.queue = [];
    /** the count of queued alerts */
    this.queueLength = 0;
    /** is the alert queued */
    this.queued = false;
    this.autoDismissTimeoutId = null;
    this.trackTimer = Date.now();
    this.openTransitionProp = "opacity";
    //--------------------------------------------------------------------------
    //
    //  Private Methods
    //
    //--------------------------------------------------------------------------
    this.setTransitionEl = (el) => {
      this.transitionEl = el;
      connectOpenCloseComponent(this);
    };
    /** close and emit calciteInternalAlertSync event with the updated queue payload */
    this.closeAlert = () => {
      this.autoDismissTimeoutId = null;
      this.queued = false;
      this.open = false;
      this.queue = this.queue.filter((el) => el !== this.el);
      this.determineActiveAlert();
      this.calciteInternalAlertSync.emit({ queue: this.queue });
    };
  }
  activeHandler(value) {
    this.open = value;
  }
  openHandler(value) {
    if (this.open && !this.queued) {
      this.calciteInternalAlertRegister.emit();
      this.active = value;
    }
    if (!this.open) {
      this.queue = this.queue.filter((el) => el !== this.el);
      this.calciteInternalAlertSync.emit({ queue: this.queue });
      this.active = false;
    }
  }
  updateRequestedIcon() {
    this.requestedIcon = setRequestedIcon(StatusIcons, this.icon, this.color);
  }
  updateDuration() {
    if (this.autoDismiss && this.autoDismissTimeoutId) {
      window.clearTimeout(this.autoDismissTimeoutId);
      this.autoDismissTimeoutId = window.setTimeout(() => this.closeAlert(), DURATIONS[this.autoDismissDuration] - (Date.now() - this.trackTimer));
    }
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  connectedCallback() {
    connectLocalized(this);
    const open = this.open || this.active;
    if (open && !this.queued) {
      this.activeHandler(open);
      this.openHandler(open);
      this.calciteInternalAlertRegister.emit();
    }
    connectOpenCloseComponent(this);
  }
  componentWillLoad() {
    this.requestedIcon = setRequestedIcon(StatusIcons, this.icon, this.color);
  }
  disconnectedCallback() {
    window.clearTimeout(this.autoDismissTimeoutId);
    disconnectOpenCloseComponent(this);
    disconnectLocalized(this);
  }
  render() {
    const closeButton = (h("button", { "aria-label": this.intlClose, class: "alert-close", onClick: this.closeAlert, ref: (el) => (this.closeButton = el), type: "button" }, h("calcite-icon", { icon: "x", scale: this.scale === "l" ? "m" : "s" })));
    numberStringFormatter.numberFormatOptions = {
      locale: this.effectiveLocale,
      numberingSystem: this.numberingSystem,
      signDisplay: "always"
    };
    const queueNumber = this.queueLength > 2 ? this.queueLength - 1 : 1;
    const queueText = numberStringFormatter.numberFormatter.format(queueNumber);
    const queueCount = (h("div", { class: `${this.queueLength > 1 ? "active " : ""}alert-queue-count` }, h("calcite-chip", { scale: this.scale, value: queueText }, queueText)));
    const { active, autoDismiss, label, placement, queued, requestedIcon } = this;
    const role = autoDismiss ? "alert" : "alertdialog";
    const hidden = !active;
    return (h(Host, { "aria-hidden": toAriaBoolean(hidden), "aria-label": label, "calcite-hydrated-hidden": hidden, role: role }, h("div", { class: {
        container: true,
        queued,
        [placement]: true
      }, ref: this.setTransitionEl }, requestedIcon ? (h("div", { class: "alert-icon" }, h("calcite-icon", { icon: requestedIcon, scale: this.scale === "l" ? "m" : "s" }))) : null, h("div", { class: "alert-content" }, h("slot", { name: SLOTS.title }), h("slot", { name: SLOTS.message }), h("slot", { name: SLOTS.link })), queueCount, !autoDismiss ? closeButton : null, active && !queued && autoDismiss ? h("div", { class: "alert-dismiss-progress" }) : null)));
  }
  // when an alert is opened or closed, update queue and determine active alert
  alertSync(event) {
    if (this.queue !== event.detail.queue) {
      this.queue = event.detail.queue;
    }
    this.queueLength = this.queue.length;
    this.determineActiveAlert();
    event.stopPropagation();
  }
  // when an alert is first registered, trigger a queue sync
  alertRegister() {
    if (this.open && !this.queue.includes(this.el)) {
      this.queued = true;
      this.queue.push(this.el);
    }
    this.calciteInternalAlertSync.emit({ queue: this.queue });
    this.determineActiveAlert();
  }
  //--------------------------------------------------------------------------
  //
  //  Public Methods
  //
  //--------------------------------------------------------------------------
  /** Sets focus on the component. */
  async setFocus() {
    const alertLinkEl = getSlotted(this.el, { selector: "calcite-link" });
    if (!this.closeButton && !alertLinkEl) {
      return;
    }
    else if (alertLinkEl) {
      alertLinkEl.setFocus();
    }
    else if (this.closeButton) {
      this.closeButton.focus();
    }
  }
  /** determine which alert is active */
  determineActiveAlert() {
    var _a;
    if (((_a = this.queue) === null || _a === void 0 ? void 0 : _a[0]) === this.el) {
      this.openAlert();
      if (this.autoDismiss && !this.autoDismissTimeoutId) {
        this.trackTimer = Date.now();
        this.autoDismissTimeoutId = window.setTimeout(() => this.closeAlert(), DURATIONS[this.autoDismissDuration]);
      }
    }
    else {
      return;
    }
  }
  onBeforeOpen() {
    this.calciteAlertBeforeOpen.emit();
  }
  onOpen() {
    this.calciteAlertOpen.emit();
  }
  onBeforeClose() {
    this.calciteAlertBeforeClose.emit();
  }
  onClose() {
    this.calciteAlertClose.emit();
  }
  /** remove queued class after animation completes */
  openAlert() {
    window.clearTimeout(this.queueTimeout);
    this.queueTimeout = window.setTimeout(() => (this.queued = false), 300);
  }
  static get is() { return "calcite-alert"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["alert.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["alert.css"]
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
              "text": "use `open` instead."
            }],
          "text": "When `true`, displays and positions the component."
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
          "text": "When `true`, displays and positions the component."
        },
        "attribute": "open",
        "reflect": true,
        "defaultValue": "false"
      },
      "autoDismiss": {
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
          "text": "When `true`, the component closes automatically (recommended for passive, non-blocking alerts)."
        },
        "attribute": "auto-dismiss",
        "reflect": true,
        "defaultValue": "false"
      },
      "autoDismissDuration": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "AlertDuration",
          "resolved": "\"fast\" | \"medium\" | \"slow\"",
          "references": {
            "AlertDuration": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the duration before the component automatically closes (only use with `autoDismiss`)."
        },
        "attribute": "auto-dismiss-duration",
        "reflect": true,
        "defaultValue": "this.autoDismiss ? \"medium\" : null"
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
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the color for the component (will apply to top border and icon)."
        },
        "attribute": "color",
        "reflect": true,
        "defaultValue": "\"blue\""
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
          "text": "When `true`, shows a default recommended icon. Alternatively,\npass a Calcite UI Icon name to display a specific icon."
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
          "text": "Specifies the text label for the close button."
        },
        "attribute": "intl-close",
        "reflect": false,
        "defaultValue": "TEXT.intlClose"
      },
      "label": {
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
          "text": "Specifies an accessible name for the component."
        },
        "attribute": "label",
        "reflect": false
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
      "placement": {
        "type": "string",
        "mutable": false,
        "complexType": {
          "original": "AlertPlacement",
          "resolved": "\"bottom\" | \"bottom-end\" | \"bottom-start\" | \"top\" | \"top-end\" | \"top-start\"",
          "references": {
            "AlertPlacement": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        },
        "required": false,
        "optional": false,
        "docs": {
          "tags": [],
          "text": "Specifies the placement of the component"
        },
        "attribute": "placement",
        "reflect": true,
        "defaultValue": "\"bottom\""
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
  static get states() {
    return {
      "effectiveLocale": {},
      "queue": {},
      "queueLength": {},
      "queued": {},
      "requestedIcon": {}
    };
  }
  static get events() {
    return [{
        "method": "calciteAlertBeforeClose",
        "name": "calciteAlertBeforeClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is requested to be closed and before the closing transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteAlertClose",
        "name": "calciteAlertClose",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is closed and animation is complete."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteAlertBeforeOpen",
        "name": "calciteAlertBeforeOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is added to the DOM but not rendered, and before the opening transition begins."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteAlertOpen",
        "name": "calciteAlertOpen",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [],
          "text": "Fires when the component is open and animation is complete."
        },
        "complexType": {
          "original": "void",
          "resolved": "void",
          "references": {}
        }
      }, {
        "method": "calciteInternalAlertSync",
        "name": "calciteInternalAlertSync",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Fires to sync queue when opened or closed."
        },
        "complexType": {
          "original": "Sync",
          "resolved": "Sync",
          "references": {
            "Sync": {
              "location": "import",
              "path": "./interfaces"
            }
          }
        }
      }, {
        "method": "calciteInternalAlertRegister",
        "name": "calciteInternalAlertRegister",
        "bubbles": true,
        "cancelable": false,
        "composed": true,
        "docs": {
          "tags": [{
              "name": "internal",
              "text": undefined
            }],
          "text": "Fires when the component is added to DOM - used to receive initial queue."
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
            },
            "HTMLCalciteLinkElement": {
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
        "propName": "icon",
        "methodName": "updateRequestedIcon"
      }, {
        "propName": "color",
        "methodName": "updateRequestedIcon"
      }, {
        "propName": "autoDismissDuration",
        "methodName": "updateDuration"
      }];
  }
  static get listeners() {
    return [{
        "name": "calciteInternalAlertSync",
        "method": "alertSync",
        "target": "window",
        "capture": false,
        "passive": false
      }, {
        "name": "calciteInternalAlertRegister",
        "method": "alertRegister",
        "target": "window",
        "capture": false,
        "passive": false
      }];
  }
}
