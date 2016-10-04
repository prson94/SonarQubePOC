import { NgModule }       from '@angular/core';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { routing }        from './app.routes';
import { HttpModule }     from '@angular/http';

import { ChartModule } from 'angular2-highcharts';

import { PipesModule } from './pipes/pipes.module';
import { CoreModule } from './components/shared/core.module';
import { SearchModule } from './components/search/search.module';
import { WorkflowModule } from './components/workflow/workflow.module';
import { SharedModule } from './components/shared/shared.module';
import { SocialModule } from './components/social/social.module';
import { NavbarModule } from './components/navbar/navbar.module';
import { FusionModule } from './components/fusion/fusion.module';
import { GroupModule } from './components/group/group.module';
import { CommunityModule } from './components/community/community.module';
import { MonitorModule } from './components/monitor/monitor.module';

import { D3SFormsModule } from './components/forms/d3sforms.module'; // why are some forms in a separate module instead of by area?

import { AdminUserGuard } from './guards/admin-user.guard';
import { AuthenticationService } from './services/authentication.service';

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
    AdminReportItemsComponent,
    AdminReportLayoutComponent,
    AdminSurveyQuestionsComponent,
    AdminRuleDimensionsComponent,
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
    ArtifactTypeMetricsComponent,
    ArtifactTypeWorkflowStatusComponent,    
} from './components/artifact/index';

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
    ResourceGroupsComponent,
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
    LoadItemTile,
    MenuBarItem,
    ModelLevelTile,
    ObjectDefinitionTile,
    ObjectDetailTile,    
    PredicatesTile,
    RelationshipsTile,            
    StructureTile,    
    SynonymsTile,
    ActivityTile,
    AssignmentsTile,
    BoardTile,
    ActivityDetailsTile,
    ResourceFollowingTile,
    ResourceResponsibilityTile,
    ResourceFollowingGridTile,    
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
        AdminReportItemsComponent,
        AdminReportLayoutComponent,
        AdminSurveyQuestionsComponent,
        AdminRuleDimensionsComponent,
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
        ArtifactTypeMetricsComponent,
        ArtifactTypeWorkflowStatusComponent,
        AttributesTile,     
        ClaimsMatrixPart,
        ClaimsTile,        
        FieldDefinitionTile,
        FusionAttributesTile,        
        FusionConfigurationTile,                
        HeaderActionsComponent,
        HeaderBreadcrumbComponent,
        HeaderBreadcrumbItemComponent,
        HeaderComponent,
        HeaderFavoritesComponent,
        HeaderFollowComponent,
        HeaderTypeaheadSearchComponent,
        HomeComponent,         
        LoadItemTile,
        MenuPart,    
        ModelComponent,
        ModelItemComponent,
        ModelLevelTile,
        ModelListComponent,
        ModelItemStructureComponent,        
        ObjectDefinitionTile,
        ObjectDetailField,
        ObjectDetailTile,                               
        PolicyComponent,
        PolicyItemComponent,    
        PredicatesTile,
        ReferenceComponent,
        ReferenceListComponent,
        RelationshipsTile,                
        ResourceApiComponent,
        ResourceComponent,
        ResourceFollowingGridTile,
        ResourceFollowingTile,
        ResourceItemComponent,        
        ResourceResponsibilityTile,
        ResourceListComponent,     
        ResourceGroupsComponent,
        RightSidebarComponent,
        RightSidebarItemComponent,
        RuleComponent,        
        RuleItemComponent,
        RuleListComponent,
        SimpleAccordion,
        SimpleDropdown,    
        StructureTile,        
        SynonymsTile,
        ActivityTile,
        AssignmentsTile,
        BoardTile,
        ActivityDetailsTile,         
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

        ChartModule,

        //d3s modules
        PipesModule,
        SearchModule,
        WorkflowModule,
        SharedModule,  
        SocialModule,   
        NavbarModule,   
        FusionModule,
        D3SFormsModule,
        GroupModule,
        CommunityModule,
        CoreModule,
        MonitorModule,
      
    ],
    bootstrap: [AppComponent],
    providers: [Title, AdminUserGuard, AuthenticationService],    
})
export class AppModule { }







