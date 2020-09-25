import { Component, OnInit, ChangeDetectionStrategy, HostListener } from '@angular/core';


@Component({
    selector: 'gallery-field-condition-grid',
    templateUrl: './gallery.field-condition-grid.component.html',
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

export class GalleryFieldConditionGridComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<ig-popup-menu [items]="items"></ig-popup-menu>';
    protected isLoading1: boolean = true;
    protected isLoading2: boolean = false;

    cleanJsonExamples: any = {};
    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "items", Type: "Array<PopupMenuItem>", Description: "Array of menu items", Default: "Empty []" });
        
    }
}
