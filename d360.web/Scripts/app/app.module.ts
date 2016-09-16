import { NgModule }       from '@angular/core';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { routing }        from './app.routes';
import { HttpModule }     from '@angular/http';
import { CHART_DIRECTIVES } from 'angular2-highcharts'; 

import { PipesModule } from './pipes/pipes.module';
import { SearchModule } from './components/search/search.module';
import { WorkflowModule } from './components/workflow/workflow.module';
import { SharedModule } from './components/shared/shared.module';
import { SocialModule } from './components/social/social.module';
import { NavbarModule } from './components/navbar/navbar.module';
import { FusionModule } from './components/fusion/fusion.module';

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
    DataListModule,
    TreeModule,
    OverlayPanelModule,
} from 'primeng/primeng';

import {
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
    AdminRelationshipsEditor,
    AdminStatisticEditor,
    AdminSurveyQuestionEditorEditor,
    AdminTaxonomyDetailComponent,
    AdminTaxonomyEditorComponent,
    AdminTaxonomyLevelEditorComponent,
    AdminTemplateEditorComponent,
    AdminStatisticCheckTypeInput,
} from './components/admin/index';

import {
    ArtifactBaseComponent,
    ArtifactColumnFilterComponent,
    ArtifactComponent,
    ArtifactDefnintionComponent,
    ArtifactGridComponent,
    ArtifactItemComponent,
    ArtifactListComponent,
    ArtifactTopLevelListComponent,
} from './components/artifact/index';

import {
    CommunityComponent,
    CommunitySummaryComponent,
} from './components/community/index';

import {
    DiagnosticComponent,
    DiagnosticIncorrectTextpathComponent,    
} from './components/diagnostic/index';

import {
    ArtifactTypeForm,    
    FieldTypeForm,
    GroupForm,
    LoadForm,
    ResponsibilityItemForm,
    ResponsibilityTypeForm,
    WorkflowItemForm,
} from './components/forms/index';

import {
    HeaderActionsComponent,
    HeaderBreadcrumbComponent,
    HeaderBreadcrumbItemComponent,
    HeaderComponent,
    HeaderTypeaheadSearchComponent,
    HeaderFavoritesComponent,
    HeaderFollowComponent,
} from './components/header/index';

import {
    HomeComponent
} from './components/home/home.component';

import {
    ModelComponent,
    ModelItemComponent,
    ModelListComponent,
    ModelItemStructureComponent,
} from './components/model/index';

import {
    MonitorComponent,
    MonitorListComponent,
} from './components/monitor/index';

import {
    ActionBar,
    SimpleAccordion,
    ClaimsMatrixPart,    
    MenuPartItem,
    MenuPart,
    SimpleDropdown,
    ObjectDetailField,
} from './components/parts/index';

import {
    PolicyComponent,
    PolicyItemComponent,  
} from './components/policy/index';

import {
    ReferenceComponent,
    ReferenceListComponent,
} from './components/reference/index';

import {
    ResourceComponent,
    ResourceItemComponent,
    ResourceApiComponent,
    ResourceListComponent,
} from './components/resource/index';

import { RightSidebarComponent } from './components/rightsidebar/right-sidebar.component';
import { RightSidebarItemComponent } from './components/rightsidebar/right-sidebar-item.component';

import {
    RuleComponent,
    RuleItemComponent,
    RuleListComponent,
} from './components/rule/index';

import {
    AttributesTile,
    FusionAttributesTile,    
    ClaimsTile,
    FieldDefinitionTile,
    FusionConfigurationTile,
    FusionFiltersTile,
    GroupMembersTile,
    LoadItemTile,
    MenuBarItem,
    ModelLevelTile,
    ObjectDefinitionTile,
    ObjectDetailTile,
    ObjectGovernanceTile,
    ObjectRelationshipsTile,
    PeopleResponsibilitiesTile,
    PredicatesTile,
    RelationshipsTile,
    ReportItemsTile,
    ReportLayoutTile,
    RuleDimensionsTile,
    StructureTile,
    SurveyQuestionsTile,
    SynonymsTile,
    ActivityTile,
    AssignmentsTile,
    BoardTile,
    ActivityDetailsTile,
    ResourceFollowingTile,
    ResourceResponsibilityTile,
    ResourceFollowingGridTile,
    ResourceResponsibilityGridTile,
} from './components/tiles/index';

@NgModule({
    declarations: [
        ActionBar,
        AdminArtifactsComponent,
        AdminAttributeTypeEditor,
        AdminAttributesComponent,
        AdminComponent,
        AdminDashboardsComponent,
        AdminDashboardsEditor,
        AdminDomainComponent,
        AdminFusionComponent,
        AdminGovernanceComponent,
        AdminGroupsComponent,
        AdminLoadComponent,
        AdminLookupTypeEditorComponent,
        AdminLookupsComponent,
        AdminPoliciesComponent,
        AdminRelationshipsComponent,
        AdminRelationshipsEditor,
        AdminResourcesComponent,
        AdminRulesComponent,
        AdminSettingsComponent,
        AdminStatisticCheckTypeInput,
        AdminStatisticEditor,
        AdminStatisticsComponent,
        AdminSurveyQuestionEditorEditor,
        AdminSurveysComponent,
        AdminTaxonomiesComponent,
        AdminTaxonomyDetailComponent,
        AdminTaxonomyEditorComponent,
        AdminTaxonomyLevelEditorComponent,
        AdminTemplateEditorComponent,
        AdminTemplatesComponent,
        AdminWorkflowComponent,
        AppComponent,
        ArtifactColumnFilterComponent,
        ArtifactComponent,
        ArtifactDefnintionComponent,
        ArtifactGridComponent,
        ArtifactItemComponent,
        ArtifactListComponent,
        ArtifactTopLevelListComponent,
        ArtifactTypeForm,
        AttributesTile,     
        ClaimsMatrixPart,
        ClaimsTile,
        CommunityComponent,
        CommunitySummaryComponent,
        DiagnosticComponent,
        DiagnosticIncorrectTextpathComponent,
        FieldDefinitionTile,
        FieldTypeForm,
        FusionAttributesTile,        
        FusionConfigurationTile,
        FusionFiltersTile,        
        GroupForm,
        GroupMembersTile,
        HeaderActionsComponent,
        HeaderBreadcrumbComponent,
        HeaderBreadcrumbItemComponent,
        HeaderComponent,
        HeaderFavoritesComponent,
        HeaderFollowComponent,
        HeaderTypeaheadSearchComponent,
        HomeComponent,    
        LoadForm,
        LoadItemTile,
        MenuPart,    
        ModelComponent,
        ModelItemComponent,
        ModelLevelTile,
        ModelListComponent,
        ModelItemStructureComponent,
        MonitorComponent,
        MonitorListComponent,
        ObjectDefinitionTile,
        ObjectDetailField,
        ObjectDetailTile,      
        ObjectGovernanceTile,     
        ObjectRelationshipsTile,    
        PeopleResponsibilitiesTile,
        PolicyComponent,
        PolicyItemComponent,    
        PredicatesTile,
        ReferenceComponent,
        ReferenceListComponent,
        RelationshipsTile,
        ReportItemsTile,
        ReportLayoutTile,
        ResourceApiComponent,
        ResourceComponent,
        ResourceFollowingGridTile,
        ResourceFollowingTile,
        ResourceItemComponent,
        ResourceResponsibilityGridTile,
        ResourceResponsibilityTile,
        ResourceListComponent,
        ResponsibilityItemForm,
        ResponsibilityTypeForm,
        RightSidebarComponent,
        RightSidebarItemComponent,
        RuleComponent,
        RuleDimensionsTile,
        RuleItemComponent,
        RuleListComponent,
        SimpleAccordion,
        SimpleDropdown,    
        StructureTile,
        SurveyQuestionsTile,
        SynonymsTile,
        ActivityTile,
        AssignmentsTile,
        BoardTile,
        ActivityDetailsTile,     
        WorkflowItemForm,     
    ],
    imports: [
        BrowserModule,
        FormsModule,
        ReactiveFormsModule,
        routing,
        HttpModule,

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
        TreeModule,
        OverlayPanelModule,
        DataListModule,

        //d3s modules
        PipesModule,
        SearchModule,
        WorkflowModule,
        SharedModule,  
        SocialModule,   
        NavbarModule,   
        FusionModule,
    ],
    bootstrap: [AppComponent],
    providers: [Title],    
})
export class AppModule { }







