import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-autocomplete',
    templateUrl: './gallery.autocomplete.component.html',
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

export class GalleryAutocompleteComponent implements OnInit {
    properties: Array<any>;

    brands: string[] = ['Audi', 'BMW','BMW M3','BMW T4', 'Fiat', 'Ford', 'Honda', 'Jaguar', 'Mercedes', 'Renault', 'Volvo', 'VW'];
    filteredBrands: string[] = [];

    sampleUsage: string = `<p-autoComplete igAutocomplete 
                        placeholder="Placeholder text"
                        [(ngModel)]="value"
                        (completeMethod)="filterItems($event)"
                        [suggestions]="filteredBrands"><p-autoComplete>`;

    value: any;

    constructor(private cdRef: ChangeDetectorRef) {}

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "label", Type: "string", Description: "Text of the button. Buttons without a label must always provide a tooltip.", Default: "" });
        this.properties.push({ Name: "icon", Type: "string", Description: "Name of the icon.", Default: "" });
        this.properties.push({ Name: "tooltip", Type: "string", Description: "Tooltip for button. Must be provided if there is no label. Will also be used as ARIA label.", Default: "" });
        this.properties.push({ Name: "loading", Type: "boolean", Description: "When present, it specifies that the component should be in loading state. When loading, the button is also disabled.", Default: "false" });
    }
    filterItems($event) {
        this.filteredBrands = [];
        this.brands.forEach(brand => {
            if (brand.toLowerCase().indexOf($event.query.toLowerCase()) == 0) {
                this.filteredBrands.push(brand);
            }
        });
        this.filteredBrands = JSON.parse(JSON.stringify(this.filteredBrands));
        this.cdRef.markForCheck();
    }
}
