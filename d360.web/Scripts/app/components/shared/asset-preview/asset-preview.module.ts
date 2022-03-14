import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssetPreviewComponent } from './asset-preview.component';
import { AssetDetailModule } from '../asset-detail/asset-detail.module';
import { TaggedAssetDetailModule } from '../tagged-assets/tagged-assets-detail.module';
import { AssetTypeDetailModule } from '../asset-type-detail/asset-type-detail.module';

@NgModule({
    imports: [
        CommonModule,
        AssetDetailModule,
        TaggedAssetDetailModule,
        AssetTypeDetailModule
    ],
    declarations: [
        AssetPreviewComponent
    ],
    exports: [
        AssetPreviewComponent
    ],
    providers: [
        
    ]
})
export class AssetPreviewModule { }