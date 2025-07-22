import { subclass, property } from "esri/core/accessorSupport/decorators";
import Widget from "esri/widgets/Widget";
import { tsx } from "esri/widgets/support/widget";
type Style = "" | "success" | "info" | "warning";

@subclass("esri.widgets.UploadWidget")
class MapAlert extends Widget {
  @property()
  message: string;
  @property()
  messages: string[];
  @property()
  style: string;
  @property()
  show: boolean;

  constructor(params?: any) {
    super(params as any);
    this.message = "";
    this.style = "";
    this.messages =[];
    this.show = false;
  }

  public ShowMessage(style: Style, message: string) {
    if (typeof message === "undefined" || message === "") {
      return;
    }
    this.style = style;
    this.message = message;
    this.messages = [];
    this.show = true;
  }

  public ShowMessages(style: Style, messages: string[]) {
    if (typeof messages === "undefined" || messages.length === 0) {
      return;
    }
    this.style = style;
    this.message = "";
    this.messages = messages;
    this.show = true;
  }

  public ShowDetailedMessages(style:Style, message: string, messages:string[]){
    if (typeof messages === "undefined"){
      return;
    }
    this.style = style;
    this.message = message;
    this.messages = messages;
    this.show = true;
  }


  closeEvt(evt: any) {
    this.show = false;
    this.messages = [];
    this.message ="";
  }

  public renderIcon() {
    let iconValue = "exclamation-mark-circle";
    switch (this.style) {
      case "success":
        iconValue = "check-square";
        break;
      case "info":
        iconValue = "information";
        break;
      case "warning":
        iconValue = "exclamation-mark-triangle";
        break;
    }
    const labelValue = this.style === "" ? "Error" : this.style;
    return (
      <calcite-icon
        aria-label={labelValue}
        icon={iconValue}
        scale="m"
      ></calcite-icon>
    );
  }
public renderList(){
  return(<ul>
    {this.messages.map((m) => {
      return(<li>{m}</li>);
    })}
  </ul>)
}
  public render(): any {
    var classes = ["alert"];
    return (
      <div
        role="alert"
        class={this.classes("alert", this.style)}
        hidden={!this.show}
      >
        {this.renderIcon()}
        <span
          class="closebtn"
          role="close"
          aria-label="close"
          onclick={this.closeEvt.bind(this)}
        >
          &times;
        </span>
        {this.message}
        {this.messages.length === 0? null : this.renderList()}
      </div>
    );
  }
}

export = MapAlert;
