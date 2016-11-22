import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import {
    GrowlModule,
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
    AutoCompleteModule,
    MultiSelectModule,    
    EditorModule,
    TooltipModule,    
    PaginatorModule,
    SharedModule,
    DialogModule,    
    TreeModule,    
    OverlayPanelModule,
} from 'primeng/primeng';

import { ChartModule } from 'angular2-highcharts';

import { SocialModule } from '../social/social.module';
import { PipesModule } from '../../pipes/pipes.module';
import { CoreModule } from './core.module';

import { ColorPickerModule } from 'angular2-color-picker';

import { ActionBar } from './action-bar.part';
import { ArtifactStatusComponent } from './artifact-status.component';
import { AssignmentsTile } from './assignments-tile.component';
import { AttributesTile } from './attributes.tile';
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
import { FieldTypeForm } from './field-type.form';
import { FollowerGridComponent } from './follower-grid.component';
import { FormMessagePart } from './form-message.part';
import { FusionAttributeItemDetailsComponent } from './fusion-attribute-item-details.component';
import { FusionFiltersComponent } from './fusion-filters.component';
import { GridPagingInfoComponent } from './grid-paging-info.component';
import { GroupMembersComponent } from './group-members.component';
import { HeaderActionsComponent } from './header/header-actions.component';
import { HeaderBreadcrumbItemComponent } from './header/header-breadcrumb-item.component';
import { HeaderBreadcrumbComponent } from './header/header-breadcrumb.component';
import { HeaderTypeaheadSearchComponent } from './header/header-typeahead-search.component';
import { HeaderFavoritesComponent } from './header/header-favorites.component';
import { HeaderFollowComponent } from './header/header-follow.component';
import { HeaderComponent } from './header/header.component';
import { ImpactComponent } from './impact.component';
import { LineageComponent } from './lineage/lineage.component';
import { LineageFusionComponent } from './lineage/lineage-fusion.component';
import { LineageMappingRulesComponent } from './lineage/lineage-mapping-rules.component';
import { LineageObjectDetailComponent } from './lineage/lineage-object-detail.component';
import { LineageRelationshipsComponent } from './lineage/lineage-relationships.component';
import { LineageResponsibilitiesComponent } from './lineage/lineage-responsibilities.component';
import { LineageSourceRuleEditorComponent } from './lineage/lineage-source-rule-editor.component';
import { LineageSourceRulesComponent } from './lineage/lineage-source-rules.component';
import { LineageTechnicalRelationshipsComponent } from './lineage/lineage-technical-relationships.component';
import { MenuPart } from './menu.part';
import { MessagesComponent } from './messages.component';
import { MessagesBarComponent } from './messages-bar.component';
import { ModelDiagramComponent } from './model-diagram.component';
import { MultiSelectGridComponent } from './multiselect-grid.component';
import { ObjectBoardComponent } from './object-board.component';
import { ObjectDefinitionTile } from './object-definition.tile';
import { ObjectDetailComponent } from './object-detail.component';
import { ObjectDetailField } from './object-detail-field.part';
import { ObjectFollowersComponent } from './object-followers.component';
import { ObjectGovernanceComponent } from './object-governance.component';
import { ObjectHealthComponent } from './object-health.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { ObjectIssuesComponent } from './object-issues.component';
import { ObjectRelationshipsComponent } from './object-relationships.component';
import { OverlayWindowComponent } from './overlay-window.component';
import { PeopleResponsibilitiesTile } from './people-responsibilities.tile';
import { PowerBIViewerComponent } from './powerbi-viewer.component';
import { PredicatesListComponent } from './predicates-list.component';
import { RaiseIssueButtonComponent } from './raise-issue-button.component';
import { RelationshipTechnicalRelationsComponent } from './relationship-technical-relations.component';
import { RightSidebarComponent } from './rightsidebar/right-sidebar.component';
import { RightSidebarItemComponent } from './rightsidebar/right-sidebar-item.component';
import { ResourceResponsibilityComponent } from './resource-responsibility.component';
import { ResourceResponsibilityGridComponent } from './resource-responsibility-grid.component';
import { ResponsibilityItemForm } from './responsibility-item.form';
import { SimpleAccordion } from './simple-accordion.part';
import { SiteMenuComponent } from './menu/site-menu.component';
import { SiteMenuCategoryComponent } from './menu/site-menu-category.component';
import { SiteMenuMegaItemComponent } from './menu/site-menu-mega-item.component';
import { StructureTile } from './structure.tile';
import { SynonymsTile } from './synonyms.tile';
import { TileActionsComponent } from './tile-actions.component';
import { TakeSurveyComponent } from './take-survey.component';
import { UserListComponent } from './user-list.component';
import { WorkflowDetailedViewComponent } from './workflow-detailed-view.component';
import { WorkflowIssueDetailsComponent } from './workflow-issue-details.component';
import { WorkflowIssueEditorComponent } from './workflow-issue-editor.component';


@NgModule({
    declarations: [
        ActionBar,
        ArtifactStatusComponent,
        AssignmentsTile,
        AttributesTile,
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
        FieldTypeForm,
        FollowerGridComponent,
        FormMessagePart,
        FusionAttributeItemDetailsComponent,        
        FusionFiltersComponent,
        GridPagingInfoComponent,
        GroupMembersComponent,
        HeaderActionsComponent,
        HeaderBreadcrumbItemComponent,
        HeaderBreadcrumbComponent,
        HeaderTypeaheadSearchComponent,
        HeaderFavoritesComponent,
        HeaderFollowComponent,
        HeaderComponent,
        ImpactComponent,        
        LineageComponent,
        LineageFusionComponent,
        LineageMappingRulesComponent,
        LineageObjectDetailComponent,
        LineageRelationshipsComponent,
        LineageResponsibilitiesComponent,
        LineageSourceRuleEditorComponent,
        LineageSourceRulesComponent,
        LineageTechnicalRelationshipsComponent,
        MenuPart,        
        MessagesBarComponent,        
        MessagesComponent, 
        ModelDiagramComponent, 
        MultiSelectGridComponent,      
        ObjectBoardComponent,        
        ObjectDefinitionTile,
        ObjectDetailComponent,
        ObjectDetailField,
        ObjectFollowersComponent,
        ObjectGovernanceComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,       
        ObjectRelationshipsComponent, 
        OverlayWindowComponent,        
        PeopleResponsibilitiesTile,
        PowerBIViewerComponent,
        PredicatesListComponent,
        RaiseIssueButtonComponent,
        RelationshipTechnicalRelationsComponent,        
        ResourceResponsibilityComponent,
        ResponsibilityItemForm,
        ResourceResponsibilityGridComponent,        
        RightSidebarComponent,
        RightSidebarItemComponent,
        SimpleAccordion,
        SiteMenuCategoryComponent,
        SiteMenuComponent,    
        SiteMenuMegaItemComponent,    
        StructureTile,
        SynonymsTile,
        TakeSurveyComponent,
        TileActionsComponent,           
        UserListComponent,
        WorkflowDetailedViewComponent,
        WorkflowIssueDetailsComponent,
        WorkflowIssueEditorComponent,
    ],
    exports: [
        ActionBar,
        ArtifactStatusComponent,
        AssignmentsTile,
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
        FieldTypeForm,
        FollowerGridComponent, 
        FusionAttributeItemDetailsComponent,      
        FusionFiltersComponent, 
        GridPagingInfoComponent,
        GroupMembersComponent,
        HeaderActionsComponent,
        HeaderBreadcrumbItemComponent,
        HeaderBreadcrumbComponent,
        HeaderTypeaheadSearchComponent,
        HeaderFavoritesComponent,
        HeaderFollowComponent,
        HeaderComponent,
        ImpactComponent,        
        LineageComponent,        
        MessagesBarComponent,        
        MessagesComponent,
        MenuPart,  
        MultiSelectGridComponent,
        ModelDiagramComponent,
        ObjectBoardComponent,        
        ObjectDefinitionTile,
        ObjectDetailComponent,
        ObjectDetailField,
        ObjectFollowersComponent,
        ObjectGovernanceComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,        
        ObjectRelationshipsComponent,
        OverlayWindowComponent,        
        PeopleResponsibilitiesTile,
        PowerBIViewerComponent,
        PredicatesListComponent,
        RaiseIssueButtonComponent,
        RelationshipTechnicalRelationsComponent,        
        ResourceResponsibilityComponent,
        ResourceResponsibilityGridComponent,        
        ResponsibilityItemForm,
        RightSidebarComponent,
        RightSidebarItemComponent,
        SimpleAccordion,
        SiteMenuCategoryComponent,
        SiteMenuComponent,
        TakeSurveyComponent,
        TileActionsComponent,       
        UserListComponent, 
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
        EditorModule,
        TooltipModule,        
        PaginatorModule, 
        SharedModule,  
        DialogModule,        
        TreeModule,        
        OverlayPanelModule,

        //highcharts
        ChartModule,

        ColorPickerModule,

        //d3s
        PipesModule,                    
        SocialModule,
        CoreModule,
    ]

})

export class D3SSharedModule { }