import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-grid-selection-info',
    templateUrl: './gallery.grid-selection-info.component.html',
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

export class GalleryGridSelectionInfoComponent implements OnInit {
    protected properties: Array<any>;
    protected events: Array<any>;
    protected sampleUsage: string = '<d3s-grid-selection-info></d3s-grid-selection-info>';

    protected items = [
        { name: 'Item 1' },
        { name: 'Item 2' },
        { name: 'Item 3' },
        { name: 'Item 4' },
        { name: 'Item 5' },
    ];

    protected selection = [];

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "includeSelectLinks", Type: "boolean", Description: "When true the select all/none buttons will be displayed", Default: "true" });
        this.properties.push({ Name: "model", Type: "any[]", Description: "Array of items representing the entire set of data", Default: "[]" });
        this.properties.push({ Name: "selection", Type: "any[]", Description: "Array of items representing the seleected data", Default: "[]" });

        this.events = new Array();
        this.events.push({ Name: "onSelectAllClick", Description: "Fires when select all is clicked" });
        this.events.push({ Name: "onSelectNoneClick", Description: "Fires when select none is clicked" });
    }
}
