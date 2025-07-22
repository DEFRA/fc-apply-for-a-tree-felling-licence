import { subclass, property } from "esri/core/accessorSupport/decorators";
import Widget from "esri/widgets/Widget";
import { tsx } from "esri/widgets/support/widget";
import Graphic = require("esri/Graphic");
import MapView = require("esri/views/MapView");

interface iShape {
  compartmentName: string;
  itemId: string;
  visible: boolean;
}

@subclass("esri.widgets.SelectionWidget")
class SelectionWidget extends Widget {
  @property()
  shapes: iShape[];

  @property()
  view!: MapView;

  constructor(params?: any) {
    super(params);
  }

  changeEvt(evt: InputEvent) {
    this.shapes.every((shape: iShape) => {
      if (shape.itemId === (evt.currentTarget as any).value) {
        shape.visible = !shape.visible;
        if (shape.visible) {
          this.emit("show", { id: shape.itemId });
        }else {
          this.emit("hide", { id: shape.itemId });
        }
        return false;
      }
      return true;
    });
  }

  renderList() {
    return (
      <calcite-list selection-appearance="border" key="graphics">
        {this.shapes.map((shape: iShape) => {
          return (
            <calcite-list-item
              onclick={this.changeEvt.bind(this)}
              key={shape.itemId}
              label={shape.compartmentName}
              value={shape.itemId}
            >
              {this.renderAction(shape)}
            </calcite-list-item>
          );
        })}
      </calcite-list>
    );
  }

  renderAction(shape: iShape) {
    var icon = shape.visible ? "layer" : "layer-hide";
    var filler = shape.visible ? " " : " not ";
    return (
      <calcite-action
        slot="actions-end"
        icon={icon}
        text={`${shape.compartmentName} is${filler}visible`}
      ></calcite-action>
    );
  }

  render() {
    let style =
      "max-height: 500px;overflow: auto;resize: vertical;";

    return (
      <div style={style}>
        <calcite-panel heading="Select Compartments">
          {this.renderList()}
        </calcite-panel>
      </div>
    );
  }
}
export = SelectionWidget;
