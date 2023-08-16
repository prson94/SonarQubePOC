import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { PipesModule } from "../../../pipes/pipes.module";
import { CoreModule } from "../core.module";
import { AssetTypeDetailV2Component } from "./asset-type-detail-v2.component";
import { PropertyGroupModule } from "../controls/property-group/property-group.component";
import { AssetTypeDetailCategoryComponent } from "./asset-type-details-category/asset-type-detail-category.component";
import { AssetTypeDetailFieldComponent } from "./asset-type-details-field/asset-type-detail-field.component";
import { TooltipModule } from 'primeng/tooltip';
import { AssetTypeModalFormModule } from '../../admin/asset-type-configuration/editor/asset-type-modal-form.module';
import { SharedFieldDefinitionModule } from '../fielddefinition/shared-field-definition.module';
import { ReferenceItemsModule } from "../reference-items/reference-items.module";


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        CoreModule,
        PipesModule,
		PropertyGroupModule,
		TooltipModule,
		AssetTypeModalFormModule,
		SharedFieldDefinitionModule,
		ReferenceItemsModule
    ],
    declarations: [
        AssetTypeDetailCategoryComponent,
        AssetTypeDetailFieldComponent,
        AssetTypeDetailV2Component
    ],
    exports: [
        AssetTypeDetailV2Component
    ]
})
export class AssetTypeDetailV2Module { }