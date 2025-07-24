import { subclass, property } from "esri/core/accessorSupport/decorators";
import Widget from "esri/widgets/Widget";
import { tsx } from "esri/widgets/support/widget";
import GeometryEngine = require("esri/geometry/geometryEngine");
import Graphic = require("esri/Graphic");
import Geometry = require("esri/geometry/Geometry");

@subclass("esri.widgets.MergeWidget")
class MergeWidget extends Widget {
  private graphics: Graphic[];

  @property()
  private enabled: boolean;

  private geometries: Geometry[];

  constructor(params?: any) {
    super(params);
    this.geometries = [];
    this.graphics = [];
    this.enabled = false;
  }

  public setGeometries(incoming: Graphic[]) {
    var _this = this;
    this.graphics = [];
    this.geometries = [];
    if (incoming) {
      incoming
        .filter(function (g) {
          return g.geometry.type === "polygon";
        })
        .forEach(function (g) {
          _this.graphics.push(g);
          _this.geometries.push(g.geometry);
        });
    }
    this.enabled = this.geometries.length > 1;
  }

  public SwapEvent() {
    this.emit("swapped", {
      graphics: [this.graphics[1], this.graphics[0]],
    });
  }

  public mergeEvent() {
    if (typeof this.geometries === "undefined" || this.geometries.length < 2) {
      return;
    }
    var result = GeometryEngine.union(this.geometries);
    this.emit("merged", { geometry: result, merged: this.graphics });
    this.setGeometries([]);
  }

  public cutEvent() {
    if (typeof this.geometries === "undefined" || this.geometries.length < 2) {
      return;
    }
    var result = GeometryEngine.difference(
      this.geometries[1],
      this.geometries[0]
    );
    this.emit("cut", { geometry: result, cut: this.graphics });
    this.setGeometries([]);
  }


  render() {
    return (
      <div style="max-height: 100px;overflow: auto;">
        <calcite-action-pad
          layout="horizontal"
          expanded
          hidden={!this.enabled}
          position="start"
        >
          <calcite-action
            text="Swap Ordering of selected shape"
            icon="center-vertical"
            scale="m"
            onclick={this.SwapEvent.bind(this)}
          ></calcite-action>
          <calcite-action
            text="Merge"
            icon="merge"
            scale="m"
            onclick={this.mergeEvent.bind(this)}
          ></calcite-action>
          <calcite-action
            text="Cut Shape 1 from shape 2"
            icon="cut-and-fill-volume-calculation"
            scale="m"
            onclick={this.cutEvent.bind(this)}
          ></calcite-action>
        </calcite-action-pad>
      </div>
    );
  }
}

export = MergeWidget;
