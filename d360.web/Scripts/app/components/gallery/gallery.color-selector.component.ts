import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';


@Component({
    selector: 'gallery-color-selector',
    templateUrl: './gallery.color-selector.component.html',
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

export class GalleryColorSelectorComponent implements OnInit {
    properties: Array<any>;
    events: Array<any>;
    sampleUsage: string = `<color-selector [(ngModel)]="value" [appendTo]="'body'"></color-selector>`;
    value: string = "#ffffff";

    ngOnInit(): void {

        this.properties = [];
        this.properties.push({ Name: "ngModel", Type: "Date", Description: "Model binding for the selected date object", Default: "" });
        this.properties.push({ Name: "appendTo", Type: "any", Description: "Target element to attach the overlay, valid values are 'body' or a local ng-template variable of another element", Default: "" });

    }
}
