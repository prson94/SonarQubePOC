import { ChangeDetectionStrategy, Component, Input, ViewEncapsulation } from '@angular/core';
import { AssetTypeDetailField, AssetTypeDetailFieldType } from "../asset-type-detail-v2.model";

@Component({
    selector: 'ig-asset-type-detail-field',
    templateUrl: './asset-type-detail-field.component.html',
    providers: [],
    styles: [
        `.row-header { display: flex; align-items: center; }`,
        `.fa-copy { color: #8A46E4; margin-left: 6px; cursor: pointer; }`
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None
})


export class AssetTypeDetailFieldComponent {
    @Input() field: AssetTypeDetailField;
    
    get fieldType(): typeof AssetTypeDetailFieldType {
        return AssetTypeDetailFieldType;
    }
}
