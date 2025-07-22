import { subclass, property } from "esri/core/accessorSupport/decorators";
import Widget from "esri/widgets/Widget";
import { tsx } from "esri/widgets/support/widget";
import MapView = require("esri/views/MapView");
import proj4 = require("proj4");

@subclass("esri.widgets.DownloadWidget")
class DownloadWidget extends Widget {
  @property()
  view!: MapView;

  constructor(params?: any) {
    super(params);
    proj4.defs("EPSG:27700", "+proj=tmerc +lat_0=49 +lon_0=-2 +k=0.999601 +x_0=400000 +y_0=-100000 +ellps=airy +towgs84=446.448,-125.157,542.06,0.15,0.247,0.842,-20.489 +units=m +no_defs");
    proj4.defs("EPSG:4326", "+proj=longlat +datum=WGS84 +no_defs");
  }


  handleDownload = () => {

    const jsonString = JSON.stringify(this.getJsonData());
    const blob = new Blob([jsonString], { type: "application/json" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = "mapDownload.geojson";
    a.click();
    URL.revokeObjectURL(a.href);

  };

  private getJsonData() {
    const jsonData = {
      type: "FeatureCollection",
      features: []
    };

    // Loop through the layers
    this.view.map.layers.forEach(layer => {
      (layer as any).source.items.forEach(graphic => {
        jsonData.features.push({
          type: "Feature",
          geometry: {
            type: this.getShapeType(graphic.geometry.type),
            coordinates: this.getCoordinates(graphic)
          },
          properties: {
            name: graphic.attributes.compartmentName,
          }
        });
      });
    });
    return jsonData;
  }

  public getShapeType(value: string): string {
    let result = "Polygon";
    if (value === "point") {
      result = "Point";
    } else if (value === "polyline") {
      result = "LineString";
    }
    return result;
  }

  public getCoordinates(graphic: any): any {
    let result: any;
    if (graphic.geometry.type === "polygon") {
      result = graphic.geometry.rings.map((ring: any) =>
        ring.map((point: any) => proj4('EPSG:27700', 'EPSG:4326', point))
      );
    } else if (graphic.geometry.type === "polyline") {
      if (graphic.geometry.paths.length < 1) {
        return [[]];
      }
      //Dropping to the first line as the rings struggle in GEOJSON
      result = graphic.geometry.paths[0].map((ring) => proj4('EPSG:27700', 'EPSG:4326', ring));
    } else {
      result = proj4("EPSG:27700", "EPSG:4326", [graphic.geometry.x, graphic.geometry.y]);
    }
    return result;
  }


  render() {
    return (
      <calcite-action-pad
        expand-disabled expanded
      >
        <calcite-action
          text="Download"
          icon="save"
          scale="m"
          onclick={this.handleDownload}
        ></calcite-action>
      </calcite-action-pad>
    );
  }
}
export = DownloadWidget;
