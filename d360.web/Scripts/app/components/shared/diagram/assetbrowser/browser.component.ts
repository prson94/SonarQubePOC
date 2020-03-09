import * as go from 'gojs';
import * as _ from 'lodash';
import { AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChange, SimpleChanges, EventEmitter, Output, AfterViewChecked } from '@angular/core';
import {
    DiagramObjectType,
    AssetBrowserTranslation,
    AssetBrowserApiHopDirection,
    AssetBrowserDiagramAsset,
    AssetBrowserTranslationNode,
    AssetBrowserTranslationLink,
    AssetBrowserTranslationRelationCount,
    FilterAncestryMode,
    FilterAncestryOption,
    AssetBrowserFilterModel,
    AssetTypeFilter,
    FilterSelectionsModel,    StoredAssetBrowserFilterModel,
    AssetBrowserApiHopRequestModel,
    AssetBrowserApiHopAssetRequestModel,
    AssetBrowserTranslationOwnerCount,
    AssetBrowserApiOwnerHopRequestModel,
    AssetBrowserAssetsModel,
    AssetBrowserOwnersModel,
    AssetBrowserModel,
    AssetBrowserAssetModel,
    AssetBrowserGenericRelationModel,
    LoadedFilterTypesModel,
    AssetBrowserApiHopType,
    AssetBrowserAlertRequest,
    AssetBrowserAlert,
    DiagramType
} from '../../../../models/lineage.model';

import { BrowserService } from '../../../../services/browser.service';
import { PermissionsService } from '../../../../services/permissions.service';

import { DiagramBaseComponent } from '../diagram-base.component';
import { AssetBrowserLayout } from './assetbrowserlayout.component';
import { MenuItem, SelectItem, TreeNode } from 'primeng/api';
import { setTimeout } from 'core-js';
import { AssetTypeClass } from '../../../../models/asset.model';
import { Observable } from 'rxjs';
import { PredicatesService } from '../../../../services/predicates.service';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { Router, ActivatedRoute } from '@angular/router';
import { SiteUrlHelpers } from '../../../../static/site-url-helpers';

declare var window: any;

@Component({
    selector: 'd3s-assetbrowser',
    templateUrl: './browser.component.html',
    providers: [BrowserService, PermissionsService, PredicatesService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserComponent extends DiagramBaseComponent implements OnInit, AfterViewInit, AfterViewChecked {
    @Input() readonly: boolean = true;
    @Input() assetUid: string;

    @ViewChild('addLineagePanel', { static: false }) addLineagePanelRef;
    @ViewChild('alertPanel', { static: false }) alertPanelRef;
    @ViewChild('infoDetailPanel', { static: false }) infoDetailPanelRef;
    @ViewChild('diagram', { static: false }) diagramRef;
    @ViewChild('filterDetailPanel', { static: false }) filterDetailPanelRef;
    DiagramObjectType = DiagramObjectType;

    private requestModel: AssetBrowserApiHopRequestModel;
    private responseModel: AssetBrowserModel = new AssetBrowserModel();
    private revealedKeys: string[] = [];
    private originalAssetUid: string;
    private menuItems: MenuItem[] = [];

    private alerts: AssetBrowserAlert[] = [];
    private assetsWithAlerts: string[] = [];
    private isAlertPanelLoading: boolean = false;
    private totalAlertCount: number = 0;

    private selectedDiagramAsset: AssetBrowserDiagramAsset;
    private isFullScreen: boolean = false;
    private isWindowLoading: boolean = false;
    private filtersLoading: boolean = false;
    private fromRefresh: boolean = false;
    private loadingText: string = '';
    private zoomText: string = '100%';

    //#endregion

    //#region Filters

    filterModel: AssetBrowserFilterModel = new AssetBrowserFilterModel();
    private readonly filterKey = 'asset-browser-filter';
    private storage = window.sessionStorage;

    selectedFilterAssetTypes: TreeNode[] = [];
    selectedFilterPredicates: TreeNode[] = [];
    selectedFilterResponsibilityTypes: TreeNode[] = [];
    filterSelectionsModel: FilterSelectionsModel = new FilterSelectionsModel([], [], []);

    savedFilters: StoredAssetBrowserFilterModel[] = [];
    selectedFilter: StoredAssetBrowserFilterModel;
    createUserFilter: StoredAssetBrowserFilterModel = new StoredAssetBrowserFilterModel();
    saveFilterModalVisible: boolean = false;
    saveFilterModalWorking: boolean = false;
    items: MenuItem[];

    //#endregion

    //#region Constants

    private readonly emptyUid: string = '00000000-0000-0000-0000-000000000000';
    private readonly fontContextMenu: string = "12px 'Source Sans Pro'";
    private readonly fontContextMenuShowDetails: string = "bold 12px 'Source Sans Pro'";

    private readonly fontOwnerBadge: string = "8pt 'Source Sans Pro'";
    private readonly fontOwnerBackColor: string = "#FEF6F2";
    private readonly fontOwnerBadgeLabelBorderColor: string = "#DE4B00";
    private readonly fontOwnerBadgeLabelBorderColor_Disabled: string = "#ebebeb";
    private readonly fontOwnerBadgeLabelBackColor: string = "#FFE5D0";
    private readonly fontOwnerBadgeLabelBackColor_Disabled: string = "#ebebeb";
    private readonly fontOwnerBadgeLabelForeColor: string = "#000000";
    private readonly fontOwnerBadgeCountBackColor: string = "#DE4B00";
    private readonly fontOwnerBadgeCountBackColor_Disabled: string = "#ebebeb";
    private readonly fontOwnerBadgeCountForeColor: string = "white";

    private readonly fontRelationBadge: string = "8pt 'Source Sans Pro'";
    private readonly fontRelationBadgeLabelBorderColor: string = "#A4AAAF";
    private readonly fontRelationBadgeLabelBorderColor_Disabled: string = "#ebebeb";
    private readonly fontRelationBadgeLabelBackColor: string = "#ffffff";
    private readonly fontRelationBadgeLabelBackColor_Disabled: string = "#ebebeb";
    private readonly fontRelationBadgeLabelForeColor: string = "#000000";
    private readonly fontRelationBadgeCountBackColor: string = "#A4AAAF";
    private readonly fontRelationBadgeCountBackColor_Disabled: string = "#ebebeb";
    private readonly fontRelationBadgeCountForeColor: string = "#ffffff";

    private readonly fontLabelIcon: string = "12px FontAwesome";
    private readonly fontLabelAlertColor: string = "#FF0000";
    private readonly fontLabel: string = "12px 'Source Sans Pro'";
    private readonly fontLabelColor: string = "#404040";
    private readonly fontLink: string = "9pt 'Source Sans Pro'";
    private readonly fontLinkColor: string = "#fff";
    private readonly linkBackColor: string = "#808080";
    private readonly lightenBoxColor: string = "#fff";
    private readonly darkenBoxColor: string = "#000";
    private readonly linkDefaultBackColor: string = '#808080';
    private readonly linkDefaultBorderColor: string = '#999';
    private readonly plusIcon: string = '\uf067';

    private readonly textMaxSize = new go.Size(200, Infinity);
    private readonly textMaxLines = 1;
    private readonly textOverflowStyle = go.TextBlock.OverflowEllipsis;

    private readonly searchHighlightColour: string = '#FFDA00';
    private readonly searchHighlightColourFocused: string = '#FD7E0E';
    private readonly selectionPathHighlightColor: string = '#F5C2FF';
    private readonly leafBackColor: string = 'transparent';

    //#endregion

    //#region Control Properties

    constructor(
        private route: ActivatedRoute,
        private myElement: ElementRef,
        private browserService: BrowserService,
        private router: Router,
        protected permissionsService: PermissionsService,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    public ngOnInit() {

        this.originalAssetUid = this.assetUid;

        //this.loadPermissions(this.permissionsService, this.objectType, this.objectID);

        this.menuItems.push(
            { icon: 'fa fa-search-minus', title: 'Zoom out' },
            { icon: 'fa fa-search-plus', title: 'Zoom in' },
            { icon: 'fa fa-refresh', title: 'Refresh' }
        );
        this.initializeDiagram();
        this.checkSecondaryNavLocalStorage();

        this.route.params.subscribe(
            params => {
                this.originalAssetUid = params['assetUid']; 
                this.refreshDiagram();
            }
        );
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
        this.cdRef.markForCheck();
    }

    public ngAfterViewChecked() {

        var panelHeaderElement: HTMLElement = this.myElement.nativeElement.querySelectorAll('.asset-browser-window-header')[0];
        var panelElements: HTMLElement[] = this.myElement.nativeElement.querySelectorAll('.asset-browser-window');

        (function () {
            if (typeof NodeList.prototype.forEach === "function") return false;
            panelElements.forEach = Array.prototype.forEach;
        })();
        var diagramSize = +this.diagramRef.nativeElement.style.height.replace('px', '');
        panelElements.forEach(el => {
            el.style.height = (diagramSize - 75) + 'px';
            el.style.maxHeight = (diagramSize - 75) + 'px';
            var panelHeaderSize = panelHeaderElement.clientHeight;

            let innerPanelHeight: string = (diagramSize - 75 - panelHeaderSize - 50) + 'px';
            if (this.addLineagePanelRef) {
                this.addLineagePanelRef.nativeElement.style.height = innerPanelHeight;
            }
            if (this.alertPanelRef) {
                this.alertPanelRef.nativeElement.style.height = innerPanelHeight;
            }
            if (this.filterDetailPanelRef) {
                this.filterDetailPanelRef.nativeElement.style.height = innerPanelHeight;
            }
            if (this.infoDetailPanelRef) {
                this.infoDetailPanelRef.nativeElement.style.height = innerPanelHeight;
            }
        });

    }

    public ngOnDestroy() {
        this.diagram.div = null;    // Garbage collection.
    }

    //#endregion

    //#region Panel Configuration

    private isAddRelationshipWindowVisible: boolean = false;

    private isAlertTabEnabled: boolean = true;
    private isAlertWindowVisible: boolean = false;

    private isInfoTabDisabled: boolean = true;
    private isInfoWindowVisible: boolean = false;

    private isFilterWindowVisible: boolean = false;

    private isSettingWindowVisible: boolean = false;

    private panelTabIndex: number = 0;

    private isWindowVisible(): boolean {
        return this.isAlertWindowVisible ||
            this.isAddRelationshipWindowVisible || 
            this.isFilterWindowVisible ||
            this.isInfoWindowVisible ||
            this.isSettingWindowVisible;
    }

    private switchToInfoDetailTab() {
        this.panelTabIndex = 0;
        this.cdRef.markForCheck();
    }

    private switchToOwnerDetailTab() {
        this.panelTabIndex = 1;
        this.cdRef.markForCheck();
    }

    //#endregion

    //#region button commands

    private savePngButtonClickCallback(image_data, assetUid) {
        var url = window.URL.createObjectURL(image_data);
        var filename = `${assetUid}.png`;
        var a = document.createElement("a");
        //a.style = "display: none";
        a.href = url;
        a.download = filename;
        // IE 11
        if (window.navigator.msSaveBlob !== undefined) {
            window.navigator.msSaveBlob(image_data, filename);
            return;
        }
        document.body.appendChild(a);
        requestAnimationFrame(function () {
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
        });
    }

    private savePngButtonClick(e) {
        let image_data = this.diagram.makeImageData({
            scale: 1,
            returnType: "blob",
            background: "#fff",
            callback: (image_data) => this.savePngButtonClickCallback(image_data, this.assetUid)
        });
    }

    private alertButtonClick(e) {
        this.panelButtonClick('alert');
        this.panelTabIndex = 0;
        if (this.selectedDiagramAsset) {
            this.showAlertsByAsset(this.selectedDiagramAsset.Uid);
        }
        else {
            this.showAlertsByDisplayedAssets();
        }
    }

    private onAlertOpenDetails(alert: AssetBrowserAlert) {
        this.alerts.forEach(a => {
            if (a.uid !== alert.uid) {
                a.selected = false;
            }
        });
        alert.selected = true;
        this.selectedDiagramAsset = new AssetBrowserDiagramAsset();
        this.selectedDiagramAsset.Uid = alert.asset.uid;
        this.selectedDiagramAsset.DisplayValue = alert.asset.displayValue;
        this.selectedDiagramAsset.Url = `/asset/${alert.asset.uid}`;
        this.showDetails(this.selectedDiagramAsset.Uid);
        this.isInfoWindowVisible = true;
        this.isAlertWindowVisible = false;
        this.panelTabIndex = 0;
    }

    private onAlertOpenInNewTab(alert: AssetBrowserAlert) {
        window.open(`/asset/${alert.asset.uid}`, "_blank");
    }

    private panelButtonClick(name: string) {
        switch (name) {
            case 'add':
                this.isAddRelationshipWindowVisible = !this.isAddRelationshipWindowVisible;
                this.isFilterWindowVisible = false;
                this.isAlertWindowVisible = false;
                this.isInfoWindowVisible = false;
                this.isSettingWindowVisible = false;
                break;
            case 'filter':
                this.isAddRelationshipWindowVisible = false;
                this.isFilterWindowVisible = !this.isFilterWindowVisible;
                this.isAlertWindowVisible = false;
                this.isInfoWindowVisible = false;
                this.isSettingWindowVisible = false;
                break;
            case 'alert':
                this.panelTabIndex = 0;
                this.isAlertTabEnabled = true;
                this.isAddRelationshipWindowVisible = false;
                this.isFilterWindowVisible = false;
                this.isAlertWindowVisible = !this.isAlertWindowVisible;
                this.isInfoWindowVisible = false;
                this.isSettingWindowVisible = false;
                break;
            case 'info':
                this.panelTabIndex = 0;
                this.isAddRelationshipWindowVisible = false;
                this.isFilterWindowVisible = false;
                this.isAlertWindowVisible = false;
                this.isInfoWindowVisible = !this.isInfoWindowVisible;
                this.isSettingWindowVisible = false;
                break;
            case 'settings':
                this.isAddRelationshipWindowVisible = false;
                this.isFilterWindowVisible = false;
                this.isAlertWindowVisible = false;
                this.isInfoWindowVisible = false;
                this.isSettingWindowVisible = !this.isSettingWindowVisible;
                break;
        }
    }

    private infoButtonClick(e) {
        this.panelButtonClick('info');

        if (this.isInfoWindowVisible && this.selectedDiagramAsset != null && this.selectedDiagramAsset.Loaded == false) {
            this.showDetails(this.selectedDiagramAsset.Uid);
            this.panelTabIndex = 0;
        }
    }

    private setFilterWindow(actOnFilterWindow: boolean) {
        let loadedTypes: LoadedFilterTypesModel = this.determineLoadedFilterOptions();

        //#region Asset Types

        this.filterSelectionsModel.FilterAssetTypes = [];
        this.filterSelectionsModel.AssetTypeOptions.forEach(at => {
            let inLoadedAssetTypes: boolean = loadedTypes.AssetTypes.findIndex(ix => { return ix == at.AssetTypeId }) > -1;
            if (inLoadedAssetTypes) {
                this.filterSelectionsModel.FilterAssetTypes.push({
                    label: at.Path,
                    data: at.AssetTypeId
                });
            } 
        });
        this.filterSelectionsModel.FilterAssetTypes.sort((a, b) => (a.label > b.label) ? 1 : -1);
        this.selectedFilterAssetTypes = this.getTreeNodeSelectionNodes(this.filterModel.SelectedAssetTypes, this.filterSelectionsModel.FilterAssetTypes);

        //#endregion

        //#region Predicates

        this.filterSelectionsModel.FilterPredicates = [];
        this.filterSelectionsModel.PredicateOptions.forEach(p => {
            let inLoadedPredicates: boolean = loadedTypes.Predicates.findIndex(ix => { return ix == p.Id }) > -1;
            if (inLoadedPredicates) {
                this.filterSelectionsModel.FilterPredicates.push({
                    label: p.Name.substring(0, 50) + ' / ' + p.Inverse.substring(0, 50),
                    data: p.Id
                });
            }
        });
        this.filterSelectionsModel.FilterPredicates.sort((a, b) => (a.label > b.label) ? 1 : -1);
        this.selectedFilterPredicates = this.getTreeNodeSelectionNodes(this.filterModel.SelectedPredicates, this.filterSelectionsModel.FilterPredicates);

        //#endregion

        //#region Responsibility Types

        this.filterSelectionsModel.FilterResponsibilityTypes = [];
        this.filterSelectionsModel.ResponsibilityTypeOptions.forEach(p => {

            let inLoadedResponsibilityTypes: boolean = loadedTypes.ResponsibilityTypes.findIndex(ix => { return ix == p.Id }) > -1;
            if (inLoadedResponsibilityTypes) {
                let thisResponsibilityTypeNode: TreeNode = {
                    label: p.Name,
                    data: p.Id,
                    children: []
                };
                this.filterSelectionsModel.FilterResponsibilityTypes.push(thisResponsibilityTypeNode);
            }

        });
        this.filterSelectionsModel.FilterResponsibilityTypes.sort((a, b) => (a.label > b.label) ? 1 : -1);
        this.selectedFilterResponsibilityTypes = this.getTreeNodeSelectionNodes(this.filterModel.SelectedResponsibilityTypes, this.filterSelectionsModel.FilterResponsibilityTypes);

        //#endregion

        if (actOnFilterWindow) {
            this.filtersLoading = false;
            this.panelButtonClick('filter');
        }
        this.cdRef.markForCheck();
    }

    private loadSavedFilters() {
        this.browserService
            .getUserFilters()
            .subscribe(filters => {
                this.savedFilters = filters;
                this.selectedFilter = filters.find(f => f.isDefault == true);
            });
    }

    private getFiltermenuItems(): MenuItem[] {
        return [
            { label: 'Save', disabled: !this.hasSelectedUserFilter(), command: (event) => { this.updateUserFilter() } },
            { label: 'Add', command: (event) => { this.addUserFilter() } },
            { label: 'Remove', disabled: !this.hasSelectedUserFilter(), command: (event) => { this.removeUserFilter() } }
        ];
    }

    private hasSelectedUserFilter(): boolean {
        return (this.selectedFilter != undefined && this.selectedFilter != null);
    }

    private applySavedFilter(e) {
        if (!this.hasSelectedUserFilter())
            return;

        var selectedAssetTypes = this.filterSelectionsModel.AssetTypeOptions
            .filter(a => this.selectedFilter.assetTypes.findIndex((f) => f.uid == a.Uid) > -1)
            .map((a) => a.AssetTypeId);

        var selectedResponsibilityTypes = this.filterSelectionsModel.ResponsibilityTypeOptions
            .filter(r => this.selectedFilter.responsibilityTypes.findIndex((f) => f.uid == r.Uid) > -1)
            .map((r) => r.Id);

        var selectedPredicates = this.filterSelectionsModel.PredicateOptions
            .filter(p => this.selectedFilter.predicates.findIndex((f) => f.uid == p.Uid) > -1)
            .map((p) => p.Id)

        this.selectedFilterAssetTypes = this.getTreeNodeSelectionNodes(selectedAssetTypes, this.filterSelectionsModel.FilterAssetTypes);
        this.filterAssetTypeChange({ value: this.selectedFilterAssetTypes });

        this.selectedFilterResponsibilityTypes = this.getTreeNodeSelectionNodes(selectedResponsibilityTypes, this.filterSelectionsModel.FilterResponsibilityTypes);
        this.filterResponsibilityTypeChange({ value: this.selectedFilterResponsibilityTypes });

        this.selectedFilterPredicates = this.getTreeNodeSelectionNodes(selectedPredicates, this.filterSelectionsModel.FilterPredicates);
        this.filterPredicateChange({ value: this.selectedFilterPredicates });

        if (this.selectedFilter.numberOfHops) {
            this.filterModel.NumberOfHops = this.selectedFilter.numberOfHops;
            this.filterNumberOfHopsChange();
        }

        if (this.selectedFilter.ancestryMode) {
            this.filterModel.AncestryMode = this.selectedFilter.ancestryMode;
            this.filterTriggerVisualizationUpdate();
        }
    }

    private addUserFilter() {
        this.saveFilterModalVisible = true;
        this.saveFilterModalWorking = false;
        this.createUserFilter = new StoredAssetBrowserFilterModel();
        this.createUserFilter.assetTypes = this.filterSelectionsModel.AssetTypeOptions
            .filter(a => this.filterModel.SelectedAssetTypes.indexOf(a.AssetTypeId) > -1)
            .map((a) => { return { uid: a.Uid, class: a.Class } });
        this.createUserFilter.responsibilityTypes = this.filterSelectionsModel.ResponsibilityTypeOptions
            .filter(r => this.filterModel.SelectedResponsibilityTypes.indexOf(r.Id) > -1)
            .map((r) => { return { uid: r.Uid, type: r.Name } });
        this.createUserFilter.predicates = this.filterSelectionsModel.PredicateOptions
            .filter(p => this.filterModel.SelectedPredicates.indexOf(p.Id) > -1)
            .map((p) => { return { uid: p.Uid, type: p.Name } });
        this.createUserFilter.ancestryMode = this.filterModel.AncestryMode;
        this.createUserFilter.numberOfHops = this.filterModel.NumberOfHops;
        this.createUserFilter.name = '';
    }

    private createUserFilterSave() {
        this.saveFilterModalWorking = true;
        this.browserService
            .saveUserFilter(this.createUserFilter)
            .subscribe(filter => {
                this.saveFilterModalVisible = false;
                this.saveFilterModalWorking = false;
                var filters = this.savedFilters;
                filters.push(filter);
                this.savedFilters = filters.filter(f => true);
                this.cdRef.markForCheck();
            });
    }

    private createUserFilterCancel() {
        this.saveFilterModalVisible = false;
    }

    private updateUserFilter() {
        if (!this.hasSelectedUserFilter())
            return;

        this.createUserFilter = JSON.parse(JSON.stringify(this.selectedFilter));
        this.createUserFilter.assetTypes = this.filterSelectionsModel.AssetTypeOptions
            .filter(a => this.filterModel.SelectedAssetTypes.indexOf(a.AssetTypeId) > -1)
            .map((a) => { return { uid: a.Uid, class: a.Class } });
        this.createUserFilter.responsibilityTypes = this.filterSelectionsModel.ResponsibilityTypeOptions
            .filter(r => this.filterModel.SelectedResponsibilityTypes.indexOf(r.Id) > -1)
            .map((r) => { return { uid: r.Uid, type: r.Name } });
        this.createUserFilter.predicates = this.filterSelectionsModel.PredicateOptions
            .filter(p => this.filterModel.SelectedPredicates.indexOf(p.Id) > -1)
            .map((p) => { return { uid: p.Uid, type: p.Name } });
        this.createUserFilter.ancestryMode = this.filterModel.AncestryMode;
        this.createUserFilter.numberOfHops = this.filterModel.NumberOfHops;

        this.browserService
            .saveUserFilter(this.createUserFilter)
            .subscribe(filter => {
                var filters = this.savedFilters;
                var idx = filters.findIndex(f => f.uid == filter.uid);
                filters[idx] = filter;
                this.savedFilters = filters.filter(f => true);
                this.selectedFilter = filter;
                this.cdRef.markForCheck();
            });
    }

    private removeUserFilter() {
        if (this.hasSelectedUserFilter()) {
            this.browserService
                .deleteUserFilter(this.selectedFilter)
                .subscribe(success => {
                    if (success) {
                        var filters = this.savedFilters;
                        var idx = filters.findIndex(f => f.uid == this.selectedFilter.uid);
                        filters.splice(idx,1);
                        this.savedFilters = filters.filter(f => true);
                        this.selectedFilter = undefined;
                        this.cdRef.markForCheck();
                    }
                });
        }
    }

    private filterButtonClick(e) {
        this.loadSavedFilters();
        if (this.filterSelectionsModel.AssetTypeOptions.length == 0) {
            this.filtersLoading = true;
            this.browserService
                .getFilterOptions()
                .subscribe(options => {
                    this.filterSelectionsModel = options;
                    this.setFilterWindow(true);
                });
        }
        else {
            this.setFilterWindow(true);
        }
    }

    private settingsButtonClick(e) {
        this.panelButtonClick('settings');
        this.cdRef.markForCheck();
    }

    private addRelationshipsClick(e) {
        this.panelButtonClick('add');
        this.cdRef.markForCheck();
    }

    private refreshButtonClick(e) {
        this.diagram.scale = 1;
        this.refreshDiagram();
        this.updateZoomText();
    }

    private zoomInButtonClick(e) {
        this.diagram.scale += .1;

        if (this.diagram.scale > 2.5) {
            this.diagram.scale = 2.5;
        }

        this.updateZoomText();
    }

    private zoomOutButtonClick(e) {
        this.diagram.scale -= .1;

        if (this.diagram.scale < .1) {
            this.diagram.scale = .1;
        }
        this.updateZoomText();
    }

    private fullScreenButtonClick(e) {
        this.isFullScreen = !this.isFullScreen;
        this.resizeDiagram();
        this.cdRef.markForCheck();
    }

    //#endregion

    //#region Diagram Switching

    private switchToImpactView(event) {
        this.filterModel.DiagramType = DiagramType.Impact;
        this.saveFilter();
        this.diagram.div = null;
        this.initializeDiagram();
    }

    private switchToLineageView(event) {
        this.filterModel.DiagramType = DiagramType.Lineage;
        this.saveFilter();
        this.diagram.div = null;
        this.initializeDiagram();
    }

    private impactViewButtonSelectedClass() {
        return (this.filterModel.DiagramType == DiagramType.Impact) ? "right-margin-4 selected" : "right-margin-4";
    }

    private lineageViewButtonSelectedClass() {
        return (this.filterModel.DiagramType == DiagramType.Lineage) ? "selected" : "";
    }

    private lineageDiagramApplies(): boolean {
        return (this.filterModel.DiagramType == DiagramType.Lineage);
    }

    //#endregion

    //#region helper methods

    private OwnershipTabEnabled() {
        let enabled: boolean = false;

        if (this.selectedDiagramAsset) {
            enabled = (this.selectedDiagramAsset.Owners.length > 0);
        }

        return enabled;
    }

    private addButtonSelectedClass() {
        let css: string = this.isAddRelationshipWindowVisible ? "selected" : "";
        if (!this.lineageDiagramApplies()) {
            css = "disabled";
        }
        return css;
    }

    private alertButtonClass() {
        let classes: string = "icon ";

        if (this.isAlertWindowVisible && this.panelTabIndex == 0) {
            classes += "selected";
        }
        if (!this.isAlertTabEnabled) {
            classes += "disabled";
        }

        return classes;
    }

    private alertButtonWidth() {
        let width: number = 32;
        if (this.totalAlertCount > 0) {
            width += (this.totalAlertCount.toLocaleString().length * 6);
            width += 10;
        }
        return width + 'px';
    }

    private alertCountClass() {
        return this.totalAlertCount > 0 ? "fa fa-bell has-alerts-label" : "fa fa-bell";
    }

    private alertCountNumberClass() {
        return this.totalAlertCount > 0 ? "has-alerts-count" : "";
    }

    private alertCountNumber() {
        return this.totalAlertCount > 0 ? this.totalAlertCount : ""; 
    }

    private determineLoadedFilterOptions(): LoadedFilterTypesModel {
        let model: LoadedFilterTypesModel = new LoadedFilterTypesModel();

        // Loop through nodes and figure out what is visible.
        this.diagram.model.nodeDataArray.forEach((tn: AssetBrowserTranslationNode) => {
            if (tn.assetTypeId) {
                let isRoot: boolean = tn.group == "" || tn.group == undefined;

                if (model.AssetTypes.findIndex(o => { return o == tn.assetTypeId }) == -1 && isRoot) {
                    model.AssetTypes.push(tn.assetTypeId);
                }
                if (tn.owners) {
                    tn.owners.forEach(r => {
                        if (model.ResponsibilityTypes.findIndex(o => { return o == r.responsibilityTypeId }) == -1) {
                            model.ResponsibilityTypes.push(r.responsibilityTypeId);
                        }
                    });
                }
                if (tn.relations) {
                    tn.relations.forEach(r => {
                        if (model.Predicates.findIndex(o => { return o == r.predicateId }) == -1) {
                            model.Predicates.push(r.predicateId);
                        }
                    });
                }
            }

            if (tn.responsibilityTypeId) {
                if (model.ResponsibilityTypes.findIndex(o => { return o == tn.responsibilityTypeId }) == -1) {
                    model.ResponsibilityTypes.push(tn.responsibilityTypeId);
                }
            }
        });

        // Now check the links for the predicates that are currently displayed.
        this.diagram.links.each(link => {
            let linkData: AssetBrowserTranslationLink = link.data as AssetBrowserTranslationLink;

            if (linkData.predicateIds) {
                linkData.predicateIds.forEach(r => {
                    if (model.Predicates.findIndex(o => { return o == r }) == -1) {
                        model.Predicates.push(r);
                    }
                });
            }
        });

        return model;
    }

    private filterButtonSelectedClass() {
        return "icon right-margin-4 " + (this.isFilterWindowVisible ? "selected" : "");
    }

    private infoButtonSelectedClass() {
        return "icon " + ((this.isInfoWindowVisible) ? "selected" : (this.isInfoTabDisabled ? "disabled" : ""));
    }

    private settingsButtonSelectedClass() {
        return "icon " + (this.isSettingWindowVisible ? "selected" : "");
    }

    private ownerRowClass(icon: string) {
        return "fa " + icon;
    }

    protected scoreBetween(value: number, start: number, end: number): boolean {
        if (value !== null && value !== undefined) {
            return +value >= start && +value <= end;
        }
        return false;
    }

    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch (err) {
            return "Error";
        }
    }

    private hideDeselectedAssetTypes(keysToBeConcernedWith: string[]) {
        // Now loop through selected asset types, as those are the ones we need to hide.
        let nodesToHide: AssetBrowserTranslationNode[] = [];
        this.diagram.model
            .nodeDataArray
            .filter((tn: AssetBrowserTranslationNode) => { return tn.template == "PortGroup" || tn.template == "HiddenData"; })
            .forEach((tn: AssetBrowserTranslationNode) => {
                if (this.filterModel.SelectedAssetTypes.findIndex(v => { return v == tn.assetTypeId; }) > -1) {
                    if (tn.template == "PortGroup") { //only hide if it is already displayed.
                        nodesToHide.push(tn);
                    }
                }
                else {
                    if (keysToBeConcernedWith) {
                        if (keysToBeConcernedWith.findIndex(ix => ix == tn.key) > -1) {
                            this.unhideNode(tn);
                        }
                    }
                    else {
                        this.unhideNode(tn);
                    }
                }
            });

        if (nodesToHide.length > 0) {
            nodesToHide.forEach(n => {
                let group: any = this.diagram.findNodeForKey(n.key);
                this.hideIndividualNode(n, group);
            });
        }
    }

    private hideDeselectedPredicates(keysToBeConcernedWith: string[]) {
        // Now loop through selected asset types, as those are the ones we need to hide.
        let nodesToHide: AssetBrowserTranslationNode[] = [];

        //#region Hide Badge

        this.diagram.startTransaction('predicateBadge');
        this.diagram.findTopLevelGroups().each(g => {
            let topLevelNode: AssetBrowserTranslationNode = g.data as AssetBrowserTranslationNode;

            let shallWeDealWithNode: boolean = false;
            if (keysToBeConcernedWith) {
                if (keysToBeConcernedWith.findIndex(ix => ix == g.key) > -1) {
                    shallWeDealWithNode = true;
                }
            }
            else {
                shallWeDealWithNode = true;
            }

            //#region Relations badge logic
            if (shallWeDealWithNode) {
                topLevelNode.relations.forEach(rC => {
                    let showBadge: boolean;

                    if (this.filterModel.SelectedPredicates.findIndex(v => { return v == rC.predicateId; }) > -1) {
                        showBadge = false;
                    }
                    else {
                        showBadge = true;
                    }

                    if (showBadge) {
                        // Check to see if we should ignore this predicate based on previouly revealed badges.
                        if (topLevelNode.ignoredPredicates.findIndex(v => { return v == rC.predicateUid; }) > -1) {
                            showBadge = false;
                        }
                    }

                    this.diagram.model.setDataProperty(rC, "showBadge", showBadge);
                });
            }
            //#endregion
        });
        this.diagram.commitTransaction('predicateBadge');

        //#endregion Badge

        //#region Hide Node

        this.diagram.links.each(link => {
            let linkData: AssetBrowserTranslationLink = link.data as AssetBrowserTranslationLink;
            if (linkData.predicateIds) {
                let g: any = this.diagram.findNodeForKey(linkData.to);
                if (g) {
                    if (linkData.predicateIds.filter(l => {
                        return this.filterModel.SelectedPredicates.findIndex(v => { return v == l; }) > -1
                    }).length > 0) {
                        this.hideIndividualNode(g.data as AssetBrowserTranslationNode, g);
                    }
                    else {
                        let shallWeDealWithNode: boolean = false;
                        if (keysToBeConcernedWith) {
                            if (keysToBeConcernedWith.findIndex(ix => ix == g.key) > -1) {
                                shallWeDealWithNode = true;
                            }
                        }
                        else {
                            shallWeDealWithNode = true;
                        }

                        if (shallWeDealWithNode) {
                            if (this.filterModel.SelectedAssetTypes.findIndex(v => { return v == (g.data as AssetBrowserTranslationNode).assetTypeId; }) == -1) {
                                this.unhideNode(g.data as AssetBrowserTranslationNode);
                            }
                        }
                    }
                }
            }
        });

        //#endregion
    }

    private hideDeselectedResponsibilityTypes(keysToBeConcernedWith: string[]) {

        //#region Hide Badge

        this.diagram.startTransaction('ownerBadge');
        this.diagram.findTopLevelGroups().each(g => {
            let topLevelNode: AssetBrowserTranslationNode = g.data as AssetBrowserTranslationNode;

            let shallWeDealWithNode: boolean = false;
            if (keysToBeConcernedWith) {
                if (keysToBeConcernedWith.findIndex(ix => ix == g.key) > -1) {
                    shallWeDealWithNode = true;
                }
            }
            else {
                shallWeDealWithNode = true;
            }

            //#region Owners badge logic
            if (shallWeDealWithNode) {
                topLevelNode.owners.forEach(rC => {
                    let showBadge: boolean = true;

                    if (this.filterModel.SelectedResponsibilityTypes.findIndex(v => { return v == rC.responsibilityTypeId; }) > -1) {
                        showBadge = false;
                    }
                    else {
                        showBadge = true;
                    }

                    this.diagram.model.setDataProperty(rC, "showBadge", showBadge);
                });
            }
            //#endregion
        });
        this.diagram.commitTransaction('ownerBadge');

        //#endregion Badge

        //#region Hide Node

        // Now loop through selected asset types, as those are the ones we need to hide.
        let nodesToHide: AssetBrowserTranslationNode[] = [];
        this.diagram.model
            .nodeDataArray
            .filter((tn: AssetBrowserTranslationNode) => { return tn.template == "Owners" || tn.template == "HiddenData"; })
            .forEach((tn: AssetBrowserTranslationNode) => {
                if (this.filterModel.SelectedResponsibilityTypes.findIndex(v => { return v == tn.responsibilityTypeId; }) > -1) {
                    if (tn.template == "Owners") { //only hide if it is already displayed.
                        nodesToHide.push(tn);
                    }
                }
                else {
                    let shallWeDealWithNode: boolean = false;
                    if (keysToBeConcernedWith) {
                        if (keysToBeConcernedWith.findIndex(ix => ix == tn.key) > -1) {
                            shallWeDealWithNode = true;
                        }
                    }
                    else {
                        shallWeDealWithNode = true;
                    }

                    if (shallWeDealWithNode) {
                        if (!(this.filterModel.SelectedAssetTypes.findIndex(v => { return v == tn.assetTypeId; }) > -1)) {
                            this.unhideNode(tn);
                        }
                    }
                }
            });

        if (nodesToHide.length > 0) {
            nodesToHide.forEach(n => {
                let group: any = this.diagram.findNodeForKey(n.key);
                this.hideIndividualNode(n, group);
            });
        }

        //#endregion
    }

    private hideIndividualNode(node: AssetBrowserTranslationNode, group: any) {
        this.diagram.startTransaction('hide');

        let hideNode = new AssetBrowserTranslationNode();

        hideNode.subgraph = new AssetBrowserTranslation();
        hideNode.template = "HiddenData";
        hideNode.assetTypeId = node.assetTypeId;
        hideNode.responsibilityTypeId = node.responsibilityTypeId;
        hideNode.back = node.back;
        hideNode.subgraph.nodes = [];
        hideNode.subgraph.links = [];
        hideNode.subgraph.nodes.push(node); //add this node to the subgraph so we can unhide it later

        let children = group.findSubGraphParts();
        children.each(c => {
            hideNode.subgraph.nodes.push(c.data);
        });

        this.diagram.model.addNodeData(hideNode);

        let upstreamLinks = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == group.key);
        let downstreamLinks = this.diagramModelAsGraph().linkDataArray.filter(l => l.from == group.key);

        upstreamLinks.forEach(l => {
            hideNode.subgraph.links.push(l);
            this.diagramModelAsGraph().removeLinkData(l);
            this.diagramModelAsGraph().addLinkData({ from: l.from, to: hideNode.key, predicateIds: l.predicateIds, expandedByBadgeKey: l.expandedByBadgeKey });
        });

        downstreamLinks.forEach(l => {
            hideNode.subgraph.links.push(l);
            this.diagramModelAsGraph().removeLinkData(l);
            this.diagramModelAsGraph().addLinkData({ from: hideNode.key, to: l.to, predicateIds: l.predicateIds, expandedByBadgeKey: l.expandedByBadgeKey });
        });

        this.diagram.remove(group);

        this.diagram.commitTransaction('hide');
    }

    private unhideNode(node: AssetBrowserTranslationNode) {
        if (node.template == "HiddenData") {
            this.diagram.startTransaction('unhide');

            let upstreamLinks = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == node.key);
            let downstreamLinks = this.diagramModelAsGraph().linkDataArray.filter(l => l.from == node.key);

            this.diagram.model.addNodeDataCollection(node.subgraph.nodes);
            this.diagramModelAsGraph().addLinkDataCollection(node.subgraph.links);

            this.diagramModelAsGraph().removeLinkDataCollection(upstreamLinks);
            this.diagramModelAsGraph().removeLinkDataCollection(downstreamLinks);

            this.diagram.model.removeNodeData(node);

            this.diagram.commitTransaction('unhide');
        }
    }

    private highlightPath(e: go.InputEvent, obj: go.Part) {
        try {
            //Set all to not highlighted.
            obj.diagram.nodes.each(n => {
                n.isHighlighted = false;
            });

            if (obj.key) {
                // Highlight the selected node.
                obj.isHighlighted = true;

                // Recurse through and highlight based on the atomic (non-grouped) links.
                this.highlightNodeImpacts(obj.key.toString(), AssetBrowserApiHopDirection.Both, undefined);
            }
            else {
                // You are clicking on a link instead.
                let link = this.diagram.findLinkForData(obj.data);
                if (obj.data) {
                    if (obj.data.impacts) {
                        let keysToHighlight: string[] = obj.data.impacts;
                        keysToHighlight.forEach(k => {
                            let node: go.Node = obj.diagram.findNodeForKey(k);
                            if (node) {
                                node.isHighlighted = true;
                            }
                        });
                    }
                }
            }
        } catch (e) {

        }
    }

    private highlightNodeImpacts(key: string, direction: AssetBrowserApiHopDirection, allRelations: AssetBrowserGenericRelationModel[]) {

        let fwd: boolean = ((direction == AssetBrowserApiHopDirection.Both) || (direction == AssetBrowserApiHopDirection.Forward));
        let bwd: boolean = ((direction == AssetBrowserApiHopDirection.Both) || (direction == AssetBrowserApiHopDirection.Backward));

        if (allRelations === undefined) {
            allRelations = new Array<AssetBrowserGenericRelationModel>();

            this.responseModel.assets.assetRelations.forEach(l => {
                allRelations.push({ from: l.subjectKey, to: l.objectKey });
            });
            this.responseModel.owners.ownerRelations.forEach(l => {
                allRelations.push({ from: l.ownerKey, to: l.assetKey });
            });
        }

        allRelations.forEach(l => {

            // Loop through the links to find ones where this node is subject, then traverse each one and do the same thing, recursively.
            if (fwd) {
                if (l.from == key) {
                    let oNode = this.diagram.findNodeForKey(l.to);
                    if (oNode) {
                        oNode.isHighlighted = true;
                        this.highlightNodeImpacts(l.to, AssetBrowserApiHopDirection.Forward, allRelations);
                    }
                }
            }

            // Loop through the links to find ones where this node is object, then traverse each one and do the same thing, recursively.
            if (bwd) {
                if (l.to == key) {
                    let sNode = this.diagram.findNodeForKey(l.from);
                    if (sNode) {
                        sNode.isHighlighted = true;
                        this.highlightNodeImpacts(l.from, AssetBrowserApiHopDirection.Backward, allRelations);
                    }
                }
            }
        });

    }

    private initializeDiagram() {
        this.initializeCustomShapes();

        this.loadFilter();

        this.diagram = this.createDiagram();

        var forelayer = this.diagram.findLayer("Foreground");
        this.diagram.addLayerBefore(this.g(go.Layer, { name: "Links" }), forelayer);

        this.diagram.groupTemplateMap.add("FocalPortGroup", this.createPortFocalGroupNode());
        this.diagram.groupTemplateMap.add("PortGroup", this.createPortGroupNode());
        this.diagram.groupTemplateMap.add("Group", this.createGroupNode());

        this.diagram.nodeTemplateMap.add("MoreData", this.createRevealNodeTemplate());
        this.diagram.nodeTemplateMap.add("HiddenData", this.createHiddenDataNode());

        this.diagram.groupTemplateMap.add("Owners", this.createOwnersGroup());
        this.diagram.nodeTemplateMap.add("Owner", this.createOwnerNode());
        this.diagram.nodeTemplate = this.createListItemNode();

        if (this.lineageDiagramApplies()) {
            this.diagram.linkTemplateMap.add("", this.createLineageLink());
        }
        else {
            this.diagram.linkTemplateMap.add("", this.createImpactLink());
        }

        this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.loadFilter();
        this.populateDiagram().subscribe(bComplete => {
            this.hideDeselectedAssetTypes(undefined);
            this.hideDeselectedPredicates(undefined);
            this.hideDeselectedResponsibilityTypes(undefined);
        });
    }

    private populateDiagram(): Observable<boolean> {
        let dgmObs: Observable<boolean>;

        dgmObs = new Observable(obs => {
            this.isLoading = true;
            this.loadingText = "Retrieving lineage from Govern..";
            this.responseModel.clear();
            this.revealedKeys = [];

            this.requestModel = new AssetBrowserApiHopRequestModel();
            this.requestModel.Assets = new Array();

            let assetRequestModel: AssetBrowserApiHopAssetRequestModel = new AssetBrowserApiHopAssetRequestModel();
            assetRequestModel.Uid = this.assetUid;
            this.requestModel.Assets.push(assetRequestModel);

            this.requestModel.Direction = AssetBrowserApiHopDirection.Both;
            this.requestModel.Hops = this.filterModel.NumberOfHops;
            this.requestModel.HopType = AssetBrowserApiHopType.Self; 

            let subscriber = (data: AssetBrowserAssetsModel) => {
                this.responseModel.assets.assets = data.assets;
                this.responseModel.assets.assetRelations = data.assetRelations;
                this.loadingText = "Determining links and meaning...";
                data = this.browserService.convertResponseModel(data, this.filterModel.AncestryMode);

                let trans: AssetBrowserTranslation = new AssetBrowserTranslation();
                trans.nodes = this.browserService.translateAssetNodes(this.filterModel.IncludeNonLeaf, data.assets);
                trans.links = this.browserService.translateAssetLinks(trans.nodes, data.assetRelations);

                this.parseData(trans);
                this.resizeDiagram();
                this.diagram.scale = 1;
                this.diagram.alignDocument(go.Spot.Center, go.Spot.Center);
                this.loadingText = "";
                this.isLoading = false;

                this.cdRef.markForCheck();

                obs.next(true);
                obs.complete();
            };

            if (this.lineageDiagramApplies()) {
                this.browserService.getAssetBrowserHop(this.requestModel).subscribe(subscriber);
            }
            else {
                this.browserService.getImpactBrowserHop(this.requestModel).subscribe(subscriber);
            }
        });

        return dgmObs;
    }

    private parseData(trans: AssetBrowserTranslation, append: boolean = false) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;

        //#region add data to diagram model

        trans.nodes.forEach(n => {
            n.showIcon = this.filterModel.DisplayIcons;
        });

        if (append) {

            trans.nodes.forEach(n => {
                n.showIcon = this.filterModel.DisplayIcons;
                let x = dm.findNodeDataForKey(n.key);
                if (x == null) {
                    //handle case where appended lineage reveals that a leaf node is
                    //now a parent of another node deeper in the hierarchy
                    if (n.group != null) {
                        let r = dm.findNodeDataForKey(n.group);
                        if (r != null) {
                            if (r.isGroup != true) {
                                dm.removeNodeData(r);
                                r.isGroup = true;
                                r.template = "Group"
                                dm.addNodeData(r);
                            }
                        }
                    }

                    dm.addNodeData(n);
                }
            });

            trans.links.forEach(l => {
                if (dm.linkDataArray.find(i => i.to == l.to && i.from == l.from) == null)
                    dm.addLinkData(l);
            });

        }
        else {
            dm.nodeDataArray = trans.nodes;
            dm.linkDataArray = trans.links;
        }

        //#endregion

        this.diagram.nodes.each(n => {
            n.isHighlighted = false;
        });

        //#region process dynamic elements like reveal nodes and relation badges

        this.diagram.findTopLevelGroups().each(g => {
            let children = g.findSubGraphParts();
            let childAssets: AssetBrowserApiHopAssetRequestModel[] = [];
            let childOwners = [];
            let childRelations = [];
            let backReveal: boolean = false;
            let forwardReveal: boolean = false;


            children.each(c => {

                let data: AssetBrowserTranslationNode = c.data;

                if (data.owners != null && data.owners.length > 0) {
                    for (let i = 0; i < data.owners.length; i++) {
                        let r = data.owners[i];
                        let rel = childOwners.find(c => c.responsibilityTypeId == r.responsibilityTypeId);
                        if (rel != null) {
                            rel.count += r.count;
                        }
                        else if (g.data.owners.find(c => c.responsibilityTypeId == r.responsibilityTypeId) == null) {
                            childOwners.push(r);
                        }
                    }
                    data.owners = [];
                }

                if (data.relations != null && data.relations.length > 0) {
                    for (let i = 0; i < data.relations.length; i++) {
                        let r = data.relations[i];
                        let rel = childRelations.find(c => c.predicateUid == r.predicateUid && c.direction == r.direction);
                        if (rel != null) {
                            rel.count += r.count;
                        }
                        else if (g.data.relations.find(c => c.predicateUid == r.predicateUid && c.direction == r.direction) == null) {
                            childRelations.push(r);
                        }
                    }
                    data.relations = [];
                }

                if (+data.showReveal !== +AssetBrowserApiHopDirection.None) {
                    if (+data.showReveal == +AssetBrowserApiHopDirection.Backward) {
                        backReveal = true; 
                        childAssets.push({ Uid: data.assetUid, Key: data.key });
                    }
                    if (+data.showReveal == +AssetBrowserApiHopDirection.Forward) {
                        forwardReveal = true;
                        childAssets.push({ Uid: data.assetUid, Key: data.key });
                    }
                }

            });


            g.data.owners = g.data.owners.concat(childOwners);
            this.diagram.model.setDataProperty(g.data, "owners", g.data.owners.slice());
            g.data.relations = g.data.relations.concat(childRelations);
            this.diagram.model.setDataProperty(g.data, "relations", g.data.relations.slice());
            this.diagram.model.setDataProperty(g.data, "showBadges", this.filterModel.DisplayBadges);

            if (backReveal) {
                if (dm.findNodeDataForKey(g.data.key + '_Backward') == null) {
                    dm.addNodeData({
                        template: 'MoreData',
                        key: g.data.key + '_Backward',
                        back: g.data.back,
                        showReveal: AssetBrowserApiHopDirection.Backward,
                        assetUid: g.data.assetUid,
                        assetUids: childAssets
                    });

                    dm.addLinkData({
                        from: g.data.key + '_Backward',
                        to: g.data.key
                    });
                }
            }

            if (forwardReveal) {
                if (dm.findNodeDataForKey(g.data.key + '_Forward') == null) {
                    dm.addNodeData({
                        template: 'MoreData',
                        key: g.data.key + '_Forward',
                        back: g.data.back,
                        showReveal: AssetBrowserApiHopDirection.Forward,
                        assetUid: g.data.assetUid,
                        assetUids: childAssets
                    });

                    dm.addLinkData({
                        from: g.data.key,
                        to: g.data.key + '_Forward'
                    });
                }
            }
        });

        //#endregion

        this.diagram.commitTransaction("load_all_data");
        this.reOrderLayout();

        this.recheckAlertCount();
    }

    private recheckAlertCount() {
        this.totalAlertCount = 0;
        this.assetsWithAlerts = [];
        this.diagram.nodes.each(n => {
            if (n.data) {
                if (n.data.actionCount) {
                    this.totalAlertCount += n.data.actionCount;
                    this.assetsWithAlerts.push(n.data.assetUid);
                }
            }
        });
        if (this.isAlertWindowVisible) {
            this.showAlertsByDisplayedAssets();
        }
    }

    /**
    * Convert the stored raw data set from the API while taking into account the ancestry setting.
    * @returns A collection of translated nodes.
    */
    private getFullResponseModelAsTranslationNodes(): AssetBrowserTranslationNode[] {
        let existingAssets = this.browserService.convertResponseModel(this.responseModel.assets, this.filterModel.AncestryMode);
        return this.browserService.translateAssetNodes(this.filterModel.IncludeNonLeaf, existingAssets.assets);
    }

    /**
    * Traverses an asset's hierarchy and sets each assets' reveal property to NONE.
    * @returns Nothing.
    */
    private setRevealKeyInHierarchy(models: AssetBrowserAssetModel[]) {
        models.forEach(t => {
            t.reveal = AssetBrowserApiHopDirection.None;
            if (t.items) {
                this.setRevealKeyInHierarchy(t.items);
            }
        });
    }

    /**
    * Takes a given asset key and searches for it within a collection of assets (each with its own hierarchy).
    * @returns The root asset that the given key is located within, regardless of level within ancestry.
    */
    private findTrueRootAssetInCollection(keyToFind: string, currentRoot: AssetBrowserAssetModel, currentParentToSearch: AssetBrowserAssetModel): AssetBrowserAssetModel {
        let foundRootAsset: AssetBrowserAssetModel;

        if (!currentRoot) {
            this.responseModel.assets.assets.forEach(a => {
                if (foundRootAsset == undefined) {
                    foundRootAsset = this.findTrueRootAssetInCollection(keyToFind, a, undefined);
                }
            });
        }
        else {
            if (currentRoot.key == keyToFind) {
                foundRootAsset = currentRoot;
            }
            else {
                if (currentParentToSearch) {
                    if (currentParentToSearch.key == keyToFind) {
                        foundRootAsset = currentRoot;
                    }
                    else {
                        if (currentParentToSearch.items) {
                            currentParentToSearch.items.forEach(i => {
                                if (foundRootAsset == undefined) {
                                    foundRootAsset = this.findTrueRootAssetInCollection(keyToFind, currentRoot, i);
                                }
                            });
                        }
                    }
                }
                else {
                    if (currentRoot.items) {
                        currentRoot.items.forEach(i => {
                            if (foundRootAsset == undefined) {
                                foundRootAsset = this.findTrueRootAssetInCollection(keyToFind, currentRoot, i);
                            }
                        });
                    }
                }
            }
        }
        return foundRootAsset;
    }

    /**
    * Based on the reveal node clicked, we determine the leaf asset that the raveal node is attached to, 
    * then get the next hop of lineage, whether backward or forward.
    * @returns Nothing
    */
    private revealLineageHop(e: go.InputEvent, obj: go.GraphObject) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let data = obj.part.data;
            let model = new AssetBrowserApiHopRequestModel();

            // This may be a top ancestor key OR a direct parent key.
            let currentTopGroupKey: string = data.key; 
            if (currentTopGroupKey.endsWith("_Backward")) {
                currentTopGroupKey = currentTopGroupKey.replace("_Backward", "");
                model.Direction = AssetBrowserApiHopDirection.Backward;
            }
            else if (currentTopGroupKey.endsWith("_Forward")) {
                currentTopGroupKey = currentTopGroupKey.replace("_Forward", "");
                model.Direction = AssetBrowserApiHopDirection.Forward;
            }
            this.revealedKeys.push(currentTopGroupKey);

            // Now we need to find the real root asset for this current key.
            let realRootAsset = this.findTrueRootAssetInCollection(currentTopGroupKey, undefined, undefined);

            model.Hops = 1;
            model.HopType = AssetBrowserApiHopType.Lineage;
            model.Assets = data.assetUids;

            this.browserService.getAssetBrowserHop(model)
                .subscribe(response => {

                    // Save a copy of the original return models so we can re-parse of filters or ancestry view changes.
                    response.assets.forEach(a => {
                        if (this.responseModel.assets.assets.find(r => r.key == a.key) == null) {
                            this.responseModel.assets.assets.push(a);
                        }
                    });

                    response.assetRelations.forEach(i => {
                        if (this.responseModel.assets.assetRelations.find(r => r.subjectKey == i.subjectKey && r.objectKey == i.objectKey) == null) {
                            this.responseModel.assets.assetRelations.push(i);
                        }
                    });

                    response = this.browserService.convertResponseModel(response, this.filterModel.AncestryMode);

                    let trans: AssetBrowserTranslation = new AssetBrowserTranslation();
                    trans.nodes = this.browserService.translateAssetNodes(this.filterModel.IncludeNonLeaf, response.assets);
                    trans.links = this.browserService.translateAssetLinks(this.getFullResponseModelAsTranslationNodes(), response.assetRelations);

                    let modelsToSetReveal: AssetBrowserAssetModel[] = [];
                    modelsToSetReveal.push(realRootAsset);
                    this.setRevealKeyInHierarchy(modelsToSetReveal);

                    this.parseData(trans, true);

                    // #region Remove the reveal node

                    this.diagram.startTransaction('reveal'); 

                    this.diagram.findTopLevelGroups().each(g => {
                        if (this.revealedKeys.findIndex(rk => { return g.key == rk; }) > -1) {

                            // Set the reveal value to None in the diagram's existing data model.
                            let children = g.findSubGraphParts();
                            children.each(c => {
                                this.diagram.model.setDataProperty(c.data, "showReveal", AssetBrowserApiHopDirection.None);
                            });

                        }
                    });

                    // Remove the link we just clicked on from the reveal node.
                    this.diagramModelAsGraph().removeNodeData(data);
                    let l = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == data.key || l.from == data.key);
                    this.diagramModelAsGraph().removeLinkDataCollection(l);

                    this.diagram.commitTransaction('reveal');

                    // #endregion

                    this.setFilterWindow(false);

                    this.hideDeselectedAssetTypes(undefined);
                    this.hideDeselectedPredicates(undefined);
                    this.hideDeselectedResponsibilityTypes(undefined);

                });
        }
    }

    private reOrderLayout() {
        this.diagram.layout.invalidateLayout();
        this.diagram.requestUpdate();
    }

    /**
    * Refreshes the data and diagram to its initially loaded state.
    * @returns Nothing
    */
    private refreshDiagram() {
        this.assetUid = this.originalAssetUid;
        this.fromRefresh = true;
        this.selectedDiagramAsset = null;
        this.isInfoWindowVisible = false;
        this.isInfoTabDisabled = true;
        this.populateDiagram().subscribe(bComplete => {
            this.fromRefresh = false;
            this.setFilterWindow(false);
            this.hideDeselectedAssetTypes(undefined);
            this.hideDeselectedPredicates(undefined);
            this.hideDeselectedResponsibilityTypes(undefined);
            this.showAlertsByDisplayedAssets();
        });
    }

    private findSubGraph(startKey: string, direction: AssetBrowserApiHopDirection): AssetBrowserTranslation {
        let subgraph = new AssetBrowserTranslation();

        subgraph.nodes = [];
        subgraph.links = [];

        let node = this.diagram.findNodeForKey(startKey);

        if (node != null) {
            let currentNodes = [];
            let nextLinks = [];
            let reverseLinks = [];
            let excludeStart = true;

            currentNodes.push(node.data);

            if (direction == AssetBrowserApiHopDirection.Forward || direction == AssetBrowserApiHopDirection.Both) {

                while (currentNodes.length > 0) {
                    nextLinks = [];
                    reverseLinks = [];

                    currentNodes.forEach(n => {
                        if (subgraph.nodes.find(s => s.key == n.key)) {
                            //already in the subgraph, skip
                        } else {
                            let l = this.diagramModelAsGraph().linkDataArray.filter(l => l.from == n.key);
                            let r = this.diagramModelAsGraph().linkDataArray.filter(r => r.to == n.key);
                            nextLinks = nextLinks.concat(l);

                            if (!(excludeStart && n.key == startKey)) {
                                subgraph.nodes.push(n);
                                reverseLinks = reverseLinks.concat(r);

                                if (n.isGroup) {
                                    let parts = (this.diagram.findNodeForData(n) as go.Group).findSubGraphParts();
                                    parts.each(p => {
                                        subgraph.nodes.push(p.data);
                                    });
                                }
                            }
                        }
                    });

                    currentNodes = [];
                    nextLinks.forEach(l => {
                        subgraph.links.push(l);
                        let nodes = this.diagram.model.nodeDataArray.filter(n => n.key == l.to);
                        nodes.forEach(n => {
                            if (subgraph.nodes.find(s => s.key == n.key) || (excludeStart && n.key == startKey)) {

                            } else {
                                currentNodes.push(n);
                            }
                        });
                    });

                    reverseLinks.forEach(r => {
                        subgraph.links.push(r);
                        let nodes = this.diagram.model.nodeDataArray.filter(n => n.key == r.from);
                        nodes.forEach(n => {
                            if (subgraph.nodes.find(s => s.key == n.key) || (excludeStart && n.key == startKey)) {

                            } else {
                                currentNodes.push(n);
                            }
                        });
                    });
                }

            }
            if (direction == AssetBrowserApiHopDirection.Backward || direction == AssetBrowserApiHopDirection.Both) {

                while (currentNodes.length > 0) {
                    nextLinks = [];
                    reverseLinks = [];
                    currentNodes.forEach(n => {
                        if (subgraph.nodes.find(s => s.key == n.key)) {
                            //already in the subgraph, skip
                        } else {
                            let l = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == n.key);
                            let r = this.diagramModelAsGraph().linkDataArray.filter(r => r.from == n.key);

                            nextLinks = nextLinks.concat(l);
                            if (!(excludeStart && n.key == startKey)) {
                                subgraph.nodes.push(n);
                                reverseLinks = reverseLinks.concat(r);

                                if (n.isGroup) {
                                    let parts = (this.diagram.findNodeForData(n) as go.Group).findSubGraphParts();
                                    parts.each(p => {
                                        subgraph.nodes.push(p.data);
                                    });
                                }
                            }
                        }
                    });

                    currentNodes = [];
                    nextLinks.forEach(l => {
                        subgraph.links.push(l);
                        let nodes = this.diagram.model.nodeDataArray.filter(n => n.key == l.from);
                        nodes.forEach(n => {
                            if (subgraph.nodes.find(s => s.key == n.key) || (excludeStart && n.key == startKey)) {

                            } else {
                                currentNodes.push(n);
                            }
                        });
                    });

                    reverseLinks.forEach(r => {
                        subgraph.links.push(r);
                        let nodes = this.diagram.model.nodeDataArray.filter(n => n.key == r.to);
                        nodes.forEach(n => {
                            if (subgraph.nodes.find(s => s.key == n.key) || (excludeStart && n.key == startKey)) {

                            } else {
                                currentNodes.push(n);
                            }
                        });
                    });
                }
            }
        }

        return subgraph;
    }

    //#endregion

    //#region session storage

    private saveState(key: string, data: any) {
        let dataString = JSON.stringify(data);
        this.storage.setItem(key, dataString);
    }

    private loadState(key: string): any {
        let dataString = this.storage.getItem(key);
        if (dataString) {
            return JSON.parse(dataString);
        }
        return null;
    }

    private saveFilter() {
        this.saveState(this.filterKey, this.filterModel);
    }

    private loadFilter() {
        let m = this.loadState(this.filterKey);
        if (m == null)
            this.filterModel = new AssetBrowserFilterModel();
        else
            this.filterModel = m;
    }

    //#endregion

    //#region events

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.resizeDiagram();
    }

    @HostListener('document:keydown', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        if (event.key === "Escape" || event.key === "Esc") {
            this.isFullScreen = false;
            this.resizeDiagram();
            this.cdRef.markForCheck();
        }
    }

    private resizeDiagram() {
        let height = window.innerHeight;
        if (this.isFullScreen)
            this.diagramRef.nativeElement.style.height = (height - 55) + 'px';
        else
            this.diagramRef.nativeElement.style.height = (height - 235) + 'px';

        this.disableDragging();
    }

    private disableDragging() {
        let unlockedKeys: string[] = [];

        this.diagram.links.each(function (l) {
            if (!unlockedKeys.some(x => x == l.fromNode.data.key))
                unlockedKeys.push(l.fromNode.data.key);

            if (!unlockedKeys.some(x => x == l.toNode.data.key))
                unlockedKeys.push(l.toNode.data.key);
        });

        this.diagram.nodes.each(function (n) {
            if (!n.data.isGroup) {
                n.movable = false;
            }
            else if (!unlockedKeys.some(x => x == n.data.key)) {
                n.movable = false;
            }
        });
    }

    private updateZoomText() {
        this.zoomText = Math.round(this.diagram.scale * 100) + '%';
        this.cdRef.markForCheck();
    }

    private onMouseEnterNode(e: any, node: go.Node) {
        node.isShadowed = true;
    }

    private onMouseLeaveNode(e: any, node: go.Node) {
        node.isShadowed = false;
    }

    private zoomDiagram(v: number) {
        this.diagram.scale = v;
    }

    private ChangedSelection(e: go.DiagramEvent) {
        if (e != null && e.subject != null) {
            if (e.subject instanceof go.Set) {
                let parts = (e.subject as go.Set<go.Part>);

                if (parts.count == 1) {
                    let data = parts.first().data;
                    let uid: string = '';

                    if (data.assetUid != null && data.assetUid != this.emptyUid) {
                        // selected item is an asset
                        uid = data.assetUid;
                    }

                    if (uid !== '' && uid != this.emptyUid) {
                        this.isInfoTabDisabled = false;
                        if (this.selectedDiagramAsset == null || this.selectedDiagramAsset.Uid != uid) {
                            //this.isInfoWindowVisible = false;
                            if (this.isAlertWindowVisible) {
                                this.showAlertsByAsset(uid);
                            }
                            else {
                                this.selectedDiagramAsset = new AssetBrowserDiagramAsset();
                                this.selectedDiagramAsset.Uid = uid;
                                this.showDetails(uid);
                                this.cdRef.markForCheck();
                            }
                        }
                    }
                    else {
                        this.diagram.nodes.each(n => {
                            n.isHighlighted = false;
                        });
                        this.selectedDiagramAsset = null;
                        this.isInfoTabDisabled = true;
                        this.isInfoWindowVisible = false;
                        if (this.isAlertWindowVisible) {
                            this.showAlertsByDisplayedAssets();
                        }
                        this.cdRef.markForCheck();
                    }

                } else if (parts.count == 0) {
                    this.diagram.nodes.each(n => {
                        n.isHighlighted = false;
                    });
                    this.selectedDiagramAsset = null;
                    this.isInfoTabDisabled = true;
                    this.panelTabIndex = 0;
                    this.isInfoWindowVisible = false;
                    if (this.isAlertWindowVisible) {
                        this.showAlertsByDisplayedAssets();
                    }
                    this.cdRef.markForCheck();
                }
            }
        }
    }

    private filterTriggerVisualizationUpdate(): void {
        this.saveFilter();
        this.isLoading = true;
        this.loadingText = "Determining links and meaning...";
        let assetData = this.browserService.convertResponseModel(this.responseModel.assets, this.filterModel.AncestryMode);

        let trans: AssetBrowserTranslation = new AssetBrowserTranslation();
        trans.nodes = this.browserService.translateAssetNodes(this.filterModel.IncludeNonLeaf, assetData.assets);
        trans.links = this.browserService.translateAssetLinks(trans.nodes, assetData.assetRelations);

        this.parseData(trans);

        this.resizeDiagram();
        this.diagram.zoomToFit();
        this.diagram.alignDocument(go.Spot.Center, go.Spot.Center);
        this.loadingText = "";
        this.isLoading = false;
        this.fromRefresh = false;

        this.setFilterWindow(false);

        this.cdRef.markForCheck();

        this.hideDeselectedAssetTypes(undefined);
        this.hideDeselectedPredicates(undefined);
        this.hideDeselectedResponsibilityTypes(undefined);
    }

    private filterBadgesChange(): void {
        this.diagram.startTransaction();
        this.diagram.findTopLevelGroups().each(g => {
            this.diagram.model.setDataProperty(g.data, "showBadges", this.filterModel.DisplayBadges);
        });
        this.saveFilter();
        this.diagram.commitTransaction();
    }

    private filterDisplayAncestorBadgesChange(): void {
        this.refreshDiagram();
    }

    private filterDisplayIconsChange(): void {
        this.diagram.startTransaction();
        this.diagram.model.nodeDataArray.forEach(d => {
            this.diagram.model.setDataProperty(d, "showIcon", this.filterModel.DisplayIcons);
        });
        this.saveFilter();
        this.diagram.commitTransaction();
    }

    private filterAssetTypeChange(e) {
        this.filterModel.SelectedAssetTypes = this.getTreeNodeSelectionKeys(e.value);
        this.saveFilter();
        this.hideDeselectedAssetTypes(undefined);
    }

    private filterNumberOfHopsChange() {
        this.saveFilter();
        this.diagram.scale = 1;
        this.refreshDiagram();
        this.updateZoomText();
    }

    private filterPredicateChange(e) {
        this.filterModel.SelectedPredicates = this.getTreeNodeSelectionKeys(e.value);
        this.saveFilter();
        this.hideDeselectedPredicates(undefined);
    }

    private filterResponsibilityTypeChange(e) {
        this.filterModel.SelectedResponsibilityTypes = this.getTreeNodeSelectionKeys(e.value);
        this.saveFilter();
        this.hideDeselectedResponsibilityTypes(undefined);
    }

    private getTreeNodeSelectionNodes(keys: number[], source: TreeNode[]) {
        let nodes: TreeNode[] = [];
        source.forEach(s => {
            if (keys.indexOf(s.data) != -1) {
                nodes.push(s);
            }
            if (s.children != null && s.children.length > 0) {
                let childNodes = this.getTreeNodeSelectionNodes(keys, s.children);
                if (childNodes != null && childNodes.length > 0) {
                    nodes = nodes.concat(childNodes);
                }
            }
        });

        return nodes;
    }

    private getTreeNodeSelectionKeys(selection: TreeNode[]): number[] {
        let keys: number[] = [];

        selection.forEach(s => {
            keys.push(+s.data);
        });

        return keys;
    }

    //#endregion

    //#region Context menu actions

    private showAlertsByDisplayedAssets() {
        if (this.assetsWithAlerts.length > 0) {
            this.isAlertPanelLoading = true;

            let model: AssetBrowserAlertRequest = new AssetBrowserAlertRequest();

            this.assetsWithAlerts.forEach(a => {
                model.assets.push({ uid: a });
            });
            this.browserService.getAlertsByAsset(model).subscribe(alerts => {
                if (alerts) {
                    this.alerts = alerts;
                    this.isAlertTabEnabled = (alerts.length > 0);
                }
                else {
                    this.alerts = [];
                    this.isAlertWindowVisible = false;
                    this.isAlertTabEnabled = false;
                }
                this.isAlertPanelLoading = false;
                this.cdRef.markForCheck();
            });
        }
        else {
            this.isAlertWindowVisible = false;
            this.isAlertTabEnabled = false;
        }
    }

    private showAlertsByAsset(assetUid: string) {
        let model: AssetBrowserAlertRequest = new AssetBrowserAlertRequest();
        model.assets.push({ uid: assetUid });

        this.isAlertPanelLoading = true;
        this.browserService.getAlertsByAsset(model).subscribe(alerts => {
            if (alerts) {
                this.alerts = alerts;
                this.isAlertTabEnabled = (alerts.length > 0);
            }
            else {
                this.alerts = [];
                this.isAlertTabEnabled = false;
            }
            this.isAlertPanelLoading = false;
            this.cdRef.markForCheck();
        });
    }

    private showDetails(assetUid: string) {
        this.isWindowLoading = true;
        this.browserService.getDetailByAsset(assetUid).subscribe(response => {
            this.selectedDiagramAsset = response;
            this.selectedDiagramAsset.Loaded = true;
            this.selectedDiagramAsset.Url = "/" + this.selectedDiagramAsset.Url;
            this.isWindowLoading = false;
            this.panelTabIndex = 0;
            this.cdRef.markForCheck();
        });
    }

    private hide(e, obj, direction: AssetBrowserApiHopDirection = null) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: AssetBrowserTranslationNode = obj.part.data;

            if (node.group != null) { //find top level node
                let n: any = this.diagram.findNodeForKey(node.group).data;
                while (n.group != null) {
                    n = this.diagram.findNodeForKey(n.group).data;
                }
                node = n;
            }

            if (node.isGroup) { //top level item

                let group: any = this.diagram.findNodeForKey(node.key);

                if (direction == null) { //hide the current node
                    this.hideIndividualNode(node, group);
                }
                else { //hide upstream or downstream
                    let subgraph = this.findSubGraph(group.key, direction);

                    if (subgraph == null || subgraph.nodes.length < 1)
                        return; //nothing to hide
                    if (subgraph.nodes.length == 1 && subgraph.nodes[0].template == "HiddenData")
                        return; //subgraph already hidden

                    this.diagram.startTransaction('hide');

                    let hideNode = new AssetBrowserTranslationNode();

                    hideNode.subgraph = subgraph;
                    hideNode.template = "HiddenData";
                    hideNode.back = node.back;

                    this.diagramModelAsGraph().removeLinkDataCollection(subgraph.links);
                    this.diagram.model.removeNodeDataCollection(subgraph.nodes);

                    this.diagram.model.addNodeData(hideNode);
                    if (direction == AssetBrowserApiHopDirection.Forward)
                        this.diagramModelAsGraph().addLinkData({ from: group.key, to: hideNode.key });
                    else
                        this.diagramModelAsGraph().addLinkData({ from: hideNode.key, to: group.key });

                    this.diagram.commitTransaction('hide');
                }

            }
        }
    }

    private unhide(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: AssetBrowserTranslationNode = obj.part.data;
            this.unhideNode(node);
        }
    }

    private collapseNodesAndLinks(dm: go.GraphLinksModel, key: string, links: go.Iterator<go.Link>) {
        if (links) {
            let lnks: any[] = [];
            links.iterator.each(link => {
                lnks.push({ link: link, node: (link.toNode.key == key) ? link.fromNode : link.toNode });
            });
            lnks.forEach(lnk => {
                if (lnk.node) {
                    let backLinks: go.Iterator<go.Link> = lnk.node.findLinksInto().filter(b => { return (b.fromNode.key !== key); });
                    this.collapseNodesAndLinks(dm, lnk.node.key, backLinks);

                    let forwardLinks: go.Iterator<go.Link> = lnk.node.findLinksOutOf().filter(b => { return (b.toNode.key !== key); });
                    this.collapseNodesAndLinks(dm, lnk.node.key, forwardLinks);

                    // Remove immediate child.
                    this.diagram.remove(lnk.node);
                    dm.removeNodeData(dm.findNodeDataForKey(lnk.node.key));
                }

                this.diagram.remove(lnk.link);
            });
        }
    }

    private collapseBadgeDependentNodesAndLinks(badgeKey: string, nodeKey: string) {
        this.diagram.startTransaction("collapseBadge");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        var links = this.diagram.links.filter(l => l.data.expandedByBadgeKey == badgeKey);
        this.collapseNodesAndLinks(dm, nodeKey, links);
        this.diagram.commitTransaction("collapseBadge");
    }

    private clickOwnerBadge(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let ix = obj.itemIndex;
            let node: AssetBrowserTranslationNode = obj.part.data;
            let owner: AssetBrowserTranslationOwnerCount = node.owners[ix];

            if (owner.expanded) {
                this.collapseBadgeDependentNodesAndLinks(owner.key, node.key);
                owner.expanded = false;
                this.diagram.model.removeArrayItem(node.owners, ix);
                this.diagram.model.insertArrayItem(node.owners, ix, owner);
                this.recheckAlertCount();
                this.cdRef.markForCheck();
            }
            else {
                let requestModel: AssetBrowserApiOwnerHopRequestModel = new AssetBrowserApiOwnerHopRequestModel();

                requestModel.Assets = [];
                requestModel.ResponsibilityTypeId = owner.responsibilityTypeId;

                let n = node;
                if (n.isGroup) {
                    // Add the root node's asset information.
                    if (this.filterModel.IncludeNonLeaf && node.assetUid !== this.emptyUid) {
                        requestModel.Assets.push({ Uid: node.assetUid, Key: node.key });
                    }
                    

                    (this.diagram.findNodeForData(n) as go.Group).findSubGraphParts().each(g => {
                        let shouldInclude: boolean = this.filterModel.IncludeNonLeaf ? true : (g.data.isGroup == undefined || g.data.isGroup == false);
                        if (shouldInclude && g.data.assetUid !== this.emptyUid) {
                            let asset = new AssetBrowserApiHopAssetRequestModel();
                            asset.Uid = g.data.assetUid;
                            asset.Key = g.data.key
                            requestModel.Assets.push(asset);
                        }
                    })
                }

                this.browserService.getAssetOwners(requestModel)
                    .subscribe(response => {

                        // Some extra data you will need later on during translation.
                        response.fromKey = node.key;
                        response.responsibilityType = owner.responsibilityType;
                        response.responsibilityTypeId = owner.responsibilityTypeId;

                        response.owners.forEach(o => {
                            this.responseModel.owners.owners.push(o);
                        });
                        response.ownerRelations.forEach(r => {
                            this.responseModel.owners.ownerRelations.push(r);
                        });

                        let trans: AssetBrowserTranslation = this.browserService.translateOwnersResponseModel(response);
                        trans.links.forEach(l => {
                            l.expandedByBadgeKey = owner.key;
                        });
                        owner.expanded = true;

                        this.parseData(trans, true);

                        this.setFilterWindow(false);
                    });
            }
        }
    }

    private clickRelationBadge(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let existingIgnoredPredicates: string[] = new Array();
            let ix = obj.itemIndex;
            let node: AssetBrowserTranslationNode = obj.part.data;
            let relation: AssetBrowserTranslationRelationCount = node.relations[ix];

            if (relation.expanded) {
                this.collapseBadgeDependentNodesAndLinks(relation.key, node.key);
                relation.expanded = false;
                this.diagram.model.removeArrayItem(node.relations, ix);
                this.diagram.model.insertArrayItem(node.relations, ix, relation);
                this.recheckAlertCount();
                this.cdRef.markForCheck();
            }
            else {

                let requestModel: AssetBrowserApiHopRequestModel = new AssetBrowserApiHopRequestModel();

                requestModel.Assets = [];
                requestModel.PredicateUid = relation.predicateUid;
                requestModel.Direction = relation.direction;
                requestModel.HopType = AssetBrowserApiHopType.Impact;

                let n = node;
                if (n.isGroup) {
                    // Add the root node's asset information.
                    if (this.filterModel.IncludeNonLeaf && node.assetUid !== this.emptyUid) {
                        requestModel.Assets.push({ Uid: node.assetUid, Key: node.key });
                    }
                    (this.diagram.findNodeForData(n) as go.Group).findSubGraphParts().each(g => {
                        let shouldInclude: boolean = this.filterModel.IncludeNonLeaf ? true : (g.data.isGroup == undefined || g.data.isGroup == false);
                        if (shouldInclude && g.data.assetUid !== this.emptyUid) {

                            // Get existing ignored predicates so we can continue to skip these along the impact chain.
                            if (g.data.ignoredPredicates !== undefined) {
                                g.data.ignoredPredicates.forEach(p => {
                                    if (existingIgnoredPredicates.findIndex(pix => p == pix) === -1) {
                                        existingIgnoredPredicates.push(p);
                                    }
                                });
                            }

                            let asset = new AssetBrowserApiHopAssetRequestModel();
                            asset.Uid = g.data.assetUid;
                            asset.Key = g.data.key
                            requestModel.Assets.push(asset);
                        }
                    })
                }

                let subscriber = (response: AssetBrowserAssetsModel) => {
                    response.assets.forEach(a => {
                        this.responseModel.assets.assets.push(a);
                    });
                    response.assetRelations.forEach(i => {
                        this.responseModel.assets.assetRelations.push(i);
                    });

                    let nodeToPull = this.findInApiModel(node.key, this.responseModel.assets);
                    if (nodeToPull) {
                        response.assets.push(nodeToPull);
                    }

                    response = this.browserService.convertResponseModel(response, this.filterModel.AncestryMode);

                    let keysToBeConcernedWith: string[] = [];
                    let nodes = this.browserService.translateAssetNodes(this.filterModel.IncludeNonLeaf, response.assets);
                    nodes.forEach(n => {

                        keysToBeConcernedWith.push(n.key);

                        // Transfer ignored predicates to the newly created nodes.
                        if (this.lineageDiagramApplies()) {
                            existingIgnoredPredicates.forEach(ep => {
                                n.ignoredPredicates.push(ep);
                            });
                            n.ignoredPredicates.push(relation.predicateUid);
                        }
                    });

                    let trans: AssetBrowserTranslation = new AssetBrowserTranslation();
                    trans.nodes = nodes;
                    trans.links = this.browserService.translateAssetLinks(this.getFullResponseModelAsTranslationNodes(), response.assetRelations);

                    trans.links.forEach(l => {
                        l.expandedByBadgeKey = relation.key;
                    });

                    relation.expanded = true;
                    this.parseData(trans, true);

                    this.setFilterWindow(false);

                    this.hideDeselectedAssetTypes(keysToBeConcernedWith);
                    this.hideDeselectedPredicates(keysToBeConcernedWith);
                    this.hideDeselectedResponsibilityTypes(keysToBeConcernedWith);
                };

                if (this.lineageDiagramApplies()) {
                    this.browserService.getAssetBrowserHop(requestModel).subscribe(subscriber);
                }
                else {
                    this.browserService.getImpactBrowserHop(requestModel).subscribe(subscriber);
                }
            }
        }
    }

    private findInApiModel(key: string, model: AssetBrowserAssetsModel): AssetBrowserAssetModel {
        let found: AssetBrowserAssetModel;

        model.assets.forEach(root => {
            if (!found) {
                if (this.findInApiItemModel(key, root)) {
                    found = root;
                }
            }
        });

        return found;
    }

    private findInApiItemModel(key: string, model: AssetBrowserAssetModel): boolean {
        let found: boolean = false;

        if (model.key == key) {
            found = true;
        }
        else {
            model.items.forEach(child => {
                if (!found) {
                    if (child.key == key) {
                        found = true;
                    }
                    else {
                        if (child.items) {
                            found = this.findInApiItemModel(key, child);
                        }
                    }
                }
            });
        }

        return found;
    }

    //#endregion

    //#region Templates

    private initializeCustomShapes() {
        go.Shape.defineFigureGenerator("RoundedRectLeft", (shape, w, h) => {
            let p1 = 5;
            if (shape !== null) {
                var param1 = shape.parameter1;
                if (!isNaN(param1) && param1 >= 0) p1 = param1;
            }
            p1 = Math.min(p1, w / 2);
            p1 = Math.min(p1, h / 2);
            let geo = new go.Geometry();

            geo.add(new go.PathFigure(0, p1)
                .add(new go.PathSegment(go.PathSegment.Arc, 180, 90, p1, p1, p1, p1))
                .add(new go.PathSegment(go.PathSegment.Line, w, 0))
                .add(new go.PathSegment(go.PathSegment.Line, w, h))
                .add(new go.PathSegment(go.PathSegment.Line, p1, h))
                .add(new go.PathSegment(go.PathSegment.Arc, 90, 90, p1, h - p1, p1, p1).close()));

            geo.spot1 = new go.Spot(0, 0, 0.3 * p1, 0.3 * p1);
            geo.spot2 = new go.Spot(1, 1, -0.3 * p1, 0);
            return geo;
        });

        go.Shape.defineFigureGenerator("RoundedRectRight", (shape, w, h) => {
            let p1 = 5;
            if (shape !== null) {
                var param1 = shape.parameter1;
                if (!isNaN(param1) && param1 >= 0) p1 = param1;
            }
            p1 = Math.min(p1, w / 2);
            p1 = Math.min(p1, h / 2);
            let geo = new go.Geometry();


            geo.add(new go.PathFigure(0, 0)
                .add(new go.PathSegment(go.PathSegment.Line, w - p1, 0))
                .add(new go.PathSegment(go.PathSegment.Arc, 270, 90, w - p1, p1, p1, p1))
                .add(new go.PathSegment(go.PathSegment.Line, w, h - p1))
                .add(new go.PathSegment(go.PathSegment.Arc, 0, 90, w - p1, h - p1, p1, p1))
                .add(new go.PathSegment(go.PathSegment.Line, 0, h).close()));


            geo.spot1 = new go.Spot(0, 0, 0.3 * p1, 0.3 * p1);
            geo.spot2 = new go.Spot(1, 1, -0.3 * p1, 0);
            return geo;
        });
    }

    private createOwnersBadge(): go.Panel {
        return this.g(go.Panel, "TableRow", {
            alignment: go.Spot.TopCenter,
            alignmentFocus: go.Spot.Bottom,
            padding: 0,
            cursor: "pointer",
            click: (e, obj) => this.clickOwnerBadge(e, obj),
        },
            this.g(go.Panel, "Horizontal",
                new go.Binding("visible", "showBadge"),
                { alignment: go.Spot.Center },
                this.g(go.Panel, "Auto",
                    this.g(go.Shape,
                        { figure: "RoundedRectLeft", parameter1: 2, strokeWidth: 0.5 },
                        new go.Binding("stroke", "expanded", (h) => (h ? this.fontOwnerBadgeLabelBorderColor_Disabled : this.fontOwnerBadgeLabelBorderColor)),
                        new go.Binding("fill", "expanded", (h) => (h ? this.fontOwnerBadgeLabelBackColor_Disabled : this.fontOwnerBadgeLabelBackColor)),
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 2,
                            alignment: go.Spot.Left,
                            editable: false,
                            font: this.fontOwnerBadge,
                            stroke: this.fontOwnerBadgeLabelForeColor
                        },
                        new go.Binding("text", "responsibilityType")
                    ),
                ),
                this.g(go.Panel, "Auto",
                    this.g(go.Shape, "RoundedRectRight",
                        { parameter1: 2, strokeWidth: 1 },
                        new go.Binding("stroke", "expanded", (h) => (h ? this.fontOwnerBadgeCountBackColor_Disabled : this.fontOwnerBadgeCountBackColor)),
                        new go.Binding("fill", "expanded", (h) => (h ? this.fontOwnerBadgeCountBackColor_Disabled : this.fontOwnerBadgeCountBackColor)),
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 2,
                            alignment: go.Spot.Center,
                            editable: false,
                            font: this.fontOwnerBadge,
                            stroke: this.fontOwnerBadgeCountForeColor
                        },
                        new go.Binding("text", "count")
                    ),
                )
            )
        );
    }

    private createRelationsBadge(): go.Panel {
        return this.g(go.Panel, "TableRow", {
            alignment: go.Spot.TopCenter,
            alignmentFocus: go.Spot.Bottom,
            padding: 0,
            cursor: "pointer",
            click: (e, obj) => this.clickRelationBadge(e, obj),
        },
            this.g(go.Panel, "Horizontal",
                new go.Binding("visible", "showBadge"),
                { alignment: go.Spot.Center },
                this.g(go.Panel, "Auto",
                    this.g(go.Shape,
                        { figure: "RoundedRectLeft", parameter1: 2, strokeWidth: 0.5 },
                        new go.Binding("stroke", "expanded", (h) => (h ? this.fontRelationBadgeLabelBorderColor_Disabled : this.fontRelationBadgeLabelBorderColor)),
                        new go.Binding("fill", "expanded", (h) => (h ? this.fontRelationBadgeLabelBackColor_Disabled : this.fontRelationBadgeLabelBackColor)),
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 2,
                            alignment: go.Spot.Left,
                            editable: false,
                            font: this.fontRelationBadge,
                            stroke: this.fontRelationBadgeLabelForeColor
                        },
                        new go.Binding("text", "predicate")
                    ),
                ),
                this.g(go.Panel, "Auto",
                    this.g(go.Shape, "RoundedRectRight",
                        { parameter1: 2, stroke: this.fontRelationBadgeCountForeColor, strokeWidth: 1 },
                        new go.Binding("fill", "expanded", (h) => (h ? this.fontRelationBadgeCountBackColor_Disabled : this.fontRelationBadgeCountBackColor)),
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 2,
                            alignment: go.Spot.Center,
                            editable: false,
                            font: this.fontRelationBadge,
                            stroke: this.fontRelationBadgeCountForeColor
                        },
                        new go.Binding("text", "count")
                    ),
                )
            )
        );
    }

    private createContextMenu(): go.Adornment {
        return this.g(
            "ContextMenu",
            { areaBackground: "#ffffff", background: "#ffffff" },
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Navigate to", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.navigateTo(e, obj) },
                new go.Binding("visible", "", function (o) {
                    return o.part.data.hasAssetReadAccess;
                }).ofObject()
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Show Details", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenuShowDetails }),
                {
                    click: (e, obj) => {
                        if (obj.part.data.assetUid != null && obj.part.data.assetUid != this.emptyUid) {
                            this.isFilterWindowVisible = false;
                            this.isInfoWindowVisible = true;
                            this.showDetails(obj.part.data.assetUid);
                        }
                    }
                }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.hide(e, obj) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Upstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.hide(e, obj, AssetBrowserApiHopDirection.Backward) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Downstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.hide(e, obj, AssetBrowserApiHopDirection.Forward) }
            )//,
            //this.g(
            //    "ContextMenuButton",
            //    this.g(go.TextBlock, { text: "Isolate", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
            //    { click: function (e, obj) { alert("Not yet implemented") } }
            //)
        );
    }

    private assetUidRedirect: string = '';
    private navigateTo(e, obj) {
        this.assetUidRedirect = obj.part.data.assetUid;

        if (this.assetUidRedirect == this.assetUid)
            return;

        this.router.navigateByUrl('/bla', { skipLocationChange: true }).then(() => {
            this.router.navigate([SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT, 'browser', this.assetUidRedirect]);
        });
    }

    private createTooltip(): go.Adornment {
        return this.g("ToolTip",
            this.g(go.TextBlock,
                {
                    maxSize: new go.Size(this.textMaxSize.width * 2, Infinity),
                    wrap: go.TextBlock.WrapFit
                },
                new go.Binding("text", "text")
            )
        );
    }

    private createDiagram(): go.Diagram {

        let layout: go.Layout;

        if (this.lineageDiagramApplies()) {
            layout = this.g(go.LayeredDigraphLayout, { layerSpacing: 150, columnSpacing: 50, setsPortSpots: false });
        }
        else {
            layout = this.g(go.ForceDirectedLayout, {
                defaultSpringLength: 50,
                defaultElectricalCharge: 250,
                arrangementSpacing: new go.Size(250, 250)
            });
        }

        let dg = this.g(go.Diagram, 'LineageDiagram', {
            initialContentAlignment: go.Spot.Center,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            scrollMode: go.Diagram.DocumentScroll,
            initialPosition: new go.Point(125, 125),
            layout: layout,
            "undoManager.isEnabled": true,
            "commandHandler.archetypeGroupData": { isGroup: true, category: "Normal" },
        });

        let model = (dg.model as go.GraphLinksModel);

        //TODO: Get this looking good with the ports. 
        //model.linkFromPortIdProperty = "fromPort";
        //model.linkToPortIdProperty = "toPort",
        model.nodeCategoryProperty = "template";
        model.nodeDataArray = [];
        model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = !this.readonly;
        dg.model.isReadOnly = this.readonly;

        return dg;
    }

    private createPortGroupNode(): go.Group {
        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.createContextMenu(),
                click: (e, obj) => this.highlightPath(e, obj as any), 
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4)
                        }
                    )
            },
            this.basePortGroupNodeContent()
        );
    }

    private createPortFocalGroupNode(): go.Group {
        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.createContextMenu(),
                click: (e, obj) => this.highlightPath(e, obj as any),
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4)
                        }
                    )
            },
            this.g(go.Panel,
                "Auto",

                this.g(
                    go.Shape,
                    "Border",
                    { strokeWidth: 2, isPanelMain: true, spot1: go.Spot.TopLeft, spot2: go.Spot.BottomRight },
                    new go.Binding("fill", "", (v) => go.Brush.mix("#ebebeb", this.lightenBoxColor, 0.7)), 
                    new go.Binding("stroke", "", (v) => this.linkBackColor) //go.Brush.mix("#cccccc", this.lightenBoxColor, 0.7)
                ),

                this.g(go.Panel, "Table",
                    this.g(go.RowColumnDefinition, { width: 10 }),
                    this.makeCircle(go.Spot.Top, 0, 1, "topNodeText", "hasTop"),
                    this.makeCircle(go.Spot.Left, 2, 0, "leftNodeText", "hasLeft"),
                    this.g(go.Panel,
                        "Auto",
                        { row: 2, column: 1 },
                        this.basePortGroupNodeContent()
                    ),
                    this.makeCircle(go.Spot.Right, 2, 2, "rightNodeText", "hasRight"),
                    this.makeCircle(go.Spot.Bottom, 3, 1, "bottomNodeText", "hasBottom")
                )
            )
        );
    }

    private makeCircle(spot, row, col, textprop, visprop) {
        return this.g(go.Panel, "Auto",
            { row: row, column: col },
            this.g(go.Shape,
                "Circle",
                { fill: "transparent", stroke: "transparent" },
                new go.Binding("visible", visprop)),
            this.g(go.TextBlock,
                new go.Binding("text", textprop),
                new go.Binding("visible", visprop))
    );
}

    private basePortGroupNodeContent(): go.Panel {
        return this.g(
            go.Panel,
            "Vertical",
            this.g(go.Panel, "Table",
                new go.Binding("itemArray", "relations"),
                new go.Binding("visible", "showBadges"),
                {
                    itemTemplate: this.createRelationsBadge()
                }
            ),
            this.g(go.Panel, "Table",
                new go.Binding("itemArray", "owners"),
                new go.Binding("visible", "showBadges"),
                {
                    itemTemplate: this.createOwnersBadge()
                }
            ),
            this.g(go.Panel, "Auto",
                this.g(
                    go.Shape,
                    "Rectangle",
                    { strokeWidth: 2, isPanelMain: true },
                    new go.Binding("fill", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, 0.9)),
                    new go.Binding("stroke", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount))
                ),
                this.g(go.Panel, "Vertical",
                    this.g(
                        go.Panel,
                        "Horizontal",
                        // button next to TextBlock
                        { stretch: go.GraphObject.Horizontal, alignment: go.Spot.Top },
                        new go.Binding("background", "", v => (v.isHighlighted) ?
                            go.Brush.mix(this.selectionPathHighlightColor, this.selectionPathHighlightColor, v.backAmount) :
                            go.Brush.mix(v.data.back, this.lightenBoxColor, v.data.backAmount)
                        ).ofObject(),
                        this.g(
                            "SubGraphExpanderButton",
                            { alignment: go.Spot.Right, margin: 5 }
                        ),
                        //icon
                        this.g(
                            go.TextBlock,
                            {
                                row: 0,
                                margin: 0,
                                alignment: go.Spot.Center,
                                editable: false,
                                font: this.fontLabelIcon
                            },
                            new go.Binding("stroke", "", (v) => go.Brush.mix(v.fore, this.darkenBoxColor, v.foreAmount)),
                            new go.Binding("text", "icon"),
                            new go.Binding("visible", "showIcon")
                        ),
                        this.g(
                            go.TextBlock,
                            {
                                alignment: go.Spot.Left,
                                editable: false,
                                margin: 5,
                                font: this.fontLabel,
                                maxLines: this.textMaxLines,
                                maxSize: this.textMaxSize,
                                overflow: this.textOverflowStyle,
                                toolTip: this.createTooltip()
                            },
                            new go.Binding("stroke", "", (v) => go.Brush.mix(v.fore, this.darkenBoxColor, v.foreAmount)),
                            new go.Binding("text", "text").makeTwoWay()
                        )
                    ),  // end Horizontal Panel
                    this.g(
                        go.Panel,
                        "Horizontal",
                        // button next to TextBlock
                        { stretch: go.GraphObject.Horizontal },
                        this.g(
                            go.Placeholder,
                            { padding: 2, alignment: go.Spot.TopLeft },
                        )
                    )  //end Horizontal Panel
                ) //end Vertical Panel,
            ) //end Auto Panel (main group Panel),
        ); //end Vertical Panel
    }

    private createGroupNode(): go.Group {

        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.createContextMenu(),
                click: (e, obj) => this.highlightPath(e, obj as any),
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                stretch: go.GraphObject.Horizontal,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4)
                        }
                    )
            },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),

            this.g(
                go.Shape,
                "Rectangle",
                { fill: null, strokeWidth: 2, stretch: go.GraphObject.Horizontal },
                new go.Binding("stroke", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount))
            ),
            this.g(
                go.Panel,
                "Vertical",  // title above Placeholder
                this.g(
                    go.Panel,
                    "Horizontal",
                    // button next to TextBlock
                    { stretch: go.GraphObject.Horizontal },
                    new go.Binding("background", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount)),
                    new go.Binding("background", "", v => (v.isHighlighted) ?
                        go.Brush.mix(this.selectionPathHighlightColor, this.selectionPathHighlightColor, v.backAmount) :
                        go.Brush.mix(v.data.back, this.lightenBoxColor, v.data.backAmount)
                    ).ofObject(),
                    this.g(
                        "SubGraphExpanderButton",
                        { alignment: go.Spot.Right, margin: 5 }
                    ),
                    //icon
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 0,
                            alignment: go.Spot.Center,
                            editable: false,
                            font: this.fontLabelIcon
                        },
                        new go.Binding("stroke", "", (v) => go.Brush.mix(v.fore, this.darkenBoxColor, v.foreAmount)),
                        new go.Binding("text", "icon"),
                        new go.Binding("visible", "showIcon")
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            alignment: go.Spot.Left,
                            editable: false,
                            margin: 5,
                            font: this.fontLabel,
                            maxLines: this.textMaxLines,
                            maxSize: this.textMaxSize,
                            overflow: this.textOverflowStyle,
                            toolTip: this.createTooltip()
                        },
                        new go.Binding("stroke", "", (v) => go.Brush.mix(v.fore, this.darkenBoxColor, v.foreAmount)),
                        new go.Binding("text", "text").makeTwoWay()
                    )
                ),  // end Horizontal Panel
                this.g(
                    go.Placeholder,
                    { padding: 2, alignment: go.Spot.TopLeft }
                )
            ),

            // end Vertical Panel
        );
    }

    private createListItemNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                contextMenu: this.createContextMenu(),
                click: (e, obj) => this.highlightPath(e, obj as any)
            },
            this.g(
                go.Panel,
                "Horizontal",
                { stretch: go.GraphObject.Horizontal, padding: 5 },
                new go.Binding("background", "isHighlighted", (h) => (h ? this.selectionPathHighlightColor : this.leafBackColor)).ofObject(),
                this.g(
                    go.Shape,
                    { width: 10, height: 0, stroke: "transparent" }
                ),
                //icon
                this.g(
                    go.TextBlock,
                    {
                        row: 0,
                        alignment: go.Spot.Center,
                        editable: false,
                        font: this.fontLabelIcon
                    },
                    new go.Binding("text", "icon"),
                    new go.Binding("visible", "showIcon"),
                    new go.Binding("stroke", "actionCount", (v) => (v > 0) ? this.fontLabelAlertColor : this.fontLabelColor)
                ),
                this.g(
                    go.Shape,
                    { width: 10, height: 0, stroke: "transparent" }
                ),
                //This TextBlock is placeholder for highlighted text
                this.g(
                    go.TextBlock,
                    {
                        stretch: go.GraphObject.Fill,
                        editable: false,
                        font: this.fontLabel,
                        stroke: this.fontLabelColor,
                        visible: false,
                        maxLines: this.textMaxLines,
                        maxSize: this.textMaxSize,
                        overflow: this.textOverflowStyle,
                        toolTip: this.createTooltip()
                    },
                    new go.Binding("text", "highlight").makeTwoWay(),
                    new go.Binding("visible", "highlight_visible").makeTwoWay(),
                    new go.Binding("background", "highlight_background").makeTwoWay()
                ),
                //This shape block is for ensuring space between highlighted text and rest of the text
                //We need this as TextBlock trims spaces
                this.g(
                    go.Shape,
                    { width: 2, height: 0, stroke: "transparent", visible: false },
                    new go.Binding("visible", "spacer_visible").makeTwoWay()
                ),
                this.g(
                    go.TextBlock,
                    {
                        editable: false,
                        font: this.fontLabel,
                        maxLines: this.textMaxLines,
                        maxSize: this.textMaxSize,
                        overflow: this.textOverflowStyle,
                        toolTip: this.createTooltip()
                    },
                    new go.Binding("text", "text").makeTwoWay(),
                    new go.Binding("stroke", "actionCount", (v) => (v > 0) ? this.fontLabelAlertColor : this.fontLabelColor)
                )
            )  // end Horizontal Panel
        );
    }

    private createOwnersGroup(): go.Group {
        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.createContextMenu(),
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4)
                        }
                    )
            },
            this.g(
                go.Panel,
                "Vertical",
                this.g(go.Panel, "Table",
                    new go.Binding("itemArray", "relations"),
                    new go.Binding("visible", "showBadges"),
                    {
                        itemTemplate: this.createRelationsBadge()
                    }
                ),
                this.g(
                    go.Shape,  // the "top" port
                    { width: 0, height: 0, portId: "T", toSpot: go.Spot.TopCenter, toLinkable: true, stroke: 'transparent' }
                ),
                this.g(go.Panel, "Auto",
                    this.g(
                        go.Shape,
                        "Rectangle",
                        { fill: this.fontOwnerBackColor, strokeWidth: 1, isPanelMain: true, stroke: this.fontOwnerBadgeLabelBorderColor }
                    ),
                    this.g(go.Panel, "Vertical",
                        this.g(
                            go.Panel,
                            "Horizontal",
                            // button next to TextBlock
                            { stretch: go.GraphObject.Horizontal, alignment: go.Spot.Top, background: this.fontOwnerBadgeLabelBackColor },
                            this.g(
                                "SubGraphExpanderButton",
                                { alignment: go.Spot.Right, margin: 5 }
                            ),
                            //icon
                            this.g(
                                go.TextBlock,
                                {
                                    row: 0,
                                    margin: 0,
                                    alignment: go.Spot.Center,
                                    editable: false,
                                    font: this.fontLabelIcon,
                                    stroke: '#000000'
                                },
                                new go.Binding("text", "icon"),
                                new go.Binding("visible", "showIcon")
                            ),
                            this.g(
                                go.TextBlock,
                                {
                                    alignment: go.Spot.Left,
                                    editable: false,
                                    margin: 5,
                                    font: this.fontLabel,
                                    maxLines: this.textMaxLines,
                                    maxSize: this.textMaxSize,
                                    overflow: this.textOverflowStyle,
                                    toolTip: this.createTooltip()
                                },
                                new go.Binding("text", "text").makeTwoWay()
                            )
                        ),  // end Horizontal Panel
                        this.g(
                            go.Panel,
                            "Horizontal",
                            // button next to TextBlock
                            { stretch: go.GraphObject.Horizontal },
                            this.g(
                                go.Shape,  // the "left" port
                                { width: 0, height: 0, portId: "L", toSpot: go.Spot.LeftCenter, toLinkable: true, stroke: "transparent" }
                            ),
                            this.g(
                                go.Placeholder,
                                { padding: 2, alignment: go.Spot.TopLeft },
                            ),
                            this.g(
                                go.Shape,  // the "right" port
                                { width: 0, height: 0, portId: "R", toSpot: go.Spot.RightCenter, toLinkable: true, stroke: "transparent" }
                            )
                        ),  //end Horizontal Panel

                        this.g(
                            go.Shape,  // the "bottom" port
                            { width: 0, height: 0, portId: "B", toSpot: go.Spot.BottomCenter, toLinkable: true, stroke: "transparent" }
                        ),
                    ) //end Vertical Panel,
                ) //end Auto Panel (main group Panel),
            ), //end Vertical Panel
        );
    }

    private createOwnerNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                contextMenu: this.createContextMenu(),
                click: (e, obj) => this.highlightPath(e, obj as any)
            },
            this.g(
                go.Panel,
                "Horizontal",
                { stretch: go.GraphObject.Horizontal, padding: 5 },
                new go.Binding("background", "isHighlighted", (h) => (h ? this.selectionPathHighlightColor : this.leafBackColor)).ofObject(),
                this.g(
                    go.Shape,
                    { width: 10, height: 0, stroke: "transparent" }
                ),
                //icon
                this.g(
                    go.TextBlock,
                    {
                        row: 0,
                        alignment: go.Spot.Center,
                        editable: false,
                        font: this.fontLabelIcon,
                        stroke: this.fontLabelColor
                    },
                    new go.Binding("text", "icon"),
                    new go.Binding("visible", "showIcon")
                ),
                this.g(
                    go.Shape,
                    { width: 10, height: 0, stroke: "transparent" }
                ),
                //This TextBlock is placeholder for highlighted text
                this.g(
                    go.TextBlock,
                    {
                        stretch: go.GraphObject.Fill,
                        editable: false,
                        font: this.fontLabel,
                        stroke: this.fontLabelColor,
                        visible: false,
                        maxLines: this.textMaxLines,
                        maxSize: this.textMaxSize,
                        overflow: this.textOverflowStyle,
                        toolTip: this.createTooltip()
                    },
                    new go.Binding("text", "highlight").makeTwoWay(),
                    new go.Binding("visible", "highlight_visible").makeTwoWay(),
                    new go.Binding("background", "highlight_background").makeTwoWay()
                ),
                //This shape block is for ensuring space between highlighted text and rest of the text
                //We need this as TextBlock trims spaces
                this.g(
                    go.Shape,
                    { width: 2, height: 0, stroke: "transparent", visible: false },
                    new go.Binding("visible", "spacer_visible").makeTwoWay()
                ),
                this.g(
                    go.TextBlock,
                    {
                        editable: false,
                        font: this.fontLabel,
                        stroke: this.fontLabelColor,
                        maxLines: this.textMaxLines,
                        maxSize: this.textMaxSize,
                        overflow: this.textOverflowStyle,
                        toolTip: this.createTooltip()
                    },
                    new go.Binding("text", "text").makeTwoWay()
                )
            )  // end Horizontal Panel
        );
    }

    private createRevealNodeTemplate(): go.Node {
        return this.g(go.Node, "Auto",
            {
                click: (e, obj) => this.revealLineageHop(e, obj),
                cursor: 'pointer'
            },
            this.g(
                go.Panel,
                "Horizontal",
                { stretch: go.GraphObject.Horizontal, padding: 10, type: go.Panel.Spot },
                this.g(
                    "Shape",
                    { alignment: go.Spot.Center, width: 25, height: 25 },
                    new go.Binding("fill", "back"),
                    new go.Binding("stroke", "back", (v) => go.Brush.mix(v.back, this.lightenBoxColor, .15)),
                ),
                this.g(
                    go.TextBlock,
                    {
                        row: 0,
                        alignment: go.Spot.Center,
                        editable: false,
                        font: this.fontLabelIcon,
                        stroke: this.fontLabelColor,
                        text: this.plusIcon
                    },
                )
            )  // end Horizontal Panel
        );
    }

    private createHiddenDataNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                click: (e, obj) => this.unhide(e, obj),
                cursor: 'pointer'
            },
            this.g(
                go.Panel,
                "Horizontal",
                { stretch: go.GraphObject.Horizontal, padding: 10, type: go.Panel.Spot },
                this.g(
                    "Shape",
                    {
                        alignment: go.Spot.Center,
                        width: 25,
                        height: 25
                    },
                    new go.Binding("fill", "back"),
                    new go.Binding("stroke", "back", (v) => go.Brush.mix(v, this.lightenBoxColor, .15))
                ),
                this.g(
                    go.TextBlock,
                    {
                        row: 0,
                        alignment: go.Spot.Center,
                        editable: false,
                        font: this.fontLabelIcon,
                        stroke: this.fontLabelColor,
                        text: this.plusIcon
                    },
                )
            )  // end Horizontal Panel
        );
    }

    private createLineageLink(): go.Link {
        return this.g(
            go.Link, {
            routing: go.Link.AvoidsNodes,
            corner: 5,
            relinkableFrom: false,
            relinkableTo: false,
            click: (e, obj) => this.highlightPath(e, obj as any),
            zOrder: 1000
        },
            // the whole link panel
            this.g(go.Shape,
                { stroke: this.linkBackColor, strokeWidth: 1 },
                new go.Binding("strokeWidth", "hasProperties", function (h) {
                    return h ? 3 : 2;
                }),
                new go.Binding("stroke", "hasProperties", (h) => (h ? "black" : this.linkBackColor))
            ), // the link shape
            this.g(go.Shape, { toArrow: "Triangle", fill: this.linkBackColor, stroke: this.linkBackColor }), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(
                    go.Shape,
                    {
                        visible: false,
                        fill: this.linkDefaultBackColor,
                        stroke: this.linkDefaultBorderColor
                    },
                    new go.Binding("background", "back"),
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) {
                        return !!a
                    })
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center",
                    font: this.fontLink,
                    stroke: this.fontLinkColor,
                    margin: 4,
                    overflow: go.TextBlock.OverflowEllipsis,
                    wrap: go.TextBlock.WrapFit,
                    maxSize: new go.Size(100, 70)
                },
                    // the label
                    new go.Binding("text", "text").makeTwoWay()
                )
            )
        );
    }

    private createImpactLink(): go.Link {
        return this.g(
            go.Link, 
            // the whole link panel
            this.g(go.Shape,
                { stroke: this.linkBackColor, strokeWidth: 1 },
                new go.Binding("strokeWidth", "hasProperties", function (h) {
                    return h ? 3 : 2;
                }),
                new go.Binding("stroke", "hasProperties", (h) => (h ? "black" : this.linkBackColor))
            ), // the link shape
            this.g(go.Shape, { toArrow: "Triangle", fill: this.linkBackColor, stroke: this.linkBackColor }), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(
                    go.Shape,
                    {
                        visible: false,
                        fill: this.linkDefaultBackColor,
                        stroke: this.linkDefaultBorderColor
                    },
                    new go.Binding("background", "back"),
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) {
                        return !!a
                    })
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center",
                    font: this.fontLink,
                    stroke: this.fontLinkColor,
                    margin: 4,
                    overflow: go.TextBlock.OverflowEllipsis,
                    wrap: go.TextBlock.WrapFit,
                    maxSize: new go.Size(100, 70)
                },
                    // the label
                    new go.Binding("text", "text").makeTwoWay()
                )
            )
        );
    }

    //#endregion

    //#region Search

    private searchResults: go.Node[] = [];
    private searchableProps: string[] = ["text"];
    private searchCurrentItem: number = 0;
    private searchValue: string = '';
    private searchTimer;

    searchDiagram(event) {
        if (event == null) {
            this.searchValue = '';
        }
        else {
            this.searchValue = event.target.value;

            if (event.keyCode == 40) {
                this.goToNext();
                return;
            }
            if (event.keyCode == 38) {
                this.goToPrevious();
                return;
            }
        }
        clearTimeout(this.searchTimer);
        this.searchTimer = setTimeout(() => {
            this.doSearch();
        }, 100);


    }

    private doSearch() {
        //Clear highlights of exisitng search results
        this.searchResults.forEach(n => {
            this.removeHighlightFromNode(n);
        });

        this.searchResults = [];

        this.searchCurrentItem = 0;
        this.diagram.zoomToFit();
        var self = this;

        this.diagram.nodes.each(function (node) {
            if (node instanceof go.Node) {
                var nodeData = node.data;
                node.isHighlighted = false;
                if (nodeData.isGroup) {
                    //This is grouping, do nothing with it (AssetType grouping)
                }
                else if (self.searchValue != '') {
                    self.searchableProps.forEach(prop => {
                        if (node.data[prop] && node.data[prop].toLowerCase().indexOf(self.searchValue.toLowerCase()) == 0) {
                            self.searchResults.push(node);
                            self.addHighlightToNode(node);
                            self.expandGroups(node.data.group);
                        }
                    });
                }
            }
        });


        this.goToNext();

        this.cdRef.markForCheck();
    }

    removeHighlightFromNode(node: go.Node) {
        this.diagram.model.commit(function (m) {
            var data = m.findNodeDataForKey(node.key);
            var fullText = (data) ? data.text : "";
            if (data.highlight) {
                fullText = data.highlight + data.text;
            }
            m.set(data, 'highlight', '');
            m.set(data, 'highlight_visible', false);
            m.set(data, 'spacer_visible', false);
            m.set(data, 'text', fullText);
        }, 'update_highlight');
    }

    addHighlightToNode(node: go.Node) {
        var self = this;
        this.diagram.model.commit(function (m) {
            var data = m.findNodeDataForKey(node.key);

            var idx = self.searchValue.length;
            var highlight = data.text.substring(0, idx);
            var text = data.text.substring(idx, data.text.length);

            if (data.text.length > idx && (data.text[idx] == ' ' || self.searchValue[idx - 1] == ' ')) {
                m.set(data, 'spacer_visible', true);
            }
            m.set(data, 'highlight', highlight);
            m.set(data, 'highlight_visible', true);
            m.set(data, 'text', text);
        }, 'update_highlight');
    }

    goToPrevious() {
        this.searchCurrentItem--;
        if (this.searchCurrentItem <= 0) {
            if (this.searchResults.length > 0) {
                this.searchCurrentItem = 1;
            }
            else {
                this.searchCurrentItem = 0;
            }
        }
        this.focusCurrentNode();
    }

    goToNext() {
        this.searchCurrentItem++;
        if (this.searchCurrentItem > this.searchResults.length)
            this.searchCurrentItem--;
        this.focusCurrentNode();

    }

    expandGroups(groupName) {
        if (groupName) {
            var group = this.diagram.findPartForKey(groupName) as go.Group;
            group.expandSubGraph();
            this.expandGroups(group.data.group);
        }
    }

    focusCurrentNode() {
        var node = this.searchResults[this.searchCurrentItem - 1];
        if (node) {
            this.diagram.centerRect(node.actualBounds);
            this.diagram.select(node);
            this.setFocusedNodeHighlight(node);
        }
    }

    setFocusedNodeHighlight(node: go.Node) {
        var self = this;
        this.diagram.model.commit(function (m) {
            self.diagram.nodes.each(function (n) {
                if (n instanceof go.Node) {
                    var data = m.findNodeDataForKey(n.key);

                    if (n.key == node.key) {
                        m.set(data, 'highlight_background', self.searchHighlightColourFocused);
                    }
                    else {
                        m.set(data, 'highlight_background', self.searchHighlightColour);
                    }
                }
            })
        });
    }

    //#endregion
} 