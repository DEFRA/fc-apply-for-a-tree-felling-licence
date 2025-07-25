import { DataSeries, Graph, TranslateOptions, Translator, Extent } from "../graph/interfaces";
/**
 * Generate a function which will translate a point
 * from the data coordinate space to svg viewbox oriented pixels
 *
 * @param root0
 * @param root0.width
 * @param root0.height
 * @param root0.min
 * @param root0.max
 */
export declare function translate({ width, height, min, max }: TranslateOptions): Translator;
/**
 * Get the min and max values from the dataset
 *
 * @param data
 */
export declare function range(data: DataSeries): Extent;
/**
 * Generate drawing commands for an area graph
 * returns a string can can be passed directly to a path element's `d` attribute
 *
 * @param root0
 * @param root0.data
 * @param root0.min
 * @param root0.max
 * @param root0.t
 */
export declare function area({ data, min, max, t }: Graph): string;
