webpackJsonp([14],{

/***/ 1157:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__shared_core_module__ = __webpack_require__(1165);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__help_routes__ = __webpack_require__(1452);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__help_component__ = __webpack_require__(1334);
/* harmony export (binding) */ __webpack_require__.d(exports, "HelpModule", function() { return HelpModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};









var HelpModule = (function () {
    function HelpModule() {
    }
    HelpModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["e" /* HttpModule */],
                __WEBPACK_IMPORTED_MODULE_4__angular_router__["RouterModule"],
                //routing 
                __WEBPACK_IMPORTED_MODULE_7__help_routes__["a" /* HelpRoutingModule */],
                //d3s        
                __WEBPACK_IMPORTED_MODULE_6__shared_core_module__["a" /* CoreModule */],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_8__help_component__["a" /* HelpComponent */],
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_3__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], HelpModule);
    return HelpModule;
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

/***/ 1334:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__shared_base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_platform_browser__ = __webpack_require__(100);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_breadcrumb_model__ = __webpack_require__(484);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HelpComponent; });
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





var HelpComponent = (function (_super) {
    __extends(HelpComponent, _super);
    function HelpComponent(titleService, headerBreadcrumbService) {
        _super.call(this);
        this.titleService = titleService;
        this.headerBreadcrumbService = headerBreadcrumbService;
    }
    HelpComponent.prototype.ngOnInit = function () {
        this.setBrowserTitle(this.titleService, 'Help');
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new __WEBPACK_IMPORTED_MODULE_4__models_breadcrumb_model__["a" /* Breadcrumb */]('Help'));
    };
    HelpComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-help-component',
            template: "\n        <div class=\"row\">\n            <div class=\"col s10 offset-s1\">\n                <div class=\"tile tile-detail\">\n                    <header>\n                        Tutorials\n                    </header>\n\n                    <div class=\"row\">\n                        <div class=\"col s12 m4 l4\">\n                            <h4>Security Overview</h4>\n                            <div class=\"directions\">In this session we will see how to create users, groups, and responsibility types.</div>\n                        </div>\n                        <div class=\"col s12 m8 l8\">\n                            <iframe src=\"//fast.wistia.net/embed/playlists/n5dmlh1fmk?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=324&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true\" allowtransparency=\"true\" frameborder=\"0\" scrolling=\"no\" class=\"wistia_playlist\" name=\"wistia_playlist\" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width=\"100%\" height=\"324\"></iframe>\n                        </div>\n                    </div>\n\n                    <div class=\"row\">\n                        <div class=\"col s12 m4 l4\">\n                            <h4>Metamodel Overview</h4>\n                            <div class=\"directions\">In this session we will walk through how to create the various types of assets in the Data3Sixty metamodel, including artifact types, model types, and attribute types.</div>\n                        </div>\n                        <div class=\"col s12 m8 l8\">\n                            <iframe src=\"//fast.wistia.net/embed/playlists/yvgr80adhn?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=316&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true\" allowtransparency=\"true\" frameborder=\"0\" scrolling=\"no\" class=\"wistia_playlist\" name=\"wistia_playlist\" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width=\"100%\" height=\"316\"></iframe>\n                        </div>\n                    </div>\n\n                    <div class=\"row\">\n                        <div class=\"col s12 m4 l4\">\n                            <h4>Relationships Overview</h4>\n                            <div class=\"directions\">In this session we will work with relationships types, explaining how they connect all your Data3Sixty assets together.</div>\n                        </div>\n                        <div class=\"col s12 m8 l8\">\n                            <iframe src=\"//fast.wistia.net/embed/playlists/2k5gywnx3m?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=316&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true\" allowtransparency=\"true\" frameborder=\"0\" scrolling=\"no\" class=\"wistia_playlist\" name=\"wistia_playlist\" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width=\"100%\" height=\"316\"></iframe>\n                        </div>\n                    </div>\n\n                    <div class=\"row\">\n                        <div class=\"col s12 m4 l4\">\n                            <h4>Integration</h4>\n                            <div class=\"directions\">This session covers bulk loading data into the system.</div>\n                        </div>\n                        <div class=\"col s12 m8 l8\">\n                            <iframe src=\"//fast.wistia.net/embed/playlists/jz5e0l1ep9?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=324&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true\" allowtransparency=\"true\" frameborder=\"0\" scrolling=\"no\" class=\"wistia_playlist\" name=\"wistia_playlist\" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width=\"100%\" height=\"324\"></iframe>\n                        </div>\n                    </div>\n\n                    <div class=\"row\">\n                        <div class=\"col s12 m4 l4\">\n                            <h4>Workflow</h4>\n                            <div class=\"directions\">This session covers high-level workflow concepts within the Data3Sixty system.</div>\n                        </div>\n                        <div class=\"col s12 m8 l8\">\n                            <iframe src=\"//fast.wistia.net/embed/playlists/bs2wblakyv?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=360&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true\" allowtransparency=\"true\" frameborder=\"0\" scrolling=\"no\" class=\"wistia_playlist\" name=\"wistia_playlist\" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width=\"100%\" height=\"360\"></iframe>\n                        </div>\n                    </div>\n\n                    <div class=\"row\">\n                        <div class=\"col s12 m4 l4\">\n                            <h4>Metrics</h4>\n                            <div class=\"directions\">These sessions give an overview of the options for creating dashboards, reports, and analytics within Data3Sixty.</div>\n                        </div>\n                        <div class=\"col s12 m8 l8\">\n                            <iframe src=\"//fast.wistia.net/embed/playlists/tqayidfa9t?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=360&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true\" allowtransparency=\"true\" frameborder=\"0\" scrolling=\"no\" class=\"wistia_playlist\" name=\"wistia_playlist\" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width=\"100%\" height=\"360\"></iframe>\n                        </div>\n                    </div>\n\n                </div>                \n            </div>\n        </div>\n         "
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__angular_platform_browser__["Title"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__angular_platform_browser__["Title"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["g" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["g" /* HeaderBreadcrumbService */]) === 'function' && _b) || Object])
    ], HelpComponent);
    return HelpComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_1__shared_base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 1452:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__help_component__ = __webpack_require__(1334);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HelpRoutingModule; });
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
    { path: '', component: __WEBPACK_IMPORTED_MODULE_2__help_component__["a" /* HelpComponent */] },
];
var HelpRoutingModule = (function () {
    function HelpRoutingModule() {
    }
    HelpRoutingModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"].forChild(routes)],
            exports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"]],
        }), 
        __metadata('design:paramtypes', [])
    ], HelpRoutingModule);
    return HelpRoutingModule;
}());


/***/ }

});
//# sourceMappingURL=helpChunk.map