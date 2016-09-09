System.register(['@angular/core', '@angular/platform-browser', './app.component', '@angular/forms', './app.routes', '@angular/http', './pipes/pipes.module', './components/search/search.module', './components/workflow/workflow.module', './components/shared/shared.module', 'primeng/primeng', './components/admin/index', './components/artifact/index', './components/community/index', './components/diagnostic/index', './components/forms/index', './components/fusion/index', './components/header/index', './components/home/home.component', './components/model/index', './components/monitor/index', './components/navbar/navbar.component', './components/navbar/navbar-item.component', './components/navbar/navbar-menu.component', './components/parts/index', './components/policy/index', './components/reference/index', './components/resource/index', './components/rightsidebar/right-sidebar.component', './components/rightsidebar/right-sidebar-item.component', './components/rule/index', './components/social/index', './components/tiles/index'], function(exports_1, context_1) {
    "use strict";
    var __moduleName = context_1 && context_1.id;
    var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
        var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
        if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
        else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
        return c > 3 && r && Object.defineProperty(target, key, r), r;
    };
    var __metadata = (this && this.__metadata) || function (k, v) {
        if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
    };
    var core_1, platform_browser_1, app_component_1, forms_1, app_routes_1, http_1, pipes_module_1, search_module_1, workflow_module_1, shared_module_1, primeng_1, index_1, index_2, index_3, index_4, index_5, index_6, index_7, home_component_1, index_8, index_9, navbar_component_1, navbar_item_component_1, navbar_menu_component_1, index_10, index_11, index_12, index_13, right_sidebar_component_1, right_sidebar_item_component_1, index_14, index_15, index_16;
    var AppModule;
    return {
        setters:[
            function (core_1_1) {
                core_1 = core_1_1;
            },
            function (platform_browser_1_1) {
                platform_browser_1 = platform_browser_1_1;
            },
            function (app_component_1_1) {
                app_component_1 = app_component_1_1;
            },
            function (forms_1_1) {
                forms_1 = forms_1_1;
            },
            function (app_routes_1_1) {
                app_routes_1 = app_routes_1_1;
            },
            function (http_1_1) {
                http_1 = http_1_1;
            },
            function (pipes_module_1_1) {
                pipes_module_1 = pipes_module_1_1;
            },
            function (search_module_1_1) {
                search_module_1 = search_module_1_1;
            },
            function (workflow_module_1_1) {
                workflow_module_1 = workflow_module_1_1;
            },
            function (shared_module_1_1) {
                shared_module_1 = shared_module_1_1;
            },
            function (primeng_1_1) {
                primeng_1 = primeng_1_1;
            },
            function (index_1_1) {
                index_1 = index_1_1;
            },
            function (index_2_1) {
                index_2 = index_2_1;
            },
            function (index_3_1) {
                index_3 = index_3_1;
            },
            function (index_4_1) {
                index_4 = index_4_1;
            },
            function (index_5_1) {
                index_5 = index_5_1;
            },
            function (index_6_1) {
                index_6 = index_6_1;
            },
            function (index_7_1) {
                index_7 = index_7_1;
            },
            function (home_component_1_1) {
                home_component_1 = home_component_1_1;
            },
            function (index_8_1) {
                index_8 = index_8_1;
            },
            function (index_9_1) {
                index_9 = index_9_1;
            },
            function (navbar_component_1_1) {
                navbar_component_1 = navbar_component_1_1;
            },
            function (navbar_item_component_1_1) {
                navbar_item_component_1 = navbar_item_component_1_1;
            },
            function (navbar_menu_component_1_1) {
                navbar_menu_component_1 = navbar_menu_component_1_1;
            },
            function (index_10_1) {
                index_10 = index_10_1;
            },
            function (index_11_1) {
                index_11 = index_11_1;
            },
            function (index_12_1) {
                index_12 = index_12_1;
            },
            function (index_13_1) {
                index_13 = index_13_1;
            },
            function (right_sidebar_component_1_1) {
                right_sidebar_component_1 = right_sidebar_component_1_1;
            },
            function (right_sidebar_item_component_1_1) {
                right_sidebar_item_component_1 = right_sidebar_item_component_1_1;
            },
            function (index_14_1) {
                index_14 = index_14_1;
            },
            function (index_15_1) {
                index_15 = index_15_1;
            },
            function (index_16_1) {
                index_16 = index_16_1;
            }],
        execute: function() {
            AppModule = (function () {
                function AppModule() {
                }
                AppModule = __decorate([
                    core_1.NgModule({
                        declarations: [
                            index_10.ActionBar,
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
                            index_5.ArtifactTypeForm,
                            index_16.AttributesTile,
                            index_10.ClaimsMatrixPart,
                            index_16.ClaimsTile,
                            index_3.CommunityComponent,
                            index_3.CommunitySummaryComponent,
                            index_4.DiagnosticComponent,
                            index_4.DiagnosticIncorrectTextpathComponent,
                            index_16.FieldDefinitionTile,
                            index_5.FieldTypeForm,
                            index_16.FusionAttributesTile,
                            index_6.FusionComponent,
                            index_16.FusionConfigurationTile,
                            index_16.FusionFiltersTile,
                            index_6.FusionItemComponent,
                            index_6.FusionListComponent,
                            index_5.GroupForm,
                            index_16.GroupMembersTile,
                            index_7.HeaderActionsComponent,
                            index_7.HeaderBreadcrumbComponent,
                            index_7.HeaderBreadcrumbItemComponent,
                            index_7.HeaderComponent,
                            index_7.HeaderFavoritesComponent,
                            index_7.HeaderTypeaheadSearchComponent,
                            home_component_1.HomeComponent,
                            index_5.LoadForm,
                            index_16.LoadItemTile,
                            index_10.MenuPart,
                            index_8.ModelComponent,
                            index_8.ModelItemComponent,
                            index_16.ModelLevelTile,
                            index_8.ModelListComponent,
                            index_9.MonitorComponent,
                            index_9.MonitorListComponent,
                            navbar_component_1.NavBarComponent,
                            navbar_item_component_1.NavBarItemComponent,
                            navbar_menu_component_1.NavBarMenuComponent,
                            index_16.ObjectDefinitionTile,
                            index_10.ObjectDetailField,
                            index_16.ObjectDetailTile,
                            index_16.ObjectGovernanceTile,
                            index_16.ObjectRelationshipsTile,
                            index_16.PeopleResponsibilitiesTile,
                            index_11.PolicyComponent,
                            index_11.PolicyItemComponent,
                            index_16.PredicatesTile,
                            index_12.ReferenceComponent,
                            index_12.ReferenceListComponent,
                            index_16.RelationshipsTile,
                            index_16.ReportItemsTile,
                            index_16.ReportLayoutTile,
                            index_13.ResourceApiComponent,
                            index_13.ResourceComponent,
                            index_16.ResourceFollowingGridTile,
                            index_16.ResourceFollowingTile,
                            index_13.ResourceItemComponent,
                            index_16.ResourceResponsibilityGridTile,
                            index_16.ResourceResponsibilityTile,
                            index_5.ResponsibilityItemForm,
                            index_5.ResponsibilityTypeForm,
                            right_sidebar_component_1.RightSidebarComponent,
                            right_sidebar_item_component_1.RightSidebarItemComponent,
                            index_14.RuleComponent,
                            index_16.RuleDimensionsTile,
                            index_14.RuleItemComponent,
                            index_14.RuleListComponent,
                            index_10.SimpleAccordion,
                            index_10.SimpleDropdown,
                            index_15.SocialBoardComponent,
                            index_15.SocialCommentComponent,
                            index_15.SocialInputComponent,
                            index_16.StructureTile,
                            index_16.SurveyQuestionsTile,
                            index_16.SynonymsTile,
                            index_16.ActivityTile,
                            index_16.AssignmentsTile,
                            index_16.BoardTile,
                            index_16.ActivityDetailsTile,
                            index_5.WorkflowItemForm,
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
                            //d3s modules
                            pipes_module_1.PipesModule,
                            search_module_1.SearchModule,
                            workflow_module_1.WorkflowModule,
                            shared_module_1.SharedModule,
                        ],
                        bootstrap: [app_component_1.AppComponent],
                        providers: [platform_browser_1.Title],
                    }), 
                    __metadata('design:paramtypes', [])
                ], AppModule);
                return AppModule;
            }());
            exports_1("AppModule", AppModule);
        }
    }
});
