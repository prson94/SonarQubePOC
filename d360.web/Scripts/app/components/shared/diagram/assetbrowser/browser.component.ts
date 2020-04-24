import * as go from 'gojs';
import * as _ from 'lodash';
import { AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChange, SimpleChanges, EventEmitter, Output, AfterViewChecked } from '@angular/core';
import {
    AssetBrowserTranslation,
    AssetBrowserApiHopDirection,
    AssetBrowserDiagramAsset,
    AssetBrowserTranslationNode,
    AssetBrowserTranslationLink,
    AssetBrowserTranslationRelationCount,
    AssetBrowserFilterModel,
    FilterSelectionsModel,
    AssetBrowserApiHopRequestModel,
    AssetBrowserApiHopAssetRequestModel,
    AssetBrowserTranslationOwnerCount,
    AssetBrowserApiOwnerHopRequestModel,
    AssetBrowserAssetsModel,
    AssetBrowserModel,
    AssetBrowserAssetModel,
    AssetBrowserGenericRelationModel,
    LoadedFilterTypesModel,
    AssetBrowserApiHopType,
    AssetBrowserAlert,
    DiagramType,
    AssetBrowserFilterChangeEventType,
    AssetBrowserFilterChangeEvent,
    AssetBrowserPanelCommand,
    AssetBrowserPanelModel,
    AssetBrowserApiHopIgnoreRequestModel
} from '../../../../models/lineage.model';

import { BrowserService } from '../../../../services/browser.service';
import { PermissionsService } from '../../../../services/permissions.service';
import { MessagesObservableService } from '../../../../services/messages-observable.service';


import { DiagramBaseComponent } from '../diagram-base.component';
import { MenuItem, SelectItem, TreeNode } from 'primeng/api';
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
    @Input() readonly = true;
    @Input() assetUid: string;

    @ViewChild('addLineagePanel', { static: false }) addLineagePanelRef;
    @ViewChild('alertPanel', { static: false }) alertPanelRef;
    @ViewChild('infoDetailPanel', { static: false }) infoDetailPanelRef;
    @ViewChild('ownerDetailPanel', { static: false }) ownerDetailPanelRef;
    @ViewChild('diagram', { static: false }) diagramRef;
    @ViewChild('filterDetailPanel', { static: false }) filterDetailPanelRef;

    private requestModel: AssetBrowserApiHopRequestModel;
    private responseModel: AssetBrowserModel = new AssetBrowserModel();
    private revealedKeys: string[] = [];
    private originalAssetUid: string;

    private alerts: AssetBrowserAlert[] = [];
    private assetsWithAlerts: string[] = [];
    private selectedAssetsWithAlerts: string[] = [];
    private totalAlertCount = 0;

    private diagramTypeSpecifiedInPath = DiagramType.Lineage;
    private isDiagramTypeSpecifiedInPath = false;

    private selectedDiagramAsset: AssetBrowserDiagramAsset;
    private isFullScreen = false;
    private loadingText = '';

    private searchText = '';
    private searchResults: go.Node[] = [];
    private searchableProps: string[] = ["text"];

    private panel_Loading = false;
    private panel_InformationDisabled = true;
    private panel_TabIndex = 0;

    private panelModel: AssetBrowserPanelModel = { selectedCommand: AssetBrowserPanelCommand.None, AddVisible: false, AlertVisible: false, FiltersVisible: false, InformationVisible: false, SettingsVisible: false };

    displayConfiguration: AssetBrowserFilterModel = new AssetBrowserFilterModel();
    private readonly displayConfigurationKey = 'asset-browser-configuration';
    private storage = window.sessionStorage;
    scale = 1;
    filter_AvailableOptions: FilterSelectionsModel = new FilterSelectionsModel([], [], []);
    filter_AllOptions: FilterSelectionsModel = new FilterSelectionsModel([], [], []);

    //#region Constants

    private readonly emptyUid: string = '00000000-0000-0000-0000-000000000000';
    private readonly fontContextMenu: string = "12px 'Source Sans Pro'";
    private readonly fontContextMenuhelper_ShowDetails: string = "bold 12px 'Source Sans Pro'";

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

    //#region Component Base Methods

    constructor(
        private route: ActivatedRoute,
        private myElement: ElementRef,
        private browserService: BrowserService,
        private router: Router,
        protected permissionsService: PermissionsService,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        protected messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    public ngOnInit() {

        this.originalAssetUid = this.assetUid;

        this.checkSecondaryNavLocalStorage();

        // Do this only on initial load.
        this.browserService
            .getFilterOptions()
            .subscribe(options => {
                this.filter_AllOptions = options;
            });

        this.route.params.subscribe(
            params => {
                this.originalAssetUid = params['assetUid'];

                this.loadFilter(); // Load the default filter BEFORE updating the pre-selected diagram type.

                if (params['diagramType']) {
                    let diagramTypeParameterValue: string = params['diagramType'];

                    this.isDiagramTypeSpecifiedInPath = (diagramTypeParameterValue in DiagramType);
                    if (!this.isDiagramTypeSpecifiedInPath) {
                        diagramTypeParameterValue = 'Lineage';
                    }

                    this.diagramTypeSpecifiedInPath = DiagramType[diagramTypeParameterValue];
                    this.helper_UpdateDiagramType(this.diagramTypeSpecifiedInPath);
                }

                if (this.diagram) this.diagram.div = null;
                this.helper_InitializeDiagram();
            }
        );
    }

    public ngAfterViewInit() {
        this.helper_ResizeDiagram();
        this.cdRef.markForCheck();
    }

    public ngAfterViewChecked() {

        const panelHeaderElement: HTMLElement = this.myElement.nativeElement.querySelectorAll('.asset-browser-window-header')[0];
        const panelElements: HTMLElement[] = this.myElement.nativeElement.querySelectorAll('.asset-browser-window');

        (function () {
            if (typeof NodeList.prototype.forEach === "function") return false;
            panelElements.forEach = Array.prototype.forEach;
        })();
        const diagramSize = +this.diagramRef.nativeElement.style.height.replace('px', '');
        panelElements.forEach(el => {
            el.style.height = (diagramSize - 75) + 'px';
            el.style.maxHeight = (diagramSize - 75) + 'px';
            const panelHeaderSize = panelHeaderElement.clientHeight;

            const innerPanelHeight = (diagramSize - 75 - panelHeaderSize - 50) + 'px';
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
            if (this.ownerDetailPanelRef) {
                this.ownerDetailPanelRef.nativeElement.style.height = innerPanelHeight;
            }
        });

    }

    public ngOnDestroy() {
        this.diagram.div = null;    // Garbage collection.
    }

    //#endregion

    // UI
    private isWindowVisible(): boolean {
        return (
            this.panelModel.AddVisible ||
            this.panelModel.AlertVisible ||
            this.panelModel.FiltersVisible ||
            this.panelModel.InformationVisible ||
            this.panelModel.SettingsVisible
        );
    }

    private ownershipTabEnabled() {
        let enabled = false;

        if (this.selectedDiagramAsset) {
            enabled = (this.selectedDiagramAsset.Owners.length > 0);
        }

        return enabled;
    }

    //#region Session storage

    private saveState(key: string, data: any) {
        const dataString = JSON.stringify(data);
        this.storage.setItem(key, dataString);
    }

    private loadState(key: string): any {
        const dataString = this.storage.getItem(key);
        if (dataString) {
            return JSON.parse(dataString);
        }
        return null;
    }

    private saveFilter() {
        this.saveState(this.displayConfigurationKey, this.displayConfiguration);
    }

    private loadFilter() {
        const m: AssetBrowserFilterModel = this.loadState(this.displayConfigurationKey);
        if (m === null)
            this.displayConfiguration = new AssetBrowserFilterModel();
        else {
            // Override the selected diagram type in the session, as you are going to a specific diagram via the path. 
            if (this.isDiagramTypeSpecifiedInPath) {
                m.DiagramType = this.diagramTypeSpecifiedInPath;
            }
            this.displayConfiguration = m;
        }
            
    }

    //#endregion

    //Core events
    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.helper_ResizeDiagram();
    }

    @HostListener('document:keydown', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        if (event.key === "Escape" || event.key === "Esc") {
            this.isFullScreen = false;
            this.helper_ResizeDiagram();
            this.cdRef.markForCheck();
        }
    }

    /**
    * Responds to the openDetail event from the shared Asset Browser Alert Panel.
    * @returns Nothing.
    */
    private alert_OpenDetail(alert: AssetBrowserAlert) {
        this.helper_SetVisiblePanel(AssetBrowserPanelCommand.Information);
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
        this.helper_ShowDetail(this.selectedDiagramAsset.Uid);
        this.panel_TabIndex = 0;
    }

    private badge_ClickImpact(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let ix = obj.itemIndex;
            let node: AssetBrowserTranslationNode = obj.part.data;
            let relation: AssetBrowserTranslationRelationCount = node.relations[ix];
            if (!relation.disabled) {
                relation.disabled = true;

                if (relation.expanded) {
                    this.helper_CollapseBadgeRelationDependentNodesAndLinks(node.key, relation.predicateId, relation.predicate, relation.direction.toString());
                    relation.expanded = false;
                    this.diagram.model.removeArrayItem(node.relations, ix);
                    this.diagram.model.insertArrayItem(node.relations, ix, relation);
                    this.helper_CalculateAlertCount();
                    this.cdRef.markForCheck();                    
                    relation.disabled = false;
                }
                else {
                    let requestModel: AssetBrowserApiHopRequestModel = new AssetBrowserApiHopRequestModel();

                    requestModel.Initial = false;
                    requestModel.Assets = [];
                    requestModel.PredicateUid = relation.predicateUid;
                    requestModel.Direction = relation.direction;
                    requestModel.HopType = AssetBrowserApiHopType.Impact;
                    requestModel.Hops = 1;
                    requestModel.LeafOnly = !this.displayConfiguration.IncludeNonLeaf;

                    let n = node;
                    if (n.isGroup) {
                        // Add the root node's asset information.
                        if (this.displayConfiguration.IncludeNonLeaf && node.assetUid !== this.emptyUid) {
                            requestModel.Assets.push({ Uid: node.assetUid, Key: node.key });
                        }
                        (this.diagram.findNodeForData(n) as go.Group).findSubGraphParts().each(g => {
                            let shouldInclude: boolean = this.displayConfiguration.IncludeNonLeaf ? true : (g.data.isGroup == undefined || g.data.isGroup == false);
                            if (shouldInclude && g.data.assetUid !== this.emptyUid) {
                                let asset = new AssetBrowserApiHopAssetRequestModel();
                                asset.Uid = g.data.assetUid;
                                asset.Key = g.data.key
                                requestModel.Assets.push(asset);
                            }
                        })
                    }

                    // Get relations to ignore.
                    requestModel.RelationsToIgnore = this.helper_GetRelationsToIgnore(relation.predicateId);

                    let subscriber = (response: AssetBrowserAssetsModel) => {
                        response.assets.forEach(a => {
                            this.responseModel.assets.assets.push(a);
                        });
                        response.assetRelations.forEach(i => {
                            this.responseModel.assets.assetRelations.push(i);
                        });

                        let nodeToPull = this.helper_FindInApiModel(node.key, this.responseModel.assets);
                        if (nodeToPull) {
                            response.assets.push(nodeToPull);
                        }

                        response = this.browserService.convertResponseModel(response, this.displayConfiguration.AncestryMode);

                        let keysToBeConcernedWith: string[] = [];
                        let nodes = this.browserService.translateAssetNodes(this.displayConfiguration.IncludeNonLeaf, response.assets);
                        nodes.forEach(n => {
                            keysToBeConcernedWith.push(n.key);
                        });

                        let trans: AssetBrowserTranslation = new AssetBrowserTranslation();
                        trans.nodes = nodes;
                        trans.links = this.browserService.translateAssetLinks(this.helper_GetFullResponseModelAsTranslationNodes(), response.assetRelations);

                        // Now track which asset Uids should be ignored for badge expansion, so we do not duplicate nodes (GOV-10039).
                        requestModel.Assets.forEach(reqAsset => {
                            response.assetRelations.forEach(r => {
                                let idxToUpdate: number = -1;
                                if (r.subjectKey == reqAsset.Key) {
                                    idxToUpdate = trans.nodes.findIndex(s => { return s.key == r.objectKey });
                                }
                                else if (r.objectKey == reqAsset.Key) {
                                    idxToUpdate = trans.nodes.findIndex(o => { return o.key == r.subjectKey });
                                }
                            });
                        });

                        relation.expanded = true;
                        this.helper_ParseTranslatedData(trans, true);

                        this.helper_SetFilterWindow();

                        this.helper_HideDeselectedAssetTypes(keysToBeConcernedWith);
                        this.helper_HideDeselectedPredicates(keysToBeConcernedWith);
                        this.helper_HideDeselectedResponsibilityTypes(keysToBeConcernedWith);

                        relation.disabled = false;
                    };

                    if (this.helper_LineageDiagramApplies()) {
                        this.requestModel.DiagramType = DiagramType.Lineage;
                        this.browserService.getAssetBrowserHop(requestModel).subscribe(subscriber);
                    }
                    else {
                        this.requestModel.DiagramType = DiagramType.Impact;
                        this.browserService.getImpactBrowserHop(requestModel).subscribe(subscriber);
                    }
                }

            }
        }
    }

    private badge_ClickOwner(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let ix = obj.itemIndex;
            let node: AssetBrowserTranslationNode = obj.part.data;
            let owner: AssetBrowserTranslationOwnerCount = node.owners[ix];

            if (owner.expanded) {
                this.helper_CollapseBadgeOwnerDependentNodesAndLinks(node.key, owner.responsibilityTypeId);
                owner.expanded = false;
                this.diagram.model.removeArrayItem(node.owners, ix);
                this.diagram.model.insertArrayItem(node.owners, ix, owner);
                this.helper_CalculateAlertCount();
                this.cdRef.markForCheck();
            }
            else {
                let requestModel: AssetBrowserApiOwnerHopRequestModel = new AssetBrowserApiOwnerHopRequestModel();

                requestModel.Assets = [];
                requestModel.ResponsibilityTypeId = owner.responsibilityTypeId;

                let n = node;
                if (n.isGroup) {
                    // Add the root node's asset information.
                    if (this.displayConfiguration.IncludeNonLeaf && node.assetUid !== this.emptyUid) {
                        requestModel.Assets.push({ Uid: node.assetUid, Key: node.key });
                    }


                    (this.diagram.findNodeForData(n) as go.Group).findSubGraphParts().each(g => {
                        let shouldInclude: boolean = this.displayConfiguration.IncludeNonLeaf ? true : (g.data.isGroup == undefined || g.data.isGroup == false);
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
                            l.responsibilityTypeId = owner.responsibilityTypeId;
                        });
                        owner.expanded = true;

                        this.helper_ParseTranslatedData(trans, true);

                        this.helper_SetFilterWindow();
                    });
            }
        }
    }

    private context_Hide(e, obj, direction: AssetBrowserApiHopDirection = null) {
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
                    this.helper_HideNode(node, group);
                }
                else { //hide upstream or downstream
                    let subgraph = this.helper_FindSubGraph(group.key, direction);

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

    private context_Unhide(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: AssetBrowserTranslationNode = obj.part.data;
            this.helper_UnhideNode(node);
        }
    }

    private event_DiagramSelectionChanged(e: go.DiagramEvent) {
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
                        this.panel_InformationDisabled = !data.hasAssetReadAccess;
                        if (this.selectedDiagramAsset == null || this.selectedDiagramAsset.Uid != uid) {
                            if (this.panelModel.AlertVisible) {
                                this.selectedAssetsWithAlerts = [uid];
                            }
                            else {
                                this.selectedDiagramAsset = new AssetBrowserDiagramAsset();
                                this.selectedDiagramAsset.Uid = uid;
                                if (this.panelModel.InformationVisible) {
                                    if (this.panel_InformationDisabled) {
                                        this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
                                    }
                                    else {
                                        this.helper_ShowDetail(uid);
                                    }
                                    
                                }
                                this.cdRef.markForCheck();
                            }
                        }
                    }
                    else {
                        this.diagram.nodes.each(n => {
                            n.isHighlighted = false;
                        });
                        this.selectedDiagramAsset = null;
                        this.panel_InformationDisabled = true;
                        if (this.panelModel.AlertVisible) {
                            this.selectedAssetsWithAlerts = this.assetsWithAlerts;
                        }
                        else if (this.panelModel.InformationVisible) {
                            this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
                        }
                        this.cdRef.markForCheck();
                    }

                } else if (parts.count == 0) {
                    this.diagram.nodes.each(n => {
                        n.isHighlighted = false;
                    });
                    this.selectedDiagramAsset = null;
                    this.panel_InformationDisabled = true;
                    this.panel_TabIndex = 0;
                    if (this.panelModel.AlertVisible) {
                        this.selectedAssetsWithAlerts = this.assetsWithAlerts;
                    }
                    else if (this.panelModel.InformationVisible) {
                        this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
                    }
                    this.cdRef.markForCheck();
                }
            }
        }
    }

    private event_Information_DetailTabClick() {
        this.panel_TabIndex = 0;
        this.cdRef.markForCheck();
    }

    private event_Information_OwnerTabClick() {
        this.panel_TabIndex = 1;
        this.cdRef.markForCheck();
    }

    private event_ViewportBoundsChanged(e: go.DiagramEvent) {
        this.scale = e.subject.scale;
        this.cdRef.markForCheck();
    }

    private filterpanel_Apply(e: AssetBrowserFilterChangeEvent) {
        this.displayConfiguration = e.Model;
        this.saveFilter();
        switch (e.Type) {
            case AssetBrowserFilterChangeEventType.Ancestry:
                this.helper_RefreshDiagram(false);
                break;
            case AssetBrowserFilterChangeEventType.AssetType:
                this.helper_HideDeselectedAssetTypes(undefined);
                break;
            case AssetBrowserFilterChangeEventType.ImpactHopCount:
            case AssetBrowserFilterChangeEventType.LineageHopCount:
                this.helper_RefreshDiagram(false);
                break;
            case AssetBrowserFilterChangeEventType.Predicate:
                this.helper_HideDeselectedPredicates(undefined);
                break;
            case AssetBrowserFilterChangeEventType.ResponsibilityType:
                this.helper_HideDeselectedResponsibilityTypes(undefined);
                break;
        }
    }

    /**
    * Calculates the assets Uid array and total alert count by looking at the currently displayed nodes and searching for the actionCount property.
    * @returns Nothing.
    */
    private helper_CalculateAlertCount() {
        this.totalAlertCount = 0;
        this.assetsWithAlerts = [];
        this.diagram.nodes.each(n => {
            if (n.data) {
                if (n.data.actionCount) {
                    // Below condition checks to see if the assetUid has already been accounted for in alert count, so as not to double-count [GOV-9970].
                    if (this.assetsWithAlerts.findIndex(a => { return a == n.data.assetUid; }) == -1) {
                        this.totalAlertCount += n.data.actionCount;
                        this.assetsWithAlerts.push(n.data.assetUid);
                    }
                }
            }
        });
        //if (this.panelModel.selectedCommand != AssetBrowserPanelCommand.Alerts) {
        //    this.showAlertsByDisplayedAssets();
        //}
    }

    private helper_CollapseBadgeOwnerDependentNodesAndLinks(nodeKey: string, responsibilityTypeId: number) {
        this.diagram.startTransaction("collapseOwnerBadge");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        let links: go.Iterator<go.Link>;

        links = this.diagram.links.filter(l => l.fromNode.key == nodeKey && l.data.responsibilityTypeId == responsibilityTypeId);
        this.helper_CollapseNodesAndLinks(dm, nodeKey, nodeKey, links);

        this.diagram.commitTransaction("collapseOwnerBadge");
    }

    private helper_CollapseBadgeRelationDependentNodesAndLinks(nodeKey: string, predicateId: number, predicateName: string, direction: string) {
        this.diagram.startTransaction("collapseRelationBadge");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        let links: go.Iterator<go.Link>;
        switch (AssetBrowserApiHopDirection[direction]) {
            case AssetBrowserApiHopDirection.Backward:
                if (this.displayConfiguration.DiagramType == DiagramType.Impact) {
                    // Impact diagram always points forward. So disregard the direciton sent in be predicate badge.
                    links = this.diagram.links.filter(l =>
                        l.data.text == predicateName &&
                        (l.fromNode.key == nodeKey) && l.data.predicateIds.findIndex(pr => { return pr == predicateId; }) > -1
                    );
                }
                else {
                    links = this.diagram.links.filter(l =>
                        l.data.text == predicateName &&
                        (l.toNode.key == nodeKey) &&
                        l.data.predicateIds.findIndex(pr => { return pr == predicateId; }) > -1
                    );
                }
                break;
            case AssetBrowserApiHopDirection.Forward:
                links = this.diagram.links.filter(l =>
                    l.data.text == predicateName &&
                    (l.fromNode.key == nodeKey) && l.data.predicateIds.findIndex(pr => { return pr == predicateId; }) > -1
                );
                break;
            default:
                links = this.diagram.links.filter(l =>
                    l.data.text == predicateName &&
                    (l.fromNode.key == nodeKey || l.toNode.key == nodeKey) &&
                    l.data.predicateIds.findIndex(pr => { return pr == predicateId; }) > -1
                );
                break;
        }

        this.helper_CollapseNodesAndLinks(dm, nodeKey, nodeKey, links);

        this.diagram.commitTransaction("collapseRelationBadge");
    }

    private helper_CollapseNodesAndLinks(dm: go.GraphLinksModel, initialKey: string, key: string, links: go.Iterator<go.Link>) {
        if (links) {
            let lnks: any[] = [];
            links.iterator.each(link => {
                lnks.push({ link: link, node: (link.toNode.key == key) ? link.fromNode : link.toNode });
            });
            lnks.forEach(lnk => {
                if (lnk.node) {
                    if (lnk.node.key != initialKey) {
                        let backLinks: go.Iterator<go.Link> = lnk.node.findLinksInto().filter(b => { return (b.fromNode.key !== key); });
                        this.helper_CollapseNodesAndLinks(dm, initialKey, lnk.node.key, backLinks);

                        let forwardLinks: go.Iterator<go.Link> = lnk.node.findLinksOutOf().filter(b => { return (b.toNode.key !== key); });
                        this.helper_CollapseNodesAndLinks(dm, initialKey, lnk.node.key, forwardLinks);

                        // Remove immediate child.
                        this.diagram.remove(lnk.node);
                        dm.removeNodeData(dm.findNodeDataForKey(lnk.node.key));
                    }
                }

                this.diagram.remove(lnk.link);
            });
        }
    }

    private helper_DetermineLoadedFilterOptions(): LoadedFilterTypesModel {
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

    private helper_DisableDragging() {
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

    private helper_FindInApiItemModel(key: string, model: AssetBrowserAssetModel): boolean {
        let found: boolean = false;

        if (model.key == key) {
            found = true;
        }
        else {
            if (model.items) {
                model.items.forEach(child => {
                    if (!found) {
                        if (child.key == key) {
                            found = true;
                        }
                        else {
                            if (child.items) {
                                found = this.helper_FindInApiItemModel(key, child);
                            }
                        }
                    }
                });
            }
        }

        return found;
    }

    private helper_FindInApiModel(key: string, model: AssetBrowserAssetsModel): AssetBrowserAssetModel {
        let found: AssetBrowserAssetModel;

        model.assets.forEach(root => {
            if (!found) {
                if (this.helper_FindInApiItemModel(key, root)) {
                    found = root;
                }
            }
        });

        return found;
    }

    private helper_FindSubGraph(startKey: string, direction: AssetBrowserApiHopDirection): AssetBrowserTranslation {
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

    /**
    * Takes a given asset key and searches for it within a collection of assets (each with its own hierarchy).
    * @returns The root asset that the given key is located within, regardless of level within ancestry.
    */
    private helper_FindTrueRootAssetInCollection(keyToFind: string, currentRoot: AssetBrowserAssetModel, currentParentToSearch: AssetBrowserAssetModel): AssetBrowserAssetModel {
        let foundRootAsset: AssetBrowserAssetModel;

        if (!currentRoot) {
            this.responseModel.assets.assets.forEach(a => {
                if (foundRootAsset == undefined) {
                    foundRootAsset = this.helper_FindTrueRootAssetInCollection(keyToFind, a, undefined);
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
                                    foundRootAsset = this.helper_FindTrueRootAssetInCollection(keyToFind, currentRoot, i);
                                }
                            });
                        }
                    }
                }
                else {
                    if (currentRoot.items) {
                        currentRoot.items.forEach(i => {
                            if (foundRootAsset == undefined) {
                                foundRootAsset = this.helper_FindTrueRootAssetInCollection(keyToFind, currentRoot, i);
                            }
                        });
                    }
                }
            }
        }
        return foundRootAsset;
    }

    /**
    * Convert the stored raw data set from the API while taking into account the ancestry setting.
    * @returns A collection of translated nodes.
    */
    private helper_GetFullResponseModelAsTranslationNodes(): AssetBrowserTranslationNode[] {
        let existingAssets = this.browserService.convertResponseModel(this.responseModel.assets, this.displayConfiguration.AncestryMode);
        return this.browserService.translateAssetNodes(this.displayConfiguration.IncludeNonLeaf, existingAssets.assets);
    }

    private helper_GetRelationsToIgnore(predicateId: number): AssetBrowserApiHopIgnoreRequestModel[] {
        let ignores: AssetBrowserApiHopIgnoreRequestModel[] = [];

        this.diagram.links.each(r => {
            if (r.data && r.data.intersectUids) {
                r.data.intersectUids.forEach(i => {
                    if (predicateId) {
                        if (predicateId === i.predicateId) {
                            ignores.push({ Uid: i.intersectUid });
                        }
                    }
                    else {
                        ignores.push({ Uid: i.intersectUid });
                    }
                });
            }
        });

        return ignores;
    }

    private helper_HideDeselectedAssetTypes(keysToBeConcernedWith: string[]) {
        // Now loop through selected asset types, as those are the ones we need to hide.
        let nodesToHide: AssetBrowserTranslationNode[] = [];
        this.diagram.model
            .nodeDataArray
            .filter((tn: AssetBrowserTranslationNode) => { return tn.template == "PortGroup" || tn.template == "HiddenData"; })
            .forEach((tn: AssetBrowserTranslationNode) => {
                if (this.displayConfiguration.SelectedAssetTypes.findIndex(v => { return v == tn.assetTypeId; }) > -1) {
                    if (tn.template == "PortGroup") { //only hide if it is already displayed.
                        nodesToHide.push(tn);
                    }
                }
                else {
                    if (keysToBeConcernedWith) {
                        if (keysToBeConcernedWith.findIndex(ix => ix == tn.key) > -1) {
                            this.helper_UnhideNode(tn);
                        }
                    }
                    else {
                        this.helper_UnhideNode(tn);
                    }
                }
            });

        if (nodesToHide.length > 0) {
            nodesToHide.forEach(n => {
                let group: any = this.diagram.findNodeForKey(n.key);
                this.helper_HideNode(n, group);
            });
        }
    }

    private helper_HideDeselectedPredicates(keysToBeConcernedWith: string[]) {
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

                    if (this.displayConfiguration.SelectedPredicates.findIndex(v => { return v == rC.predicateId; }) > -1) {
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
        this.diagram.commitTransaction('predicateBadge');

        //#endregion Badge

        //#region Hide Node

        this.diagram.links.each(link => {
            let linkData: AssetBrowserTranslationLink = link.data as AssetBrowserTranslationLink;
            if (linkData.predicateIds) {
                let g: any = this.diagram.findNodeForKey(linkData.to);
                if (g) {
                    if (linkData.predicateIds.filter(l => {
                        return this.displayConfiguration.SelectedPredicates.findIndex(v => { return v == l; }) > -1
                    }).length > 0) {
                        this.helper_HideNode(g.data as AssetBrowserTranslationNode, g);
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
                            if (this.displayConfiguration.SelectedAssetTypes.findIndex(v => { return v == (g.data as AssetBrowserTranslationNode).assetTypeId; }) == -1) {
                                this.helper_UnhideNode(g.data as AssetBrowserTranslationNode);
                            }
                        }
                    }
                }
            }
        });

        //#endregion
    }

    private helper_HideDeselectedResponsibilityTypes(keysToBeConcernedWith: string[]) {

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

                    if (this.displayConfiguration.SelectedResponsibilityTypes.findIndex(v => { return v == rC.responsibilityTypeId; }) > -1) {
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

        this.diagram.findNodesByExample({ template: function (t) { return (t == "Owners") || (t == "HiddenData"); }}).each(n => {
            let topLevelNode: AssetBrowserTranslationNode = n.data as AssetBrowserTranslationNode;

            //#region Owners node/link logic
            if (topLevelNode.responsibilityTypeId) {
                // We are dealing with an Owners root node.
                let showBadge: boolean = (this.displayConfiguration.SelectedResponsibilityTypes.findIndex(v => { return v == topLevelNode.responsibilityTypeId; }) == -1);
                this.diagram.model.setDataProperty(topLevelNode, "showNode", showBadge);
            }
            //#endregion
        });

        this.diagram.commitTransaction('ownerBadge');

        //#endregion Badge
    }

    private helper_HideNode(node: AssetBrowserTranslationNode, group: any) {
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

        try {
            let children = group.findSubGraphParts();
            children.each(c => {
                hideNode.subgraph.nodes.push(c.data);
            });
        } catch (e) {
            console.log(group);
        }

        this.diagram.model.addNodeData(hideNode);

        let upstreamLinks = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == group.key);
        let downstreamLinks = this.diagramModelAsGraph().linkDataArray.filter(l => l.from == group.key);

        upstreamLinks.forEach(l => {
            hideNode.subgraph.links.push(l);
            this.diagramModelAsGraph().removeLinkData(l);
            this.diagramModelAsGraph().addLinkData({ from: l.from, to: hideNode.key, predicateIds: l.predicateIds, intersectUids: l.intersectUids, responsibilityTypeId: l.responsibilityTypeId });
        });

        downstreamLinks.forEach(l => {
            hideNode.subgraph.links.push(l);
            this.diagramModelAsGraph().removeLinkData(l);
            this.diagramModelAsGraph().addLinkData({ from: hideNode.key, to: l.to, predicateIds: l.predicateIds, intersectUids: l.intersectUids, responsibilityTypeId: l.responsibilityTypeId });
        });

        this.diagram.remove(group);

        this.diagram.commitTransaction('hide');
    }

    private helper_HighlightNodeImpacts(key: string, direction: AssetBrowserApiHopDirection, allRelations: AssetBrowserGenericRelationModel[]) {

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
                        this.helper_HighlightNodeImpacts(l.to, AssetBrowserApiHopDirection.Forward, allRelations);
                    }
                    else {
                        // You have a possible hidden node to deal with.
                        this.helper_HighlightViaHiddenNode(AssetBrowserApiHopDirection.Forward, l.from, allRelations);
                    }
                }
            }

            // Loop through the links to find ones where this node is object, then traverse each one and do the same thing, recursively.
            if (bwd) {
                if (l.to == key) {
                    let sNode = this.diagram.findNodeForKey(l.from);
                    if (sNode) {
                        sNode.isHighlighted = true;
                        this.helper_HighlightNodeImpacts(l.from, AssetBrowserApiHopDirection.Backward, allRelations);
                    }
                    else {
                        // You have a possible hidden node to deal with.
                        this.helper_HighlightViaHiddenNode(AssetBrowserApiHopDirection.Backward, l.to, allRelations);
                    }
                }
            }
        });
    }

    private helper_HighlightPath(e: go.InputEvent, obj: go.Part) {
        try {
            //Set all to not highlighted.
            obj.diagram.nodes.each(n => {
                n.isHighlighted = false;
            });

            if (obj.key) {
                // Highlight the selected node.
                obj.isHighlighted = true;

                // Recurse through and highlight based on the atomic (non-grouped) links.
                this.helper_HighlightNodeImpacts(obj.key.toString(), AssetBrowserApiHopDirection.Both, undefined);
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

    /**
    * Determines if a particular node is Hidden, then interrogate its subgraph to determine the path to continue highlighting.
    * @returns Nothing.
    */
    private helper_HighlightViaHiddenNode(direction: AssetBrowserApiHopDirection, key: string, allRelations: Array<AssetBrowserGenericRelationModel>) {
        let node = this.diagram.findNodeForKey(key);
        if (node) {
            let parentGroup: go.Group = node.containingGroup;
            let fromLinks: go.Iterator<go.Link>;
            while (parentGroup != null) {
                fromLinks = (direction == AssetBrowserApiHopDirection.Backward ? parentGroup.findLinksInto() : parentGroup.findLinksOutOf());
                parentGroup = parentGroup.containingGroup;
            }
            if (fromLinks) {
                fromLinks.each(lnk => {
                    let data: any = (direction == AssetBrowserApiHopDirection.Backward ? lnk.fromNode.data : lnk.toNode.data);
                    let templateName: string = data.template;
                    if (templateName == "HiddenData") {
                        let subgraph: AssetBrowserTranslation = data.subgraph;
                        if (subgraph) {
                            subgraph.nodes.forEach(nd => {
                                // You have found the node, now traverse the hidden links for this node.
                                let relevantRelations = allRelations.filter(r => { return nd.key == (direction == AssetBrowserApiHopDirection.Backward ? r.to : r.from) });
                                relevantRelations.forEach(r => {
                                    let nodeToHighlight = this.diagram.findNodeForKey((direction == AssetBrowserApiHopDirection.Backward ? r.from : r.to));
                                    if (nodeToHighlight) {
                                        nodeToHighlight.isHighlighted = true;
                                    }
                                    this.helper_HighlightNodeImpacts((direction == AssetBrowserApiHopDirection.Backward ? r.from : r.to), direction, allRelations);
                                });
                            });
                        }
                    }
                });
            }
        }
    }

    private helper_InitializeDiagram() {
        this.template_BadgeShapes();

        this.diagram = this.template_Diagram();

        var forelayer = this.diagram.findLayer("Foreground");
        this.diagram.addLayerBefore(this.g(go.Layer, { name: "Links" }), forelayer);

        this.diagram.groupTemplateMap.add("FocalPortGroup", this.template_FocalRootNode());
        this.diagram.groupTemplateMap.add("PortGroup", this.template_RootNode());
        this.diagram.groupTemplateMap.add("Group", this.template_AncestorNode());

        this.diagram.nodeTemplateMap.add("MoreData", this.template_RevealNode());
        this.diagram.nodeTemplateMap.add("HiddenData", this.template_HiddenNode());

        this.diagram.groupTemplateMap.add("Owners", this.template_OwnersRootNode());
        this.diagram.nodeTemplateMap.add("Owner", this.template_LeafOwnerNode());
        this.diagram.nodeTemplate = this.template_LeafAssetNode();

        if (this.helper_LineageDiagramApplies()) {
            this.diagram.linkTemplateMap.add("", this.template_LineageLink());
        }
        else {
            this.diagram.linkTemplateMap.add("", this.template_ImpactLink());
        }

        this.diagram.addDiagramListener('ChangedSelection', e => this.event_DiagramSelectionChanged(e));
        this.diagram.addDiagramListener('ViewportBoundsChanged', e => this.event_ViewportBoundsChanged(e));

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        //this.loadFilter();
        this.helper_PopulateDiagram().subscribe(bComplete => {
            this.helper_HideDeselectedAssetTypes(undefined);
            this.helper_HideDeselectedPredicates(undefined);
            this.helper_HideDeselectedResponsibilityTypes(undefined);
            if (this.searchText !== '') {
                this.search_Execute(this.searchText);
            }
        });
    }

    /**
    * Determines whether the Lineage view is currently selected.
    * @returns A boolean value on whether the lineage view is selected.
    */
    private helper_LineageDiagramApplies(): boolean {
        return (+this.displayConfiguration.DiagramType === +DiagramType.Lineage);
    }

    /**
    * Determines whether the Lineage view is currently selected.
    * @returns A boolean value on whether the lineage view is selected.
    */
    private helper_NumberOfHops(): number {
        let isLineage: boolean = this.helper_LineageDiagramApplies();
        return isLineage ? this.displayConfiguration.NumberOfLineageHops : this.displayConfiguration.NumberOfImpactHops
    }

    private helper_ParseTranslatedData(trans: AssetBrowserTranslation, append: boolean = false) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;

        //#region add data to diagram model

        trans.nodes.forEach(n => {
            n.showIcon = this.displayConfiguration.DisplayIcons;
        });

        if (append) {

            trans.nodes.forEach(n => {
                n.showIcon = this.displayConfiguration.DisplayIcons;
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
            this.diagram.model.setDataProperty(g.data, "showBadges", this.displayConfiguration.DisplayBadges);

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
        this.helper_UpdateDiagramLayout();

        this.helper_CalculateAlertCount();
    }

    private helper_PopulateDiagram(): Observable<boolean> {
        let dgmObs: Observable<boolean>;

        dgmObs = new Observable(obs => {
            let isLineage: boolean = this.helper_LineageDiagramApplies();

            this.isLoading = true;
            this.loadingText = `Retrieving ${isLineage ? 'lineage' : 'impacts'} from Govern..`;
            this.responseModel.clear();
            this.revealedKeys = [];

            this.requestModel = new AssetBrowserApiHopRequestModel();
            this.requestModel.Initial = true;
            this.requestModel.Assets = new Array();
            this.requestModel.RelationsToIgnore = [];

            let assetRequestModel: AssetBrowserApiHopAssetRequestModel = new AssetBrowserApiHopAssetRequestModel();
            assetRequestModel.Uid = this.assetUid;
            this.requestModel.Assets.push(assetRequestModel);

            this.requestModel.Direction = AssetBrowserApiHopDirection.Both;
            this.requestModel.Hops = this.helper_NumberOfHops();
            this.requestModel.LeafOnly = !this.displayConfiguration.IncludeNonLeaf;

            let subscriber = (data: AssetBrowserAssetsModel) => {
                this.responseModel.assets.assets = data.assets;
                this.responseModel.assets.assetRelations = data.assetRelations;
                this.loadingText = "Determining links and meaning...";
                data = this.browserService.convertResponseModel(data, this.displayConfiguration.AncestryMode);

                let trans: AssetBrowserTranslation = new AssetBrowserTranslation();
                trans.nodes = this.browserService.translateAssetNodes(this.displayConfiguration.IncludeNonLeaf, data.assets);
                trans.links = this.browserService.translateAssetLinks(trans.nodes, data.assetRelations);

                this.helper_ParseTranslatedData(trans);
                this.helper_ResizeDiagram();
                this.helper_ScaleDiagram(1);
                this.diagram.alignDocument(go.Spot.Center, go.Spot.Center);
                this.loadingText = "";
                this.isLoading = false;

                this.cdRef.markForCheck();

                obs.next(true);
                obs.complete();
            };

            if (isLineage) {
                this.requestModel.DiagramType = DiagramType.Lineage;
                this.requestModel.HopType = AssetBrowserApiHopType.Lineage;
                this.browserService.getAssetBrowserHop(this.requestModel).subscribe(subscriber);
            }
            else {
                this.requestModel.DiagramType = DiagramType.Impact;
                this.requestModel.HopType = AssetBrowserApiHopType.Impact;
                this.browserService.getImpactBrowserHop(this.requestModel).subscribe(subscriber);
            }
        });

        return dgmObs;
    }

    /**
    * Refreshes the data and diagram to its initially loaded state.
    * @returns Nothing
    */
    private helper_RefreshDiagram(closePanels: boolean = true) {

        // Clear out the current diagram data first.
        this.diagram.startTransaction('RefreshDiagramCommand');
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        dm.nodeDataArray = [];
        dm.linkDataArray = [];
        this.diagram.commitTransaction('RefreshDiagramCommand');


        this.assetUid = this.originalAssetUid;
        this.isLoading = true;
        this.selectedDiagramAsset = null;
        if (closePanels) {
            this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
        }
        this.panel_InformationDisabled = true;
        this.helper_PopulateDiagram().subscribe(bComplete => {
            this.isLoading = false;
            this.helper_SetFilterWindow();
            this.helper_HideDeselectedAssetTypes(undefined);
            this.helper_HideDeselectedPredicates(undefined);
            this.helper_HideDeselectedResponsibilityTypes(undefined);
            //if (this.panelModel.selectedCommand != AssetBrowserPanelCommand.Alerts) {
            this.helper_CalculateAlertCount();
            //}
        });
    }

    /**
    * Resizes the diagram according to the current height of the containing HTML element.
    * @returns Nothing
    */
    private helper_ResizeDiagram() {
        let height = window.innerHeight;
        if (this.isFullScreen)
            this.diagramRef.nativeElement.style.height = (height - 55) + 'px';
        else
            this.diagramRef.nativeElement.style.height = (height - 265) + 'px';

        this.helper_DisableDragging();
    }

    /**
    * Based on the reveal node clicked, we determine the leaf asset that the reveal node is attached to, 
    * then get the next hop of lineage, whether backward or forward.
    * @returns Nothing
    */
    private helper_RevealLineageHop(e: go.InputEvent, obj: go.GraphObject) {
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
            let realRootAsset = this.helper_FindTrueRootAssetInCollection(currentTopGroupKey, undefined, undefined);

            model.Initial = false;
            model.Hops = 1;
            model.LeafOnly = !this.displayConfiguration.IncludeNonLeaf;
            model.DiagramType = DiagramType.Lineage;
            model.HopType = AssetBrowserApiHopType.Lineage;
            model.Assets = data.assetUids;

            // Get relations to ignore.
            model.RelationsToIgnore = this.helper_GetRelationsToIgnore(undefined);

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

                    response = this.browserService.convertResponseModel(response, this.displayConfiguration.AncestryMode);

                    let trans: AssetBrowserTranslation = new AssetBrowserTranslation();
                    trans.nodes = this.browserService.translateAssetNodes(this.displayConfiguration.IncludeNonLeaf, response.assets);
                    trans.links = this.browserService.translateAssetLinks(this.helper_GetFullResponseModelAsTranslationNodes(), response.assetRelations);

                    let modelsToSetReveal: AssetBrowserAssetModel[] = [];
                    modelsToSetReveal.push(realRootAsset);
                    this.helper_SetRevealKeyInHierarchy(modelsToSetReveal);

                    this.helper_ParseTranslatedData(trans, true);

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

                    this.helper_SetFilterWindow();

                    this.helper_HideDeselectedAssetTypes(undefined);
                    this.helper_HideDeselectedPredicates(undefined);
                    this.helper_HideDeselectedResponsibilityTypes(undefined);

                });
        }
    }

    private helper_ScaleDiagram(_scale: number) {
        this.diagram.scale = _scale;
        this.scale = _scale;
    }

    private helper_SetFilterWindow() {
        let loadedTypes: LoadedFilterTypesModel = this.helper_DetermineLoadedFilterOptions();

        this.filter_AvailableOptions = new FilterSelectionsModel([], [], []);
        this.filter_AvailableOptions.AncestryOptions = this.filter_AllOptions.AncestryOptions;
        this.filter_AvailableOptions.AssetTypeOptions = this.filter_AllOptions.AssetTypeOptions;
        this.filter_AvailableOptions.HopOptions = this.filter_AllOptions.HopOptions;
        this.filter_AvailableOptions.PredicateOptions = this.filter_AllOptions.PredicateOptions;
        this.filter_AvailableOptions.ResponsibilityTypeOptions = this.filter_AllOptions.ResponsibilityTypeOptions;

        //#region Asset Types

        this.filter_AvailableOptions.FilterAssetTypes = [];
        this.filter_AvailableOptions.AssetTypeOptions.forEach(at => {
            let inLoadedAssetTypes: boolean = loadedTypes.AssetTypes.findIndex(ix => { return ix == at.AssetTypeId }) > -1;
            if (inLoadedAssetTypes) {
                this.filter_AvailableOptions.FilterAssetTypes.push({
                    label: at.Path,
                    data: at.AssetTypeId
                });
            }
        });
        this.filter_AvailableOptions.FilterAssetTypes.sort((a, b) => (a.label > b.label) ? 1 : -1);
        //this.selectedFilterAssetTypes = this.helper_GetTreeNodeSelectionNodes(this.displayConfiguration.SelectedAssetTypes, this.filter_AvailableOptions.FilterAssetTypes);

        //#endregion

        //#region Predicates

        this.filter_AvailableOptions.FilterPredicates = [];
        this.filter_AvailableOptions.PredicateOptions.forEach(p => {
            let inLoadedPredicates: boolean = loadedTypes.Predicates.findIndex(ix => { return ix == p.Id }) > -1;
            if (inLoadedPredicates) {
                this.filter_AvailableOptions.FilterPredicates.push({
                    label: p.Name.substring(0, 50) + ' / ' + p.Inverse.substring(0, 50),
                    data: p.Id
                });
            }
        });
        this.filter_AvailableOptions.FilterPredicates.sort((a, b) => (a.label > b.label) ? 1 : -1);
        //this.selectedFilterPredicates = this.helper_GetTreeNodeSelectionNodes(this.displayConfiguration.SelectedPredicates, this.filter_AvailableOptions.FilterPredicates);

        //#endregion

        //#region Responsibility Types

        this.filter_AvailableOptions.FilterResponsibilityTypes = [];
        this.filter_AvailableOptions.ResponsibilityTypeOptions.forEach(p => {

            let inLoadedResponsibilityTypes: boolean = loadedTypes.ResponsibilityTypes.findIndex(ix => { return ix == p.Id }) > -1;
            if (inLoadedResponsibilityTypes) {
                let thisResponsibilityTypeNode: TreeNode = {
                    label: p.Name,
                    data: p.Id,
                    children: []
                };
                this.filter_AvailableOptions.FilterResponsibilityTypes.push(thisResponsibilityTypeNode);
            }

        });
        this.filter_AvailableOptions.FilterResponsibilityTypes.sort((a, b) => (a.label > b.label) ? 1 : -1);
        //this.selectedFilterResponsibilityTypes = this.helper_GetTreeNodeSelectionNodes(this.displayConfiguration.SelectedResponsibilityTypes, this.filter_AvailableOptions.FilterResponsibilityTypes);

        //#endregion

        this.cdRef.markForCheck();
    }

    /**
    * Traverses an asset's hierarchy and sets each assets' reveal property to NONE.
    * @returns Nothing.
    */
    private helper_SetRevealKeyInHierarchy(models: AssetBrowserAssetModel[]) {
        models.forEach(t => {
            t.reveal = AssetBrowserApiHopDirection.None;
            if (t.items) {
                this.helper_SetRevealKeyInHierarchy(t.items);
            }
        });
    }

    private helper_SetVisiblePanel(command: AssetBrowserPanelCommand, overrideCloseCheck: boolean = false) {

        if (!overrideCloseCheck) {
            if (this.panelModel.selectedCommand == command) {
                command = AssetBrowserPanelCommand.None;
            }
        }

        this.panelModel = {
            selectedCommand: command,
            AddVisible: (command == AssetBrowserPanelCommand.Add),
            AlertVisible: (command == AssetBrowserPanelCommand.Alerts),
            FiltersVisible: (command == AssetBrowserPanelCommand.Filters),
            InformationVisible: (command == AssetBrowserPanelCommand.Information),
            SettingsVisible: (command == AssetBrowserPanelCommand.Settings)
        };
    }

    private helper_ShowDetail(assetUid: string) {
        this.panel_TabIndex = 0;

        this.panel_Loading = true;
        this.browserService.getDetailByAsset(assetUid).subscribe(response => {
            this.selectedDiagramAsset = response;
            this.selectedDiagramAsset.Loaded = true;
            this.selectedDiagramAsset.Url = "/" + this.selectedDiagramAsset.Url;
            this.panel_Loading = false;
            this.cdRef.markForCheck();
        });
    }

    /**
     * Sorts go.Parts based on their display names
     */
    private helper_SortParts(a: go.Part, b: go.Part): number {
        if (a == null || b == null || a.data == null || b.data == null)
            return 0;

        let al = a.data.text ? a.data.text.toLowerCase() : '';
        let bl = b.data.text ? b.data.text.toLowerCase() : '';

        if (al > bl)
            return 1;
        else if (al < bl)
            return -1;
        else
            return 0;
    }

    private helper_UnhideNode(node: AssetBrowserTranslationNode) {
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

    private helper_UpdateDiagramLayout() {
        this.diagram.layout.invalidateLayout();
        this.diagram.requestUpdate();
    }

    private helper_UpdateDiagramType(dt: DiagramType) {
        let model: AssetBrowserFilterModel = _.cloneDeep(this.displayConfiguration);
        model.DiagramType = dt;
        this.displayConfiguration = model;
    }

    private helper_UpdateVisualization(): void {
        this.saveFilter();
        this.isLoading = true;
        this.loadingText = "Determining links and meaning...";
        let assetData = this.browserService.convertResponseModel(this.responseModel.assets, this.displayConfiguration.AncestryMode);

        let trans: AssetBrowserTranslation = new AssetBrowserTranslation();
        trans.nodes = this.browserService.translateAssetNodes(this.displayConfiguration.IncludeNonLeaf, assetData.assets);
        trans.links = this.browserService.translateAssetLinks(trans.nodes, assetData.assetRelations);

        this.helper_ParseTranslatedData(trans);

        this.helper_ResizeDiagram();
        this.diagram.zoomToFit();
        this.diagram.alignDocument(go.Spot.Center, go.Spot.Center);
        this.loadingText = "";
        this.isLoading = false;

        this.helper_SetFilterWindow();

        this.helper_HideDeselectedAssetTypes(undefined);
        this.helper_HideDeselectedPredicates(undefined);
        this.helper_HideDeselectedResponsibilityTypes(undefined);
    }

    private panels_Click(e: AssetBrowserPanelCommand) {
        switch (e) {
            case AssetBrowserPanelCommand.Add:
                this.helper_SetVisiblePanel(e);
                break;
            case AssetBrowserPanelCommand.Alerts:
                this.panel_TabIndex = 0;
                this.helper_SetVisiblePanel(e);
                if (this.selectedDiagramAsset) {
                    this.selectedAssetsWithAlerts = [this.selectedDiagramAsset.Uid];
                    //this.selectedAssetsWithAlerts.push(this.selectedDiagramAsset.Uid);
                }
                else {
                    this.selectedAssetsWithAlerts = this.assetsWithAlerts;
                }
                break;
            case AssetBrowserPanelCommand.Download:
                this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
                let image_data = this.diagram.makeImageData({
                    scale: 1,
                    returnType: "blob",
                    background: "#fff",
                    callback: (image_data) => this.panels_Download_Callback(image_data, this.assetUid)
                });
                break;
            case AssetBrowserPanelCommand.Filters:
                this.helper_SetVisiblePanel(e);
                this.helper_SetFilterWindow();
                break;
            case AssetBrowserPanelCommand.FullScreen:
                this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
                this.isFullScreen = !this.isFullScreen;
                this.helper_ResizeDiagram();
                break;
            case AssetBrowserPanelCommand.Information:
                this.helper_SetVisiblePanel(e);
                let allowInformationPopup: boolean = false;

                if (this.selectedDiagramAsset) {
                    allowInformationPopup = (this.selectedDiagramAsset.Uid != this.emptyUid);
                }
                
                if (allowInformationPopup) {
                    if (this.selectedDiagramAsset != null) {
                        this.helper_ShowDetail(this.selectedDiagramAsset.Uid);
                    }
                }
                else {
                    this.panelModel.selectedCommand = AssetBrowserPanelCommand.None;//this.panelModel = { commandToResetTo: AssetBrowserPanelCommand.None };
                }
                break;
            case AssetBrowserPanelCommand.Refresh:
                this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
                this.helper_RefreshDiagram();
                break;
            case AssetBrowserPanelCommand.Settings:
                this.helper_SetVisiblePanel(e);
                break;
        }
    }

    private panels_Download_Callback(image_data, assetUid) {
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

    private savedfilter_Apply(e: AssetBrowserFilterModel) {
        let diagramTypeChanged: boolean = (this.displayConfiguration.DiagramType !== e.DiagramType);
        this.displayConfiguration = new AssetBrowserFilterModel();
        this.displayConfiguration = e;

        if (diagramTypeChanged) {
            this.diagram.div = null;
            this.helper_InitializeDiagram();
        }
        else {
            this.helper_RefreshDiagram(false);
        }
        this.cdRef.markForCheck();
    }

    search_AddHighlightToNode(node: go.Node, phrase: string) {
        this.diagram.model.commit(function (m) {
            var data = m.findNodeDataForKey(node.key);

            var idx = phrase.length;
            var highlight = data.text.substring(0, idx);
            var text = data.text.substring(idx, data.text.length);

            if (data.text.length > idx && (data.text[idx] == ' ' || phrase[idx - 1] == ' ')) {
                m.set(data, 'spacer_visible', true);
            }
            m.set(data, 'highlight', highlight);
            m.set(data, 'highlight_visible', true);
            m.set(data, 'text', text);
        }, 'update_highlight');
    }

    /**
    * Responds to the search event from the shared Asset Browser Searchbar control.
    * @returns Nothing
    */
    private search_Execute(phrase: string) {

        // Clear highlights of exisitng search results
        this.searchResults.forEach(n => {
            this.search_RemoveHighlightFromNode(n);
        });

        this.searchText = phrase;
        let foundResults: go.Node[] = [];
        this.searchResults = [];

        this.helper_ScaleDiagram(1);//this.diagram.zoomToFit();
        var self = this;

        this.diagram.nodes.each(function (node) {
            if (node instanceof go.Node) {
                var nodeData = node.data;
                node.isHighlighted = false;
                if (nodeData.isGroup) {
                    //This is grouping, do nothing with it (AssetType grouping)
                }
                else if (phrase != '') {
                    self.searchableProps.forEach(prop => {
                        if (node.data[prop] && node.data[prop].toLowerCase().indexOf(phrase.toLowerCase()) == 0) {
                            foundResults.push(node);
                            self.search_AddHighlightToNode(node, phrase);
                            self.search_ExpandGroups(node.data.group);
                        }
                    });
                }
            }
        });

        this.searchResults = foundResults;

        this.search_GoToResult(1);
        this.cdRef.markForCheck();
    }

    search_ExpandGroups(groupName) {
        if (groupName) {
            var group = this.diagram.findPartForKey(groupName) as go.Group;
            group.expandSubGraph();
            this.search_ExpandGroups(group.data.group);
        }
    }

    /**
    * Responds to the next/previous event from the shared Asset Browser Searchbar control.
    * @returns Nothing
    */
    private search_GoToResult(position: number) {
        var node = this.searchResults[position - 1];
        if (node) {
            this.diagram.centerRect(node.actualBounds);
            this.diagram.select(node);
            this.search_SetFocusedNodeHighlight(node);
        }
    }

    search_RemoveHighlightFromNode(node: go.Node) {
        try {
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
        } catch (e) {

        }
    }

    search_SetFocusedNodeHighlight(node: go.Node) {
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

    private settingspanel_Apply(e: AssetBrowserFilterChangeEvent) {
        this.displayConfiguration = e.Model;
        this.saveFilter();
        switch (e.Type) {
            case AssetBrowserFilterChangeEventType.AllBadges:
                this.diagram.startTransaction();
                this.diagram.findTopLevelGroups().each(g => {
                    this.diagram.model.setDataProperty(g.data, "showBadges", this.displayConfiguration.DisplayBadges);
                });
                this.diagram.commitTransaction();
                break;
            case AssetBrowserFilterChangeEventType.AncestorBadges:
                this.helper_RefreshDiagram(false);
                break;
            case AssetBrowserFilterChangeEventType.Icons:
                this.diagram.startTransaction();
                this.diagram.model.nodeDataArray.forEach(d => {
                    this.diagram.model.setDataProperty(d, "showIcon", this.displayConfiguration.DisplayIcons);
                });
                this.diagram.commitTransaction();
                break;
            case AssetBrowserFilterChangeEventType.Scores:

                break;
        }
    }

    private template_AncestorNode(): go.Group {

        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.template_ContextMenu(),
                click: (e, obj) => this.helper_HighlightPath(e, obj as any),
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                stretch: go.GraphObject.Horizontal,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4),
                            comparer: (a, b) => this.helper_SortParts(a, b)
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
                            toolTip: this.template_Tooltip()
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

    private template_BadgeShapes() {
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

    private template_ContextMenu(): go.Adornment {
        return this.g(
            "ContextMenu",
            { areaBackground: "#ffffff", background: "#ffffff" },
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Navigate to", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                {
                    click: (e, obj) => {
                        let assetUidRedirect: string = '';
                        assetUidRedirect = obj.part.data.assetUid;
                        if (assetUidRedirect == this.assetUid)
                            return;

                        this.router.navigateByUrl('/bla', { skipLocationChange: true }).then(() => {
                            this.router.navigate([SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT, 'browser', assetUidRedirect]);
                        });
                    }
                },
                new go.Binding("visible", "", (o) => (o.part.data.assetUid !== this.emptyUid && o.part.data.hasAssetReadAccess)).ofObject()
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Show Details", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenuhelper_ShowDetails }),
                {
                    click: (e, obj) => {
                        if (obj.part.data.assetUid != null && obj.part.data.assetUid != this.emptyUid) {
                            this.helper_SetVisiblePanel(AssetBrowserPanelCommand.Information, true);
                            this.helper_ShowDetail(obj.part.data.assetUid);
                        }
                    }
                },
                new go.Binding("visible", "", (o) => (o.part.data.assetUid !== this.emptyUid && o.part.data.hasAssetReadAccess)).ofObject()
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.context_Hide(e, obj) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Upstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.context_Hide(e, obj, AssetBrowserApiHopDirection.Backward) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Downstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.context_Hide(e, obj, AssetBrowserApiHopDirection.Forward) }
            )//,
            //this.g(
            //    "ContextMenuButton",
            //    this.g(go.TextBlock, { text: "Isolate", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
            //    { click: function (e, obj) { alert("Not yet implemented") } }
            //)
        );
    }

    private template_Diagram(): go.Diagram {

        let layout: go.Layout;

        if (this.helper_LineageDiagramApplies()) {
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

    private template_FocalPositioningHelper(spot, row, col, textprop, visprop) {
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

    private template_FocalRootNode(): go.Group {
        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.template_ContextMenu(),
                click: (e, obj) => this.helper_HighlightPath(e, obj as any),
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4),
                            sorting: go.GridLayout.Ascending,
                            comparer: (a, b) => this.helper_SortParts(a, b)
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
                    this.template_FocalPositioningHelper(go.Spot.Top, 0, 1, "topNodeText", "hasTop"),
                    this.template_FocalPositioningHelper(go.Spot.Left, 2, 0, "leftNodeText", "hasLeft"),
                    this.g(go.Panel,
                        "Auto",
                        { row: 2, column: 1 },
                        this.template_RootNodeContent()
                    ),
                    this.template_FocalPositioningHelper(go.Spot.Right, 2, 2, "rightNodeText", "hasRight"),
                    this.template_FocalPositioningHelper(go.Spot.Bottom, 3, 1, "bottomNodeText", "hasBottom")
                )
            )
        );
    }

    private template_HiddenNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                click: (e, obj) => this.context_Unhide(e, obj),
                cursor: 'pointer'
            },
            new go.Binding("visible", "showNode"),
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

    private template_ImpactBadges(): go.Panel {
        return this.g(go.Panel, "TableRow", {
            alignment: go.Spot.TopCenter,
            alignmentFocus: go.Spot.Bottom,
            padding: 0,
            cursor: "pointer",
            click: (e, obj) => this.badge_ClickImpact(e, obj),
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

    private template_ImpactLink(): go.Link {
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

    private template_LeafAssetNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                contextMenu: this.template_ContextMenu(),
                click: (e, obj) => this.helper_HighlightPath(e, obj as any)
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
                        editable: false,
                        font: this.fontLabel,
                        stroke: this.fontLabelColor,
                        visible: false,
                        maxLines: this.textMaxLines,
                        maxSize: this.textMaxSize,
                        overflow: this.textOverflowStyle,
                        toolTip: this.template_Tooltip(),
                        margin: 0
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
                        toolTip: this.template_Tooltip()
                    },
                    new go.Binding("text", "text").makeTwoWay(),
                    new go.Binding("stroke", "actionCount", (v) => (v > 0) ? this.fontLabelAlertColor : this.fontLabelColor)
                )
            )  // end Horizontal Panel
        );
    }

    private template_LeafOwnerNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                contextMenu: this.template_ContextMenu(),
                click: (e, obj) => this.helper_HighlightPath(e, obj as any)
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
                        editable: false,
                        font: this.fontLabel,
                        stroke: this.fontLabelColor,
                        visible: false,
                        maxLines: this.textMaxLines,
                        maxSize: this.textMaxSize,
                        overflow: this.textOverflowStyle,
                        toolTip: this.template_Tooltip(),
                        margin: 0
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
                        toolTip: this.template_Tooltip()
                    },
                    new go.Binding("text", "text").makeTwoWay()
                )
            )  // end Horizontal Panel
        );
    }

    private template_LineageLink(): go.Link {
        return this.g(
            go.Link, {
            routing: go.Link.AvoidsNodes,
            corner: 5,
            relinkableFrom: false,
            relinkableTo: false,
            click: (e, obj) => this.helper_HighlightPath(e, obj as any),
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

    private template_OwnerBadges(): go.Panel {
        return this.g(go.Panel, "TableRow", {
            alignment: go.Spot.TopCenter,
            alignmentFocus: go.Spot.Bottom,
            padding: 0,
            cursor: "pointer",
            click: (e, obj) => this.badge_ClickOwner(e, obj),
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

    private template_OwnersRootNode(): go.Group {
        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.template_ContextMenu(),
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4),
                            comparer: (a, b) => this.helper_SortParts(a, b)
                        }
                    )
            },
            new go.Binding("visible", "showNode"),
            this.g(
                go.Panel,
                "Vertical",
                this.g(go.Panel, "Table",
                    new go.Binding("itemArray", "relations"),
                    new go.Binding("visible", "showBadges"),
                    {
                        itemTemplate: this.template_ImpactBadges()
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
                                    toolTip: this.template_Tooltip()
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

    private template_RevealNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                click: (e, obj) => this.helper_RevealLineageHop(e, obj),
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

    private template_RootNode(): go.Group {
        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.template_ContextMenu(),
                click: (e, obj) => this.helper_HighlightPath(e, obj as any),
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4),
                            comparer: (a, b) => this.helper_SortParts(a, b)
                        }
                    )
            },
            this.template_RootNodeContent()
        );
    }

    private template_RootNodeContent(): go.Panel {
        return this.g(
            go.Panel,
            "Vertical",
            this.g(go.Panel, "Table",
                new go.Binding("itemArray", "relations"),
                new go.Binding("visible", "showBadges"),
                {
                    itemTemplate: this.template_ImpactBadges()
                }
            ),
            this.g(go.Panel, "Table",
                new go.Binding("itemArray", "owners"),
                new go.Binding("visible", "showBadges"),
                {
                    itemTemplate: this.template_OwnerBadges()
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
                                toolTip: this.template_Tooltip()
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

    private template_Tooltip(): go.Adornment {
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

    /**
    * Responds to the change event from the shared Asset Browser ViewChange control.
    * @returns The DiagramType.
    */
    private viewchange_Apply(e: DiagramType) {
        this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
        this.panelModel.selectedCommand = AssetBrowserPanelCommand.None;
        this.helper_UpdateDiagramType(e);
        this.saveFilter();
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT}/browser/${this.assetUid}/${DiagramType[e]}`);
    }

    /**
    * Responds to the change event from the shared Asset Browser Zoom control.
    * @returns Nothing.
    */
    private zoom_Change(_scale: number) {
        this.helper_ScaleDiagram(_scale);
    }
} 