import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import {
    GrowlModule,
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
    DragDropModule,
    PaginatorModule,
    SharedModule,
    DialogModule,
    TabViewModule,
    SliderModule,
} from 'primeng/primeng';

import { ChartModule } from 'angular2-highcharts';

import { SocialModule } from '../social/social.module';
import { PipesModule } from '../../pipes/pipes.module';
import { CoreModule } from './core.module';
import { D3SFormsModule } from '../forms/d3sforms.module';

import { ColorPickerModule } from 'angular2-color-picker';

import { ArtifactStatusComponent } from './artifact-status.component';
import { AuditComponent } from './audit.component';
import { DashboardTabComponent } from './dashboard-tab.component';
import { DeleteForm } from './delete.form';
import { DynamicFieldComponent } from './dynamic-field.component';
import { DynamicGridComponent } from './dynamic-grid.component';
import { DynamicEditorComponent } from './dynamic-editor.component';
import { DynamicFieldValueComponent } from './dynamic-field-value.component';
import { DynamicLookupGridComponent } from './dynamic-lookup-grid.component';
import { DynamicRelationshipGridComponent } from './dynamic-relationship-grid.component';
import { FieldDefinitionComponent } from './field-definition.component';
import { FollowerGridComponent } from './follower-grid.component';
import { FusionAttributeItemDetailsComponent } from './fusion-attribute-item-details.component';
import { FusionFiltersComponent } from './fusion-filters.component';
import { GridPagingInfoComponent } from './grid-paging-info.component';
import { GroupMembersComponent } from './group-members.component';
import { ImpactComponent } from './impact.component';
import { IconPickerComponent } from './icon-picker.component';
import { LineageComponent } from './lineage/lineage.component';
import { LineageFusionComponent } from './lineage/lineage-fusion.component';
import { LineageMappingRulesComponent } from './lineage/lineage-mapping-rules.component';
import { LineageObjectDetailComponent } from './lineage/lineage-object-detail.component';
import { LineageResponsibilitiesComponent } from './lineage/lineage-responsibilities.component';
import { LineageSourceRuleEditorComponent } from './lineage/lineage-source-rule-editor.component';
import { LineageSourceRulesComponent } from './lineage/lineage-source-rules.component';
import { LineageTechnicalRelationshipsComponent } from './lineage/lineage-technical-relationships.component';
import { MessagesComponent } from './messages.component';
import { MessagesBarComponent } from './messages-bar.component';
import { MultiSelectGridComponent } from './multiselect-grid.component';
import { ObjectBoardComponent } from './object-board.component';
import { ObjectChallengeComponent } from './object-challenge.component';
import { ObjectDetailComponent } from './object-detail.component';
import { ObjectDetailField } from './object-detail-field.part';
import { ObjectFollowersComponent } from './object-followers.component';
import { ObjectGovernanceComponent } from './object-governance.component';
import { ObjectHealthComponent } from './object-health.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { ObjectIssuesComponent } from './object-issues.component';
import { ObjectRelationshipsComponent } from './object-relationships.component';
import { PageLinksComponent } from './page-links.component';
import { PeopleResponsibilitiesTile } from './people-responsibilities.tile';
import { PowerBIViewerComponent } from './powerbi-viewer.component';
import { PredicatesListComponent } from './predicates-list.component';
import { RaiseIssueButtonComponent } from './raise-issue-button.component';
import { RelationshipTechnicalRelationsComponent } from './relationship-technical-relations.component';
import { ResourceResponsibilityComponent } from './resource-responsibility.component';
import { ResourceResponsibilityGridComponent } from './resource-responsibility-grid.component';
import { SimpleAccordion } from './simple-accordion.part';
import { SiteMenuComponent } from './menu/site-menu.component';
import { SiteMenuCategoryComponent } from './menu/site-menu-category.component';
import { SiteMenuMegaItemComponent } from './menu/site-menu-mega-item.component';
import { TileActionsComponent } from './tile-actions.component';
import { TakeSurveyComponent } from './take-survey.component';
import { WorkflowDetailedViewComponent } from './workflow-detailed-view.component';
import { WorkflowIssueDetailsComponent } from './workflow-issue-details.component';
import { WorkflowIssueEditorComponent } from './workflow-issue-editor.component';

@NgModule({
    declarations: [
        ArtifactStatusComponent,
        AuditComponent,
        DashboardTabComponent,
        DeleteForm,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicFieldValueComponent,
        DynamicGridComponent,
        DynamicLookupGridComponent,
        DynamicRelationshipGridComponent,
        DashboardTabComponent,
        FieldDefinitionComponent,
        FollowerGridComponent,
        FusionAttributeItemDetailsComponent,        
        FusionFiltersComponent,
        GridPagingInfoComponent,
        GroupMembersComponent,
        ImpactComponent,
        IconPickerComponent,
        LineageComponent,
        LineageFusionComponent,
        LineageMappingRulesComponent,
        LineageObjectDetailComponent,
        LineageResponsibilitiesComponent,
        LineageSourceRuleEditorComponent,
        LineageSourceRulesComponent,
        LineageTechnicalRelationshipsComponent,
        MessagesBarComponent,        
        MessagesComponent,  
        MultiSelectGridComponent,      
        ObjectBoardComponent,
        ObjectChallengeComponent,
        ObjectDetailComponent,
        ObjectDetailField,
        ObjectFollowersComponent,
        ObjectGovernanceComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,       
        ObjectRelationshipsComponent, 
        PageLinksComponent,
        PeopleResponsibilitiesTile,
        PowerBIViewerComponent,
        PredicatesListComponent,
        RaiseIssueButtonComponent,
        RelationshipTechnicalRelationsComponent,
        ResourceResponsibilityComponent,
        ResourceResponsibilityGridComponent,        
        SimpleAccordion,
        SiteMenuCategoryComponent,
        SiteMenuComponent,    
        SiteMenuMegaItemComponent,    
        TakeSurveyComponent,
        TileActionsComponent,        
        WorkflowDetailedViewComponent,
        WorkflowIssueDetailsComponent,
        WorkflowIssueEditorComponent,
    ],
    exports: [
        ArtifactStatusComponent,
        AuditComponent,
        DashboardTabComponent,
        DeleteForm,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicFieldValueComponent,
        DynamicGridComponent,
        DynamicLookupGridComponent,
        DynamicRelationshipGridComponent,
        DashboardTabComponent,
        FieldDefinitionComponent,
        FollowerGridComponent, 
        FusionAttributeItemDetailsComponent,      
        FusionFiltersComponent, 
        GridPagingInfoComponent,
        GroupMembersComponent,
        ImpactComponent,
        IconPickerComponent,
        LineageComponent,
        MessagesBarComponent,        
        MessagesComponent,
        MultiSelectGridComponent,
        ObjectBoardComponent,
        ObjectChallengeComponent,
        ObjectDetailComponent,
        ObjectDetailField,
        ObjectFollowersComponent,
        ObjectGovernanceComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,        
        ObjectRelationshipsComponent,
        PageLinksComponent,
        PeopleResponsibilitiesTile,
        PowerBIViewerComponent,
        PredicatesListComponent,
        RaiseIssueButtonComponent,
        RelationshipTechnicalRelationsComponent,
        ResourceResponsibilityComponent,
        ResourceResponsibilityGridComponent,        
        SimpleAccordion,
        SiteMenuCategoryComponent,
        SiteMenuComponent,
        TakeSurveyComponent,
        TileActionsComponent,   
        WorkflowDetailedViewComponent,     
        WorkflowIssueDetailsComponent,
        WorkflowIssueEditorComponent,
        ]
    , imports: [
        CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,
        ReactiveFormsModule,

        //primeng
        GrowlModule,
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
        DragDropModule,
        PaginatorModule, 
        SharedModule,  
        DialogModule,
        TabViewModule,
        SliderModule,

        //highcharts
        ChartModule,

        ColorPickerModule,

        //d3s
        PipesModule,             
        D3SFormsModule,   
        SocialModule,
        CoreModule,
    ]

})

export class D3SSharedModule { }