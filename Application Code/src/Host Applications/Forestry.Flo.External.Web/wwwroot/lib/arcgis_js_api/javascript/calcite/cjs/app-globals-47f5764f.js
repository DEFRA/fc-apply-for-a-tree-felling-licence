/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

const resources = require('./resources-3fd6da1b.js');

/**
 * Emits when the theme is dynamically toggled between light and dark on <body> or in OS preferences.
 */
function initThemeChangeEvent() {
  const { classList } = document.body;
  const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
  const getTheme = () => classList.contains(resources.darkTheme) || (classList.contains(resources.autoTheme) && prefersDark) ? "dark" : "light";
  const emitThemeChange = (theme) => document.body.dispatchEvent(new CustomEvent("calciteThemeChange", { bubbles: true, detail: { theme } }));
  const themeChangeHandler = (newTheme) => {
    currentTheme !== newTheme && emitThemeChange(newTheme);
    currentTheme = newTheme;
  };
  let currentTheme = getTheme();
  // emits event on page load
  emitThemeChange(currentTheme);
  // emits event when changing OS theme preferences
  window
    .matchMedia("(prefers-color-scheme: dark)")
    .addEventListener("change", (event) => themeChangeHandler(event.matches ? "dark" : "light"));
  // emits event when toggling between theme classes on <body>
  new MutationObserver(() => themeChangeHandler(getTheme())).observe(document.body, {
    attributes: true,
    attributeFilter: ["class"]
  });
}

/**
 * This file is imported in Stencil's `globalScript` config option.
 *
 * @see {@link https://stenciljs.com/docs/config#globalscript}
 */
function appGlobalScript () {
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

const globalScripts = appGlobalScript;

exports.globalScripts = globalScripts;
