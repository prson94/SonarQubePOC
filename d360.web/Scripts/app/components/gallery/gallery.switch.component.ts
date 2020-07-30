import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-switch',
    templateUrl: './gallery.switch.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class GallerySwitchComponent implements OnInit {
    protected properties: Array<any>;    
    protected events: Array<any>;
    protected sampleUsage: string = '<ig-switch></ig-switch>';
    protected bindingVal: boolean = true;
    protected formVal: boolean = false;
    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "ngModel", Type: "boolean", Description: "Value for the switch control true/false/undefined", Default: "false" });
        this.properties.push({ Name: "trueLabel", Type: "string", Description: "Text to show for the true side of the switch", Default: "True" });
        this.properties.push({ Name: "falseLabel", Type: "string", Description: "Text to show for the false side of the switch", Default: "False" });
        this.properties.push({ Name: "alwaysSet", Type: "boolean", Description: "Determines if the control allows the unselected state", Default: "false" });
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Used to set the control to disabled state where the user cannot interact with it", Default: "false" });
        this.properties.push({ Name: "style", Type: "string", Description: "Inline style of the component.", Default: "" });
        this.properties.push({ Name: "styleClass", Type: "string", Description: "Style class of the component.", Default: "" });
        this.properties.push({ Name: "inputId", Type: "string", Description: "Identifier of the focus input to match a label defined for the dropdown.", Default: "" });
        this.properties.push({ Name: "tabindex", Type: "number", Description: "Index of the element in tabbing order.", Default: "0" });
        

        this.events = new Array();
        this.events.push({ Name: "onChange", Description: "Fired when the selection changes" });
    }
}
