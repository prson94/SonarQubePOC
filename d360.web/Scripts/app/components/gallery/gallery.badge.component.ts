import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-badge',
    templateUrl: './gallery.badge.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ],    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryBadgeComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<ig-badge [text]="\'Im a badge!\'"></ig-badge>';
    
    protected clicks: string[] = [];

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "text", Type: "string", Description: "The text value to be displayed by the badge.", Default: "" });
        this.properties.push({ Name: "variant", Type: "string", Description: "String value for the style for the badge. [default, emphasis, positive, negative, warning, light, custom-light and custom-dark] are the options", Default: "default" });
        this.properties.push({ Name: "backgroundColor", Type: "string", Description: "An override for the background color.", Default: "" });
    }
}
