import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { ColorPickerModule } from 'angular2-color-picker';

import { AceEditorDirective, AceEditorComponent } from 'ng2-ace-editor';

import { AdminRoutingModule } from './admin.routes';
import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { D3SFormsModule } from '../forms/d3sforms.module';
import { PipesModule } from '../../pipes/pipes.module';

import {
    GrowlModule,
    InputTextareaModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,
    SpinnerModule,
    EditorModule,
    TooltipModule,    
    PaginatorModule,
    DataListModule,
    TreeModule,    
    SharedModule,
} from 'primeng/primeng';

import {
    AdminAttributeAllocationComponent,
    AdminArtifactsComponent,
    AdminComponent,
    AdminAttributesComponent,
    AdminDashboardsComponent,
    AdminDomainComponent,
    AdminFusionComponent,
    AdminGovernanceComponent,
    AdminGroupsComponent,
    AdminLoadComponent,
    AdminLookupsComponent,
    AdminPoliciesComponent,
    AdminRelationshipsComponent,
    AdminResourcesComponent,
    AdminRulesComponent,
    AdminSettingsComponent,
    AdminStatisticsComponent,
    AdminSurveysComponent,
    AdminTaxonomiesComponent,
    AdminTemplatesComponent,
    AdminWorkflowComponent,
    AdminAttributeTypeEditor,
    AdminDashboardsEditor,
    AdminLookupTypeEditorComponent,
    AdminLevelListComponent,
    AdminRelationshipsEditor,
    AdminRelationshipsListComponent,
    AdminStatisticEditor,
    AdminSurveyQuestionEditorEditor,
    AdminTaxonomyDetailComponent,
    AdminTaxonomyEditorComponent,
    AdminLevelEditorComponent,
    AdminTemplateEditorComponent,
    AdminStatisticCheckTypeInput,
    AdminReportItemsComponent,
    AdminReportTileEditorComponent,
    AdminRuleDimensionsComponent,
    AdminSurveyQuestionsComponent,
    AdminModelClassificationComponent,
    AdminRelationshipRolesComponent,
    ClaimsTile,
    ClaimsMatrixPart,
    FusionAttributesTile,
    FusionConfigurationTile,
} from './index';


@NgModule({
    declarations: [
        AdminAttributeAllocationComponent,
        AdminArtifactsComponent,
        AdminComponent,
        AdminAttributesComponent,
        AdminDashboardsComponent,
        AdminDomainComponent,
        AdminFusionComponent,
        AdminGovernanceComponent,
        AdminGroupsComponent,
        AdminLoadComponent,
        AdminLookupsComponent,
        AdminPoliciesComponent,
        AdminRelationshipsComponent,
        AdminResourcesComponent,
        AdminRulesComponent,
        AdminSettingsComponent,
        AdminStatisticsComponent,
        AdminSurveysComponent,
        AdminTaxonomiesComponent,
        AdminTemplatesComponent,
        AdminWorkflowComponent,
        AdminAttributeTypeEditor,
        AdminDashboardsEditor,
        AdminLookupTypeEditorComponent,
        AdminLevelListComponent,
        AdminRelationshipsEditor,
        AdminRelationshipsListComponent,
        AdminStatisticEditor,
        AdminSurveyQuestionEditorEditor,
        AdminTaxonomyDetailComponent,
        AdminTaxonomyEditorComponent,
        AdminLevelEditorComponent,
        AdminTemplateEditorComponent,
        AdminStatisticCheckTypeInput,
        AdminReportItemsComponent,
        AdminReportTileEditorComponent,
        AdminRuleDimensionsComponent,
        AdminSurveyQuestionsComponent,
        AdminModelClassificationComponent,
        AdminRelationshipRolesComponent,
        ClaimsTile,
        ClaimsMatrixPart,
        FusionAttributesTile,
        FusionConfigurationTile,

        AceEditorComponent,
    ]
    , imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,
        AdminRoutingModule,
        
        //primeng
        GrowlModule,
        InputTextareaModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        CalendarModule,
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,
        SpinnerModule,
        EditorModule,
        TooltipModule,        
        PaginatorModule,
        TreeModule,        
        DataListModule,
        SharedModule,


        //color picker
        ColorPickerModule,

        //d3s
        D3SSharedModule,
        D3SFormsModule,
        CoreModule,
        PipesModule,        
    ] 

})

export class AdminModule { }