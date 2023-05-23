import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RelationLookupFieldTypeEditorComponent } from './relation-lookup-field-type-editor.component';
import { DropdownModule } from 'primeng/dropdown';
import { DirectivesModule } from '../../../../../directives/directives.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CoreModule } from '../../../core.module';
import { TooltipModule } from 'primeng/tooltip';
import { PropertyGroupModule } from '../../../controls/property-group/property-group.component';
import { FormFeedbackBadgesModule } from '../../../controls/form-feedback-badges/form-feedback-badges.component';
import { PopupMenuModule } from '../../../controls/popup-menu/popup-menu.component';
import { CheckboxModule } from 'primeng/checkbox';
import { ColorSelectorModule } from '../../../controls/color-selector/color-selector.component';
import { SwitchModule } from '../../../controls/switch/switch';
import { SiteModalModule } from '../../../modal/gov-modal.module';
import { IgColorPickerModule } from '../../../controls/color-picker/color-picker.module';
import { IgDateModule } from '../../../controls/date/date';
import { RadioButtonModule } from 'primeng/radiobutton';
import { IconPickerModule } from '../../../controls/icon-picker/icon-picker.component';
import { IgNumberFieldModule } from '../../../controls/number-picker/number-input.component';
import { TableModule } from 'primeng/table';
import { SearchFieldModule } from '../../../controls/search-field/search-field.component';
import { InputModule } from '../../../../../directives/ig-input-directive';



@NgModule({
	declarations: [
		RelationLookupFieldTypeEditorComponent
	],
	imports: [
		CommonModule,
		DropdownModule,
		DirectivesModule,
		FormsModule,
		ReactiveFormsModule,
		DropdownModule,
		ButtonModule,
		DirectivesModule,
		CoreModule,
		TooltipModule,

		SiteModalModule,
		PropertyGroupModule,
		FormFeedbackBadgesModule,
		PopupMenuModule,
		SwitchModule,
		CheckboxModule,
		ColorSelectorModule,
		IgColorPickerModule,
		InputModule,
		IgDateModule,
		RadioButtonModule,
		IconPickerModule,
		IgNumberFieldModule,
		TableModule,
		SearchFieldModule
	],
	exports: [
		RelationLookupFieldTypeEditorComponent
	]
})
export class RelationLookupFieldTypeEditorModule { }
