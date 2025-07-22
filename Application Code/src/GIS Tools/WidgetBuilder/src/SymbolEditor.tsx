import { subclass, property } from "esri/core/accessorSupport/decorators";
import Widget from "esri/widgets/Widget";
import { tsx } from "esri/widgets/support/widget";
import Symbol = require("esri/symbols/Symbol");
import MapView = require("esri/views/MapView");
import Input = require("esri/views/input/Input");

@subclass("esri.widgets.SelectionWidget")
class SymbolEditor extends Widget {
  @property()
  shape: {
    shapeType: string;
    shapeSymbol: Symbol;
    labelSymbol: Symbol;
    id: string;
  };

  @property()
  view!: MapView;

  constructor(params?: any) {
    super(params);
    this.shape = null;
  }

  shapeChanged() {
    var jsonSymbol = {};
    let val = "";
    if (this.shape !== null) {
      val = this.shape.shapeType;
    }
    switch (val) {
      case "polygon":
        jsonSymbol = {
          type: this.shape.shapeSymbol.type,
          style: (this.shape.shapeSymbol as any).style,
          color: this.shape.shapeSymbol.color,
          outline: {
            color: (this.shape.shapeSymbol as any).outline.color,
            width: (this.shape.shapeSymbol as any).width,
          },
        };
        break;
      case "polyline":
        jsonSymbol = {
          type: this.shape.shapeSymbol.type,
          style: (this.shape.shapeSymbol as any).style,
          cap: (this.shape.shapeSymbol as any).cap,
          join: (this.shape.shapeSymbol as any).join,
          width: (this.shape.shapeSymbol as any).width,
          color: (this.shape.shapeSymbol as any).color,
          xoffset: (this.shape.shapeSymbol as any).xoffset,
          yoffset: (this.shape.shapeSymbol as any).yoffset,
        };

        break;
      case "point":
        jsonSymbol = {
          type: this.shape.shapeSymbol.type,
          style: (this.shape.shapeSymbol as any).style,
          angle: (this.shape.shapeSymbol as any).angle,
          size: (this.shape.shapeSymbol as any).size,
          xoffset: (this.shape.shapeSymbol as any).xoffset,
          yoffset: (this.shape.shapeSymbol as any).yoffset,
          color: (this.shape.shapeSymbol as any).color,
          outline: {
            style: (this.shape.shapeSymbol as any).outline.style,
            cap: (this.shape.shapeSymbol as any).outline.cap,
            join: (this.shape.shapeSymbol as any).outline.join,
            width: (this.shape.shapeSymbol as any).outline.width,
            color: (this.shape.shapeSymbol as any).outline.color,
          },
        };
        break;
    }

    this.emit("shapeChanged", {
      id: this.shape.id,
      Symbol: jsonSymbol,
    });
  }

  LabelChanged() {
    const jsonSymbol = {
      type: this.shape.labelSymbol.type,
      text: (this.shape.labelSymbol as any).text,
      color: (this.shape.labelSymbol as any).color,
      haloColor: (this.shape.labelSymbol as any).haloColor,
      haloSize: (this.shape.labelSymbol as any).haloSize,
      xoffset: (this.shape.labelSymbol as any).xoffset,
      yoffset: (this.shape.labelSymbol as any).yoffset,
      font: {
        size: (this.shape.labelSymbol as any).font.size,
        weight: (this.shape.labelSymbol as any).font.weight,
      },
    };

    this.emit("labelChanged", {
      id: this.shape.id,
      Symbol: jsonSymbol,
    });
  }

  renderNotSelected() {
    return (
      <calcite-block width="200px"
        key="nullOption"
        open
        heading="No Shape Selected"
      >
        <calcite-icon
          scale="s"
          slot="icon"
          icon="exclamation-mark-triangle"
        ></calcite-icon>
         <calcite-notice open>
                <div slot="message">Please select shape to edit symbols</div>
            </calcite-notice>
      </calcite-block>
    );
  }

  renderShape() {
    let val = "";
    let renderer = "";
    if (this.shape !== null) {
      val = this.shape.shapeType;
    }

    switch (val) {
      case "polygon":
        renderer = this.renderPolygon() as any;
        break;
      case "polyline":
        renderer = this.renderPolyline() as any;
        break;
      case "point":
        renderer = this.renderPoint() as any;
        break;
    }
    return renderer;
  }

  rgba2hex(orig) {
    orig = orig.toString();
    var a,
      isPercent,
      rgb = orig
        .replace(/\s/g, "")
        .match(/^rgba?\((\d+),(\d+),(\d+),?([^,\s)]+)?/i),
      alpha = ((rgb && rgb[4]) || "").trim(),
      hex = rgb
        ? (rgb[1] | (1 << 8)).toString(16).slice(1) +
          (rgb[2] | (1 << 8)).toString(16).slice(1) +
          (rgb[3] | (1 << 8)).toString(16).slice(1)
        : orig;

    if (alpha !== "") {
      a = alpha;
    } else {
      a = 0o1;
    }

    return "#" + hex;
  }

  renderPolygon() {
    return (
      <calcite-block
        key="polygonSymbol"
        collapsible
        open
        heading="Polygon Style"
      >
        <calcite-icon scale="s" slot="icon" icon="polygon"></calcite-icon>
        <calcite-label>
          style
          <select
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).style = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).style}
          >
            <option>backward-diagonal</option>
            <option>cross</option>
            <option>diagonal-cross</option>
            <option>forward-diagonal</option>
            <option>horizontal</option>
            <option>none</option>
            <option>solid</option>
            <option>vertical</option>
          </select>
        </calcite-label>
        <calcite-label>
          Fill
          <input
            type="color"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).color = evt.target.value;
              this.shapeChanged();
            }}
            value={this.rgba2hex((this.shape.shapeSymbol as any).color)}
          />
        </calcite-label>
        <calcite-block-section open text="Outline" toggle-display="button">
          <calcite-notice open>
            <div slot="message">
              <calcite-label>
                Color
                <input
                  type="color"
                  onchange={(evt: any) => {
                    (this.shape.shapeSymbol as any).outline.color =
                      evt.target.value;
                    this.shapeChanged();
                  }}
                  value={this.rgba2hex(
                    (this.shape.shapeSymbol as any).outline.color
                  )}
                />
              </calcite-label>
              <calcite-label>
                Width
                <input
                  type="number"
                  min="0"
                  step="0.25"
                  onchange={(evt: any) => {
                    (this.shape.shapeSymbol as any).outline.width =
                      evt.target.value;
                    this.shapeChanged();
                  }}
                  value={(this.shape.shapeSymbol as any).outline.width}
                />
              </calcite-label>
            </div>
          </calcite-notice>
        </calcite-block-section>
      </calcite-block>
    );
  }

  renderPolyline() {
    return (
      <calcite-block key="polylineSymbol" open heading="Polyline Style">
        <calcite-icon scale="s" slot="icon" icon="line"></calcite-icon>
        <calcite-label>
          Style
          <select
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).style = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).style}
          >
            <option value="dash">dash</option>
            <option value="dash-dot">dash-dot</option>
            <option value="dot">dot</option>
            <option value="long-dash">long-dash</option>
            <option value="long-dash-dot">long-dash-dot</option>
            <option value="long-dash-dot-dot">long-dash-dot-dot</option>
            <option value="none">none</option>
            <option value="short-dash">short-dash</option>
            <option value="short-dash-dot">short-dash-dot</option>
            <option value="short-dash-dot-dot">short-dash-dot-dot</option>
            <option value="short-dot">short-dot</option>
            <option value="solid">solid</option>
          </select>
        </calcite-label>
        <calcite-label>
          Cap
          <select
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).cap = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).cap}
          >
            <option value="butt">butt</option>
            <option value="round">round</option>
            <option value="square">square</option>
          </select>
        </calcite-label>
        <calcite-label>
          Join
          <select
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).join = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).join}
          >
            <option value="miter">miter</option>
            <option value="round">round</option>
            <option value="bevel">bevel</option>
          </select>
        </calcite-label>
        <calcite-label>
          Width
          <input
            type="number"
            min="0"
            step="0.25"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).width = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).width}
          />
        </calcite-label>
        <calcite-label>
          Color
          <input
            type="color"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).color = evt.target.value;
              this.shapeChanged();
            }}
            value={this.rgba2hex((this.shape.shapeSymbol as any).color)}
          />
        </calcite-label>
        <calcite-label>
          X-Offset
          <input
            type="number"
            min="0"
            step="1"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).xoffset = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).xoffset}
          />
        </calcite-label>
        <calcite-label>
          Y-Offset
          <input
            type="number"
            min="0"
            step="1"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).yoffset = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).yoffset}
          />
        </calcite-label>
      </calcite-block>
    );
  }

  renderPoint() {
    return (
      <calcite-block key="PointSymbol" open heading="Point Style">
        <calcite-icon scale="s" slot="icon" icon="point"></calcite-icon>
        <calcite-label>
          Style
          <select
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).style = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).style}
          >
            <option value="circle">circle</option>
            <option value="cross">cross</option>
            <option value="diamond">diamond</option>
            <option value="path">path</option>
            <option value="square">square</option>
            <option value="x">x</option>
          </select>
        </calcite-label>
        <calcite-label>
          Angle
          <input
            type="range"
            placeholder="0"
            min="-360"
            max="360"
            step="1"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).angle = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).angle}
          ></input>
        </calcite-label>
        <calcite-label>
          Size
          <input
            type="number"
            min="0"
            step="1"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).size = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).size}
          />
        </calcite-label>
        <calcite-label>
          X-Offset
          <input
            type="number"
            min="0"
            step="1"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).xoffset = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).xoffset}
          />
        </calcite-label>
        <calcite-label>
          Y-Offset
          <input
            type="number"
            min="0"
            step="1"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).yoffset = evt.target.value;
              this.shapeChanged();
            }}
            value={(this.shape.shapeSymbol as any).yoffset}
          />
        </calcite-label>
        <calcite-label>
          Color
          <input
            type="color"
            onchange={(evt: any) => {
              (this.shape.shapeSymbol as any).color = evt.target.value;
              this.shapeChanged();
            }}
            value={this.rgba2hex((this.shape.shapeSymbol as any).color)}
          />
        </calcite-label>
        <calcite-block-section open text="Outline" toggle-display="button">
          <calcite-notice open>
            <div slot="message">
              <calcite-label>
                Style
                <select
                  onchange={(evt: any) => {
                    (this.shape.shapeSymbol as any).outline.style =
                      evt.target.value;
                    this.shapeChanged();
                  }}
                  value={(this.shape.shapeSymbol as any).outline.style}
                >
                  <option value="dash">dash</option>
                  <option value="dash-dot">dash-dot</option>
                  <option value="dot">dot</option>
                  <option value="long-dash">long-dash</option>
                  <option value="long-dash-dot">long-dash-dot</option>
                  <option value="long-dash-dot-dot">long-dash-dot-dot</option>
                  <option value="none">none</option>
                  <option value="short-dash">short-dash</option>
                  <option value="short-dash-dot">short-dash-dot</option>
                  <option value="short-dash-dot-dot">short-dash-dot-dot</option>
                  <option value="short-dot">short-dot</option>
                  <option value="solid">solid</option>
                </select>
              </calcite-label>
              <calcite-label>
                Cap
                <select
                  onchange={(evt: any) => {
                    (this.shape.shapeSymbol as any).outline.cap =
                      evt.target.value;
                    this.shapeChanged();
                  }}
                  value={(this.shape.shapeSymbol as any).outline.cap}
                >
                  <option value="butt">butt</option>
                  <option value="round">round</option>
                  <option value="square">square</option>
                </select>
              </calcite-label>
              <calcite-label>
                Join
                <select
                  onchange={(evt: any) => {
                    (this.shape.shapeSymbol as any).outline.join =
                      evt.target.value;
                    this.shapeChanged();
                  }}
                  value={(this.shape.shapeSymbol as any).outline.join}
                >
                  <option value="miter">miter</option>
                  <option value="round">round</option>
                  <option value="bevel">bevel</option>
                </select>
              </calcite-label>
              <calcite-label>
                Width
                <input
                  type="number"
                  min="0"
                  step="0.25"
                  onchange={(evt: any) => {
                    (this.shape.shapeSymbol as any).outline.width =
                      evt.target.value;
                    this.shapeChanged();
                  }}
                  value={(this.shape.shapeSymbol as any).outline.width}
                />
              </calcite-label>
              <calcite-label>
                Color
                <input
                  type="color"
                  onchange={(evt: any) => {
                    (this.shape.shapeSymbol as any).outline.color =
                      evt.target.value;
                    this.shapeChanged();
                  }}
                  value={this.rgba2hex(
                    (this.shape.shapeSymbol as any).outline.color
                  )}
                />
              </calcite-label>
            </div>
          </calcite-notice>
        </calcite-block-section>
      </calcite-block>
    );
  }

  renderLabel() {
    return (
      <calcite-block key="LabelSymbol" collapsible heading="Label Style" open>
        <calcite-icon scale="s" slot="icon" icon="label"></calcite-icon>
        <calcite-label>
          Text
          <input
            type="text"
            value={(this.shape.labelSymbol as any).text}
            onchange={(evt: any) => {
              (this.shape.labelSymbol as any).text = evt.target.value;
              this.LabelChanged();
            }}
          />
        </calcite-label>
        <calcite-label>
          Color
          <input
            type="color"
            onchange={(evt: any) => {
              (this.shape.labelSymbol as any).color = evt.target.value;
              this.LabelChanged();
            }}
            value={this.rgba2hex((this.shape.labelSymbol as any).color)}
          />
        </calcite-label>
        <calcite-label>
          Halo Color
          <input
            type="color"
            onchange={(evt: any) => {
              (this.shape.labelSymbol as any).haloColor = evt.target.value;
              this.LabelChanged();
            }}
            value={this.rgba2hex((this.shape.labelSymbol as any).haloColor)}
          />
        </calcite-label>
        <calcite-label>
          Halo Size
          <input
            type="number"
            min="0"
            step="1"
            max={(this.shape.labelSymbol as any).font.size}
            onchange={(evt: any) => {
              (this.shape.labelSymbol as any).haloSize = evt.target.value;
              this.LabelChanged();
            }}
            value={(this.shape.labelSymbol as any).haloSize}
          />
        </calcite-label>
        <calcite-label>
          X-Offset
          <input
            type="number"
            min="0"
            step="1"
            onchange={(evt: any) => {
              (this.shape.labelSymbol as any).xoffset = evt.target.value;
              this.LabelChanged();
            }}
            value={(this.shape.labelSymbol as any).xoffset}
          />
        </calcite-label>
        <calcite-label>
          Y-Offset
          <input
            type="number"
            min="0"
            step="1"
            onchange={(evt: any) => {
              (this.shape.labelSymbol as any).yoffset = evt.target.value;
              this.LabelChanged();
            }}
            value={(this.shape.labelSymbol as any).yoffset}
          />
        </calcite-label>
        <calcite-block-section open text="Font" toggle-display="button">
          <calcite-notice open>
            <div slot="message">
              <calcite-label>
                Size
                <input
                  type="number"
                  min="0"
                  step="1"
                  max={100}
                  onchange={(evt: any) => {
                    (this.shape.labelSymbol as any).font.size =
                      evt.target.value;
                    this.LabelChanged();
                  }}
                  value={(this.shape.labelSymbol as any).font.size}
                />
              </calcite-label>
              <calcite-label>
                style
                <select
                  onchange={(evt: any) => {
                    (this.shape.labelSymbol as any).font.weight =
                      evt.target.value;
                    this.LabelChanged();
                  }}
                  value={(this.shape.labelSymbol as any).font.weight}
                >
                  <option value="normal">normal</option>
                  <option value="bold">bold</option>
                  <option value="bolder">bolder</option>
                  <option value="lighter">lighter</option>
                </select>
              </calcite-label>
            </div>
          </calcite-notice>
        </calcite-block-section>
      </calcite-block>
    );
  }

  render() {
    let style =
      this.shape === null
        ? ""
        : "max-height: 500px;overflow: auto;resize: vertical;";
    return (
      <div style={style}>
        <calcite-panel heading="Style">
          {this.renderShape()}
          {this.shape === null ? this.renderNotSelected() : this.renderLabel()}
        </calcite-panel>
      </div>
    );
  }
}
export = SymbolEditor;
