/**
 * ValidateShape.js
 * 
 * Provides validation logic for map shapes (graphics) in the Forestry Flo application.
 * Used to check geometry properties, orientation, and spatial relationships for polygons.
 * 
 * Main responsibilities:
 * - Determine if a polygon's ring is oriented clockwise.
 * - Filter out non-relevant graphics (e.g., text symbols).
 * - Validate polygons for self-intersection, out-of-bounds, and overlapping features.
 * - Return appropriate result codes from CheckResults.js.
 * 
 * Key methods:
 * - isClockwise(points): Returns true if the ring of points is clockwise.
 * - GetGranpic(graphics): Returns the first non-text graphic from the input.
 * - Excute(graphics, otherGraphics): Runs all validation checks and returns a result.
 * 
 * Note: The code assumes rings are arrays of points. If geometry data structure varies (e.g., nested arrays for cut shapes), additional normalization may be required.
 */
define(["/js/mapping/CheckResults.js"], function (ResultType) {
    var ValidateShape = function (geometryEngine, defaultMapExtentForEngland) {
        this._geometryEngine = geometryEngine;
        this._defaultMapExtentForEngland = defaultMapExtentForEngland;
    };

    /*
     * Checks if a ring of points is oriented clockwise.
     * @param {Array} points - Array of [x, y] coordinates.
     * @returns {boolean} True if clockwise, false otherwise.
     */
    ValidateShape.prototype.isClockwise = function (points) {
        let sum = 0;
        for (let i = 0, len = points.length; i < len; i++) {
            const [x1, y1] = points[i];
            const [x2, y2] = points[(i + 1) % len];
            sum += (x2 - x1) * (y2 + y1);
        }
        return sum > 0;
    }

    /*
     * Returns the first non-text graphic from the input array.
     * If input is not an array, returns it directly.
     * @param {Array|Object} graphics - Array of graphics or a single graphic.
     * @returns {Object|null} The first non-text graphic or null if none found.
    */
    ValidateShape.prototype.GetGraphic = function (graphics) {

        if (!Array.isArray(graphics)) {
            return graphics;
        }

        var filtered = graphics.filter(function (g) {
            return !g.symbol || !g.symbol.type || g.symbol.type !== "text";
        });

        if (filtered.length === 0) {
            return null;
        }

        return filtered[0];
    }

    /*
     * Validates the geometry of a graphic and its relationship to other graphics.
     * Checks for polygon type, self-intersection, out-of-bounds, ring orientation, and overlapping features.
     * Returns a result code from ResultType.
     * @param {Array|Object} graphics - Graphics to validate.
     * @param {Array} otherGraphics - Other graphics to check for overlaps.
     * @returns {string} Validation result code.
     */
    ValidateShape.prototype.Execute = function (graphics, otherGraphics, simplifyOperator) {
        const g = this.GetGraphic(graphics);

        if (!g) {
            return ResultType.Notchecked;
        }

        // Legacy check! 
        if (g.geometry.type !== "polygon") {
            return ResultType.Passed;
        }
        const simplified = simplifyOperator.execute(g.geometry);
        if (!simplifyOperator.isSimple(simplified)) {
            return ResultType.IsSelfIntersecting;
        }

        if (!this._geometryEngine.contains(this._defaultMapExtentForEngland, g.geometry)) {
            return ResultType.OutOfBounds;
        }

        if (Array.isArray(g.geometry.rings)) {
            let clockwiseCount = 0;
            for (const ring of g.geometry.rings) {

                if (this.isClockwise(ring)) {
                    clockwiseCount++;
                }

            }
            if (clockwiseCount > 1) {
                return ResultType.TooManyRings;
            }
        }

        if (otherGraphics === undefined || otherGraphics.length < 1) {
            return ResultType.Passed;
        }

        for (const otherGraphic of otherGraphics) {
            if (otherGraphic.geometry.type !== "polygon") {
                continue;
            }

            const overlaps = this._geometryEngine.overlaps(otherGraphic.geometry, g.geometry);
            const contained = this._geometryEngine.contains(otherGraphic.geometry, g.geometry);
            const within = this._geometryEngine.within(otherGraphic.geometry, g.geometry);
            if (overlaps || contained || within) {
                return ResultType.OverlappingFeatures;
            }
        }


        return ResultType.Passed;
    };

    return ValidateShape;
});