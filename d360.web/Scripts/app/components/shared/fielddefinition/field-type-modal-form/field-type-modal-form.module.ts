import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DropdownModule } from 'primeng/dropdown';
import { EditorModule } from 'primeng/editor';
import { RadioButtonModule } from 'primeng/radiobutton';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { TreeTableModule } from 'primeng/treetable';
import { DirectivesModule } from '../../../../directives/directives.module';
import { AutocompleteDirective } from '../../../../directives/ig-autocomplete-directive';
import { IgColorPickerModule } from '../../../shared/controls/color-picker/color-picker.module';
import { ColorSelectorModule } from '../../../shared/controls/color-selector/color-selector.component';
import { FormFeedbackBadgesModule } from '../../../shared/controls/form-feedback-badges/form-feedback-badges.component';
import { IconPickerModule } from '../../../shared/controls/icon-picker/icon-picker.component';
import { IgNumberFieldModule } from '../../../shared/controls/number-picker/number-input.component';
import { PopupMenuModule } from '../../../shared/controls/popup-menu/popup-menu.component';
import { PropertyGroupModule } from '../../../shared/controls/property-group/property-group.component';
import { SwitchModule } from '../../../shared/controls/switch/switch';
import { CoreModule } from '../../../shared/core.module';
import { SiteModalModule } from '../../../shared/modal/gov-modal.module';
import { IgDateModule } from '../../controls/date/date';
import { SearchFieldModule } from '../../controls/search-field/search-field.component';
import { ConfigurationFieldTypeModalFormComponent } from './field-type-modal-form.component';
import { RelationLookupFieldTypeEditorModule } from './relation-lookup-field-type-editor/relation-lookup-field-type-editor.module';

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
		IgDateModule,
		RadioButtonModule,
		IconPickerModule,
		IgNumberFieldModule,
		TableModule,
		RelationLookupFieldTypeEditorModule,
		SearchFieldModule,
		AutoCompleteModule
    ],
	declarations: [
		ConfigurationFieldTypeModalFormComponent
    ],
	exports: [
		ConfigurationFieldTypeModalFormComponent
	],
})
export class FieldTypeModalFormModule { }
