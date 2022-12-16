import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { EditorModule } from 'primeng/editor';
import { TreeTableModule } from 'primeng/treetable';
import { DirectivesModule } from '../../../../directives/directives.module';
import { FormFeedbackBadgesModule } from '../../../shared/controls/form-feedback-badges/form-feedback-badges.component';
import { PropertyGroupModule } from '../../../shared/controls/property-group/property-group.component';
import { SiteModalModule } from '../../../shared/modal/gov-modal.module';
import { ConfigurationAssetTypeModalForm } from './asset-type-modal-form.component';

@NgModule({
    imports: [
        CommonModule,
		FormsModule,
		ReactiveFormsModule,
        TreeTableModule,
        EditorModule,
        DropdownModule,
		ButtonModule,
		DirectivesModule,

		SiteModalModule,
		PropertyGroupModule,
		FormFeedbackBadgesModule
    ],
	declarations: [
		ConfigurationAssetTypeModalForm
    ],
	exports: [
		ConfigurationAssetTypeModalForm
	],
})
export class AssetTypeModalFormModule { }
