import { Input, Component } from '@angular/core';
import { Category } from '../../../models/object-detail.model';
import { CompanySettingEnum } from '../../../models/settings.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'ig-asset-detail-category',
    templateUrl: './asset-detail-category.component.html',
    styles: [`.category-column { display:inline-grid; margin-right:40px;max-height: 300px; overflow-x: hidden;padding-bottom: 4px; }
              .category-column .ig-label { word-break: break-word; }`]
})

export class AssetDetailCategoryComponent {
    @Input() category: Category;
    @Input() assetUID: string;
    @Input() tooltipAlign: string;
    @Input() spacerHeight: string = '32px';
    @Input() isSidePanel: boolean = false;

    constructor(
        protected settingsService: CompanySettingsService
    ) {}

    getRowClass(data: any[]): string {
        if (this.showInColumn(data)) {
            return 'category-column';
        }
        return '';
    }

    getColumnWidth(data: any[]): string {
        if (this.showInColumn(data)) {
            let columnWidth = this.settingsService.getSettingById(CompanySettingEnum.AssetDefinitionColumnWidth).NumberSetting.Value;
            return (columnWidth).toString();
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
