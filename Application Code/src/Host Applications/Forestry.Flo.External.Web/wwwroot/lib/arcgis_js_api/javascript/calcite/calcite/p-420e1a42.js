/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
function t(t,s,n){return new(function(t){class s extends window.MutationObserver{constructor(t){super(t),this.observedEntry=[],this.callback=t}observe(t,s){return this.observedEntry.push({target:t,options:s}),super.observe(t,s)}unobserve(t){const s=this.observedEntry.filter((s=>s.target!==t));this.observedEntry=[],this.callback(super.takeRecords(),this),this.disconnect(),s.forEach((t=>this.observe(t.target,t.options)))}}return"intersection"===t?window.IntersectionObserver:"mutation"===t?s:window.ResizeObserver}(t))(s,n)}export{t as c}