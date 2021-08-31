import { Input, Component } from '@angular/core';
import { Category } from '../../../models/object-detail.model';

@Component({
    selector: 'ig-asset-detail-category',
    templateUrl: './asset-detail-category.component.html',
    styles: [`.category-column { float:left; margin-right:40px; }`]
})

export class AssetDetailCategoryComponent {
    @Input() category: Category;
    @Input() assetUID: string;
    @Input() tooltipAlign: string;
    @Input() spacerHeight: string = '32px';
    @Input() isSidePanel: boolean = false;

    columnWidth: 200;
}
