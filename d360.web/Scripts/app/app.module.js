"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d === decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var core_1 = require('@angular/core');
var platform_browser_1 = require('@angular/platform-browser');
var app_component_1 = require('./app.component');
var forms_1 = require('@angular/forms');
var app_routes_1 = require('./app.routes');
var http_1 = require('@angular/http');
var angular2_highcharts_1 = require('angular2-highcharts');
var pipes_module_1 = require('./pipes/pipes.module');
var core_module_1 = require('./components/shared/core.module');
var search_module_1 = require('./components/search/search.module');
var workflow_module_1 = require('./components/workflow/workflow.module');
var shared_module_1 = require('./components/shared/shared.module');
var social_module_1 = require('./components/social/social.module');
var navbar_module_1 = require('./components/navbar/navbar.module');
var fusion_module_1 = require('./components/fusion/fusion.module');
var group_module_1 = require('./components/group/group.module');
var community_module_1 = require('./components/community/community.module');
var monitor_module_1 = require('./components/monitor/monitor.module');
var reference_module_1 = require('./components/reference/reference.module');
var d3sforms_module_1 = require('./components/forms/d3sforms.module'); // why are some forms in a separate module instead of by area?
var admin_user_guard_1 = require('./guards/admin-user.guard');
var authentication_service_1 = require('./services/authentication.service');
var primeng_1 = require('primeng/primeng');
var index_1 = require('./components/admin/index');
var index_2 = require('./components/artifact/index');
var index_3 = require('./components/header/index');
var home_component_1 = require('./components/home/home.component');
var index_4 = require('./components/model/index');
var index_5 = require('./components/parts/index');
var index_6 = require('./components/policy/index');
var index_7 = require('./components/resource/index');
var right_sidebar_component_1 = require('./components/rightsidebar/right-sidebar.component');
var right_sidebar_item_component_1 = require('./components/rightsidebar/right-sidebar-item.component');
var index_8 = require('./components/rule/index');
var index_9 = require('./components/tiles/index');
var AppModule = (function () {
    function AppModule() {
    }
    AppModule = __decorate([
        core_1.NgModule({
            declarations: [
                index_5.ActionBar,
                index_1.AdminArtifactsComponent,
                index_1.AdminAttributeTypeEditor,
                index_1.AdminAttributesComponent,
                index_1.AdminComponent,
                index_1.AdminDashboardsComponent,
                index_1.AdminDashboardsEditor,
                index_1.AdminDomainComponent,
                index_1.AdminFusionComponent,
                index_1.AdminGovernanceComponent,
                index_1.AdminGroupsComponent,
                index_1.AdminLoadComponent,
                index_1.AdminLookupTypeEditorComponent,
                index_1.AdminLookupsComponent,
                index_1.AdminPoliciesComponent,
                index_1.AdminRelationshipsComponent,
                index_1.AdminRelationshipsEditor,
                index_1.AdminResourcesComponent,
                index_1.AdminRulesComponent,
                index_1.AdminSettingsComponent,
                index_1.AdminStatisticCheckTypeInput,
                index_1.AdminReportItemsComponent,
                index_1.AdminReportLayoutComponent,
                index_1.AdminSurveyQuestionsComponent,
                index_1.AdminRuleDimensionsComponent,
                index_1.AdminStatisticEditor,
                index_1.AdminStatisticsComponent,
                index_1.AdminSurveyQuestionEditorEditor,
                index_1.AdminSurveysComponent,
                index_1.AdminTaxonomiesComponent,
                index_1.AdminTaxonomyDetailComponent,
                index_1.AdminTaxonomyEditorComponent,
                index_1.AdminTaxonomyLevelEditorComponent,
                index_1.AdminTemplateEditorComponent,
                index_1.AdminTemplatesComponent,
                index_1.AdminWorkflowComponent,
                app_component_1.AppComponent,
                index_2.ArtifactColumnFilterComponent,
                index_2.ArtifactComponent,
                index_2.ArtifactDefnintionComponent,
                index_2.ArtifactGridComponent,
                index_2.ArtifactItemComponent,
                index_2.ArtifactListComponent,
                index_2.ArtifactTopLevelListComponent,
                index_2.ArtifactTypeMetricsComponent,
                index_2.ArtifactTypeWorkflowStatusComponent,
                index_9.AttributesTile,
                index_5.ClaimsMatrixPart,
                index_9.ClaimsTile,
                index_9.FusionAttributesTile,
                index_9.FusionConfigurationTile,
                index_3.HeaderActionsComponent,
                index_3.HeaderBreadcrumbComponent,
                index_3.HeaderBreadcrumbItemComponent,
                index_3.HeaderComponent,
                index_3.HeaderFavoritesComponent,
                index_3.HeaderFollowComponent,
                index_3.HeaderTypeaheadSearchComponent,
                home_component_1.HomeComponent,
                index_9.LoadItemTile,
                index_5.MenuPart,
                index_4.ModelComponent,
                index_4.ModelItemComponent,
                index_9.ModelLevelTile,
                index_4.ModelListComponent,
                index_4.ModelItemStructureComponent,
                index_9.ObjectDefinitionTile,
                index_6.PolicyComponent,
                index_6.PolicyItemComponent,
                index_6.PolicyItemStructureComponent,
                index_9.PredicatesTile,
                index_9.RelationshipsTile,
                index_7.ResourceApiComponent,
                index_7.ResourceComponent,
                index_9.ResourceFollowingGridTile,
                index_9.ResourceFollowingTile,
                index_7.ResourceItemComponent,
                index_9.ResourceResponsibilityTile,
                index_7.ResourceListComponent,
                index_7.ResourceGroupsComponent,
                right_sidebar_component_1.RightSidebarComponent,
                right_sidebar_item_component_1.RightSidebarItemComponent,
                index_8.RuleComponent,
                index_8.RuleItemComponent,
                index_8.RuleListComponent,
                index_5.SimpleDropdown,
                index_9.StructureTile,
                index_9.SynonymsTile,
                index_9.ActivityTile,
                index_9.AssignmentsTile,
                index_9.BoardTile,
                index_9.ActivityDetailsTile,
            ],
            imports: [
                platform_browser_1.BrowserModule,
                forms_1.FormsModule,
                forms_1.ReactiveFormsModule,
                app_routes_1.routing,
                http_1.HttpModule,
                //primeng
                primeng_1.GrowlModule,
                primeng_1.InputTextModule,
                primeng_1.InputMaskModule,
                primeng_1.DataTableModule,
                primeng_1.TreeTableModule,
                primeng_1.ButtonModule,
                primeng_1.DropdownModule,
                primeng_1.CheckboxModule,
                primeng_1.CalendarModule,
                primeng_1.MenuModule,
                primeng_1.MenubarModule,
                primeng_1.AccordionModule,
                primeng_1.SelectButtonModule,
                primeng_1.AutoCompleteModule,
                primeng_1.MultiSelectModule,
                primeng_1.SpinnerModule,
                primeng_1.EditorModule,
                primeng_1.TooltipModule,
                primeng_1.DragDropModule,
                primeng_1.PaginatorModule,
                primeng_1.TreeModule,
                primeng_1.OverlayPanelModule,
                primeng_1.DataListModule,
                angular2_highcharts_1.ChartModule,
                //d3s modules
                pipes_module_1.PipesModule,
                search_module_1.SearchModule,
                workflow_module_1.WorkflowModule,
                shared_module_1.SharedModule,
                social_module_1.SocialModule,
                navbar_module_1.NavbarModule,
                fusion_module_1.FusionModule,
                d3sforms_module_1.D3SFormsModule,
                group_module_1.GroupModule,
                community_module_1.CommunityModule,
                core_module_1.CoreModule,
                monitor_module_1.MonitorModule,
                reference_module_1.ReferenceModule,
            ],
            bootstrap: [app_component_1.AppComponent],
            providers: [platform_browser_1.Title, admin_user_guard_1.AdminUserGuard, authentication_service_1.AuthenticationService],
        }), 
        __metadata('design:paramtypes', [])
    ], AppModule);
    return AppModule;
}());
exports.AppModule = AppModule;
//# sourceMappingURL=app.module.js.map