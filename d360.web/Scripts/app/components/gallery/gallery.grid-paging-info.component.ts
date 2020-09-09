import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-grid-paging-info',
    templateUrl: './gallery.grid-paging-info.component.html',
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

export class GalleryGridPagingInfoComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<d3s-grid-paging-info></d3s-grid-paging-info>';
    protected first: number = 5;
    protected rows = 10;
    protected totalRecords = 1500;
    protected items: any[] = [];

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "first", Type: "number", Description: "Number of the first item", Default: "" });
        this.properties.push({ Name: "rows", Type: "number", Description: "Number of rows in the page", Default: "" });
        this.properties.push({ Name: "totalRecords", Type: "number", Description: "Total number of rows", Default: "" });

        let i = 0;
        this.items = [];
        while (i < 1000) {
            i++;
            this.items.push({ name: 'Item ' + i});
        }
    }
}
