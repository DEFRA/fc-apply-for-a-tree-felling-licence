/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
/**
 * This module helps users provide custom configuration for component internals.
 *
 * @internal
 */
const configOverrides = globalThis["calciteComponentsConfig"];
const config = {
  /**
   * We apply a custom fix to improve positioning for non-Chromium browsers.
   * The fix comes at a performance cost, so provides users a way to opt-out if necessary.
   *
   * @internal
   */
  floatingUINonChromiumPositioningFix: true,
  ...configOverrides
};
export { config };
