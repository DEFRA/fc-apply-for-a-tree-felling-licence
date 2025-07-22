/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
import{f as t}from"./p-452677fb.js";import{c as o}from"./p-420e1a42.js";const n=new Set;let s;const c={childList:!0};function f(t){s||(s=o("mutation",a)),s.observe(t.el,c)}function i(t){n.delete(t.el),a(s.takeRecords()),s.disconnect();for(const[t]of n.entries())s.observe(t,c)}function a(o){o.forEach((({target:o})=>{t(o)}))}export{f as c,i as d}