import { Input, Component } from '@angular/core';
import { Category } from '../../../models/object-detail.model';

@Component({
    selector: 'object-detail-category',
    templateUrl: './object-detail-category.component.html'
})

export class ObjectDetailCategoryComponent {
    @Input() category: Category;
    @Input() assetUID: string;
}
