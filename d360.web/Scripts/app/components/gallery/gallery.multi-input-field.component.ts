import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-multi-input-field',
    templateUrl: './gallery.multi-input-field.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }

        .requiredMessage{
            padding: 5px;
            color: #900;
            font-weight: bold;
        }
        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryMultiInputFieldComponent implements OnInit {
    properties: Array<any>;
    events: Array<any>;
    sampleUsage: string = '<ig-icon-picker [(ngModel)]="model.Icon"></ig-icon-picker>';

    value;
    formValue;

    constructor(private ref: ChangeDetectorRef) { }

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "ngModel", Type: "string", Description: "Model representing the value of the icon picker control", Default: "null" });
        this.properties.push({ Name: "tabindex", Type: "number", Description: "Index of the element in tabbing order.", Default: "0" });
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Used to set the control to disabled state where the user cannot interact with it", Default: "false" });
        this.properties.push({ Name: "required", Type: "boolean", Description: "When this attribute is present the control must have a selected value to be valid", Default: "" });
        this.properties.push({ Name: "style", Type: "string", Description: "Inline style of the component.", Default: "" });

        this.events = new Array();
        this.events.push({ Name: "ngModelChange", Description: "Fired when the selection changes" });

        //wait for icons to load
        setTimeout(() => this.ref.markForCheck(), 500);
    }
}
