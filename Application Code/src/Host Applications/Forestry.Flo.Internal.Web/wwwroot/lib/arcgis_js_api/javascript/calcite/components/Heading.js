/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import { h } from '@stencil/core/internal/client/index.js';

function constrainHeadingLevel(level) {
  return Math.min(Math.max(Math.ceil(level), 1), 6);
}
const Heading = (props, children) => {
  const HeadingTag = `h${props.level}`;
  delete props.level;
  return h(HeadingTag, { ...props }, children);
};

export { Heading as H, constrainHeadingLevel as c };
