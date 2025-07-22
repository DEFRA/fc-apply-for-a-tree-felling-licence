import { subclass, property } from "esri/core/accessorSupport/decorators";
import Widget from "esri/widgets/Widget";
import { tsx } from "esri/widgets/support/widget";

interface iShape {
  shapeID: number;
  compartmentName: string;
  subCompartmentName: string;
  designation: string;
  woodlandName: string;
}

@subclass("esri.widgets.EditWidget")
class EditWidget extends Widget {
  @property()
  private shape: any;

  @property()
  private compartmentMessage: string = "";

  @property()
  private subCompartmentMessage: string = "";

  @property()
  private woodlandMessage: string = "";

  @property()
  private designationMessage: string = "";


  private reg: RegExp;

  constructor(params?: any) {
    super(params);
    this.compartmentMessage = "";
    this.subCompartmentMessage = "";
    this.shape = null;
    this.reg = new RegExp("^[-a-zA-Z0-9'\\s]*$");
  }

  public SetShape(shape?: iShape) {
    this.subCompartmentMessage = this.compartmentMessage = "";
    this.shape = shape;
  }

  handleNameChange = (evt) => {
    this.shape.compartmentName = evt.target.value;
    if (
      this.shape.compartmentName === null ||
      this.shape.compartmentName.length === 0
    ) {
      this.compartmentMessage = "Compartment name or number must be provided";
      return;
    }
    if (this.shape.compartmentName.length > 35) {
      this.compartmentMessage =
        "Compartment name or number must be 35 characters or less";
        return;
    }
    if (!this.reg.test(this.shape.compartmentName)) {
      this.compartmentMessage =
        "Compartment name or number must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes";
        return;
      }
      this.compartmentMessage="";
  };

  handleSubNameChange = (evt) => {
    this.shape.subCompartmentName = evt.target.value;
    if (
      this.shape.subCompartmentName === null ||
      this.shape.subCompartmentName.length === 0
    ) {
      return;
    }
    if (this.shape.subCompartmentName.length > 10) {
      this.subCompartmentMessage =
        "Sub-compartment name or number must be 10 characters or less";
        return;
    }
    if (!this.reg.test(this.shape.subCompartmentName)) {
      this.subCompartmentMessage =
        "Sub-compartment name or number must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes";
        return;
      }
      this.subCompartmentMessage ="";
  };

  handleDesignationChange = (evt) => {
    this.shape.designation = evt.target.value;
    if (this.shape.designation.length > 35) {
      this.designationMessage =
        "Designation must be 35 characters or less";
        return;
    }
  };

  handleWoodlandChange = (evt) => {
    this.shape.woodlandName = evt.target.value;
    if (this.shape.woodlandName.length > 35) {
      this.woodlandMessage =
        "Woodland must be 35 characters or less";
        return;
    }
  };

  handleCancel = (evt) => {
    this.SetShape(null);
    this.emit("cancel", {});
  };

  handleDelete = (evt) => {
    this.emit("delete", {shapeID: this.shape.shapeID});
    this.SetShape(null);
  };

  handleSubmit = (evt) => {
    this.emit("save", {
      shapeID: this.shape.shapeID,
      compartmentName: this.shape.compartmentName,
      subCompartmentName: this.shape.subCompartmentName,
      designation: this.shape.designation,
      woodlandName: this.shape.woodlandName
    });
    this.SetShape(null);
  };

  renderCompartmentNumber() {
    return (
      <calcite-label>
        Compartment
        <input type="text"
          onchange={this.handleNameChange}
          value={this.shape.compartmentName}
        ></input>
        <calcite-input-message active={this.compartmentMessage.length > 0}>
          {this.compartmentMessage}
        </calcite-input-message>
      </calcite-label>
    );
  }

  renderSubCompartmentName() {
    return (
      <calcite-label>
        Sub-compartment (optional)
        <input type="text" onchange={this.handleSubNameChange}
          value={this.shape.subCompartmentName}>
        </input>
        <calcite-input-message active={this.subCompartmentMessage.length > 0}>
          {this.subCompartmentMessage}
        </calcite-input-message>
      </calcite-label>
    );
  }

  renderWoodland() {
    return (
      <calcite-label>
        Woodland (optional)
        <input type="text" onchange={this.handleWoodlandChange}
          value={this.shape.woodlandName}>
        </input>
        <calcite-input-message active={this.woodlandMessage.length > 0}>
          {this.woodlandMessage}
        </calcite-input-message>
      </calcite-label>
    );
  }

  renderDesignation() {
    return (
      <calcite-label>
        Designation (optional)
        <input type="text" onchange={this.handleDesignationChange}
          value={this.shape.designation}>
        </input>
        <calcite-input-message active={this.designationMessage.length > 0}>
          {this.designationMessage}
        </calcite-input-message>
      </calcite-label>
    );
  }

  render() {
    return (
      <div>
        {this.shape === null ? null : (
          <calcite-panel heading="">
            <calcite-button color="red" width="auto"
             slot="footer-actions"
             onclick={this.handleDelete}
            >Remove Compartment</calcite-button>
            <calcite-button
              width="auto"
              slot="footer-actions"
              appearance="outline"
              onclick={this.handleCancel}
            >
              Cancel
            </calcite-button>
            <calcite-button
              width="auto"
              slot="footer-actions"
              disabled={
                this.compartmentMessage.length > 0 ||
                this.subCompartmentMessage.length > 0 ||
                this.designationMessage.length > 0 ||
                this.woodlandMessage.length >0
              }
              onclick={this.handleSubmit}
            >
              Apply
            </calcite-button>
            <calcite-block open>
              {this.shape === null
                ? null
                : [
                    this.renderCompartmentNumber(),
                    this.renderSubCompartmentName(),
                    this.renderWoodland(),
                    this.renderDesignation(),
                  ]}
            </calcite-block>
          </calcite-panel>
        )}
      </div>
    );
  }
}
export = EditWidget;
