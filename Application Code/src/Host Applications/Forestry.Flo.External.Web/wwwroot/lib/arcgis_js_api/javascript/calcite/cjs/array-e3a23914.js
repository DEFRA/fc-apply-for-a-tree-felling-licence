/*!
 * All material copyright ESRI, All Rights Reserved, unless otherwise specified.
 * See https://github.com/Esri/calcite-components/blob/master/LICENSE.md for details.
 * v1.0.0-beta.98
 */
'use strict';

function getRoundRobinIndex(index, total) {
  return (index + total) % total;
}

exports.getRoundRobinIndex = getRoundRobinIndex;
