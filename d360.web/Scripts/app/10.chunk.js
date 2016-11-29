webpackJsonp([10,11,12],{

/***/ 1150:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__shared_core_module__ = __webpack_require__(1165);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__shared_tiles_tiles_module__ = __webpack_require__(1166);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__shared_grid_paging_info_component__ = __webpack_require__(1167);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__workflow_assignments_component__ = __webpack_require__(1193);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_10__workflow_component__ = __webpack_require__(1181);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11__workflow_detail_component__ = __webpack_require__(1196);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_12__workflow_suggest_details_component__ = __webpack_require__(1200);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_13__workflow_certify_details_component__ = __webpack_require__(1194);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_14__workflow_certify_editor_component__ = __webpack_require__(1195);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_15__workflow_raise_issue_component__ = __webpack_require__(1178);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_16__workflow_suggest_editor_component__ = __webpack_require__(1201);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_17__workflow_view_status_component__ = __webpack_require__(1179);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_18__workflow_work_item_component__ = __webpack_require__(1180);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_19__workflow_detailed_view_component__ = __webpack_require__(1197);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_20__workflow_issue_details_component__ = __webpack_require__(1198);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_21__workflow_issue_editor_component__ = __webpack_require__(1199);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_22__workflow_routes__ = __webpack_require__(1202);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_23_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_23_primeng_primeng__);
/* harmony export (binding) */ __webpack_require__.d(exports, "WorkflowModule", function() { return WorkflowModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
























var WorkflowModule = (function () {
    function WorkflowModule() {
    }
    WorkflowModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["e" /* HttpModule */],
                __WEBPACK_IMPORTED_MODULE_4__angular_router__["RouterModule"],
                __WEBPACK_IMPORTED_MODULE_22__workflow_routes__["a" /* WorkflowRoutingModule */],
                //primeng  
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["GrowlModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["InputTextModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["DataTableModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["ButtonModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["DropdownModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["CheckboxModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["MultiSelectModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["TooltipModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["EditorModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["AutoCompleteModule"],
                __WEBPACK_IMPORTED_MODULE_23_primeng_primeng__["SharedModule"],
                //d3s
                __WEBPACK_IMPORTED_MODULE_6__shared_core_module__["a" /* CoreModule */],
                __WEBPACK_IMPORTED_MODULE_7__shared_tiles_tiles_module__["a" /* TilesModule */],
                __WEBPACK_IMPORTED_MODULE_8__shared_grid_paging_info_component__["a" /* SharedGridPagingInfoModule */],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_9__workflow_assignments_component__["a" /* WorkflowAssignmentsComponent */],
                __WEBPACK_IMPORTED_MODULE_13__workflow_certify_details_component__["a" /* WorkflowCertifyDetailsComponent */],
                __WEBPACK_IMPORTED_MODULE_14__workflow_certify_editor_component__["a" /* WorkflowCertifyEditorComponent */],
                __WEBPACK_IMPORTED_MODULE_15__workflow_raise_issue_component__["a" /* WorkflowRaiseIssueComponent */],
                __WEBPACK_IMPORTED_MODULE_12__workflow_suggest_details_component__["a" /* WorkflowSuggestDetailsComponent */],
                __WEBPACK_IMPORTED_MODULE_16__workflow_suggest_editor_component__["a" /* WorkflowSuggestEditorComponent */],
                __WEBPACK_IMPORTED_MODULE_17__workflow_view_status_component__["a" /* WorkflowViewStatusComponent */],
                __WEBPACK_IMPORTED_MODULE_18__workflow_work_item_component__["a" /* WorkflowWorkItemComponent */],
                __WEBPACK_IMPORTED_MODULE_11__workflow_detail_component__["a" /* WorkflowDetailComponent */],
                __WEBPACK_IMPORTED_MODULE_10__workflow_component__["a" /* WorkflowComponent */],
                __WEBPACK_IMPORTED_MODULE_19__workflow_detailed_view_component__["a" /* WorkflowDetailedViewComponent */],
                __WEBPACK_IMPORTED_MODULE_20__workflow_issue_details_component__["a" /* WorkflowIssueDetailsComponent */],
                __WEBPACK_IMPORTED_MODULE_21__workflow_issue_editor_component__["a" /* WorkflowIssueEditorComponent */],
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_9__workflow_assignments_component__["a" /* WorkflowAssignmentsComponent */],
                __WEBPACK_IMPORTED_MODULE_13__workflow_certify_details_component__["a" /* WorkflowCertifyDetailsComponent */],
                __WEBPACK_IMPORTED_MODULE_14__workflow_certify_editor_component__["a" /* WorkflowCertifyEditorComponent */],
                __WEBPACK_IMPORTED_MODULE_15__workflow_raise_issue_component__["a" /* WorkflowRaiseIssueComponent */],
                __WEBPACK_IMPORTED_MODULE_12__workflow_suggest_details_component__["a" /* WorkflowSuggestDetailsComponent */],
                __WEBPACK_IMPORTED_MODULE_16__workflow_suggest_editor_component__["a" /* WorkflowSuggestEditorComponent */],
                __WEBPACK_IMPORTED_MODULE_18__workflow_work_item_component__["a" /* WorkflowWorkItemComponent */],
                __WEBPACK_IMPORTED_MODULE_11__workflow_detail_component__["a" /* WorkflowDetailComponent */],
                __WEBPACK_IMPORTED_MODULE_10__workflow_component__["a" /* WorkflowComponent */],
                __WEBPACK_IMPORTED_MODULE_19__workflow_detailed_view_component__["a" /* WorkflowDetailedViewComponent */],
                __WEBPACK_IMPORTED_MODULE_20__workflow_issue_details_component__["a" /* WorkflowIssueDetailsComponent */],
                __WEBPACK_IMPORTED_MODULE_21__workflow_issue_editor_component__["a" /* WorkflowIssueEditorComponent */],
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_3__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], WorkflowModule);
    return WorkflowModule;
}());


/***/ },

/***/ 1152:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__shared_core_module__ = __webpack_require__(1165);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__shared_tiles_tiles_module__ = __webpack_require__(1166);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__home_search_component__ = __webpack_require__(1287);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__search_results_component__ = __webpack_require__(1291);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_10__search_result_item_component__ = __webpack_require__(1290);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11__search_component__ = __webpack_require__(1280);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_12__search_autocomplete_list_component__ = __webpack_require__(1288);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_13__search_input_component__ = __webpack_require__(1289);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_14__search_routes__ = __webpack_require__(1292);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_15_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_15_primeng_primeng__);
/* harmony export (binding) */ __webpack_require__.d(exports, "SearchModule", function() { return SearchModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
















var SearchModule = (function () {
    function SearchModule() {
    }
    SearchModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["e" /* HttpModule */],
                __WEBPACK_IMPORTED_MODULE_4__angular_router__["RouterModule"],
                __WEBPACK_IMPORTED_MODULE_14__search_routes__["a" /* SearchRoutingModule */],
                //primeng         
                __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__["InputTextModule"],
                __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__["ButtonModule"],
                __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__["DropdownModule"],
                __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__["CheckboxModule"],
                __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__["MultiSelectModule"],
                __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__["TooltipModule"],
                __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__["PaginatorModule"],
                __WEBPACK_IMPORTED_MODULE_15_primeng_primeng__["SharedModule"],
                //d3s        
                __WEBPACK_IMPORTED_MODULE_6__shared_core_module__["a" /* CoreModule */],
                __WEBPACK_IMPORTED_MODULE_7__shared_tiles_tiles_module__["a" /* TilesModule */],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_8__home_search_component__["a" /* HomeSearchComponent */],
                __WEBPACK_IMPORTED_MODULE_9__search_results_component__["a" /* SearchResultsComponent */],
                __WEBPACK_IMPORTED_MODULE_10__search_result_item_component__["a" /* SearchResultItemComponent */],
                __WEBPACK_IMPORTED_MODULE_11__search_component__["a" /* SearchComponent */],
                __WEBPACK_IMPORTED_MODULE_12__search_autocomplete_list_component__["a" /* SearchAutocompleteListComponent */],
                __WEBPACK_IMPORTED_MODULE_13__search_input_component__["a" /* SearchInputComponent */],
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_8__home_search_component__["a" /* HomeSearchComponent */],
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_3__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], SearchModule);
    return SearchModule;
}());


/***/ },

/***/ 1158:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__shared_core_module__ = __webpack_require__(1165);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__shared_social_social_module__ = __webpack_require__(1203);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__workflow_workflow_module__ = __webpack_require__(1150);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__search_search_module__ = __webpack_require__(1152);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__shared_tiles_tiles_module__ = __webpack_require__(1166);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_10__shared_grid_paging_info_component__ = __webpack_require__(1167);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11__shared_dashboard_shared_dashboard_module__ = __webpack_require__(1283);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_12__home_component__ = __webpack_require__(1335);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_13__activity_tile_component__ = __webpack_require__(1454);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_14__activity_details_tile_component__ = __webpack_require__(1453);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_15__board_tile_component__ = __webpack_require__(1455);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_16__home_routes__ = __webpack_require__(1456);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_17_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_17_primeng_primeng__);
/* harmony export (binding) */ __webpack_require__.d(exports, "HomeModule", function() { return HomeModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};


















var HomeModule = (function () {
    function HomeModule() {
    }
    HomeModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_http__["e" /* HttpModule */],
                __WEBPACK_IMPORTED_MODULE_3__angular_router__["RouterModule"],
                __WEBPACK_IMPORTED_MODULE_16__home_routes__["a" /* HomeRoutingModule */],
                //primeng  
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["GrowlModule"],
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["InputTextModule"],
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["DataTableModule"],
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["ButtonModule"],
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["DropdownModule"],
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["CheckboxModule"],
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["MultiSelectModule"],
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["TooltipModule"],
                __WEBPACK_IMPORTED_MODULE_17_primeng_primeng__["SharedModule"],
                //d3s        
                __WEBPACK_IMPORTED_MODULE_5__shared_core_module__["a" /* CoreModule */],
                __WEBPACK_IMPORTED_MODULE_8__search_search_module__["SearchModule"],
                __WEBPACK_IMPORTED_MODULE_6__shared_social_social_module__["a" /* SocialModule */],
                __WEBPACK_IMPORTED_MODULE_7__workflow_workflow_module__["WorkflowModule"],
                __WEBPACK_IMPORTED_MODULE_9__shared_tiles_tiles_module__["a" /* TilesModule */],
                __WEBPACK_IMPORTED_MODULE_10__shared_grid_paging_info_component__["a" /* SharedGridPagingInfoModule */],
                __WEBPACK_IMPORTED_MODULE_11__shared_dashboard_shared_dashboard_module__["a" /* SharedDashboardModule */],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_14__activity_details_tile_component__["a" /* ActivityDetailsTile */],
                __WEBPACK_IMPORTED_MODULE_13__activity_tile_component__["a" /* ActivityTile */],
                __WEBPACK_IMPORTED_MODULE_15__board_tile_component__["a" /* BoardTile */],
                __WEBPACK_IMPORTED_MODULE_12__home_component__["a" /* HomeComponent */],
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_2__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_4__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], HomeModule);
    return HomeModule;
}());


/***/ },

/***/ 1165:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__tooltip_component__ = __webpack_require__(1186);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__loading_component__ = __webpack_require__(1185);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return CoreModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var CoreModule = (function () {
    function CoreModule() {
    }
    CoreModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            declarations: [
                __WEBPACK_IMPORTED_MODULE_2__tooltip_component__["a" /* TooltipComponent */],
                __WEBPACK_IMPORTED_MODULE_3__loading_component__["a" /* LoadingComponent */],
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_2__tooltip_component__["a" /* TooltipComponent */],
                __WEBPACK_IMPORTED_MODULE_3__loading_component__["a" /* LoadingComponent */],
            ],
            imports: [
                __WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"]
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], CoreModule);
    return CoreModule;
}());


/***/ },

/***/ 1166:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__tile_actions_component__ = __webpack_require__(1189);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_3_primeng_primeng__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TilesModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var TilesModule = (function () {
    function TilesModule() {
    }
    TilesModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                //prime
                __WEBPACK_IMPORTED_MODULE_3_primeng_primeng__["MenubarModule"],
                __WEBPACK_IMPORTED_MODULE_3_primeng_primeng__["TooltipModule"],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_2__tile_actions_component__["a" /* TileActionsComponent */]
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_2__tile_actions_component__["a" /* TileActionsComponent */]
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], TilesModule);
    return TilesModule;
}());


/***/ },

/***/ 1167:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_component__ = __webpack_require__(113);
/* unused harmony export GridPagingInfoComponent */
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SharedGridPagingInfoModule; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var GridPagingInfoComponent = (function (_super) {
    __extends(GridPagingInfoComponent, _super);
    function GridPagingInfoComponent() {
        _super.apply(this, arguments);
    }
    Object.defineProperty(GridPagingInfoComponent.prototype, "startValue", {
        get: function () {
            if (this.first != undefined) {
                return (this.first + 1).toLocaleString();
            }
            return '';
        },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(GridPagingInfoComponent.prototype, "endValue", {
        get: function () {
            if ((this.first + Number(this.rows)) > this.totalRecords) {
                return this.totalRecords.toLocaleString();
            }
            return (this.first + Number(this.rows)).toLocaleString();
        },
        enumerable: true,
        configurable: true
    });
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], GridPagingInfoComponent.prototype, "first", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], GridPagingInfoComponent.prototype, "rows", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], GridPagingInfoComponent.prototype, "totalRecords", void 0);
    GridPagingInfoComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-grid-paging-info',
            template: "   \n            Rows {{startValue}} - {{endValue}} of {{totalRecords?.toLocaleString()}} Items\n        ",
            changeDetection: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectionStrategy"].OnPush
        }), 
        __metadata('design:paramtypes', [])
    ], GridPagingInfoComponent);
    return GridPagingInfoComponent;
}(__WEBPACK_IMPORTED_MODULE_2__base_component__["a" /* BaseComponent */]));
var SharedGridPagingInfoModule = (function () {
    function SharedGridPagingInfoModule() {
    }
    SharedGridPagingInfoModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
            ],
            declarations: [
                GridPagingInfoComponent
            ],
            exports: [
                GridPagingInfoComponent
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], SharedGridPagingInfoModule);
    return SharedGridPagingInfoModule;
}());


/***/ },

/***/ 1168:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* unused harmony export WorkflowTypeRelationEditorModel */
/* harmony export (binding) */ __webpack_require__.d(exports, "e", function() { return WorkflowItem; });
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowType; });
/* unused harmony export IssueType */
/* harmony export (binding) */ __webpack_require__.d(exports, "d", function() { return Issue; });
/* unused harmony export IssueDetail */
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return CertifyItem; });
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return SuggestedItem; });
/* unused harmony export WorkflowStepStatistic */
/* unused harmony export ArtifactTypeWorkflowBreakdown */
/* unused harmony export WorkflowStep */
/* unused harmony export WorkflowStatusDetailField */
/* unused harmony export WorkflowAssignment */
/* unused harmony export WorkflowStatusDetails */
var WorkflowTypeRelationEditorModel = (function () {
    function WorkflowTypeRelationEditorModel() {
    }
    return WorkflowTypeRelationEditorModel;
}());
var WorkflowItem = (function () {
    function WorkflowItem() {
    }
    return WorkflowItem;
}());
var WorkflowType;
(function (WorkflowType) {
    WorkflowType[WorkflowType["SuggestNewArtifact"] = 1] = "SuggestNewArtifact";
    WorkflowType[WorkflowType["CertifyArtifact"] = 2] = "CertifyArtifact";
    WorkflowType[WorkflowType["WorkIssue"] = 3] = "WorkIssue";
    WorkflowType[WorkflowType["ChallengeArtifact"] = 4] = "ChallengeArtifact";
    WorkflowType[WorkflowType["SuggestNewArtifactMulti"] = 5] = "SuggestNewArtifactMulti";
})(WorkflowType || (WorkflowType = {}));
var IssueType;
(function (IssueType) {
    IssueType[IssueType["Issue"] = 0] = "Issue";
    IssueType[IssueType["Challenge"] = 1] = "Challenge";
})(IssueType || (IssueType = {}));
var Issue = (function () {
    function Issue() {
    }
    return Issue;
}());
var IssueDetail = (function () {
    function IssueDetail() {
    }
    return IssueDetail;
}());
var CertifyItem = (function () {
    function CertifyItem() {
    }
    return CertifyItem;
}());
var SuggestedItem = (function () {
    function SuggestedItem() {
    }
    return SuggestedItem;
}());
var WorkflowStepStatistic = (function () {
    function WorkflowStepStatistic() {
    }
    return WorkflowStepStatistic;
}());
var ArtifactTypeWorkflowBreakdown = (function () {
    function ArtifactTypeWorkflowBreakdown() {
    }
    return ArtifactTypeWorkflowBreakdown;
}());
var WorkflowStep = (function () {
    function WorkflowStep() {
    }
    return WorkflowStep;
}());
var WorkflowStatusDetailField = (function () {
    function WorkflowStatusDetailField() {
    }
    return WorkflowStatusDetailField;
}());
var WorkflowAssignment = (function () {
    function WorkflowAssignment() {
    }
    return WorkflowAssignment;
}());
var WorkflowStatusDetails = (function () {
    function WorkflowStatusDetails() {
    }
    return WorkflowStatusDetails;
}());


/***/ },

/***/ 1176:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return CurrentCompanySettings; });
var CurrentCompanySettings = (function () {
    function CurrentCompanySettings() {
    }
    CurrentCompanySettings.settings = CompanySettings;
    CurrentCompanySettings.disableCommunityPosting = CurrentCompanySettings.settings.DisableCommunityPosting === 'true';
    CurrentCompanySettings.defaultSearchTypes = CurrentCompanySettings.settings.DefaultSearchTypes;
    CurrentCompanySettings.headerBackgroundColor = CurrentCompanySettings.settings.HeaderBackgroundColor;
    CurrentCompanySettings.headerProfileLinkColor = CurrentCompanySettings.settings.HeaderProfileLinkColor;
    CurrentCompanySettings.hideData3SixtyUsers = CurrentCompanySettings.settings.HideData3SixtyUsers;
    CurrentCompanySettings.artifactType_TaxonomyTypeID = CurrentCompanySettings.settings.ArtifactType_TaxonomyTypeID;
    CurrentCompanySettings.artifactType_TaxonomyTypeIDNodes = CurrentCompanySettings.settings.ArtifactType_TaxonomyTypeIDNodes;
    CurrentCompanySettings.companyIcon = CurrentCompanySettings.settings.CompanyIcon;
    CurrentCompanySettings.companyLogo = CurrentCompanySettings.settings.CompanyLogo;
    return CurrentCompanySettings;
}());


/***/ },

/***/ 1178:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__ = __webpack_require__(100);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__models_breadcrumb_model__ = __webpack_require__(484);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__static_d3s_object_helpers__ = __webpack_require__(1188);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowRaiseIssueComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};







var WorkflowRaiseIssueComponent = (function (_super) {
    __extends(WorkflowRaiseIssueComponent, _super);
    function WorkflowRaiseIssueComponent(tagService, workflowService, objectDetailService, location, titleService, headerBreadcrumbService, webAnalyticsService, rightSidebarService) {
        _super.call(this, rightSidebarService, webAnalyticsService);
        this.tagService = tagService;
        this.workflowService = workflowService;
        this.objectDetailService = objectDetailService;
        this.location = location;
        this.titleService = titleService;
        this.headerBreadcrumbService = headerBreadcrumbService;
        this.terms = [];
        this.selectedOption = 'other';
    }
    WorkflowRaiseIssueComponent.prototype.ngOnInit = function () {
        this.setBrowserTitle(this.titleService, 'Take Action');
        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.id)
            this.objectId = this.headerBreadcrumbService.currentObject.id;
        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.type)
            this.objectType = this.headerBreadcrumbService.currentObject.type;
        this.loadDetails(this.objectId, this.objectType);
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new __WEBPACK_IMPORTED_MODULE_5__models_breadcrumb_model__["a" /* Breadcrumb */]('Take Action'));
        this.clearSidebar();
    };
    WorkflowRaiseIssueComponent.prototype.ngOnDestroy = function () {
    };
    WorkflowRaiseIssueComponent.prototype.loadDetails = function (objectId, objectType) {
        var _this = this;
        if (objectId == undefined || objectType == undefined)
            return;
        this.isLoading = true;
        this.objectDetailService.getObject(objectId, objectType).then(function (res) {
            _this.objectDetail = res;
            _this.selectedOption = 'current';
            _this.selectedObjectId = _this.objectId;
            _this.selectedObjectType = _this.objectType;
            _this.isLoading = false;
        });
    };
    WorkflowRaiseIssueComponent.prototype.onSubmit = function () {
        var _this = this;
        this.isLoading = true;
        this.workflowService.raiseIssue(this.selectedObjectId, this.selectedObjectType, this.issue, this.issueType)
            .then(function (res) {
            _this.isLoading = false;
            _this.location.back();
        });
    };
    WorkflowRaiseIssueComponent.prototype.cancel = function () {
        this.location.back();
    };
    WorkflowRaiseIssueComponent.prototype.search = function (event) {
        var _this = this;
        this.tagService.getTags(event.query).then(function (data) {
            _this.terms = data;
        });
    };
    WorkflowRaiseIssueComponent.prototype.selectItem = function () {
        this.selectedObjectType = this.term.Object;
        this.selectedObjectId = this.term.ObjectID;
    };
    WorkflowRaiseIssueComponent.prototype.userFriendlyObjectName = function (objectType) {
        return __WEBPACK_IMPORTED_MODULE_6__static_d3s_object_helpers__["a" /* D3SObjectHelpers */].getObjectTypeFriendlyName(objectType);
    };
    WorkflowRaiseIssueComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-raise-issue',
            template: "\n            <d3s-loading [isLoading]=\"isLoading\"></d3s-loading>\n            <div class=\"row\" *ngIf=\"!isLoading\">\n                <div class=\"col s12\">\n                    <div class=\"tile tile-detail\">\n                        <header>Report a problem</header>\n                        <form (ngSubmit)=\"onSubmit()\" #issueForm=\"ngForm\">                        \n                            <div class=\"row\">\n                                <div class=\"col s12\">\n                                    <div class=\"FieldName\">What item would you like to report a problem with?</div>\n                                    <div *ngIf=\"objectDetail\" style=\"padding-left:20px\"><label><input name=\"selObject\" type=\"radio\"  [(ngModel)]=\"selectedOption\" (click)=\"selectedObjectId=objectId;selectedObjectType=objectType;\" value=\"current\">{{objectDetail.Name}}</label></div>\n                                    <div>\n                                        <label style=\"padding-left:20px\"><input name=\"selObject\" type=\"radio\" value=\"other\" [(ngModel)]=\"selectedOption\">Other item</label>\n                                        <div *ngIf=\"selectedOption=='other'\" style=\"padding-left:40px\"><p-autoComplete size=\"100\"                                                \n                                                scrollHeight=\"400px\"\n                                                name=\"other\"\n                                                [inputStyle]=\"{width:'100%'}\"\n                                                [(ngModel)]=\"term\" \n                                                [suggestions]=\"terms\" \n                                                (completeMethod)=\"search($event)\"                                                 \n                                                placeholder=\"Select an item\"\n                                                field=\"TextPath\" \n                                                (onSelect)=\"selectItem()\">     \n                                            <template let-item>\n                                                <span style=\"color:#999999;\">{{userFriendlyObjectName(item.Object)}} - <span *ngIf=\"item.ObjectTypeName\">{{item.ObjectTypeName}} -</span></span> {{item.TextPath}}\n                                            </template>                  \n                                        </p-autoComplete></div>                                        \n                                    </div>\n                                </div>       \n                                <div class=\"col s12\" *ngIf=\"selectedObjectId&&selectedObjectType\">\n                                    <div>&nbsp;</div>\n                                    <div class=\"FieldName\">What type of problem are you reporting?</div>                                    \n                                </div>                 \n                                <div class=\"col s12\" *ngIf=\"selectedObjectId&&selectedObjectType\">\n                                    <div style=\"padding-left:20px\"><label><input required type=\"radio\" name=\"issueType\" [(ngModel)]=\"issueType\" value=\"Issue\" checked=\"checked\" />Business Data Incorrect</label></div>\n                                </div>\n                                <div class=\"col s12\" *ngIf=\"selectedObjectId&&selectedObjectType\">\n                                    <div style=\"padding-left:20px\"><label><input required type=\"radio\" name=\"issueType\" [(ngModel)]=\"issueType\" value=\"Challenge\"/>Governance Information Incorrect</label></div>                                    \n                                </div>\n                                <div class=\"col s12\" *ngIf=\"selectedObjectId&&selectedObjectType\">\n                                    <div>&nbsp;</div>\n                                    <div class=\"FieldName\">What are the details of this problem?</div>\n                                    <div><p-editor name=\"Issue\" [style]=\"{'height':'400px'}\" [(ngModel)]=\"issue\" #issueText=\"ngModel\"></p-editor></div>                                                        \n                                    <div [hidden]=\"issueText.valid || issueText.pristine\">Issue details are required</div>\n                                </div>       \n                                <div class=\"col s12\">&nbsp;</div>\n                                <div class=\"col s12\">\n                                    <button pButton type=\"submit\" [disabled]=\"!issueForm.form.valid\" label=\"Save\"></button>                            \n                                    <button pButton type=\"button\" (click)=\"cancel();\" label=\"Cancel\"></button>\n                                </div>\n                            </div>\n                        </form>\n                    </div>\n                </div>\n            </div>\n        ",
            providers: [__WEBPACK_IMPORTED_MODULE_4__services_index__["d" /* WorkflowService */], __WEBPACK_IMPORTED_MODULE_4__services_index__["f" /* ObjectDetailService */], __WEBPACK_IMPORTED_MODULE_4__services_index__["c" /* TagService */]]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["c" /* TagService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["c" /* TagService */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["d" /* WorkflowService */]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["f" /* ObjectDetailService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["f" /* ObjectDetailService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_1__angular_common__["Location"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_common__["Location"]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__["Title"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__["Title"]) === 'function' && _e) || Object, (typeof (_f = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["g" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["g" /* HeaderBreadcrumbService */]) === 'function' && _f) || Object, (typeof (_g = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["h" /* WebAnalyticsService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["h" /* WebAnalyticsService */]) === 'function' && _g) || Object, (typeof (_h = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["i" /* RightSidebarService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["i" /* RightSidebarService */]) === 'function' && _h) || Object])
    ], WorkflowRaiseIssueComponent);
    return WorkflowRaiseIssueComponent;
    var _a, _b, _c, _d, _e, _f, _g, _h;
}(__WEBPACK_IMPORTED_MODULE_2__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1179:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__ = __webpack_require__(100);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__models_breadcrumb_model__ = __webpack_require__(484);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowViewStatusComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};






var WorkflowViewStatusComponent = (function (_super) {
    __extends(WorkflowViewStatusComponent, _super);
    function WorkflowViewStatusComponent(route, router, rightSidebarService, titleService, headerBreadcrumbService) {
        _super.call(this);
        this.route = route;
        this.router = router;
        this.titleService = titleService;
        this.headerBreadcrumbService = headerBreadcrumbService;
    }
    WorkflowViewStatusComponent.prototype.ngOnInit = function () {
        var _this = this;
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new __WEBPACK_IMPORTED_MODULE_5__models_breadcrumb_model__["a" /* Breadcrumb */]('Workflow Item Status'));
        this.setBrowserTitle(this.titleService, 'Workflow Item Status');
        this.sub = this.route.params.subscribe(function (params) {
            _this.workflowId = params['workflowId']; // (+) converts string 'id' to a number               
        });
    };
    WorkflowViewStatusComponent.prototype.ngOnDestroy = function () {
        this.sub.unsubscribe();
    };
    WorkflowViewStatusComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-view-status',
            template: " \n                <div class=\"tile tile-detail\">\n                    <header>Workflow Item Details</header>\n                    <d3s-workflow-detailed-view [workflowId]=\"workflowId\"></d3s-workflow-detailed-view>\n                </div>\n              "
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["ActivatedRoute"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["ActivatedRoute"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["i" /* RightSidebarService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["i" /* RightSidebarService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__["Title"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__["Title"]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["g" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["g" /* HeaderBreadcrumbService */]) === 'function' && _e) || Object])
    ], WorkflowViewStatusComponent);
    return WorkflowViewStatusComponent;
    var _a, _b, _c, _d, _e;
}(__WEBPACK_IMPORTED_MODULE_2__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1180:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_platform_browser__ = __webpack_require__(100);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__models_breadcrumb_model__ = __webpack_require__(484);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__models_workflow_model__ = __webpack_require__(1168);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowWorkItemComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};








var WorkflowWorkItemComponent = (function (_super) {
    __extends(WorkflowWorkItemComponent, _super);
    function WorkflowWorkItemComponent(route, location, router, workflowService, rightSidebarService, titleService, headerBreadcrumbService) {
        _super.call(this);
        this.route = route;
        this.location = location;
        this.router = router;
        this.workflowService = workflowService;
        this.titleService = titleService;
        this.headerBreadcrumbService = headerBreadcrumbService;
        this.WorkflowType = __WEBPACK_IMPORTED_MODULE_7__models_workflow_model__["a" /* WorkflowType */];
    }
    WorkflowWorkItemComponent.prototype.ngOnInit = function () {
        var _this = this;
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.sub = this.route.params.subscribe(function (params) {
            _this.isLoading = true;
            var workflowId = params['workflowId']; // (+) converts string 'id' to a number  
            _this.workflowType = +params['workflowType'];
            switch (_this.workflowType) {
                case __WEBPACK_IMPORTED_MODULE_7__models_workflow_model__["a" /* WorkflowType */].WorkIssue:
                    _this.headerBreadcrumbService.showBreadcrumb(new __WEBPACK_IMPORTED_MODULE_6__models_breadcrumb_model__["a" /* Breadcrumb */]('Work Issue'));
                    _this.setBrowserTitle(_this.titleService, 'Work Issue');
                    break;
                case __WEBPACK_IMPORTED_MODULE_7__models_workflow_model__["a" /* WorkflowType */].CertifyArtifact:
                    _this.headerBreadcrumbService.showBreadcrumb(new __WEBPACK_IMPORTED_MODULE_6__models_breadcrumb_model__["a" /* Breadcrumb */]('Certify Artifact'));
                    _this.setBrowserTitle(_this.titleService, 'Certify Artifact');
                    break;
                case __WEBPACK_IMPORTED_MODULE_7__models_workflow_model__["a" /* WorkflowType */].SuggestNewArtifact:
                    _this.headerBreadcrumbService.showBreadcrumb(new __WEBPACK_IMPORTED_MODULE_6__models_breadcrumb_model__["a" /* Breadcrumb */]('Suggest New Artifact'));
                    _this.setBrowserTitle(_this.titleService, 'Suggest New Artifact');
                    break;
            }
            _this.workflowService.getWorkflowDetails(workflowId)
                .then(function (result) {
                _this.issue = result;
                _this.isLoading = false;
            });
        });
    };
    WorkflowWorkItemComponent.prototype.ngOnDestroy = function () {
        this.sub.unsubscribe();
    };
    WorkflowWorkItemComponent.prototype.save = function () {
        this.location.back();
    };
    WorkflowWorkItemComponent.prototype.close = function () {
        this.location.back();
    };
    WorkflowWorkItemComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-work-item',
            template: " \n                <template [ngIf]=\"!isLoading\">\n                    <div class=\"tile tile-detail\" [ngSwitch]=\"workflowType\">\n                        <d3s-workflow-issue-editor *ngSwitchCase=\"WorkflowType.WorkIssue\" [issue]=\"issue\" (closeClick)=\"close()\" (saveClick)=\"save()\"></d3s-workflow-issue-editor>\n                        <d3s-workflow-certify-editor *ngSwitchCase=\"WorkflowType.CertifyArtifact\" [certify]=\"issue\" (closeClick)=\"close()\" (saveClick)=\"save()\"></d3s-workflow-certify-editor>\n                        <d3s-workflow-suggest-editor *ngSwitchCase=\"WorkflowType.SuggestNewArtifact\" [suggest]=\"issue\" (closeClick)=\"close()\" (saveClick)=\"save()\"></d3s-workflow-suggest-editor>\n                    </div>\n                </template>\n              ",
            providers: [__WEBPACK_IMPORTED_MODULE_5__services_index__["d" /* WorkflowService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__angular_router__["ActivatedRoute"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__angular_router__["ActivatedRoute"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_common__["Location"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_common__["Location"]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_2__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__angular_router__["Router"]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_5__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_5__services_index__["d" /* WorkflowService */]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_5__services_index__["i" /* RightSidebarService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_5__services_index__["i" /* RightSidebarService */]) === 'function' && _e) || Object, (typeof (_f = typeof __WEBPACK_IMPORTED_MODULE_4__angular_platform_browser__["Title"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__angular_platform_browser__["Title"]) === 'function' && _f) || Object, (typeof (_g = typeof __WEBPACK_IMPORTED_MODULE_5__services_index__["g" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_5__services_index__["g" /* HeaderBreadcrumbService */]) === 'function' && _g) || Object])
    ], WorkflowWorkItemComponent);
    return WorkflowWorkItemComponent;
    var _a, _b, _c, _d, _e, _f, _g;
}(__WEBPACK_IMPORTED_MODULE_3__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1181:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var WorkflowComponent = (function () {
    function WorkflowComponent() {
    }
    WorkflowComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-reference',
            template: "\n                <div id=\"main\">\n                    <router-outlet></router-outlet>\n                </div>\n             ",
        }), 
        __metadata('design:paramtypes', [])
    ], WorkflowComponent);
    return WorkflowComponent;
}());


/***/ },

/***/ 1182:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "d", function() { return SocialVoteType; });
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SocialCommentType; });
/* unused harmony export SocialCommentTag */
/* unused harmony export SocialVote */
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return SocialComment; });
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return SocialEditCommentData; });
var SocialVoteType;
(function (SocialVoteType) {
    SocialVoteType[SocialVoteType["DownVote"] = -1] = "DownVote";
    SocialVoteType[SocialVoteType["UpVote"] = 1] = "UpVote";
})(SocialVoteType || (SocialVoteType = {}));
var SocialCommentType;
(function (SocialCommentType) {
    SocialCommentType[SocialCommentType["System"] = 1] = "System";
    SocialCommentType[SocialCommentType["Social"] = 2] = "Social";
    SocialCommentType[SocialCommentType["Governance"] = 3] = "Governance";
    SocialCommentType[SocialCommentType["Relationship"] = 4] = "Relationship";
    SocialCommentType[SocialCommentType["Issue"] = 5] = "Issue";
    SocialCommentType[SocialCommentType["Task"] = 6] = "Task";
    SocialCommentType[SocialCommentType["RedFlag"] = 7] = "RedFlag";
    SocialCommentType[SocialCommentType["DataEvent"] = 8] = "DataEvent";
    SocialCommentType[SocialCommentType["Challenge"] = 9] = "Challenge";
})(SocialCommentType || (SocialCommentType = {}));
var SocialCommentTag = (function () {
    function SocialCommentTag() {
    }
    return SocialCommentTag;
}());
var SocialVote = (function () {
    function SocialVote() {
    }
    return SocialVote;
}());
var SocialComment = (function () {
    function SocialComment() {
    }
    return SocialComment;
}());
var SocialEditCommentData = (function () {
    function SocialEditCommentData(comment, tags) {
        if (comment)
            this.Comment = comment;
        if (tags)
            this.Tags = tags;
    }
    return SocialEditCommentData;
}());


/***/ },

/***/ 1185:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return LoadingComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var LoadingComponent = (function () {
    function LoadingComponent() {
        this.showTransparentLoader = false;
    }
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], LoadingComponent.prototype, "isLoading", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], LoadingComponent.prototype, "showTransparentLoader", void 0);
    LoadingComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-loading',
            template: " \n                <div *ngIf=\"isLoading && !showTransparentLoader\">\n                    <div style=\"padding:10px;text-align:center;\"><i class=\"fa fa-spinner fa-spin fa-2x\"></i></div>\n                </div>\n                <div *ngIf=\"isLoading && showTransparentLoader\" style=\"postion:relative;overflow:hidden;width100%;\">\n                    <div style=\"position:absolute;top:0;left:0;background:rgba(128,128,128,0.25);height:100%;width:100%;\">&nbsp;</div>\n                    <div style=\"padding:10px;text-align:center;position:absolute;top:20%;left:0;height:100%;width:100%;\"><i class=\"fa fa-spinner fa-spin fa-2x\"></i></div>\n                </div>\n                ",
            changeDetection: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectionStrategy"].OnPush
        }), 
        __metadata('design:paramtypes', [])
    ], LoadingComponent);
    return LoadingComponent;
}());
;


/***/ },

/***/ 1186:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TooltipComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var TooltipComponent = (function () {
    function TooltipComponent() {
        this.click = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    TooltipComponent.prototype.getIconName = function () {
        return 'fa-' + this.icon;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], TooltipComponent.prototype, "tooltipType", void 0);
    __decorate([
        // preview, certificate etc;
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], TooltipComponent.prototype, "objectType", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], TooltipComponent.prototype, "objectId", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], TooltipComponent.prototype, "icon", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["HostBinding"])('style.color'),
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], TooltipComponent.prototype, "iconColor", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["HostBinding"])('style.background'),
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], TooltipComponent.prototype, "foreColor", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TooltipComponent.prototype, "click", void 0);
    TooltipComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-tooltip',
            template: "                 \n                <a *ngIf=\"icon && icon !=''\" [attr.data-type]=\"objectType\" [attr.data-context]=\"tooltipType\" [attr.data-id]=\"objectId\" (click)=\"click.emit()\" data-hasqtip=\"true\" aria-describedby=\"qtip-1\"><i class=\"fa\" [ngClass]=\"getIconName()\"  [ngStyle]=\"{'color': iconColor}\"></i></a>\n                <div *ngIf=\"icon == null || icon ==''\"  style=\"display: inline-block;\" [attr.data-type]=\"objectType\" [attr.data-context]=\"tooltipType\" [attr.data-id]=\"objectId\" (click)=\"click.emit()\" data-hasqtip=\"true\" aria-describedby=\"qtip-1\">\n                    <ng-content></ng-content>\n                </div>\n              "
        }), 
        __metadata('design:paramtypes', [])
    ], TooltipComponent);
    return TooltipComponent;
}());
;


/***/ },

/***/ 1188:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return D3SObjectHelpers; });
var D3SObjectHelpers = (function () {
    function D3SObjectHelpers() {
    }
    // Given an d3s object name get its friendly name to display to users
    D3SObjectHelpers.getObjectTypeFriendlyName = function (objectType) {
        switch (objectType.toUpperCase()) {
            case "FUSIONATTRIBUTES":
                return "Fusion";
            case "ARTIFACT":
                return "Glossary";
            case "TAXONOMY":
                return "Model";
            case "DOMAIN":
                return "Reference";
            default:
                return objectType;
        }
    };
    return D3SObjectHelpers;
}());


/***/ },

/***/ 1189:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TileActionsComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var TileActionsComponent = (function () {
    function TileActionsComponent() {
        this.addClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.exportClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.exportErrorsClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.exportOriginalClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.editClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.dateClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.closeClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.refreshClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.authenticateClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.apiClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.passwordClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.suggestClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.saveClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.filterMode = false;
        this.filterModeChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.hasAdd = false;
        this.hasExport = false;
        this.hasExportErrors = false;
        this.hasExportOriginal = false;
        this.hasEdit = false;
        this.hasDate = false;
        this.hasClose = false;
        this.hasFilterMode = false;
        this.hasRefresh = false;
        this.hasAuthenticate = false;
        this.hasApi = false;
        this.hasPassword = false;
        this.hasFullScreen = false;
        this.hasSuggest = false;
        this.hasSave = false;
        this.hasMenu = false;
        this.menuItems = [];
        this.menuClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.fullScreenClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.dateMenuItems = [];
    }
    TileActionsComponent.prototype.ngOnInit = function () {
    };
    TileActionsComponent.prototype.ngOnChanges = function (changes) {
        this.buildMenu();
    };
    TileActionsComponent.prototype.buildMenu = function () {
        var _this = this;
        if (this.hasDate) {
            this.dateMenuItems.push({
                icon: 'fa-clock-o',
                items: [
                    { label: 'Past Week', command: function () { return _this.dateClick.emit({ days: 7 }); } },
                    { label: 'Past Month', command: function () { return _this.dateClick.emit({ days: 30 }); } },
                    { label: 'Past Year', command: function () { return _this.dateClick.emit({ days: 365 }); } },
                    { label: 'All', command: function () { return _this.dateClick.emit({ days: 0 }); } }
                ]
            });
        }
        if (this.hasMenu && this.menuItems.length > 0) {
            this.setMenuItemCommands(this.menuItems);
        }
    };
    TileActionsComponent.prototype.setMenuItemCommands = function (items) {
        var _this = this;
        items.forEach(function (i) {
            i.command = function () { return _this.menuClick.emit(i); };
            if (i.items && i.items.length > 0) {
                _this.setMenuItemCommands(i.items);
            }
        });
    };
    TileActionsComponent.prototype.filterClick = function () {
        this.filterMode = !this.filterMode;
        this.filterModeChange.emit(this.filterMode);
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "addClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "exportClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "exportErrorsClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "exportOriginalClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "editClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "dateClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "closeClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "refreshClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "authenticateClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "apiClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "passwordClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "suggestClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "saveClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "filterMode", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "filterModeChange", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasAdd", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasExport", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasExportErrors", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasExportOriginal", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasEdit", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasDate", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasClose", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasFilterMode", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasRefresh", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasAuthenticate", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasApi", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasPassword", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasFullScreen", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasSuggest", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasSave", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], TileActionsComponent.prototype, "hasMenu", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Array)
    ], TileActionsComponent.prototype, "menuItems", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "menuClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], TileActionsComponent.prototype, "fullScreenClick", void 0);
    TileActionsComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-tile-actions',
            styles: ["\n     :host{\n            text-transform:none;\n        }\n    \n  "],
            template: "\n                <div class=\"TileTools\"> \n                    <p-menubar *ngIf=\"hasDate\" [model]=\"dateMenuItems\"></p-menubar><!--workaround to position bug in menu-->\n                    <p-menubar *ngIf=\"hasMenu && menuItems.length > 0\" [model]=\"menuItems\"></p-menubar>\n                    <div *ngIf=\"!hasDate && !hasMenu\">\n                        <ul>                                                      \n                            <li class=\"left\" *ngIf=\"hasAdd\"><a class=\"Action\" (click)=\"addClick.emit(null)\" pTooltip=\"Add\"><i class=\"fa fa-plus fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasSuggest\"><a class=\"Action\" (click)=\"suggestClick.emit(null)\" pTooltip=\"Suggest\"><i class=\"fa fa-commenting fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasExport\"><a class=\"Action\" (click)=\"exportClick.emit(null)\" pTooltip=\"Export to Excel\"><i class=\"fa fa-download fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasExportErrors\"><a class=\"Action\" (click)=\"exportErrorsClick.emit(null)\" pTooltip=\"Export Errors to Excel\"><i class=\"fa fa-download red-text fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasExportOriginal\"><a class=\"Action\" (click)=\"exportOriginalClick.emit(null)\" pTooltip=\"Export Original Spreadsheet\"><i class=\"fa fa-download blue-text fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasEdit\"><a class=\"Action\" (click)=\"editClick.emit(null)\" pTooltip=\"Edit\"><i class=\"fa fa-pencil fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasSave\"><a class=\"Action\" (click)=\"saveClick.emit(null)\" pTooltip=\"Save\"><i class=\"fa fa-floppy-o fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasClose\"><a class=\"Action\" (click)=\"closeClick.emit(null)\" pTooltip=\"Close\"><i class=\"fa fa-remove fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasFilterMode\"><a class=\"Action\" (click)=\"filterClick()\" pTooltip=\"Filter Mode\">\n                                <i class=\"fa fa-filter fa-fw\" [ngClass]=\"{'red-text darken-2':!filterMode}\"></i>                                \n                            </a></li>\n                            <li class=\"left\" *ngIf=\"hasRefresh\"><a class=\"Action\" (click)=\"refreshClick.emit()\" pTooltip=\"Refresh\"><i class=\"fa fa-refresh fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasAuthenticate\"><a class=\"Action\" (click)=\"authenticateClick.emit()\" pTooltip=\"Authenticate\"><i class=\"fa fa-sign-in fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasApi\"><a class=\"Action\" (click)=\"apiClick.emit()\" pTooltip=\"API Key\"><i class=\"fa fa-key fa-fw\"></i></a></li>\n                            <li class=\"left\" *ngIf=\"hasPassword\"><a class=\"Action\" (click)=\"passwordClick.emit()\" pTooltip=\"Password\"><i class=\"fa fa-asterisk fa-fw\"></i></a></li>                        \n                            <li class=\"left\" *ngIf=\"hasFullScreen\"><a class=\"Action\" (click)=\"fullScreenClick.emit()\" pTooltip=\"Fullscreen\"><i class=\"fa fa-arrows-alt fa-fw\"></i></a></li>                        \n                        </ul>\n                    </div>\n                </div>          \n                ",
            changeDetection: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectionStrategy"].OnPush
        }), 
        __metadata('design:paramtypes', [])
    ], TileActionsComponent);
    return TileActionsComponent;
}());


/***/ },

/***/ 1193:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__ = __webpack_require__(1168);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowAssignmentsComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var WorkflowAssignmentsComponent = (function (_super) {
    __extends(WorkflowAssignmentsComponent, _super);
    function WorkflowAssignmentsComponent(workflowService, resourcesService) {
        _super.call(this);
        this.workflowService = workflowService;
        this.resourcesService = resourcesService;
        this.resourceId = -1;
        this.showItemDetail = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.counts = [];
        this.daysToLookBack = 7;
        this.isLoaded = false;
        this.resource = null;
    }
    WorkflowAssignmentsComponent.prototype.ngOnInit = function () {
        if (!this.isLoaded)
            this.load();
    };
    WorkflowAssignmentsComponent.prototype.load = function () {
        var _this = this;
        this.isLoading = true;
        var loadResource = (this.resourceId != null && this.resourceId >= 0);
        this.workflowService.getMyCounts(this.daysToLookBack, (loadResource ? this.resourceId : null))
            .then(function (res) {
            _this.counts = res.filter(function (item) { return (item.Total > 0); });
            if (loadResource)
                _this.resourcesService.getResource(_this.resourceId)
                    .then(function (r) {
                    _this.resource = r;
                    _this.isLoading = false;
                    _this.isLoaded = true;
                });
            else {
                _this.isLoading = false;
                _this.isLoaded = true;
            }
        });
    };
    WorkflowAssignmentsComponent.prototype.doSelect = function (item) {
        this.showItemDetail.emit({
            workflowType: this.getWorkflowType(item)
        });
    };
    WorkflowAssignmentsComponent.prototype.getWorkflowType = function (item) {
        if (!item)
            return null;
        switch (item.Name.toUpperCase()) {
            case "CERTIFY ARTIFACT":
                return __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["a" /* WorkflowType */].CertifyArtifact;
            case "CHALLENGE":
                return __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["a" /* WorkflowType */].ChallengeArtifact;
            case "PROPOSE NEW ARTIFACT":
                return __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["a" /* WorkflowType */].SuggestNewArtifact;
            case "ISSUES":
                return __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["a" /* WorkflowType */].WorkIssue;
        }
        return null;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Object)
    ], WorkflowAssignmentsComponent.prototype, "resourceId", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowAssignmentsComponent.prototype, "showItemDetail", void 0);
    WorkflowAssignmentsComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-assignments',
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */], __WEBPACK_IMPORTED_MODULE_2__services_index__["e" /* ResourcesService */]],
            template: "\n                <div class=\"tile tile-detail\">\n                   <header *ngIf=\"resourceId >= 0\">{{resource?.FirstName}}'s Assignments\n                    <d3s-tile-actions [hasAdd]=\"false\"></d3s-tile-actions>                            \n                   </header>\n                   <header *ngIf=\"resourceId == null || resourceId < 0\">Your Assignments\n                    <d3s-tile-actions [hasAdd]=\"false\"></d3s-tile-actions>                            \n                   </header>\n                    <d3s-loading [isLoading]=\"isLoading\"></d3s-loading>\n                    <p-dataTable *ngIf=\"!isLoading && counts.length > 0\" sortField=\"Name\" [sortOrder]=\"1\" [value]=\"counts\" selectionMode=\"single\" [(selection)]=\"selected\" (onRowDblclick)=\"selected=$event.data;doSelect(selected)\" >                    \n                        <p-column field=\"Name\" header=\"Name\" [sortable]=\"true\">\n                            <template let-item=\"rowData\" pTemplate type=\"body\">\n                                    <a (click)=\"doSelect(item)\">{{item.Name}}</a>\n                            </template>\n                        </p-column>           \n                        <p-column field=\"Total\" header=\"Count\" [sortable]=\"true\" [style]=\"{'text-align':'center'}\"></p-column>                                                                \n                    </p-dataTable>                      \n                    <div *ngIf=\"counts.length == 0 && !isLoading\" style=\"padding:10px\">You currently have no assignments</div>\n                </div>\n                "
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["e" /* ResourcesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["e" /* ResourcesService */]) === 'function' && _b) || Object])
    ], WorkflowAssignmentsComponent);
    return WorkflowAssignmentsComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1194:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_3_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowCertifyDetailsComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var WorkflowCertifyDetailsComponent = (function (_super) {
    __extends(WorkflowCertifyDetailsComponent, _super);
    function WorkflowCertifyDetailsComponent(workflowService) {
        _super.call(this);
        this.workflowService = workflowService;
        this.items = [];
        this.showEditor = false;
        this.objectID = 0;
        this.hasCloseButton = true;
        this.hasCertifyButton = true;
        this.close = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    WorkflowCertifyDetailsComponent.prototype.ngOnInit = function () {
        this.loadCertifications();
    };
    WorkflowCertifyDetailsComponent.prototype.loadCertifications = function () {
        var _this = this;
        this.isLoading = true;
        this.workflowService.getCertifyItems(this.objectID, this.objectType)
            .then(function (result) {
            _this.items = result;
            if (_this.items.length && _this.items.length > 0)
                _this.selected = _this.items[0];
            _this.isLoading = false;
        });
    };
    WorkflowCertifyDetailsComponent.prototype.handleSave = function () {
        this.showEditor = false;
        this.loadCertifications();
    };
    WorkflowCertifyDetailsComponent.prototype.handleRowDblClick = function () {
        if (this.selected.Activity > 0)
            this.showEditor = true;
    };
    WorkflowCertifyDetailsComponent.prototype.columnSort = function (event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.items = __WEBPACK_IMPORTED_MODULE_3_lodash__["orderBy"](this.items, [function (item) { return item[event.field] ? item[event.field].toLowerCase() : item[event.field]; }], [event.order == -1 ? 'desc' : 'asc']);
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], WorkflowCertifyDetailsComponent.prototype, "objectID", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], WorkflowCertifyDetailsComponent.prototype, "objectType", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], WorkflowCertifyDetailsComponent.prototype, "objectName", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], WorkflowCertifyDetailsComponent.prototype, "hasCloseButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], WorkflowCertifyDetailsComponent.prototype, "hasCertifyButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowCertifyDetailsComponent.prototype, "close", void 0);
    WorkflowCertifyDetailsComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-certify-details',
            template: "     \n            <d3s-workflow-certify-editor *ngIf=\"!isLoading && showEditor\" [certify]=\"selected\" (saveClick)=\"handleSave();\" (closeClick)=\"showEditor=false\"></d3s-workflow-certify-editor>       \n            <div class=\"row\" *ngIf=\"!isLoading && !showEditor\">\n                <header>Open Artifact Certifications<d3s-tile-actions [hasAdd]=\"false\" [hasFilterMode]=\"true\" [(filterMode)]=\"showSimpleFilter\"></d3s-tile-actions></header>\n                <div class=\"col s12\">     \n                    <input #gb [hidden]=\"!showSimpleFilter\" type=\"text\" pInputText size=\"100\" placeholder=\"Search...\" class=\"grid-simple-filter\">                                  \n                    <p-dataTable #dt [globalFilter]=\"gb\" scrollable=\"true\" scrollWidth=\"100%\" [rows]=\"defaultInitialItemsPerPage\" [rowsPerPageOptions]=\"defaultPagingOptions\" [value]=\"items\" selectionMode=\"single\" paginator=\"true\" pageLinks=\"3\" [(selection)]=\"selected\"  (onRowDblclick)=\"selected=$event.data;handleRowDblClick();\" >\n                        <footer *ngIf=\"dt.totalRecords\"><d3s-grid-paging-info [totalRecords]=\"dt.totalRecords\" [first]=\"dt.first\" [rows]=\"dt.rows\"></d3s-grid-paging-info></footer>\n                        <p-column field=\"ActivityName\" header=\"Status\" sortable=\"custom\" (sortFunction)=\"columnSort($event)\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-data=\"rowData\" pTemplate type=\"body\">\n                                <span *ngIf=\"data.Activity <= 0\">{{data.ActivityName}}</span>\n                                <a *ngIf=\"data.Activity > 0\" (click)=\"selected=data;showEditor=true\">{{data.ActivityName}}</a>\n                            </template>\n                        </p-column>\n                        <p-column field=\"TypeName\" header=\"Type Name\" [sortable]=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\"></p-column>\n                        <p-column field=\"Name\" header=\"Name\" sortable=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-item=\"rowData\" pTemplate type=\"body\">\n                                <d3s-tooltip objectType=\"Artifact\" [objectId]=\"item.ID\" tooltipType=\"preview\">{{item.Name}}</d3s-tooltip>                                \n                            </template>\n                        </p-column>                                                                        \n                        <p-column field=\"StartDate\" header=\"Created\" sortable=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-data=\"rowData\" pTemplate type=\"body\">\n                                <span>{{data.StartDate | date: 'medium'}}</span>\n                            </template>\n                        </p-column>\n                        <p-column field=\"DueDate\" header=\"Due\" sortable=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-data=\"rowData\" pTemplate type=\"body\">\n                                <span>{{data.DueDate | date: 'medium'}}</span>\n                            </template>\n                        </p-column>                        \n                        <p-column  *ngIf=\"hasCertifyButton\" [style]=\"{width:'40px'}\">\n                            <template let-item=\"rowData\" pTemplate type=\"body\">\n                                <div class=\"RowTools\" *ngIf=\"item.Activity > 0\">                                \n                                    <a style=\"cursor:pointer;\" (click)=\"showEditor=true\"><i class=\"fa fa-check-circle-o\"></i></a>                                    \n                                </div>\n                            </template>\n                        </p-column>                            \n                    </p-dataTable>   \n                </div>\n                <div class=\"col s12\">\n                    <button *ngIf=\"hasCloseButton\" pButton type=\"button\" (click)=\"close.emit();\" label=\"Close\" style=\"width: 150px;\"></button>\n                </div>  \n            </div>                        \n        ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]) === 'function' && _a) || Object])
    ], WorkflowCertifyDetailsComponent);
    return WorkflowCertifyDetailsComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1195:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__ = __webpack_require__(1168);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowCertifyEditorComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var WorkflowCertifyEditorComponent = (function (_super) {
    __extends(WorkflowCertifyEditorComponent, _super);
    function WorkflowCertifyEditorComponent(workflowService) {
        _super.call(this);
        this.workflowService = workflowService;
        this.closeClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.saveClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    WorkflowCertifyEditorComponent.prototype.onSubmit = function () {
        var _this = this;
        this.isLoading = true;
        this.workflowService.certifyArtifact(this.certify).then(function (res) {
            _this.isLoading = false;
            _this.saveClick.emit();
        });
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["b" /* CertifyItem */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["b" /* CertifyItem */]) === 'function' && _a) || Object)
    ], WorkflowCertifyEditorComponent.prototype, "certify", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowCertifyEditorComponent.prototype, "closeClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowCertifyEditorComponent.prototype, "saveClick", void 0);
    WorkflowCertifyEditorComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-certify-editor',
            template: " \n                <form (ngSubmit)=\"onSubmit()\" #certifyEditorForm=\"ngForm\">\n                <header>Certify Artifact</header>\n                <div id=\"FormDescription\" class=\"form-instructions\">The workflow that is triggered when an owner must certify an artifact to validate that all data is correct.</div>                \n                <div class=\"row\">                    \n                    <div class=\"col s12 l6\">\n                        <div class=\"FieldName\">Type</div>\n                        <div [innerHtml]=\"certify?.TypeName\"></div>\n                        <div class=\"FieldName\"><d3s-tooltip [objectType]=\"'Artifact'\" [objectId]=\"certify.ID\" [tooltipType]=\"'preview'\">{{certify.Name}}</d3s-tooltip></div>\n                        <div>{{certify?.Name}}</div>\n                        <div class=\"FieldName\">Start Date</div>\n                        <div>{{certify?.StartDate | date: 'medium'}}</div>\n                        <div class=\"FieldName\">Start Date</div>\n                        <div>{{certify?.DueDate | date: 'medium'}}</div>\n                    </div>\n                    <div class=\"col s12 l6\">      \n                        <div id=\"PoolMessage\">\n                           By clicking the Certify button below, I certify that all data on this item is correct.\n                        </div>                                                                                                                 \n                    </div>                    \n                </div>\n                <div class=\"row\">\n                    <div class=\"col s12\">&nbsp;</div>\n                    <div class=\"col s12\">\n                        <button pButton type=\"submit\" [disabled]=\"!certifyEditorForm.form.valid\" style=\"width: 150px;\" label=\"Certify\"></button>                            \n                        <button pButton type=\"button\" (click)=\"closeClick.emit();\" label=\"Close\" style=\"width: 150px;\"></button>\n                    </div>\n                </div>\n                </form>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]) === 'function' && _b) || Object])
    ], WorkflowCertifyEditorComponent);
    return WorkflowCertifyEditorComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1196:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_workflow_model__ = __webpack_require__(1168);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowDetailComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var WorkflowDetailComponent = (function (_super) {
    __extends(WorkflowDetailComponent, _super);
    function WorkflowDetailComponent() {
        _super.apply(this, arguments);
        this.hasCloseButton = true;
        this.hasCertifyButton = true;
        this.close = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.tempWorkflowtype = __WEBPACK_IMPORTED_MODULE_2__models_workflow_model__["a" /* WorkflowType */];
    }
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__models_workflow_model__["a" /* WorkflowType */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__models_workflow_model__["a" /* WorkflowType */]) === 'function' && _a) || Object)
    ], WorkflowDetailComponent.prototype, "workflowType", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], WorkflowDetailComponent.prototype, "hasCloseButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], WorkflowDetailComponent.prototype, "hasCertifyButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowDetailComponent.prototype, "close", void 0);
    WorkflowDetailComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-detail',
            template: " \n                <d3s-workflow-issue-details *ngIf=\"workflowType == tempWorkflowtype.WorkIssue\" [hasCloseButton]=\"hasCloseButton\" [hasCertifyButton]=\"hasCertifyButton\" (close)=\"close.emit({});\"></d3s-workflow-issue-details>                    \n                <d3s-workflow-suggest-details *ngIf=\"workflowType == tempWorkflowtype.SuggestNewArtifact\" [hasCloseButton]=\"hasCloseButton\" [hasCertifyButton]=\"hasCertifyButton\" (close)=\"close.emit({});\"></d3s-workflow-suggest-details>                    \n                <d3s-workflow-certify-details *ngIf=\"workflowType == tempWorkflowtype.CertifyArtifact\" [hasCloseButton]=\"hasCloseButton\" [hasCertifyButton]=\"hasCertifyButton\" (close)=\"close.emit({});\"></d3s-workflow-certify-details>                                  \n                ",
        }), 
        __metadata('design:paramtypes', [])
    ], WorkflowDetailComponent);
    return WorkflowDetailComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1197:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowDetailedViewComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var WorkflowDetailedViewComponent = (function (_super) {
    __extends(WorkflowDetailedViewComponent, _super);
    function WorkflowDetailedViewComponent(workflowService) {
        _super.call(this);
        this.workflowService = workflowService;
    }
    WorkflowDetailedViewComponent.prototype.ngOnChanges = function (changes) {
        if (changes['workflowId'] && this.workflowId) {
            this.load();
        }
    };
    WorkflowDetailedViewComponent.prototype.load = function () {
        var _this = this;
        this.isLoading = true;
        this.workflowService.getWorkflowStatus(this.workflowId)
            .then(function (result) {
            _this.workflowStatusData = result;
            _this.isLoading = false;
        });
    };
    WorkflowDetailedViewComponent.prototype.isDateField = function (field) {
        return field.toUpperCase().indexOf('DATE') >= 0;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], WorkflowDetailedViewComponent.prototype, "workflowId", void 0);
    WorkflowDetailedViewComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-detailed-view',
            template: "\n            <d3s-loading [isLoading]=\"isLoading\"></d3s-loading>\n            <div *ngIf=\"!isLoading\">\n                <div class=\"row\" *ngFor=\"let field of workflowStatusData?.Fields\">                    \n                    <div class=\"col s6\">\n                        {{field.Name}}\n                    </div>\n                    <div class=\"col s6\" *ngIf=\"!isDateField(field.Name)\" [innerHtml]=\"field.Value\"></div>                    \n                    <div class=\"col s6\" *ngIf=\"isDateField(field.Name)\">{{field.Value | date : 'short'}}</div>                    \n                </div>\n                <div class=\"row\">&nbsp;</div>                \n                <p-dataTable #dt [rows]=\"defaultInitialItemsPerPage\" [rowsPerPageOptions]=\"defaultPagingOptions\" paginator=\"true\" pageLinks=\"3\" [value]=\"workflowStatusData?.Assignments\" selectionMode=\"single\" scrollable=\"true\" scrollWidth=\"100%\" >                    \n                    <footer *ngIf=\"dt.totalRecords\"><d3s-grid-paging-info [totalRecords]=\"dt.totalRecords\" [first]=\"dt.first\" [rows]=\"dt.rows\"></d3s-grid-paging-info></footer>\n                    <p-column field=\"ActivityTypeName\" header=\"Activity\" sortable=\"true\"></p-column>                                \n                    <p-column field=\"ResourceID\" header=\"User\" sortable=\"true\">\n                        <template let-item=\"rowData\" pTemplate type=\"body\">\n                            <span><d3s-tooltip objectType=\"Resource\" [objectId]=\"item.ResourceID\" tooltipType=\"preview\">{{item.ResourceName}}</d3s-tooltip></span>\n                        </template>\n                    </p-column>\n                    <p-column field=\"IsComplete\" header=\"Completed?\" sortable=\"true\">\n                        <template let-activity=\"rowData\" pTemplate type=\"body\">\n                            <span><i class=\"fa fa-times disabled\" *ngIf=\"!activity.IsComplete\"></i><i class=\"fa fa-check enabled\" *ngIf=\"activity.IsComplete\"></i></span>\n                        </template>\n                    </p-column>\n                </p-dataTable>                \n            </div>\n        ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]) === 'function' && _a) || Object])
    ], WorkflowDetailedViewComponent);
    return WorkflowDetailedViewComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1198:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_3_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowIssueDetailsComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var WorkflowIssueDetailsComponent = (function (_super) {
    __extends(WorkflowIssueDetailsComponent, _super);
    function WorkflowIssueDetailsComponent(workflowService) {
        _super.call(this);
        this.workflowService = workflowService;
        this.issues = [];
        this.loaded = false;
        this.showEditor = false;
        this.objectID = 0;
        this.hasCloseButton = false;
        this.hasCertifyButton = false;
        this.close = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.countsChanged = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    WorkflowIssueDetailsComponent.prototype.ngOnInit = function () {
        if (!this.loaded)
            this.loadIssues();
    };
    WorkflowIssueDetailsComponent.prototype.loadIssues = function () {
        var _this = this;
        this.isLoading = true;
        this.workflowService.getIssues(this.objectID, this.objectType)
            .then(function (result) {
            _this.issues = result;
            if (_this.issues.length && _this.issues.length > 0)
                _this.selected = _this.issues[0];
            _this.isLoading = false;
            _this.loaded = true;
        });
    };
    WorkflowIssueDetailsComponent.prototype.handleSave = function () {
        this.showEditor = false;
        this.loadIssues();
        this.countsChanged.emit();
    };
    WorkflowIssueDetailsComponent.prototype.handleRowDblClick = function () {
        if (this.selected.Activity > 0)
            this.showEditor = true;
    };
    WorkflowIssueDetailsComponent.prototype.columnSort = function (event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.issues = __WEBPACK_IMPORTED_MODULE_3_lodash__["orderBy"](this.issues, [function (item) { return item[event.field] ? item[event.field].toLowerCase() : item[event.field]; }], [event.order == -1 ? 'desc' : 'asc']);
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], WorkflowIssueDetailsComponent.prototype, "objectID", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], WorkflowIssueDetailsComponent.prototype, "objectType", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], WorkflowIssueDetailsComponent.prototype, "objectName", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], WorkflowIssueDetailsComponent.prototype, "hasCloseButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], WorkflowIssueDetailsComponent.prototype, "hasCertifyButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowIssueDetailsComponent.prototype, "close", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowIssueDetailsComponent.prototype, "countsChanged", void 0);
    WorkflowIssueDetailsComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-issue-details',
            template: "\n            <d3s-workflow-issue-editor *ngIf=\"!isLoading && showEditor\" [issue]=\"selected\" (saveClick)=\"handleSave();\" (closeClick)=\"showEditor=false\"></d3s-workflow-issue-editor>\n            <div class=\"row\" *ngIf=\"!isLoading && issues.length > 0 && !showEditor\">\n                <header>Open Issues<d3s-tile-actions [hasAdd]=\"false\" hasFilterMode=\"true\" [(filterMode)]=\"showSimpleFilter\"></d3s-tile-actions></header>\n                <div class=\"col s12\"> \n                    <input #gb [hidden]=\"!showSimpleFilter\" type=\"text\" pInputText size=\"100\" placeholder=\"Search...\" class=\"grid-simple-filter\">                                       \n                    <p-dataTable #dt [globalFilter]=\"gb\" scrollable=\"true\" scrollWidth=\"100%\" [rowsPerPageOptions]=\"defaultPagingOptions\" [value]=\"issues\" selectionMode=\"single\" [rows]=\"defaultInitialItemsPerPage\" paginator=\"true\" pageLinks=\"3\" [(selection)]=\"selected\" (onRowDblclick)=\"selected=$event.data;handleRowDblClick();\" >\n                        <footer *ngIf=\"dt.totalRecords\"><d3s-grid-paging-info [totalRecords]=\"dt.totalRecords\" [first]=\"dt.first\" [rows]=\"dt.rows\"></d3s-grid-paging-info></footer>\n                        <p-column field=\"ActivityName\" header=\"Status\" sortable=\"custom\" (sortFunction)=\"columnSort($event)\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-data=\"rowData\" pTemplate type=\"body\">\n                                <span *ngIf=\"data.Activity <= 0\">{{data.ActivityName}}</span>\n                                <a *ngIf=\"data.Activity > 0\" (click)=\"selected=data;showEditor=true\">{{data.ActivityName}}</a>\n                            </template>\n                        </p-column>\n                        <p-column field=\"IssueTypeName\" header=\"Type\" sortable=\"true\" [style]=\"{'width':'150px'}\" [filter]=\"!showSimpleFilter\"></p-column>\n                        <p-column field=\"Issue\" header=\"Issue\" [sortable]=\"false\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-issue=\"rowData\" pTemplate type=\"body\">\n                                <span [innerHtml]=\"issue?.Issue\"></span>\n                            </template>\n                        </p-column>\n                        <p-column field=\"ResourceName\" header=\"Reported By\" sortable=\"custom\" (sortFunction)=\"columnSort($event)\"  [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\"></p-column>\n                        <p-column field=\"DateStarted\" header=\"Created\" sortable=\"custom\" (sortFunction)=\"columnSort($event)\"  [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-data=\"rowData\" pTemplate type=\"body\">\n                                <span>{{data.DateStarted | date: 'medium'}}</span>\n                            </template>\n                        </p-column>                        \n                        <p-column  *ngIf=\"hasCertifyButton\" [style]=\"{width:'40px'}\">\n                            <template let-issue=\"rowData\" pTemplate type=\"body\">\n                                <div class=\"RowTools\" *ngIf=\"issue.Activity > 0\">                                \n                                    <a style=\"cursor:pointer;\" (click)=\"showEditor=true\"><i class=\"fa fa-check-circle-o\"></i></a>                                    \n                                </div>\n                            </template>\n                        </p-column>                            \n                    </p-dataTable>   \n                </div>\n            </div>            \n            <div style=\"min-height:100px\" *ngIf=\"!isLoading && issues.length == 0\">\n                <h4 *ngIf=\"objectName\">No issues currently exist for <b>{{objectName}}</b>.</h4>\n                <h4 *ngIf=\"!objectName\">No issues assigned.</h4>\n            </div>\n            <div style=\"padding:10px\" *ngIf=\"!showEditor\">\n                <button *ngIf=\"hasCloseButton\" pButton type=\"button\" (click)=\"close.emit();\" label=\"Close\" style=\"width: 150px;\"></button>\n            </div>  \n            \n        ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]) === 'function' && _a) || Object])
    ], WorkflowIssueDetailsComponent);
    return WorkflowIssueDetailsComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1199:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__ = __webpack_require__(1168);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowIssueEditorComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var WorkflowIssueEditorComponent = (function (_super) {
    __extends(WorkflowIssueEditorComponent, _super);
    function WorkflowIssueEditorComponent(resourcesService, workflowService, messagesService) {
        _super.call(this);
        this.resourcesService = resourcesService;
        this.workflowService = workflowService;
        this.messagesService = messagesService;
        this.closeClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.saveClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.resources = [];
        this.comments = "";
    }
    WorkflowIssueEditorComponent.prototype.ngOnInit = function () {
        if (this.resources.length <= 0) {
            this.loadResources();
        }
    };
    WorkflowIssueEditorComponent.prototype.loadResources = function () {
        var _this = this;
        this.isLoading = true;
        this.resourcesService.getResources()
            .then(function (res) {
            _this.isLoading = false;
            _this.resources = res;
        });
    };
    WorkflowIssueEditorComponent.prototype.onSubmit = function () {
        var _this = this;
        this.isLoading = true;
        this.workflowService.updateIssue(this.issue, this.action, this.comments, this.assignToId).then(function (res) {
            _this.showMessageForResult(_this.messagesService, res);
            _this.isLoading = false;
            _this.saveClick.emit();
        });
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["d" /* Issue */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["d" /* Issue */]) === 'function' && _a) || Object)
    ], WorkflowIssueEditorComponent.prototype, "issue", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowIssueEditorComponent.prototype, "closeClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowIssueEditorComponent.prototype, "saveClick", void 0);
    WorkflowIssueEditorComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-issue-editor',
            template: " \n                <form (ngSubmit)=\"onSubmit()\" #issueEditorForm=\"ngForm\">\n                <header>Work Issue</header>\n                <div id=\"FormDescription\" class=\"form-instructions\">The workflow that is triggered when an issue is reported.  The owner is assigned as a potential resource to work on the issue. They must still choose to work the issue.</div>                \n                <div class=\"row\">                    \n                    <div class=\"col s12 l6\">\n                        <div class=\"FieldName\">Issue</div>\n                        <div [innerHtml]=\"issue?.Issue\"></div>\n                        <div class=\"FieldName\">Issue Type</div>\n                        <div>{{issue?.IssueTypeName}}</div>                        \n                        <div class=\"FieldName\">Requestor</div>\n                        <div>{{issue?.ResourceName}}</div>\n                        <div class=\"FieldName\">Date</div>\n                        <div>{{issue?.DateStarted | date: 'medium'}}</div>\n                    </div>\n                    <div class=\"col s12 l6\">      \n                        <div id=\"PoolMessage\">\n                            By clicking Save below, you are assigning yourself to this issue.  Please provide a comment below.  As you are working on this issue,\n                            you may also comment on the issue where it is listed on your Board.\n                        </div>\n                        <div class=\"row\">                                          \n                            <div class=\"col s12 m4 l4\" *ngIf=\"issue?.Activity == 3\">\n                                <label><input required type=\"radio\" name=\"Action\" [(ngModel)]=\"action\" value=\"assign\" checked=\"checked\" />Accept Assignment</label>\n                            </div>\n                            <div class=\"col s12 m4 l4\" *ngIf=\"issue?.Activity != 3\">\n                                <label><input required type=\"radio\" [(ngModel)]=\"action\" name=\"Action\" value=\"reassign\" />Re-assign</label>                                \n                                <select name=\"reassignTo\" style=\"width:100%;\" [(ngModel)]=\"assignToId\" [disabled]=\"action != 'reassign'\">\n                                      <option></option>\n                                      <option *ngFor=\"let p of resources\" [value]=\"p.ID\">{{p.FirstName}} {{p.LastName}}</option>\n                                </select>\n                            </div>\n                            <div class=\"col s12 m4 l4\" *ngIf=\"issue?.Activity != 3\">\n                                <label><input required type=\"radio\" [(ngModel)]=\"action\" name=\"Action\" value=\"close\" checked=\"checked\"/>Close</label>\n                            </div>     \n                        </div>\n                        <div id=\"CommentArea\">\n                            <div class=\"FieldName\">Comments</div>\n                            <textarea name=\"Comment\" [(ngModel)]=\"comments\"></textarea>\n                        </div>                                                                                           \n                    </div>                    \n                </div>\n                <div class=\"row\">\n                    <div class=\"col s12\">&nbsp;</div>\n                    <div class=\"col s12\">\n                        <button pButton type=\"submit\" [disabled]=\"!issueEditorForm.form.valid\" label=\"Save\"></button>                            \n                        <button pButton type=\"button\" (click)=\"closeClick.emit();\" label=\"Cancel\"></button>\n                    </div>\n                </div>\n                </form>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["e" /* ResourcesService */], __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["e" /* ResourcesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["e" /* ResourcesService */]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["a" /* MessagesService */]) === 'function' && _d) || Object])
    ], WorkflowIssueEditorComponent);
    return WorkflowIssueEditorComponent;
    var _a, _b, _c, _d;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1200:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_3_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowSuggestDetailsComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var WorkflowSuggestDetailsComponent = (function (_super) {
    __extends(WorkflowSuggestDetailsComponent, _super);
    function WorkflowSuggestDetailsComponent(workflowService) {
        _super.call(this);
        this.workflowService = workflowService;
        this.items = [];
        this.showEditor = false;
        this.objectID = 0;
        this.hasCloseButton = true;
        this.hasCertifyButton = true;
        this.close = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    WorkflowSuggestDetailsComponent.prototype.ngOnInit = function () {
        this.loadSuggestions();
    };
    WorkflowSuggestDetailsComponent.prototype.loadSuggestions = function () {
        var _this = this;
        this.isLoading = true;
        this.workflowService.getSuggestedItems(this.objectID, this.objectType)
            .then(function (result) {
            _this.items = result;
            if (_this.items.length && _this.items.length > 0)
                _this.selected = _this.items[0];
            _this.isLoading = false;
        });
    };
    WorkflowSuggestDetailsComponent.prototype.handleSave = function () {
        this.showEditor = false;
        this.loadSuggestions();
    };
    WorkflowSuggestDetailsComponent.prototype.handleRowDblClick = function () {
        if (this.selected.Activity > 0)
            this.showEditor = true;
    };
    WorkflowSuggestDetailsComponent.prototype.columnSort = function (event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.items = __WEBPACK_IMPORTED_MODULE_3_lodash__["orderBy"](this.items, [function (item) { return item[event.field] ? item[event.field].toLowerCase() : item[event.field]; }], [event.order == -1 ? 'desc' : 'asc']);
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], WorkflowSuggestDetailsComponent.prototype, "objectID", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], WorkflowSuggestDetailsComponent.prototype, "objectType", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], WorkflowSuggestDetailsComponent.prototype, "objectName", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], WorkflowSuggestDetailsComponent.prototype, "hasCloseButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], WorkflowSuggestDetailsComponent.prototype, "hasCertifyButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowSuggestDetailsComponent.prototype, "close", void 0);
    WorkflowSuggestDetailsComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-suggest-details',
            template: "        \n            <d3s-workflow-suggest-editor *ngIf=\"!isLoading && showEditor\" [suggest]=\"selected\" (saveClick)=\"handleSave();\" (closeClick)=\"showEditor=false\"></d3s-workflow-suggest-editor>           \n            <div class=\"row\" *ngIf=\"!isLoading && !showEditor\">\n                <header>Open Proposed New Artifacts<d3s-tile-actions [hasAdd]=\"false\" [hasFilterMode]=\"true\" [(filterMode)]=\"showSimpleFilter\"></d3s-tile-actions></header>\n                <div class=\"col s12\">                    \n                    <input #gb [hidden]=\"!showSimpleFilter\" type=\"text\" pInputText size=\"100\" placeholder=\"Search...\" class=\"grid-simple-filter\">                   \n                    <p-dataTable #dt [globalFilter]=\"gb\" scrollable=\"true\" scrollWidth=\"100%\" [rowsPerPageOptions]=\"defaultPagingOptions\" [value]=\"items\" selectionMode=\"single\" [rows]=\"defaultInitialItemsPerPage\" paginator=\"true\" pageLinks=\"3\" [(selection)]=\"selected\" (onRowDblclick)=\"selected=$event.data;handleRowDblClick();\" >\n                        <footer *ngIf=\"dt.totalRecords\"><d3s-grid-paging-info [totalRecords]=\"dt.totalRecords\" [first]=\"dt.first\" [rows]=\"dt.rows\"></d3s-grid-paging-info></footer>\n                        <p-column field=\"ActivityName\" header=\"Status\" sortable=\"custom\" (sortFunction)=\"columnSort($event)\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-data=\"rowData\" pTemplate type=\"body\">\n                                <span *ngIf=\"data.Activity <= 0\">{{data.ActivityName}}</span>\n                                <a *ngIf=\"data.Activity > 0\" (click)=\"selected=data;showEditor=true\">{{data.ActivityName}}</a>\n                            </template>\n                        </p-column>\n                        <p-column field=\"Name\" header=\"Type\" sortable=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-item=\"rowData\" pTemplate type=\"body\">\n                                <d3s-tooltip objectType=\"ArtifactType\" [objectId]=\"item.ID\" tooltipType=\"preview\">{{item.Name}}</d3s-tooltip>                                \n                            </template>\n                        </p-column>                        \n                        <p-column field=\"RequestingResourceName\" header=\"Requested By\" [sortable]=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-item=\"rowData\" pTemplate type=\"body\">\n                                <d3s-tooltip objectType=\"Resource\" [objectId]=\"item.RequestingResourceID\" tooltipType=\"preview\">{{item.RequestingResourceName}}</d3s-tooltip>                                \n                            </template>\n                        </p-column>\n                        <p-column field=\"StartDate\" header=\"Created\" sortable=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-data=\"rowData\" pTemplate type=\"body\">\n                                <span>{{data.StartDate | date: 'medium'}}</span>\n                            </template>\n                        </p-column>\n                        <p-column field=\"ProposedName\" header=\"Proposed Name\" sortable=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\">\n                            <template let-col let-item=\"rowData\" pTemplate type=\"body\">\n                                <span [innerHtml]=\"item?.ProposedName\"></span>\n                            </template>\n                        </p-column>\n                        <p-column field=\"TaxonomyTypeName\" header=\"Subject Area\" sortable=\"true\" [style]=\"{'width':'250px'}\" [filter]=\"!showSimpleFilter\"></p-column>                        \n                        <p-column  *ngIf=\"hasCertifyButton\" [style]=\"{width:'40px'}\">\n                            <template let-item=\"rowData\" pTemplate type=\"body\">\n                                <div class=\"RowTools\" *ngIf=\"item.Activity > 0\">                                \n                                    <a style=\"cursor:pointer;\" (click)=\"showEditor=true\"><i class=\"fa fa-check-circle-o\"></i></a>                                    \n                                </div>\n                            </template>\n                        </p-column>                            \n                    </p-dataTable>   \n                </div>\n                <div style=\"padding:10px\">\n                    <button *ngIf=\"hasCloseButton\" pButton type=\"button\" (click)=\"close.emit();\" label=\"Close\" style=\"width: 150px;\"></button>\n                </div>  \n            </div>                        \n        ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]) === 'function' && _a) || Object])
    ], WorkflowSuggestDetailsComponent);
    return WorkflowSuggestDetailsComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1201:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__ = __webpack_require__(1168);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowSuggestEditorComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var WorkflowSuggestEditorComponent = (function (_super) {
    __extends(WorkflowSuggestEditorComponent, _super);
    function WorkflowSuggestEditorComponent(workflowService) {
        _super.call(this);
        this.workflowService = workflowService;
        this.closeClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.saveClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.comments = "";
        this.action = "assign";
    }
    WorkflowSuggestEditorComponent.prototype.handleApproval = function (approved) {
        var _this = this;
        this.isLoading = true;
        this.workflowService.updateSuggestion(this.suggest, approved, this.comments).then(function (res) {
            _this.isLoading = false;
            _this.saveClick.emit();
        });
    };
    WorkflowSuggestEditorComponent.prototype.onSubmit = function () {
        this.handleApproval(true);
    };
    WorkflowSuggestEditorComponent.prototype.reject = function () {
        this.handleApproval(false);
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["c" /* SuggestedItem */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_workflow_model__["c" /* SuggestedItem */]) === 'function' && _a) || Object)
    ], WorkflowSuggestEditorComponent.prototype, "suggest", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowSuggestEditorComponent.prototype, "closeClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], WorkflowSuggestEditorComponent.prototype, "saveClick", void 0);
    WorkflowSuggestEditorComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-workflow-suggest-editor',
            template: " \n                <form (ngSubmit)=\"onSubmit()\" #suggestEditorForm=\"ngForm\">\n                <header>Suggest Artifact</header>\n                <div id=\"FormDescription\" class=\"form-instructions\"></div>                \n                <div class=\"row\">                    \n                    <div class=\"col s12 l6\">\n                        <div class=\"FieldName\">Type</div>\n                        <div><d3s-tooltip [objectType]=\"'ArtifactType'\" [objectId]=\"suggest.ID\" [tooltipType]=\"'preview'\">{{suggest.Name}}</d3s-tooltip></div>\n                        <div class=\"FieldName\">Proposed Name</div>\n                        <div [innerHtml]=\"suggest.ProposedName\"></div>\n                        <div *ngIf=\"suggest.ProposedDescription\" class=\"FieldName\">Proposed Description</div>\n                        <div *ngIf=\"suggest.ProposedDescription\" [innerHtml]=\"suggest.ProposedDescription\"></div>\n                        <div class=\"FieldName\">Requestor</div>\n                        <div><d3s-tooltip [objectType]=\"'Resource'\" [objectId]=\"suggest.RequestingResourceID\" [tooltipType]=\"'preview'\">{{suggest.RequestingResourceName}}</d3s-tooltip></div>\n                        <div class=\"FieldName\">Subject Area</div>\n                        <div>{{suggest.TaxonomyTypeName}}</div>\n                        <div class=\"FieldName\">Date</div>\n                        <div id=\"DateValue\">{{suggest?.StartDate | date: 'medium'}}</div>\n                    </div>\n                    <div class=\"col s12 l6\">                                                                              \n                        <div class=\"FieldName\">Comments</div>\n                        <textarea name=\"Comment\" [(ngModel)]=\"comments\"></textarea>\n                    </div>                    \n                </div>\n                <div class=\"row\">\n                    <div class=\"col s12\">&nbsp;</div>\n                    <div class=\"col s12\">\n                        <button pButton type=\"submit\" style=\"width: 150px;\" label=\"Approve\"></button>                            \n                        <button pButton type=\"button\" style=\"width: 150px;\" label=\"Reject\" (click)=\"reject();\"></button>\n                        <button pButton type=\"button\" (click)=\"closeClick.emit();\" label=\"Close\" style=\"width: 150px;\"></button>\n                    </div>\n                </div>\n                </form>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["d" /* WorkflowService */]) === 'function' && _b) || Object])
    ], WorkflowSuggestEditorComponent);
    return WorkflowSuggestEditorComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1202:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__workflow_component__ = __webpack_require__(1181);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__workflow_raise_issue_component__ = __webpack_require__(1178);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__workflow_work_item_component__ = __webpack_require__(1180);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__workflow_view_status_component__ = __webpack_require__(1179);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__static_site_url_helpers__ = __webpack_require__(85);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowRoutingModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};







var routes = [
    {
        path: '',
        component: __WEBPACK_IMPORTED_MODULE_2__workflow_component__["a" /* WorkflowComponent */],
        children: [
            {
                path: __WEBPACK_IMPORTED_MODULE_6__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_WORKFLOW_RAISE_ISSUE, component: __WEBPACK_IMPORTED_MODULE_3__workflow_raise_issue_component__["a" /* WorkflowRaiseIssueComponent */]
            },
            {
                path: __WEBPACK_IMPORTED_MODULE_6__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_WORKFLOW_VIEW_ITEM + '/:workflowType/:workflowId', component: __WEBPACK_IMPORTED_MODULE_4__workflow_work_item_component__["a" /* WorkflowWorkItemComponent */]
            },
            {
                path: __WEBPACK_IMPORTED_MODULE_6__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_WORKFLOW_VIEW_STATUS + '/:workflowId', component: __WEBPACK_IMPORTED_MODULE_5__workflow_view_status_component__["a" /* WorkflowViewStatusComponent */]
            }
        ]
    }
];
var WorkflowRoutingModule = (function () {
    function WorkflowRoutingModule() {
    }
    WorkflowRoutingModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"].forChild(routes)],
            exports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"]],
        }), 
        __metadata('design:paramtypes', [])
    ], WorkflowRoutingModule);
    return WorkflowRoutingModule;
}());


/***/ },

/***/ 1203:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__core_module__ = __webpack_require__(1165);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__social_board_component__ = __webpack_require__(1216);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__social_comment_component__ = __webpack_require__(1217);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__social_input_component__ = __webpack_require__(1218);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_10__social_tag_input_component__ = __webpack_require__(1219);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_11_primeng_primeng__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SocialModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};












var SocialModule = (function () {
    function SocialModule() {
    }
    SocialModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["e" /* HttpModule */],
                __WEBPACK_IMPORTED_MODULE_4__angular_router__["RouterModule"],
                //primeng        
                __WEBPACK_IMPORTED_MODULE_11_primeng_primeng__["AutoCompleteModule"],
                __WEBPACK_IMPORTED_MODULE_11_primeng_primeng__["EditorModule"],
                __WEBPACK_IMPORTED_MODULE_11_primeng_primeng__["ButtonModule"],
                //d3s
                __WEBPACK_IMPORTED_MODULE_6__core_module__["a" /* CoreModule */],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_7__social_board_component__["a" /* SocialBoardComponent */],
                __WEBPACK_IMPORTED_MODULE_8__social_comment_component__["a" /* SocialCommentComponent */],
                __WEBPACK_IMPORTED_MODULE_9__social_input_component__["a" /* SocialInputComponent */],
                __WEBPACK_IMPORTED_MODULE_10__social_tag_input_component__["a" /* SocialTagInputComponent */],
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_7__social_board_component__["a" /* SocialBoardComponent */],
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_3__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], SocialModule);
    return SocialModule;
}());


/***/ },

/***/ 1216:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_social_model__ = __webpack_require__(1182);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__static_company_settings__ = __webpack_require__(1176);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SocialBoardComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};





var SocialBoardComponent = (function (_super) {
    __extends(SocialBoardComponent, _super);
    function SocialBoardComponent(socialService) {
        _super.call(this);
        this.socialService = socialService;
        this.objectID = 0;
        this.hasCloseButton = false;
        this.hasNewInput = true;
        this.daysToLookBack = -1;
        this.countsChanged = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.close = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.rowCount = 5;
        this.pageNumber = 0;
        this.hasMore = true;
        this.comments = [];
    }
    SocialBoardComponent.prototype.ngOnInit = function () {
        if (this.objectID > 0) {
            this.socialMessage = "Comments for " + this.objectName;
        }
        else {
            if (this.limitToType == __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Social)
                this.socialMessage = "My Comment's " + this.daysMessage();
            else if (this.limitToType == __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Issue)
                this.socialMessage = "My Issue's " + this.daysMessage();
            else if (this.limitToType == __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Task)
                this.socialMessage = "My Task's " + this.daysMessage();
            else
                this.socialMessage = 'My Comments';
        }
        this.loadComments();
    };
    SocialBoardComponent.prototype.daysMessage = function () {
        return this.daysToLookBack > 0 ? ('for the last ' + this.daysToLookBack + ' days') : '- all';
    };
    SocialBoardComponent.prototype.loadComments = function () {
        var _this = this;
        this.isLoading = true;
        this.socialService.getComments(this.objectID, this.objectType, this.daysToLookBack, (this.pageNumber) * this.rowCount, this.rowCount, this.limitToType)
            .then(function (res) {
            _this.isLoading = false;
            _this.comments = _this.comments.concat(res);
            _this.hasMore = (res.length && res.length > 0);
        });
        this.pageNumber++;
    };
    SocialBoardComponent.prototype.allowComments = function () {
        return this.hasNewInput && !__WEBPACK_IMPORTED_MODULE_4__static_company_settings__["a" /* CurrentCompanySettings */].disableCommunityPosting;
    };
    SocialBoardComponent.prototype.deleteComment = function (event) {
        var _this = this;
        var comment = event.comment;
        if (!comment)
            return;
        this.isLoading = true;
        var editData = new __WEBPACK_IMPORTED_MODULE_3__models_social_model__["b" /* SocialEditCommentData */](comment, comment.Tags);
        editData.ObjectID = this.objectID;
        editData.ObjectType = this.objectType;
        editData.Comment.IsDeleted = true;
        this.socialService.editComment(editData).
            then(function (res) {
            if (res.IsDeleted) {
                var index = _this.comments.findIndex(function (x) { return x.ID == res.ID; });
                if (index >= 0) {
                    _this.comments.splice(index, 1);
                }
            }
            _this.countsChanged.emit({}); // counts changed fire event
            _this.isLoading = false;
        });
    };
    SocialBoardComponent.prototype.addComment = function (event) {
        var _this = this;
        var commentContent = event.comment;
        if (!commentContent)
            return;
        this.isLoading = true;
        var comment = new __WEBPACK_IMPORTED_MODULE_3__models_social_model__["c" /* SocialComment */]();
        comment.Body = commentContent;
        comment.CommentTypeID = __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Social;
        var addData = new __WEBPACK_IMPORTED_MODULE_3__models_social_model__["b" /* SocialEditCommentData */](comment);
        addData.ObjectID = this.objectID;
        addData.ObjectType = this.objectType;
        addData.Tags = event.tags ? event.tags : [];
        this.socialService.addComment(addData).
            then(function (res) {
            if (res) {
                _this.comments.unshift(res);
            }
            _this.countsChanged.emit({}); // counts have changed fire event
            _this.isLoading = false;
        });
    };
    SocialBoardComponent.prototype.editComment = function (event) {
        var _this = this;
        var comment = event.comment;
        if (!comment)
            return;
        this.isLoading = true;
        var editData = new __WEBPACK_IMPORTED_MODULE_3__models_social_model__["b" /* SocialEditCommentData */](comment, comment.Tags);
        editData.ObjectID = this.objectID;
        editData.ObjectType = this.objectType;
        this.socialService.editComment(editData).
            then(function (res) {
            _this.isLoading = false;
        });
    };
    SocialBoardComponent.prototype.replyToComment = function (event) {
        var _this = this;
        if (!event) {
            console.log("DEV ERROR - EVENT OBJECT IS NULL!");
            return;
        }
        var replyText = event.reply;
        var commentId = event.commentId;
        if (!replyText || !commentId)
            return;
        this.isLoading = true;
        var comment = new __WEBPACK_IMPORTED_MODULE_3__models_social_model__["c" /* SocialComment */]();
        comment.Body = replyText;
        comment.CommentTypeID = __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Social;
        comment.ParentID = commentId;
        var addData = new __WEBPACK_IMPORTED_MODULE_3__models_social_model__["b" /* SocialEditCommentData */](comment);
        addData.ObjectID = this.objectID;
        addData.ObjectType = this.objectType;
        addData.Tags = [];
        this.socialService.addComment(addData).
            then(function (res) {
            if (res) {
                var index = _this.comments.findIndex(function (x) { return x.ID == res.ParentID; });
                if (index >= 0) {
                    if (!_this.comments[index].Comments)
                        _this.comments[index].Comments = [];
                    _this.comments[index].Comments.push(res);
                }
            }
            _this.isLoading = false;
        });
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], SocialBoardComponent.prototype, "objectID", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], SocialBoardComponent.prototype, "objectType", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], SocialBoardComponent.prototype, "objectName", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], SocialBoardComponent.prototype, "hasCloseButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], SocialBoardComponent.prototype, "hasNewInput", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], SocialBoardComponent.prototype, "daysToLookBack", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */]) === 'function' && _a) || Object)
    ], SocialBoardComponent.prototype, "limitToType", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SocialBoardComponent.prototype, "countsChanged", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SocialBoardComponent.prototype, "close", void 0);
    SocialBoardComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-social-board',
            template: " \n                <div class=\"row\">\n                    <div class=\"col s12\">\n                        <header>{{socialMessage}}</header>  \n                        <d3s-social-input (commented)=\"addComment($event);\" *ngIf=\"allowComments()\"></d3s-social-input>                        \n                        <d3s-loading [isLoading]=\"isLoading\" showTransparentLoader=\"true\"></d3s-loading>\n                        <div *ngFor=\"let comment of comments\">\n                            <d3s-social-comment [comment]=\"comment\" (delete)=\"deleteComment($event);\" (reply)=\"replyToComment($event);\" (edit)=\"editComment($event);\"></d3s-social-comment>                            \n                        </div>                \n                        <div style=\"margin-top:10px;\">\n                            <button pButton type=\"button\" [disabled]=\"!hasMore\" (click)=\"loadComments();\" label=\"Load more comments...\"></button>\n                            <button *ngIf=\"hasCloseButton\" pButton type=\"button\" (click)=\"close.emit();\" label=\"Close\" style=\"width: 150px;\"></button>                    \n                        </div>\n                    </div>\n                </div>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["b" /* SocialService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["b" /* SocialService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["b" /* SocialService */]) === 'function' && _b) || Object])
    ], SocialBoardComponent);
    return SocialBoardComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_1__base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1217:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_social_model__ = __webpack_require__(1182);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__static_company_settings__ = __webpack_require__(1176);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SocialCommentComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};






var SocialCommentComponent = (function (_super) {
    __extends(SocialCommentComponent, _super);
    function SocialCommentComponent(socialService, router) {
        _super.call(this);
        this.socialService = socialService;
        this.router = router;
        this.delete = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.reply = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.edit = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.upVotes = 0;
        this.downVotes = 0;
        this.showTools = false;
        this.showReply = false;
        this.showEdit = false;
        this.replyText = "";
        this.editText = "";
        this.socialVoteType = __WEBPACK_IMPORTED_MODULE_3__models_social_model__["d" /* SocialVoteType */]; // for template to use enum
    }
    SocialCommentComponent.prototype.ngOnInit = function () {
        if (this.comment && this.comment.Votes) {
            this.calculateVotes();
        }
    };
    SocialCommentComponent.prototype.calculateVotes = function () {
        this.upVotes = this.comment.Votes.filter(function (res) { return res.Vote == __WEBPACK_IMPORTED_MODULE_3__models_social_model__["d" /* SocialVoteType */].UpVote; }).length;
        this.downVotes = this.comment.Votes.filter(function (res) { return res.Vote == __WEBPACK_IMPORTED_MODULE_3__models_social_model__["d" /* SocialVoteType */].DownVote; }).length;
    };
    SocialCommentComponent.prototype.doVote = function (vote) {
        var _this = this;
        this.socialService.vote(this.comment.ID, vote).then(function (res) {
            if (res) {
                _this.comment.Votes = res;
                _this.calculateVotes();
            }
        });
    };
    SocialCommentComponent.prototype.deleteCommentClick = function () {
        this.delete.emit({ comment: this.comment });
    };
    SocialCommentComponent.prototype.changeUrl = function (route) {
        this.router.navigate([route]);
    };
    SocialCommentComponent.prototype.commentTypeIcon = function () {
        switch (this.comment.CommentTypeID) {
            case __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Challenge:
                return "Challenge";
            case __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Issue:
                return "Issue";
            case __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Social:
                return "";
            case __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Task:
                return "Task";
        }
        return "Other";
    };
    SocialCommentComponent.prototype.handleReplyClick = function () {
        this.reply.emit({ reply: this.replyText, commentId: this.comment.ID });
        this.showReply = false;
    };
    SocialCommentComponent.prototype.handleEditClick = function () {
        this.comment.Body = this.editText;
        this.edit.emit({ comment: this.comment });
        this.showEdit = false;
    };
    SocialCommentComponent.prototype.isChallenge = function () {
        return this.comment.CommentTypeID == __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Challenge;
    };
    SocialCommentComponent.prototype.isSocial = function () {
        return this.comment.CommentTypeID == __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Social;
    };
    SocialCommentComponent.prototype.isIssue = function () {
        return this.comment.CommentTypeID == __WEBPACK_IMPORTED_MODULE_3__models_social_model__["a" /* SocialCommentType */].Issue;
    };
    SocialCommentComponent.prototype.canReply = function () {
        return !__WEBPACK_IMPORTED_MODULE_5__static_company_settings__["a" /* CurrentCompanySettings */].disableCommunityPosting;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__models_social_model__["c" /* SocialComment */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_social_model__["c" /* SocialComment */]) === 'function' && _a) || Object)
    ], SocialCommentComponent.prototype, "comment", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SocialCommentComponent.prototype, "delete", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SocialCommentComponent.prototype, "reply", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SocialCommentComponent.prototype, "edit", void 0);
    SocialCommentComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-social-comment',
            template: " \n                <div class=\"row comment\" (mouseenter)=\"showTools=true\" (mouseleave)=\"showTools=false\" [ngStyle]=\"{'background':(showTools ? '#EFEFEF': '')}\">                                \n                    <div class=\"col s1 right-align\">\n                        <img class=\"user\" height=\"35\" [src]=\"'/resources/image/' + comment.CreatingResourceID + '?size=35'\" width=\"35\">                        \n                    </div>\n                    <div class=\"col s11\">\n                        <div class=\"row\" *ngIf=\"!showEdit\">\n                            <div class=\"col s12 toolbox\">                                \n                                <span class=\"commentType\"><i class=\"fa\" [ngClass]=\"{'fa-comment blue-text': isSocial() ,'fa-question-circle purple-text': isChallenge(), 'fa-exclamation-triangle orange-text': isIssue()}\" aria-hidden=\"true\" ></i></span> <span class=\"user\"><d3s-tooltip [objectType]=\"'Resource'\" [objectId]=\"comment.CreatingResourceID\" [tooltipType]=\"'preview'\" >{{comment.ResourceName}}</d3s-tooltip></span> <span class=\"postDate\">{{comment.DateCreated | date:'medium'}}</span> \n                                <div *ngIf=\"showTools\" class=\"comment-tools\">\n                                    <a *ngIf=\"canReply()\" class=\"comment-tool-item-mid\" (click)=\"showReply=true;\"><i class=\"fa fa-reply\" aria-hidden=\"true\" ></i></a>\n                                    <a *ngIf=\"comment.IsDeletable\" class=\"comment-tool-item-mid\" (click)=\"deleteCommentClick();\"><i class=\"fa fa-trash-o\" aria-hidden=\"true\" ></i></a>                                    \n                                    <a *ngIf=\"comment.IsEditable\" class=\"comment-tool-item-mid\" (click)=\"showEdit = true;editText = comment.Body\"><i class=\"fa fa-pencil-square-o\" aria-hidden=\"true\" ></i></a>                                    \n                                    <a class=\"comment-tool-item-mid\" (click)=\"doVote(socialVoteType.UpVote);\"><d3s-tooltip [objectType]=\"'Comment/Votes'\" [objectId]=\"comment.ID\" [tooltipType]=\"'up'\" [icon]=\"'thumbs-o-up'\" [iconColor]=\"'#646464'\"></d3s-tooltip> {{upVotes}}</a>\n                                    <a class=\"comment-tool-item-mid\" (click)=\"doVote(socialVoteType.DownVote);\"><d3s-tooltip [objectType]=\"'Comment/Votes'\" [objectId]=\"comment.ID\" [tooltipType]=\"'down'\" [icon]=\"'thumbs-o-down'\" [iconColor]=\"'#646464'\"></d3s-tooltip> {{downVotes}}</a>\n                                </div>                      \n                            </div>\n                            <div class=\"col s12\" [innerHtml]=\"comment.Body\"></div>                            \n                            <div class=\"col s12\">\n                                <i class=\"fa fa-tag\" aria-hidden=\"true\"></i> Tags: <d3s-tooltip *ngFor=\"let tag of comment.Tags\" class=\"comment-tag\" (click)=\"changeUrl(tag.Url)\" [objectType]=\"tag.Object\" [objectId]=\"tag.ObjectID\" [tooltipType]=\"'preview'\" [iconColor]=\"tag.IconForeColor\" [foreColor]=\"tag.IconBackColor\">{{tag.TextPath}}</d3s-tooltip>\n                            </div>\n                        </div>                        \n                        <div class=\"row\" *ngIf=\"showEdit\">\n                            <div class=\"col s11 offset-s1\" style=\"padding-top:15px\">   \n                                <p-editor name=\"Edit\" [style]=\"{'height':'50px'}\" [(ngModel)]=\"editText\" ></p-editor>                 \n                            </div>\n                            <div class=\"col s11 offset-s1\" style=\"padding-top:15px;padding-botton:15px;\">   \n                                <button pButton type=\"button\" (click)=\"handleEditClick();\" label=\"Edit\"></button>\n                                <button pButton type=\"button\" (click)=\"showEdit = false;\" label=\"Cancel\"></button>\n                            </div>\n                        </div>\n                    </div>                                    \n                </div> \n                <div class=\"row add-reply\" *ngIf=\"showReply\">\n                    <div class=\"col s11 offset-s1\" style=\"padding-top:15px\">   \n                        <p-editor placeholder=\"Post Reply...\" name=\"Reply\" [style]=\"{'height':'50px'}\" [(ngModel)]=\"replyText\" ></p-editor>                 \n                    </div>\n                    <div class=\"col s11 offset-s1\" style=\"padding-top:15px;padding-botton:15px;\">   \n                        <button pButton type=\"button\" (click)=\"handleReplyClick();\" label=\"Post\"></button>\n                        <button pButton type=\"button\" (click)=\"showReply = false;replyText='';\" label=\"Cancel\"></button>\n                    </div>\n                </div>   \n                <div class=\"row reply\" *ngFor=\"let response of comment?.Comments\">\n                    <div class=\"col s2 right-align\"><img class=\"user\" height=\"35\" [src]=\"'/resources/image/' + response.CreatingResourceID + '?size=35'\" width=\"35\"></div>\n                    <div class=\"col s10\">\n                        <div><span class=\"user\"><d3s-tooltip [objectType]=\"'Resource'\" [objectId]=\"comment.CreatingResourceID\" [tooltipType]=\"'preview'\" >{{response.ResourceName}}</d3s-tooltip></span> <span class=\"postDate\">{{response.DateCreated | date:'medium'}}</span>                        \n                        <div [innerHtml]=\"response.Body\"></div>                            \n                    </div>                                \n                </div>                 \n                ",
            styles: ["\n                span.user{\n                    font-weight:bold;\n                }\n                span.postDate{\n                    color: #AAAAAA;                    \n                }\n                img.user{\n                    border-radius:5px;\n                }                                              \n                .comment-tag{\n                    border-radius: 5px;\n                    margin-right: 5px;\n                    padding: 3px 10px;\n                    cursor:pointer;\n                }\n                .comment, .reply{\n                    padding:5px 0;\n                }\n                .comment-tool-item :hover, .comment-tool-item-mid :hover{\n                    color:rgba(84,164,218,1);\n                }\n                .comment-tool-item, .comment-tool-item-mid{\n                    padding:5px;\n                    font-size:1.4em;\n                    color:#646464;\n                    cursor:pointer;\n                }\n                .comment-tool-item-mid{\n                    border-right:1px solid #AAAAAA;\n                }\n                .comment-tools{                                  \n                    display:inline-block;\n                    position:absolute;\n                    top: -.50rem;\n                    right: .25rem;\n                    border: 1px solid #AAAAAA;\n                    border-radius: 5px;\n                    box-sizing:border-box;\n                    overflow:hidden;\n                    background:white;                    \n                }\n                .toolbox{\n                    position:relative;\n                }\n            "]
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["b" /* SocialService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["b" /* SocialService */]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_4__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__angular_router__["Router"]) === 'function' && _c) || Object])
    ], SocialCommentComponent);
    return SocialCommentComponent;
    var _a, _b, _c;
}(__WEBPACK_IMPORTED_MODULE_1__base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1218:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__base_component__ = __webpack_require__(113);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SocialInputComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};


var SocialInputComponent = (function (_super) {
    __extends(SocialInputComponent, _super);
    function SocialInputComponent() {
        _super.apply(this, arguments);
        this.commented = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.isEditing = false;
        this.comment = '';
        this.tags = [];
    }
    SocialInputComponent.prototype.ngAfterViewInit = function () {
        var _this = this;
        this.viewChildren.changes.subscribe(function (x) { return _this.setFocus(x); });
    };
    SocialInputComponent.prototype.handleCommentClick = function () {
        this.commented.emit({
            comment: this.comment,
            tags: this.tags
        });
        this.isEditing = false;
    };
    SocialInputComponent.prototype.setFocus = function (items) {
        if (items.length > 0) {
            items._results[0].quill.focus();
        }
    };
    SocialInputComponent.prototype.addTag = function (event) {
        this.tags.push(event.tag);
    };
    SocialInputComponent.prototype.removeTag = function (tag) {
        var index = this.tags.findIndex(function (x) { return x.Object == tag.Object && x.ObjectID == tag.ObjectID; });
        if (index >= 0 && index < this.tags.length) {
            this.tags.splice(index, 1);
        }
    };
    SocialInputComponent.prototype.showEditor = function () {
        this.tags = [];
        this.comment = "";
        this.isEditing = true;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SocialInputComponent.prototype, "commented", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["ViewChildren"])('editor'), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["QueryList"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["QueryList"]) === 'function' && _a) || Object)
    ], SocialInputComponent.prototype, "viewChildren", void 0);
    SocialInputComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-social-input',
            template: " \n                <div class=\"row\" *ngIf=\"!isEditing\">\n                    <input type=\"text\" placeholder=\"Add a comment\" class=\"fakeInput\" (click)=\"showEditor();\">\n                </div>\n                <div class=\"row comment-input\" *ngIf=\"isEditing\">\n                    <div class=\"col s12\">\n                        <p-editor #editor placeholder=\"Add a comment\" name=\"Description\" [style]=\"{'height':'50px'}\" [(ngModel)]=\"comment\" ></p-editor>\n                    </div>                               \n                </div>               \n                <div class=\"row\" *ngIf=\"isEditing\" style=\"padding-top:15px;padding-botton:15px;\">\n                    <div class=\"col s12\" style=\"padding-bottom:15px;\" *ngIf=\"tags.length > 0\">\n                        <d3s-tooltip *ngFor=\"let tag of tags\" class=\"comment-tag\" (click)=\"changeUrl(tag.Url)\" [objectType]=\"tag.Object\" [objectId]=\"tag.ObjectID\" [tooltipType]=\"'preview'\" [iconColor]=\"tag.IconForeColor\" [foreColor]=\"tag.IconBackColor\">{{tag.TextPath}} <i class=\"fa fa-times\" (click)=\"removeTag(tag)\"></i></d3s-tooltip>\n                    </div>\n                    <div class=\"col s10\">\n                        <d3s-social-tag-input (selectTag)=\"addTag($event)\"></d3s-social-tag-input>                                               \n                    </div>         \n                    <div class=\"col s2\">\n                        <button class=\"right\" pButton type=\"button\" (click)=\"isEditing=false;\" label=\"Cancel\"></button>\n                        <button class=\"right\" pButton type=\"button\" (click)=\"handleCommentClick();\" label=\"Post\"></button>                        \n                    </div>\n                </div> \n                ",
            styles: ["  \n            .fakeInput{\n                width:100%;\n                padding:10px;\n                border: 1px solid #CCCCCC;\n                border-radius: 5px;\n                margin: 5px;\n            }    \n            .comment-tag{\n                border-radius: 5px;\n                margin-right: 5px;\n                padding: 3px 10px;\n                cursor:pointer;\n            }      \n        "],
        }), 
        __metadata('design:paramtypes', [])
    ], SocialInputComponent);
    return SocialInputComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1219:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SocialTagInputComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var SocialTagInputComponent = (function (_super) {
    __extends(SocialTagInputComponent, _super);
    function SocialTagInputComponent(tagService) {
        _super.call(this);
        this.tagService = tagService;
        this.selectTag = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.tags = [];
    }
    SocialTagInputComponent.prototype.search = function (event) {
        var _this = this;
        this.tagService.getTags(event.query).then(function (data) {
            _this.tags = data;
        });
    };
    SocialTagInputComponent.prototype.selectItem = function () {
        this.selectTag.emit({
            tag: this.tag
        });
        this.tag = null;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SocialTagInputComponent.prototype, "selectTag", void 0);
    SocialTagInputComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-social-tag-input',
            template: "\n           <p-autoComplete size=\"50\"\n                            scrollHeight=\"400px\"\n                            [(ngModel)]=\"tag\" \n                            [suggestions]=\"tags\" \n                            (completeMethod)=\"search($event)\" \n                            field=\"TextPath\"  \n                            placeholder=\"Tag an item\"\n                            (onSelect)=\"selectItem()\">                       \n                    </p-autoComplete>\n        ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["c" /* TagService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["c" /* TagService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["c" /* TagService */]) === 'function' && _a) || Object])
    ], SocialTagInputComponent);
    return SocialTagInputComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1277:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* unused harmony export SearchResult */
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return SearchFullResult; });
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return SearchCategories; });
/* unused harmony export SearchResultInfo */
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SearchResultsObject; });
/* harmony export (binding) */ __webpack_require__.d(exports, "d", function() { return AdvancedSearchFilter; });
var SearchResult = (function () {
    function SearchResult() {
    }
    return SearchResult;
}());
var SearchFullResult = (function () {
    function SearchFullResult() {
    }
    return SearchFullResult;
}());
var SearchCategories = (function () {
    function SearchCategories() {
    }
    return SearchCategories;
}());
var SearchResultInfo = (function () {
    function SearchResultInfo() {
    }
    return SearchResultInfo;
}());
var SearchResultsObject = (function () {
    function SearchResultsObject() {
    }
    return SearchResultsObject;
}());
var AdvancedSearchFilter = (function () {
    function AdvancedSearchFilter(field, value) {
        this.exact = false;
        this.connector = 'and';
        this.field = field;
        this.value = value;
    }
    return AdvancedSearchFilter;
}());


/***/ },

/***/ 1280:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__ = __webpack_require__(100);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__models_breadcrumb_model__ = __webpack_require__(484);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__static_company_settings__ = __webpack_require__(1176);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SearchComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};








var SearchComponent = (function (_super) {
    __extends(SearchComponent, _super);
    function SearchComponent(route, titleService, headerBreadcrumbService, searchService, typeaheadSearchService) {
        _super.call(this);
        this.route = route;
        this.titleService = titleService;
        this.headerBreadcrumbService = headerBreadcrumbService;
        this.searchService = searchService;
        this.typeaheadSearchService = typeaheadSearchService;
        this.categories = [];
        this.isExactMatch = true;
        this.searchTypes = __WEBPACK_IMPORTED_MODULE_6__static_company_settings__["a" /* CurrentCompanySettings */].defaultSearchTypes ? __WEBPACK_IMPORTED_MODULE_6__static_company_settings__["a" /* CurrentCompanySettings */].defaultSearchTypes.split(',') : [];
        this.advancedFilters = [];
        this.resultsPerPage = 10;
        this.pageNumber = 0;
        this.showAdvanced = false;
    }
    SearchComponent.prototype.ngOnInit = function () {
        var _this = this;
        this.setBrowserTitle(this.titleService, 'Search');
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new __WEBPACK_IMPORTED_MODULE_5__models_breadcrumb_model__["a" /* Breadcrumb */]('Search'));
        this.sub = this.route.queryParams.subscribe(function (params) {
            _this.showAdvanced = params['advanced'] == '1';
            _this.searchText = params['query'] ? params['query'] : '';
            if (params['types']) {
                _this.searchTypes = params['types'].split(',');
            }
            if (_this.searchText.length > 0)
                _this.doSearch();
        });
    };
    SearchComponent.prototype.doSearch = function (filterCategory) {
        var _this = this;
        this.isLoading = true;
        this.searchService.getSearchResults(this.searchText, this.resultsPerPage, this.pageNumber, (this.showAdvanced ? undefined : this.searchTypes), filterCategory, this.isExactMatch, this.showAdvanced ? this.advancedFilters : undefined)
            .then(function (res) {
            _this.isLoading = false;
            _this.searchResults = res;
            if (filterCategory == undefined)
                _this.categories = res.Categories;
        });
    };
    SearchComponent.prototype.filterByCategory = function (category) {
        this.selectedCategory = category;
        this.doSearch(this.selectedCategory);
    };
    SearchComponent.prototype.paginate = function (event) {
        if (!event.size == undefined) {
            console.log("ERROR : MISSING ITEMS PER PAGE.");
            return;
        }
        if (event.page == undefined) {
            console.log("ERROR : MISSING PAGE NUMBER.");
            return;
        }
        if (!event.first == undefined) {
            console.log("ERROR : MISSING INDEX OF FIRST PAGE.");
            return;
        }
        this.resultsPerPage = event.size;
        this.pageNumber = event.first == 0 ? 0 : (event.first / this.resultsPerPage);
        this.doSearch(this.selectedCategory);
    };
    SearchComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-search',
            template: "               \n                <d3s-search-input (search)=\"doSearch()\" [isAdvancedMode]=\"showAdvanced\" (isAdvancedModeChange)=\"showAdvanced=$event;searchResults=null;\" [(advancedFilters)]=\"advancedFilters\" [(isExactMatch)]=\"isExactMatch\" [(searchTypes)]=\"searchTypes\" [hasAdvanced]=\"true\" [(searchText)]=\"searchText\"></d3s-search-input>                              \n                <d3s-search-results [loading]=\"isLoading\" [itemsPerPage]=\"resultsPerPage\" [results]=\"searchResults\" [categories]=\"categories\" (paginateClick)=\"paginate($event);\" (selectedCategoryChange)=\"filterByCategory($event);\"></d3s-search-results>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_4__services_index__["V" /* SearchService */], __WEBPACK_IMPORTED_MODULE_4__services_index__["W" /* TypeaheadSearchService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["ActivatedRoute"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["ActivatedRoute"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__["Title"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__angular_platform_browser__["Title"]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["g" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["g" /* HeaderBreadcrumbService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["V" /* SearchService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["V" /* SearchService */]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_4__services_index__["W" /* TypeaheadSearchService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_index__["W" /* TypeaheadSearchService */]) === 'function' && _e) || Object])
    ], SearchComponent);
    return SearchComponent;
    var _a, _b, _c, _d, _e;
}(__WEBPACK_IMPORTED_MODULE_2__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1281:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return DashboardTabComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var DashboardTabComponent = (function (_super) {
    __extends(DashboardTabComponent, _super);
    function DashboardTabComponent(dashboardService) {
        _super.call(this);
        this.dashboardService = dashboardService;
        this.objectID = 0;
        this.dashboards = [];
    }
    DashboardTabComponent.prototype.ngOnInit = function () {
        this.loadAvailableDashboards();
    };
    DashboardTabComponent.prototype.loadAvailableDashboards = function () {
        var _this = this;
        this.isLoading = true;
        this.dashboardService.getDashboards(this.objectID, this.objectType)
            .then(function (result) {
            _this.dashboards = result;
            _this.isLoading = false;
        });
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], DashboardTabComponent.prototype, "objectID", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], DashboardTabComponent.prototype, "objectType", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], DashboardTabComponent.prototype, "objectName", void 0);
    DashboardTabComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-dashboard-tab',
            template: "\n            <d3s-loading [isLoading]=\"isLoading\"></d3s-loading>\n            <div class=\"row\" *ngIf=\"!isLoading\">\n                <div class=\"col s12\">\n                    <div class=\"tile tile-detail\">  \n                        <header>Dashboards for {{objectName}}</header>\n                        <div class=\"row\">\n                            <div class=\"col s12\">\n                                <span style=\"padding:0 10px;\">Dashboard:</span>\n                                <select [(ngModel)]=\"dashboard\" style=\"width:300px;\">\n                                    <option></option>\n                                    <option *ngFor=\"let dashboard of dashboards\" [ngValue]=\"dashboard\">{{dashboard.Name}}</option>\n                                </select>                                \n                                \n                                <button pButton type=\"button\" (click)=\"selected=dashboard;\" label=\"Render\" style=\"width: '150px';padding:4px;\"></button>\n                            </div>  \n                            <div *ngIf=\"dashboard?.Description\" class=\"col s12\" [innerHtml]=\"dashboard?.Description\"></div>                          \n                        </div>                        \n                    </div>\n                    <div class=\"tile tile-detail\" *ngIf=\"selected\">\n                        <d3s-powerbi-viewer [dashboard]=\"selected\"></d3s-powerbi-viewer>                        \n                    </div>\n                    <div class=\"tile tile-detail\" *ngIf=\"!selected\">\n                        <h4 class=\"center\" style=\"padding:30px;\">Please choose a dashboard from the dropdown above and press render to view the specified dashboards content.</h4>\n                    </div>\n                </div>\n            </div>\n        ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["S" /* DashboardService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["S" /* DashboardService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["S" /* DashboardService */]) === 'function' && _a) || Object])
    ], DashboardTabComponent);
    return DashboardTabComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1282:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_dashboard_model__ = __webpack_require__(1284);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return PowerBIViewerComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var PowerBIViewerComponent = (function (_super) {
    __extends(PowerBIViewerComponent, _super);
    function PowerBIViewerComponent(el, dashboardService, webAnalyticsService) {
        _super.call(this, undefined, webAnalyticsService);
        this.el = el;
        this.dashboardService = dashboardService;
        this.shouldRender = false;
    }
    PowerBIViewerComponent.prototype.ngOnChanges = function (changes) {
        if (this.dashboard)
            this.loadTokens();
    };
    PowerBIViewerComponent.prototype.ngAfterViewInit = function () {
        var _this = this;
        this.biContainer.changes.subscribe(function () { return _this.initPowerBi(); });
    };
    PowerBIViewerComponent.prototype.showFullscreen = function () {
        if (this.biContainer) {
            var report = window.powerbi.get(this.biContainer.first.nativeElement);
            report.fullscreen();
        }
    };
    PowerBIViewerComponent.prototype.initPowerBi = function () {
        if (this.biContainer && this.biContainer.length > 0 && this.shouldRender) {
            if (!this.biContainer.first)
                console.log("ERROR: FIRST BICONTAINER ELEMENT IS NULL!");
            else if (!this.biContainer.first.nativeElement)
                console.log("ERROR: FIRST BICONTAINER NATIVE ELEMENT IS NULL!");
            else {
                this.shouldRender = false;
                window.powerbi.embed(this.biContainer.first.nativeElement);
                console.log("DEV: RENDERING POWER BI REPORT");
                this.logAction('open', 'Report', this.dashboard.ID);
            }
        }
    };
    PowerBIViewerComponent.prototype.loadTokens = function () {
        var _this = this;
        this.isLoading = true;
        this.dashboardService.getPowerBIReportTokens(this.dashboard.PowerBIReportID)
            .then(function (result) {
            _this.shouldRender = true; // make sure only one call to power bi per load of this.           
            _this.powerBIDetails = result;
            _this.isLoading = false;
        });
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__models_dashboard_model__["a" /* Dashboard */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_dashboard_model__["a" /* Dashboard */]) === 'function' && _a) || Object)
    ], PowerBIViewerComponent.prototype, "dashboard", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["ViewChildren"])("biContainer"), 
        __metadata('design:type', (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["QueryList"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["QueryList"]) === 'function' && _b) || Object)
    ], PowerBIViewerComponent.prototype, "biContainer", void 0);
    PowerBIViewerComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-powerbi-viewer',
            template: " \n                <header>{{dashboard?.Name}}<d3s-tile-actions [hasFullScreen]=\"true\" (fullScreenClick)=\"showFullscreen()\"></d3s-tile-actions></header>\n                <div class=\"row\">\n                    <div class=\"col s12\">\n                        <d3s-loading [isLoading]=\"isLoading\"></d3s-loading>                        \n                        <div *ngIf=\"!isLoading\" #biContainer style=\"height:75vh\" class=\"powerbi\"\n                                powerbi-type=\"report\"\n                                [attr.powerbi-embed-url]=\"powerBIDetails?.Report?.embedUrl\"\n                                [attr.powerbi-access-token]=\"powerBIDetails?.AccessToken\"\n                        ></div>\n                    </div>\n                </div>\n            ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["S" /* DashboardService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["ElementRef"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["ElementRef"]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["S" /* DashboardService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["S" /* DashboardService */]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["h" /* WebAnalyticsService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["h" /* WebAnalyticsService */]) === 'function' && _e) || Object])
    ], PowerBIViewerComponent);
    return PowerBIViewerComponent;
    var _a, _b, _c, _d, _e;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1283:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_5_primeng_primeng__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__core_module__ = __webpack_require__(1165);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__tiles_tiles_module__ = __webpack_require__(1166);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__dashboard_tab_component__ = __webpack_require__(1281);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__powerbi_viewer_component__ = __webpack_require__(1282);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SharedDashboardModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};










var SharedDashboardModule = (function () {
    function SharedDashboardModule() {
    }
    SharedDashboardModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["e" /* HttpModule */],
                //d3s
                __WEBPACK_IMPORTED_MODULE_6__core_module__["a" /* CoreModule */],
                __WEBPACK_IMPORTED_MODULE_7__tiles_tiles_module__["a" /* TilesModule */],
                //prime
                __WEBPACK_IMPORTED_MODULE_5_primeng_primeng__["ButtonModule"],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_8__dashboard_tab_component__["a" /* DashboardTabComponent */],
                __WEBPACK_IMPORTED_MODULE_9__powerbi_viewer_component__["a" /* PowerBIViewerComponent */]
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_8__dashboard_tab_component__["a" /* DashboardTabComponent */]
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_3__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_4__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], SharedDashboardModule);
    return SharedDashboardModule;
}());


/***/ },

/***/ 1284:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return Dashboard; });
/* unused harmony export PowerBIReport */
/* unused harmony export DashboardTokens */
var Dashboard = (function () {
    function Dashboard() {
    }
    return Dashboard;
}());
var PowerBIReport = (function () {
    function PowerBIReport() {
    }
    return PowerBIReport;
}());
var DashboardTokens = (function () {
    function DashboardTokens() {
    }
    return DashboardTokens;
}());


/***/ },

/***/ 1287:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__static_company_settings__ = __webpack_require__(1176);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HomeSearchComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var HomeSearchComponent = (function (_super) {
    __extends(HomeSearchComponent, _super);
    function HomeSearchComponent(searchService, typeaheadSearchService) {
        _super.call(this);
        this.searchService = searchService;
        this.typeaheadSearchService = typeaheadSearchService;
        this.categories = [];
        this.isExactMatch = true;
        this.searchTypes = __WEBPACK_IMPORTED_MODULE_3__static_company_settings__["a" /* CurrentCompanySettings */].defaultSearchTypes ? __WEBPACK_IMPORTED_MODULE_3__static_company_settings__["a" /* CurrentCompanySettings */].defaultSearchTypes.split(',') : [];
        this.resultsPerPage = 5;
        this.pageNumber = 0;
    }
    HomeSearchComponent.prototype.doSearch = function (filterCategory) {
        var _this = this;
        this.searchService.getSearchResults(this.searchText, this.resultsPerPage, this.pageNumber, this.searchTypes, filterCategory, this.isExactMatch)
            .then(function (res) {
            _this.searchResults = res;
            if (filterCategory == undefined)
                _this.categories = res.Categories;
        });
    };
    HomeSearchComponent.prototype.filterByCategory = function (category) {
        this.selectedCategory = category;
        this.doSearch(this.selectedCategory);
    };
    HomeSearchComponent.prototype.paginate = function (event) {
        if (!event.size == undefined) {
            console.log("ERROR : MISSING ITEMS PER PAGE.");
            return;
        }
        if (event.page == undefined) {
            console.log("ERROR : MISSING PAGE NUMBER.");
            return;
        }
        if (!event.first == undefined) {
            console.log("ERROR : MISSING INDEX OF FIRST PAGE.");
            return;
        }
        this.resultsPerPage = event.size;
        this.pageNumber = event.first == 0 ? 0 : (event.first / this.resultsPerPage);
        this.doSearch(this.selectedCategory);
    };
    HomeSearchComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-home-search',
            template: "               \n                <d3s-search-input (search)=\"doSearch()\" [(isExactMatch)]=\"isExactMatch\" [(searchTypes)]=\"searchTypes\" [(searchText)]=\"searchText\"></d3s-search-input>                \n                <d3s-search-results [itemsPerPage]=\"resultsPerPage\" [results]=\"searchResults\" [categories]=\"categories\" (paginateClick)=\"paginate($event);\" (selectedCategoryChange)=\"filterByCategory($event);\"></d3s-search-results>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["V" /* SearchService */], __WEBPACK_IMPORTED_MODULE_2__services_index__["W" /* TypeaheadSearchService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["V" /* SearchService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["V" /* SearchService */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["W" /* TypeaheadSearchService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["W" /* TypeaheadSearchService */]) === 'function' && _b) || Object])
    ], HomeSearchComponent);
    return HomeSearchComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1288:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__ = __webpack_require__(85);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SearchAutocompleteListComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var SearchAutocompleteListComponent = (function (_super) {
    __extends(SearchAutocompleteListComponent, _super);
    function SearchAutocompleteListComponent(elementRef, router) {
        _super.call(this);
        this.elementRef = elementRef;
        this.router = router;
        this.autocompletions = [];
        this.showResults = true;
        this.width = '400px';
    }
    SearchAutocompleteListComponent.prototype.ngOnInit = function () {
        if (this.element && this.element.offsetWidth)
            this.width = this.element.offsetWidth + 'px';
    };
    SearchAutocompleteListComponent.prototype.ngOnChanges = function (changes) {
        this.showResults = true;
    };
    SearchAutocompleteListComponent.prototype.goTo = function (item) {
        this.router.navigateByUrl(__WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__["a" /* SiteUrlHelpers */].convertClassicUrl(item.Url));
    };
    SearchAutocompleteListComponent.prototype.onClick = function (event) {
        if (this.showResults && !this.elementRef.nativeElement.contains(event.target)) {
            this.showResults = false;
        }
    };
    SearchAutocompleteListComponent.prototype.highlightedResult = function (item) {
        if (!item)
            return "";
        //var regEx = new RegExp(this.searchText, "ig");
        //return item.replace(regEx, `<strong class="item-highlight">${this.searchText}</strong>`);
        return item;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Array)
    ], SearchAutocompleteListComponent.prototype, "autocompletions", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], SearchAutocompleteListComponent.prototype, "searchText", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Object)
    ], SearchAutocompleteListComponent.prototype, "element", void 0);
    SearchAutocompleteListComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-search-autocomplete-list',
            host: {
                '(document:click)': 'onClick($event)',
            },
            styles: ["\n                :host{\n                    position:relative;\n                    margin-left:11.25px;\n                }                                     \n            "],
            template: " \n                <div *ngIf=\"showResults && autocompletions.length > 0\" class=\"tt-menu\" style=\"position:absolute;top:-3px;left:0;min-width:400px\" [ngStyle]=\"{'width':width}\">                         \n                    <div class=\"header\">Select an item from the dropdown to go directly to it, or to see more search results type in the text you want to search by.</div>\n                    <div *ngFor=\"let autocomplete of autocompletions\" class=\"tt-suggestion tt-selectable\" (click)=\"goTo(autocomplete)\">\n                        <span class=\"type\">{{autocomplete.Type}}</span> <span [innerHtml]=\"highlightedResult(autocomplete.Name)\"></span>\n                    </div>                    \n                </div>\n                \n                ",
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["ElementRef"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["ElementRef"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _b) || Object])
    ], SearchAutocompleteListComponent);
    return SearchAutocompleteListComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1289:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_search_result_model__ = __webpack_require__(1277);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__static_site_url_helpers__ = __webpack_require__(85);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SearchInputComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};






var SearchInputComponent = (function (_super) {
    __extends(SearchInputComponent, _super);
    function SearchInputComponent(router, searchService, typeaheadSearchService) {
        _super.call(this);
        this.router = router;
        this.searchService = searchService;
        this.typeaheadSearchService = typeaheadSearchService;
        this.isExactMatch = true;
        this.isExactMatchChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.searchTypes = ["Artifact", "Synonym"];
        this.searchTypesChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.searchTextChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.search = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.hasAdvanced = false;
        this.isAdvancedMode = false;
        this.isAdvancedModeChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.advancedFilters = [];
        this.advancedFiltersChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.fields = [
            { title: "Category", value: "Type" },
            { title: "Description", value: "Description" },
            { title: "Name", value: "Name" },
            { title: "Type", value: "_type" },
        ];
        this.types = [
            { title: "Attribute", value: "Attribute" },
            { title: "Fusion", value: "FusionAttributes" },
            { title: "Fusion Type", value: "FusionType" },
            { title: "Glossary", value: "Artifact" },
            { title: "Group", value: "Group" },
            { title: "Model", value: "Taxonomy" },
            { title: "Reference", value: "Domain" },
            { title: "User", value: "Users" },
            { title: "Synonym", value: "Synonym" },
        ];
        this.searchObjectTypes = [
            { value: "Attribute", label: "Attribute" },
            { value: "FusionAttributes", label: "Fusion" },
            { value: "FusionType", label: "Fusion Type" },
            { value: "Artifact", label: "Glossary" },
            { value: "Group", label: "Group" },
            { value: "Taxonomy", label: "Model" },
            { value: "Domain", label: "Reference" },
            { value: "Users", label: "User" },
            { value: "Synonym", label: "Synonym" },
        ];
        this.simpleSearchID = 0;
        this.autocompleteResultSize = 5;
        this.autocompletions = [];
    }
    SearchInputComponent.prototype.ngOnChanges = function (changes) {
        if (this.isAdvancedMode && this.advancedFilters.length == 0)
            this.advancedFilters.push(new __WEBPACK_IMPORTED_MODULE_4__models_search_result_model__["d" /* AdvancedSearchFilter */]("Name", this.searchText));
    };
    SearchInputComponent.prototype.triggerSearch = function () {
        this.cancelAutocomplete();
        this.autocompletions = [];
        this.search.emit({
            text: this.searchText,
            exactMatch: this.isExactMatch,
            types: this.searchTypes
        });
    };
    SearchInputComponent.prototype.triggerAdvancedSearch = function () {
        this.cancelAutocomplete();
        this.autocompletions = [];
        this.search.emit({
            adv: this.advancedFilters
        });
    };
    SearchInputComponent.prototype.cancelAutocomplete = function () {
        if (this.simpleSearchID > 0) {
            window.clearTimeout(this.simpleSearchID);
            this.simpleSearchID = 0;
        }
    };
    SearchInputComponent.prototype.checkAdvSearchKey = function (event) {
        if (event.keyCode == 13) {
            this.triggerAdvancedSearch();
        }
    };
    SearchInputComponent.prototype.checkSearchKey = function (event) {
        var _this = this;
        if (event.keyCode == 13) {
            this.triggerSearch();
        }
        else if (this.searchText.length > 3) {
            this.cancelAutocomplete();
            this.simpleSearchID = window.setTimeout(function () { return _this.doAutocompleteSearch(); }, 1000);
        }
    };
    SearchInputComponent.prototype.doAutocompleteSearch = function () {
        var _this = this;
        if (!this.searchText || this.searchText.length == 0)
            return;
        this.typeaheadSearchService.getResults(this.autocompleteResultSize, this.searchText, this.searchTypes)
            .then(function (res) {
            _this.autocompletions = res;
        });
    };
    SearchInputComponent.prototype.removeFilter = function (filter) {
        var index = this.advancedFilters.findIndex(function (x) { return x == filter; });
        if (index >= 0 && index < this.advancedFilters.length) {
            this.advancedFilters.splice(index, 1);
            this.advancedFiltersChange.emit(this.advancedFilters);
        }
    };
    SearchInputComponent.prototype.addFilter = function () {
        this.advancedFilters.push(new __WEBPACK_IMPORTED_MODULE_4__models_search_result_model__["d" /* AdvancedSearchFilter */]());
        this.advancedFiltersChange.emit(this.advancedFilters);
    };
    SearchInputComponent.prototype.handleAdvancedClick = function () {
        if (this.hasAdvanced) {
            this.isAdvancedMode = !this.isAdvancedMode;
            this.isAdvancedModeChange.emit(this.isAdvancedMode);
        }
        else {
            this.router.navigateByUrl(__WEBPACK_IMPORTED_MODULE_5__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_SEARCH_ROOT + "?query=" + (this.searchText ? encodeURIComponent(this.searchText) : '') + "&advanced=1&types=" + (this.searchTypes ? this.searchTypes.join(',') : ''));
        }
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], SearchInputComponent.prototype, "isExactMatch", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SearchInputComponent.prototype, "isExactMatchChange", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Array)
    ], SearchInputComponent.prototype, "searchTypes", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SearchInputComponent.prototype, "searchTypesChange", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], SearchInputComponent.prototype, "searchText", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SearchInputComponent.prototype, "searchTextChange", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SearchInputComponent.prototype, "search", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], SearchInputComponent.prototype, "hasAdvanced", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], SearchInputComponent.prototype, "isAdvancedMode", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SearchInputComponent.prototype, "isAdvancedModeChange", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Array)
    ], SearchInputComponent.prototype, "advancedFilters", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SearchInputComponent.prototype, "advancedFiltersChange", void 0);
    SearchInputComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-search-input',
            template: "      \n                <div *ngIf=\"!isAdvancedMode\">\n                    <div class=\"search-input-container\" >           \n                        <div class=\"search-input-text-container\">                        \n                            <input #search [ngModel]=\"searchText\" (ngModelChange)=\"searchText=$event;searchTextChange.emit(searchText);\" (keyup)=\"checkSearchKey($event);\" type=\"text\" id=\"home-search-text\" placeholder=\"What do you want to find?\" class=\"search-input-text\" autofocus autocomplete=\"off\" />                        \n                        </div>\n                        <div class=\"search-input-exact-container hide-on-med-and-down\">\n                            <div class=\"adv-search-btn\">\n                                <label><input type=\"checkbox\" name=\"search-exact-chk\" id=\"search-exact-chk\" [ngModel]=\"isExactMatch\" (ngModelChange)=\"isExactMatch=$event;isExactMatchChange.emit(isExactMatch);\"> Exact match</label>\n                            </div>\n                        </div>\n                        <div class=\"search-input-types-container hide-on-med-and-down\">\n                            <div class=\"search-btn\">\n                                <p-multiSelect [options]=\"searchObjectTypes\" [ngModel]=\"searchTypes\" (ngModelChange)=\"searchTypes=$event;searchTypesChange.emit(searchTypes);\"></p-multiSelect>                        \n                            </div>\n                        </div>\n                        <div class=\"search-input-adv-container hide-on-med-and-down\">\n                            <button type=\"button\" name=\"action\" id=\"home-adv-btn\" class=\"adv-search-btn\" (click)=\"handleAdvancedClick()\">Advanced&nbsp;<i class=\"fa fa-caret-down\"></i></button>\n                        </div>\n                        <div class=\"search-input-button-container\">\n                            <button type=\"submit\" name=\"action\" id=\"home-search-btn\" class=\"search-input-btn\" (click)=\"triggerSearch()\">\n                                <i class=\"fa fa-search\"></i>\n                            </button>\n                        </div>                    \n                    </div>  \n                    <d3s-search-autocomplete-list *ngIf=\"!isAdvancedMode\" [searchText]=\"searchText\" [element]=\"search\" [autocompletions]=\"autocompletions\"></d3s-search-autocomplete-list>            \n                </div>\n                <div *ngIf=\"isAdvancedMode\" class=\"tile tile-detail\">                             \n                    <header>Advanced Search <d3s-tile-actions [hasAdd]=\"false\" [hasClose]=\"true\" (closeClick)=\"handleAdvancedClick()\"></d3s-tile-actions></header>\n                    <div *ngFor=\"let filter of advancedFilters; let last=last\" class=\"row advSearchRow\">\n                        <div class=\"col s1 center-align\">Field</div>\n                        <div class=\"col s3\">\n                            <select [(ngModel)]=\"filter.field\" style=\"width:100%;\">\n                                    <option value=\"\" disabled selected>Please Choose...</option>\n                                    <option *ngFor=\"let p of fields\" [value]=\"p.value\">{{p.title}}</option>\n                            </select>\n                        </div>\n                        <div class=\"col s3\" *ngIf=\"filter.field != '_type'\">\n                            <input type=\"text\" [(ngModel)]=\"filter.value\" style=\"width:100%\" placeholder=\"Enter a value\" (keyup)=\"checkAdvSearchKey($event);\">\n                        </div>\n                        <div class=\"col s3\" *ngIf=\"filter.field == '_type'\">\n                            <select [(ngModel)]=\"filter.value\" style=\"width:100%;\" placeholder=\"Choose a type\">\n                                    <option value=\"\" disabled selected>Please Choose...</option>\n                                    <option *ngFor=\"let p of types\" [value]=\"p.value\">{{p.title}}</option>\n                            </select>\n                        </div>\n                        <div class=\"col s1\" *ngIf=\"filter.field != '_type'\">\n                                <label><input type=\"checkbox\" [(ngModel)]=\"filter.exact\">Exact match</label>\n                        </div>\n                        <div class=\"col s1\" *ngIf=\"filter.field == '_type'\">&nbsp;</div>\n                        <div class=\"col s1\" *ngIf=\"last\" (click)=\"addFilter()\" style=\"cursor:pointer\"><i class=\"fa fa-plus\" aria-hidden=\"true\" title=\"add filter\" style=\"font-size:1.5em\"></i></div>\n                        <div class=\"col s1\" *ngIf=\"!last\" (click)=\"removeFilter(filter)\"  style=\"cursor:pointer\"><i class=\"fa fa-minus\" aria-hidden=\"true\" title=\"remove filter\" style=\"font-size:1.5em\"></i></div>\n                    </div>\n                    <div class=\"row\">\n                        <div class=\"col s1 offset-s1\">\n                            <button pButton type=\"button\" (click)=\"triggerAdvancedSearch()\" label=\"Search\" style=\"width:150px;\"></button>\n                        </div>\n                    </div>\n                </div>                     \n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_3__services_index__["V" /* SearchService */], __WEBPACK_IMPORTED_MODULE_3__services_index__["W" /* TypeaheadSearchService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["V" /* SearchService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["V" /* SearchService */]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["W" /* TypeaheadSearchService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["W" /* TypeaheadSearchService */]) === 'function' && _c) || Object])
    ], SearchInputComponent);
    return SearchInputComponent;
    var _a, _b, _c;
}(__WEBPACK_IMPORTED_MODULE_2__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1290:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_search_result_model__ = __webpack_require__(1277);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__static_site_url_helpers__ = __webpack_require__(85);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SearchResultItemComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};






var SearchResultItemComponent = (function (_super) {
    __extends(SearchResultItemComponent, _super);
    function SearchResultItemComponent(router) {
        _super.call(this);
        this.router = router;
    }
    SearchResultItemComponent.prototype.navigateLink = function () {
        this.router.navigateByUrl(__WEBPACK_IMPORTED_MODULE_5__static_site_url_helpers__["a" /* SiteUrlHelpers */].convertClassicUrl(this.result.Url));
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_4__models_search_result_model__["c" /* SearchFullResult */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__models_search_result_model__["c" /* SearchFullResult */]) === 'function' && _a) || Object)
    ], SearchResultItemComponent.prototype, "result", void 0);
    SearchResultItemComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-search-result-item',
            template: "       \n                <div class=\"search-res-container\">\n                    <h4 class=\"search-result-name\"><a (click)=\"navigateLink()\" class=\"search-result-link\" [innerHtml]=\"result?.Name\"></a></h4>\n                    <p class=\"search-result-desc\" [innerHtml]=\"result?.Description\"></p>\n                    <h5 class=\"search-result-attributes\">Category: <em class=\"result-category\" [innerHtml]=\"result?.Type\"></em>&nbsp;&nbsp;Type: <em class=\"result-type\">{{result?.Group}}</em></h5>\n                </div>        \n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_3__services_index__["V" /* SearchService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _b) || Object])
    ], SearchResultItemComponent);
    return SearchResultItemComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1291:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_search_result_model__ = __webpack_require__(1277);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SearchResultsComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var SearchResultsComponent = (function (_super) {
    __extends(SearchResultsComponent, _super);
    function SearchResultsComponent() {
        _super.call(this);
        this.categories = [];
        this.itemsPerPage = 5;
        this.loading = false;
        this.paginateClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.selectedCategoryChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    SearchResultsComponent.prototype.selectCategory = function (category) {
        this.selectedCategory = category;
        this.selectedCategoryChange.emit(this.selectedCategory);
    };
    SearchResultsComponent.prototype.paginate = function (data) {
        /*
            event.page: New page number
            event.first: Index of first record
            event.rows: Number of rows to display in new page
            event.pageCount: Total number of pages
        */
        this.paginateClick.emit({ page: data.page, size: data.rows, first: data.first });
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__models_search_result_model__["a" /* SearchResultsObject */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_search_result_model__["a" /* SearchResultsObject */]) === 'function' && _a) || Object)
    ], SearchResultsComponent.prototype, "results", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Array)
    ], SearchResultsComponent.prototype, "categories", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], SearchResultsComponent.prototype, "itemsPerPage", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], SearchResultsComponent.prototype, "loading", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SearchResultsComponent.prototype, "paginateClick", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__models_search_result_model__["b" /* SearchCategories */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_search_result_model__["b" /* SearchCategories */]) === 'function' && _b) || Object)
    ], SearchResultsComponent.prototype, "selectedCategory", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SearchResultsComponent.prototype, "selectedCategoryChange", void 0);
    SearchResultsComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-search-results',
            template: "               \n                <div *ngIf=\"results?.Result?.Results?.length > 0\">\n                    <div class=\"row\">\n                        <div class=\"col l3 m5 hide-on-small-only\">\n                            <div class=\"tile tile-detail\">\n                                <header>Categories</header>\n                                <div class=\"widget search-category-area\" id=\"CategoryResults\">\n                                    <div class=\"row\">\n                                        <div class=\"col l10 m10 s11 entry\">                                            \n                                            <a (click)=\"selectCategory(null)\" style=\"cursor:pointer\" class=\"search-type-link\" [title]=\"'All'\" [ngClass]=\"{selected:!selectedCategory}\">All</a>                                            \n                                        </div>\n                                        <div class=\"col l2 m2 s1\">\n                                            <span style=\"float:right\">{{results?.Result?.Matches}}</span>\n                                        </div>                                        \n                                    </div>\n                                    <template let-category ngFor [ngForOf]=\"categories\">\n                                        <div class=\"row\">\n                                            <div class=\"col l10 m10 s11 entry\">\n                                                <i class=\"search-category-type-group fa fa-angle-right\" data-bind=\"click: toggleVisibility,visible: showToggle,css: {'fa-angle-right' : showRow, 'fa-angle-down' : !showRow()}\"></i>\n                                                <a (click)=\"selectCategory(category);\" style=\"cursor:pointer\" class=\"search-type-link\" [title]=\"category.DisplayName\" [ngClass]=\"{selected:category.DisplayName==selectedCategory?.DisplayName}\">{{category.DisplayName}}</a>                                            \n                                            </div>\n                                            <div class=\"col l2 m2 s1\">\n                                                <span style=\"float:right\">{{category?.ResultCount}}</span>\n                                            </div>                                        \n                                        </div>\n                                        <div class=\"row\" *ngFor=\"let subCategory of category?.Categories\">\n                                            <div class=\"col l10 m10 s11 entry\">                                            \n                                                <a (click)=\"selectCategory(subCategory);\" style=\"cursor:pointer\" [ngClass]=\"{selected:subCategory.Name==selectedCategory?.Name}\" class=\"search-category-link\" [title]=\"subCategory.Name\">{{subCategory.Name}}</a>\n                                            </div>\n                                            <div class=\"col l2 m2 s1\">\n                                                <span style=\"float:right\">{{subCategory?.ResultCount}}</span>\n                                            </div>                                        \n                                        </div>\n                                    </template>\n                                </div>\n                            </div>\n                        </div>                        \n                        <div class=\"col l9 m7\">                            \n                            <div class=\"tile tile-detail\">                                \n                                <header>Search results - <span style=\"color:#999;font-size:75%\">found {{ results?.Result?.Matches }} matches in ({{results?.Result?.ElapsedMS /1000}} seconds)</span></header>\n                                <span *ngIf=\"!loading\">\n                                    <div *ngFor=\"let result of results?.Result?.Results\">\n                                        <d3s-search-result-item [result]=\"result\"></d3s-search-result-item>\n                                    </div>\n                                </span>\n                                <d3s-loading [isLoading]=\"loading\"></d3s-loading>\n                                <p-paginator [rows]=\"itemsPerPage\" [totalRecords]=\"results?.Result?.Matches\" (onPageChange)=\"paginate($event)\"></p-paginator>\n                            </div>\n                        </div>\n                    </div>\n                </div>\n                <div *ngIf=\"results?.Result?.Results?.length == 0\">\n                    <div class=\"row\">\n                        <div class=\"tile tile-detail search-nodata\">       \n                            <header>Your search did not find any results.</header>\n                            <span style=\"padding-left:15px\">Suggestions:</span>\n                            <ul>\n                                <li>Check your spelling</li>\n                                <li>Try broader search criteria</li>\n                                <li>Try a different keyword</li>\n                            </ul>\n                        </div>\n                    </div>\n                </div>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["V" /* SearchService */]],
            changeDetection: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectionStrategy"].OnPush,
        }), 
        __metadata('design:paramtypes', [])
    ], SearchResultsComponent);
    return SearchResultsComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1292:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__search_component__ = __webpack_require__(1280);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SearchRoutingModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var routes = [
    {
        path: '',
        component: __WEBPACK_IMPORTED_MODULE_2__search_component__["a" /* SearchComponent */],
    }
];
var SearchRoutingModule = (function () {
    function SearchRoutingModule() {
    }
    SearchRoutingModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"].forChild(routes)],
            exports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"]],
        }), 
        __metadata('design:paramtypes', [])
    ], SearchRoutingModule);
    return SearchRoutingModule;
}());


/***/ },

/***/ 1335:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_platform_browser__ = __webpack_require__(100);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_breadcrumb_model__ = __webpack_require__(484);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__models_social_model__ = __webpack_require__(1182);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HomeComponent; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};






var HomeComponent = (function (_super) {
    __extends(HomeComponent, _super);
    function HomeComponent(titleService, headerBreadcrumbService, webAnalyticsService, rightSidebarService) {
        _super.call(this, rightSidebarService, webAnalyticsService);
        this.titleService = titleService;
        this.headerBreadcrumbService = headerBreadcrumbService;
        this.showActivityDetails = false;
        this.showBoardDetails = false;
        this.showAssignmentDetails = false;
        this.activityDaysToLookBack = 7;
        this.boardDaysToLookBack = 7;
    }
    HomeComponent.prototype.ngOnInit = function () {
        this.setBrowserTitle(this.titleService, 'Home');
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new __WEBPACK_IMPORTED_MODULE_4__models_breadcrumb_model__["a" /* Breadcrumb */]('Home'));
        this.clearSidebar();
        this.setCommonRightSideBar(false, false, true);
    };
    HomeComponent.prototype.ngOnDestroy = function () {
        this.clearSidebar();
    };
    HomeComponent.prototype.onShowActivityDetails = function (event) {
        this.showActivityDetails = true;
        this.showBoardDetails = false;
        this.showAssignmentDetails = false;
        this.selectedArtifactTypeId = event.Id;
        this.selectedArtifactTypeName = event.name;
    };
    HomeComponent.prototype.onShowAssignmentDetails = function (event) {
        this.showActivityDetails = false;
        this.showBoardDetails = false;
        this.showAssignmentDetails = true;
        this.selectedWorkflowType = event.workflowType;
    };
    HomeComponent.prototype.onShowBoardDetails = function (event) {
        if (!event.selected) {
            console.log("ERROR NO SELECTION PASSED ON BOARD DETAILS CLICK.");
            return;
        }
        switch (event.selected.Name.toUpperCase()) {
            case "COMMENT":
                this.selectedSocialType = __WEBPACK_IMPORTED_MODULE_5__models_social_model__["a" /* SocialCommentType */].Social;
                break;
            case "ISSUES":
                this.selectedSocialType = __WEBPACK_IMPORTED_MODULE_5__models_social_model__["a" /* SocialCommentType */].Issue;
                break;
            case "TASK":
                this.selectedSocialType = __WEBPACK_IMPORTED_MODULE_5__models_social_model__["a" /* SocialCommentType */].Task;
                break;
            default:
                this.selectedSocialType = undefined;
                break;
        }
        this.showBoardDetails = true;
        this.showAssignmentDetails = false;
        this.showActivityDetails = false;
    };
    HomeComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'home',
            template: __webpack_require__(1510)
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__angular_platform_browser__["Title"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__angular_platform_browser__["Title"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["g" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["g" /* HeaderBreadcrumbService */]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["h" /* WebAnalyticsService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["h" /* WebAnalyticsService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["i" /* RightSidebarService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["i" /* RightSidebarService */]) === 'function' && _d) || Object])
    ], HomeComponent);
    return HomeComponent;
    var _a, _b, _c, _d;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1453:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__ = __webpack_require__(85);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_5_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ActivityDetailsTile; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};






var ActivityDetailsTile = (function (_super) {
    __extends(ActivityDetailsTile, _super);
    function ActivityDetailsTile(router, artifactService) {
        _super.call(this);
        this.router = router;
        this.artifactService = artifactService;
        this.items = [];
        this.objectId = 0;
        this.daysToLookBack = 7;
        this.close = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    ActivityDetailsTile.prototype.ngOnInit = function () {
        if (this.objectId > 0)
            this.load();
    };
    ActivityDetailsTile.prototype.navigateToArtifact = function () {
        this.router.navigateByUrl(__WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ARTIFACT_ROOT + "/" + this.selected.ArtifactTypeID + "/" + this.selected.ID);
    };
    ActivityDetailsTile.prototype.artifactLink = function (artifactTypeId, artifactId) {
        this.router.navigateByUrl(__WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ARTIFACT_ROOT + "/" + artifactTypeId + "/" + artifactId);
    };
    ActivityDetailsTile.prototype.load = function () {
        var _this = this;
        this.isLoading = true;
        this.artifactService.getActivityDetails(this.objectId, this.daysToLookBack)
            .then(function (res) {
            _this.items = res;
            _this.isLoading = false;
        });
    };
    ActivityDetailsTile.prototype.certificateColor = function (item) {
        switch (item.Status) {
            case 'Certified':
                return '#3f9d40';
            case 'Under Review':
                return '#e2792a';
        }
        return '#ebebeb';
    };
    ActivityDetailsTile.prototype.columnSort = function (event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.items = __WEBPACK_IMPORTED_MODULE_5_lodash__["orderBy"](this.items, [function (item) { return item[event.field] ? item[event.field].toLowerCase() : item[event.field]; }], [event.order == -1 ? 'desc' : 'asc']);
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], ActivityDetailsTile.prototype, "objectName", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], ActivityDetailsTile.prototype, "objectId", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], ActivityDetailsTile.prototype, "daysToLookBack", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], ActivityDetailsTile.prototype, "close", void 0);
    ActivityDetailsTile = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-activity-details-tile',
            providers: [__WEBPACK_IMPORTED_MODULE_3__services_index__["m" /* ArtifactService */]],
            template: "\n                <div class=\"tile tile-detail\">\n                   <header>Activity for {{objectName}}\n                    <d3s-tile-actions [hasAdd]=\"false\" [hasFilterMode]=\"true\" [(filterMode)]=\"showSimpleFilter\"></d3s-tile-actions>                            \n                   </header>\n                    <d3s-loading [isLoading]=\"isLoading\"></d3s-loading>\n                    <span *ngIf=\"!isLoading\">\n                        <input #gb [hidden]=\"!showSimpleFilter\" type=\"text\" pInputText size=\"100\" placeholder=\"Search...\" class=\"grid-simple-filter\">\n                        <p-dataTable #dt [globalFilter]=\"gb\" [value]=\"items\" selectionMode=\"single\" [(selection)]=\"selected\" (onRowDblclick)=\"selected=$event.data;navigateToArtifact();\" scrollable=\"true\" scrollWidth=\"100%\" [rows]=\"defaultInitialItemsPerPage\" paginator=\"true\" pageLinks=\"3\" [rowsPerPageOptions]=\"defaultPagingOptions\">                    \n                            <footer *ngIf=\"dt.totalRecords\"><d3s-grid-paging-info [totalRecords]=\"dt.totalRecords\" [first]=\"dt.first\" [rows]=\"dt.rows\"></d3s-grid-paging-info></footer>\n                            <p-column field=\"Name\" header=\"Name\" sortable=\"custom\" (sortFunction)=\"columnSort($event)\"  [filter]=\"!showSimpleFilter\">\n                                <template let-col let-item=\"rowData\" pTemplate type=\"body\">\n                                    <a (click)=\"artifactLink(item.ArtifactTypeID, item.ID)\">{{item.Name}}</a>\n                                </template>\n                            </p-column>                                                                                                   \n                            <p-column field=\"Status\" header=\"Status\" sortable=\"true\" [filter]=\"!showSimpleFilter\" [style]=\"{'width':'150px'}\"></p-column>\n                            <p-column [style]=\"{width:'40px'}\">\n                                <template let-item=\"rowData\" pTemplate type=\"body\">\n                                    <div class=\"RowTools\">\n                                        <d3s-tooltip [objectType]=\"'Artifact'\" [objectId]=\"item.ID\" [tooltipType]=\"'certificate'\" [icon]=\"'certificate'\" [iconColor]=\"certificateColor(item)\"></d3s-tooltip>                                            \n                                    </div>\n                                </template>\n                            </p-column>\n                            <p-column [style]=\"{width:'40px'}\">\n                                <template let-item=\"rowData\" pTemplate type=\"body\">\n                                    <div class=\"RowTools\">\n                                        <d3s-tooltip [objectType]=\"'Artifact'\" [objectId]=\"item.ID\" (click)=\"selectArtifact(item)\" [tooltipType]=\"'Preview'\" [icon]=\"'info'\"></d3s-tooltip>                                            \n                                    </div>\n                                </template>\n                            </p-column>\n                        </p-dataTable>      \n                    </span>\n                    <button pButton type=\"button\" (click)=\"close.emit();\" label=\"Close\" style=\"width:150px;margin-top:10px\"></button>                    \n                </div>\n                "
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["m" /* ArtifactService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["m" /* ArtifactService */]) === 'function' && _b) || Object])
    ], ActivityDetailsTile);
    return ActivityDetailsTile;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1454:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ActivityTile; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var ActivityTile = (function (_super) {
    __extends(ActivityTile, _super);
    function ActivityTile(artifactService) {
        _super.call(this);
        this.artifactService = artifactService;
        this.counts = [];
        this.isLoaded = false;
        this.daysToLookBack = 7;
        this.daysToLookBackChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.showItemDetail = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    ActivityTile.prototype.ngOnInit = function () {
        if (!this.isLoaded)
            this.load();
    };
    ActivityTile.prototype.load = function () {
        var _this = this;
        this.isLoading = true;
        this.artifactService.getActivityCount(this.daysToLookBack)
            .then(function (res) {
            _this.counts = res;
            _this.isLoading = false;
            _this.isLoaded = true;
        });
    };
    ActivityTile.prototype.doSelect = function (item) {
        this.showItemDetail.emit({
            Id: item.Id,
            name: item.Name
        });
    };
    ActivityTile.prototype.changeDates = function (event) {
        this.daysToLookBack = event.days;
        this.daysToLookBackChange.emit(this.daysToLookBack);
        this.load();
    };
    ActivityTile.prototype.timeFrameMessage = function () {
        switch (this.daysToLookBack) {
            case 7:
                return ' (Past week)';
            case 30:
                return ' (Past month)';
            case 365:
                return ' (Past year)';
        }
        return ' (All Activity)';
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], ActivityTile.prototype, "daysToLookBack", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], ActivityTile.prototype, "daysToLookBackChange", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], ActivityTile.prototype, "showItemDetail", void 0);
    ActivityTile = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-activity-tile',
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["m" /* ArtifactService */]],
            template: "\n                <div class=\"tile tile-detail\">\n                   <header>Activity <span style=\"color:#999;font-size:60%;vertical-align:middle;\">{{timeFrameMessage()}}</span>\n                    <d3s-tile-actions [hasAdd]=\"false\" [hasDate]=\"true\" (dateClick)=\"changeDates($event);\"></d3s-tile-actions>                            \n                   </header>\n                    <d3s-loading [isLoading]=\"isLoading\"></d3s-loading>\n                    <p-dataTable #dt *ngIf=\"!isLoading && counts.length > 0\" sortField=\"Name\" sortOrder=\"1\" [value]=\"counts\" selectionMode=\"single\" [(selection)]=\"selected\" (onRowDblclick)=\"selected=$event.data;doSelect(selected)\" paginator=\"true\" pageLinks=\"3\" [rows]=\"defaultInitialItemsPerPage\" [rowsPerPageOptions]=\"defaultPagingOptions\">                    \n                        <footer *ngIf=\"dt.totalRecords\"><d3s-grid-paging-info [totalRecords]=\"dt.totalRecords\" [first]=\"dt.first\" [rows]=\"dt.rows\"></d3s-grid-paging-info></footer>\n                        <p-column field=\"Name\" header=\"Name\" [sortable]=\"true\">\n                            <template let-item=\"rowData\" pTemplate type=\"body\">\n                                    <a (click)=\"doSelect(item)\">{{item.Name}}</a>\n                            </template>\n                        </p-column>                                                                           \n                        <p-column field=\"New\" header=\"Total\" [sortable]=\"true\" [style]=\"{'text-align':'center'}\"></p-column>                          \n                    </p-dataTable>                      \n                    <div *ngIf=\"counts.length == 0 && !isLoading\" style=\"padding:10px\">No activity for this timeframe</div>                    \n                </div>\n                "
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["m" /* ArtifactService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["m" /* ArtifactService */]) === 'function' && _a) || Object])
    ], ActivityTile);
    return ActivityTile;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1455:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return BoardTile; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var BoardTile = (function (_super) {
    __extends(BoardTile, _super);
    function BoardTile(socialService) {
        _super.call(this);
        this.socialService = socialService;
        this.counts = [];
        this.daysToLookBack = 7;
        this.daysToLookBackChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.showItemDetail = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    BoardTile.prototype.ngOnInit = function () {
        this.load();
    };
    BoardTile.prototype.load = function () {
        var _this = this;
        this.isLoading = true;
        this.socialService.getMyCounts(this.daysToLookBack).then(function (res) {
            _this.counts = res.filter(function (item) { return item.Total > 0; });
            _this.isLoading = false;
        });
    };
    BoardTile.prototype.doSelect = function (item) {
        this.showItemDetail.emit({
            selected: item
        });
    };
    BoardTile.prototype.changeDates = function (event) {
        this.daysToLookBack = event.days;
        this.daysToLookBackChange.emit(this.daysToLookBack);
        this.load();
    };
    BoardTile.prototype.timeFrameMessage = function () {
        switch (this.daysToLookBack) {
            case 7:
                return ' (Past week)';
            case 30:
                return ' (Past month)';
            case 365:
                return ' (Past year)';
        }
        return ' (All)';
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], BoardTile.prototype, "daysToLookBack", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], BoardTile.prototype, "daysToLookBackChange", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], BoardTile.prototype, "showItemDetail", void 0);
    BoardTile = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-board-tile',
            template: "\n                <div class=\"tile tile-detail\">\n                   <header>Board<span style=\"color:#999;font-size:60%;vertical-align:middle;\">{{timeFrameMessage()}}</span>\n                    <d3s-tile-actions [hasAdd]=\"false\" [hasDate]=\"true\" (dateClick)=\"changeDates($event);\"></d3s-tile-actions>                            \n                   </header>\n                    <d3s-loading [isLoading]=\"isLoading\"></d3s-loading>\n                    <p-dataTable *ngIf=\"!isLoading && counts.length > 0\"  sortField=\"Name\" [sortOrder]=\"1\" [value]=\"counts\" selectionMode=\"single\" [(selection)]=\"selected\" (onRowDblclick)=\"selected=$event.data;doSelect(selected)\">                    \n                        <p-column field=\"Name\" header=\"Name\" [sortable]=\"true\">\n                            <template let-item=\"rowData\" pTemplate type=\"body\">\n                                    <a (click)=\"doSelect(item)\">{{item.Name}}</a>\n                            </template>\n                        </p-column>                                                                           \n                        <p-column field=\"Total\" header=\"Total\" [sortable]=\"true\" [style]=\"{'text-align':'center'}\"></p-column>\n                    </p-dataTable>   \n                    <div *ngIf=\"counts.length == 0 && !isLoading\" style=\"padding:10px\">No board activity for this timeframe</div>\n                </div>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["b" /* SocialService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["b" /* SocialService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["b" /* SocialService */]) === 'function' && _a) || Object])
    ], BoardTile);
    return BoardTile;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 1456:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__home_component__ = __webpack_require__(1335);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HomeRoutingModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var routes = [
    {
        path: '',
        component: __WEBPACK_IMPORTED_MODULE_2__home_component__["a" /* HomeComponent */],
    }
];
var HomeRoutingModule = (function () {
    function HomeRoutingModule() {
    }
    HomeRoutingModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"].forChild(routes)],
            exports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"]],
        }), 
        __metadata('design:paramtypes', [])
    ], HomeRoutingModule);
    return HomeRoutingModule;
}());


/***/ },

/***/ 1510:
/***/ function(module, exports) {

module.exports = "<d3s-dashboard-tab *ngIf=\"isDashboardVisible\" [objectID]=\"'0'\" [objectName]=\"'Home'\" [objectType]=\"'Home'\"></d3s-dashboard-tab>\r\n<div class=\"row home-tiles\" *ngIf=\"!showActivityDetails && !showBoardDetails && !showAssignmentDetails && !isDashboardVisible\">\r\n    <div class=\"col l4 s12\">\r\n        <d3s-workflow-assignments (showItemDetail)=\"onShowAssignmentDetails($event);\"></d3s-workflow-assignments>\r\n    </div>\r\n    <div class=\"col l4 s12\">\r\n        <d3s-board-tile (showItemDetail)=\"onShowBoardDetails($event);\" [(daysToLookBack)]=\"boardDaysToLookBack\"></d3s-board-tile>\r\n    </div>\r\n    <div class=\"col l4 s12\">\r\n        <d3s-activity-tile (showItemDetail)=\"onShowActivityDetails($event);\" [(daysToLookBack)]=\"activityDaysToLookBack\"></d3s-activity-tile>\r\n    </div>\r\n</div>\r\n\r\n<div class=\"row\" *ngIf=\"showActivityDetails && !isDashboardVisible\">\r\n    <div class=\"col s12\">\r\n        <d3s-activity-details-tile [objectId]=\"selectedArtifactTypeId\" [objectName]=\"selectedArtifactTypeName\" (close)=\"showActivityDetails = false;\" [daysToLookBack]=\"activityDaysToLookBack\"></d3s-activity-details-tile>\r\n    </div>\r\n</div>\r\n\r\n<div class=\"row\" *ngIf=\"showBoardDetails && !isDashboardVisible\">\r\n    <div class=\"col s12\">\r\n        <div class=\"tile tile-detail\">\r\n            <d3s-social-board [limitToType]=\"selectedSocialType\" [daysToLookBack]=\"boardDaysToLookBack\" [hasNewInput]=\"false\" [hasCloseButton]=\"true\" (close)=\"showBoardDetails=false\"></d3s-social-board>\r\n        </div>\r\n    </div>\r\n</div>\r\n\r\n<div class=\"row\" *ngIf=\"showAssignmentDetails && !isDashboardVisible\">\r\n    <div class=\"col s12\">\r\n        <div class=\"tile tile-detail\">\r\n            <d3s-workflow-detail [workflowType]=\"selectedWorkflowType\" (close)=\"showAssignmentDetails=false\"></d3s-workflow-detail>\r\n        </div>\r\n    </div>\r\n</div>\r\n\r\n<d3s-home-search *ngIf=\"!showActivityDetails && !showBoardDetails && !showAssignmentDetails && !isDashboardVisible\"></d3s-home-search>    \r\n"

/***/ }

});
//# sourceMappingURL=homeChunk.map