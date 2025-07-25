/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { isPrimaryPointerButton } from "../../utils/dom";
import { TOOLTIP_DELAY_MS } from "./resources";
export default class TooltipManager {
  constructor() {
    // --------------------------------------------------------------------------
    //
    //  Private Properties
    //
    // --------------------------------------------------------------------------
    this.registeredElements = new WeakMap();
    this.hoverTimeouts = new WeakMap();
    this.registeredElementCount = 0;
    // --------------------------------------------------------------------------
    //
    //  Private Methods
    //
    // --------------------------------------------------------------------------
    this.queryTooltip = (composedPath) => {
      const { registeredElements } = this;
      const registeredElement = composedPath.find((pathEl) => registeredElements.has(pathEl));
      return registeredElements.get(registeredElement);
    };
    this.keyDownHandler = (event) => {
      if (event.key === "Escape") {
        const { activeTooltipEl } = this;
        if (activeTooltipEl) {
          this.clearHoverTimeout(activeTooltipEl);
          this.toggleTooltip(activeTooltipEl, false);
        }
      }
    };
    this.mouseEnterShow = (event) => {
      this.hoverEvent(event, true);
    };
    this.mouseLeaveHide = (event) => {
      this.hoverEvent(event, false);
    };
    this.clickHandler = (event) => {
      if (!isPrimaryPointerButton(event)) {
        return;
      }
      const clickedTooltip = this.queryTooltip(event.composedPath());
      this.clickedTooltip = clickedTooltip;
      if (clickedTooltip === null || clickedTooltip === void 0 ? void 0 : clickedTooltip.closeOnClick) {
        this.toggleTooltip(clickedTooltip, false);
        this.clearHoverTimeout(clickedTooltip);
      }
    };
    this.focusShow = (event) => {
      this.focusEvent(event, true);
    };
    this.blurHide = (event) => {
      this.focusEvent(event, false);
    };
    this.hoverToggle = (tooltip, value) => {
      const { hoverTimeouts } = this;
      hoverTimeouts.delete(tooltip);
      if (value) {
        this.closeExistingTooltip();
      }
      this.toggleTooltip(tooltip, value);
    };
  }
  // --------------------------------------------------------------------------
  //
  //  Public Methods
  //
  // --------------------------------------------------------------------------
  registerElement(referenceEl, tooltip) {
    this.registeredElementCount++;
    this.registeredElements.set(referenceEl, tooltip);
    if (this.registeredElementCount === 1) {
      this.addListeners();
    }
  }
  unregisterElement(referenceEl) {
    if (this.registeredElements.delete(referenceEl)) {
      this.registeredElementCount--;
    }
    if (this.registeredElementCount === 0) {
      this.removeListeners();
    }
  }
  addListeners() {
    document.addEventListener("keydown", this.keyDownHandler);
    document.addEventListener("pointerover", this.mouseEnterShow, { capture: true });
    document.addEventListener("pointerout", this.mouseLeaveHide, { capture: true });
    document.addEventListener("pointerdown", this.clickHandler, { capture: true });
    document.addEventListener("focusin", this.focusShow, { capture: true });
    document.addEventListener("focusout", this.blurHide, { capture: true });
  }
  removeListeners() {
    document.removeEventListener("keydown", this.keyDownHandler);
    document.removeEventListener("pointerover", this.mouseEnterShow, { capture: true });
    document.removeEventListener("pointerout", this.mouseLeaveHide, { capture: true });
    document.removeEventListener("pointerdown", this.clickHandler, { capture: true });
    document.removeEventListener("focusin", this.focusShow, { capture: true });
    document.removeEventListener("focusout", this.blurHide, { capture: true });
  }
  clearHoverTimeout(tooltip) {
    const { hoverTimeouts } = this;
    if (hoverTimeouts.has(tooltip)) {
      window.clearTimeout(hoverTimeouts.get(tooltip));
      hoverTimeouts.delete(tooltip);
    }
  }
  closeExistingTooltip() {
    const { activeTooltipEl } = this;
    if (activeTooltipEl) {
      this.toggleTooltip(activeTooltipEl, false);
    }
  }
  focusTooltip(tooltip, value) {
    this.closeExistingTooltip();
    if (value) {
      this.clearHoverTimeout(tooltip);
    }
    this.toggleTooltip(tooltip, value);
  }
  toggleTooltip(tooltip, value) {
    tooltip.open = value;
    if (value) {
      this.activeTooltipEl = tooltip;
    }
  }
  hoverTooltip(tooltip, value) {
    this.clearHoverTimeout(tooltip);
    const { hoverTimeouts } = this;
    const timeoutId = window.setTimeout(() => this.hoverToggle(tooltip, value), TOOLTIP_DELAY_MS || 0);
    hoverTimeouts.set(tooltip, timeoutId);
  }
  activeTooltipHover(event) {
    const { activeTooltipEl, hoverTimeouts } = this;
    const { type } = event;
    if (!activeTooltipEl) {
      return;
    }
    if (type === "pointerover" && event.composedPath().includes(activeTooltipEl)) {
      this.clearHoverTimeout(activeTooltipEl);
    }
    else if (type === "pointerout" && !hoverTimeouts.has(activeTooltipEl)) {
      this.hoverTooltip(activeTooltipEl, false);
    }
  }
  hoverEvent(event, value) {
    const tooltip = this.queryTooltip(event.composedPath());
    this.activeTooltipHover(event);
    if (!tooltip) {
      return;
    }
    this.hoverTooltip(tooltip, value);
  }
  focusEvent(event, value) {
    const tooltip = this.queryTooltip(event.composedPath());
    if (!tooltip || tooltip === this.clickedTooltip) {
      this.clickedTooltip = null;
      return;
    }
    this.focusTooltip(tooltip, value);
  }
}
