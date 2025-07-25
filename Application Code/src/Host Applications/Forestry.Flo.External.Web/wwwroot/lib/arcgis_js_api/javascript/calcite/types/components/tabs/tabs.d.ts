import { VNode } from "../../stencil-public-runtime";
import { TabLayout, TabPosition } from "./interfaces";
import { Scale } from "../interfaces";
/**
 * @slot - A slot for adding `calcite-tab`s.
 * @slot tab-nav - A slot for adding a `calcite-tab-nav`.
 */
export declare class Tabs {
  el: HTMLCalciteTabsElement;
  /**
   * Specifies the layout of the `calcite-tab-nav`, justifying the `calcite-tab-title`s to the start (`"inline"`), or across and centered (`"center"`).
   */
  layout: TabLayout;
  /**
   * Specifies the position of the component in relation to the `calcite-tab`s. The `"above"` and `"below"` values are deprecated.
   *
   */
  position: TabPosition;
  /**
   * Specifies the size of the component.
   */
  scale: Scale;
  /**
   * When `true`, the component will display with a folder style menu.
   */
  bordered: boolean;
  render(): VNode;
  /**
   * @param event
   * @internal
   */
  calciteInternalTabTitleRegister(event: CustomEvent): void;
  /**
   * @param event
   * @internal
   */
  calciteTabTitleUnregister(event: CustomEvent): void;
  /**
   * @param event
   * @internal
   */
  calciteInternalTabRegister(event: CustomEvent): void;
  /**
   * @param event
   * @internal
   */
  calciteTabUnregister(event: CustomEvent): void;
  /**
   *
   * Stores an array of ids of `<calcite-tab-titles>`s to match up ARIA
   * attributes.
   */
  titles: HTMLCalciteTabTitleElement[];
  /**
   *
   * Stores an array of ids of `<calcite-tab>`s to match up ARIA attributes.
   */
  tabs: HTMLCalciteTabElement[];
  /**
   *
   * Matches up elements from the internal `tabs` and `titles` to automatically
   * update the ARIA attributes and link `<calcite-tab>` and
   * `<calcite-tab-title>` components.
   */
  registryHandler(): Promise<void>;
}
