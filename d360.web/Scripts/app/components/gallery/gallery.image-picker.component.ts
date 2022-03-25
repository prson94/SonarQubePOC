import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-image-picker',
    templateUrl: './gallery.image-picker.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        .model-value {
            display: inline-block;
            width: 400px;
            text-overflow: ellipsis;
            white-space: nowrap;
            overflow: hidden;
            position: relative;
            top: 5px;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryImagePickerComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<image-picker></image-picker>';
    modelBinding: string = '';

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "imageType", Type: "string", Description: "Allowed values: 'ICO','LOGO' or empty string", Default: "" });
        this.properties.push({ Name: "allowedExtensions", Type: "string", Description: "Comma separated values of allowed file types", Default: "image/png,image/gif,image/jpg,image/jpeg" });
        this.properties.push({ Name: "maxHeight", Type: "number", Description: "Max image height", Default: "" });
        this.properties.push({ Name: "maxWidth", Type: "number", Description: "Max image width", Default: "" });
        this.properties.push({ Name: "maxSize", Type: "number", Description: "Max image size in bytes", Default: "" });
        this.properties.push({ Name: "message", Type: "string", Description: "User defined message to appear on right side of component", Default: "" });
        this.properties.push({ Name: "chooseFileTooltip", Type: "string", Description: "Tooltip value of 'Choose file' button", Default: "Choose file" });
        this.properties.push({ Name: "restoreFileTooltip", Type: "string", Description: "Tooltip value of 'Restore file' button", Default: "Restore the file" });
    }
}
