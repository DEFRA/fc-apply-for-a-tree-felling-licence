/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from "@stencil/core";
import { isValidHex } from "../color-picker/utils";
import { hexToHue, stringToHex } from "./utils";
import { getThemeName } from "../../utils/dom";
export class Avatar {
  constructor() {
    //--------------------------------------------------------------------------
    //
    //  Properties
    //
    //--------------------------------------------------------------------------
    /** Specifies the size of the component. */
    this.scale = "m";
    //--------------------------------------------------------------------------
    //
    //  Private State/Props
    //
    //--------------------------------------------------------------------------
    this.thumbnailFailedToLoad = false;
  }
  //--------------------------------------------------------------------------
  //
  //  Lifecycle
  //
  //--------------------------------------------------------------------------
  render() {
    return this.determineContent();
  }
  //--------------------------------------------------------------------------
  //
  //  Private Methods
  //
  //--------------------------------------------------------------------------
  determineContent() {
    if (this.thumbnail && !this.thumbnailFailedToLoad) {
      return (h("img", { alt: "", class: "thumbnail", onError: () => (this.thumbnailFailedToLoad = true), src: this.thumbnail }));
    }
    const initials = this.generateInitials();
    const backgroundColor = this.generateFillColor();
    return (h("span", { class: "background", style: { backgroundColor } }, initials ? (h("span", { "aria-hidden": "true", class: "initials" }, initials)) : (h("calcite-icon", { class: "icon", icon: "user", scale: this.scale }))));
  }
  /**
   * Generate a valid background color that is consistent and unique to this user
   */
  generateFillColor() {
    const { userId, username, fullName, el } = this;
    const theme = getThemeName(el);
    const id = userId && `#${userId.substr(userId.length - 6)}`;
    const name = username || fullName || "";
    const hex = id && isValidHex(id) ? id : stringToHex(name);
    // if there is not unique information, or an invalid hex is produced, return a default
    if ((!userId && !name) || !isValidHex(hex)) {
      return `var(--calcite-ui-foreground-2)`;
    }
    const hue = hexToHue(hex);
    const l = theme === "dark" ? 20 : 90;
    return `hsl(${hue}, 60%, ${l}%)`;
  }
  /**
   * Use fullname or username to generate initials
   */
  generateInitials() {
    const { fullName, username } = this;
    if (fullName) {
      return fullName
        .trim()
        .split(" ")
        .map((name) => name.substring(0, 1))
        .join("");
    }
    else if (username) {
      return username.substring(0, 2);
    }
    return false;
  }
  static get is() { return "calcite-avatar"; }
  static get encapsulation() { return "shadow"; }
  static get originalStyleUrls() {
    return {
      "$": ["avatar.scss"]
    };
  }
  static get styleUrls() {
    return {
      "$": ["avatar.css"]
    };
  }
  static get properties() {
    return {
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
      "thumbnail": {
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
          "text": "Specifies the `src` to an image (remember to add a token if the user is private)."
        },
        "attribute": "thumbnail",
        "reflect": true
      },
      "fullName": {
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
          "text": "Specifies the full name of the user."
        },
        "attribute": "full-name",
        "reflect": true
      },
      "username": {
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
          "text": "Specifies the username of the user."
        },
        "attribute": "username",
        "reflect": true
      },
      "userId": {
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
          "text": "Specifies the unique id of the user."
        },
        "attribute": "user-id",
        "reflect": true
      }
    };
  }
  static get states() {
    return {
      "thumbnailFailedToLoad": {}
    };
  }
  static get elementRef() { return "el"; }
}
