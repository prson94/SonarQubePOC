import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-boolean',
    templateUrl: './gallery.boolean.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryBooleanComponent implements OnInit {
    protected properties: Array<any>;    
    protected events: Array<any>;

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "label", Type: "string", Description: "Label to show on top of the editor", Default: "" });
        this.properties.push({ Name: "value", Type: "boolean", Description: "Selected value of the control.  Doesnt support two way binding", Default: "false" });
        this.properties.push({ Name: "falseTitle", Type: "string", Description: "doesnt work", Default: "" });
        this.properties.push({ Name: "falseText", Type: "string", Description: "doesnt work", Default: "" });
        this.properties.push({ Name: "falseButtonText", Type: "string", Description: "doesnt work", Default: "" });
        this.properties.push({ Name: "trueTitle", Type: "string", Description: "doesnt work", Default: "" });
        this.properties.push({ Name: "trueText", Type: "string", Description: "doesnt work", Default: "" });
        this.properties.push({ Name: "trueButtonText", Type: "string", Description: "doesnt work", Default: "" });

        this.events = new Array();
        this.events.push({ Name: "onchange", Description: "Fired when the selection changes" });
    }
}
