import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-date-picker',
    templateUrl: './gallery.date-picker.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryDatePickerComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<date-picker igTextArea></date-picker>';

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Whether or not the textarea control is disabled", Default: "" });
        this.properties.push({ Name: "required", Type: "Boolean", Description: "Whether or not the textarea control is required", Default: "" });
    }
}
