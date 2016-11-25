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
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';

import {    
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
    MultiSelectModule,    
    EditorModule,
    TooltipModule,        
    TreeModule,
    GrowlModule,
    SharedModule,
} from 'primeng/primeng';

import { AdminAttributeAllocationComponent } from './admin-attribute-allocation.component';
import { AdminGovernanceComponent } from './admin-governance.component';
import { AdminSettingsComponent } from './admin-settings.component';
import { AdminGroupsComponent } from './admin-groups.component';
import { AdminWorkflowComponent } from './admin-workflow.component';
import { AdminArtifactsComponent } from './admin-artifacts.component';
import { AdminTemplatesComponent } from './admin-templates.component';
import { AdminTaxonomiesComponent } from './admin-taxonomies.component';
import { AdminLookupsComponent } from './admin-lookups.component';
import { AdminRulesComponent } from './admin-rules.component';
import { AdminPoliciesComponent } from './admin-policies.component';
import { AdminAttributesComponent } from './admin-attributes.component';
import { AdminRelationshipsComponent } from './admin-relationships.component';
import { AdminResourcesComponent } from './admin-resources.component';
import { AdminStatisticsComponent } from './admin-statistics.component';
import { AdminDashboardsComponent } from './admin-dashboards.component';
import { AdminLoadComponent } from './admin-load.component';
import { AdminFusionComponent } from './admin-fusion.component';
import { AdminSurveysComponent } from './admin-surveys.component';
import { AdminComponent } from './admin.component';
import { AdminAttributeTypeEditor } from './admin-attribute-type-editor.component';
import { AdminDashboardsEditor } from './admin-dashboards-editor.component';
import { AdminLookupTypeEditorComponent } from './admin-lookup-type-editor.component';
import { AdminRelationshipsEditor } from './admin-relationships-editor.component';
import { AdminReportItemsComponent } from './admin-report-items.component';
import { AdminStatisticEditor } from './admin-statistics-editor.component';
import { AdminSurveyQuestionEditorEditor } from './admin-survey-question-editor.component';
import { AdminTaxonomyEditorComponent } from './admin-taxonomy-editor.component';
import { AdminTaxonomyDetailComponent } from './admin-taxonomy-detail.component';
import { AdminLevelEditorComponent } from './admin-level-editor.component';
import { AdminTemplateEditorComponent } from './admin-template-editor';
import { AdminStatisticCheckTypeInput } from './admin-statistic-checktype-input';
import { AdminSurveyQuestionsComponent } from './admin-survey-questions.component';
import { AdminRuleDimensionsComponent } from './admin-rule-dimensions.component';
import { AdminRelationshipsListComponent } from './admin-relationships-list.component';
import { AdminLevelListComponent } from './admin-level-list.component';
import { AdminModelClassificationComponent } from './admin-model-classification.component';
import { AdminRelationshipRolesComponent } from './admin-relationship-roles.component';
import { AdminReportTileEditorComponent } from './admin-report-tile-editor.component';
import { ArtifactTypeForm } from './artifact-type.form';
import { BulkLoadItemComponent } from './bulk-load-item.component';
import { ClaimsTile } from './claims.tile';
import { ClaimsMatrixPart } from './claims-matrix.part';
import { FusionConfigurationTile } from './fusion-configuration.tile';
import { FusionAttributesTile } from './fusion-attributes.tile';
import { GroupForm } from './group.form';
import { IconPickerComponent } from './icon-picker.component';
import { LoadForm } from './load.form';
import { PredicatesListComponent } from './predicates-list.component';
import { ResponsibilityTypeForm } from './responsibility-type.form';
import { WorkflowItemForm } from './workflow-item.form';



@NgModule({
    declarations: [
        AdminAttributeAllocationComponent,
        AdminArtifactsComponent,
        AdminComponent,
        AdminAttributesComponent,
        AdminDashboardsComponent,        
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
        ArtifactTypeForm,
        BulkLoadItemComponent,
        ClaimsTile,
        ClaimsMatrixPart,
        FusionAttributesTile,
        FusionConfigurationTile,
        GroupForm,
        IconPickerComponent,
        LoadForm,
        PredicatesListComponent,
        ResponsibilityTypeForm,
        WorkflowItemForm,

        AceEditorComponent,
    ]
    , imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,
        AdminRoutingModule,
        
        //primeng        
        InputTextareaModule,
        InputTextModule,        
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
        MultiSelectModule,        
        EditorModule,
        TooltipModule,                
        TreeModule,                
        SharedModule,
        GrowlModule,

        //color picker
        ColorPickerModule,

        //d3s
        D3SSharedModule,        
        CoreModule,
        PipesModule, 
        TilesModule,       
    ] 

})

export class AdminModule { }