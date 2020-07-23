import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-icon-picker',
    templateUrl: './gallery.icon-picker.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryIconPickerComponent implements OnInit {
    protected properties: Array<any>;
    protected events: Array<any>;
    protected sampleUsage: string = '<d3s-icon-picker [(ngModel)]="model.Icon"></d3s-icon-picker>';

    private value;

    constructor(private ref: ChangeDetectorRef) { }

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "ngModel", Type: "string", Description: "Model representing the value of the icon picker control", Default: "null" });
        this.properties.push({ Name: "tabindex", Type: "string", Description: "Index of the element in tabbing order.", Default: "null" });
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Used to set the control to disabled state where the user cannot interact with it", Default: "false" });
        this.properties.push({ Name: "ngModel", Type: "string", Description: "Model representing the value of the icon picker control", Default: "null" });

        this.events = new Array();
        this.events.push({ Name: "ngModelChange", Description: "Fired when the selection changes" });

        //wait for icons to load
        setTimeout(() => this.ref.markForCheck(), 500);
    }
}
