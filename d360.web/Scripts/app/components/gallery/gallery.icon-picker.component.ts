import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-icon-picker',
    templateUrl: './gallery.icon-picker.component.html',
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

        this.events = new Array();
        this.events.push({ Name: "ngModelChange", Description: "Fired when the selection changes" });

        //wait for icons to load
        setTimeout(() => this.ref.markForCheck(), 500);
    }
}
