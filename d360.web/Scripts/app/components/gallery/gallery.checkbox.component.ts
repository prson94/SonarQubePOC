import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-checkbox',
    templateUrl: './gallery.checkbox.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        .row{
            padding: 8px
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryCheckboxComponent implements OnInit {
    protected properties: Array<any>;
    private filteredBrands: string[] = [];
    sampleUsage: string = `<p-checkbox igCheckbox [(ngModel)]="val" label="Checkbox"></p-checkbox>`;
    sampleUsage2: string = ` <p-triStateCheckbox igCheckbox [(ngModel)]="tristateval" label="Tri state checkbox"></p-triStateCheckbox>`;

    val: boolean = false;
    tristateval: any;

    constructor(private cdRef: ChangeDetectorRef) {}

    ngOnInit(): void {
        this.properties = new Array();
    }
}
