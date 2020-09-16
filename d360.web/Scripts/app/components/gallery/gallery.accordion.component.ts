import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-accordion',
    templateUrl: './gallery.accordion.component.html',
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

export class GalleryAccordionComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<simple-accordion header="Section 1"><p>Lorem ipsum...</p></simple-accordion>';

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "header", Type: "string", Description: "Title of the expandable section", Default: "" });
        this.properties.push({ Name: "active", Type: "boolean", Description: "Whether or not the accordion is expanded", Default: "" });
        this.properties.push({ Name: "tooltip", Type: "string", Description: "Text for optional tooltip", Default: "" });
    }
}
