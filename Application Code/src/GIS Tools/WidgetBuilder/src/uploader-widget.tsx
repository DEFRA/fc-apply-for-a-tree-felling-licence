import { subclass, property } from "esri/core/accessorSupport/decorators";
import Widget from "esri/widgets/Widget";
import { tsx } from "esri/widgets/support/widget";
import MapView from "esri/views/MapView";
import Map from "esri/Map";
import FeatureLayer from "esri/layers/FeatureLayer";
import Graphic from "esri/Graphic";
import Field from "esri/layers/support/Field";

interface iParams extends __esri.WidgetProperties {
  view: MapView;
  map: Map;
  geoConvertor: any;
  multiUpload: boolean;
}

@subclass("esri.widgets.UploadWidget")
class UploadWidget extends Widget {
  @property()
  supportedFileTypes!: string[];

  @property()
  view!: MapView;

  @property()
  multiUpload!: boolean;

  @property()
  geoConvertor!: any;

  @property()
  map!: Map;

  @property()
  hasUpload: boolean;

  @property()
  currentlySelecting: boolean;

  @property()
  errorMessage: string;

  @property()
  isUploading: boolean;

  @property()
  hasMappings: boolean;

  @property()
  allowMappings: boolean;

  @property()
  compartmentName: string;

  @property()
  subCompartmentName: string;

  @property()
  designation: string;

  @property()
  woodlandName: string;

  @property()
  maxFileSize:number;

  constructor(params?: iParams) {
    super(params as any);
    this.errorMessage = "";
    this.isUploading = false;
    this.hasUpload = false;
    this.currentlySelecting = false;
  }

  stopSelection() {
    if (this.currentlySelecting !== false) {
      this.currentlySelecting = false;
    }
  }

  clearEvt() {
    let elm = document.getElementById("inFile");
    if (elm) {
      (elm as HTMLInputElement).value = "";
    }

    const importedLayers = (this.map.layers as any).items.filter((l: any) => {
      return l.IsImported;
    });
    this.compartmentName =null;
    this.subCompartmentName = null;
    this.woodlandName = null;
    this.designation = null;
    this.emit("noSelecting", {});
    this.map.layers.removeMany(importedLayers);
    this.errorMessage = "";
    this.isUploading = false;
    this.hasUpload = false;
  }

getSize(value: number){
  return value/ Math.pow(1024,2) + "MB";
}

  changeEvt(evt: InputEvent) {
    var that = this;
    this.errorMessage = "";
    let elm = document.getElementById("inFile");
    if (!elm) {
      this.errorMessage = "Unable to find file";
      return;
    }
    const filePath = (elm as HTMLInputElement).value.toLowerCase();
    if (typeof filePath === "undefined" || filePath.length === 0) {
      this.errorMessage = "No file found";
      return;
    }
    const fileExt = filePath.slice(filePath.length - 4, filePath.length);
    if (!this.supportedFileTypes.includes(fileExt)) {
      this.errorMessage = "File Type is not supported";
      return;
    }

    this.isUploading = true;

    if (fileExt.toLocaleLowerCase() === ".kml") {
      var fileInput = document.getElementById("inFile");
      if (!fileInput) {
        this.isUploading = false;
        this.errorMessage = "Unable to find file";
        return;
      }
      var fileReader = new FileReader();
      fileReader.readAsText((fileInput as any).files[0]);
      fileReader.onload = function () {
        var dom = new DOMParser().parseFromString(
          (fileReader as any).result,
          "text/xml"
        );
        fetch(window.origin + "/api/Gis/GetShapesFromString", {
          method: "POST",
          body: that.getStringFormData(
            that.geoConvertor.kml(dom),
            that.getfileNamePartsArray(filePath)
          ),
        })
          .then((r) => {
            if (r.status != 200) {
              return r
                .text()
                .then((data) => ({ status: r.status, body: data }));
            } else {
              return r
                .json()
                .then((data) => ({ status: r.status, body: data }));
            }
          })
          .then((obj) => {
            if (obj.status !== 200) {
              that.isUploading = false;
              that.hasUpload = false;
              that.emit("failed", { message: obj.body });
              return;
            }
            that.processResult(obj.body);
            that.errorMessage = "";
            that.isUploading = false;
            that.hasUpload = true;
            that.selectEvt();
            if (that.allowMappings) {
              that.emit("uploaded", {
                compartmentName: that.compartmentName,
                subCompartmentName: that.subCompartmentName,
                designation: that.designation,
                woodlandName: that.woodlandName,
              });
            }
            else {
              that.emit("uploaded");
            }
          })
          .catch(function (e) {
            that.isUploading = false;
            that.hasUpload = false;
            that.emit("failed", { message: e });
          });
      };
      fileReader.onerror = function () {
        that.isUploading = false;
        that.errorMessage = fileReader.error.message;
      };
    } else {

      if( (document.getElementById("inFile") as any).files[0].size > that.maxFileSize){
        that.isUploading = false;
        that.hasUpload = false;
        that.errorMessage = "File is too big to be imported currently supported size is " + that.getSize(that.maxFileSize) + "";
        return;
      }
      
      fetch(window.origin + "/api/Gis/GetShapes", {
        method: "POST",
        body: this.getFormData(this.getfileNamePartsArray(filePath)),
      })
        .then((r) => {
          if (r.status != 200) {
            return r.text().then((data) => ({ status: r.status, body: data }));
          } else {
            return r.json().then((data) => ({ status: r.status, body: data }));
          }
        })
        .then((obj) => {
          if (obj.status !== 200) {
            that.isUploading = false;
            that.hasUpload = false;
            that.emit("failed", { message: obj.body });
            return;
          }
          that.processResult(obj.body);
          that.errorMessage = "";
          that.isUploading = false;
          that.hasUpload = true;
          that.selectEvt();
          if (that.allowMappings) {
            that.emit("uploaded", {
              compartmentName: that.compartmentName,
              subCompartmentName: that.subCompartmentName,
              designation: that.designation,
              woodlandName: that.woodlandName,
            });
          }
          else {
            that.emit("uploaded");
          }
        })
        .catch(function (e) {
          that.isUploading = false;
          that.hasUpload = false;
          that.emit("failed", { message: e });
        });
    }
  }

  public selectEvt() {
    this.currentlySelecting = !this.currentlySelecting;
    this.emit(this.currentlySelecting ? "selecting" : "noSelecting", {});
  }

  processResult(response: any) {
    var sourceGraphics: any[] = [];
    var layers = response.featureCollection.layers.map(function (layer) {
      var graphics = layer.featureSet.features.map(function (feature) {
        return Graphic.fromJSON(feature);
      });
      sourceGraphics = sourceGraphics.concat(graphics);
      var featureLayer = new FeatureLayer({
        source: graphics,
        fields: layer.layerDefinition.fields.map(function (field) {
          return Field.fromJSON(field);
        }),
      });
      (featureLayer as any).IsImported = true;
      return featureLayer;
    });

    this.map.addMany(layers);
    this.view.goTo(sourceGraphics).catch(function (error) {
      if (error.name != "AbortError") {
        console.error(error);
      }
    });
  }

  getfileNamePartsArray(filePath: string) {
    return filePath.replace("c:\\fakepath\\", "").split(".");
  }

  getFormData(fileNameParts: string[]) {
    var name = "";
    for (let i = fileNameParts.length - 2; i >= 0; i--) {
      name = fileNameParts[i] + name;
    }
    var formData = new FormData();
    formData.append(
      "file",
      (document.getElementById("inFile") as any).files[0]
    );
    formData.append("name", name);
    formData.append("ext", fileNameParts[fileNameParts.length - 1]);
    return formData;
  }

  getStringFormData(data: JSON, fileNameParts: string[]) {
    var name = "";
    for (let i = fileNameParts.length - 2; i >= 0; i--) {
      name = fileNameParts[i] + name;
    }
    var formData = new FormData();
    formData.append("valueString", JSON.stringify(data));
    formData.append("name", name);
    formData.append("ext", "geojson");
    return formData;
  }

  handleNameChange = (evt) => {
    this.compartmentName = evt.target.value;
  };

  handleSubNameChange = (evt) => {
    this.subCompartmentName = evt.target.value;
  };

  handleDesignationChange = (evt) => {
    this.designation = evt.target.value;
  };

  handleWoodlandChange = (evt) => {
    this.woodlandName = evt.target.value;
  };

  renderLoading() {
    return this.isUploading ? (
      <calcite-progress
        label="uploading"
        text="uploading"
        type="indeterminate"
      ></calcite-progress>
    ) : null;
  }

  renderError() {
    return !this.errorMessage ? null : (
      <calcite-notice open>
        <div slot="title">Error</div>
        <div slot="message">{this.errorMessage}</div>
      </calcite-notice>
    );
  }

  renderUploadButton() {
    return (
      <calcite-button
        slot="footer-actions"
        width="auto"
        disabled={this.isUploading}
        onclick={this.changeEvt.bind(this)}
      >
        upload
      </calcite-button>
    );
  }

  renderClearButton() {
    return (
      <calcite-button
        slot="footer-actions"
        width="auto"
        disabled={this.isUploading}
        onclick={this.clearEvt.bind(this)}
        appearance="clear"
      >
        Clear
      </calcite-button>
    );
  }

  renderCompartmentNumber() {
    return (
      <calcite-label>
        Compartment
        <input
          type="text"
          onchange={this.handleNameChange}
          value={this.compartmentName}
        ></input>
      </calcite-label>
    );
  }

  renderSubCompartmentName() {
    return (
      <calcite-label>
        Sub-compartment
        <input
          type="text"
          onchange={this.handleSubNameChange}
          value={this.subCompartmentName}
        ></input>
      </calcite-label>
    );
  }

  renderWoodland() {
    return (
      <calcite-label>
        Woodland
        <input
          type="text"
          onchange={this.handleWoodlandChange}
          value={this.woodlandName}
        ></input>
      </calcite-label>
    );
  }

  renderDesignation() {
    return (
      <calcite-label>
        Designation
        <input
          type="text"
          onchange={this.handleDesignationChange}
          value={this.designation}
        ></input>
      </calcite-label>
    );
  }

  renderMapping() {
    return (
      <calcite-block
        collapsible hidden={this.isUploading}
        heading="Field Mapping"
        description="Please select fields to be mapped from source file"
      >
        {[
          this.renderCompartmentNumber(),
          this.renderSubCompartmentName(),
          this.renderWoodland(),
          this.renderDesignation(),
        ]}
      </calcite-block>
    );
  }

  renderUpload() {
    return (
      <calcite-panel heading="Upload File">
        {[this.renderUploadButton(),this.renderClearButton()]}
        <calcite-block open hidden={!this.errorMessage}>
          {this.renderError()}
        </calcite-block>
        <calcite-block open hidden={this.isUploading} heading="File Settings">
          <form enctype="multipart/form-data" method="post" id="uploadForm">
            <input type="file" name="file" id="inFile"></input>
          </form>
        </calcite-block>
        {this.allowMappings ? this.renderMapping() : null}
        <calcite-block open hidden={!this.isUploading}>
          {this.renderLoading()}
        </calcite-block>
      </calcite-panel>
    );
  }

  renderButtons() {
    return (
      <calcite-action-bar layout="vertical" expanded>
        <calcite-action
          text="Import Shapes"
          icon="select"
          scale="m"
          active={this.currentlySelecting}
          onclick={this.selectEvt.bind(this)}
        ></calcite-action>
        <calcite-action
          text="upload new file"
          icon="upload-to"
          scale="m"
          onclick={this.clearEvt.bind(this)}
        ></calcite-action>
      </calcite-action-bar>
    );
  }

  render() {
    let style = "max-height: 500px;overflow: auto;height: 200px;resize: vertical;"
     if(this.hasUpload){
      style = "max-height: 500px;overflow: auto;"
     }
    return (
      <div style={style}>
        {this.hasUpload ? this.renderButtons() : this.renderUpload()}
      </div>
    );
  }
}

export = UploadWidget;
