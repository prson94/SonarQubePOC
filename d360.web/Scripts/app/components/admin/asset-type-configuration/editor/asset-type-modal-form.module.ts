import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DropdownModule } from 'primeng/dropdown';
import { EditorModule } from 'primeng/editor';
import { TreeTableModule } from 'primeng/treetable';
import { DirectivesModule } from '../../../../directives/directives.module';
import { IgColorPickerModule } from '../../../shared/controls/color-picker/color-picker.module';
import { ColorSelectorModule } from '../../../shared/controls/color-selector/color-selector.component';
import { FormFeedbackBadgesModule } from '../../../shared/controls/form-feedback-badges/form-feedback-badges.component';
import { IconPickerModule } from '../../../shared/controls/icon-picker/icon-picker.component';
import { PopupMenuModule } from '../../../shared/controls/popup-menu/popup-menu.component';
import { PropertyGroupModule } from '../../../shared/controls/property-group/property-group.component';
import { SwitchModule } from '../../../shared/controls/switch/switch';
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
		FormFeedbackBadgesModule,
		PopupMenuModule,
		SwitchModule,
		CheckboxModule,
		ColorSelectorModule,
		IgColorPickerModule,
		IconPickerModule
    ],
	declarations: [
		ConfigurationAssetTypeModalForm
    ],
	exports: [
		ConfigurationAssetTypeModalForm
	],
})
export class AssetTypeModalFormModule { }
