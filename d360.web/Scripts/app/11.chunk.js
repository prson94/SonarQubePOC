webpackJsonp([11],{

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


/***/ }

});
//# sourceMappingURL=workflowChunk.map