/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { initThemeChangeEvent } from "./theme";
/**
 * This file is imported in Stencil's `globalScript` config option.
 *
 * @see {@link https://stenciljs.com/docs/config#globalscript}
 */
export default function () {
  const isBrowser = typeof window !== "undefined" &&
    typeof location !== "undefined" &&
    typeof document !== "undefined" &&
    window.location === location &&
    window.document === document;
  if (isBrowser) {
    if (document.readyState === "interactive") {
      initThemeChangeEvent();
    }
    else {
      document.addEventListener("DOMContentLoaded", () => initThemeChangeEvent(), { once: true });
    }
  }
}
