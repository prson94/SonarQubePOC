import {
    ChangeDetectionStrategy,
    Component,
    Input,
    ViewEncapsulation
} from '@angular/core';
import { AssetTypeDetailCategory } from "../asset-type-detail-v2.model";

@Component({
    selector: 'ig-asset-type-detail-category',
    templateUrl: './asset-type-detail-category.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None
})


export class AssetTypeDetailCategoryComponent {
    @Input() category: AssetTypeDetailCategory;
}
