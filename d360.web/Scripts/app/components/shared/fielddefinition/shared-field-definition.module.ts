import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { AutoCompleteModule } from 'primeng/autocomplete';
import { TableModule } from 'primeng/table';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { CalendarModule } from 'primeng/calendar';
import { CheckboxModule } from 'primeng/checkbox';
import { DropdownModule } from 'primeng/dropdown';
import { SharedModule } from 'primeng/api';
import { EditorModule } from 'primeng/editor';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from "primeng/tooltip";


import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedFormMessageModule } from '../form-message.part';
import { SimpleAccordionModule } from '../simple-accordion.part';

import { FieldTypeForm } from './field-type-form/field-type.form';
import { FieldDefinitionComponent } from './field-definition.component';
import { RadioButtonModule } from 'primeng/radiobutton';
import { SidePanelModule } from '../sidepanel/side-panel.module';
import { AssetPreviewModule } from '../asset-preview/asset-preview.module';
import { AngularSplitModule } from 'angular-split';
import { SearchFieldModule } from '../controls/search-field/search-field.component';
import { AdvancedFiltersModule } from '../../assets-grid/advanced-filtering/advanced-filtering.module';
import { PopupMenuModule } from '../controls/popup-menu/popup-menu.component';
import { FieldTypeDetailModule } from './field-type-details/field-type-details.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        //d3s
        CoreModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        TilesModule,
        SimpleAccordionModule,   

        //prime
        AutoCompleteModule,
        CalendarModule,
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        InputTextModule,
        InputTextareaModule,
        EditorModule,
        MultiSelectModule,
        SharedModule,
        TableModule,
        TooltipModule,
		RadioButtonModule,
		SidePanelModule,
		AngularSplitModule,
		SearchFieldModule,
		AdvancedFiltersModule,
		PopupMenuModule,
		FieldTypeDetailModule
    ],
    declarations: [
        FieldTypeForm,
        FieldDefinitionComponent
    ],
    exports: [
        FieldDefinitionComponent
    ],
    providers: [

    ]
})
export class SharedFieldDefinitionModule { }
