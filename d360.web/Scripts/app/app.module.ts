import { NgModule }       from '@angular/core';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { routing }        from './app.routes';
import { HttpModule }     from '@angular/http';
import { CHART_DIRECTIVES } from 'angular2-highcharts'; 
import { PipesModule } from './pipes/pipes.module';

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
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,
    SpinnerModule,
    EditorModule,
    TooltipModule,    
    DragDropModule,
    PaginatorModule,
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
    DeleteForm,
    FieldTypeForm,
    GroupForm,
    LoadForm,
    ResponsibilityItemForm,
    ResponsibilityTypeForm,
    WorkflowItemForm,
} from './components/forms/index';

import {
    FusionComponent,
    FusionItemComponent,
    FusionListComponent, 
} from './components/fusion/index';

import {
    HeaderActionsComponent,
    HeaderBreadcrumbComponent,
    HeaderBreadcrumbItemComponent,
    HeaderComponent,
    HeaderTypeaheadSearchComponent,
    HeaderFavoritesComponent,
} from './components/header/index';

import {
    HomeComponent
} from './components/home/home.component';

import {
    ModelComponent,
    ModelItemComponent,
    ModelListComponent,
} from './components/model/index';

import {
    MonitorComponent,
    MonitorListComponent,
} from './components/monitor/index';

import { NavBarComponent } from './components/navbar/navbar.component';
import { NavBarItemComponent } from './components/navbar/navbar-item.component';
import { NavBarMenuComponent } from './components/navbar/navbar-menu.component';

import {
    ActionBar,
    SimpleAccordion,
    ClaimsMatrixPart,
    FormMessagePart,
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
} from './components/resource/index';

import { RightSidebarComponent } from './components/rightsidebar/right-sidebar.component';
import { RightSidebarItemComponent } from './components/rightsidebar/right-sidebar-item.component';

import {
    RuleComponent,
    RuleItemComponent,
    RuleListComponent,
} from './components/rule/index';

import {
    AuditComponent,
    BaseComponent,
    DashboardTabComponent,
    DynamicEditorComponent,
    DynamicFieldComponent,
    DynamicGridComponent,
    DynamicRelationshipGridComponent,
    LineageComponent,
    MessagesComponent,
    ObjectBoardComponent,    
    ObjectChallengeComponent,
    ObjectHealthComponent,
    ObjectHealthDetailsComponent,
    ObjectIssuesComponent,    
    OwnershipTabComponent,
    PageLinksComponent,
    PowerBIViewerComponent,
    TooltipComponent,
    DynamicLookupGridComponent,
    TagInputComponent,
    ObjectFollowersComponent,
    FollowerGridComponent,
} from './components/shared/index';

import {
    SocialBoardComponent,
    SocialCommentComponent,
    SocialInputComponent,
} from './components/social/index';

import {
    AttributesTile,
    FusionAttributesTile,
    TileActionsComponent,
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


import {
    WorkflowIssueEditorComponent,
    WorkflowDetailComponent,
    WorkflowIssueDetailsComponent,
    WorkflowSuggestDetailsComponent,
    WorkflowCertifyDetailsComponent,
    WorkflowCertifyEditorComponent,
} from './components/workflow/index';

import {
    HomeSearchComponent,
    SearchResultsComponent,
    SearchResultItemComponent,
    SearchComponent,
} from './components/search/index';

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
        ArtifactTypeForm,
        AttributesTile,
        AuditComponent,
        CHART_DIRECTIVES,
        ClaimsMatrixPart,
        ClaimsTile,
        CommunityComponent,
        CommunitySummaryComponent,
        DashboardTabComponent,
        DeleteForm,
        DiagnosticComponent,
        DiagnosticIncorrectTextpathComponent,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicGridComponent,
        DynamicLookupGridComponent,
        TagInputComponent,
        DynamicRelationshipGridComponent,
        FieldDefinitionTile,
        FieldTypeForm,
        FollowerGridComponent,
        FormMessagePart,
        FusionAttributesTile,
        FusionComponent,
        FusionConfigurationTile,
        FusionFiltersTile,
        FusionItemComponent,
        FusionListComponent,
        GroupForm,
        GroupMembersTile,
        HeaderActionsComponent,
        HeaderBreadcrumbComponent,
        HeaderBreadcrumbItemComponent,
        HeaderComponent,
        HeaderFavoritesComponent,
        HeaderTypeaheadSearchComponent,
        HomeComponent,
        LineageComponent,
        LoadForm,
        LoadItemTile,
        MenuPart,
        MessagesComponent,
        ModelComponent,
        ModelItemComponent,
        ModelLevelTile,
        ModelListComponent,
        MonitorComponent,
        MonitorListComponent,
        NavBarComponent,
        NavBarItemComponent,
        NavBarMenuComponent,
        ObjectBoardComponent,        
        ObjectChallengeComponent,
        ObjectDefinitionTile,
        ObjectDetailField,
        ObjectDetailTile,
        ObjectFollowersComponent,
        ObjectGovernanceTile,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,        
        ObjectIssuesComponent,
        ObjectRelationshipsTile,
        OwnershipTabComponent,
        PageLinksComponent,
        PeopleResponsibilitiesTile,
        PolicyComponent,
        PolicyItemComponent,
        PowerBIViewerComponent,
        PredicatesTile,
        ReferenceComponent,
        ReferenceListComponent,
        RelationshipsTile,
        ReportItemsTile,
        ReportLayoutTile,
        ResourceComponent,
        ResourceFollowingGridTile,
        ResourceFollowingTile,
        ResourceItemComponent,
        ResourceResponsibilityGridTile,
        ResourceResponsibilityTile,
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
        SocialBoardComponent,
        SocialCommentComponent,
        SocialInputComponent,
        StructureTile,
        SurveyQuestionsTile,
        SynonymsTile,
        ActivityTile,
        AssignmentsTile,
        BoardTile,
        ActivityDetailsTile,
        TileActionsComponent,
        TooltipComponent,
        WorkflowIssueEditorComponent,
        WorkflowDetailComponent,
        WorkflowIssueDetailsComponent,
        WorkflowSuggestDetailsComponent,
        WorkflowCertifyDetailsComponent,
        WorkflowCertifyEditorComponent,
        WorkflowItemForm,
        HomeSearchComponent,
        SearchResultsComponent,
        SearchResultItemComponent,
        SearchComponent,
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
        AccordionModule,
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,
        SpinnerModule,
        EditorModule,
        TooltipModule,        
        DragDropModule,
        PaginatorModule,

        //d3s modules
        PipesModule,
        

    ],
    bootstrap: [AppComponent],
    providers: [Title],    
})
export class AppModule { }







