import { Input, Component } from '@angular/core';
import { Category } from '../../../models/object-detail.model';

@Component({
    selector: 'ig-asset-detail-category',
    templateUrl: './asset-detail-category.component.html'
})

export class AssetDetailCategoryComponent {
    @Input() category: Category;
    @Input() assetUID: string;
    @Input() tooltipAlign: string;
    @Input() spacerHeight: string = '32px';
}
