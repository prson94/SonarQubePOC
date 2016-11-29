webpackJsonp([16],{

/***/ 113:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__models_rightsidebar_model__ = __webpack_require__(493);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__models_permission_model__ = __webpack_require__(633);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__static_string_constants__ = __webpack_require__(490);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return BaseComponent; });



var BaseComponent = (function () {
    function BaseComponent(rightSidebarService, webAnalyticsService) {
        this.rightSidebarService = rightSidebarService;
        this.webAnalyticsService = webAnalyticsService;
        this.isLoading = false;
        //tabs
        this.isAuditVisible = false;
        this.isOwnershipVisible = false;
        this.isDashboardVisible = false;
        this.isLineageVisible = false;
        this.isImpactVisible = false;
        this.isRelationshipsVisible = false;
        this.isFollowersVisible = false;
        //filter mode
        this.showSimpleFilter = true;
        //permissions
        // Ideally this should be an input so we dont have to copy / past it...
        // child classes that support permissions input....
        this.permissions = [];
        //default paging options
        this.defaultPagingOptions = [10, 25, 50, 100];
        this.defaultInitialItemsPerPage = 10;
    }
    BaseComponent.prototype.setBrowserTitle = function (tileService, area) {
        tileService.setTitle("D3S - " + area);
    };
    BaseComponent.prototype.logAction = function (actionName, objectName, objectId) {
        if (this.webAnalyticsService) {
            this.webAnalyticsService.logActivity({
                Activity: actionName,
                ObjectId: objectId,
                ObjectName: objectName
            });
        }
    };
    /*permissions functionality */
    BaseComponent.prototype.loadPermissions = function (permissionsService, objectType, objectID) {
        var _this = this;
        permissionsService.getPermissions(objectID, objectType)
            .then(function (result) {
            _this.permissions = result;
        });
    };
    BaseComponent.prototype.hasPermission = function (object, claim) { return __WEBPACK_IMPORTED_MODULE_1__models_permission_model__["a" /* Permission */].hasPermission(this.permissions, object, claim); };
    BaseComponent.prototype.hasCreatePermissions = function (object) { return this.hasPermission(object, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimCreate); };
    BaseComponent.prototype.hasDeletePermissions = function (object) { return this.hasPermission(object, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimDelete); };
    BaseComponent.prototype.hasUpdatePermissions = function (object) { return this.hasPermission(object, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimUpdate); };
    BaseComponent.prototype.hasReadPermissions = function (object) { return this.hasPermission(object, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimRead); };
    BaseComponent.prototype.hasRootCreatePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectRoot, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimCreate); };
    BaseComponent.prototype.hasRootDeletePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectRoot, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimDelete); };
    BaseComponent.prototype.hasRootUpdatePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectRoot, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimUpdate); };
    BaseComponent.prototype.hasRootReadPermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectRoot, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimRead); };
    BaseComponent.prototype.hasRelationshipCreatePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectRelationship, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimCreate); };
    BaseComponent.prototype.hasRelationshipDeletePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectRelationship, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimDelete); };
    BaseComponent.prototype.hasRelationshipUpdatePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectRelationship, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimUpdate); };
    BaseComponent.prototype.hasRelationshipReadPermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectRelationship, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimRead); };
    BaseComponent.prototype.hasAttributeCreatePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectAttribute, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimCreate); };
    BaseComponent.prototype.hasAttributeDeletePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectAttribute, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimDelete); };
    BaseComponent.prototype.hasAttributeUpdatePermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectAttribute, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimUpdate); };
    BaseComponent.prototype.hasAttributeReadPermissions = function () { return this.hasPermission(__WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ObjectAttribute, __WEBPACK_IMPORTED_MODULE_2__static_string_constants__["a" /* StringConstants */].ClaimRead); };
    /*end permissions functionality*/
    BaseComponent.prototype.setCommonRightSideBar = function (hasAudit, hasOwnership, hasDashboard, hasLineage, hasImpact, hasRelationships, hasFollowers) {
        var _this = this;
        if (this.rightSidebarService) {
            if (hasLineage)
                this.rightSidebarService.showItem(new __WEBPACK_IMPORTED_MODULE_0__models_rightsidebar_model__["a" /* RightSidebarItem */]('Lineage', 'lineage', ['fa-random']));
            if (hasAudit || hasAudit === undefined)
                this.rightSidebarService.showItem(new __WEBPACK_IMPORTED_MODULE_0__models_rightsidebar_model__["a" /* RightSidebarItem */]('Audit', 'audit', ['fa-eye']));
            if (hasOwnership)
                this.rightSidebarService.showItem(new __WEBPACK_IMPORTED_MODULE_0__models_rightsidebar_model__["a" /* RightSidebarItem */]('Ownership', 'ownership', ['fa-user']));
            if (hasDashboard)
                this.rightSidebarService.showItem(new __WEBPACK_IMPORTED_MODULE_0__models_rightsidebar_model__["a" /* RightSidebarItem */]('Dashboards', 'dashboards', ['fa-tachometer']));
            if (hasImpact)
                this.rightSidebarService.showItem(new __WEBPACK_IMPORTED_MODULE_0__models_rightsidebar_model__["a" /* RightSidebarItem */]('Impact', 'impact', ['fa-exchange']));
            if (hasRelationships)
                this.rightSidebarService.showItem(new __WEBPACK_IMPORTED_MODULE_0__models_rightsidebar_model__["a" /* RightSidebarItem */]('Relations', 'relationship', ['fa-retweet']));
            if (hasFollowers)
                this.rightSidebarService.showItem(new __WEBPACK_IMPORTED_MODULE_0__models_rightsidebar_model__["a" /* RightSidebarItem */]('Followers', 'followers', ['fa-bookmark-o']));
            this.sidebarSubscription = this.rightSidebarService.rightSidebarClicked$.subscribe(function (item) {
                if (item.tag == 'audit')
                    _this.isAuditVisible = !_this.isAuditVisible;
                else if (item.tag == 'ownership')
                    _this.isOwnershipVisible = !_this.isOwnershipVisible;
                else if (item.tag == 'dashboards')
                    _this.isDashboardVisible = !_this.isDashboardVisible;
                else if (item.tag == 'lineage')
                    _this.isLineageVisible = !_this.isLineageVisible;
                else if (item.tag == 'impact')
                    _this.isImpactVisible = !_this.isImpactVisible;
                else if (item.tag == 'relationship')
                    _this.isRelationshipsVisible = !_this.isRelationshipsVisible;
                else if (item.tag == 'followers')
                    _this.isFollowersVisible = !_this.isFollowersVisible;
                else
                    _this.showHideBreadcrumbItem(item);
            });
        }
    };
    BaseComponent.prototype.hideSidebarItems = function () {
        this.isAuditVisible = false;
        this.isOwnershipVisible = false;
        this.isDashboardVisible = false;
        this.isFollowersVisible = false;
        this.isImpactVisible = false;
        this.isLineageVisible = false;
        this.isRelationshipsVisible = false;
    };
    //This is generally overloaded to show hide in your own class.
    BaseComponent.prototype.showHideBreadcrumbItem = function (activatedItem) {
        //console.log('show/hide :');
        //console.log(activatedItem);
    };
    BaseComponent.prototype.clearSidebar = function (unsubscribe) {
        if (this.rightSidebarService) {
            this.rightSidebarService.clearItems();
            if (this.sidebarSubscription && (unsubscribe || unsubscribe == undefined)) {
                //console.log("DEV INFO - UNSUBSCRIBING FROM RIGHT SIDE BAR SUBSCRIPTION");
                this.sidebarSubscription.unsubscribe();
            }
        }
    };
    BaseComponent.prototype.showMessageForResult = function (messagesService, result) {
        if (result.type == 'error')
            messagesService.showError(result.title, result.message);
        else
            messagesService.showInfoMessage(result.title, result.message);
    };
    return BaseComponent;
}());


/***/ },

/***/ 1147:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_platform_browser_dynamic__ = __webpack_require__(199);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__app_module__ = __webpack_require__(459);



if (window.location.href.indexOf('.local') < 0) {
    __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_1__angular_core__["enableProdMode"])();
}
else {
    console.log("Running in d3s developer mode...");
}
__webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_platform_browser_dynamic__["a" /* platformBrowserDynamic */])().bootstrapModule(__WEBPACK_IMPORTED_MODULE_2__app_module__["a" /* AppModule */]);


/***/ },

/***/ 115:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SortOrder; });
var SortOrder;
(function (SortOrder) {
    SortOrder[SortOrder["Descending"] = -1] = "Descending";
    SortOrder[SortOrder["None"] = 0] = "None";
    SortOrder[SortOrder["Ascending"] = 1] = "Ascending";
})(SortOrder || (SortOrder = {}));


/***/ },

/***/ 143:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Observable__ = __webpack_require__(1);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Observable___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_rxjs_Observable__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2_rxjs_add_operator_catch__ = __webpack_require__(432);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2_rxjs_add_operator_catch___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_2_rxjs_add_operator_catch__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_rxjs_add_observable_throw__ = __webpack_require__(431);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3_rxjs_add_observable_throw___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_3_rxjs_add_observable_throw__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return AuthenticationConnectionBackend; });
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};




var AuthenticationConnectionBackend = (function (_super) {
    __extends(AuthenticationConnectionBackend, _super);
    function AuthenticationConnectionBackend(_browserXhr, _baseResponseOptions, _xsrfStrategy) {
        _super.call(this, _browserXhr, _baseResponseOptions, _xsrfStrategy);
    }
    AuthenticationConnectionBackend.prototype.createConnection = function (request) {
        var xhrConnection = _super.prototype.createConnection.call(this, request);
        xhrConnection.response = xhrConnection.response.catch(function (error) {
            if ((error.status === 401 || error.status === 403) && (window.location.href.match(/\?/g) || []).length < 2) {
                console.log('The authentication session expires or the user is not authorized. Forcing refresh of the current page.');
                window.location.href = window.location.href + '?' + new Date().getMilliseconds();
            }
            return __WEBPACK_IMPORTED_MODULE_1_rxjs_Observable__["Observable"].throw(error);
        });
        return xhrConnection;
    };
    return AuthenticationConnectionBackend;
}(__WEBPACK_IMPORTED_MODULE_0__angular_http__["d" /* XHRBackend */]));


/***/ },

/***/ 144:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "e", function() { return BaseEditorModel; });
/* unused harmony export SelectItem */
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return FormHelper; });
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return MessageType; });
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return FormMessage; });
/* harmony export (binding) */ __webpack_require__.d(exports, "d", function() { return FormMode; });
var BaseEditorModel = (function () {
    function BaseEditorModel() {
    }
    return BaseEditorModel;
}());
var SelectItem = (function () {
    function SelectItem() {
    }
    return SelectItem;
}());
var FormHelper;
(function (FormHelper) {
    function mapSelectItems(s) {
        s.forEach(function (s) { s.value = s.Value; s.label = s.Text; });
    }
    FormHelper.mapSelectItems = mapSelectItems;
    function getSelectList(items, label, value) {
        if (label === void 0) { label = 'label'; }
        if (value === void 0) { value = 'value'; }
        var list = new Array();
        items.forEach(function (i) {
            var l = new SelectItem();
            l.label = i[label];
            l.Text = i[label];
            l.Value = i[value];
            l.value = i[value];
            list.push(l);
        });
        return list;
    }
    FormHelper.getSelectList = getSelectList;
    function getDataUrl(file) {
        var reader = new FileReader();
        return new Promise(function (resolve, reject) {
            reader.onloadend = function () {
                resolve(reader.result);
            };
            reader.readAsDataURL(file);
        }).then(function () {
            //console.log(reader.result);
            return reader.result;
        });
    }
    FormHelper.getDataUrl = getDataUrl;
    function formTree(data, idField, parentField, expandAll) {
        if (idField === void 0) { idField = 'ID'; }
        if (parentField === void 0) { parentField = 'ParentID'; }
        if (expandAll === void 0) { expandAll = true; }
        var tree = new Array();
        if (data && data.filter) {
            data.filter(function (d) { return d[parentField] == null; }).forEach(function (d) {
                tree.push({ data: d, children: [], expanded: expandAll });
            });
            tree.forEach(function (t) {
                FormHelper.formTreeR(t, data, idField, parentField, expandAll);
            });
        }
        //console.log(tree);
        return tree;
    }
    FormHelper.formTree = formTree;
    function formTreeR(node, data, idField, parentField, expandAll) {
        if (expandAll === void 0) { expandAll = true; }
        data.filter(function (d) { return d[parentField] == node.data[idField]; }).forEach(function (d) {
            var child = { data: d, children: [], expanded: expandAll };
            node.children.push(child);
            FormHelper.formTreeR(child, data, idField, parentField, expandAll);
        });
    }
    FormHelper.formTreeR = formTreeR;
    function flattenTree(data, subDataField, idField, parentField) {
        if (idField === void 0) { idField = null; }
        if (parentField === void 0) { parentField = null; }
        var flattened = [];
        for (var i = 0; i < data.length; i++) {
            flattened.push(data[i]);
            if (data[i][subDataField] && data[i][subDataField].length > 0) {
                var sub = flattenTree(data[i][subDataField], subDataField, idField, parentField);
                sub.forEach(function (s) {
                    if (idField && parentField)
                        s[parentField] = data[i][idField];
                    flattened.push(s);
                });
            }
        }
        return flattened;
    }
    FormHelper.flattenTree = flattenTree;
    function convertToolBarToMenuItem(data) {
        var items = [];
        for (var i = 0; i < data.length; i++) {
            var m = {};
            m.icon = 'fa-' + data[i].Icon;
            m.label = data[i].Title;
            m.url = data[i].Uri;
            if (data[i].Items.length > 0)
                m.items = convertToolBarToMenuItem(data[i].Items);
            items.push(m);
        }
        return items;
    }
    FormHelper.convertToolBarToMenuItem = convertToolBarToMenuItem;
    function convertToNgUrl(data, field) {
        for (var _i = 0, data_1 = data; _i < data_1.length; _i++) {
            var d = data_1[_i];
            d[field] = (d[field]).replace('#', 'a');
            d[field] = (d[field]).replace('artifacts', 'artifact');
        }
        return data;
    }
    FormHelper.convertToNgUrl = convertToNgUrl;
})(FormHelper || (FormHelper = {}));
var MessageType;
(function (MessageType) {
    MessageType[MessageType["Error"] = 0] = "Error";
    MessageType[MessageType["Success"] = 1] = "Success";
    MessageType[MessageType["Info"] = 2] = "Info";
    MessageType[MessageType["Warning"] = 3] = "Warning";
})(MessageType || (MessageType = {}));
var FormMessage = (function () {
    function FormMessage() {
        this.Visible = true;
    }
    FormMessage.prototype.Success = function (msg) {
        this.MessageType = MessageType.Success;
        this.Message = msg;
    };
    FormMessage.prototype.Info = function (msg) {
        this.MessageType = MessageType.Info;
        this.Message = msg;
    };
    FormMessage.prototype.Error = function (msg) {
        this.MessageType = MessageType.Error;
        this.Message = msg;
    };
    FormMessage.prototype.Warning = function (msg) {
        this.MessageType = MessageType.Warning;
        this.Message = msg;
    };
    Object.defineProperty(FormMessage.prototype, "isError", {
        get: function () {
            return this.MessageType == MessageType.Error;
        },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(FormMessage.prototype, "isSuccess", {
        get: function () {
            return this.MessageType == MessageType.Success;
        },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(FormMessage.prototype, "isInfo", {
        get: function () {
            return this.MessageType == MessageType.Info;
        },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(FormMessage.prototype, "isWarning", {
        get: function () {
            return this.MessageType == MessageType.Warning;
        },
        enumerable: true,
        configurable: true
    });
    return FormMessage;
}());
var FormMode;
(function (FormMode) {
    FormMode[FormMode["Default"] = 1] = "Default";
    FormMode[FormMode["Editing"] = 2] = "Editing";
    FormMode[FormMode["Adding"] = 3] = "Adding";
    FormMode[FormMode["Deleting"] = 4] = "Deleting";
})(FormMode || (FormMode = {}));


/***/ },

/***/ 145:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__ = __webpack_require__(15);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderBreadcrumbService; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};


var HeaderBreadcrumbService = (function () {
    function HeaderBreadcrumbService() {
        // Observable sources
        this.breadcrumbSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.breadcrumbClearSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.breadcrumbTreeSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.breadcrumbPopLastSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.currentObjectInfoSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        // Observable streams
        this.breadcrumbs$ = this.breadcrumbSource.asObservable();
        this.breadcrumbClear$ = this.breadcrumbClearSource.asObservable();
        this.breadcrumbTreeSource$ = this.breadcrumbTreeSource.asObservable();
        this.breadcrumbPopLastSource$ = this.breadcrumbPopLastSource.asObservable();
        this.currentObjectInfo$ = this.currentObjectInfoSource.asObservable();
    }
    // Service message commands
    HeaderBreadcrumbService.prototype.clearCurrentObjectInfo = function () {
        this.currentObject = { type: null, id: null };
        this.currentObjectInfoSource.next({ type: null, id: null });
    };
    HeaderBreadcrumbService.prototype.setCurrentObjectInfo = function (type, id) {
        this.currentObject = { type: type, id: id };
        this.currentObjectInfoSource.next({ type: type, id: id });
    };
    HeaderBreadcrumbService.prototype.showBreadcrumb = function (breadcrumb) {
        this.breadcrumbSource.next(breadcrumb);
    };
    HeaderBreadcrumbService.prototype.clearBreadcrumbs = function () {
        this.breadcrumbClearSource.next(true);
    };
    HeaderBreadcrumbService.prototype.breadcrumbTreeClick = function (id) {
        this.breadcrumbTreeSource.next(id);
    };
    HeaderBreadcrumbService.prototype.popLastBreadcrumb = function () {
        this.breadcrumbPopLastSource.next(true);
    };
    HeaderBreadcrumbService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [])
    ], HeaderBreadcrumbService);
    return HeaderBreadcrumbService;
}());


/***/ },

/***/ 177:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__ = __webpack_require__(15);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderActionsService; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};


var HeaderActionsService = (function () {
    function HeaderActionsService() {
        this.showFavorite = true;
        this.showNotifications = false;
        this.showLegacy = false;
        this.showHelp = true;
        this.showSearch = true;
        this.showRaiseIssue = false;
        this.showFollow = true;
        // Observable sources
        this.onFavoritesChangeSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.onFavoritesChanges$ = this.onFavoritesChangeSource.asObservable();
        this.onSiteNavChangeSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.onSiteNavChanges$ = this.onSiteNavChangeSource.asObservable();
    }
    HeaderActionsService.prototype.emitFavoritesChange = function () {
        this.onFavoritesChangeSource.next();
    };
    HeaderActionsService.prototype.emitSiteNavChange = function () {
        this.onSiteNavChangeSource.next();
    };
    HeaderActionsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [])
    ], HeaderActionsService);
    return HeaderActionsService;
}());


/***/ },

/***/ 200:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return SiteMenuItem; });
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return SiteMenu; });
/* unused harmony export SiteMenuModel */
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SiteNav; });
var SiteMenuItem = (function () {
    function SiteMenuItem() {
    }
    return SiteMenuItem;
}());
var SiteMenu = (function () {
    function SiteMenu() {
        this.isActiveItem = false;
    }
    return SiteMenu;
}());
var SiteMenuModel = (function () {
    function SiteMenuModel() {
        this.IsAdmin = false;
    }
    return SiteMenuModel;
}());
var SiteNav = (function () {
    function SiteNav() {
        this.IsCustom = false;
    }
    SiteNav.zindex = 1000;
    return SiteNav;
}());


/***/ },

/***/ 256:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Rx__ = __webpack_require__(293);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Rx___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_rxjs_Rx__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return AuthenticationService; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};


var AuthenticationService = (function () {
    function AuthenticationService() {
        this.admin$ = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Rx__["Subject"]();
    }
    AuthenticationService.prototype.admin = function () {
        return this.admin$;
    };
    AuthenticationService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [])
    ], AuthenticationService);
    return AuthenticationService;
}());


/***/ },

/***/ 294:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* unused harmony export GridField */
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return GridColumn; });
/* harmony export (binding) */ __webpack_require__.d(exports, "e", function() { return GridRelationshipFilterExpression; });
/* harmony export (binding) */ __webpack_require__.d(exports, "f", function() { return GridAttributeFilterExpression; });
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return GridFilterFieldType; });
/* harmony export (binding) */ __webpack_require__.d(exports, "d", function() { return GridFilterExpression; });
/* unused harmony export GridFilterColumn */
/* unused harmony export GridDefinition */
/* unused harmony export DynamicGridDefinitionBase */
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return LookupGrid; });
/* unused harmony export DynamicGridResultsInData */
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var GridField = (function () {
    function GridField() {
    }
    return GridField;
}());
var GridColumn = (function () {
    function GridColumn() {
    }
    return GridColumn;
}());
var GridRelationshipFilterExpression = (function () {
    function GridRelationshipFilterExpression() {
        this.includeType = "Any";
    }
    return GridRelationshipFilterExpression;
}());
var GridAttributeFilterExpression = (function () {
    function GridAttributeFilterExpression() {
    }
    return GridAttributeFilterExpression;
}());
var GridFilterFieldType;
(function (GridFilterFieldType) {
    GridFilterFieldType[GridFilterFieldType["Normal"] = 0] = "Normal";
    GridFilterFieldType[GridFilterFieldType["Hidden"] = 1] = "Hidden";
    GridFilterFieldType[GridFilterFieldType["Relation"] = 2] = "Relation";
})(GridFilterFieldType || (GridFilterFieldType = {}));
var GridFilterExpression = (function () {
    function GridFilterExpression() {
    }
    return GridFilterExpression;
}());
var GridFilterColumn = (function () {
    function GridFilterColumn() {
    }
    return GridFilterColumn;
}());
var GridDefinition = (function () {
    function GridDefinition() {
    }
    return GridDefinition;
}());
var DynamicGridDefinitionBase = (function () {
    function DynamicGridDefinitionBase() {
    }
    return DynamicGridDefinitionBase;
}());
var LookupGrid = (function (_super) {
    __extends(LookupGrid, _super);
    function LookupGrid() {
        _super.apply(this, arguments);
    }
    return LookupGrid;
}(DynamicGridDefinitionBase));
var DynamicGridResultsInData = (function (_super) {
    __extends(DynamicGridResultsInData, _super);
    function DynamicGridResultsInData() {
        _super.apply(this, arguments);
    }
    return DynamicGridResultsInData;
}(DynamicGridDefinitionBase));


/***/ },

/***/ 299:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_compiler__ = __webpack_require__(101);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_4_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return DynamicTypeBuilder; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};





var DynamicTypeBuilder = (function () {
    // wee need Dynamic component builder
    function DynamicTypeBuilder(compiler) {
        this.compiler = compiler;
        // this object is singleton - so we can use this as a cache
        this._cacheOfFactories = {};
    }
    DynamicTypeBuilder.prototype.createComponentFactory = function (template) {
        var _this = this;
        var factory = this._cacheOfFactories[template];
        /*   if (factory) {
               console.log("Module and Type are returned from cache")
   
               return new Promise((resolve) => {
                   resolve(factory);
               });
           }*/
        // unknown template ... let's create a Type for it
        var type = this.createNewComponent(template);
        var module = this.createComponentModule(type);
        return new Promise(function (resolve) {
            _this.compiler
                .compileModuleAndAllComponentsAsync(module)
                .then(function (moduleWithFactories) {
                factory = __WEBPACK_IMPORTED_MODULE_4_lodash__["find"](moduleWithFactories.componentFactories, { componentType: type });
                //   this._cacheOfFactories[template] = factory;
                resolve(factory);
            });
        });
    };
    DynamicTypeBuilder.prototype.createNewComponent = function (tmpl) {
        var CustomDynamicComponent = (function () {
            function CustomDynamicComponent() {
            }
            CustomDynamicComponent = __decorate([
                __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_2__angular_core__["Component"])({
                    selector: 'dynamic-component',
                    template: tmpl,
                }), 
                __metadata('design:paramtypes', [])
            ], CustomDynamicComponent);
            return CustomDynamicComponent;
        }());
        ;
        // a component for this particular template
        return CustomDynamicComponent;
    };
    DynamicTypeBuilder.prototype.createComponentModule = function (componentType) {
        var RuntimeComponentModule = (function () {
            function RuntimeComponentModule() {
            }
            RuntimeComponentModule = __decorate([
                __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_2__angular_core__["NgModule"])({
                    imports: [
                        __WEBPACK_IMPORTED_MODULE_0__angular_common__["CommonModule"],
                        __WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"],
                    ],
                    declarations: [
                        componentType
                    ],
                }), 
                __metadata('design:paramtypes', [])
            ], RuntimeComponentModule);
            return RuntimeComponentModule;
        }());
        // a module for just this Type
        return RuntimeComponentModule;
    };
    DynamicTypeBuilder = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_2__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__angular_compiler__["c" /* RuntimeCompiler */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__angular_compiler__["c" /* RuntimeCompiler */]) === 'function' && _a) || Object])
    ], DynamicTypeBuilder);
    return DynamicTypeBuilder;
    var _a;
}());


/***/ },

/***/ 459:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_platform_browser__ = __webpack_require__(100);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__app_component__ = __webpack_require__(614);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__app_routes__ = __webpack_require__(615);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__angular_compiler__ = __webpack_require__(101);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_6_primeng_primeng__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__components_shared_rightsidebar_right_sidebar_module__ = __webpack_require__(631);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__components_shared_menu_site_menu_module__ = __webpack_require__(628);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__components_shared_header_header_module__ = __webpack_require__(623);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_10__guards_admin_user_guard__ = __webpack_require__(498);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11__services_authentication_service__ = __webpack_require__(256);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_12__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_13__services_dynamic_type_builder__ = __webpack_require__(299);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_14__authentication_connection_backend__ = __webpack_require__(143);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return AppModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};















var AppModule = (function () {
    function AppModule() {
    }
    AppModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            declarations: [
                __WEBPACK_IMPORTED_MODULE_2__app_component__["a" /* AppComponent */],
            ],
            imports: [
                __WEBPACK_IMPORTED_MODULE_1__angular_platform_browser__["BrowserModule"],
                __WEBPACK_IMPORTED_MODULE_3__app_routes__["a" /* AppRoutingModule */],
                __WEBPACK_IMPORTED_MODULE_4__angular_http__["e" /* HttpModule */],
                // prime 
                __WEBPACK_IMPORTED_MODULE_6_primeng_primeng__["GrowlModule"],
                //d3s modules                                            
                __WEBPACK_IMPORTED_MODULE_7__components_shared_rightsidebar_right_sidebar_module__["a" /* RightsidebarModule */],
                __WEBPACK_IMPORTED_MODULE_8__components_shared_menu_site_menu_module__["a" /* SiteMenuModule */],
                __WEBPACK_IMPORTED_MODULE_9__components_shared_header_header_module__["a" /* HeaderModule */],
            ],
            bootstrap: [__WEBPACK_IMPORTED_MODULE_2__app_component__["a" /* AppComponent */]],
            providers: [
                __WEBPACK_IMPORTED_MODULE_10__guards_admin_user_guard__["a" /* AdminUserGuard */],
                { provide: __WEBPACK_IMPORTED_MODULE_4__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_14__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
                __WEBPACK_IMPORTED_MODULE_11__services_authentication_service__["a" /* AuthenticationService */],
                __WEBPACK_IMPORTED_MODULE_5__angular_compiler__["d" /* COMPILER_PROVIDERS */],
                __WEBPACK_IMPORTED_MODULE_13__services_dynamic_type_builder__["a" /* DynamicTypeBuilder */],
                __WEBPACK_IMPORTED_MODULE_1__angular_platform_browser__["Title"],
                __WEBPACK_IMPORTED_MODULE_12__services_index__["o" /* HeaderActionsService */],
                __WEBPACK_IMPORTED_MODULE_12__services_index__["g" /* HeaderBreadcrumbService */],
                __WEBPACK_IMPORTED_MODULE_12__services_index__["a" /* MessagesService */],
                __WEBPACK_IMPORTED_MODULE_12__services_index__["i" /* RightSidebarService */],
                __WEBPACK_IMPORTED_MODULE_12__services_index__["h" /* WebAnalyticsService */],
                __WEBPACK_IMPORTED_MODULE_12__services_index__["y" /* StateService */]
            ],
        }), 
        __metadata('design:paramtypes', [])
    ], AppModule);
    return AppModule;
}());


/***/ },

/***/ 484:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return Breadcrumb; });
/* unused harmony export BreadcrumbItem */
var Breadcrumb = (function () {
    function Breadcrumb(text, link, active, type, objectId, treeItems, selectedTreeNode) {
        this.text = "-";
        this.link = null;
        this.text = text === undefined ? "-" : text;
        this.link = link === undefined ? null : link;
        this.active = active === undefined ? false : active;
        this.objectType = type === undefined ? undefined : type;
        this.objectId = objectId === undefined ? undefined : objectId;
        this.treeItems = treeItems === undefined ? undefined : treeItems;
        this.selectedTreeNode = selectedTreeNode === undefined ? undefined : selectedTreeNode;
    }
    Breadcrumb.prototype.hasLink = function () {
        return (this.link && this.link.length > 0 && !this.active);
    };
    return Breadcrumb;
}());
var BreadcrumbItem = (function () {
    function BreadcrumbItem() {
    }
    return BreadcrumbItem;
}());


/***/ },

/***/ 485:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__dropdown_to_selectitem_pipe__ = __webpack_require__(635);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__model_type_pipe__ = __webpack_require__(636);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__score_display_pipe__ = __webpack_require__(637);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__technical_to_display_pipe__ = __webpack_require__(638);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__tree_search_pipe__ = __webpack_require__(639);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return PipesModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};







var PipesModule = (function () {
    function PipesModule() {
    }
    PipesModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"]],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_6__tree_search_pipe__["a" /* TreeSearchPipe */],
                __WEBPACK_IMPORTED_MODULE_2__dropdown_to_selectitem_pipe__["a" /* DropdownItemToSelectItemPipe */],
                __WEBPACK_IMPORTED_MODULE_3__model_type_pipe__["a" /* ModelTypePipe */],
                __WEBPACK_IMPORTED_MODULE_4__score_display_pipe__["a" /* ScoreDisplayPipe */],
                __WEBPACK_IMPORTED_MODULE_5__technical_to_display_pipe__["a" /* TechnicalNameToDisplayValuePipe */],
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_6__tree_search_pipe__["a" /* TreeSearchPipe */],
                __WEBPACK_IMPORTED_MODULE_2__dropdown_to_selectitem_pipe__["a" /* DropdownItemToSelectItemPipe */],
                __WEBPACK_IMPORTED_MODULE_3__model_type_pipe__["a" /* ModelTypePipe */],
                __WEBPACK_IMPORTED_MODULE_4__score_display_pipe__["a" /* ScoreDisplayPipe */],
                __WEBPACK_IMPORTED_MODULE_5__technical_to_display_pipe__["a" /* TechnicalNameToDisplayValuePipe */],
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], PipesModule);
    return PipesModule;
}());


/***/ },

/***/ 486:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_form_model__ = __webpack_require__(144);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ObjectDetailService; });
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





var ObjectDetailService = (function (_super) {
    __extends(ObjectDetailService, _super);
    function ObjectDetailService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ObjectDetailService.prototype.getObjectDetail = function (objectID, objectType) {
        var _this = this;
        return this.http.get("api/" + objectType + "/" + objectID + "/detail")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.getObject = function (objectID, objectType) {
        var _this = this;
        return this.http.get("api/" + objectType + "/" + objectID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.getObjectSynonyms = function (objectID, objectType) {
        var _this = this;
        return this.http.get("api/" + objectType + "/" + objectID + "/synonyms")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.getSynonymTypes = function (objectID, objectType) {
        var _this = this;
        return this.http.get("form/SynonymTypes?id=" + objectID + "&type=" + objectType)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.getSynonymOptions = function (type, typeId, object, objectId, query) {
        //string type, int typeId, string obj, int objId, string query = ""
        var _this = this;
        if (query === void 0) { query = ''; }
        return this.http.get("form/SynonymsOptions?type=" + type + "&typeId=" + typeId + "&obj=" + object + "&objid=" + objectId + "&query=" + query)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) {
            r.items.forEach(function (i) {
                i.ID = i[0].Value;
                i.Name = i[1].Value;
                i.TargetingSubject = i[2].Value;
            });
            return r;
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.postSynonym = function (model) {
        var _this = this;
        return this.http.post('form/AddSynonym', model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.getAttributeHierarchyItems = function (objectID, objectType) {
        var _this = this;
        return this.http.get("attributes/hierarchy/" + objectType + "/" + objectID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.getAttributeHierarchyTree = function (objectID, objectType) {
        return this.getAttributeHierarchyItems(objectID, objectType).then(function (result) {
            var data = __WEBPACK_IMPORTED_MODULE_4__models_form_model__["a" /* FormHelper */].flattenTree(result, 'Items', 'ID', 'ParentUID');
            return __WEBPACK_IMPORTED_MODULE_4__models_form_model__["a" /* FormHelper */].formTree(data, 'ID', 'ParentUID');
        });
    };
    ObjectDetailService.prototype.getAttributeActions = function (objectID, objectType, ownerID, ownerType, attributeID) {
        var _this = this;
        if (attributeID === void 0) { attributeID = null; }
        return this.http.get("attributes/AttributeActionsNg?id=" + objectID + "&type=" + objectType + "&ownerID=" + ownerID + "&owner=" + ownerID + "&attributeID=" + attributeID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.getRelationsHierarchy = function (predicateType, type, id) {
        var _this = this;
        return this.http.get("relations/hierarchy/" + predicateType + "/" + type + "/" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService.prototype.getRelationsHierarchyTree = function (predicateType, type, id) {
        return this.getRelationsHierarchy(predicateType, type, id).then(function (result) {
            return __WEBPACK_IMPORTED_MODULE_4__models_form_model__["a" /* FormHelper */].formTree(result, 'UID', 'ParentID');
        });
    };
    ObjectDetailService.prototype.testDynamicParams = function () {
        var params = [];
        params.push(1);
        params.push('bob');
        params.push(3);
        params.push(4);
        return this.http.post('form/dynamiceditor/new/attribute', params)
            .toPromise()
            .then(function (result) { return result.json(); });
    };
    //TODO: make explicit call here instead of passing uri
    ObjectDetailService.prototype.getLookupGrid = function (uri) {
        var _this = this;
        return this.http.get(uri)
            .toPromise()
            .then(function (result) { return result.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectDetailService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ObjectDetailService);
    return ObjectDetailService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 487:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_form_model__ = __webpack_require__(144);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return FusionService; });
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





var FusionService = (function (_super) {
    __extends(FusionService, _super);
    function FusionService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    FusionService.prototype.getFusionTypes = function (query) {
        var _this = this;
        if (query === void 0) { query = ''; }
        return this.http.get("services/fusion?" + query)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionAttributeTypes = function (id, query) {
        var _this = this;
        if (query === void 0) { query = ''; }
        return this.http.get("services/fusion/" + id + "/attributetypes?" + query)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionAttributeTypeTree = function (id, query) {
        var _this = this;
        if (query === void 0) { query = ''; }
        return this.getFusionAttributeTypes(id, query)
            .then(function (r) {
            return __WEBPACK_IMPORTED_MODULE_4__models_form_model__["a" /* FormHelper */].formTree(r);
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionConfiguration = function (fusionId) {
        var _this = this;
        return this.http.get("services/fusion/configurationById/" + fusionId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionConfigurationFromObjectId = function (fusionAttributeId) {
        var _this = this;
        return this.http.get("services/fusion/configurationByObjectId/" + fusionAttributeId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionConfigurations = function () {
        var _this = this;
        return this.http.get("services/fusion/configurations?$orderby=FusionType,Name")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionAgentHistory = function (maxRows, fusionId) {
        var _this = this;
        var url = "services/fusion/agenthistory?$top=" + (maxRows ? maxRows : '100') + "&$orderby=DateStarted%20desc";
        if (fusionId) {
            url += "&$filter=FusionID%20eq%20" + fusionId;
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionAgentErrorHistory = function (maxRows, days) {
        var _this = this;
        var url = "services/fusion/agenterrors?$top=" + (maxRows ? maxRows : '100') + "&$orderby=Date%20desc";
        if (days) {
            var d = new Date();
            d.setDate(d.getDate() - days);
            url += "&$filter=Date ge DateTime'" + d.toISOString() + "'";
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionProcessErrorHistory = function (maxRows, days) {
        var _this = this;
        var url = "services/fusion/executionerrors?$top=" + (maxRows ? maxRows : '100') + "&$orderby=Date%20desc";
        if (days) {
            var d = new Date();
            d.setDate(d.getDate() - days);
            url += "&$filter=Date ge DateTime'" + d.toISOString() + "'";
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionWorkerExecutionHistory = function (maxRows, fusionId) {
        var _this = this;
        var url = "services/fusion/executionhistory?$top=" + (maxRows ? maxRows : '100') + "&$orderby=DateStarted%20desc";
        if (fusionId) {
            url += "&$filter=FusionID%20eq%20" + fusionId;
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionPromotionHistory = function (maxRows) {
        var _this = this;
        return this.http.get("services/fusion/promotionhistory?$top=" + (maxRows ? maxRows : '100') + "&$orderby=DateStarted%20desc")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionStatsSummary = function (daysToLookBack) {
        var _this = this;
        return this.http.get("api/fusion/statistics?daysToLookBack=" + daysToLookBack)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.exportFusionConfigurations = function () {
        window.location.assign("services/fusion/configurations/excel.xls");
    };
    FusionService.prototype.getFusionConfigurationsByType = function (id) {
        var _this = this;
        return this.http.get("services/fusion/" + id + "/configurations")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionConfigurationGridDefinition = function (id) {
        var _this = this;
        return this.http.get("api/fusiontype/" + id + "/grid/definition")
            .toPromise()
            .then(function (response) { return response.json().Columns; })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionConfigurationFilters = function (fusionTypeID, fusionID) {
        var _this = this;
        return this.http.get("api/fusion/" + fusionTypeID + "/configurations/" + fusionID + "/filters")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionAttributeTypeList = function (fusionID) {
        var _this = this;
        return this.http.get("form/getfusionattributetypes?fusionID=" + fusionID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.postFusionConfigurationFilter = function (filter) {
        var _this = this;
        return this.http.post('form/fusionfilter', filter)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.putFusionConfigurationFilter = function (filter) {
        var _this = this;
        return this.http.put('form/fusionfilter', filter)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionQueryAttributeTypes = function (typeid, id, query) {
        var _this = this;
        if (query === void 0) { query = ''; }
        return this.http.get("services/fusion/" + typeid + "/configurations/" + id + "/queryattributetypes?" + query)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.postFusionType = function (fusionType, objectStyle) {
        var _this = this;
        if (objectStyle === void 0) { objectStyle = null; }
        return this.http.post('form/FusionType', { fusion: fusionType, style: objectStyle })
            .toPromise().
            then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.putFusionType = function (fusionType, objectStyle) {
        var _this = this;
        if (objectStyle === void 0) { objectStyle = null; }
        return this.http.put('form/FusionType', { fusion: fusionType, style: objectStyle })
            .toPromise().
            then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.postFusionAttributeType = function (fusionAttributeType) {
        var _this = this;
        return this.http.post('form/FusionAttributeType', fusionAttributeType)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.putFusionAttributeType = function (fusionAttributeType) {
        var _this = this;
        return this.http.put('form/FusionAttributeType', fusionAttributeType)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionTechnicalMappings = function () {
        var _this = this;
        return this.http.get('api/fusion/technicalmapping')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionFusionAttributeTypes = function (fusionId) {
        var _this = this;
        return this.http.get("services/fusion/" + fusionId + "/attributetypes?$orderby=Name")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionRules = function (fusionID) {
        var _this = this;
        return this.http.get("api/fusion/" + fusionID + "/rules")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionRuleSteps = function (ruleID) {
        var _this = this;
        return this.http.get("api/fusion/rules/" + ruleID + "/steps")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getRuleSteps = function (ruleID, ruleStepID) {
        var _this = this;
        return this.http.get("api/fusion/rule/" + ruleID + "/steps/" + ruleStepID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionRuleItems = function (id) {
        var _this = this;
        return this.http.get("api/fusion/" + id + "/FusionRuleItems")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionRuleStepMappings = function (id) {
        var _this = this;
        return this.http.get("api/fusion/" + id + "/FusionRuleStepMappings")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionExecutionErrors = function (executionId) {
        var _this = this;
        return this.http.get("services/fusion/executionerrors?$filter=ExecutionID%20eq%20" + executionId + "&$orderby=Date%20desc")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionExecutionResults = function (executionId) {
        var _this = this;
        return this.http.get("services/fusion/executions/" + executionId + "/results")
            .toPromise()
            .then(function (response) { return response.json().results; })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.downloadFusionManualLoadTemplate = function (fusionId, fusionTypeId, fusionAttributeTypeId) {
        window.location.assign("internal/fusion/" + fusionTypeId + "/configurations/" + fusionId + "/template/" + fusionAttributeTypeId);
    };
    FusionService.prototype.getEditFusionRule = function (id) {
        var _this = this;
        return this.http.get("form/GetEditFusionRule?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.postEditFusionRule = function (rule) {
        var _this = this;
        return this.http.post('form/PostEditFusionRule', rule)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.deleteFusionRuleById = function (id) {
        var _this = this;
        return this.http.delete("form/DeleteFusionRuleById?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getAddFusionRule = function (typeID) {
        var _this = this;
        return this.http.get("form/GetAddFusionRule?typeID=" + typeID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.postAddFusionRule = function (rule) {
        var _this = this;
        return this.http.post('form/PostAddFusionRule', rule)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getAddFusionRuleStep = function (ruleID) {
        var _this = this;
        return this.http.get("form/GetAddFusionRuleStep?ruleID=" + ruleID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.postAddFusionRuleStep = function (step) {
        var _this = this;
        return this.http.post('form/PostAddFusionRuleStep', step)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getEditFusionRuleStep = function (ruleID, ruleStepID) {
        var _this = this;
        return this.http.get("form/GetEditFusionRuleStep?ruleID=" + ruleID + "&ruleStepID=" + ruleStepID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.putEditFusionRuleStep = function (step) {
        var _this = this;
        return this.http.put('form/PutEditFusionRuleStep', step)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.deleteFusionRuleStep = function (ruleID, ruleStepID) {
        var _this = this;
        return this.http.delete("form/DeleteFusionRuleStepByID?ruleID=" + ruleID + "&ruleStepID=" + ruleStepID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getAddFusionRuleStepMapping = function (id) {
        var _this = this;
        return this.http.get("form/GetAddFusionRuleStepMapping?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.postAddFusionRuleStepMapping = function (map) {
        var _this = this;
        return this.http.post('form/PostAddFusionRuleStepMapping', map)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.deleteFusionRuleStepMapping = function (id) {
        var _this = this;
        return this.http.delete("form/DeleteFusionRuleStepMappingByID?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getEditFusionRuleStepMapping = function (id) {
        var _this = this;
        return this.http.get("form/GetEditFusionRuleStepMapping?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.putEditFusionRuleStepMapping = function (map) {
        var _this = this;
        return this.http.put('form/PutEditFusionRuleStepMapping', map)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getAddFusionRuleItem = function (id) {
        var _this = this;
        return this.http.get("form/GetAddFusionRuleItem?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.postAddFusionRuleItem = function (form) {
        var _this = this;
        return this.http.post('form/PostAddFusionRuleItem', form)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.deleteFusionRuleItem = function (id) {
        var _this = this;
        return this.http.delete("form/DeleteFusionRuleItemByID?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionPromotionItems = function (fusionID, fusionTypeID) {
        var _this = this;
        return this.http.get("api/fusion/" + fusionTypeID + "/configurations/" + fusionID + "/promotion/options")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getPromotionParents = function (parentTypeID, objectType) {
        var _this = this;
        return this.http.get("api/" + objectType + "/" + parentTypeID + "/fieldlookup")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getPromotionRuleSteps = function (ruleID, ruleStepID) {
        var _this = this;
        return this.http.get("api/fusion/rule/" + ruleID + "/steps/" + ruleStepID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getPromotionFusionOwnerRules = function (fusionID) {
        var _this = this;
        return this.http.get("api/fusion/rule/fusionOwners/" + fusionID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFindSourceFields = function (ruleObjectType, ruleObjectID) {
        var _this = this;
        return this.http.get("fields/" + ruleObjectType + "/" + ruleObjectID + ".json")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFindModels = function () {
        var _this = this;
        return this.http.get('api/catalogs')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFindArtifactTypes = function () {
        var _this = this;
        return this.http.get('api/artifacttypes?$orderby=Name')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFindFusionAttributeTypes = function () {
        var _this = this;
        return this.http.get('api/fusion/rule/fusionattributetypes')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFindAttributeTypes = function () {
        var _this = this;
        return this.http.get('services/fusion/attributetypes')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFindPromotions = function (fusionAttributeID) {
        var _this = this;
        return this.http.get("services/fusion/promotions/" + fusionAttributeID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFindReferenceItemTypes = function () {
        var _this = this;
        return this.http.get('api/referenceitemtypes?$orderby=Name')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getLineageRoles = function () {
        var _this = this;
        return this.http.get('/api/fusion/rule/lineage/roles')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getRelateIntersectTypes = function () {
        var _this = this;
        return this.http.get('/api/fusion/rule/relate/intersectTypes')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionConfigurationFromAttributeId = function (fusionAtttributeId) {
        var _this = this;
        return this.http.get("api/fusion/" + fusionAtttributeId + "/configurations/fromFusionAttribute")
            .toPromise()
            .then(function (response) { return response.json()[0]; })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getFusionRelationIntersectTypes = function () {
        var _this = this;
        return this.http.get('/api/fusion/rule/relate/intersectTypes')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService.prototype.getPromotionChildAttributeNodes = function (fusionID, targetFusionAttributeTypeID, ruleID, currentFusionAttributeTypeID, fusionAttributeID) {
        var _this = this;
        if (currentFusionAttributeTypeID === void 0) { currentFusionAttributeTypeID = 0; }
        if (fusionAttributeID === void 0) { fusionAttributeID = 0; }
        return this.http.get("api/fusion/promotion/ChildAttributeNodes?fusionID=" + fusionID + "&targetFusionAttributeTypeID=" + targetFusionAttributeTypeID + "&ruleID=" + ruleID + "&currentFusionAttributeTypeID=" + currentFusionAttributeTypeID + "&fusionAttributeID=" + fusionAttributeID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], FusionService);
    return FusionService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__base_service__["a" /* BaseService */]));


/***/ },

/***/ 488:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return SurveyTypeDisplayStyle; });
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return SurveyType; });
/* unused harmony export SurveyQuestionType */
/* unused harmony export SurveyQuestionOption */
/* harmony export (binding) */ __webpack_require__.d(exports, "d", function() { return SurveyQuestionTypeDetails; });
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SurveyResponse; });
var SurveyTypeDisplayStyle;
(function (SurveyTypeDisplayStyle) {
    SurveyTypeDisplayStyle[SurveyTypeDisplayStyle["RadioList"] = 1] = "RadioList";
    SurveyTypeDisplayStyle[SurveyTypeDisplayStyle["Rating"] = 2] = "Rating";
    SurveyTypeDisplayStyle[SurveyTypeDisplayStyle["CheckList"] = 3] = "CheckList";
})(SurveyTypeDisplayStyle || (SurveyTypeDisplayStyle = {}));
var SurveyType = (function () {
    function SurveyType() {
    }
    return SurveyType;
}());
var SurveyQuestionType = (function () {
    function SurveyQuestionType() {
    }
    return SurveyQuestionType;
}());
var SurveyQuestionOption = (function () {
    function SurveyQuestionOption() {
    }
    return SurveyQuestionOption;
}());
var SurveyQuestionTypeDetails = (function () {
    function SurveyQuestionTypeDetails() {
    }
    return SurveyQuestionTypeDetails;
}());
var SurveyResponse = (function () {
    function SurveyResponse() {
    }
    return SurveyResponse;
}());


/***/ },

/***/ 489:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return GroupService; });
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




var GroupService = (function (_super) {
    __extends(GroupService, _super);
    function GroupService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    GroupService.prototype.getGroupList = function () {
        var _this = this;
        return this.http.get('api/groups')
            .toPromise()
            .then(function (r) { return r.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.getGroupResourceList = function (id) {
        var _this = this;
        return this.http.get("api/groups/" + id + "/resources")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.getGroup = function (id) {
        var _this = this;
        return this.http.get("form/Group?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.putGroup = function (group) {
        var _this = this;
        return this.http.put('form/Group', group)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.postGroup = function (group) {
        var _this = this;
        return this.http.post('form/Group', group)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.deleteGroup = function (id) {
        var _this = this;
        return this.http.delete("form/DeleteGroupByID?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.postResourceGroup = function (resourceGroup) {
        var _this = this;
        return this.http.post('form/ResourceGroup', resourceGroup)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.deleteResourceGroup = function (groupID, resourceID) {
        var _this = this;
        return this.http.delete("form/ResourceGroup?groupID=" + groupID + "&resourceID=" + resourceID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.getGroupUserList = function (id) {
        var _this = this;
        return this.http.get("form/GetGroupUserList?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService.prototype.getResponsibilityBreakdownByGroup = function (id) {
        var _this = this;
        return this.http.get("tiles/ResponsibilityBreakdownByGroup?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GroupService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], GroupService);
    return GroupService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 490:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return StringConstants; });
var StringConstants = (function () {
    function StringConstants() {
    }
    //object types
    StringConstants.ObjectArtifact = "Artifact";
    StringConstants.ObjectArtifactType = "ArtifactType";
    StringConstants.ObjectRelationship = "Relationship";
    StringConstants.ObjectAttribute = "Attribute";
    StringConstants.ObjectGovernance = "Governance";
    StringConstants.ObjectRoot = "Root";
    StringConstants.ObjectTaxonomy = "Taxonomy";
    StringConstants.ObjectRule = "Rule";
    StringConstants.ObjectPolicy = "Policy";
    StringConstants.ObjectFusion = "Fusion";
    StringConstants.ObjectResource = "Resource";
    StringConstants.ObjectTaxonomyType = "TaxonomyType";
    StringConstants.ObjectPolicyType = "PolicyType";
    StringConstants.ObjectRuleType = "RuleType";
    StringConstants.ObjectGroup = "Group";
    //claim types
    StringConstants.ClaimRead = "Read";
    StringConstants.ClaimDelete = "Delete";
    StringConstants.ClaimCreate = "Create";
    StringConstants.ClaimUpdate = "Update";
    return StringConstants;
}());


/***/ },

/***/ 491:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return RelationshipsService; });
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




var RelationshipsService = (function (_super) {
    __extends(RelationshipsService, _super);
    function RelationshipsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    RelationshipsService.prototype.getRelations = function () {
        var _this = this;
        return this.http.get('relations/_intersectTypes')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getRelation = function (id) {
        var _this = this;
        return this.http.get("form/IntersectType_FormData?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getPossibleTechnicalRelations = function (id) {
        var _this = this;
        return this.http.get("relations/GetPossibleRelationshipsObjectByIntersect?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getObjectRelations = function (objectType, objectId) {
        var _this = this;
        return this.http.get("/api/" + objectType + "/" + objectId + "/relationshipTypes")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getRelatedObjects = function (objectType, objectId) {
        var _this = this;
        return this.http.get("/api/RelationshipObjectsByType?type=" + objectType + "&id=" + objectId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.deleteRelationship = function (id) {
        return this.deleteDynamicWithResult(this.http, 'intersecttype', id);
    };
    RelationshipsService.prototype.saveRelationship = function (relationship) {
        if (relationship.ID == undefined || !relationship.ID) {
            return this.postDynamic(this.http, 'intersecttype', relationship);
        }
        return this.putDynamic(this.http, 'intersecttype', relationship);
    };
    RelationshipsService.prototype.getRelationshipPredicates = function (subject, subjectId, object, objectId, predicateId) {
        var _this = this;
        var url = "form/IntersectType_PredicateOptions?subject=" + subject + "&subjectID=" + subjectId;
        if (object != undefined)
            url = url += "&object=" + object;
        if (objectId != undefined)
            url = url += "&objectID=" + objectId;
        if (predicateId != undefined)
            url = url += "&predicateID=" + predicateId;
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getSide1Options = function () {
        var _this = this;
        return this.http.get('form/IntersectType_Side1Options')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getSide2Options = function (id, type, selectedId, selectedType, predicateId) {
        var _this = this;
        var url = "form/IntersectType_Side2Options?id=" + id + "&type=" + type;
        if (selectedId != undefined)
            url = url += "&side2ID=" + selectedId;
        if (selectedType != undefined)
            url = url += "&side2Type=" + selectedType;
        if (predicateId != undefined)
            url = url += "&predicateID=" + predicateId;
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getRelationshipCounts = function (objectType, objectId) {
        var _this = this;
        return this.http.get("/api/" + objectType + "/" + objectId + "/relationships/counts")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getObjectRelationships = function (objectType, objectId, targetType, targetTypeId, intersectTypeID, criticalOnly) {
        var _this = this;
        criticalOnly = (criticalOnly == undefined ? false : criticalOnly);
        return this.http.get("/api/" + objectType + "/" + objectId + "/relationships/" + targetType + "/" + targetTypeId + "/" + intersectTypeID + "/" + criticalOnly)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getTechnicalRelationships = function (objectType, objectId) {
        var _this = this;
        return this.http.get("/api/" + objectType + "/" + objectId + "/relations")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.exportObjectRelationshipsToExcel = function (objectType, objectId, targetType, targetTypeId, intersectTypeID, criticalOnly) {
        criticalOnly = (criticalOnly == undefined ? false : criticalOnly);
        window.location.assign("/api/export/" + objectType + "/" + objectId + "/relationships/" + targetType + "/" + targetTypeId + "/" + intersectTypeID + "/excel.xls");
    };
    RelationshipsService.prototype.deleteRelationshipItem = function (id) {
        var _this = this;
        var url = "/api/relationships/" + id;
        return this.http
            .delete(url)
            .toPromise()
            .then(function (response) { return response; })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.deleteHierarchyItem = function (id) {
        var _this = this;
        return this.http.delete("relations/hierarchy/delete/" + id)
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getHierarchyArtifacts = function (model) {
        var _this = this;
        return this.http.post('relations/hierarchy/artifacts', model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.postHierarchy = function (model) {
        var _this = this;
        return this.http.post('relations/hierarchy/save', model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.getRelationshipRoles = function () {
        var _this = this;
        return this.http.get('relations/IntersectRoles')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RelationshipsService.prototype.deleteRelationshipRole = function (id) {
        return this.deleteDynamicWithResult(this.http, 'relationshiprole', id);
    };
    RelationshipsService.prototype.saveRelationshipRole = function (relationshipRole) {
        if (relationshipRole.ID == undefined || !relationshipRole.ID) {
            return this.postDynamic(this.http, 'relationshiprole', relationshipRole);
        }
        return this.putDynamic(this.http, 'relationshiprole', relationshipRole);
    };
    RelationshipsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], RelationshipsService);
    return RelationshipsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 492:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_form_model__ = __webpack_require__(144);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ResponsibilityService; });
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





var ResponsibilityService = (function (_super) {
    __extends(ResponsibilityService, _super);
    function ResponsibilityService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ResponsibilityService.prototype.getResponsibilityDetail = function (objectID, objectType, showHidden) {
        var _this = this;
        if (showHidden === void 0) { showHidden = true; }
        return this.http.get("api/" + objectType + "/" + objectID + "/ownership?showHidden=" + showHidden)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) {
            //TODO: use same model in api get as post instead of Responsibility vs ResponsibilityDetail???
            r.forEach(function (i) { return i.ID = i.ResponsibilityID; });
            return r;
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityService.prototype.getResponsibilityItemEditor = function (objectID, objectType, responsibilityID) {
        var _this = this;
        return this.http.get("form/Responsibility?responsibilityID=" + responsibilityID + "&id=" + objectID + "&type=" + objectType)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (model) {
            __WEBPACK_IMPORTED_MODULE_2__models_form_model__["a" /* FormHelper */].mapSelectItems(model.resources);
            __WEBPACK_IMPORTED_MODULE_2__models_form_model__["a" /* FormHelper */].mapSelectItems(model.responsibilityTypes);
            __WEBPACK_IMPORTED_MODULE_2__models_form_model__["a" /* FormHelper */].mapSelectItems(model.contexts);
            if (model.responsibility.ResponsibleObjectType)
                model.selectedResource = model.responsibility.ResponsibleObjectType + '|' + model.responsibility.ResponsibleObjectID;
            else if (model.resources && model.resources.length > 0)
                model.selectedResource = model.resources[0].value;
            if (model.responsibility.ResponsibilityTypeID)
                model.selectedResponsibilityType = model.responsibility.ResponsibilityTypeID.toString();
            else if (model.responsibilityTypes && model.responsibilityTypes.length > 0)
                model.selectedResponsibilityType = model.responsibilityTypes[0].value;
            model.selectedContexts = model.contexts.filter(function (c) { return c.Selected; }).map(function (c) { return c.value; });
            return model;
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityService.prototype.postResponsibility = function (responsibility) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        return this.http.post('form/responsibility', JSON.stringify(responsibility), { headers: headers })
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ResponsibilityService);
    return ResponsibilityService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_4__base_service__["a" /* BaseService */]));


/***/ },

/***/ 493:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return RightSidebarItem; });
var RightSidebarItem = (function () {
    function RightSidebarItem(title, tag, icons) {
        if (title)
            this.title = title;
        if (tag)
            this.tag = tag;
        this.active = false;
        this.icons = icons ? icons : ["fa-share-alt"];
    }
    return RightSidebarItem;
}());


/***/ },

/***/ 494:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "j", function() { return FieldDefinition; });
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return FieldTypeEditorModel; });
/* harmony export (binding) */ __webpack_require__.d(exports, "f", function() { return FilteredLookupItem; });
/* unused harmony export FilteredLookupDisplayField */
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return FieldType; });
/* unused harmony export Field */
/* harmony export (binding) */ __webpack_require__.d(exports, "i", function() { return FieldTypeFusionItemEditorModel; });
/* harmony export (binding) */ __webpack_require__.d(exports, "h", function() { return FieldTypeItemDisplayFieldEditorModel; });
/* harmony export (binding) */ __webpack_require__.d(exports, "d", function() { return FieldTypeRelationItemEditorModel; });
/* unused harmony export FieldTypeFusionLookupDefinition */
/* unused harmony export FieldTypeRelationLookupDefinition */
/* unused harmony export FieldTypeRelationLookupDisplayField */
/* harmony export (binding) */ __webpack_require__.d(exports, "e", function() { return FieldTypeFusionLookupDisplayField; });
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return Lookups; });
/* unused harmony export LookupItem */
/* harmony export (binding) */ __webpack_require__.d(exports, "g", function() { return ComplexLookupRelationType; });
var FieldDefinition = (function () {
    function FieldDefinition() {
    }
    return FieldDefinition;
}());
var FieldTypeEditorModel = (function () {
    function FieldTypeEditorModel() {
        this.FusionItems = new Array();
        this.RelationItems = [];
        this.LookupTokens = new Array();
        this.FilteredLookupItems = [];
    }
    return FieldTypeEditorModel;
}());
var FilteredLookupItem = (function () {
    function FilteredLookupItem() {
        this.DisplayFields = [];
    }
    return FilteredLookupItem;
}());
var FilteredLookupDisplayField = (function () {
    function FilteredLookupDisplayField() {
    }
    return FilteredLookupDisplayField;
}());
var FieldType = (function () {
    function FieldType() {
    }
    return FieldType;
}());
var Field = (function () {
    function Field() {
    }
    return Field;
}());
var FieldTypeFusionItemEditorModel = (function () {
    function FieldTypeFusionItemEditorModel() {
        this.DisplayFields = new Array();
        this.TargetFusionAttributeTypes = new Array();
        this.FusionDisplayFields = new Array();
    }
    return FieldTypeFusionItemEditorModel;
}());
var FieldTypeItemDisplayFieldEditorModel = (function () {
    function FieldTypeItemDisplayFieldEditorModel() {
    }
    return FieldTypeItemDisplayFieldEditorModel;
}());
var FieldTypeRelationItemEditorModel = (function () {
    function FieldTypeRelationItemEditorModel() {
        this.DisplayFields = [];
        this.SortOrderList = [];
        this.relationsLoading = false;
    }
    return FieldTypeRelationItemEditorModel;
}());
var FieldTypeFusionLookupDefinition = (function () {
    function FieldTypeFusionLookupDefinition() {
    }
    return FieldTypeFusionLookupDefinition;
}());
var FieldTypeRelationLookupDefinition = (function () {
    function FieldTypeRelationLookupDefinition() {
    }
    return FieldTypeRelationLookupDefinition;
}());
var FieldTypeRelationLookupDisplayField = (function () {
    function FieldTypeRelationLookupDisplayField() {
    }
    return FieldTypeRelationLookupDisplayField;
}());
var FieldTypeFusionLookupDisplayField = (function () {
    function FieldTypeFusionLookupDisplayField() {
    }
    return FieldTypeFusionLookupDisplayField;
}());
var Lookups = (function () {
    function Lookups() {
        this.ComplexLookupRelations = [];
        this.FilteredLookups = [];
        this.ReferenceTypes = new Array();
    }
    return Lookups;
}());
var LookupItem = (function () {
    function LookupItem() {
    }
    return LookupItem;
}());
var ComplexLookupRelationType;
(function (ComplexLookupRelationType) {
    ComplexLookupRelationType[ComplexLookupRelationType["StandardRelationhip"] = 1] = "StandardRelationhip";
    ComplexLookupRelationType[ComplexLookupRelationType["ChildRelationship"] = 2] = "ChildRelationship";
    ComplexLookupRelationType[ComplexLookupRelationType["ChildItem"] = 3] = "ChildItem";
    ComplexLookupRelationType[ComplexLookupRelationType["ParentItem"] = 4] = "ParentItem";
})(ComplexLookupRelationType || (ComplexLookupRelationType = {}));


/***/ },

/***/ 495:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_fields_model__ = __webpack_require__(494);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return FieldsService; });
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





var FieldsService = (function (_super) {
    __extends(FieldsService, _super);
    function FieldsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    FieldsService.prototype.getFields = function (objectID, objectType) {
        var _this = this;
        return this.http.get("/fields/" + objectType + "/" + objectID + "/full")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getFieldTypeEditor = function (id) {
        var _this = this;
        return this.http.get("form/FieldType?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getFusionLookupDisplayFields = function (id) {
        var _this = this;
        return this.http.get("form/FieldType_FusionLookup_DisplayFields?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) { return _this.ftItemToSelectItem(r); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getFusionLookupTargetAttributeTypes = function (sourceID, referenceTypeID) {
        var _this = this;
        return this.http.get("form/FieldType_FusionLookup_TargetAttributeTypes?s=" + sourceID + "&r=" + referenceTypeID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) { return _this.ftItemToSelectItem(r); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getRelationLookupDisplayFields = function (id, type, intersectTypeID) {
        var _this = this;
        return this.http.get("form/FieldType_RelationLookup_DisplayFields?id=" + id + "&type=" + type + "&intersectTypeID=" + intersectTypeID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) { return _this.ftItemToSelectItem(r); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getLookupTokens = function (id, type) {
        var _this = this;
        return this.http.get("form/FieldType_Lookup_Tokens?id=" + id + "&type=" + type)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) { return _this.ftItemToSelectItem(r); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getLookups = function (id, type) {
        var _this = this;
        return this.http.get("form/FieldType_Lookups?id=" + id + "&type=" + type + "&isNg=true")
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) {
            var l = new __WEBPACK_IMPORTED_MODULE_2__models_fields_model__["a" /* Lookups */]();
            l.DataTypes = _this.ftItemToSelectItem(r.DataTypes);
            l.FusionAttributeTypes = _this.ftItemToSelectItem(r.FusionAttributeTypes);
            var i = _this.ftItemToSelectItem(r.IntersectTypes);
            l.IntersectTypes = [];
            i.forEach(function (j) {
                l.IntersectTypes.push({ value: j.value, label: j.label, id: null });
            });
            l.Lookups = _this.ftItemToSelectItem(r.Lookups);
            l.Patterns = _this.ftItemToSelectItem(r.Patterns);
            l.ComplexLookupRelations = r.ComplexLookupRelations;
            l.FilteredLookups = r.FilteredLookups;
            return l;
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getFormData = function (id) {
        var _this = this;
        return this.http.get("form/FieldType_FormData?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getFusionDisplayFields = function (id) {
        var _this = this;
        return this.http.get("form/FieldType_FusionLookup_DisplayFields?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) { return _this.ftItemToSelectItem(r); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getFusionReferenceTypes = function () {
        return [
            { label: 'Self Reference', value: '1' },
            { label: 'Parent Reference', value: '2' },
            { label: 'Child Reference', value: '3' },
            { label: 'Relationship Reference', value: '4' },
        ];
    };
    FieldsService.prototype.getReferenceTypes = function () {
        return [
            { label: 'Self Reference', value: '1' },
            { label: 'Child Reference', value: '2' },
        ];
    };
    FieldsService.prototype.putFieldType = function (model) {
        var _this = this;
        return this.http.put('form/EditFieldType', model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.postFieldType = function (model) {
        var _this = this;
        return this.http.post('form/AddFieldType', model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.deleteFieldType = function (id) {
        var _this = this;
        return this.http.delete("form/DeleteFieldTypeByID?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.ftItemToSelectItem = function (items) {
        var s = new Array();
        items.forEach(function (i) {
            s.push({ label: i.title, value: i.value });
        });
        return s;
    };
    FieldsService.prototype.getRelationLookupChildIntersectTypes = function (id) {
        var _this = this;
        return this.http.get("form/FieldType_RelationLookup_ChildIntersectTypes?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getChildRelations = function (type, id) {
        var _this = this;
        return this.http.get("form/FieldType_ComplexLookup_ChildItems?type=" + type + "&id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getParentRelations = function (type, id) {
        var _this = this;
        return this.http.get("form/FieldType_ComplexLookup_ParentItems?type=" + type + "&id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getStandardRelations = function (type, id) {
        var _this = this;
        return this.http.get("form/FieldType_ComplexLookup_IntersectTypes?type=" + type + "&id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.moveUp = function (type, id, fieldId) {
        var _this = this;
        return this.http.post("fields/" + type + "/" + id + "/" + fieldId + "/move/up", null)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.moveDown = function (type, id, fieldId) {
        var _this = this;
        return this.http.post("fields/" + type + "/" + id + "/" + fieldId + "/move/dpwn", null)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService.prototype.getFilteredLookupDisplayFields = function (type, id, listType, listID) {
        var _this = this;
        return this.http.get("form/FieldType_FilteredLookup_DisplayFields?type=" + type + "&id=" + id + "&listType=" + listType + "&listID=" + listID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FieldsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], FieldsService);
    return FieldsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_4__base_service__["a" /* BaseService */]));
var FtItem = (function () {
    function FtItem() {
    }
    return FtItem;
}());


/***/ },

/***/ 496:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "b", function() { return ResponsibilityType; });
/* harmony export (binding) */ __webpack_require__.d(exports, "d", function() { return ResponsibilityTypeRelation; });
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ResponsibilityTypeGroup; });
/* unused harmony export ResponsibilityTypeCount */
/* harmony export (binding) */ __webpack_require__.d(exports, "c", function() { return ResourceResponsibilityTypeCount; });
var ResponsibilityType = (function () {
    function ResponsibilityType() {
        this.ResponsibilityTypeRelations = [];
        this.AllocationsList = [];
    }
    return ResponsibilityType;
}());
var ResponsibilityTypeRelation = (function () {
    function ResponsibilityTypeRelation() {
    }
    return ResponsibilityTypeRelation;
}());
var ResponsibilityTypeGroup;
(function (ResponsibilityTypeGroup) {
    ResponsibilityTypeGroup[ResponsibilityTypeGroup["People"] = 1] = "People";
    ResponsibilityTypeGroup[ResponsibilityTypeGroup["Sourcing"] = 2] = "Sourcing";
})(ResponsibilityTypeGroup || (ResponsibilityTypeGroup = {}));
var ResponsibilityTypeCount = (function () {
    function ResponsibilityTypeCount() {
    }
    return ResponsibilityTypeCount;
}());
var ResourceResponsibilityTypeCount = (function () {
    function ResourceResponsibilityTypeCount() {
    }
    return ResourceResponsibilityTypeCount;
}());


/***/ },

/***/ 497:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ClaimsService; });
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




var ClaimsService = (function (_super) {
    __extends(ClaimsService, _super);
    function ClaimsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ClaimsService.prototype.getClaims = function (objectID, objectType) {
        var _this = this;
        return this.http.get("/api/ownership/" + objectType + "/" + objectID + "/responsibilitytypes")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ClaimsService.prototype.getClaimsDisplayModel = function (objectID, objectType, responsibilityTypeID) {
        var _this = this;
        return this.http.get("parts/ClaimsMatrix?type=" + objectType + "&id=" + objectID + "&responsibilityTypeID=" + responsibilityTypeID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ClaimsService.prototype.putClaims = function (objectID, objectType, responsibilityTypeID, claims) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var model = {
            claims: claims,
            objectType: objectType,
            objectID: objectID,
            responsibilityTypeID: responsibilityTypeID
        };
        return this.http.put('form/EditClaimsMatrix', JSON.stringify(model), { headers: headers })
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    ClaimsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ClaimsService);
    return ClaimsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__base_service__["a" /* BaseService */]));


/***/ },

/***/ 498:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_authentication_service__ = __webpack_require__(256);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__ = __webpack_require__(85);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return AdminUserGuard; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var AdminUserGuard = (function () {
    function AdminUserGuard(authenticationService, router) {
        this.authenticationService = authenticationService;
        this.router = router;
        this._isAdmin = false;
    }
    AdminUserGuard.prototype.canActivate = function (route, state) {
        var _this = this;
        // wait for user to be authenticated then return if the user is an admin               
        this.authenticationService.admin().subscribe(function (res) {
            // Navigate to the home page
            if (!res) {
                _this.router.navigate([__WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_HOME_ROOT]);
            }
        }, function (err) { return console.log(err); }, function () {
            //  console.log('auth guard can activate complete')
        });
        return this.authenticationService.admin();
    };
    AdminUserGuard = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_authentication_service__["a" /* AuthenticationService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_authentication_service__["a" /* AuthenticationService */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _b) || Object])
    ], AdminUserGuard);
    return AdminUserGuard;
    var _a, _b;
}());


/***/ },

/***/ 499:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ArtifactTypeService; });
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




var ArtifactTypeService = (function (_super) {
    __extends(ArtifactTypeService, _super);
    function ArtifactTypeService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ArtifactTypeService.prototype.getArtifactTypeEditor = function (id, parentID) {
        var _this = this;
        return this.http.get("form/ArtifactType?parentID=" + parentID + "&id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactTypeService.prototype.getArtifactTypeDetails = function (id) {
        var _this = this;
        return this.http.get("api/artifacts/" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactTypeService.prototype.putArtifactType = function (model) {
        var _this = this;
        return this.http.put('form/ArtifactType', model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactTypeService.prototype.postArtifactType = function (model) {
        var _this = this;
        return this.http.post('form/ArtifactType', model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactTypeService.prototype.getArtifactTypeTree = function () {
        var _this = this;
        return this.http.get('internal/artifacts/types')
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) { return _this.formTree(r); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactTypeService.prototype.findArtifactType = function (tree, id) {
        for (var i = 0; i < tree.length; i++) {
            var n;
            if (tree[i].data.ID == id)
                return tree[i];
            if (tree[i].children && tree[i].children.length > 0) {
                n = this.findArtifactType(tree[i].children, id);
            }
            if (n)
                return n;
        }
        return null;
    };
    ArtifactTypeService.prototype.formTree = function (data) {
        var _this = this;
        var tree = new Array();
        data.filter(function (d) { return d.ParentID == null; }).forEach(function (d) {
            tree.push({ data: d, children: [], expanded: false });
        });
        tree.forEach(function (t) {
            _this.formTreeR(t, data);
        });
        return tree;
    };
    ArtifactTypeService.prototype.formTreeR = function (node, data) {
        var _this = this;
        data.filter(function (d) { return d.ParentID == node.data.ID; }).forEach(function (d) {
            var child = { data: d, children: [] };
            node.children.push(child);
            _this.formTreeR(child, data);
        });
    };
    ArtifactTypeService.prototype.getTopLevelSummary = function () {
        var _this = this;
        return this.http.get('internal/artifacts/typeswithstatistics')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactTypeService.prototype.getArtifactTypeStatus = function (artifactTypeId) {
        var _this = this;
        return this.http.get("/queries/" + artifactTypeId + "/StatusBreakdownByArtifactType")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactTypeService.prototype.getArtifactTypeUsedVsUnusedResponsibilities = function (artifactTypeId) {
        var _this = this;
        return this.http.get("queries/" + artifactTypeId + "/UsedVsUnusedResponsibilitiesByArtifactType")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactTypeService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ArtifactTypeService);
    return ArtifactTypeService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__base_service__["a" /* BaseService */]));


/***/ },

/***/ 500:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_responsibility_type_model__ = __webpack_require__(496);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ResponsibilityTypeService; });
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





var ResponsibilityTypeService = (function (_super) {
    __extends(ResponsibilityTypeService, _super);
    function ResponsibilityTypeService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ResponsibilityTypeService.prototype.getResponsibilityTypes = function () {
        var _this = this;
        return this.http.get('api/ownership/types')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityTypeService.prototype.getResponsibilityType = function (id, group) {
        var _this = this;
        if (group === void 0) { group = __WEBPACK_IMPORTED_MODULE_2__models_responsibility_type_model__["a" /* ResponsibilityTypeGroup */].People; }
        return this.http.get("form/ResponsibilityType?id=" + id + "&group=" + group)
            .toPromise()
            .then(function (r) { return r.json(); })
            .then(function (r) {
            var t = new __WEBPACK_IMPORTED_MODULE_2__models_responsibility_type_model__["b" /* ResponsibilityType */]();
            t = r.model;
            t.AllocationsList = r.allocations;
            t.ResponsibilityTypeRelations = r.selectedAllocations;
            if (t.ResponsibilityTypeRelations == null)
                t.ResponsibilityTypeRelations = [];
            return t;
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityTypeService.prototype.putResponsibilityType = function (responsibilityType) {
        var _this = this;
        return this.http.put("form/ResponsibilityType", responsibilityType)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityTypeService.prototype.postResponsibilityType = function (responsibilityType) {
        var _this = this;
        return this.http.post("form/ResponsibilityType", responsibilityType)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityTypeService.prototype.deleteResponsibilityType = function (id) {
        var _this = this;
        return this.http.delete("form/DeleteResponsibilityTypeByID?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityTypeService.prototype.getResponsibilityTypeBreakdown = function () {
        var _this = this;
        return this.http.get('queries/ResponsibilityTypeBreakdown')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityTypeService.prototype.getResourceResponsibilityByType = function (responsibilityTypeId) {
        var _this = this;
        return this.http.get("queries/" + responsibilityTypeId + "/ResourcesByResponsibilityType")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResponsibilityTypeService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ResponsibilityTypeService);
    return ResponsibilityTypeService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_4__base_service__["a" /* BaseService */]));


/***/ },

/***/ 501:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4_rxjs_add_operator_toPromise__ = __webpack_require__(278);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4_rxjs_add_operator_toPromise___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_4_rxjs_add_operator_toPromise__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TemplatesService; });
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





var TemplatesService = (function (_super) {
    __extends(TemplatesService, _super);
    function TemplatesService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
        this.templatesUrl = 'api/templates/tooltip';
    }
    TemplatesService.prototype.getTemplates = function () {
        var _this = this;
        return this.http.get(this.templatesUrl)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TemplatesService.prototype.getTemplate = function (id) {
        return this.getTemplates()
            .then(function (templates) { return templates.filter(function (template) { return template.ID === id; })[0]; });
    };
    TemplatesService.prototype.deleteTemplateById = function (id) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var url = "form/templates/tooltip/" + id;
        return this.http
            .delete(url, headers)
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TemplatesService.prototype.putTemplate = function (template) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var url = "form/EditTooltipTemplateRaw";
        return this.http
            .put(url, JSON.stringify(template), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TemplatesService.prototype.postTemplate = function (template) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var url = "form/AddTooltipTemplateRaw";
        return this.http
            .post(url, JSON.stringify(template), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TemplatesService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], TemplatesService);
    return TemplatesService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__base_service__["a" /* BaseService */]));


/***/ },

/***/ 502:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_form_model__ = __webpack_require__(144);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WorkflowService; });
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





var WorkflowService = (function (_super) {
    __extends(WorkflowService, _super);
    function WorkflowService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    WorkflowService.prototype.getWorkflows = function () {
        var _this = this;
        return this.http.get('/api/workflows/relations')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getWorkflow = function (id, workflowType) {
        var _this = this;
        return this.http.get("form/WorkflowAllocation?id=" + id + "&workflowType=" + workflowType)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.postWorkflow = function (workflow) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        return this.http.post('form/WorkflowAllocation', JSON.stringify(workflow), { headers: headers })
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.deleteWorkflow = function (id) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        return this.http.delete("/form/DeleteWorkflowAllocationByID?id=" + id, headers)
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getResponsibilityTypeSelectList = function (id, type) {
        var _this = this;
        return this.http.get("/workflow/WorkflowResponsibilityTypeOptions?type=" + type + "&id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) {
            __WEBPACK_IMPORTED_MODULE_2__models_form_model__["a" /* FormHelper */].mapSelectItems(r);
            return r;
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getParentTypeSelectList = function (id, type, workflowType) {
        var _this = this;
        return this.http.get("/workflow/WorkflowParentTypeOptions?workflowType=" + workflowType + "&type=" + type + "&id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) {
            __WEBPACK_IMPORTED_MODULE_2__models_form_model__["a" /* FormHelper */].mapSelectItems(r);
            return r;
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getMyCounts = function (daysToLookBack, resourceId) {
        var _this = this;
        return this.http.get(("api/count/assignments/" + daysToLookBack) + (resourceId ? "?id=" + resourceId : ''))
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getSuggestedItems = function (objectID, objectType) {
        var _this = this;
        var url = 'services/workflow/tasks/types/1/';
        if (objectID > 0 && objectType != undefined) {
            url += objectID + "/" + objectType;
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getCertifyItems = function (objectID, objectType) {
        var _this = this;
        var url = 'services/workflow/tasks/types/2/';
        if (objectID > 0 && objectType != undefined) {
            url += objectID + "/" + objectType;
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.exportAllIssueDetails = function () {
        window.location.assign('services/workflow/all/issues/excel/excel.xls');
    };
    WorkflowService.prototype.getAllIssueDetails = function () {
        var _this = this;
        var url = 'services/workflow/all/issues?$orderby=DateStarted%20desc,Issue';
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getWorkflowDetails = function (workflowId) {
        var _this = this;
        var url = "services/workflow/tasks/" + workflowId;
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getIssues = function (objectID, objectType) {
        var _this = this;
        var url = 'services/workflow/tasks/types/3/';
        if (objectID > 0 && objectType != undefined) {
            url += objectID + "/" + objectType;
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.updateIssue = function (issue, action, comment, assignTo) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("/services/workflow/tasks/" + issue.WorkflowID, JSON.stringify({ WorkflowAction: action, AssignTo: assignTo, Comment: comment }), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.updateSuggestion = function (suggestion, approve, comments) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("/services/workflow/tasks/" + suggestion.WorkflowID, JSON.stringify({
            WorkflowAction: 'ApprovalFromOwner', Approved: approve, Notes: comments }), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.certifyArtifact = function (certify) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("/services/workflow/tasks/" + certify.WorkflowID, JSON.stringify({ WorkflowAction: 'CertificationFromOwner' }), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.raiseIssue = function (objectId, objectType, issue, type) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("/api/issue/raise/" + objectType + "/" + objectId + "/" + type, JSON.stringify(issue), { headers: headers })
            .toPromise()
            .then(function (res) { return res; })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getWorkflowStepBreakdownByArtifactType = function (artifactTypeId) {
        var _this = this;
        return this.http.get("workflow/WorkflowStepBreakdownByArtifactType?id=" + artifactTypeId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getWorkflowsByArtifactTypeAndStep = function (artifactTypeId, workflowTypeId, stepId) {
        var _this = this;
        return this.http.get("workflow/WorkflowsByArtifactTypeAndWorkflowTypeAndStep?id=" + artifactTypeId + "&type=" + workflowTypeId + "&step=" + stepId + "&isNg=true")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService.prototype.getWorkflowStatus = function (workflowId) {
        var _this = this;
        return this.http.get("services/workflow/" + workflowId + "/status")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    WorkflowService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], WorkflowService);
    return WorkflowService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_4__base_service__["a" /* BaseService */]));


/***/ },

/***/ 6:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__ = __webpack_require__(15);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_site_message_model__ = __webpack_require__(634);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return MessagesService; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var MessagesService = (function () {
    function MessagesService() {
        // Observable sources
        this.errorMessageSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.infoMessageSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        // Observable streams
        this.errorMessage$ = this.errorMessageSource.asObservable();
        this.infoMessage$ = this.infoMessageSource.asObservable();
    }
    // Service message commands
    MessagesService.prototype.showError = function (summary, detail) {
        this.errorMessageSource.next(new __WEBPACK_IMPORTED_MODULE_2__models_site_message_model__["a" /* SiteMessage */](summary, detail));
    };
    MessagesService.prototype.showInfoMessage = function (summary, detail) {
        this.infoMessageSource.next(new __WEBPACK_IMPORTED_MODULE_2__models_site_message_model__["a" /* SiteMessage */](summary, detail));
    };
    MessagesService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [])
    ], MessagesService);
    return MessagesService;
}());


/***/ },

/***/ 614:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_dynamic_type_builder__ = __webpack_require__(299);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return AppComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var AppComponent = (function () {
    function AppComponent(typeBuilder, componentFactoryResolver, messagesService) {
        var _this = this;
        this.typeBuilder = typeBuilder;
        this.componentFactoryResolver = componentFactoryResolver;
        this.messagesService = messagesService;
        this.msgs = [];
        this.subscription = messagesService.errorMessage$.subscribe(function (errorMsg) {
            _this.msgs.push({ severity: 'error', summary: errorMsg.summary, detail: errorMsg.detail });
        });
        this.subscription = messagesService.infoMessage$.subscribe(function (infoMsg) {
            _this.msgs.push({ severity: 'info', summary: infoMsg.summary, detail: infoMsg.detail });
        });
    }
    AppComponent.prototype.ngAfterViewInit = function () {
        this.initializeQtipTooltips(); // initialize qtips library for tooltips we use in the site it needs to be a global js function                           
    };
    AppComponent.prototype.ngOnDestroy = function () {
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
    };
    AppComponent.prototype.initializeQtipTooltips = function () {
        var me = this;
        $('body').on('mouseenter', '*[data-type]', function (event) {
            $(this).qtip({
                content: {
                    title: $(this).data('title'),
                    // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                    text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                    ajax: {
                        url: "/resources/" + $(this).data("type") + "/" + $(this).data("id") + "/templates/tooltip/" + $(this).data("context") + "?isNg=true",
                        once: false,
                        success: function (data) {
                            if (!data || !data.length) {
                                this.destroy();
                            }
                            else {
                                if (me.componentRef) {
                                    me.componentRef.destroy();
                                }
                                // wrap with a div with id we know
                                data = "<div id='qTipContentCnt' style='display:none'>" + data + "</div>";
                                // here we get Factory (just compiled or from cache)
                                me.typeBuilder
                                    .createComponentFactory(data)
                                    .then(function (factory) {
                                    // Target will instantiate and inject component (we'll keep reference to it)                                        
                                    me.componentRef = me
                                        .dynamicComponentTarget
                                        .createComponent(factory);
                                });
                                var qtipScope = this;
                                setTimeout(function () {
                                    qtipScope.set('content.text', $('#qTipContentCnt'));
                                }, 100);
                            }
                        }
                    }
                },
                position: {
                    at: 'bottom center',
                    my: 'top center',
                    viewport: $(window),
                    effect: false,
                },
                overwrite: false,
                show: {
                    event: event.type,
                    solo: false,
                    ready: true
                },
                hide: {
                    fixed: true,
                    delay: 250,
                },
                style: {
                    classes: 'qtip-light qtip-shadow'
                }
            });
        });
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["ViewChild"])('target', { read: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ViewContainerRef"] }), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["ViewContainerRef"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["ViewContainerRef"]) === 'function' && _a) || Object)
    ], AppComponent.prototype, "dynamicComponentTarget", void 0);
    AppComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-app',
            template: " <header>\n                    <d3s-header></d3s-header>\n                    <d3s-site-menu></d3s-site-menu>\n                </header>\n                <main>                                                                          \n                    <div class=\"row\">                         \n                        <div class=\"col s12\">            \n                            <div class=\"maincontent\">                                                                                                                                                                            \n                                <router-outlet></router-outlet>                                                \n                            </div>  \n                        </div>                                                \n                    </div>                    \n                    <d3s-right-sidebar></d3s-right-sidebar>                        \n                </main>\n                <p-growl [value]=\"msgs\"></p-growl>\n                <div #target></div>                \n              "
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_dynamic_type_builder__["a" /* DynamicTypeBuilder */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_dynamic_type_builder__["a" /* DynamicTypeBuilder */]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["ComponentFactoryResolver"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["ComponentFactoryResolver"]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_1__services_index__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__services_index__["a" /* MessagesService */]) === 'function' && _d) || Object])
    ], AppComponent);
    return AppComponent;
    var _a, _b, _c, _d;
}());


/***/ },

/***/ 615:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__ = __webpack_require__(85);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__selective_preloading_strategy__ = __webpack_require__(640);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return AppRoutingModule; });
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
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_PREFIX, redirectTo: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_HOME_ROOT, pathMatch: 'full' },
    // lazy loaded modules 
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_COMMUNITY_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(8).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1154)['CommunityModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_HELP_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(14).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1157)['HelpModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(1).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1151)['AdminModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_FUSION_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(2).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1155)['FusionModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_MONITOR_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(13).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1160)['MonitorModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_RULE_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(4).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1164)['RuleModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_GROUP_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(7).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1156)['GroupModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_POLICY_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(5).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1161)['PolicyModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_RESOURCE_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(6).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1163)['ResourceModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_MODEL_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(3).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1159)['ModelModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_REFERENCE_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(9).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1162)['ReferenceModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ARTIFACT_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(0).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1153)['ArtifactModule']); }).bind(null, __webpack_require__)); }); }, data: { preload: true } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_HOME_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(10).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1158)['HomeModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_SEARCH_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(12).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1152)['SearchModule']); }).bind(null, __webpack_require__)); }); } },
    { path: __WEBPACK_IMPORTED_MODULE_2__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_WORKFLOW_ROOT, loadChildren: function () { return new Promise(function (resolve) { __webpack_require__.e/* nsure */(11).catch(function(err) { __webpack_require__.oe(err); }).then((function (require) { resolve(__webpack_require__(1150)['WorkflowModule']); }).bind(null, __webpack_require__)); }); } },
];
var AppRoutingModule = (function () {
    function AppRoutingModule() {
    }
    AppRoutingModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            providers: [__WEBPACK_IMPORTED_MODULE_3__selective_preloading_strategy__["a" /* SelectivePreloadingStrategy */]],
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"].forRoot(routes, { preloadingStrategy: __WEBPACK_IMPORTED_MODULE_3__selective_preloading_strategy__["a" /* SelectivePreloadingStrategy */] })],
            exports: [__WEBPACK_IMPORTED_MODULE_1__angular_router__["RouterModule"]],
        }), 
        __metadata('design:paramtypes', [])
    ], AppRoutingModule);
    return AppRoutingModule;
}());


/***/ },

/***/ 616:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_header_actions_service__ = __webpack_require__(177);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__ = __webpack_require__(85);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_4_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderActionsComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};





var HeaderActionsComponent = (function () {
    function HeaderActionsComponent(headerActionsService, router) {
        this.headerActionsService = headerActionsService;
        this.router = router;
        this.resourceId = CurrentResourceID;
        this.isAdminUrl = false;
        this.uri = "";
        this.hasRaiseIssueButton = true;
    }
    HeaderActionsComponent.prototype.ngOnInit = function () {
        var _this = this;
        this.sub = this.router.events.subscribe(function (e) {
            if (e instanceof __WEBPACK_IMPORTED_MODULE_1__angular_router__["NavigationEnd"]) {
                _this.uri = __WEBPACK_IMPORTED_MODULE_4_lodash__["trimStart"](e.url, '/');
                _this.isAdminUrl = (_this.uri || '').toUpperCase().startsWith(__WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT.toUpperCase());
            }
            //dont show raise issue button on raise issue screen or any admin screens            
            _this.hasRaiseIssueButton = (!e.url.toLowerCase().endsWith('workflow/raiseissue') && (e.url.toLowerCase().indexOf('/admin/') == -1));
        });
    };
    HeaderActionsComponent.prototype.resourceUrl = function () {
        return __WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__["a" /* SiteUrlHelpers */].getObjectUrl('Resource', this.resourceId);
    };
    HeaderActionsComponent.prototype.ngOnDestroy = function () {
        this.sub.unsubscribe();
    };
    HeaderActionsComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-header-actions',
            template: "\n                <ul class=\"right hide-on-med-and-down\">\n                    <li *ngIf=\"hasRaiseIssueButton\"><d3s-raise-issue-button></d3s-raise-issue-button></li>\n                    <li *ngIf=\"headerActionsService.showFavorite && !isAdminUrl\" style=\"cursor: pointer\"><d3s-header-favorites [uri]=\"uri\"></d3s-header-favorites></li>\n                    <li *ngIf=\"headerActionsService.showFollow  && !isAdminUrl\" style=\"cursor: pointer\"><d3s-header-follow></d3s-header-follow></li>\n                    <li *ngIf=\"headerActionsService.showLegacy\"><a href=\"/legacy\" title=\"Go to legacy UI\"><i class=\"fa fa-moon-o\"></i></a></li>\n                    <li *ngIf=\"headerActionsService.showHelp\"><a routerLink=\"help\" class=\"help\" title=\"Get help!\"><i class=\"fa fa-question-circle\"></i></a></li>\n                    <li *ngIf=\"headerActionsService.showSearch\"><d3s-header-typeahead-search></d3s-header-typeahead-search></li>\n                    <li *ngIf=\"headerActionsService.showNotifications\"><a href=\"#\" title=\"Go to notification settings\"><i class=\"fa fa-bell-o\"></i></a></li>\n                    <li><a [routerLink]=\"resourceUrl()\" class=\"photo\" title=\"Go to your profile\"><img [src]=\"'/resources/image/' + resourceId + '?size=25'\" height=\"25\" width=\"25\" /></a></li>\n                </ul> \n                ",
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_header_actions_service__["a" /* HeaderActionsService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_header_actions_service__["a" /* HeaderActionsService */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _b) || Object])
    ], HeaderActionsComponent);
    return HeaderActionsComponent;
    var _a, _b;
}());


/***/ },

/***/ 617:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_breadcrumb_model__ = __webpack_require__(484);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__services_index__ = __webpack_require__(71);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderBreadcrumbItemComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var HeaderBreadcrumbItemComponent = (function () {
    function HeaderBreadcrumbItemComponent(renderer, modelsService, elementRef, router, typeaheadSearchService) {
        this.renderer = renderer;
        this.modelsService = modelsService;
        this.elementRef = elementRef;
        this.router = router;
        this.typeaheadSearchService = typeaheadSearchService;
        this.treeClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.treeItems = [];
    }
    HeaderBreadcrumbItemComponent.prototype.ngOnChanges = function (changes) {
        if (this.breadcrumb)
            this.treeItems = this.breadcrumb.treeItems;
    };
    HeaderBreadcrumbItemComponent.prototype.isChangableItem = function () {
        return (this.breadcrumb.objectType && this.breadcrumb.objectId && !this.isTreeItem());
    };
    HeaderBreadcrumbItemComponent.prototype.isTreeItem = function () {
        return this.breadcrumb.objectType == 'Taxonomy';
    };
    HeaderBreadcrumbItemComponent.prototype.in = function (panel, event) {
        if (this.isChangableItem()) {
            this.showSearch = true;
        }
        if (this.isTreeItem()) {
            panel.toggle(event);
        }
    };
    HeaderBreadcrumbItemComponent.prototype.search = function (event) {
        var _this = this;
        this.typeaheadSearchService.getObjectTypeItems(10, event.query, this.breadcrumb.objectType, this.breadcrumb.objectId).then(function (data) {
            _this.results = data;
        });
    };
    HeaderBreadcrumbItemComponent.prototype.selectItem = function () {
        this.router.navigateByUrl(this.result.Url);
    };
    HeaderBreadcrumbItemComponent.prototype.onClick = function (event) {
        if (this.showSearch && !this.elementRef.nativeElement.contains(event.target)) {
            this.showSearch = false;
        }
    };
    HeaderBreadcrumbItemComponent.prototype.nodeSelect = function (event, panel) {
        this.breadcrumb.text = event.node.label;
        this.treeClick.emit({ id: event.node.data.id });
        panel.hide();
    };
    HeaderBreadcrumbItemComponent.prototype.setTreeNodeStyles = function (node) {
        if (!node.data)
            return null;
        var styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };
        return styles;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__models_breadcrumb_model__["a" /* Breadcrumb */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__models_breadcrumb_model__["a" /* Breadcrumb */]) === 'function' && _a) || Object)
    ], HeaderBreadcrumbItemComponent.prototype, "breadcrumb", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], HeaderBreadcrumbItemComponent.prototype, "lastItem", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], HeaderBreadcrumbItemComponent.prototype, "treeClick", void 0);
    HeaderBreadcrumbItemComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-header-breadcrumb-item',
            providers: [__WEBPACK_IMPORTED_MODULE_3__services_index__["W" /* TypeaheadSearchService */], __WEBPACK_IMPORTED_MODULE_3__services_index__["O" /* ModelsService */]],
            host: {
                '(document:click)': 'onClick($event)',
            },
            template: " <a *ngIf=\"breadcrumb.hasLink()\" [routerLink]=\"[breadcrumb.link]\" class=\"breadcrumb\">{{ breadcrumb.text }}</a>\n                <div *ngIf=\"!breadcrumb.hasLink() && !showSearch\" (mouseover)=\"in(treePanel,$event)\" class=\"breadcrumb\" [ngClass]=\"{'breadcrumb-link':isChangableItem() || isTreeItem()}\">{{ breadcrumb.text }}</div>\n                <p-autoComplete size=\"40\"                                                      \n                            *ngIf=\"showSearch\" \n                            [inputStyle]=\"{'border':'2px solid #54a4da','border-radius':'4px'}\"\n                            styleClass=\"searchTypeahead\"             \n                            [minLength]=\"1\"                               \n                            [(ngModel)]=\"result\" \n                            [suggestions]=\"results\" \n                            (completeMethod)=\"search($event)\" \n                            field=\"Name\"  \n                            [placeholder]=\"breadcrumb.text\"\n                            (onSelect)=\"selectItem()\">                       \n                    </p-autoComplete>                    \n                <div *ngIf=\"!lastItem\" class=\"sep breadcrumb\">::</div>                \n                <p-overlayPanel #treePanel>  \n                        <input type=\"text\" pInputText [(ngModel)]=\"searchValue\" placeholder=\"Search\" style=\"width: 100%;\">                      \n                        <p-tree [value]=\"treeItems | treeSearch: searchValue\" selectionMode=\"single\" [(selection)]=\"breadcrumb.selectedTreeNode\" styleClass=\"breadcrumbTree\" [style]=\"{'max-height':'800px','overflow':'auto','line-height':'25px'}\" \n                            (onNodeSelect)=\"nodeSelect($event,treePanel)\">\n                            <template let-node pTemplate type=\"default\">\n                                <span [ngStyle]=\"setTreeNodeStyles(node)\">{{node.label}} <i *ngIf=\"node.data?.hasRelations\" class=\"fa fa-share-alt\" aria-hidden=\"true\" title=\"Item has relationships\" style=\"color:#999;\"></i></span>\n                            </template>\n                        </p-tree>\n                </p-overlayPanel>                \n              "
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["Renderer"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["Renderer"]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["O" /* ModelsService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["O" /* ModelsService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["ElementRef"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["ElementRef"]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _e) || Object, (typeof (_f = typeof __WEBPACK_IMPORTED_MODULE_3__services_index__["W" /* TypeaheadSearchService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_index__["W" /* TypeaheadSearchService */]) === 'function' && _f) || Object])
    ], HeaderBreadcrumbItemComponent);
    return HeaderBreadcrumbItemComponent;
    var _a, _b, _c, _d, _e, _f;
}());


/***/ },

/***/ 618:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__services_header_breadcrumb_service__ = __webpack_require__(145);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderBreadcrumbComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};


var HeaderBreadcrumbComponent = (function () {
    function HeaderBreadcrumbComponent(headerBreadcrumbService) {
        var _this = this;
        this.headerBreadcrumbService = headerBreadcrumbService;
        this.showLastOnly = false;
        this.breadcrumbs = [];
        this.subscriptionAdd = headerBreadcrumbService.breadcrumbs$.subscribe(function (breadcrumb) {
            _this.breadcrumbs.push(breadcrumb);
            _this.resizeControlsToFit(window.innerWidth, _this.breadcrumbUIElement);
        });
        this.subscriptionClear = headerBreadcrumbService.breadcrumbClear$.subscribe(function (breadcrumb) {
            _this.breadcrumbs.splice(0, _this.breadcrumbs.length);
        });
        this.subscriptionPop = headerBreadcrumbService.breadcrumbPopLastSource$.subscribe(function (breadcrumb) {
            _this.breadcrumbs.pop();
        });
    }
    HeaderBreadcrumbComponent.prototype.ngOnDestroy = function () {
        // prevent memory leak when component destroyed
        this.subscriptionPop.unsubscribe();
        this.subscriptionClear.unsubscribe();
        this.subscriptionAdd.unsubscribe();
    };
    HeaderBreadcrumbComponent.prototype.handleTreeClick = function (event) {
        this.headerBreadcrumbService.breadcrumbTreeClick(event.id);
    };
    HeaderBreadcrumbComponent.prototype.resizeControlsToFit = function (windowWidth, element) {
        if (windowWidth < 650) {
            this.showLastOnly = true;
            return;
        }
        var controlsWidth = (windowWidth > 991) ? 360 : 0; // only visible medium and up
        var logoWidth = 200;
        var breadcrumbWidth = element.offsetWidth;
        var combinedWidth = controlsWidth + logoWidth + breadcrumbWidth;
        //if the width of this + the logo + the controls is bigger than screen start hiding breadcrumbs
        if (combinedWidth > windowWidth) {
            this.showLastOnly = true;
        }
        else {
            //check how many breadcrumbs there are and what would happen if we showed the full version            
            var worseCaseWidth = this.maxLength() + logoWidth + controlsWidth;
            if (worseCaseWidth > windowWidth) {
                this.showLastOnly = true;
            }
            else {
                this.showLastOnly = false;
            }
        }
    };
    HeaderBreadcrumbComponent.prototype.onResize = function (event, element) {
        this.resizeControlsToFit(event.target.innerWidth, element);
    };
    HeaderBreadcrumbComponent.prototype.maxLength = function () {
        var max = 0;
        for (var _i = 0, _a = this.breadcrumbs; _i < _a.length; _i++) {
            var breadcrumb = _a[_i];
            max += breadcrumb.text.length * 8; // 8 is based on the font size.
        }
        return max;
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["ViewChild"])('bread'), 
        __metadata('design:type', Object)
    ], HeaderBreadcrumbComponent.prototype, "breadcrumbUIElement", void 0);
    HeaderBreadcrumbComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-header-breadcrumb',
            template: " <div #bread class=\"breadcrumbs\" (window:resize)=\"onResize($event,bread)\">\n                 <div *ngFor=\"let breadcrumb of breadcrumbs;let last=last\" [ngClass]=\"{'active':last,'inactive':!last}\">\n                    <d3s-header-breadcrumb-item *ngIf=\"(showLastOnly && last) || !showLastOnly\" [breadcrumb]=\"breadcrumb\" [lastItem]=\"last\" (treeClick)=\"handleTreeClick($event)\"></d3s-header-breadcrumb-item>                    \n                 </div>                \n                </div>                \n              "
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__services_header_breadcrumb_service__["a" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__services_header_breadcrumb_service__["a" /* HeaderBreadcrumbService */]) === 'function' && _a) || Object])
    ], HeaderBreadcrumbComponent);
    return HeaderBreadcrumbComponent;
    var _a;
}());


/***/ },

/***/ 619:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_favorite_model__ = __webpack_require__(632);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__services_header_breadcrumb_service__ = __webpack_require__(145);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__services_header_actions_service__ = __webpack_require__(177);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__static_site_url_helpers__ = __webpack_require__(85);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderFavoritesComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};







var HeaderFavoritesComponent = (function () {
    function HeaderFavoritesComponent(router, messagesService, favoritesService, breadcrumbService, headerActionsService) {
        this.router = router;
        this.messagesService = messagesService;
        this.favoritesService = favoritesService;
        this.breadcrumbService = breadcrumbService;
        this.headerActionsService = headerActionsService;
        this.isFavoriteItem = false;
        this.isLoading = false;
    }
    HeaderFavoritesComponent.prototype.ngOnInit = function () {
        var _this = this;
        this.subObjectChange = this.breadcrumbService.currentObjectInfo$.subscribe(function (c) {
            _this.currentObject = c.type;
            _this.currentObjectId = c.id;
            if (_this.favItems == null) {
                _this.favoritesService.getFavorites()
                    .then(function (fav) {
                    _this.favItems = fav;
                    _this.checkIsFavorite();
                });
            }
            else {
                _this.checkIsFavorite();
            }
        });
        this.subBreadcrumb = this.breadcrumbService.breadcrumbs$.subscribe(function (b) {
            _this.name = b.text;
        });
        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(function () {
            _this.favoritesService.getFavorites().then(function (res) {
                _this.favItems = res;
                _this.checkIsFavorite();
            });
        });
    };
    HeaderFavoritesComponent.prototype.ngOnChanges = function (changes) {
        if (this.uri && changes["uri"]) {
            this.checkIsFavorite();
        }
    };
    HeaderFavoritesComponent.prototype.handleClick = function () {
        var _this = this;
        if (this.isLoading) {
            console.log('ERROR: CANNOT SAVE FAVORITE LOADING');
            return;
        }
        if (this.isAdminUri()) {
            console.log('ERROR: CANNOT SAVE FAVORITE FOR ADMIN PAGES');
            return;
        }
        this.isLoading = true;
        var f = new __WEBPACK_IMPORTED_MODULE_3__models_favorite_model__["a" /* Favorite */]();
        f.ObjectID = this.currentObjectId;
        f.Object = this.currentObject;
        f.Name = this.name;
        f.Route = this.uri ? this.uri : 'home'; //null route is home        
        this.isFavoriteItem = !this.isFavoriteItem;
        this.favoritesService.toggleFavorite(f)
            .then(function (fav) {
            _this.headerActionsService.emitFavoritesChange();
            _this.isLoading = false;
        });
    };
    HeaderFavoritesComponent.prototype.checkIsFavorite = function () {
        var _this = this;
        if (this.favItems == null)
            return;
        this.isFavoriteItem = false;
        if (!this.uri)
            this.uri = 'home';
        var index = this.favItems.findIndex(function (x) { return x.Route == _this.uri; });
        this.isFavoriteItem = index >= 0;
    };
    HeaderFavoritesComponent.prototype.ngOnDestroy = function () {
        this.subObjectChange.unsubscribe();
        this.subFavorites.unsubscribe();
        this.subBreadcrumb.unsubscribe();
    };
    HeaderFavoritesComponent.prototype.isAdminUri = function () {
        return (this.uri || '').toUpperCase().startsWith(__WEBPACK_IMPORTED_MODULE_6__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT.toUpperCase());
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], HeaderFavoritesComponent.prototype, "uri", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], HeaderFavoritesComponent.prototype, "isFavoriteItem", void 0);
    HeaderFavoritesComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-header-favorites',
            template: "\n        <span (click)=\"handleClick()\" class=\"favorite\" [ngClass]=\"{'active':isFavoriteItem}\" [title]=\"isFavoriteItem ? 'Remove from favorites' : 'Add to favorites'\" >\n            <i *ngIf=\"!isLoading\" class=\"fa fa-star\"></i><i *ngIf=\"isLoading\" style=\"color: #000;\" class=\"fa fa-spinner fa-spin\"></i>        \n        </span>\n    ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["X" /* FavoritesService */]]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["a" /* MessagesService */]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["X" /* FavoritesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["X" /* FavoritesService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_4__services_header_breadcrumb_service__["a" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_header_breadcrumb_service__["a" /* HeaderBreadcrumbService */]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_5__services_header_actions_service__["a" /* HeaderActionsService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_5__services_header_actions_service__["a" /* HeaderActionsService */]) === 'function' && _e) || Object])
    ], HeaderFavoritesComponent);
    return HeaderFavoritesComponent;
    var _a, _b, _c, _d, _e;
}());


/***/ },

/***/ 620:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__services_header_breadcrumb_service__ = __webpack_require__(145);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__services_header_actions_service__ = __webpack_require__(177);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderFollowComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};





var HeaderFollowComponent = (function () {
    function HeaderFollowComponent(router, route, followerService, breadcrumbService, headerActionsService) {
        this.router = router;
        this.route = route;
        this.followerService = followerService;
        this.breadcrumbService = breadcrumbService;
        this.headerActionsService = headerActionsService;
        this.active = false;
        this.onClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.visible = true;
        this.isFollowing = false;
        this.isFollowingParent = false;
        this.objectType = "";
        this.objectId = 0;
        this.parentObjectType = "";
        this.parentObjectId = 0;
        this.isLoading = false;
        this.tooltipString = 'Stop following';
    }
    HeaderFollowComponent.prototype.ngOnInit = function () {
        var _this = this;
        this.sub = this.breadcrumbService.currentObjectInfo$.subscribe(function (c) {
            _this.objectType = c.type;
            _this.objectId = c.id;
            //console.log(c);
            _this.checkActive();
        });
        //set values on initial load
        var o = this.breadcrumbService.currentObject;
        if (o != null) {
            this.objectType = o.type;
            this.objectId = o.id;
        }
        this.checkActive();
        //console.log(o);
    };
    HeaderFollowComponent.prototype.checkActive = function () {
        var _this = this;
        this.active = false;
        this.visible = true;
        if (this.objectType == null || this.objectType == "" || this.objectId < 0) {
            this.visible = false;
            return;
        }
        this.followerService.getFollowInfo(this.objectType, this.objectId)
            .then(function (f) {
            //console.log('getFollowInfo', f);
            _this.isFollowing = f.isFollowing;
            _this.isFollowingParent = f.isFollowingParent;
            if (f.parent) {
                _this.parentObjectType = f.parent.ObjectType;
                _this.parentObjectId = f.parent.ObjectID;
            }
            else {
                _this.parentObjectType = '';
                _this.parentObjectId = 0;
            }
            _this.updateTooltip();
        });
    };
    HeaderFollowComponent.prototype.toggleFollow = function () {
        var _this = this;
        //console.log('follow', this.isFollowingParent, this.objectType, this.objectId);
        if (this.isFollowingParent && (this.objectType != this.parentObjectType || this.objectId != this.parentObjectId))
            return;
        if (this.objectType == null || this.objectType == "" || this.objectId < 0) {
            return;
        }
        this.isLoading = true;
        var includeChildren = this.objectType.endsWith('Type');
        this.followerService.updateFollowStatus(this.objectType, this.objectId, includeChildren)
            .then(function (f) {
            //console.log(f);
            if (f.type == 'notification') {
                _this.active = !_this.active;
                _this.checkActive();
            }
            _this.isLoading = false;
        });
    };
    HeaderFollowComponent.prototype.ngOnDestroy = function () {
        this.sub.unsubscribe();
    };
    HeaderFollowComponent.prototype.updateTooltip = function () {
        if (this.isFollowing || this.isFollowingParent)
            this.active = true;
        if (!this.isFollowingParent && this.isFollowing)
            this.tooltipString = 'Stop following';
        else if (!this.isFollowingParent && !this.isFollowing)
            this.tooltipString = 'Follow this item';
        else if (this.isFollowingParent && this.objectType.endsWith('Type'))
            this.tooltipString = 'Stop following';
        else if (this.isFollowingParent && !this.objectType.endsWith('Type'))
            this.tooltipString = 'Following parent item';
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], HeaderFollowComponent.prototype, "uri", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], HeaderFollowComponent.prototype, "active", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], HeaderFollowComponent.prototype, "onClick", void 0);
    HeaderFollowComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-header-follow',
            styles: [
                "\n            .follow {\n                font-size: 1.2em;\n                color: #666;\n                padding: 0 15px;\n            }\n\n            .follow.active {\n                color: #0376c4;\n            }\n        "
            ],
            template: "\n        <span *ngIf=\"visible\" (click)=\"toggleFollow()\" [class.active]=\"active\" class=\"follow\" [title]=\"tooltipString\">\n            <i *ngIf=\"!isLoading\" class=\"fa fa-bookmark\"></i>\n            <i *ngIf=\"isLoading\" class=\"fa fa-spinner fa-spin\" style=\"color:black;\"></i>\n        </span>\n    ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["n" /* FollowerService */]]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["ActivatedRoute"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["ActivatedRoute"]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["n" /* FollowerService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["n" /* FollowerService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_3__services_header_breadcrumb_service__["a" /* HeaderBreadcrumbService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__services_header_breadcrumb_service__["a" /* HeaderBreadcrumbService */]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_4__services_header_actions_service__["a" /* HeaderActionsService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_4__services_header_actions_service__["a" /* HeaderActionsService */]) === 'function' && _e) || Object])
    ], HeaderFollowComponent);
    return HeaderFollowComponent;
    var _a, _b, _c, _d, _e;
}());


/***/ },

/***/ 621:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__ = __webpack_require__(85);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderTypeaheadSearchComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var HeaderTypeaheadSearchComponent = (function () {
    function HeaderTypeaheadSearchComponent(router, typeaheadSearchService) {
        this.router = router;
        this.typeaheadSearchService = typeaheadSearchService;
        this.active = false;
        this.hideHandle = 0;
    }
    HeaderTypeaheadSearchComponent.prototype.search = function (event) {
        var _this = this;
        this.searchText = event.query;
        this.typeaheadSearchService.getResults(10, event.query).then(function (data) {
            _this.results = data;
        });
    };
    HeaderTypeaheadSearchComponent.prototype.show = function (item) {
        // check for any pending hides and cancel them
        if (this.hideHandle > 0) {
            window.clearTimeout(this.hideHandle);
            this.hideHandle = 0;
        }
        var panel = item.children[0].nextElementSibling;
        if (panel) {
            this.active = true;
            panel.style.zIndex = 1000;
            panel.style.top = (item.offsetHeight - 1) + 'px'; // -1 for the border so it blends
            panel.style.right = '0px';
            //focus the input so user can just type
            // this needs to be done on timer so the elements are all visible and there.
            window.setTimeout(function () {
                var inputs = panel.getElementsByClassName("ui-autocomplete-input");
                if (inputs && inputs.length > 0) {
                    inputs[0].focus();
                }
            }, 300);
        }
    };
    HeaderTypeaheadSearchComponent.prototype.hide = function (item) {
        var _this = this;
        if (this.hideHandle > 0)
            return; //pending hide ignore new request
        //queue up a request to hide the window.
        this.hideHandle = window.setTimeout(function () {
            _this.active = false;
        }, 500);
    };
    HeaderTypeaheadSearchComponent.prototype.selectItem = function () {
        this.router.navigateByUrl(__WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__["a" /* SiteUrlHelpers */].convertClassicUrl(this.result.Url));
    };
    HeaderTypeaheadSearchComponent.prototype.checkKey = function (event) {
        if (event.keyCode == 13) {
            this.active = false;
            this.router.navigateByUrl(__WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_SEARCH_ROOT + "?query=" + encodeURIComponent(event.srcElement.value));
        }
    };
    HeaderTypeaheadSearchComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-header-typeahead-search',
            template: " <span #item style=\"display:table;\" class=\"header-search\" [ngClass]=\"{'header-search-active':active}\" (mouseenter)=\"show(item)\" (mouseleave)=\"hide(item)\" (keyup)=\"checkKey($event)\" >\n                    <a><i class=\"fa fa-search\"></i></a>\n                    <div class=\"search-child header-search-panel\">\n                        <p-autoComplete size=\"50\" *ngIf=\"active\"\n                                styleClass=\"searchTypeahead\" \n                                scrollHeight=\"400px\"\n                                [(ngModel)]=\"result\" \n                                [suggestions]=\"results\" \n                                field=\"Name\"\n                                (completeMethod)=\"search($event)\"                              \n                                placeholder=\"Search Data3Sixty\"                                \n                                (onSelect)=\"selectItem()\">                       \n                            <template let-result>\n                                <div style=\"padding:5px 0;\">                                \n                                    <div class=\"tt-suggestion tt-selectable\"><span style=\"color:#999;\">{{result.Type}}:</span> {{result.Name}}</div>\n                                </div>                            \n                            </template>\n                        </p-autoComplete>\n                    </div>\n                <span>",
            providers: [__WEBPACK_IMPORTED_MODULE_1__services_index__["W" /* TypeaheadSearchService */]]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__angular_router__["Router"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__services_index__["W" /* TypeaheadSearchService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__services_index__["W" /* TypeaheadSearchService */]) === 'function' && _b) || Object])
    ], HeaderTypeaheadSearchComponent);
    return HeaderTypeaheadSearchComponent;
    var _a, _b;
}());


/***/ },

/***/ 622:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__shared_base_component__ = __webpack_require__(113);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderComponent; });
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



var HeaderComponent = (function (_super) {
    __extends(HeaderComponent, _super);
    function HeaderComponent(router, route) {
        _super.call(this);
        this.router = router;
        this.route = route;
    }
    HeaderComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-header',
            template: " <div class=\"navbar-fixed\">\n                <nav class=\"top\">  \n                    <span class=\"logo\" routerLink=\"\" style=\"cursor:pointer;\"></span>                                 \n                    <d3s-header-breadcrumb></d3s-header-breadcrumb>                                          \n                    <d3s-header-actions></d3s-header-actions>\n                </nav>\n                </div>\n              ",
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["ActivatedRoute"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["ActivatedRoute"]) === 'function' && _b) || Object])
    ], HeaderComponent);
    return HeaderComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__shared_base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 623:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_6_primeng_primeng__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__pipes_pipes_module__ = __webpack_require__(485);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__header_actions_component__ = __webpack_require__(616);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__header_breadcrumb_item_component__ = __webpack_require__(617);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_10__header_breadcrumb_component__ = __webpack_require__(618);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11__header_typeahead_search_component__ = __webpack_require__(621);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_12__header_favorites_component__ = __webpack_require__(619);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_13__header_follow_component__ = __webpack_require__(620);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_14__header_component__ = __webpack_require__(622);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_15__raise_issue_button_component__ = __webpack_require__(624);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return HeaderModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
















var HeaderModule = (function () {
    function HeaderModule() {
    }
    HeaderModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["e" /* HttpModule */],
                __WEBPACK_IMPORTED_MODULE_4__angular_router__["RouterModule"],
                //d3s
                __WEBPACK_IMPORTED_MODULE_7__pipes_pipes_module__["a" /* PipesModule */],
                //primeng        
                __WEBPACK_IMPORTED_MODULE_6_primeng_primeng__["AutoCompleteModule"],
                __WEBPACK_IMPORTED_MODULE_6_primeng_primeng__["SharedModule"],
                __WEBPACK_IMPORTED_MODULE_6_primeng_primeng__["TreeModule"],
                __WEBPACK_IMPORTED_MODULE_6_primeng_primeng__["OverlayPanelModule"],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_8__header_actions_component__["a" /* HeaderActionsComponent */],
                __WEBPACK_IMPORTED_MODULE_9__header_breadcrumb_item_component__["a" /* HeaderBreadcrumbItemComponent */],
                __WEBPACK_IMPORTED_MODULE_10__header_breadcrumb_component__["a" /* HeaderBreadcrumbComponent */],
                __WEBPACK_IMPORTED_MODULE_11__header_typeahead_search_component__["a" /* HeaderTypeaheadSearchComponent */],
                __WEBPACK_IMPORTED_MODULE_12__header_favorites_component__["a" /* HeaderFavoritesComponent */],
                __WEBPACK_IMPORTED_MODULE_13__header_follow_component__["a" /* HeaderFollowComponent */],
                __WEBPACK_IMPORTED_MODULE_14__header_component__["a" /* HeaderComponent */],
                __WEBPACK_IMPORTED_MODULE_15__raise_issue_button_component__["a" /* RaiseIssueButtonComponent */],
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_14__header_component__["a" /* HeaderComponent */]
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_3__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], HeaderModule);
    return HeaderModule;
}());


/***/ },

/***/ 624:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__ = __webpack_require__(85);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return RaiseIssueButtonComponent; });
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




var RaiseIssueButtonComponent = (function (_super) {
    __extends(RaiseIssueButtonComponent, _super);
    function RaiseIssueButtonComponent(router) {
        _super.call(this);
        this.router = router;
    }
    RaiseIssueButtonComponent.prototype.raiseIssue = function () {
        this.router.navigateByUrl(__WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_WORKFLOW_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_3__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_WORKFLOW_RAISE_ISSUE);
    };
    RaiseIssueButtonComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-raise-issue-button',
            template: "           \n        <button type=\"button\"  class=\"issue-button\" (click)=\"raiseIssue()\">Take Action</button>\n        ",
            styles: ["\n        :host{\n            float:right;\n        }\n    "]
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _a) || Object])
    ], RaiseIssueButtonComponent);
    return RaiseIssueButtonComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_2__base_component__["a" /* BaseComponent */]));


/***/ },

/***/ 625:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_site_menu_model__ = __webpack_require__(200);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SiteMenuCategoryComponent; });
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



var SiteMenuCategoryComponent = (function (_super) {
    __extends(SiteMenuCategoryComponent, _super);
    function SiteMenuCategoryComponent() {
        _super.call(this);
        this.showClearButton = false;
        this.clearClick = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    SiteMenuCategoryComponent.prototype.show = function (item) {
        var _this = this;
        if (this.menu && this.menu.NavigationItems) {
            var submenu_1 = item.children[0].nextElementSibling;
            if (submenu_1) {
                this.menu.isActiveItem = true;
                submenu_1.style.zIndex = ++__WEBPACK_IMPORTED_MODULE_2__models_site_menu_model__["a" /* SiteNav */].zindex;
                submenu_1.style.top = '0px';
                submenu_1.style.left = item.offsetWidth + 'px';
                window.setTimeout(function () {
                    _this.repositionMenuToFit(window.innerHeight, submenu_1);
                }, 150);
            }
        }
    };
    SiteMenuCategoryComponent.prototype.repositionMenuToFit = function (windowHeight, element) {
        var dims = element.getBoundingClientRect();
        if (dims) {
            var maxHeight = dims.top + dims.height;
            //case where menu is bigger than height of page
            if (dims.height > windowHeight) {
                element.style.height = windowHeight + 'px';
                element.style.overflow = 'auto';
                element.style.top = '-' + element.style.top + 'px';
            }
            else if (maxHeight > windowHeight) {
                var topOffset = windowHeight - maxHeight;
                element.style.top = topOffset + 'px';
            }
        }
    };
    SiteMenuCategoryComponent.prototype.hide = function (item) {
        if (this.menu)
            this.menu.isActiveItem = false;
    };
    SiteMenuCategoryComponent.prototype.getColumnClass = function (menu) {
        var len = menu.NavigationItems.length;
        switch (len) {
            case 1:
                return "col s12";
            case 2:
            case 3:
                return "col s6";
            case 4:
                return "col s6";
            default:
                return "col s6";
        }
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], SiteMenuCategoryComponent.prototype, "url", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], SiteMenuCategoryComponent.prototype, "title", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], SiteMenuCategoryComponent.prototype, "rootIconName", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__models_site_menu_model__["b" /* SiteMenu */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__models_site_menu_model__["b" /* SiteMenu */]) === 'function' && _a) || Object)
    ], SiteMenuCategoryComponent.prototype, "menu", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], SiteMenuCategoryComponent.prototype, "showClearButton", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SiteMenuCategoryComponent.prototype, "clearClick", void 0);
    SiteMenuCategoryComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-site-menu-category',
            template: " \n                    <li #item [ngClass]=\"{'menu-category':true,'menu-parent':menu && (menu.NavigationItems),'menu-active':menu?.isActiveItem}\" (mouseenter)=\"show(item)\" (mouseleave)=\"hide(item)\">\n                        <span *ngIf=\"menu && menu.NavigationItems && menu.NavigationItems.length > 0\"><i *ngIf=\"url\" [class]=\"'fa ' + rootIconName\" [routerLink]=\"url\"></i><i *ngIf=\"!url\" [class]=\"'fa ' + rootIconName\"></i></span>\n                        <span *ngIf=\"!menu || !menu.NavigationItems || menu.NavigationItems.length == 0\" [pTooltip]=\"title\"><i [class]=\"'fa ' + rootIconName\" [routerLink]=\"url\"></i></span>\n                        <div *ngIf=\"menu && menu.NavigationItems && menu.NavigationItems.length > 0\" class=\"menu-child megamenu-panel\">\n                            <div>\n                                <div class=\"megamenu-title truncate\">{{title}}<span class=\"megamenu-tools\" *ngIf=\"showClearButton\"><i (click)=\"clearClick.emit(true)\" class=\"fa fa-eraser\" [pTooltip]=\"'Clear ' + title + ' List'\"></i></span></div>\n                                <div class=\"row\">\n                                    <div [class]=\"getColumnClass(menu)\" *ngFor=\"let item of menu.NavigationItems\">\n                                        <ul class=\"menu-group\">                                        \n                                            <d3s-site-menu-mega-item [item]=\"item\" [level]=\"0\" [(active)]=\"menu.isActiveItem\"></d3s-site-menu-mega-item>\n                                        </ul>\n                                    </div>\n                                </div>\n                            </div>\n                        </div>\n                    </li>                    \n                ",
            changeDetection: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectionStrategy"].OnPush
        }), 
        __metadata('design:paramtypes', [])
    ], SiteMenuCategoryComponent);
    return SiteMenuCategoryComponent;
    var _a;
}(__WEBPACK_IMPORTED_MODULE_1__base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 626:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__ = __webpack_require__(200);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SiteMenuMegaItemComponent; });
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




var SiteMenuMegaItemComponent = (function (_super) {
    __extends(SiteMenuMegaItemComponent, _super);
    function SiteMenuMegaItemComponent(router) {
        _super.call(this);
        this.router = router;
        this.activeChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
    }
    SiteMenuMegaItemComponent.prototype.getMargin = function () {
        return (this.level * 10) + 'px';
    };
    SiteMenuMegaItemComponent.prototype.itemClick = function () {
        if (this.item.IsLink)
            window.location.href = this.item.Url;
        else
            this.router.navigateByUrl(this.item.Url);
        this.active = false;
        this.activeChange.emit(this.active);
    };
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', (typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__["c" /* SiteMenuItem */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__["c" /* SiteMenuItem */]) === 'function' && _a) || Object)
    ], SiteMenuMegaItemComponent.prototype, "item", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Number)
    ], SiteMenuMegaItemComponent.prototype, "level", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], SiteMenuMegaItemComponent.prototype, "active", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], SiteMenuMegaItemComponent.prototype, "activeChange", void 0);
    SiteMenuMegaItemComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-site-menu-mega-item',
            template: " \n                <a (click)=\"itemClick()\" class=\"menu-item truncate\" [ngStyle]=\"{'margin-left': getMargin()}\">\n                    <i [class]=\"'fa fa-circle menu-level-indicator-' + level\" aria-hidden=\"true\"></i>{{item.Name}}</a>                    \n                <d3s-site-menu-mega-item *ngFor=\"let sub of item.Items\" [item]=\"sub\" [level]=\"level + 1\" [active]=\"active\" (activeChange)=\"active=$event;activeChange.emit(active);\"></d3s-site-menu-mega-item>                \n                ",
            changeDetection: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectionStrategy"].OnPush
        }), 
        __metadata('design:paramtypes', [(typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_router__["Router"]) === 'function' && _b) || Object])
    ], SiteMenuMegaItemComponent);
    return SiteMenuMegaItemComponent;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 627:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__base_component__ = __webpack_require__(113);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__ = __webpack_require__(200);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__ = __webpack_require__(85);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_5_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SiteMenuComponent; });
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






var SiteMenuComponent = (function (_super) {
    __extends(SiteMenuComponent, _super);
    function SiteMenuComponent(messagesService, stateService, headerActionsService, authenticationService, siteMenuService, favoritesService) {
        _super.call(this);
        this.messagesService = messagesService;
        this.stateService = stateService;
        this.headerActionsService = headerActionsService;
        this.authenticationService = authenticationService;
        this.siteMenuService = siteMenuService;
        this.favoritesService = favoritesService;
        this.isAdmin = false;
        this.siteMenu = [];
    }
    SiteMenuComponent.prototype.ngOnInit = function () {
        var _this = this;
        this.loadMenu();
        this.loadFavorites();
        this.subSiteNav = this.stateService.siteMenuRequiresReload$.subscribe(function () {
            _this.loadMenu();
        });
        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(function () {
            _this.loadFavorites();
        });
    };
    SiteMenuComponent.prototype.ngOnDestroy = function () {
        this.subSiteNav.unsubscribe();
        this.subFavorites.unsubscribe();
    };
    SiteMenuComponent.prototype.loadFavorites = function () {
        var _this = this;
        this.favoritesService.getFavorites().then(function (favorites) {
            favorites = __WEBPACK_IMPORTED_MODULE_5_lodash__["sortBy"](favorites, 'SortOrder'); // sort the favorites
            _this.favorites = new __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__["b" /* SiteMenu */]();
            _this.favorites.NavigationItems = [];
            for (var _i = 0, favorites_1 = favorites; _i < favorites_1.length; _i++) {
                var favorite = favorites_1[_i];
                _this.favorites.NavigationItems.push({
                    Name: favorite.Name,
                    Url: favorite.Route,
                    IsLink: false,
                    Items: null
                });
            }
        });
    };
    SiteMenuComponent.prototype.loadMenu = function () {
        var _this = this;
        this.siteMenuService.getMenu()
            .then(function (result) {
            result.MenuItems = result.MenuItems.filter(function (x) { return (x.MenuID != '#Admin'); }); //remove admin menu it will get built later.
            // add properties we need to add to the burned in menus
            for (var _i = 0, _a = result.MenuItems; _i < _a.length; _i++) {
                var menu = _a[_i];
                switch (menu.MenuID) {
                    case '#Glossary':
                        menu.ngUrl = __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ARTIFACT_ROOT;
                        break;
                    case '#Models':
                        menu.ngUrl = __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_MODEL_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_MODEL_CLASSIFICATION;
                        break;
                    case '#Policy':
                        menu.ngUrl = __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_POLICY_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_POLICY_CLASSIFICATION;
                        break;
                    case '#Data Quality':
                        break;
                    case '#Monitor':
                        menu.NavigationItems = [];
                        menu.ngUrl = __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_MONITOR_ROOT;
                        break;
                    case '#Reference':
                        menu.NavigationItems = [];
                        menu.ngUrl = __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_REFERENCE_ROOT;
                        break;
                    case '#Fusion':
                        menu.NavigationItems = [];
                        menu.ngUrl = __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_FUSION_ROOT;
                        break;
                    case '#Community':
                        menu.NavigationItems = [];
                        menu.ngUrl = __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_COMMUNITY_ROOT;
                        break;
                    default:
                        //is it a custom menu?
                        if (menu.MenuID.startsWith('~')) {
                            if (!menu.Title)
                                menu.Title = menu.MenuID.replace('~', '');
                        }
                        break;
                }
                if (!menu.Icon)
                    menu.Icon = 'fa-folder';
            }
            _this.siteMenu = __WEBPACK_IMPORTED_MODULE_5_lodash__["sortBy"](result.MenuItems, 'SortOrder'); // sort the menu's by display order
            if (result.IsAdmin)
                _this.buildAdminMenu();
            // used to enable guard that allows access to administrative routes                
            _this.authenticationService.admin$.next(result.IsAdmin);
            _this.authenticationService.admin$.complete();
            _this.isAdmin = result.IsAdmin;
        });
    };
    SiteMenuComponent.prototype.clearFavorites = function () {
        var _this = this;
        this.favoritesService.deleteCurrentUsersFavorites().
            then(function (result) {
            _this.showMessageForResult(_this.messagesService, result);
            _this.loadFavorites(); // reload favorites because the user could still have global favorites.
        });
    };
    SiteMenuComponent.prototype.buildAdminMenu = function () {
        this.adminMenu = new __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__["b" /* SiteMenu */]();
        this.adminMenu.NavigationItems = [];
        var metaMenu = new __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__["c" /* SiteMenuItem */]();
        metaMenu.Name = "MetaModel";
        metaMenu.Items = [];
        metaMenu.Items.push({ Name: 'Artifacts', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ARTIFACTS, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Attributes', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ATTRIBUTES, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Lookups', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_LOOKUPS, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Models', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_MODELS, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Policies', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_POLICIES, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Relationship Types', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_RELATIONSHIPS, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Rules', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_RULES, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Surveys', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_SURVEYS, Items: null, IsLink: false });
        this.adminMenu.NavigationItems.push(metaMenu);
        var integrationMenu = new __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__["c" /* SiteMenuItem */]();
        integrationMenu.Name = "Integration";
        integrationMenu.Items = [];
        integrationMenu.Items.push({ Name: 'API', Url: '/swagger/ui/index', Items: null, IsLink: true });
        integrationMenu.Items.push({ Name: 'Bulk Loader', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_BULK_LOAD, Items: null, IsLink: false });
        integrationMenu.Items.push({ Name: 'Fusion', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_FUSION, Items: null, IsLink: false });
        this.adminMenu.NavigationItems.push(integrationMenu);
        var metricsMenu = new __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__["c" /* SiteMenuItem */]();
        metricsMenu.Name = "Metrics";
        metricsMenu.Items = [];
        metricsMenu.Items.push({ Name: 'Analytics', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ANALYTICS, Items: null, IsLink: false });
        metricsMenu.Items.push({ Name: 'Dashboard', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_DASHBOARDS, Items: null, IsLink: false });
        this.adminMenu.NavigationItems.push(metricsMenu);
        var securityMenu = new __WEBPACK_IMPORTED_MODULE_3__models_site_menu_model__["c" /* SiteMenuItem */]();
        securityMenu.Name = "Security";
        securityMenu.Items = [];
        securityMenu.Items.push({ Name: 'Groups', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_GROUPS, Items: null, IsLink: false });
        securityMenu.Items.push({ Name: 'Responsibilities', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_RESPONSIBILITIES, Items: null, IsLink: false });
        securityMenu.Items.push({ Name: 'Users', Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_RESOURCES, Items: null, IsLink: false });
        this.adminMenu.NavigationItems.push(securityMenu);
        this.adminMenu.NavigationItems.push({ Name: 'Settings', Items: null, Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_SETTINGS, IsLink: false });
        this.adminMenu.NavigationItems.push({ Name: 'Templates', Items: null, Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_TEMPLATES, IsLink: false });
        this.adminMenu.NavigationItems.push({ Name: 'Workflow', Items: null, Url: __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_ROOT + "/" + __WEBPACK_IMPORTED_MODULE_4__static_site_url_helpers__["a" /* SiteUrlHelpers */].SITE_URL_ADMIN_WORKFLOW, IsLink: false });
    };
    SiteMenuComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-site-menu',
            template: " \n                <ul class=\"left-side-nav\">\n                    <d3s-site-menu-category *ngIf=\"favorites\" [title]=\"'My Favorites'\" showClearButton=\"true\" (clearClick)=\"clearFavorites()\" [menu]=\"favorites\" rootIconName=\"fa-star\"></d3s-site-menu-category>\n                    <template ngFor let-menu [ngForOf]=\"siteMenu\">\n                        <d3s-site-menu-category [url]=\"menu.ngUrl\" [title]=\"menu.Title\" [rootIconName]=\"menu.Icon\" [menu]=\"menu\"></d3s-site-menu-category>\n                    </template>                  \n                    <d3s-site-menu-category *ngIf=\"isAdmin\" [title]=\"'Settings'\" rootIconName=\"fa-cog\" [menu]=\"adminMenu\"></d3s-site-menu-category>                    \n                </ul>\n                ",
            providers: [__WEBPACK_IMPORTED_MODULE_2__services_index__["H" /* SiteMenuService */], __WEBPACK_IMPORTED_MODULE_2__services_index__["X" /* FavoritesService */]],
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["a" /* MessagesService */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["y" /* StateService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["y" /* StateService */]) === 'function' && _b) || Object, (typeof (_c = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["o" /* HeaderActionsService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["o" /* HeaderActionsService */]) === 'function' && _c) || Object, (typeof (_d = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["Y" /* AuthenticationService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["Y" /* AuthenticationService */]) === 'function' && _d) || Object, (typeof (_e = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["H" /* SiteMenuService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["H" /* SiteMenuService */]) === 'function' && _e) || Object, (typeof (_f = typeof __WEBPACK_IMPORTED_MODULE_2__services_index__["X" /* FavoritesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__services_index__["X" /* FavoritesService */]) === 'function' && _f) || Object])
    ], SiteMenuComponent);
    return SiteMenuComponent;
    var _a, _b, _c, _d, _e, _f;
}(__WEBPACK_IMPORTED_MODULE_1__base_component__["a" /* BaseComponent */]));
;


/***/ },

/***/ 628:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__angular_router__ = __webpack_require__(17);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__site_menu_component__ = __webpack_require__(627);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__site_menu_mega_item_component__ = __webpack_require__(626);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__site_menu_category_component__ = __webpack_require__(625);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9_primeng_primeng__ = __webpack_require__(114);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9_primeng_primeng___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_9_primeng_primeng__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SiteMenuModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};










var SiteMenuModule = (function () {
    function SiteMenuModule() {
    }
    SiteMenuModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["e" /* HttpModule */],
                __WEBPACK_IMPORTED_MODULE_4__angular_router__["RouterModule"],
                //prime
                __WEBPACK_IMPORTED_MODULE_9_primeng_primeng__["TooltipModule"],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_6__site_menu_component__["a" /* SiteMenuComponent */],
                __WEBPACK_IMPORTED_MODULE_7__site_menu_mega_item_component__["a" /* SiteMenuMegaItemComponent */],
                __WEBPACK_IMPORTED_MODULE_8__site_menu_category_component__["a" /* SiteMenuCategoryComponent */],
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_6__site_menu_component__["a" /* SiteMenuComponent */],
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_3__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_5__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], SiteMenuModule);
    return SiteMenuModule;
}());


/***/ },

/***/ 629:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return RightSidebarItemComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var RightSidebarItemComponent = (function () {
    function RightSidebarItemComponent() {
        this.activeChange = new __WEBPACK_IMPORTED_MODULE_0__angular_core__["EventEmitter"]();
        this.activeIcons = ["fa-share-alt"];
    }
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Output"])(), 
        __metadata('design:type', Object)
    ], RightSidebarItemComponent.prototype, "activeChange", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Boolean)
    ], RightSidebarItemComponent.prototype, "active", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', String)
    ], RightSidebarItemComponent.prototype, "title", void 0);
    __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Input"])(), 
        __metadata('design:type', Array)
    ], RightSidebarItemComponent.prototype, "activeIcons", void 0);
    RightSidebarItemComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-right-sidebar-item',
            template: " <div class=\"right-side-item row center-align\" (click)=\"active=!active;activeChange.emit(active);\" [ngClass]=\"{'right-side-active':active}\" [title]=\"title\">                    \n                    <i *ngIf=\"active\" class=\"fa fa-times fa-lg\"></i>\n                    <template [ngIf]=\"!active\">\n                        <i *ngIf=\"activeIcons.length==1\" [class]=\"'fa fa-lg ' + activeIcons[0]\"></i>    \n                        <span *ngIf=\"activeIcons.length>1\" class=\"fa-stack fa-lg\">\n                            <i\u00A0[class]=\"'fa ' + activeIcons[0] + ' fa-stack-2x'\"></i>\n\u00A0\u00A0                          <i\u00A0[class]=\"'fa ' +  activeIcons[1] + ' fa-stack-1x'\"></i>\n                        </span>\n                    </template>                    \n                </div>\n              ",
            changeDetection: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectionStrategy"].OnPush
        }), 
        __metadata('design:paramtypes', [])
    ], RightSidebarItemComponent);
    return RightSidebarItemComponent;
}());
;


/***/ },

/***/ 630:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__services_index__ = __webpack_require__(71);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_2_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return RightSidebarComponent; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var RightSidebarComponent = (function () {
    function RightSidebarComponent(rightSidebarService, ref) {
        var _this = this;
        this.rightSidebarService = rightSidebarService;
        this.items = [];
        this.subscription = rightSidebarService.rightSidebar$.subscribe(function (item) {
            _this.items.push(item);
            _this.items = __WEBPACK_IMPORTED_MODULE_2_lodash__["sortBy"](_this.items, 'title');
            ref.markForCheck();
        });
        this.subscriptionClear = rightSidebarService.rightSidebarClear$.subscribe(function (item) {
            _this.items.splice(0, _this.items.length);
            ref.markForCheck();
        });
    }
    RightSidebarComponent.prototype.ngOnDestroy = function () {
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
        this.subscriptionClear.unsubscribe();
    };
    RightSidebarComponent.prototype.itemClicked = function (item) {
        if (item.active) {
            //look for any other already active items and fire click for them
            for (var _i = 0, _a = this.items; _i < _a.length; _i++) {
                var ritem = _a[_i];
                if (ritem.active && ritem.title != item.title) {
                    this.rightSidebarService.itemClicked(ritem);
                    ritem.active = false;
                }
            }
        }
        this.rightSidebarService.itemClicked(item);
    };
    RightSidebarComponent = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Component"])({
            selector: 'd3s-right-sidebar',
            template: " <div *ngIf=\"items && items.length > 0\" class=\"hide-on-small-only right-sidebar\">                \n                    <div *ngFor=\"let item of items\">\n                        <d3s-right-sidebar-item [active]=\"item.active\" (activeChange)=\"item.active=$event;itemClicked(item)\" [title]=\"item.title\" [activeIcons]=\"item.icons\"></d3s-right-sidebar-item>\n                    </div>\n                </div>\n              ",
            changeDetection: __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectionStrategy"].OnPush
        }), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__services_index__["i" /* RightSidebarService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__services_index__["i" /* RightSidebarService */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectorRef"] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_0__angular_core__["ChangeDetectorRef"]) === 'function' && _b) || Object])
    ], RightSidebarComponent);
    return RightSidebarComponent;
    var _a, _b;
}());
;


/***/ },

/***/ 631:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_common__ = __webpack_require__(3);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__angular_forms__ = __webpack_require__(20);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__authentication_connection_backend__ = __webpack_require__(143);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__right_sidebar_item_component__ = __webpack_require__(629);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__right_sidebar_component__ = __webpack_require__(630);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return RightsidebarModule; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};







var RightsidebarModule = (function () {
    function RightsidebarModule() {
    }
    RightsidebarModule = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["NgModule"])({
            imports: [__WEBPACK_IMPORTED_MODULE_1__angular_common__["CommonModule"],
                __WEBPACK_IMPORTED_MODULE_2__angular_forms__["FormsModule"],
                __WEBPACK_IMPORTED_MODULE_3__angular_http__["e" /* HttpModule */],
            ],
            declarations: [
                __WEBPACK_IMPORTED_MODULE_5__right_sidebar_item_component__["a" /* RightSidebarItemComponent */],
                __WEBPACK_IMPORTED_MODULE_6__right_sidebar_component__["a" /* RightSidebarComponent */]
            ],
            exports: [
                __WEBPACK_IMPORTED_MODULE_6__right_sidebar_component__["a" /* RightSidebarComponent */]
            ],
            providers: [
                { provide: __WEBPACK_IMPORTED_MODULE_3__angular_http__["d" /* XHRBackend */], useClass: __WEBPACK_IMPORTED_MODULE_4__authentication_connection_backend__["a" /* AuthenticationConnectionBackend */] },
            ]
        }), 
        __metadata('design:paramtypes', [])
    ], RightsidebarModule);
    return RightsidebarModule;
}());


/***/ },

/***/ 632:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return Favorite; });
var Favorite = (function () {
    function Favorite() {
        this.isOverride = false;
    }
    return Favorite;
}());


/***/ },

/***/ 633:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return Permission; });
var Permission = (function () {
    function Permission() {
    }
    Permission.hasPermission = function (permissions, object, claim) {
        var uObject = object.toUpperCase();
        var uClaim = claim.toUpperCase();
        var index = permissions.findIndex(function (i) { return i.Claim.toUpperCase() == uClaim && i.ClaimObject.toUpperCase() == uObject; });
        if (index >= 0 && index < permissions.length)
            return true;
        return false;
    };
    return Permission;
}());


/***/ },

/***/ 634:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SiteMessage; });
var SiteMessage = (function () {
    function SiteMessage(summary, detail) {
        this.summary = summary;
        this.detail = detail;
    }
    return SiteMessage;
}());


/***/ },

/***/ 635:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return DropdownItemToSelectItemPipe; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var DropdownItemToSelectItemPipe = (function () {
    function DropdownItemToSelectItemPipe() {
    }
    DropdownItemToSelectItemPipe.prototype.transform = function (items) {
        var selectlist = [];
        for (var _i = 0, items_1 = items; _i < items_1.length; _i++) {
            var item = items_1[_i];
            selectlist.push({ label: item.Text, value: item.Value });
        }
        return selectlist;
    };
    DropdownItemToSelectItemPipe = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Pipe"])({ name: 'dropdownItemToSelectItemPipe' }), 
        __metadata('design:paramtypes', [])
    ], DropdownItemToSelectItemPipe);
    return DropdownItemToSelectItemPipe;
}());


/***/ },

/***/ 636:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ModelTypePipe; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var ModelTypePipe = (function () {
    function ModelTypePipe() {
    }
    ModelTypePipe.prototype.transform = function (items, type) {
        if (!type || type.length == 0)
            return items;
        var search = type.toLowerCase();
        return items.filter(function (item) { return item.TaxonomyTypeClass && item.TaxonomyTypeClass.toLowerCase().includes(search); });
    };
    ModelTypePipe = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Pipe"])({ name: 'modelType' }), 
        __metadata('design:paramtypes', [])
    ], ModelTypePipe);
    return ModelTypePipe;
}());


/***/ },

/***/ 637:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ScoreDisplayPipe; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var ScoreDisplayPipe = (function () {
    function ScoreDisplayPipe() {
    }
    ScoreDisplayPipe.prototype.transform = function (score) {
        return (score == null) ? 'N/A' : (Math.round(score * 100).toString() + '%');
    };
    ScoreDisplayPipe = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Pipe"])({ name: 'scoreDisplay' }), 
        __metadata('design:paramtypes', [])
    ], ScoreDisplayPipe);
    return ScoreDisplayPipe;
}());


/***/ },

/***/ 638:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TechnicalNameToDisplayValuePipe; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};

var TechnicalNameToDisplayValuePipe = (function () {
    function TechnicalNameToDisplayValuePipe() {
    }
    TechnicalNameToDisplayValuePipe.prototype.transform = function (objectType) {
        if (!objectType)
            return;
        switch (objectType.toUpperCase()) {
            case "ARTIFACTTYPE":
                return "Business Term";
            case "POLICYTYPE":
                return "Policy";
            case "TAXONOMYTYPE":
                return "Model";
            case "FUSIONATTRIBUTETYPE":
                return "Fusion";
            case "RULETYPE":
                return "Rule";
        }
        return objectType;
    };
    TechnicalNameToDisplayValuePipe = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Pipe"])({ name: 'technicalNameToDisplayValue' }), 
        __metadata('design:paramtypes', [])
    ], TechnicalNameToDisplayValuePipe);
    return TechnicalNameToDisplayValuePipe;
}());


/***/ },

/***/ 639:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_lodash__ = __webpack_require__(84);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_lodash___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_lodash__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TreeSearchPipe; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};


var TreeSearchPipe = (function () {
    function TreeSearchPipe() {
    }
    TreeSearchPipe.prototype.transform = function (tree, searchTerm, field) {
        var newTree = [];
        if (!searchTerm || searchTerm.length == 0) {
            return tree;
        }
        var dupTree = __WEBPACK_IMPORTED_MODULE_1_lodash__["cloneDeep"](tree); // dup tree so we dont mess with original
        var search = searchTerm.toLowerCase();
        for (var _i = 0, dupTree_1 = dupTree; _i < dupTree_1.length; _i++) {
            var node = dupTree_1[_i];
            var nameField = field ? node.data[field] : node.label;
            if (((nameField || '').toLowerCase().indexOf(search) != -1 || this.findSelectedTreeNode(node.children, search, field))) {
                node = this.removeChildren(node, search, field);
                newTree.push(node);
            }
        }
        return newTree;
    };
    TreeSearchPipe.prototype.removeChildren = function (node, search, field) {
        if (!node.children)
            return node;
        for (var i = node.children.length - 1; i >= 0; i--) {
            var cNode = node.children[i];
            var nameField = field ? cNode.data[field] : cNode.label;
            if (!nameField)
                continue;
            if (nameField.toLowerCase().indexOf(search) == -1 && !this.findSelectedTreeNode(cNode.children, search, field)) {
                node.children.splice(i, 1);
            }
            else if (cNode.children) {
                cNode = this.removeChildren(cNode, search, field);
            }
        }
        return node;
    };
    TreeSearchPipe.prototype.findSelectedTreeNode = function (tree, search, field) {
        var nodes = [];
        if (!tree)
            return false;
        // add root nodes
        for (var _i = 0, tree_1 = tree; _i < tree_1.length; _i++) {
            var rNode = tree_1[_i];
            nodes.push(rNode);
        }
        //do a breadth first search for the given treenode
        if (!nodes || nodes.length == 0)
            return false;
        var node = nodes[0];
        while (node) {
            var nameField = field ? node.data[field] : node.label;
            if (nameField && nameField.toLowerCase().indexOf(search) != -1)
                return true;
            //push children
            if (node.children) {
                for (var _a = 0, _b = node.children; _a < _b.length; _a++) {
                    var cNode = _b[_a];
                    nodes.push(cNode);
                }
            }
            //remove this node
            nodes.splice(0, 1);
            if (!nodes || nodes.length == 0)
                return false;
            node = nodes[0];
        }
    };
    TreeSearchPipe = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Pipe"])({ name: 'treeSearch' }), 
        __metadata('design:paramtypes', [])
    ], TreeSearchPipe);
    return TreeSearchPipe;
}());


/***/ },

/***/ 640:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_observable_of__ = __webpack_require__(430);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0_rxjs_add_observable_of___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_0_rxjs_add_observable_of__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Observable__ = __webpack_require__(1);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Observable___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_rxjs_Observable__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SelectivePreloadingStrategy; });


var SelectivePreloadingStrategy = (function () {
    function SelectivePreloadingStrategy() {
    }
    SelectivePreloadingStrategy.prototype.preload = function (route, load) {
        if (route.data && route.data['preload']) {
            console.log('Preloaded: ' + route.path);
            return load();
        }
        else {
            return __WEBPACK_IMPORTED_MODULE_1_rxjs_Observable__["Observable"].of(null);
        }
    };
    return SelectivePreloadingStrategy;
}());


/***/ },

/***/ 641:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_enums_model__ = __webpack_require__(115);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__models_grid_definition_model__ = __webpack_require__(294);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ArtifactService; });
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






var ArtifactService = (function (_super) {
    __extends(ArtifactService, _super);
    function ArtifactService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ArtifactService.prototype.getArtifacts = function (artifactTypeId, pagesize, pagenum, sortfield, sortorder, filters, relationships, attributes, simpleFilter) {
        var _this = this;
        var sortOrderText = sortorder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].None ? "" : (sortorder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Descending ? "desc" : "asc");
        var uri = "internal/artifacts/ArtifactsByType?id=" + artifactTypeId + "&pagesize=" + pagesize + "&pagenum=" + pagenum + "&sortDataField=" + sortfield + "&sortOrder=" + sortOrderText;
        if (filters != undefined) {
            //regular fields
            var normalFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_5__models_grid_definition_model__["a" /* GridFilterFieldType */].Normal; });
            var count = 0;
            uri += '&filterscount=' + normalFilters.length;
            for (var _i = 0, normalFilters_1 = normalFilters; _i < normalFilters_1.length; _i++) {
                var filter = normalFilters_1[_i];
                uri += "&filterdatafield" + count + "=" + filter.field + "&filtercondition" + count + "=" + filter.condition + "&filtervalue" + count + "=" + filter.value;
                count++;
            }
            //related filter fields
            var rellFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_5__models_grid_definition_model__["a" /* GridFilterFieldType */].Relation; });
            count = 0;
            uri += '&relfilterscount=' + rellFilters.length;
            for (var _a = 0, rellFilters_1 = rellFilters; _a < rellFilters_1.length; _a++) {
                var filter = rellFilters_1[_a];
                uri += "&relfilterdatafield" + count + "=" + filter.field.replace("Field", "") + "&relfiltercondition" + count + "=" + filter.condition + "&relfiltervalue" + count + "=" + filter.value;
                count++;
            }
            //hiden filter fields
            var hidFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_5__models_grid_definition_model__["a" /* GridFilterFieldType */].Hidden; });
            count = 0;
            uri += '&hidfilterscount=' + hidFilters.length;
            for (var _b = 0, hidFilters_1 = hidFilters; _b < hidFilters_1.length; _b++) {
                var filter = hidFilters_1[_b];
                uri += "&hidfilterdatafield" + count + "=" + filter.field.replace("Field", "") + "&hidfiltercondition" + count + "=" + filter.condition + "&hidfiltervalue" + count + "=" + encodeURIComponent(filter.value);
                count++;
            }
        }
        if (attributes != undefined) {
            uri += "&AttributeSearchValue=" + attributes.attributeSearchValue + "&AttributeType=" + attributes.attributeType;
        }
        if (relationships != undefined) {
            uri += "&RelationshipIncludeType=" + relationships.includeType + "&RelationshipObjectType=" + relationships.relationshipType.TargetType.replace("Type", "") + "&RelationshipObjectIDs=" + relationships.objectIds.join(",");
        }
        if (simpleFilter != undefined) {
            uri += "&filter=" + encodeURIComponent(simpleFilter);
        }
        return this.http.get(uri)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactService.prototype.getArtifactByParentAndArtifactType = function (parentId, artifactTypeId, filter, pagesize, pagenum, sortfield, sortorder) {
        var _this = this;
        var sortOrderText = sortorder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].None ? "" : (sortorder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Descending ? "desc" : "asc");
        var uri = "internal/artifacts/artifactsbyparent?parentID=" + parentId + "&childArtifactTypeID=" + artifactTypeId + "&pagesize=" + pagesize + "&pagenum=" + pagenum + "&sortDataField=" + sortfield + "&sortOrder=" + sortOrderText + "&filter=" + (filter ? filter : '');
        return this.http.get(uri)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactService.prototype.getArtifactsXls = function (artifactType, sortfield, sortorder, filters, relationships, attributes, simpleFilter) {
        var sortOrderText = sortorder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].None ? "" : (sortorder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Descending ? "desc" : "asc");
        var uri = "internal/artifacts/download/excel/" + artifactType.ID + ".xls?&sortDataField=" + sortfield + "&sortOrder=" + sortOrderText;
        if (filters != undefined) {
            //regular fields
            var normalFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_5__models_grid_definition_model__["a" /* GridFilterFieldType */].Normal; });
            var count = 0;
            uri += '&filterscount=' + normalFilters.length;
            for (var _i = 0, normalFilters_2 = normalFilters; _i < normalFilters_2.length; _i++) {
                var filter = normalFilters_2[_i];
                uri += "&filterdatafield" + count + "=" + filter.field + "&filtercondition" + count + "=" + filter.condition + "&filtervalue" + count + "=" + filter.value;
                count++;
            }
            //related filter fields
            var rellFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_5__models_grid_definition_model__["a" /* GridFilterFieldType */].Relation; });
            count = 0;
            uri += '&relfilterscount=' + rellFilters.length;
            for (var _a = 0, rellFilters_2 = rellFilters; _a < rellFilters_2.length; _a++) {
                var filter = rellFilters_2[_a];
                uri += "&relfilterdatafield" + count + "=" + filter.field.replace("Field", "") + "&relfiltercondition" + count + "=" + filter.condition + "&relfiltervalue" + count + "=" + filter.value;
                count++;
            }
            //hiden filter fields
            var hidFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_5__models_grid_definition_model__["a" /* GridFilterFieldType */].Hidden; });
            count = 0;
            uri += '&hidfilterscount=' + hidFilters.length;
            for (var _b = 0, hidFilters_2 = hidFilters; _b < hidFilters_2.length; _b++) {
                var filter = hidFilters_2[_b];
                uri += "&hidfilterdatafield" + count + "=" + filter.field.replace("Field", "") + "&hidfiltercondition" + count + "=" + filter.condition + "&hidfiltervalue" + count + "=" + encodeURIComponent(filter.value);
                count++;
            }
        }
        if (attributes != undefined) {
            uri += "&AttributeSearchValue=" + attributes.attributeSearchValue + "&AttributeType=" + attributes.attributeType;
        }
        if (relationships != undefined) {
            uri += "&RelationshipIncludeType=" + relationships.includeType + "&RelationshipObjectType=" + relationships.relationshipType.TargetType.replace("Type", "") + "&RelationshipObjectIDs=" + relationships.objectIds.join(",");
        }
        if (simpleFilter != undefined) {
            uri += "&filter=" + encodeURIComponent(simpleFilter);
        }
        window.location.assign(uri);
    };
    ArtifactService.prototype.getArtifact = function (id) {
        var _this = this;
        return this.http.get("api/artifact/" + id + "?isNg=true")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactService.prototype.deleteArtifact = function (id) {
        return this.deleteDynamicWithResult(this.http, 'artifact', id);
    };
    ArtifactService.prototype.saveArtifact = function (artifact, hasSuggest) {
        if (artifact.ID == undefined || !artifact.ID) {
            return this.postDynamic(this.http, hasSuggest ? 'suggestartifact' : 'artifact', artifact);
        }
        return this.putDynamic(this.http, 'artifact', artifact);
    };
    ArtifactService.prototype.getActivityCount = function (daysToLookBack) {
        var _this = this;
        return this.http.get("api/count/activity/" + daysToLookBack)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactService.prototype.getActivityDetails = function (artifactTypeId, daysToLookBack) {
        var _this = this;
        return this.http.get("api/countitems/activity/" + artifactTypeId + "/" + daysToLookBack)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactService.prototype.requestCertification = function (objectId) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });
        this.addRequestVerificationHeaders(headers);
        return this.http
            .post('form/RequestCertification', "ID=" + objectId, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactService.prototype.getSimilarArtifactNames = function (typeID, query) {
        var _this = this;
        return this.http.get("form/Aritfact_SimilarItems?typeID=" + typeID + "&query=" + query)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ArtifactService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ArtifactService);
    return ArtifactService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 642:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return AttributeTypeService; });
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




var AttributeTypeService = (function (_super) {
    __extends(AttributeTypeService, _super);
    function AttributeTypeService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    AttributeTypeService.prototype.getAttributes = function () {
        var _this = this;
        return this.http.get('attributes/fulltypes')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AttributeTypeService.prototype.getAttributeCategoryTypes = function (parentID) {
        var _this = this;
        var url = "attributes/categories";
        if (parentID != undefined)
            url = "attributes/categories?parentID={parentID}";
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AttributeTypeService.prototype.getAttributeTypeAllocations = function (attributeTypeId) {
        var _this = this;
        return this.http.get("/api/AttributeType/" + attributeTypeId + "/allocations")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AttributeTypeService.prototype.deleteAttributeTypeAllocations = function (attributeTypeId, objectTypeId, objectType) {
        var _this = this;
        return this.http
            .delete("form/DeleteAttributeTypeRelationWithUri?AttributeTypeID=" + attributeTypeId + "&ObjectType=" + encodeURIComponent(objectType) + "&ObjectID=" + objectTypeId)
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AttributeTypeService.prototype.addAttributeTypeAllocations = function (objectTypeInfo, allowMultiple, attributeTypeId) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });
        this.addRequestVerificationHeaders(headers);
        return this.http
            .post('form/AddAttributeTypeRelation', "AllowMultipleEntries=" + allowMultiple + "&ObjectTypeInfo=" + objectTypeInfo + "&AttributeTypeID=" + attributeTypeId, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AttributeTypeService.prototype.editAttributeTypeAllocations = function (objectTypeInfo, allowMultiple, attributeTypeId) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });
        this.addRequestVerificationHeaders(headers);
        return this.http
            .put('form/EditAttributeTypeRelation', "AllowMultipleEntries=" + allowMultiple + "&ObjectTypeInfo=" + objectTypeInfo + "&AttributeTypeID=" + attributeTypeId, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AttributeTypeService.prototype.getAttributeTypesForObject = function (objectType, objectId) {
        var _this = this;
        return this.http.get("/api/" + objectType + "/" + objectId + "/attributetypefilters")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AttributeTypeService.prototype.getAttributeFilterValues = function (objectType, objectId, attributeId) {
        var _this = this;
        return this.http.get("/api/" + objectType + "/" + objectId + "/" + attributeId + "/attributefiltervalues")
            .toPromise()
            .then(function (response) { return response.json().map(function (item) { return item['Name']; }); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AttributeTypeService.prototype.deleteAttributeType = function (id) {
        return this.deleteDynamic(this.http, 'attributetype', id);
    };
    AttributeTypeService.prototype.saveAttributeType = function (attributeType) {
        if (attributeType.ID == undefined || !attributeType.ID) {
            return this.postDynamic(this.http, 'attributetype', attributeType);
        }
        return this.putDynamic(this.http, 'attributetype', attributeType);
    };
    AttributeTypeService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], AttributeTypeService);
    return AttributeTypeService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 643:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_enums_model__ = __webpack_require__(115);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return AuditService; });
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





var AuditService = (function (_super) {
    __extends(AuditService, _super);
    function AuditService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    AuditService.prototype.getAuditData = function (objectID, objectType, pageNum, pageSize, sortOrder, sortField, filters) {
        var _this = this;
        var sortCol = sortField != undefined ? sortField : "";
        var url = "overlays/" + objectType + "/" + objectID + "/auditcombined.json?pagenum=" + pageNum + "&pagesize=" + pageSize + "&sortdatafield=" + sortField + "&sortorder=" + (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].None ? "" : (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Ascending ? "asc" : "desc"));
        var indx = 0;
        if (filters != undefined) {
            url += "&filterscount=" + filters.length;
            for (var _i = 0, filters_1 = filters; _i < filters_1.length; _i++) {
                var filter = filters_1[_i];
                url += "&filtervalue" + indx + "=" + filter.value + "&filtercondition" + indx + "=" + filter.condition + "&filteroperator" + indx + "=1&filterdatafield" + indx + "=" + filter.field;
                indx++;
            }
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    AuditService.prototype.exportToExcel = function (objectID, objectType) {
        window.location.assign("overlays/" + objectType + "/" + objectID + "/download/excel/audit.xls");
    };
    AuditService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], AuditService);
    return AuditService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 644:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return DashboardService; });
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




var DashboardService = (function (_super) {
    __extends(DashboardService, _super);
    function DashboardService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    DashboardService.prototype.getDashboards = function (objectID, objectType) {
        var _this = this;
        if (!objectType || objectType == '')
            objectType = 'Home';
        if (!objectID || objectID == 0)
            objectID = 0;
        return this.http.get("reports/bycontext/" + objectType + "/" + objectID + "/powerbi")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DashboardService.prototype.getPowerBIReportTokens = function (reportId) {
        var _this = this;
        return this.http.get("reports/powerbi/tokens/" + reportId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DashboardService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], DashboardService);
    return DashboardService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 645:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return DiagramService; });
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




var DiagramService = (function (_super) {
    __extends(DiagramService, _super);
    function DiagramService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    DiagramService.prototype.getLineageDiagram = function (type, id, viewID) {
        var _this = this;
        return this.http.get("diagrams/" + type + "/" + id + "/lineage/" + viewID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getLineageSourceRules = function (source, sourceId, target, targetId) {
        var _this = this;
        return this.http.get("api/" + source + "/" + sourceId + "/sources/" + target + "/" + targetId + "/rules")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getLineageSourceRulesFocal = function (focal, focalId, source, sourceId, target, targetId) {
        var _this = this;
        return this.http.get("api/" + focal + "/" + focalId + "/" + source + "/" + sourceId + "/" + target + "/" + targetId + "/rules")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getLineageObjectDetail = function (type, id) {
        var _this = this;
        return this.http.get("resources/" + type + "/" + id + "/templates/tooltip/preview?isNg=true")
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getLineageTechnicalRelationships = function (source, sourceId, target, targetId) {
        var _this = this;
        return this.http.get("relations/ChildRelationshipsBySourceAndTarget?s=" + source + "&sid=" + sourceId + "&t=" + target + "&tid=" + targetId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getLineageResponsibilities = function (type, id, showHidden) {
        var _this = this;
        if (showHidden === void 0) { showHidden = false; }
        return this.http.get("api/" + type + "/" + id + "/ownership?showHidden=" + showHidden)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getLineageMapItems = function (source, sourceId, target, targetId) {
        var _this = this;
        return this.http.get("api/maps/" + source + "/" + sourceId + "/" + target + "/" + targetId + "/mapItems")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getLineageMapSequence = function (object, objectId) {
        var _this = this;
        return this.http.get("form/mapsequence/" + object + "/" + objectId + "/mapitems")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.postLineageMapSequence = function (object, objectId, model) {
        var _this = this;
        return this.http.post("form/mapsequence/" + object + "/" + objectId + "/mapitems", model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getImpactDiagram = function (object, objectId) {
        var _this = this;
        return this.http.get("diagrams/ImpactAnalysis?type=" + object + "&id=" + objectId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getCatalogDiagram = function (id) {
        var _this = this;
        return this.http.get("diagrams/InformationCatalogDiagramData?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService.prototype.getRelations = function (object, objectId) {
        var _this = this;
        return this.http.get("api/" + object + "/" + objectId + "/relations")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    DiagramService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], DiagramService);
    return DiagramService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__base_service__["a" /* BaseService */]));


/***/ },

/***/ 646:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return EditorDefinitionService; });
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




var EditorDefinitionService = (function (_super) {
    __extends(EditorDefinitionService, _super);
    function EditorDefinitionService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    EditorDefinitionService.prototype.getEditorDefinition = function (ID, objectID, objectType, parentID, targetType, targetTypeID, createParams, editParams) {
        var _this = this;
        var uri = "";
        if (ID == undefined) {
            if (parentID)
                uri = "form/dynamiceditor/new/" + objectType + "/" + objectID + "/" + parentID;
            else if (targetType && targetTypeID)
                uri = "form/dynamiceditorrel/new/" + objectType + "/" + objectID + "/" + targetType + "/" + targetTypeID;
            else
                uri = "form/dynamiceditor/new/" + objectType + "/" + objectID;
        }
        else {
            uri = "form/dynamiceditor/edit/" + objectType + "/" + ID;
        }
        if (createParams && createParams.length > 0) {
            return this.http.post("form/dynamiceditor/new/" + objectType, createParams)
                .toPromise()
                .then(function (response) { return response.json(); })
                .catch(function (err) { return _this.handleError(err); });
        }
        else if (editParams && editParams.length > 0) {
            return this.http.post("form/dynamiceditor/edit/" + objectType, editParams)
                .toPromise()
                .then(function (response) { return response.json(); })
                .catch(function (err) { return _this.handleError(err); });
        }
        else {
            return this.http.get(uri)
                .toPromise()
                .then(function (response) { return response.json(); })
                .catch(function (err) { return _this.handleError(err); });
        }
    };
    EditorDefinitionService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], EditorDefinitionService);
    return EditorDefinitionService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 647:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return FavoritesService; });
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




var FavoritesService = (function (_super) {
    __extends(FavoritesService, _super);
    function FavoritesService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    FavoritesService.prototype.getFavorites = function (adminOnly) {
        var _this = this;
        if (adminOnly === void 0) { adminOnly = false; }
        return this.http.get("navigation/getfavorites?adminOnly=" + adminOnly)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FavoritesService.prototype.deleteCurrentUsersFavorites = function () {
        var _this = this;
        return this.http.delete('navigation/deletemyfavorites')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FavoritesService.prototype.toggleFavorite = function (favorite) {
        var _this = this;
        return this.http.put("navigation/togglefavorite", favorite)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FavoritesService.prototype.moveUp = function (route, admin) {
        var _this = this;
        if (admin === void 0) { admin = false; }
        var m = {
            route: route,
            moveUp: true
        };
        return this.http.put("navigation/movefavorite?admin=" + admin, m)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FavoritesService.prototype.moveDown = function (route, admin) {
        var _this = this;
        if (admin === void 0) { admin = false; }
        var m = {
            route: route,
            moveUp: false
        };
        return this.http.put("navigation/movefavorite?admin=" + admin, m)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FavoritesService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], FavoritesService);
    return FavoritesService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__base_service__["a" /* BaseService */]));


/***/ },

/***/ 648:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return FollowerService; });
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




var FollowerService = (function (_super) {
    __extends(FollowerService, _super);
    function FollowerService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    FollowerService.prototype.getFollowers = function (type, id) {
        var _this = this;
        return this.http.get("api/" + type + "/" + id + "/followers")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FollowerService.prototype.getFollowInfo = function (type, id) {
        var _this = this;
        return this.http.get("api/followinfo/" + type + "/" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FollowerService.prototype.updateFollowStatus = function (type, id, includeChildren) {
        var _this = this;
        if (includeChildren === void 0) { includeChildren = false; }
        return this.http.post('resources/UpdateFollowStatus', { type: type, id: id, includeChildren: includeChildren })
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FollowerService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], FollowerService);
    return FollowerService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 649:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_enums_model__ = __webpack_require__(115);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return FusionAttributeService; });
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





var FusionAttributeService = (function (_super) {
    __extends(FusionAttributeService, _super);
    function FusionAttributeService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    FusionAttributeService.prototype.getFusionAttributes = function (fusionId, fusionAttributeTypeId, pageNumber, pageSize, sortField, sortOrder, filters) {
        var _this = this;
        var sortOrderText = '';
        if (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Ascending)
            sortOrderText = 'asc';
        if (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Descending)
            sortOrderText = 'desc';
        var url = "internal/fusion/ItemsByAttributeType?fusionID=" + fusionId + "&fusionAttributeTypeID=" + fusionAttributeTypeId + "&pagenum=" + (pageNumber ? pageNumber : 0) + "&pagesize=" + (pageSize ? pageSize : 20) + "&sortDataField=" + (sortField ? sortField : '') + "&sortOrder=" + sortOrderText;
        if (filters && filters.length > 0) {
            url += "&filterscount=" + filters.length;
            var index = 0;
            for (var _i = 0, filters_1 = filters; _i < filters_1.length; _i++) {
                var filter = filters_1[_i];
                url += "&filterdatafield" + index + "=" + filter.dataField + "&filtercondition" + index + "=" + filter.condition + "&filtervalue" + index + "=" + filter.value;
                index++;
            }
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionAttributeService.prototype.getFusionQueryAttributes = function (fusionId, fusionQueryAttributeTypeId, pageNumber, pageSize, sortField, sortOrder, filters) {
        var _this = this;
        var sortOrderText = '';
        if (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Ascending)
            sortOrderText = 'asc';
        if (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Descending)
            sortOrderText = 'desc';
        var url = "internal/fusion/QueryItemsByAttributeType?fusionID=" + fusionId + "&fusionQueryAttributeTypeID=" + fusionQueryAttributeTypeId + "&pagenum=" + (pageNumber ? pageNumber : 0) + "&pagesize=" + (pageSize ? pageSize : 20) + "&sortDataField=" + (sortField ? sortField : '') + "&sortOrder=" + sortOrderText;
        if (filters && filters.length > 0) {
            url += "&filterscount=" + filters.length;
            var index = 0;
            for (var _i = 0, filters_2 = filters; _i < filters_2.length; _i++) {
                var filter = filters_2[_i];
                url += "&filterdatafield" + index + "=" + filter.dataField + "&filtercondition" + index + "=" + filter.condition + "&filtervalue" + index + "=" + filter.value;
                index++;
            }
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionAttributeService.prototype.getFusionAttributeExcel = function (fusionId, fusionAttributeTypeId, sortField, sortOrder, filters) {
        var sortOrderText = '';
        if (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Ascending)
            sortOrderText = 'asc';
        if (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Descending)
            sortOrderText = 'desc';
        var url = "internal/fusion/ExportItemsByAttributeType?fusionID=" + fusionId + "&fusionAttributeTypeID=" + fusionAttributeTypeId + "&sortDataField=" + (sortField ? sortField : '') + "&sortOrder=" + sortOrderText;
        if (filters && filters.length > 0) {
            url += "&filterscount=" + filters.length;
            var index = 0;
            for (var _i = 0, filters_3 = filters; _i < filters_3.length; _i++) {
                var filter = filters_3[_i];
                url += "&filterdatafield" + index + "=" + filter.dataField + "&filtercondition" + index + "=" + filter.condition + "&filtervalue" + index + "=" + filter.value;
                index++;
            }
        }
        window.location.assign(url);
    };
    FusionAttributeService.prototype.getFusionQueryAttributeExcel = function (fusionId, fusionQueryAttributeTypeId, sortField, sortOrder, filters) {
        var sortOrderText = '';
        if (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Ascending)
            sortOrderText = 'asc';
        if (sortOrder == __WEBPACK_IMPORTED_MODULE_4__models_enums_model__["a" /* SortOrder */].Descending)
            sortOrderText = 'desc';
        var url = "internal/fusion/ExportQueryItemsByAttributeType?fusionID=" + fusionId + "&fusionQueryAttributeTypeID=" + fusionQueryAttributeTypeId + "&sortDataField=" + (sortField ? sortField : '') + "&sortOrder=" + sortOrderText;
        if (filters && filters.length > 0) {
            url += "&filterscount=" + filters.length;
            var index = 0;
            for (var _i = 0, filters_4 = filters; _i < filters_4.length; _i++) {
                var filter = filters_4[_i];
                url += "&filterdatafield" + index + "=" + filter.dataField + "&filtercondition" + index + "=" + filter.condition + "&filtervalue" + index + "=" + filter.value;
                index++;
            }
        }
        window.location.assign(url);
    };
    FusionAttributeService.prototype.getFusionAttributeDetails = function (fusionAttributeId) {
        var _this = this;
        return this.http.get("internal/fusion/details/FusionAttribute/" + fusionAttributeId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    FusionAttributeService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], FusionAttributeService);
    return FusionAttributeService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_2__base_service__["a" /* BaseService */]));


/***/ },

/***/ 650:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return GridDefinitionService; });
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




var GridDefinitionService = (function (_super) {
    __extends(GridDefinitionService, _super);
    function GridDefinitionService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    GridDefinitionService.prototype.getGridDefinition = function (objectID, objectType, parentID, parentType) {
        var _this = this;
        var url = "api/" + objectType + "/" + objectID + "/grid/definition";
        if (parentID && parentType) {
            url += "?" + parentType + "=" + parentID;
        }
        return this.http.get(url)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    GridDefinitionService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], GridDefinitionService);
    return GridDefinitionService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 651:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return LevelsService; });
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




var LevelsService = (function (_super) {
    __extends(LevelsService, _super);
    function LevelsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    LevelsService.prototype.getObjectLevels = function (objectID, objectType) {
        var _this = this;
        return this.http.get("api/" + objectType + "/" + objectID + "/levels")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    LevelsService.prototype.saveObjectLevel = function (level, objectType, objectId, action) {
        level.ID = objectId;
        if (action == 'new') {
            return this.postDynamic(this.http, objectType + "level", level);
        }
        return this.putDynamic(this.http, objectType + "level", level);
    };
    LevelsService.prototype.deleteObjectLevel = function (objectType, objectId, levelId) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var url = "form/" + objectType + "/" + objectId + "/levels/" + levelId;
        return this.http
            .delete(url, headers)
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    LevelsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], LevelsService);
    return LevelsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 652:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return LookupService; });
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




var LookupService = (function (_super) {
    __extends(LookupService, _super);
    function LookupService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    LookupService.prototype.getLookups = function () {
        var _this = this;
        return this.http.get('resources/_Lookups')
            .toPromise()
            .then(function (response) { return response.json().results; })
            .catch(function (err) { return _this.handleError(err); });
    };
    LookupService.prototype.deleteLookup = function (lookupId) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var url = "form/LookupType/" + lookupId;
        return this.http
            .delete(url, headers)
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    LookupService.prototype.saveLookup = function (lookup) {
        if (lookup.ID == undefined || !lookup.ID) {
            return this.post(lookup);
        }
        return this.put(lookup);
    };
    LookupService.prototype.post = function (lookup) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("form/AddLookupTypeRaw", JSON.stringify(lookup), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    LookupService.prototype.put = function (lookup) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .put('form/EditLookupTypeRaw', JSON.stringify(lookup), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    LookupService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], LookupService);
    return LookupService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 653:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ModelsService; });
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




var ModelsService = (function (_super) {
    __extends(ModelsService, _super);
    function ModelsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ModelsService.prototype.getModels = function () {
        var _this = this;
        return this.http.get("api/catalogs")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ModelsService.prototype.getModel = function (id) {
        var _this = this;
        return this.http.get("api/catalogs/" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ModelsService.prototype.getModelHierarchy = function (id, details) {
        var _this = this;
        return this.http.get("internal/taxonomy/ModelHierarchy" + (details ? 'Detailed' : '') + "?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ModelsService.prototype.deleteModelHierarchy = function (id) {
        return this.deleteDynamicWithResult(this.http, 'taxonomy', id);
    };
    ModelsService.prototype.saveModelHierarchy = function (hierarchy) {
        if (hierarchy.ID == undefined || !hierarchy.ID) {
            return this.postDynamic(this.http, 'taxonomy', hierarchy);
        }
        return this.putDynamic(this.http, 'taxonomy', hierarchy);
    };
    ModelsService.prototype.getModelClassifications = function () {
        var _this = this;
        return this.http.get("api/TaxonomyTypeClasses?$orderby=Name")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ModelsService.prototype.deleteClassification = function (id) {
        return this.deleteDynamicWithResult(this.http, 'taxonomytypeclass', id);
    };
    ModelsService.prototype.saveClassification = function (classification) {
        if (classification.ID == undefined || !classification.ID) {
            return this.postDynamic(this.http, 'taxonomytypeclass', classification);
        }
        return this.putDynamic(this.http, 'taxonomytypeclass', classification);
    };
    ModelsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ModelsService);
    return ModelsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 654:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ObjectActionsService; });
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




var ObjectActionsService = (function (_super) {
    __extends(ObjectActionsService, _super);
    function ObjectActionsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ObjectActionsService.prototype.getObjectActions = function (objectID, objectType, context) {
        var _this = this;
        var currentContext = context == undefined ? "default" : context;
        return this.http.get("api/" + objectType + "/" + objectID + "/angularactions/" + currentContext)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectActionsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ObjectActionsService);
    return ObjectActionsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 655:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ObjectStatisticsService; });
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




var ObjectStatisticsService = (function (_super) {
    __extends(ObjectStatisticsService, _super);
    function ObjectStatisticsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ObjectStatisticsService.prototype.getObjectStatistics = function (objectID, objectType) {
        var _this = this;
        return this.http.get("api/" + objectType + "/" + objectID + "/object/statistics")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectStatisticsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ObjectStatisticsService);
    return ObjectStatisticsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 656:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ObjectStyleService; });
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




var ObjectStyleService = (function (_super) {
    __extends(ObjectStyleService, _super);
    function ObjectStyleService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ObjectStyleService.prototype.getObjectStyle = function (objectID, objectType) {
        var _this = this;
        return this.http.get("api/" + objectType + "/" + objectID + "/style")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ObjectStyleService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ObjectStyleService);
    return ObjectStyleService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 657:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return PermissionsService; });
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




var PermissionsService = (function (_super) {
    __extends(PermissionsService, _super);
    function PermissionsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    PermissionsService.prototype.getPermissions = function (objectID, objectType) {
        var _this = this;
        return this.http.get("api/" + objectType + "/" + objectID + "/permissions")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    PermissionsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], PermissionsService);
    return PermissionsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 658:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return PoliciesService; });
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




var PoliciesService = (function (_super) {
    __extends(PoliciesService, _super);
    function PoliciesService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    PoliciesService.prototype.getPolicyTypes = function () {
        var _this = this;
        return this.http.get('api/policytypes')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    PoliciesService.prototype.getPolicyTypesWithClassification = function () {
        var _this = this;
        return this.http.get('api/policytypesWithClassification')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    PoliciesService.prototype.getPolicies = function (policyTypeId) {
        var _this = this;
        return this.http.get("api/policytypes/" + policyTypeId + "/policies")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    PoliciesService.prototype.getPolicyType = function (id) {
        var _this = this;
        return this.http.get("api/policytypes/" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    PoliciesService.prototype.deletePolicy = function (id) {
        return this.deleteDynamicWithResult(this.http, 'policytype', id);
    };
    PoliciesService.prototype.savePolicyType = function (policyType) {
        if (policyType.ID == undefined || !policyType.ID) {
            return this.postDynamic(this.http, 'policytype', policyType);
        }
        return this.putDynamic(this.http, 'policytype', policyType);
    };
    PoliciesService.prototype.deletePolicyItem = function (id) {
        return this.deleteDynamicWithResult(this.http, 'policy', id);
    };
    PoliciesService.prototype.savePolicy = function (policy) {
        if (policy.ID == undefined || !policy.ID) {
            return this.postDynamic(this.http, 'policy', policy);
        }
        return this.putDynamic(this.http, 'policy', policy);
    };
    PoliciesService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], PoliciesService);
    return PoliciesService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 659:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return PredicatesService; });
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




var PredicatesService = (function (_super) {
    __extends(PredicatesService, _super);
    function PredicatesService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    PredicatesService.prototype.getPredicates = function () {
        var _this = this;
        return this.http.get("relations/predicates")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    PredicatesService.prototype.deletePredicate = function (id) {
        return this.deleteDynamicWithResult(this.http, 'predicate', id);
    };
    PredicatesService.prototype.savePredicate = function (predicate) {
        if (predicate.ID == undefined || !predicate.ID) {
            return this.postDynamic(this.http, 'predicate', predicate);
        }
        return this.putDynamic(this.http, 'predicate', predicate);
    };
    PredicatesService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], PredicatesService);
    return PredicatesService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 660:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ReferenceService; });
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




var ReferenceService = (function (_super) {
    __extends(ReferenceService, _super);
    function ReferenceService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ReferenceService.prototype.getReferenceItemTypes = function () {
        var _this = this;
        return this.http.get("api/referenceItemTypes")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ReferenceService.prototype.getReferenceItems = function (referenceItemTypeId) {
        var _this = this;
        return this.http.get("api/referenceItems/" + referenceItemTypeId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ReferenceService.prototype.saveReferenceItemType = function (item) {
        if (item.ID == undefined || !item.ID) {
            return this.postDynamic(this.http, 'referenceItemType', item);
        }
        return this.putDynamic(this.http, 'referenceItemType', item);
    };
    ReferenceService.prototype.deleteReferenceItemType = function (id) {
        return this.deleteDynamicWithResult(this.http, 'referenceItemType', id);
    };
    ReferenceService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ReferenceService);
    return ReferenceService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 661:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ReportsService; });
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




var ReportsService = (function (_super) {
    __extends(ReportsService, _super);
    function ReportsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ReportsService.prototype.getReports = function () {
        var _this = this;
        return this.http.get('reports')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ReportsService.prototype.deleteReport = function (id) {
        return this.deleteDynamicWithResult(this.http, 'report', id);
    };
    ReportsService.prototype.saveReport = function (report, file) {
        if (report.ID == undefined || !report.ID) {
            return this.postDynamic(this.http, 'report', report, file);
        }
        return this.putDynamic(this.http, 'report', report, file);
    };
    ReportsService.prototype.getReportTiles = function (report) {
        var _this = this;
        return this.http.get("reports/" + report.ID + "/tiles")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ReportsService.prototype.deleteReportTile = function (id) {
        return this.deleteDynamicWithResult(this.http, 'reporttile', id);
    };
    ReportsService.prototype.saveReportTile = function (reportTile, powerBIFile) {
        if (reportTile.ID == undefined || !reportTile.ID) {
            return this.postDynamic(this.http, 'reporttile', reportTile, powerBIFile);
        }
        return this.putDynamic(this.http, 'reporttile', reportTile, powerBIFile);
    };
    ReportsService.prototype.getReportLayout = function (report) {
        var _this = this;
        return this.http.get("reports/" + report.ID + "/layout")
            .toPromise()
            .then(function (response) { return response.json()[0]; })
            .catch(function (err) { return _this.handleError(err); });
    };
    ReportsService.prototype.getReportTargetTypes = function () {
        var _this = this;
        return this.http.get('api/reports/targets')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ReportsService.prototype.getReportLayouts = function () {
        var _this = this;
        return this.http.get('api/reports/layouts')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ReportsService.prototype.setPowerBICredentials = function (user, password) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });
        this.addRequestVerificationHeaders(headers);
        return this.http
            .post("form/AddPowerBICredentials", "Username=" + user + "&Password=" + password, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ReportsService.prototype.saveTile = function (tile) {
        if (tile.ID == undefined || !tile.ID) {
            return this.postDynamic(this.http, 'reporttile', tile);
        }
        return this.putDynamic(this.http, 'reporttile', tile);
    };
    ReportsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ReportsService);
    return ReportsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 662:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ResourcesService; });
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




var ResourcesService = (function (_super) {
    __extends(ResourcesService, _super);
    function ResourcesService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ResourcesService.prototype.getResources = function () {
        var _this = this;
        return this.http.get('/api/resources/1')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResourcesService.prototype.getResource = function (id) {
        var _this = this;
        return this.http.get("/api/resources/1/" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResourcesService.prototype.getResponsibilityBreakdownByResource = function (id) {
        var _this = this;
        return this.http.get("tiles/ResponsibilityBreakdownByResource?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResourcesService.prototype.getFollowingBreakdownByResource = function (id) {
        var _this = this;
        return this.http.get("tiles/FollowingBreakdownByResource?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResourcesService.prototype.getResponsibilitiesByResourceByType = function (type, id, targetType, targetId) {
        var _this = this;
        return this.http.get("api/" + type + "/" + id + "/ownership/" + targetType + "/" + targetId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    //    public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
    ResourcesService.prototype.getFollowingByResourceByType = function (resourceID, type, id) {
        var _this = this;
        return this.http.get("queries/followingbyresourcebytype?resourceID=" + resourceID + "&type=" + type + "&id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResourcesService.prototype.exportFollowingByResourceByType = function (resourceID, type, id) {
        window.location.assign("/resources/" + resourceID + "/following/" + type + "/" + id + ".xlsx");
    };
    ResourcesService.prototype.exportResponsibilitiesByResourceByType = function (resourceID, type, id) {
        window.location.assign("/resources/" + resourceID + "/ownership/" + type + "/" + id + ".xlsx");
    };
    ResourcesService.prototype.getMyCredentials = function () {
        var _this = this;
        return this.http.get('overlays/myapicredentialsng')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResourcesService.prototype.getUserGroups = function (resourceID) {
        var _this = this;
        return this.http.get("resources/_GroupsByResourceID?id=" + resourceID)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResourcesService.prototype.resetResourcesPassword = function (resourceID) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        return this.http
            .post("form/ResetResourcePassword", 'ID=' + resourceID, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ResourcesService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ResourcesService);
    return ResourcesService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 663:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__ = __webpack_require__(15);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return RightSidebarService; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};


var RightSidebarService = (function () {
    function RightSidebarService() {
        // Observable sources
        this.rightSidebarSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.rightSidebarClearSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.rightSidebarClickedSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        // Observable streams
        this.rightSidebar$ = this.rightSidebarSource.asObservable();
        this.rightSidebarClear$ = this.rightSidebarClearSource.asObservable();
        this.rightSidebarClicked$ = this.rightSidebarClickedSource.asObservable();
    }
    // Service message commands
    RightSidebarService.prototype.showItem = function (rightSidebarItem) {
        this.rightSidebarSource.next(rightSidebarItem);
    };
    RightSidebarService.prototype.clearItems = function () {
        this.rightSidebarClearSource.next(true);
    };
    RightSidebarService.prototype.itemClicked = function (item) {
        this.rightSidebarClickedSource.next(item);
    };
    RightSidebarService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [])
    ], RightSidebarService);
    return RightSidebarService;
}());


/***/ },

/***/ 664:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_grid_definition_model__ = __webpack_require__(294);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__models_enums_model__ = __webpack_require__(115);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return RulesService; });
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






var RulesService = (function (_super) {
    __extends(RulesService, _super);
    function RulesService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    RulesService.prototype.getRuleTypes = function () {
        var _this = this;
        return this.http.get('api/ruletypes')
            .toPromise()
            .then(function (response) { return response.json().ruleTypes; })
            .catch(function (err) { return _this.handleError(err); });
    };
    RulesService.prototype.getRules = function () {
        var _this = this;
        return this.http.get('api/rules')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RulesService.prototype.getRule = function (id) {
        var _this = this;
        return this.http.get("api/rule/" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RulesService.prototype.deleteRule = function (id) {
        return this.deleteDynamicWithResult(this.http, 'rule', id);
    };
    RulesService.prototype.saveRule = function (rule) {
        if (rule.ID == undefined || !rule.ID) {
            return this.postDynamic(this.http, 'rule', rule);
        }
        return this.putDynamic(this.http, 'rule', rule);
    };
    RulesService.prototype.getRuleDimensions = function () {
        var _this = this;
        return this.http.get('api/ruledimensions')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RulesService.prototype.getResultsByRule = function (id, pageNumber, pageSize, sortField, sortOrder, filters, relationships, attributes, simpleFilter) {
        var _this = this;
        var sortOrderText = sortOrder == __WEBPACK_IMPORTED_MODULE_5__models_enums_model__["a" /* SortOrder */].None ? "" : (sortOrder == __WEBPACK_IMPORTED_MODULE_5__models_enums_model__["a" /* SortOrder */].Descending ? "desc" : "asc");
        var uri = "internal/monitor/rules/" + id + "/results?pagesize=" + pageSize + "&pagenum=" + pageNumber + "&sortDataField=" + sortField + "&sortOrder=" + sortOrderText;
        if (filters != undefined) {
            //regular fields
            var normalFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_4__models_grid_definition_model__["a" /* GridFilterFieldType */].Normal; });
            var count = 0;
            uri += '&filterscount=' + normalFilters.length;
            for (var _i = 0, normalFilters_1 = normalFilters; _i < normalFilters_1.length; _i++) {
                var filter = normalFilters_1[_i];
                uri += "&filterdatafield" + count + "=" + filter.field + "&filtercondition" + count + "=" + filter.condition + "&filtervalue" + count + "=" + filter.value;
                count++;
            }
            //related filter fields
            var rellFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_4__models_grid_definition_model__["a" /* GridFilterFieldType */].Relation; });
            count = 0;
            uri += '&relfilterscount=' + rellFilters.length;
            for (var _a = 0, rellFilters_1 = rellFilters; _a < rellFilters_1.length; _a++) {
                var filter = rellFilters_1[_a];
                uri += "&relfilterdatafield" + count + "=" + filter.field.replace("Field", "") + "&relfiltercondition" + count + "=" + filter.condition + "&relfiltervalue" + count + "=" + filter.value;
                count++;
            }
            //hiden filter fields
            var hidFilters = filters.filter(function (f) { return f.fieldtype == __WEBPACK_IMPORTED_MODULE_4__models_grid_definition_model__["a" /* GridFilterFieldType */].Hidden; });
            count = 0;
            uri += '&hidfilterscount=' + hidFilters.length;
            for (var _b = 0, hidFilters_1 = hidFilters; _b < hidFilters_1.length; _b++) {
                var filter = hidFilters_1[_b];
                uri += "&hidfilterdatafield" + count + "=" + filter.field.replace("Field", "") + "&hidfiltercondition" + count + "=" + filter.condition + "&hidfiltervalue" + count + "=" + filter.value;
                count++;
            }
        }
        if (attributes != undefined) {
            uri += "&AttributeSearchValue=" + attributes.attributeSearchValue + "&AttributeType=" + attributes.attributeType;
        }
        if (relationships != undefined) {
            uri += "&RelationshipIncludeType=" + relationships.includeType + "&RelationshipObjectType=" + relationships.relationshipType.TargetType.replace("Type", "") + "&RelationshipObjectIDs=" + relationships.objectIds.join(",");
        }
        if (simpleFilter != undefined) {
            uri += "&filter=" + simpleFilter;
        }
        return this.http.get(uri)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    RulesService.prototype.getResultsByRuleExcel = function (id) {
        window.location.assign("internal/monitor/ExportResultsByRule?id=" + id);
    };
    RulesService.prototype.deleteDimension = function (id) {
        return this.deleteDynamicWithResult(this.http, 'ruledimension', id);
    };
    RulesService.prototype.saveDimension = function (ruleDimension) {
        if (ruleDimension.ID == undefined || !ruleDimension.ID) {
            return this.postDynamic(this.http, 'ruledimension', ruleDimension);
        }
        return this.putDynamic(this.http, 'ruledimension', ruleDimension);
    };
    RulesService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], RulesService);
    return RulesService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 665:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return ScoreService; });
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




var ScoreService = (function (_super) {
    __extends(ScoreService, _super);
    function ScoreService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    ScoreService.prototype.getPointBreakdown = function (objectID, objectType) {
        var _this = this;
        return this.http.get("queries/" + objectType + "/" + objectID + "/PointBreakdownByObject")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ScoreService.prototype.getScoreHistory = function (objectID, objectType) {
        var _this = this;
        return this.http.get("queries/" + objectType + "/" + objectID + "/ScoreHistoryByObject")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ScoreService.prototype.getAverageScore = function (objectID, objectType) {
        var _this = this;
        return this.http.get("queries/" + objectType + "/" + objectID + "/AverageScoreByObjectType")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    ScoreService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], ScoreService);
    return ScoreService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 666:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SearchService; });
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




var SearchService = (function (_super) {
    __extends(SearchService, _super);
    function SearchService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    SearchService.prototype.getSearchResults = function (term, size, pageNum, searchTypes, category, isExactMatch, advancedSearchFilter) {
        var _this = this;
        term = (isExactMatch ? "'" + term + "'" : term);
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });
        this.addRequestVerificationHeaders(headers);
        var url = '';
        if (category && category.Categories)
            url = "from=" + pageNum + "&size=" + size + "&search=" + (advancedSearchFilter ? '' : term) + "&group=&type=" + category.Name + "&adv=" + (advancedSearchFilter ? JSON.stringify(advancedSearchFilter) : '');
        else
            url = "from=" + pageNum + "&size=" + size + "&search=" + (advancedSearchFilter ? '' : term) + "&group=" + (category && !category.DisplayName ? category.Name : '') + "&type=" + (searchTypes ? searchTypes.join(',') : '') + "&adv=" + (advancedSearchFilter ? JSON.stringify(advancedSearchFilter) : '');
        return this.http
            .post('search/results', url, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SearchService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], SearchService);
    return SearchService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 667:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return CompanySettingsService; });
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




var CompanySettingsService = (function (_super) {
    __extends(CompanySettingsService, _super);
    function CompanySettingsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    CompanySettingsService.prototype.getSettings = function () {
        var _this = this;
        return this.http.get('/form/CompanySettings')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    CompanySettingsService.prototype.putSettings = function (companySettings) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        return this.http.put('/form/UpdateCompanySettings', JSON.stringify(companySettings), { headers: headers })
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    CompanySettingsService.prototype.getAuthenticationModel = function () {
        var _this = this;
        return this.http.get('api/authenticationModel')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    CompanySettingsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], CompanySettingsService);
    return CompanySettingsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 668:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SiteMenuService; });
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




var SiteMenuService = (function (_super) {
    __extends(SiteMenuService, _super);
    function SiteMenuService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    SiteMenuService.prototype.getMenu = function () {
        var _this = this;
        return this.http.get('navigation/sitemenu')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.getAvailableItems = function () {
        var _this = this;
        return this.http.get('navigation/GetAvailableSiteNavigation')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.addFolderItem = function (item) {
        var _this = this;
        return this.http.post('navigation/AddFolderItem', item)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.addFolder = function (model) {
        var _this = this;
        return this.http.post('navigation/AddFolder', model)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.removeFolderItem = function (id) {
        var _this = this;
        return this.http.post("navigation/RemoveFolderItem?id=" + id, null)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.removeFolder = function (id) {
        var _this = this;
        return this.http.post("navigation/RemoveFolder?id=" + id, null)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.moveFolderUp = function (id) {
        var _this = this;
        return this.http.put("navigation/MoveUp?id=" + id, null)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.moveFolderDown = function (id) {
        var _this = this;
        return this.http.put("navigation/MoveDown?id=" + id, null)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.editFolder = function (folder) {
        var _this = this;
        return this.http.put('navigation/EditFolder', folder)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.getSiteNavItems = function () {
        var _this = this;
        return this.http.get('navigation/GetSiteNavItems')
            .toPromise()
            .then(function (response) { return response.json(); })
            .then(function (r) {
            r.forEach(function (s) {
                if (s.Name.indexOf('#') == 0) {
                    s.IsCustom = false;
                    s.DisplayName = s.Name.substring(1);
                }
                else {
                    s.DisplayName = s.Name;
                    s.IsCustom = true;
                }
            });
            return r;
        })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService.prototype.getSiteNavFolderItems = function (folderId) {
        var _this = this;
        return this.http.get("form/GetSiteNavFolderItems?id=" + folderId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SiteMenuService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], SiteMenuService);
    return SiteMenuService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 669:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SocialService; });
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




var SocialService = (function (_super) {
    __extends(SocialService, _super);
    function SocialService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    SocialService.prototype.getComments = function (objectID, objectType, daysToLookBack, page, count, typeFilter) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });
        this.addRequestVerificationHeaders(headers);
        return this.http
            .post("services/community/comments", "IsNg=true&ObjectType=" + objectType + "&ObjectID=" + (objectID > 0 ? objectID : '') + "&Skip=" + (page ? page : 0) + "&Take=" + (count ? count : 10) + "&DateFilter=-" + daysToLookBack + "&TypeFilter=" + (typeFilter == undefined ? '' : typeFilter), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SocialService.prototype.vote = function (commentID, vote) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });
        this.addRequestVerificationHeaders(headers);
        return this.http
            .post('services/community/vote', "CommentID=" + commentID + "&Vote=" + vote, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SocialService.prototype.editComment = function (commentEditData) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        this.addRequestVerificationHeaders(headers);
        return this.http
            .post('services/community/edit', commentEditData, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SocialService.prototype.addComment = function (commentAddData) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        this.addRequestVerificationHeaders(headers);
        return this.http
            .post('services/community/comment', commentAddData, { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SocialService.prototype.getMyCounts = function (daysToLookBack) {
        var _this = this;
        return this.http.get("api/count/social/" + daysToLookBack)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SocialService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], SocialService);
    return SocialService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 670:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__ = __webpack_require__(15);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__models_enums_model__ = __webpack_require__(115);
/* unused harmony export ArtifactTypeFilters */
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return StateService; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};



var ArtifactTypeFilters = (function () {
    function ArtifactTypeFilters() {
        this.currentPageNumber = 0;
        this.sortField = "";
        this.sortOrder = __WEBPACK_IMPORTED_MODULE_2__models_enums_model__["a" /* SortOrder */].None;
        this.filters = [];
        this.showSimpleFilter = true;
    }
    return ArtifactTypeFilters;
}());
var StateService = (function () {
    function StateService() {
        this.siteMenuRequiresReloadSource = new __WEBPACK_IMPORTED_MODULE_1_rxjs_Subject__["Subject"]();
        this.siteMenuRequiresReload$ = this.siteMenuRequiresReloadSource.asObservable();
        this.artifactTypeFilters = new ArtifactTypeFilters();
    }
    StateService.prototype.resetArtifactTypeFilterIfRequired = function (artifactTypeId) {
        if (this.artifactTypeFilters.artifactTypeId != artifactTypeId) {
            this.artifactTypeFilters = new ArtifactTypeFilters();
            this.artifactTypeFilters.artifactTypeId = artifactTypeId;
        }
    };
    StateService.prototype.reloadLeftNavMenu = function () {
        this.siteMenuRequiresReloadSource.next(true);
    };
    StateService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [])
    ], StateService);
    return StateService;
}());


/***/ },

/***/ 671:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return StatisticService; });
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




var StatisticService = (function (_super) {
    __extends(StatisticService, _super);
    function StatisticService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    StatisticService.prototype.getStatistics = function () {
        var _this = this;
        return this.http.get("api/statistics")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    StatisticService.prototype.getStatistic = function (id) {
        var _this = this;
        return this.http.get("form/statistictype_formdata?id=" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    StatisticService.prototype.deleteStatistic = function (id) {
        return this.deleteDynamicWithResult(this.http, 'statistictype', id);
    };
    StatisticService.prototype.saveStatistic = function (statisticType) {
        if (statisticType.ID == undefined || !statisticType.ID) {
            return this.postDynamic(this.http, 'statistictype', statisticType);
        }
        return this.putDynamic(this.http, 'statistictype', statisticType);
    };
    StatisticService.prototype.getStatisticCheckTypes = function () {
        var _this = this;
        return this.http.get('form/statistictype_checktypeoptions')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    StatisticService.prototype.getStatisticObjects = function () {
        var _this = this;
        return this.http.get('form/statistictype_objectoptions')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    StatisticService.prototype.getStatisticCheckObjects = function (type, id, check) {
        var _this = this;
        return this.http.get("form/statistictype_checkobjectoptions?type=" + type + "&id=" + id + "&check=" + check)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    StatisticService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], StatisticService);
    return StatisticService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 672:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__models_survey_model__ = __webpack_require__(488);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SurveysService; });
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





var SurveysService = (function (_super) {
    __extends(SurveysService, _super);
    function SurveysService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    SurveysService.prototype.getSurveyTypes = function () {
        var _this = this;
        return this.http.get("api/surveys")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SurveysService.prototype.getSurveyTypeQuestions = function (survey) {
        var _this = this;
        return this.http.get("api/surveys/" + survey.ID + "/questions")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SurveysService.prototype.getSurveyTypeQuestionDetails = function (id, surveyTypeId) {
        var _this = this;
        return this.http.get("form/questiontype_formdata?id=" + id + "&surveyTypeID=" + surveyTypeId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SurveysService.prototype.deleteSurveyTypeById = function (id) {
        return this.deleteDynamicWithResult(this.http, 'surveytype', id);
    };
    SurveysService.prototype.deleteSurveyQuestionType = function (id) {
        return this.deleteDynamicWithResult(this.http, 'surveyquestiontype', id);
    };
    SurveysService.prototype.saveSurveyType = function (surveyType) {
        if (surveyType.ID == undefined || !surveyType.ID) {
            return this.postDynamic(this.http, 'surveytype', surveyType);
        }
        return this.putDynamic(this.http, 'surveytype', surveyType);
    };
    SurveysService.prototype.saveSurveyTypeQuestion = function (surveyQuestion) {
        if (surveyQuestion.ID == undefined || !surveyQuestion.ID) {
            return this.addSurveyTypeQuestion(surveyQuestion);
        }
        return this.editSurveyTypeQuestion(surveyQuestion);
    };
    SurveysService.prototype.addSurveyTypeQuestion = function (surveyQuestion) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .post('form/AddQuestionType', JSON.stringify(surveyQuestion), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SurveysService.prototype.editSurveyTypeQuestion = function (surveyQuestion) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .put('form/EditQuestionType/', JSON.stringify(surveyQuestion), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SurveysService.prototype.getObjectSurvey = function (parentObjectID, parentObjectType, objectID, objectType) {
        var _this = this;
        return this.http.get("api/surveys/" + parentObjectType + "/" + parentObjectID + "/" + objectType + "/" + objectID + "/survey")
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SurveysService.prototype.saveSurveyResponse = function (response, surveyId, objectType, objectId) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        var surveyResponse = new __WEBPACK_IMPORTED_MODULE_4__models_survey_model__["a" /* SurveyResponse */]();
        for (var _i = 0, response_1 = response; _i < response_1.length; _i++) {
            var question = response_1[_i];
            question.Values = question.Items;
        }
        surveyResponse.Questions = response;
        return this.http
            .post("api/survey/" + surveyId + "/" + objectId + "/" + objectType, JSON.stringify(surveyResponse), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    SurveysService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], SurveysService);
    return SurveysService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 673:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TagService; });
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




var TagService = (function (_super) {
    __extends(TagService, _super);
    function TagService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    TagService.prototype.getTags = function (phrase) {
        var _this = this;
        return this.http.get("api/tagsuggestions?phrase=" + phrase)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TagService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], TagService);
    return TagService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 674:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TaxonomiesService; });
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




var TaxonomiesService = (function (_super) {
    __extends(TaxonomiesService, _super);
    function TaxonomiesService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    TaxonomiesService.prototype.getTaxonomies = function () {
        var _this = this;
        return this.http.get('/api/catalogs')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TaxonomiesService.prototype.getTaxonomy = function (id) {
        var _this = this;
        return this.http.get("/api/catalogs/" + id)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TaxonomiesService.prototype.getTaxonomyClassifications = function () {
        var _this = this;
        return this.http.get('/api/TaxonomyClassifications')
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TaxonomiesService.prototype.saveTaxonomy = function (taxonomy) {
        if (taxonomy.ID == undefined || !taxonomy.ID) {
            return this.post(taxonomy);
        }
        return this.put(taxonomy);
    };
    TaxonomiesService.prototype.updateTaxonomyWithId = function (taxonomy, result) {
        taxonomy.ID = Number(result.id);
        return taxonomy;
    };
    TaxonomiesService.prototype.post = function (taxonomy) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("form/AddTaxonomyTypeRaw", JSON.stringify(taxonomy), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TaxonomiesService.prototype.put = function (taxonomy) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/json'
        });
        return this.http
            .put('form/EditTaxonomyTypeRaw', JSON.stringify(taxonomy), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TaxonomiesService.prototype.deleteTaxonomy = function (taxonomyId) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var url = "form/catalogs/" + taxonomyId;
        return this.http
            .delete(url, headers)
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TaxonomiesService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], TaxonomiesService);
    return TaxonomiesService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 675:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return TypeaheadSearchService; });
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




var TypeaheadSearchService = (function (_super) {
    __extends(TypeaheadSearchService, _super);
    function TypeaheadSearchService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    TypeaheadSearchService.prototype.getResults = function (size, term, types) {
        var _this = this;
        return this.http.get("search/typeahead?q=" + term + "&num=" + size + "&t=" + (types != undefined ? types.join(',') : ''))
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TypeaheadSearchService.prototype.getObjectTypeItems = function (size, term, objectType, objectId) {
        var _this = this;
        return this.http.get("api/breadcrumb/typeahead?q=" + term + "&num=" + size + "&objectType=" + objectType + "&objectId=" + objectId)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    TypeaheadSearchService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], TypeaheadSearchService);
    return TypeaheadSearchService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 676:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return UriBasedService; });
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




var UriBasedService = (function (_super) {
    __extends(UriBasedService, _super);
    function UriBasedService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    UriBasedService.prototype.getItems = function (uri) {
        var _this = this;
        return this.http.get(uri)
            .toPromise()
            .then(function (response) { return response.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    //assumes delete url ends with id of item to delete...
    UriBasedService.prototype.deleteItem = function (uri, id) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        return this.http
            .delete("" + uri + id, headers)
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    UriBasedService.prototype.deleteItemWithResult = function (uri, id) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        return this.http
            .delete("" + uri + id, headers)
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    UriBasedService.prototype.saveItem = function (createUri, editUri, item) {
        if (item.ID == undefined || !item.ID) {
            return this.post(createUri, item);
        }
        return this.put(editUri, item);
    };
    UriBasedService.prototype.post = function (uri, item) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        return this.http
            .post(uri, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    UriBasedService.prototype.put = function (uri, item) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        return this.http
            .put(uri, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    UriBasedService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], UriBasedService);
    return UriBasedService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 677:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__base_service__ = __webpack_require__(7);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return WebAnalyticsService; });
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




var WebAnalyticsService = (function (_super) {
    __extends(WebAnalyticsService, _super);
    function WebAnalyticsService(http, messagesService) {
        _super.call(this, messagesService);
        this.http = http;
    }
    WebAnalyticsService.prototype.logActivity = function (activity) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({ 'Content-Type': 'application/json' });
        var options = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["c" /* RequestOptions */]({ headers: headers });
        this.http.post('webanalytics/logactivity', JSON.stringify(activity), options)
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    WebAnalyticsService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_1__angular_http__["b" /* Http */]) === 'function' && _a) || Object, (typeof (_b = typeof __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_2__messages_service__["a" /* MessagesService */]) === 'function' && _b) || Object])
    ], WebAnalyticsService);
    return WebAnalyticsService;
    var _a, _b;
}(__WEBPACK_IMPORTED_MODULE_3__base_service__["a" /* BaseService */]));


/***/ },

/***/ 7:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__angular_core__ = __webpack_require__(0);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__angular_http__ = __webpack_require__(4);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2_rxjs_add_operator_toPromise__ = __webpack_require__(278);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2_rxjs_add_operator_toPromise___default = __webpack_require__.n(__WEBPACK_IMPORTED_MODULE_2_rxjs_add_operator_toPromise__);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__messages_service__ = __webpack_require__(6);
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return BaseService; });
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};




var BaseService = (function () {
    function BaseService(messages) {
        this.messages = messages;
    }
    BaseService.prototype.handleError = function (error) {
        console.error('An error occurred', error);
        if (this && this.messages)
            this.messages.showError('Error', error.toString());
        return Promise.reject(error.message || error);
    };
    BaseService.prototype.deleteDynamic = function (http, type, id) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var url = "form/dynamicedit/delete/" + type + "/" + id;
        return http
            .delete(url, headers)
            .toPromise()
            .catch(function (err) { return _this.handleError(err); });
    };
    BaseService.prototype.deleteDynamicWithResult = function (http, type, id) {
        var _this = this;
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]();
        headers.append('Content-Type', 'application/json');
        var url = "form/dynamicedit/delete/" + type + "/" + id;
        return http
            .delete(url, headers)
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    BaseService.prototype.postDynamic = function (http, type, item, file) {
        var _this = this;
        if (file != undefined) {
            var form = new FormData();
            form.append('json', JSON.stringify(item));
            form.append('file', file);
            return http
                .post("form/dynamicedit/create/" + type, form)
                .toPromise()
                .then(function (res) { return res.json(); })
                .catch(function (err) { return _this.handleError(err); });
        }
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        return http
            .post("form/dynamicedit/create/" + type, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    BaseService.prototype.putDynamic = function (http, type, item, file) {
        var _this = this;
        if (file != undefined) {
            var form = new FormData();
            form.append('json', JSON.stringify(item));
            form.append('file', file);
            return http
                .put("form/dynamicedit/edit/" + type, form)
                .toPromise()
                .then(function (res) { return res.json(); })
                .catch(function (err) { return _this.handleError(err); });
        }
        var headers = new __WEBPACK_IMPORTED_MODULE_1__angular_http__["a" /* Headers */]({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });
        return http
            .put("form/dynamicedit/edit/" + type, 'json=' + encodeURIComponent(JSON.stringify(item)), { headers: headers })
            .toPromise()
            .then(function (res) { return res.json(); })
            .catch(function (err) { return _this.handleError(err); });
    };
    BaseService.prototype.addRequestVerificationHeaders = function (headers) {
        headers.append('RequestVerificationToken', document.getElementById('antiForgeryToken').value);
        headers.append('X-Requested-With', 'XMLHttpRequest');
    };
    BaseService = __decorate([
        __webpack_require__.i(__WEBPACK_IMPORTED_MODULE_0__angular_core__["Injectable"])(), 
        __metadata('design:paramtypes', [(typeof (_a = typeof __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */] !== 'undefined' && __WEBPACK_IMPORTED_MODULE_3__messages_service__["a" /* MessagesService */]) === 'function' && _a) || Object])
    ], BaseService);
    return BaseService;
    var _a;
}());


/***/ },

/***/ 71:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_0__messages_service__ = __webpack_require__(6);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_1__artifact_type_service__ = __webpack_require__(499);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_2__claims_service__ = __webpack_require__(497);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_3__fields_service__ = __webpack_require__(495);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_4__responsibility_type_service__ = __webpack_require__(500);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_5__header_actions_service__ = __webpack_require__(177);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_6__header_breadcrumb_service__ = __webpack_require__(145);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_7__object_detail_service__ = __webpack_require__(486);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_8__responsibility_service__ = __webpack_require__(492);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_9__settings_service__ = __webpack_require__(667);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_10__templates_service__ = __webpack_require__(501);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_11__workflow_service__ = __webpack_require__(502);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_12__typeahead_search_service__ = __webpack_require__(675);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_13__taxonomies_service__ = __webpack_require__(674);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_14__object_style_service__ = __webpack_require__(656);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_15__lookup_service__ = __webpack_require__(652);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_16__grid_definition_service__ = __webpack_require__(650);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_17__uri_based_service__ = __webpack_require__(676);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_18__editor_definition_service__ = __webpack_require__(646);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_19__rules_service__ = __webpack_require__(664);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_20__policies_service__ = __webpack_require__(658);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_21__predicates_service__ = __webpack_require__(659);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_22__relationships_service__ = __webpack_require__(491);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_23__statistics_service__ = __webpack_require__(671);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_24__reports_service__ = __webpack_require__(661);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_25__attribute_type_service__ = __webpack_require__(642);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_26__site_menu_service__ = __webpack_require__(668);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_27__right_sidebar_service__ = __webpack_require__(663);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_28__audit_service__ = __webpack_require__(643);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_29__artifacts_service__ = __webpack_require__(641);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_30__permissions_service__ = __webpack_require__(657);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_31__fusion_service__ = __webpack_require__(487);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_32__models_service__ = __webpack_require__(653);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_33__surveys_service__ = __webpack_require__(672);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_34__object_statistics_service__ = __webpack_require__(655);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_35__dashboard_service__ = __webpack_require__(644);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_36__object_actions_service__ = __webpack_require__(654);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_37__web_analytics_service__ = __webpack_require__(677);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_38__score_service__ = __webpack_require__(665);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_39__resources_service__ = __webpack_require__(662);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_40__social_service__ = __webpack_require__(669);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_41__tag_service__ = __webpack_require__(673);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_42__search_service__ = __webpack_require__(666);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_43__follower_service__ = __webpack_require__(648);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_44__state_service__ = __webpack_require__(670);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_45__fusion_attribute_service__ = __webpack_require__(649);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_46__group_service__ = __webpack_require__(489);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_47__authentication_service__ = __webpack_require__(256);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_48__reference_service__ = __webpack_require__(660);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_49__favorites_service__ = __webpack_require__(647);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_50__diagram_service__ = __webpack_require__(645);
/* harmony import */ var __WEBPACK_IMPORTED_MODULE_51__levels_service__ = __webpack_require__(651);
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "a", function() { return __WEBPACK_IMPORTED_MODULE_0__messages_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "w", function() { return __WEBPACK_IMPORTED_MODULE_1__artifact_type_service__["a"]; });
/* unused harmony namespace reexport */
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "A", function() { return __WEBPACK_IMPORTED_MODULE_3__fields_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "u", function() { return __WEBPACK_IMPORTED_MODULE_4__responsibility_type_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "o", function() { return __WEBPACK_IMPORTED_MODULE_5__header_actions_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "g", function() { return __WEBPACK_IMPORTED_MODULE_6__header_breadcrumb_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "f", function() { return __WEBPACK_IMPORTED_MODULE_7__object_detail_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "L", function() { return __WEBPACK_IMPORTED_MODULE_8__responsibility_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "t", function() { return __WEBPACK_IMPORTED_MODULE_9__settings_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "K", function() { return __WEBPACK_IMPORTED_MODULE_10__templates_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "d", function() { return __WEBPACK_IMPORTED_MODULE_11__workflow_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "W", function() { return __WEBPACK_IMPORTED_MODULE_12__typeahead_search_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "z", function() { return __WEBPACK_IMPORTED_MODULE_13__taxonomies_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "E", function() { return __WEBPACK_IMPORTED_MODULE_14__object_style_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "F", function() { return __WEBPACK_IMPORTED_MODULE_15__lookup_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "l", function() { return __WEBPACK_IMPORTED_MODULE_16__grid_definition_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "k", function() { return __WEBPACK_IMPORTED_MODULE_17__uri_based_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "j", function() { return __WEBPACK_IMPORTED_MODULE_18__editor_definition_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "M", function() { return __WEBPACK_IMPORTED_MODULE_19__rules_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "B", function() { return __WEBPACK_IMPORTED_MODULE_20__policies_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "G", function() { return __WEBPACK_IMPORTED_MODULE_21__predicates_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "v", function() { return __WEBPACK_IMPORTED_MODULE_22__relationships_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "I", function() { return __WEBPACK_IMPORTED_MODULE_23__statistics_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "J", function() { return __WEBPACK_IMPORTED_MODULE_24__reports_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "C", function() { return __WEBPACK_IMPORTED_MODULE_25__attribute_type_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "H", function() { return __WEBPACK_IMPORTED_MODULE_26__site_menu_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "i", function() { return __WEBPACK_IMPORTED_MODULE_27__right_sidebar_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "x", function() { return __WEBPACK_IMPORTED_MODULE_28__audit_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "m", function() { return __WEBPACK_IMPORTED_MODULE_29__artifacts_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "s", function() { return __WEBPACK_IMPORTED_MODULE_30__permissions_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "D", function() { return __WEBPACK_IMPORTED_MODULE_31__fusion_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "O", function() { return __WEBPACK_IMPORTED_MODULE_32__models_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "r", function() { return __WEBPACK_IMPORTED_MODULE_33__surveys_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "p", function() { return __WEBPACK_IMPORTED_MODULE_34__object_statistics_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "S", function() { return __WEBPACK_IMPORTED_MODULE_35__dashboard_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "U", function() { return __WEBPACK_IMPORTED_MODULE_36__object_actions_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "h", function() { return __WEBPACK_IMPORTED_MODULE_37__web_analytics_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "q", function() { return __WEBPACK_IMPORTED_MODULE_38__score_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "e", function() { return __WEBPACK_IMPORTED_MODULE_39__resources_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "b", function() { return __WEBPACK_IMPORTED_MODULE_40__social_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "c", function() { return __WEBPACK_IMPORTED_MODULE_41__tag_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "V", function() { return __WEBPACK_IMPORTED_MODULE_42__search_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "n", function() { return __WEBPACK_IMPORTED_MODULE_43__follower_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "y", function() { return __WEBPACK_IMPORTED_MODULE_44__state_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "P", function() { return __WEBPACK_IMPORTED_MODULE_45__fusion_attribute_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "R", function() { return __WEBPACK_IMPORTED_MODULE_46__group_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "Y", function() { return __WEBPACK_IMPORTED_MODULE_47__authentication_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "T", function() { return __WEBPACK_IMPORTED_MODULE_48__reference_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "X", function() { return __WEBPACK_IMPORTED_MODULE_49__favorites_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "Q", function() { return __WEBPACK_IMPORTED_MODULE_50__diagram_service__["a"]; });
/* harmony namespace reexport (by used) */ __webpack_require__.d(exports, "N", function() { return __WEBPACK_IMPORTED_MODULE_51__levels_service__["a"]; });






















































/***/ },

/***/ 85:
/***/ function(module, exports, __webpack_require__) {

"use strict";
/* harmony export (binding) */ __webpack_require__.d(exports, "a", function() { return SiteUrlHelpers; });
var SiteUrlHelpers = (function () {
    function SiteUrlHelpers() {
    }
    // getObjectUrl - Generates the url for an object based on its type
    SiteUrlHelpers.getObjectUrl = function (objectType, objectId, parentId, objectName) {
        switch (objectType.toUpperCase()) {
            case 'ARTIFACTTYPE':
                return SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT + "/" + objectId;
            case 'ARTIFACT':
                return SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT + "/" + parentId + "/" + objectId;
            case 'FUSIONTYPE':
                return SiteUrlHelpers.SITE_URL_FUSION_ROOT + "/" + objectId;
            case 'FUSIONATTRIBUTE':
                return SiteUrlHelpers.SITE_URL_FUSION_ROOT + "/" + SiteUrlHelpers.SITE_URL_FUSION_BY_FUSIONATTRIBUTEID + "/" + parentId + "/" + objectId;
            case 'GROUP':
                return SiteUrlHelpers.SITE_URL_GROUP_ROOT + "/" + objectId;
            case 'RESOURCE':
                return SiteUrlHelpers.SITE_URL_RESOURCE_ROOT + "/" + objectId;
            case 'TAXONOMY':
                return SiteUrlHelpers.SITE_URL_MODEL_ROOT + "/" + parentId + ";hierarchyId=" + objectId;
            case 'TAXONOMYTYPE':
                return SiteUrlHelpers.SITE_URL_MODEL_ROOT + "/" + objectId + "/structure";
            case 'TAXONOMYTYPECLASS':
                return SiteUrlHelpers.SITE_URL_MODEL_ROOT + "/classification/" + objectName;
            case 'POLICYTYPECLASS':
                return SiteUrlHelpers.SITE_URL_POLICY_ROOT + "/classification/" + objectId;
            case 'POLICYTYPE':
                return SiteUrlHelpers.SITE_URL_POLICY_ROOT + "/" + objectId + "/structure";
            case 'RULE':
                return SiteUrlHelpers.SITE_URL_RULE_ROOT + "/" + objectId;
            default:
                console.log('Unable to generate object link', objectType, objectId);
        }
    };
    // convertClassicUrl - Converts a url from the legacy site to the new url used in angular
    // inputs - url the old url
    // output - the converted url
    // CURRENT USES mainly used by search as elastic search stores the url of the results but doesnt store the parent type
    // of objects making it not posible to get the object url by building it
    SiteUrlHelpers.convertClassicUrl = function (url) {
        console.log("convert", url);
        if (url.startsWith('#/artifacts'))
            return url.replace('#/artifacts', SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT);
        else if (url.startsWith('#/resources'))
            return url.replace('#/resources', SiteUrlHelpers.SITE_URL_RESOURCE_ROOT);
        else if (url.startsWith('#/groups'))
            return url.replace('#/groups', SiteUrlHelpers.SITE_URL_GROUP_ROOT);
        else if (url.startsWith('#/fusion/item')) {
            var parts = url.split('/');
            if (parts.length == 5) {
                return SiteUrlHelpers.SITE_URL_FUSION_ROOT + "/" + SiteUrlHelpers.SITE_URL_FUSION_BY_FUSIONATTRIBUTEID + "/" + parts[3] + "/" + parts[4];
            }
            console.log('[ERROR] - INVALID FORMAT FOR FUSION ATTRIBUTE URL', url);
        }
        else if (url.startsWith('#/fusion/')) {
            var parts = url.split('/');
            if (parts.length == 4) {
                return SiteUrlHelpers.SITE_URL_FUSION_ROOT + "/" + SiteUrlHelpers.SITE_URL_FUSION_LIST + parts[3];
            }
            console.log('[ERROR] - INVALID FORMAT FOR FUSION TYPE URL', url);
        }
        else if (url.startsWith('#/catalogs')) {
            var parts = url.split('/');
            if (parts.length == 4) {
                return SiteUrlHelpers.SITE_URL_MODEL_ROOT + "/" + parts[2] + ";hierarchyId=" + parts[3];
            }
            else if (parts.length == 3) {
                return SiteUrlHelpers.SITE_URL_MODEL_ROOT + "/" + parts[2] + "/structure";
            }
            console.log('[ERROR] - INVALID FORMAT FOR MODEL URL', url);
        }
        else if (url.startsWith('#/domains')) {
            console.log('[ERROR] - DOMAIN TYPE NOT SUPPORTED BY NEW UI');
            return url;
        }
        else {
            console.log('[ERROR] - CANNOT CONVERT CLASSIC URL TO NEW URL', url);
            return url;
        }
    };
    // returns the font awesome icon for the associated url
    SiteUrlHelpers.getObjectIcon = function (objectType) {
        switch (objectType.toUpperCase()) {
            case 'ARTIFACTTYPE':
            case 'ARTIFACT':
                return 'book';
            case 'FUSIONTYPE':
            case 'GROUP':
            case 'COMMUNITY':
                return 'users';
            case 'RESOURCE':
                return 'user';
            case 'TAXONOMY':
            case 'TAXONOMYTYPE':
            case 'TAXONOMYTYPECLASS':
            case 'MODEL':
                return 'sitemap';
            case 'POLICY':
                return 'university';
            case 'RULE':
                return 'pie-chart';
            case 'MONITOR':
                return 'tachometer';
            case 'REFERENCE':
                return 'cubes';
            case 'FUSION':
                return 'database';
            default:
                return 'question';
        }
    };
    //prefix route for all routes
    // THIS SETTING NEEDS TO BE IN SYNC WITH THE SETTING IN D360.WEB / STARTUP.CS SO THE APPROPRIATE HTML PAGE IS INITIALLY SERVED
    SiteUrlHelpers.SITE_URL_PREFIX = ''; // a/
    //main site routes
    // WARNING!! - SOME URLS SUCH AS TOOLTIPS ARE BURNED IN THE DB DO NOT CHANGES THE BELOW WITHOUT 
    // UPDATING BOTH!!
    SiteUrlHelpers.SITE_URL_FUSION_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "fusion";
    SiteUrlHelpers.SITE_URL_REFERENCE_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "reference";
    SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "artifact";
    SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "community";
    SiteUrlHelpers.SITE_URL_HELP_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "help";
    SiteUrlHelpers.SITE_URL_MONITOR_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "monitor";
    SiteUrlHelpers.SITE_URL_POLICY_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "policy";
    SiteUrlHelpers.SITE_URL_GROUP_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "group";
    SiteUrlHelpers.SITE_URL_RESOURCE_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "resource";
    SiteUrlHelpers.SITE_URL_RULE_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "quality/rule";
    SiteUrlHelpers.SITE_URL_SEARCH_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "search";
    SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "workflow";
    SiteUrlHelpers.SITE_URL_MODEL_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "model";
    SiteUrlHelpers.SITE_URL_ADMIN_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "admin";
    SiteUrlHelpers.SITE_URL_HOME_ROOT = SiteUrlHelpers.SITE_URL_PREFIX + "home";
    //model child routes
    SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION = 'classification';
    //policy child routes 
    SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION = 'classification';
    //workflow child routes
    SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE = 'raiseissue';
    SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_ITEM = 'work';
    SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_STATUS = 'status';
    //fusion child routes
    SiteUrlHelpers.SITE_URL_FUSION_BY_FUSIONATTRIBUTEID = 'fusionattribute';
    SiteUrlHelpers.SITE_URL_FUSION_LIST = '';
    //admin child routes
    SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD = "load";
    SiteUrlHelpers.SITE_URL_ADMIN_FUSION = "fusion";
    SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES = "attributes";
    SiteUrlHelpers.SITE_URL_ADMIN_ARTIFACTS = "artifacts";
    SiteUrlHelpers.SITE_URL_ADMIN_LOOKUPS = 'lookups';
    SiteUrlHelpers.SITE_URL_ADMIN_MODELS = 'taxonomies';
    SiteUrlHelpers.SITE_URL_ADMIN_POLICIES = 'policies';
    SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS = 'relationships';
    SiteUrlHelpers.SITE_URL_ADMIN_RULES = 'rules';
    SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS = 'surveys';
    SiteUrlHelpers.SITE_URL_ADMIN_ANALYTICS = 'analytics';
    SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS = 'dashboards';
    SiteUrlHelpers.SITE_URL_ADMIN_GROUPS = 'groups';
    SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES = 'responsibilities';
    SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES = 'resources';
    SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS = 'settings';
    SiteUrlHelpers.SITE_URL_ADMIN_TEMPLATES = 'templates';
    SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW = 'workflow';
    SiteUrlHelpers.SITE_URL_ADMIN_DOMAIN = 'domain';
    return SiteUrlHelpers;
}());


/***/ }

},[1147]);
//# sourceMappingURL=main.map