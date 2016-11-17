import { NgModule }       from '@angular/core';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { routing }        from './app.routes';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { COMPILER_PROVIDERS } from '@angular/compiler';

import { ChartModule } from 'angular2-highcharts';

import { ColorPickerModule } from 'angular2-color-picker';

import { AceEditorDirective, AceEditorComponent } from 'ng2-ace-editor';

import { PipesModule } from './pipes/pipes.module';
import { CoreModule } from './components/shared/core.module';
import { SearchModule } from './components/search/search.module';
import { WorkflowModule } from './components/workflow/workflow.module';
import { D3SSharedModule } from './components/shared/shared.module';
import { SocialModule } from './components/social/social.module';
import { FusionModule } from './components/fusion/fusion.module';
import { GroupModule } from './components/group/group.module';
import { CommunityModule } from './components/community/community.module';
import { MonitorModule } from './components/monitor/monitor.module';
import { ReferenceModule } from './components/reference/reference.module';
import { PolicyModule } from './components/policy/policy.module';
import { HelpModule } from './components/help/help.module';

import { D3SFormsModule } from './components/forms/d3sforms.module'; // why are some forms in a separate module instead of by area?

import { AdminUserGuard } from './guards/admin-user.guard';

import { AuthenticationService } from './services/authentication.service';
import { MessagesService, HeaderBreadcrumbService, HeaderActionsService, RightSidebarService, WebAnalyticsService, StateService  } from './services/index';
import { DynamicTypeBuilder }     from './services/dynamic-type-builder';

import { AuthenticationConnectionBackend } from './authentication-connection-backend';

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
    DragDropModule,
    PaginatorModule,
    DataListModule,
    TreeModule,
    OverlayPanelModule,
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
} from './components/admin/index';

import {
    ArtifactBaseComponent,
    ArtifactColumnFilterComponent,    
    ArtifactComponent,
    ArtifactDefnintionComponent,
    ArtifactGridComponent,
    ArtifactItemChildGridComponent,
    ArtifactItemChildrenComponent,
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
    ClaimsMatrixPart,    
    MenuPartItem,
    MenuPart,    
} from './components/parts/index';

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
    RuleColumnFilterComponent,
    RuleItemComponent,
    RuleListComponent,
    RuleResultsGridComponent,
} from './components/rule/index';

import {
    AttributesTile,
    FusionAttributesTile,    
    ClaimsTile,    
    FusionConfigurationTile,            
    MenuBarItem,    
    ObjectDefinitionTile,                  
    StructureTile,    
    SynonymsTile,
    ActivityTile,
    AssignmentsTile,
    BoardTile,
    ActivityDetailsTile,
    ResourceFollowingTile,    
    ResourceFollowingGridTile,    
} from './components/tiles/index';

@NgModule({
    declarations: [
        ActionBar,
        AdminAttributeAllocationComponent,
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
        AdminLevelListComponent,
        AdminPoliciesComponent,
        AdminRelationshipsComponent,
        AdminRelationshipsEditor,
        AdminResourcesComponent,
        AdminRulesComponent,
        AdminSettingsComponent,
        AdminStatisticCheckTypeInput,
        AdminReportItemsComponent,        
        AdminReportTileEditorComponent,
        AdminSurveyQuestionsComponent,
        AdminModelClassificationComponent,
        AdminRelationshipRolesComponent,
        AdminRuleDimensionsComponent,
        AdminRelationshipsListComponent,
        AdminStatisticEditor,
        AdminStatisticsComponent,
        AdminSurveyQuestionEditorEditor,
        AdminSurveysComponent,
        AdminTaxonomiesComponent,
        AdminTaxonomyDetailComponent,
        AdminTaxonomyEditorComponent,
        AdminLevelEditorComponent,
        AdminTemplateEditorComponent,
        AdminTemplatesComponent,
        AdminWorkflowComponent,
        AppComponent,
        ArtifactColumnFilterComponent,        
        ArtifactComponent,
        ArtifactDefnintionComponent,
        ArtifactGridComponent,
        ArtifactItemChildGridComponent,
        ArtifactItemChildrenComponent,
        ArtifactItemComponent,
        ArtifactListComponent,
        ArtifactTopLevelListComponent,        
        ArtifactTypeMetricsComponent,
        ArtifactTypeWorkflowStatusComponent,
        AttributesTile,     
        ClaimsMatrixPart,
        ClaimsTile,           
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
        MenuPart,    
        ModelComponent,
        ModelItemComponent,        
        ModelListComponent,
        ModelItemStructureComponent,        
        ObjectDefinitionTile,                                                                           
        ResourceApiComponent,
        ResourceComponent,
        ResourceFollowingGridTile,
        ResourceFollowingTile,
        ResourceItemComponent,                
        ResourceListComponent,     
        ResourceGroupsComponent,
        RightSidebarComponent,
        RightSidebarItemComponent,
        RuleColumnFilterComponent,
        RuleComponent,        
        RuleItemComponent,
        RuleListComponent,    
        RuleResultsGridComponent,            
        StructureTile,        
        SynonymsTile,
        ActivityTile,
        AssignmentsTile,
        BoardTile,
        ActivityDetailsTile,  
        
        AceEditorComponent,
    ],
    imports: [
        BrowserModule,
        FormsModule,
        ReactiveFormsModule,
        routing,
        HttpModule,

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
        DragDropModule,
        PaginatorModule,
        TreeModule,
        OverlayPanelModule,
        DataListModule,
        SharedModule,


        //highcharts
        ChartModule,

        ColorPickerModule,

        //d3s modules
        PipesModule,
        SearchModule,
        WorkflowModule,
        D3SSharedModule,  
        SocialModule,           
        FusionModule,
        D3SFormsModule,
        GroupModule,
        CommunityModule,
        CoreModule,
        MonitorModule,
        ReferenceModule,
        PolicyModule,
        HelpModule,
    ],
    bootstrap: [AppComponent],
    providers: [
        AdminUserGuard,
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
        AuthenticationService,
        COMPILER_PROVIDERS,
        DynamicTypeBuilder,
        Title,
        HeaderActionsService,
        HeaderBreadcrumbService,
        MessagesService,        
        RightSidebarService,
        WebAnalyticsService,
        StateService
    ],    
})
export class AppModule { }







