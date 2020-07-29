import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-tag-picker',
    templateUrl: './gallery.tag-picker.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryTagPickerComponent implements OnInit {
    protected properties: Array<any>;
    protected events: Array<any>;
    protected sampleUsage: string = '<ig-tag-picker [(ngModel)]="model.Tags"></ig-tag-picker>';

    private value: string = 'Added Tag|Testing Data';
    private formValue;

    constructor(private ref: ChangeDetectorRef) { }

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "ngModel", Type: "string", Description: "Model representing the value of the tag picker control. Tag values are separated by '|'.", Default: "null" });
        this.properties.push({ Name: "tabindex", Type: "string", Description: "Index of the element in tabbing order.", Default: "null" });
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Used to set the control to disabled state where the user cannot interact with it", Default: "false" });
        this.properties.push({ Name: "style", Type: "string", Description: "Inline style of the component.", Default: "" });

        this.events = new Array();
        this.events.push({ Name: "ngModelChange", Description: "Fired when the selection changes" });
    }
}
