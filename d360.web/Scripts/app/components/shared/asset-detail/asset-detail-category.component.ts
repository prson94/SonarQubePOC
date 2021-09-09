import { Input, Component } from '@angular/core';
import { Category } from '../../../models/object-detail.model';

declare var CompanySettings;

@Component({
    selector: 'ig-asset-detail-category',
    templateUrl: './asset-detail-category.component.html',
    styles: [`.category-column { display:inline-grid; margin-right:40px;max-height: 300px; overflow-x: hidden;padding-bottom: 4px; }`]
})

export class AssetDetailCategoryComponent {
    @Input() category: Category;
    @Input() assetUID: string;
    @Input() tooltipAlign: string;
    @Input() spacerHeight: string = '32px';
    @Input() isSidePanel: boolean = false;

    getRowClass(data: any[]): string {
        if (this.showInColumn(data)) {
            return 'category-column';
        }
        return '';
    }

    getColumnWidth(data: any[]): string {
        if (this.showInColumn(data)) {
            return (CompanySettings?.AssetDefinitionColumnWidth ?? 200).toString();
        }
        return 'unset';
    }

    showInColumn(data: any[]): boolean {
        if (!data || this.category.name === "System Fields") {
            return false;
        }
        if (data.length > 1) {
            return true;
        }
        return false;
    }
}
