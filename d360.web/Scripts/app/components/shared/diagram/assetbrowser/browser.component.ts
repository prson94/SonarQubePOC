import * as go from 'gojs';
import * as _ from 'lodash';
import { AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChange, SimpleChanges, EventEmitter, Output, AfterViewChecked } from '@angular/core';
import {
    AssetBrowserApiHopDirection,
    AssetBrowserDiagramAsset,
    AssetBrowserTranslationNode,
    AssetBrowserTranslationLink,
    AssetBrowserTranslationRelationCount,
    AssetBrowserFilterModel,
    FilterSelectionsModel,
    AssetBrowserApiHopAssetRequestModel,
    AssetBrowserTranslationOwnerCount,
    AssetBrowserGenericRelationModel,
    LoadedFilterTypesModel,
    AssetBrowserAlert,
    DiagramType,
    AssetBrowserFilterChangeEventType,
    AssetBrowserFilterChangeEvent,
    AssetBrowserPanelCommand,
    AssetBrowserPanelModel,
    DiagramTypesModel,
    FilterAncestryMode,
    AssetBrowserResponseModel,
    AssetBrowserLineageRequest,
} from '../../../../models/lineage.model';

import { BrowserService } from '../../../../services/browser.service';
import { PermissionsService } from '../../../../services/permissions.service';
import { MessagesObservableService } from '../../../../services/messages-observable.service';


import { DiagramBaseComponent } from '../diagram-base.component';
import { TreeNode } from 'primeng/api';
import { forkJoin, Observable, Subscription } from 'rxjs';
import { PredicatesService } from '../../../../services/predicates.service';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { Router, ActivatedRoute } from '@angular/router';
import { SiteUrlHelpers } from '../../../../static/site-url-helpers';
import { ProcessDiagramComponent } from '../process-diagram/process-diagram.component';
import { ProcessService } from '../../../../services/process.service';
import { AssetBrowserOverviewComponent } from './tools/overview.component';
import { FontAwesomeHelper } from '../../../../static/font-awesome-helper';
import { FieldsObservableService } from '../../../../services/fieldsObservable.service';
import { AssetService } from '../../../../services/asset.service';
import { ResponsibilityService } from '../../../../services/responsibility.service';
import { ObjectStatisticsService } from '../../../../services/object-statistics.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { CompanySettingEnum } from '../../../../models/settings.model';
import { AssetDetailComponent } from '../../asset-detail/asset-detail.component';
import { LinkClickInterceptor } from '../../../../services/href-click-service';

declare var window: any;
@Component({
    selector: 'd3s-assetbrowser',
    templateUrl: './browser.component.html',
    providers: [
        BrowserService,
        PermissionsService,
        PredicatesService,
        ProcessService,
        FieldsObservableService,
        AssetService,
        ResponsibilityService,
        ObjectStatisticsService,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserComponent extends DiagramBaseComponent implements OnInit, AfterViewInit, AfterViewChecked {
    @Input() readonly = true;
    @Input() assetUid: string;

    @Output() saveStateChanged: EventEmitter<any> = new EventEmitter<any>();

    @ViewChild('addLineagePanel', { static: false }) addLineagePanelRef;
    @ViewChild('alertPanel', { static: false }) alertPanelRef;
    @ViewChild('infoDetailPanel', { static: false }) infoDetailPanelRef;
    @ViewChild('settingsPanel', { static: false }) settingsPanelRef;
    @ViewChild('ownerDetailPanel', { static: false }) ownerDetailPanelRef;
    @ViewChild('diagram', { static: false }) diagramRef;
    @ViewChild('filterDetailPanel', { static: false }) filterDetailPanelRef;
    @ViewChild('processDiagram', { static: false }) processDiagramRef: ProcessDiagramComponent;
    @ViewChild('overview') overviewControlRef: AssetBrowserOverviewComponent;

    @ViewChild('relationshipBadges', { static: false }) relationshipBadgesRef: ElementRef;
    @ViewChild('relationshipBadgesTooltip', { static: false }) relationshipBadgesTooltipRef: ElementRef;

    @ViewChild('assetDetailComponent', { static: false }) assetDetailComponent: AssetDetailComponent;

    private diagramData: AssetBrowserResponseModel;

    private originalAssetUid: string;

    alerts: AssetBrowserAlert[] = [];
    assetsWithAlerts: string[] = [];
    selectedAssetsWithAlerts: string[] = [];
    totalAlertCount = 0;
    showEditor: boolean = false;

    diagramTypeSpecifiedInPath = DiagramType.Lineage;
    isDiagramTypeSpecifiedInPath = false;

    selectedDiagramAsset: AssetBrowserDiagramAsset;
    highlightedPart: go.Part;
    isFullScreen = false;
    loadingText = '';

    isError: boolean = false;
    errorText = '';

    searchText = '';
    searchResults: go.Node[] = [];
    searchableProps: string[] = ["text"];

    panel_Loading = false;
    panel_InformationDisabled = false;
    panel_InformationHasReadAccess = false;
    panel_TabIndex = 0;
    linkMenuItems: any[] = [
        { title: $localize`Open` },
        { title: $localize`Open in New Tab` },
    ];

    panelModel: AssetBrowserPanelModel = { selectedCommand: AssetBrowserPanelCommand.None, AddVisible: false, AlertVisible: false, FiltersVisible: false, InformationVisible: false, SettingsVisible: false };

    displayConfiguration: AssetBrowserFilterModel = new AssetBrowserFilterModel();
    private readonly displayConfigurationKey = 'asset-browser-configuration';
    private storage = window.sessionStorage;
    scale = 1;
    filter_AvailableOptions: FilterSelectionsModel = new FilterSelectionsModel([], [], []);
    filter_AllOptions: FilterSelectionsModel = new FilterSelectionsModel([], [], []);
    diagramTypes: DiagramTypesModel = null;

    showNodeCount: boolean = true;
    autoCollapseNodeCount: number = 10; //0 or less disables auto-collapse

    autoCollapseRelationshipCount: number = 3;
    performanceLinkMode: boolean = false;
    maxLinkCountToAvoidNodesTemplate: number = 2;

    popupMenuItems = [
        {
            title: $localize`Export to excel`,
            callback: () => this.processDiagramRef.doControlledAction('export')
        },
        {
            title: $localize`Replace diagram`,
            callback: () => this.processDiagramRef.doControlledAction('open-diagram-replace')
        }
    ]

    //#region Constants

    private readonly emptyUid: string = '00000000-0000-0000-0000-000000000000';
    private readonly fontContextMenu: string = "12px 'Precisely'";
    private readonly fontContextMenuhelper_ShowDetails: string = "bold 12px 'Precisely'";

    private readonly fontOwnerBackColor: string = "#FEF6F2";
    private readonly fontOwnerBadgeLabelBorderColor: string = "#DE4B00";
    private readonly fontOwnerBadgeLabelBackColor: string = "#FFE5D0";

    private readonly fontLabelIcon: string = "12px FontAwesome";
    private readonly fontLabelAlertColor: string = "#FF0000";
    private readonly fontLabel: string = "14px 'Precisely'";
    private readonly fontLabelColor: string = "#404040";
    private readonly fontLink: string = "9pt 'Precisely'";
    private readonly fontLinkColor: string = "#fff";
    private readonly linkBackColor: string = "#808080";
    private readonly lightenBoxColor: string = "#fff";
    private readonly linkDefaultBackColor: string = '#808080';
    private readonly linkDefaultBorderColor: string = '#999';
    private readonly plusIcon: string = '\uf067';
    private readonly hideIcon: string = '\uf070';
    private readonly disabledNodeBackColor: string = '#fff';

    private readonly textMaxSize = new go.Size(200, Infinity);
    private readonly textMaxLines = 1;
    private readonly textOverflowStyle = go.TextBlock.OverflowEllipsis;

    private readonly searchHighlightColour: string = '#FFDA00';
    private readonly searchHighlightColourFocused: string = '#FD7E0E';
    private readonly selectionPathHighlightColor: string = '#F5C2FF';
    private readonly leafBackColor: string = 'transparent';

    private readonly badgeFont: string = "14px 'Precisely'";
    private readonly badgeStrokeColor = "#d6d5d5";
    private readonly badgeTextColor = "#6d18dd";
    private readonly ignoredPanelFieldTypes = [
        "Tag",
        "Relationship",
        "FieldFromRelationship",
        "Score",
        "ComputedRelationshipLookup",
        "DataTableSelect",
        "File",
        "JsonElement",
        "ComputedOwnershipLookup",
        "Path",
        "RefListRelationship"
    ];

    //#endregion

    hrefSub: Subscription;
    selectedAsset: any;
    selectedReferenceItem: any;
    selectedTag: any;

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
        private cdRef: ChangeDetectorRef,
        private processService: ProcessService,
        private fieldsService: FieldsObservableService,
        private assetService: AssetService,
        private responsibilityService: ResponsibilityService,
        private objectStatisticsService: ObjectStatisticsService,
        protected settingsService: CompanySettingsService,
        private linkClickInterceptor: LinkClickInterceptor
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.selectedDiagramAsset = null;
            this.linkClickInterceptor.handleEvent(this, ev);
        });

        this.maxLinkCountToAvoidNodesTemplate = settingsService.getSettingById(CompanySettingEnum.DiagramMaxAvoidNodesLinkCount).NumberSetting.Value;
    }

    public ngOnInit() {

        this.originalAssetUid = this.assetUid;

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

                this.browserService.getDiagramTypes(this.originalAssetUid)
                    .subscribe(res => {
                        this.diagramTypes = res;

                        if (params['diagramType']) {
                            let diagramTypeParameterValue: string = params['diagramType'];

                            this.isDiagramTypeSpecifiedInPath = (diagramTypeParameterValue in DiagramType);
                            if (!this.isDiagramTypeSpecifiedInPath || !this.diagramTypes.items.some(x => x.value == DiagramType[diagramTypeParameterValue])) {
                                diagramTypeParameterValue = DiagramType[this.diagramTypes.initial];
                            }

                            this.diagramTypeSpecifiedInPath = DiagramType[diagramTypeParameterValue];
                            this.helper_UpdateDiagramType(this.diagramTypeSpecifiedInPath);
                        } else {
                            this.helper_UpdateDiagramType(this.diagramTypes.initial);

                        }

                        if (this.diagram) this.diagram.div = null;

                        if (this.displayConfiguration.DiagramType != DiagramType.Process)
                            this.helper_InitializeDiagram();

                    });
            }
        );
    }

    public ngAfterViewInit() {
        if (this.displayConfiguration.DiagramType == DiagramType.Process)
            return;

        this.helper_ResizeDiagram();
        this.cdRef.markForCheck();
    }

    public ngAfterViewChecked() {
        if (this.displayConfiguration.DiagramType == DiagramType.Process)
            return;

        const panelHeaderElement: HTMLElement = this.myElement.nativeElement.querySelectorAll('.asset-browser-window-header')[0];
        const panelElements: HTMLElement[] = this.myElement.nativeElement.querySelectorAll('.asset-browser-window');

        (function () {
            if (typeof NodeList.prototype.forEach === "function") return false;
            panelElements.forEach = Array.prototype.forEach;
        })();
        const diagramSize = +this.diagramRef.nativeElement.style.height.replace('px', '');
        panelElements.forEach(el => {
            el.style.height = diagramSize + 'px';
            el.style.maxHeight = diagramSize + 'px';
            const panelHeaderSize = panelHeaderElement.clientHeight;
            const innerPanelHeight = (diagramSize - panelHeaderSize - 1) + (this.isFullScreen ? -48 : 0) + 'px';

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
            if (this.settingsPanelRef) {
                this.settingsPanelRef.nativeElement.style.height = innerPanelHeight;
            }
        });
        this.updatePredicateSelectorPosition();
    }

    public ngOnDestroy() {
        if (this.diagram)
            this.diagram.div = null;    // Garbage collection.
        if (this.cdRef)
            this.cdRef.detach();
    }

    public canEditProcessDiagram() {
        return this.displayConfiguration.DiagramType == DiagramType.Process
            && (this.diagramTypes && this.diagramTypes.items && this.diagramTypes.items.some(x => x.value == DiagramType.Process && x.canEdit));
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
        if (this.diagramTypeSpecifiedInPath == DiagramType.Process)
            return;

        this.helper_ResizeDiagram();
    }

    @HostListener('document:keydown', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        if (event.key === "Escape" || event.key === "Esc") {
            this.isFullScreen = false;
            this.helper_ResizeDiagram();
            this.cdRef.markForCheck();
        }
    }

    @HostListener('document:click', ['$event']) onDocumentClick(event: MouseEvent) {
        if (this.isRelationshipSelectorAvailable) {
            if (!this.relationshipBadgesRef.nativeElement.contains(event.target)
                && !(event.target as HTMLElement).classList.contains("rel-expand-icon")
            ) {
                this.isRelationshipSelectorAvailable = false;
                this.diagram.model.setDataProperty(this.followPart.data, "relExpanded" + this.relationshipSelectorType, false);
            }
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

    private badge_RemoveDependentNodes(badgeIdentifier: string, direction: AssetBrowserApiHopDirection) {
        let links = this.diagramModelAsGraph().linkDataArray;
        let badgeLinks = links.filter(l => { return l.badgeIdentifier === badgeIdentifier; });
        badgeLinks.forEach(badgeLink => {
            if (badgeLink) {
                // Line below would only be used IF impacts were to go in both directions. As it is now, we hard-code them to only go in one direction (forward).
                let impactNodeKey = direction == AssetBrowserApiHopDirection.Backward ? badgeLink.from : badgeLink.to;
                //let impactNodeKey = badgeLink.to;
                let impactNode = this.diagram.findNodeForKey(impactNodeKey);
                if (impactNode) {
                    let impactData = impactNode.data as AssetBrowserTranslationNode;
                    if (impactData) {
                        // First, remove this node from the hierarchy collection, which represents all root nodes currently in the diagram.
                        let ixToDelete = this.diagramData.hierarchy.findIndex(o => { return o.hierarchyKey === impactNodeKey; });
                        if (ixToDelete > -1) {
                            this.diagramData.hierarchy.splice(ixToDelete, 1);
                            ixToDelete = -1;
                        }
                        // Next, remove this and all descendant nodes from the nodes collection, which is a flat list of nodes currently in the diagram.
                        let nodesToDelete = this.diagramData.nodes.filter(o => { return o.hierarchyKey === impactNodeKey; });
                        nodesToDelete.forEach(nodeToDelete => {
                            ixToDelete = this.diagramData.nodes.findIndex(o => { return o.key === nodeToDelete.key; });
                            if (ixToDelete > -1) {
                                let dn = this.diagram.findNodeForKey(nodeToDelete.key);
                                if (dn) {
                                    this.diagram.remove(dn);
                                }
                                this.diagramData.nodes.splice(ixToDelete, 1);
                                ixToDelete = -1;
                            }
                        });
                        // Last, remove the dependent impact nodes attached to the one we are currently trying to remove.
                        impactData.relations.forEach((r, rix) => {
                            let innerBadgeIdentifier: string = impactData.hierarchyKey + '|' + rix;
                            this.badge_RemoveDependentNodes(innerBadgeIdentifier, r.direction);
                        });
                    }

                    this.diagram.remove(impactNode);
                }
                this.diagramModelAsGraph().removeLinkData(badgeLink);
            }
        });
    }

    private animateBadgeIcon(part: go.Part): go.Animation {
        var icon = part.findObject("badge-icon");
        if (icon) {
            var animation = new go.Animation();

            animation.add(icon, "angle", icon.angle, 360);
            animation.duration = 1000;
            animation.start();
            animation.finished = (animation: go.Animation) => {
                if (!part.data["expanded"]) {
                    this.animateBadgeIcon(part);
                }
            }
            return animation;
        }
    }

    private badge_ClickImpact(e, obj, overrideItemIndex: number = NaN) {
        if (obj !== null && obj.part !== null && obj.part.data !== null) {
            var currentAnimation = this.animateBadgeIcon(obj);
            let ix = obj.itemIndex;
            if (!isNaN(overrideItemIndex)) {
                ix = overrideItemIndex;
            }
            let node: AssetBrowserTranslationNode = obj.part.data;
            let relation: AssetBrowserTranslationRelationCount = node.relations[ix];
            let badgeIdentifier: string = node.hierarchyKey + "|" + ix;

            this.diagram.model.setDataProperty(relation, 'showLoading', true);

            let lastHighlightedPart = this.highlightedPart;
            if (!relation.disabled) {
                if (relation.expanded) {
                    this.badge_RemoveDependentNodes(badgeIdentifier, relation.direction);
                    this.diagram.model.removeArrayItem(node.relations, ix);
                    this.diagram.model.insertArrayItem(node.relations, ix, relation);
                    this.helper_CalculateAlertCount();
                    this.diagram.model.setDataProperty(relation, 'expanded', false);
                    this.diagram.model.setDataProperty(relation, 'disabled', false);
                    this.diagram.model.setDataProperty(relation, 'showLoading', false);
                    this.helper_UpdateDiagramLayout();
                    this.helper_HighlightPath(null, lastHighlightedPart);

                    if (currentAnimation)
                        currentAnimation.stop();
                }
                else {
                    relation.disabled = true;

                    let assets: AssetBrowserApiHopAssetRequestModel[] = [];
                    let hierarchyNodes = this.diagramData.nodes.filter(n => { return n.hierarchyKey === node.hierarchyKey; });
                    hierarchyNodes.forEach(n => {
                        if (!n.key.endsWith("_Reveal") && n.assetUid !== this.emptyUid && assets.findIndex(a => { return a.Uid === n.assetUid; }) === -1) {
                            if (this.displayConfiguration.IncludeNonLeaf || n.leaf) {
                                assets.push({
                                    Uid: n.assetUid,
                                    Key: n.key
                                });
                            }
                        }
                    });

                    let preloadedIntersects = this.helper_GetDiagramIntersectIds(relation.predicateId);
                    let direction = relation.direction;

                    let ancestryMode = (this.displayConfiguration.DiagramType == DiagramType.Impact) ? FilterAncestryMode.NoAncestor : this.displayConfiguration.AncestryMode;
                    this.browserService.getImpactHop(node.hierarchyKey, relation.predicateUid, direction, assets, preloadedIntersects)
                        .subscribe((response: AssetBrowserResponseModel) => {

                            // Save a copy of the original return models so we can re-parse of filters or ancestry view changes.
                            response.hierarchy.forEach(o => {
                                this.diagramData.hierarchy.push(o);
                            });
                            response.links.forEach(o => {
                                this.diagramData.links.push(o);
                            });
                            response.nodes.forEach(o => {
                                this.diagramData.nodes.push(o);
                            });
                            if (response.reveals) {
                                response.reveals.forEach(o => {
                                    this.diagramData.reveals.push(o);
                                });
                            }
                            this.diagram.model.setDataProperty(relation, 'expanded', true);
                            this.diagram.model.setDataProperty(relation, 'disabled', false);
                            this.diagram.model.setDataProperty(relation, 'showLoading', false);

                            this.helper_ParseTranslatedData(response, true, badgeIdentifier);

                            this.helper_SetFilterWindow();

                            this.helper_HideDeselectedAssetTypes();
                            this.helper_HideDeselectedPredicates();
                            this.helper_HideDeselectedResponsibilityTypes();
                            this.helper_HighlightPath(null, lastHighlightedPart);

                            if (currentAnimation)
                                currentAnimation.stop();
                        });
                }
            }
        }
    }

    private badge_ClickOwner(e, obj, overrideItemIndex: number = NaN) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            var currentAnimation = this.animateBadgeIcon(obj);

            let ix = obj.itemIndex;

            if (!isNaN(overrideItemIndex)) {
                ix = overrideItemIndex;
            }

            let node: AssetBrowserTranslationNode = obj.part.data;
            let owner: AssetBrowserTranslationOwnerCount = node.owners[ix];
            let badgeIdentifier: string = node.hierarchyKey + "|O|" + ix;

            this.diagram.model.setDataProperty(owner, 'showLoading', true);
            let lastHighlightedPart = this.highlightedPart;

            if (owner.expanded) {
                this.badge_RemoveDependentNodes(badgeIdentifier, AssetBrowserApiHopDirection.Forward);
                this.diagram.model.removeArrayItem(node.owners, ix);
                this.diagram.model.insertArrayItem(node.owners, ix, owner);
                this.diagram.model.setDataProperty(owner, 'expanded', false);
                this.diagram.model.setDataProperty(owner, 'showLoading', false);

                if (currentAnimation)
                    currentAnimation.stop();

                this.helper_UpdateDiagramLayout();
                this.helper_HighlightPath(null, lastHighlightedPart);
            }
            else {

                let assets: AssetBrowserApiHopAssetRequestModel[] = [];

                let n = node;
                if (n.isGroup) {
                    // Add the root node's asset information.
                    if (this.displayConfiguration.IncludeNonLeaf && node.assetUid !== this.emptyUid) {
                        assets.push({ Uid: node.assetUid, Key: node.key });
                    }

                    (this.diagram.findNodeForData(n) as go.Group).findSubGraphParts().each(g => {
                        let shouldInclude: boolean = this.displayConfiguration.IncludeNonLeaf ? true : (g.data.isGroup == undefined || g.data.isGroup == false);
                        if (shouldInclude && g.data.assetUid !== this.emptyUid) {
                            let asset = new AssetBrowserApiHopAssetRequestModel();
                            asset.Uid = g.data.assetUid;
                            asset.Key = g.data.key
                            assets.push(asset);
                        }
                    })
                }

                this.browserService.getOwnerHop(node.hierarchyKey, ix, owner.id, owner.text, assets)
                    .subscribe(response => {
                        this.diagram.model.setDataProperty(owner, 'expanded', true);
                        this.diagram.model.setDataProperty(owner, 'showLoading', false);

                        // Save a copy of the original return models so we can re-parse of filters or ancestry view changes.
                        response.hierarchy.forEach(o => {
                            this.diagramData.hierarchy.push(o);
                        });
                        response.links.forEach(o => {
                            this.diagramData.links.push(o);
                        });
                        response.nodes.forEach(o => {
                            this.diagramData.nodes.push(o);
                        });
                        if (response.reveals) {
                            response.reveals.forEach(o => {
                                this.diagramData.reveals.push(o);
                            });
                        }
                        this.helper_ParseTranslatedData(response, true, badgeIdentifier);
                        this.helper_SetFilterWindow();
                        this.helper_HighlightPath(null, lastHighlightedPart);

                        if (currentAnimation)
                            currentAnimation.stop();
                    });
            }
        }
    }

    private menu_ClickLinkItem(e: any) {
        if (e.value.toLowerCase() === 'open') {
            this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT}/browser/${this.selectedDiagramAsset.Uid}/${DiagramType[this.displayConfiguration.DiagramType]}`);
        }
        if (e.value.toLowerCase() === 'open in new tab') {
            window.open(`${SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT}/browser/${this.selectedDiagramAsset.Uid}/${DiagramType[this.displayConfiguration.DiagramType]}`, "_blank");
        }
    }

    //#region Hiding / Unhiding

    private context_Hide(e, obj, direction: AssetBrowserApiHopDirection = null) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: AssetBrowserTranslationNode = obj.part.data;
            let rootNode = this.diagram.findNodeForKey(node.hierarchyKey) as go.Group;
            let rootNodeData = rootNode.data as AssetBrowserTranslationNode;

            this.diagram.startTransaction("context_hide_node");

            // Now determine if we need to hide backward or forward direction.
            if (direction) {
                let links = (direction == AssetBrowserApiHopDirection.Backward) ? rootNode.findLinksInto() : rootNode.findLinksOutOf();
                let propertyName = (direction == AssetBrowserApiHopDirection.Backward) ? "upstreamHidden" : "downstreamHidden";
                this.diagram.model.setDataProperty(rootNodeData, propertyName, true);
                this.helper_HideDirection(rootNodeData, links, direction);
            }
            else {
                this.diagram.model.setDataProperty(rootNodeData, 'template', "HiddenNode");
                let hierarchyNodes = rootNode.findSubGraphParts();
                hierarchyNodes.each(c => {
                    this.diagram.model.setDataProperty(c.data, 'visible', false);
                    this.diagram.model.setDataProperty(c.data, 'opacity', 0);
                    this.diagram.model.setDataProperty(c.data, 'template', (c.data.isGroup) ? "HiddenSubNode" : "HiddenLeafNode");
                });
            }

            this.diagram.commitTransaction("context_hide_node");
        }
    }

    private context_UnhideDirection(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: any = obj.part.data;
            let streamKey = node.key as string;
            let streamNode = this.diagram.findNodeForKey(streamKey);
            let hideMode = node.hideMode as AssetBrowserApiHopDirection;
            let heirarchyNodeKey = node.hidingKey as string
            let hierarchyNode = this.diagram.findNodeForKey(heirarchyNodeKey);
            let links = (hideMode == AssetBrowserApiHopDirection.Backward) ? hierarchyNode.findLinksInto() : hierarchyNode.findLinksOutOf();

            let propertyName = (hideMode == AssetBrowserApiHopDirection.Backward) ? "upstreamHidden" : "downstreamHidden";
            this.diagram.model.setDataProperty(hierarchyNode.data, propertyName, null);

            if (streamNode) {
                this.diagram.remove(streamNode);
                this.helper_UnhideDirection(links, hideMode);
            }
        }
    }

    private context_UnhideNode(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: AssetBrowserTranslationNode = obj.part.data;
            this.helper_UnhideNode(node);
        }
    }

    private helper_HideDirection(startingHierarchyNode: AssetBrowserTranslationNode, links: go.Iterator<go.Link>, direction: AssetBrowserApiHopDirection) {
        let streamKey: string;

        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        dm.startTransaction("hide_next_hop");

        if (startingHierarchyNode) {
            streamKey = startingHierarchyNode.hierarchyKey + "_" + direction.toString();

            dm.addNodeData({
                hideMode: direction,
                key: streamKey,
                hidingKey: startingHierarchyNode.hierarchyKey,
                back: startingHierarchyNode.back,
                template: "HiddenStreamNode"
            });
        }

        links.each(l => {

            if (startingHierarchyNode) {
                let fromKey: string = (direction == AssetBrowserApiHopDirection.Backward) ? streamKey : l.fromNode.key.toString();
                let toKey: string = (direction == AssetBrowserApiHopDirection.Backward) ? l.toNode.key.toString() : streamKey;
                let linkData = {
                    from: fromKey,
                    to: toKey
                };
                dm.addLinkData(linkData);

                if (direction == AssetBrowserApiHopDirection.Forward) {
                    // We could have other branches that WERE going into this same path we are hiding. Need to connect to the new node above.
                    l.toNode.findLinksInto().each(nl => {
                        if (nl.fromNode.key !== l.fromNode.key) {
                            dm.addLinkData({
                                from: nl.fromNode.key.toString(),
                                to: toKey
                            });
                        }
                    });
                }
            }

            let rootNode = ((direction == AssetBrowserApiHopDirection.Backward) ? l.fromNode : l.toNode) as go.Group;
            let rootNodeData = rootNode.data as AssetBrowserTranslationNode;

            if (rootNodeData.template == "MoreData") {
                this.diagram.model.setDataProperty(rootNodeData, 'opacity', 0);
            }
            else if (rootNodeData.template == "HiddenStreamNode") {
                this.diagram.model.setDataProperty(rootNodeData, 'opacity', 0);
            }
            else {
                this.diagram.model.setDataProperty(rootNodeData, 'template', "HiddenNode");
                this.diagram.model.setDataProperty(rootNodeData, 'visible', false);
                this.diagram.model.setDataProperty(rootNodeData, 'opacity', 0);

                let hierarchyNodes = rootNode.findSubGraphParts();
                hierarchyNodes.each(c => {
                    this.diagram.model.setDataProperty(c.data, 'opacity', 0);
                    this.diagram.model.setDataProperty(c.data, 'visible', false);
                    this.diagram.model.setDataProperty(c.data, 'template', (c.data.isGroup) ? "HiddenSubNode" : "HiddenLeafNode");
                });

                this.helper_HideDirection(
                    null,
                    (direction == AssetBrowserApiHopDirection.Backward) ? rootNode.findLinksInto() : rootNode.findLinksOutOf(),
                    direction
                );
            }
        });

        dm.commitTransaction("hide_next_hop");
        this.helper_UpdateDiagramLayout();
    }

    private helper_UnhideDirection(links: go.Iterator<go.Link>, direction: AssetBrowserApiHopDirection) {
        let hiddenStreamKeysToRemove: string[] = [];

        links.each(l => {
            let rootNode = ((direction == AssetBrowserApiHopDirection.Backward) ? l.fromNode : l.toNode) as go.Group;
            let rootNodeData = rootNode.data as AssetBrowserTranslationNode;

            if (rootNodeData.template == "MoreData") {
                this.diagram.model.setDataProperty(rootNodeData, 'opacity', 1);
            }
            else if (rootNodeData.template == "HiddenStreamNode") {
                hiddenStreamKeysToRemove.push(rootNodeData.key);
            }
            else {
                this.diagram.model.setDataProperty(rootNodeData, 'template', rootNodeData.nonHiddenTemplate);
                this.diagram.model.setDataProperty(rootNodeData, 'visible', true);
                this.diagram.model.setDataProperty(rootNodeData, 'opacity', 1);

                let propertyName = (direction == AssetBrowserApiHopDirection.Backward) ? "upstreamHidden" : "downstreamHidden";
                this.diagram.model.setDataProperty(rootNodeData, propertyName, null);

                let hierarchyNodes = rootNode.findSubGraphParts();
                hierarchyNodes.each(c => {
                    this.diagram.model.setDataProperty(c.data, 'opacity', 1);
                    this.diagram.model.setDataProperty(c.data, 'visible', true);
                    this.diagram.model.setDataProperty(c.data, 'template', c.data.nonHiddenTemplate);
                });

                this.helper_UnhideDirection(
                    (direction == AssetBrowserApiHopDirection.Backward) ? rootNode.findLinksInto() : rootNode.findLinksOutOf(),
                    direction
                );
            }
        });

        //Have to do this after we loop through links above, otherwise we get an exception.
        hiddenStreamKeysToRemove.forEach(k => {
            let n = this.diagram.findNodeForKey(k);
            if (n) {
                this.diagram.remove(n);
            }

        });
    }

    private helper_UnhideNode(node: AssetBrowserTranslationNode) {

        let rootNode = this.diagram.findNodeForKey(node.hierarchyKey) as go.Group;
        let rootNodeData = rootNode.data as AssetBrowserTranslationNode;

        this.diagram.startTransaction("context_unhide_node");

        this.diagram.model.setDataProperty(rootNodeData, 'template', rootNodeData.nonHiddenTemplate);

        let hierarchyNodes = rootNode.findSubGraphParts();
        hierarchyNodes.each(c => {
            this.diagram.model.setDataProperty(c.data, 'visible', "true");
            this.diagram.model.setDataProperty(c.data, 'template', c.data.nonHiddenTemplate);
        });

        this.diagram.commitTransaction("context_unhide_node");
    }

    //#endregion

    private event_DiagramSelectionChanged(e: go.DiagramEvent) {
        this.selectedAsset = this.selectedTag = this.selectedReferenceItem = null;
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
                        this.panel_InformationDisabled = false;
                        this.panel_InformationHasReadAccess = data.hasAssetReadAccess;

                        if (this.selectedDiagramAsset == null || this.selectedDiagramAsset.Uid != uid) {
                            if (this.panelModel.AlertVisible) {
                                this.selectedAssetsWithAlerts = [uid];
                                this.cdRef.markForCheck();
                            }
                            else {
                                this.selectedDiagramAsset = new AssetBrowserDiagramAsset();
                                this.selectedDiagramAsset.Uid = uid;
                                this.selectedDiagramAsset.Id = data.assetId;
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
                        if (this.panelModel.AlertVisible) {
                            this.selectedAssetsWithAlerts = this.assetsWithAlerts;
                        }
                        this.cdRef.markForCheck();
                    }

                } else if (parts.count == 0) {
                    this.diagram.nodes.each(n => {
                        n.isHighlighted = false;
                    });
                    this.selectedDiagramAsset = null;
                    this.panel_TabIndex = 0;
                    if (this.panelModel.AlertVisible) {
                        this.selectedAssetsWithAlerts = this.assetsWithAlerts;
                    }
                    this.cdRef.markForCheck();
                }
            }
        }
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
            case AssetBrowserFilterChangeEventType.Descendancy:
                this.helper_RefreshDiagram(false);
                break;
            case AssetBrowserFilterChangeEventType.AssetType:
                this.helper_HideDeselectedAssetTypes();
                break;
            case AssetBrowserFilterChangeEventType.ImpactHopCount:
            case AssetBrowserFilterChangeEventType.LineageHopCount:
                this.helper_RefreshDiagram(false);
                break;
            case AssetBrowserFilterChangeEventType.Predicate:
                this.helper_HideDeselectedPredicates();
                this.helper_UpdateParts();
                break;
            case AssetBrowserFilterChangeEventType.ResponsibilityType:
                this.helper_HideDeselectedResponsibilityTypes();
                this.helper_UpdateParts();
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
                        if (model.ResponsibilityTypes.findIndex(o => { return o == r.id }) == -1) {
                            model.ResponsibilityTypes.push(r.id);
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

        if (this.diagram == null)
            return;

        this.diagram.links.each(function (l) {
            if (l.fromNode && l.fromNode.data) {
                if (!unlockedKeys.some(x => x == l.fromNode.data.key))
                    unlockedKeys.push(l.fromNode.data.key);
            }
            if (l.toNode && l.toNode.data) {
                if (!unlockedKeys.some(x => x == l.toNode.data.key))
                    unlockedKeys.push(l.toNode.data.key);
            }
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

    private helper_GetDiagramIntersectIds(predicateId: number): number[] {
        let preloadedIntersects: number[] = [];
        if (this.diagramData.links) {
            this.diagramData.links.forEach(i => {
                if (i.predicateId == predicateId || !predicateId) {
                    if (i.links) {
                        i.links.forEach(c => {
                            preloadedIntersects.push(c.id)
                        });
                    }
                }
            });
        }
        return preloadedIntersects;
    }

    private helper_HideAndDisableSingleGroup(node: go.Group, filterHiddenBy: string) {
        this.diagram.startTransaction("HideAndDisableSingleGroup");

        this.diagram.model.setDataProperty(node.data, 'filterHiddenBy', filterHiddenBy);
        this.diagram.model.setDataProperty(node.data, 'hideMode', 0);
        this.diagram.model.setDataProperty(node.data, 'template', "HiddenDisabledNode");

        let hierarchyNodes = node.findSubGraphParts();
        hierarchyNodes.each(c => {
            this.diagram.model.setDataProperty(c.data, 'visible', false);
            this.diagram.model.setDataProperty(c.data, 'opacity', 0);
            this.diagram.model.setDataProperty(c.data, 'template', (c.data.isGroup) ? "HiddenSubNode" : "HiddenLeafNode");
        });

        this.diagram.commitTransaction("HideAndDisableSingleGroup");
    }

    private helper_ShowAndEnableSingleGroup(node: go.Group) {
        this.diagram.startTransaction("ShowAndEnableSingleGroup");

        this.diagram.model.setDataProperty(node.data, 'filterHiddenBy', null);
        this.diagram.model.setDataProperty(node.data, 'hideMode', null);
        this.diagram.model.setDataProperty(node.data, 'template', node.data.nonHiddenTemplate);

        let hierarchyNodes = node.findSubGraphParts();
        hierarchyNodes.each(c => {
            this.diagram.model.setDataProperty(c.data, 'visible', true);
            this.diagram.model.setDataProperty(c.data, 'opacity', 1);
            this.diagram.model.setDataProperty(c.data, 'template', c.data.nonHiddenTemplate);
        });

        this.diagram.commitTransaction("ShowAndEnableSingleGroup");
    }

    private helper_HideDeselectedAssetTypes() {
        let hiddenIds = this.displayConfiguration.SelectedAssetTypes;
        this.diagram.findTopLevelGroups().each(g => {
            let groupAssetTypeId = (g.data && g.data.assetTypeId) ? g.data.assetTypeId : -1;
            if (groupAssetTypeId > -1) {
                if (hiddenIds.findIndex(id => { return id == groupAssetTypeId; }) > -1) {
                    this.helper_HideAndDisableSingleGroup(g, "a");
                }
                else {
                    if (g.data.isGroup && (g.data.filterHiddenBy == "a" || !g.data.filterHiddenBy) && g.data.template !== "HiddenNode") {
                        this.helper_ShowAndEnableSingleGroup(g);
                    }
                }
            }
        });
    }

    private helper_HideDeselectedPredicates() {
        let hiddenIds = this.displayConfiguration.SelectedPredicates;
        this.diagram.startTransaction('HideDeselectedPredicates');

        this.diagram.findTopLevelGroups().each(g => {
            let gData = g.data as AssetBrowserTranslationNode;

            if (gData.relations) {
                // Hide Badges
                gData.relations.forEach(rC => {
                    let showBadge: boolean = (hiddenIds.findIndex(v => { return v == rC.predicateId; }) == -1);
                    this.diagram.model.setDataProperty(rC, "showBadge", showBadge);
                });
            }

            g.findLinksOutOf().each(l => {
                if (hiddenIds.findIndex(id => { return l.data.predicateId == id; }) == -1) {
                    if (l.toNode && l.toNode.data && l.toNode.data.isGroup && (l.toNode.data.filterHiddenBy == "p" || !l.toNode.data.filterHiddenBy) && l.toNode.data.template !== "HiddenNode") {
                        this.helper_ShowAndEnableSingleGroup(l.toNode as go.Group);
                    }
                }
                else {
                    if (l.toNode && l.toNode.data && l.toNode.data.isGroup) {
                        this.helper_HideAndDisableSingleGroup(l.toNode as go.Group, "p");
                    }
                }
            });
        });

        this.diagram.commitTransaction('HideDeselectedPredicates');
    }

    private helper_HideDeselectedResponsibilityTypes() {
        let hiddenIds = this.displayConfiguration.SelectedResponsibilityTypes;

        this.diagram.startTransaction('HideDeselectedResponsibilityTypes');

        this.diagram.findTopLevelGroups().each(g => {
            let gData = g.data as AssetBrowserTranslationNode;

            if (gData.owners) {
                // Hide Badges
                gData.owners.forEach(rC => {
                    let showBadge: boolean = (hiddenIds.findIndex(v => { return v == rC.id; }) == -1);
                    this.diagram.model.setDataProperty(rC, "showBadge", showBadge);
                });
            }

            g.findLinksOutOf().each(l => {
                if (hiddenIds.findIndex(id => { return l.data.responsibilityTypeId == id; }) == -1) {
                    if (l.toNode && l.toNode.data && l.toNode.data.isGroup && (l.toNode.data.filterHiddenBy == "r" || !l.toNode.data.filterHiddenBy) && l.toNode.data.template !== "HiddenNode") {
                        this.helper_ShowAndEnableSingleGroup(l.toNode as go.Group);
                    }
                }
                else {
                    if (l.toNode && l.toNode.data && l.toNode.data.isGroup) {
                        this.helper_HideAndDisableSingleGroup(l.toNode as go.Group, "r");
                    }
                }
            });
        });

        this.diagram.commitTransaction('HideDeselectedResponsibilityTypes');
    }

    private helper_HighlightNodeImpacts(key: string, direction: AssetBrowserApiHopDirection, allRelations: AssetBrowserGenericRelationModel[], visitedNodes: Set<string>) {
        // cycle detection. Set
        if (visitedNodes == null) {
            visitedNodes = new Set<string>();
        }

        // check if we already encountered this key it would already be in the Set if we have.
        if (visitedNodes.has(key)) {
            console.warn('warning:cycle detected ending node highlighting of this path.')
            return;
        }

        visitedNodes.add(key);

        let fwd: boolean = ((direction == AssetBrowserApiHopDirection.Both) || (direction == AssetBrowserApiHopDirection.Forward));
        let bwd: boolean = ((direction == AssetBrowserApiHopDirection.Both) || (direction == AssetBrowserApiHopDirection.Backward));

        if (allRelations === undefined) {
            allRelations = [];

            this.diagramData.links.forEach(l => {
                if (l.links) {
                    l.links.forEach(cl => {
                        allRelations.push({ from: cl.from, to: cl.to });
                    });
                }
            });
        }

        allRelations.forEach(l => {

            // Loop through the links to find ones where this node is subject, then traverse each one and do the same thing, recursively.
            if (fwd) {
                if (l.from == key) {
                    let oNode = this.diagram.findNodeForKey(l.to);
                    if (oNode) {
                        oNode.isHighlighted = true;
                        this.helper_HighlightNodeImpacts(l.to, AssetBrowserApiHopDirection.Forward, allRelations, visitedNodes);
                    }
                }
            }

            // Loop through the links to find ones where this node is object, then traverse each one and do the same thing, recursively.
            if (bwd) {
                if (l.to == key) {
                    let sNode = this.diagram.findNodeForKey(l.from);
                    if (sNode) {
                        sNode.isHighlighted = true;
                        this.helper_HighlightNodeImpacts(l.from, AssetBrowserApiHopDirection.Backward, allRelations, visitedNodes);
                    }
                }
            }
        });
    }

    private helper_HighlightPath(e: go.InputEvent, obj: go.Part) {
        try {
            if (obj == null)
                return;

            if (obj.diagram == null)
                return;

            if (obj.diagram.nodes == null)
                return;

            if (e == null) {
                this.helper_ShowDetail(obj.data['assetUid']);
                this.panel_InformationDisabled = false;
            }

            this.highlightedPart = obj;
            //Set all to not highlighted.
            obj.diagram.nodes.each(n => {
                n.isHighlighted = false;
            });
            if (obj.key) {
                // Highlight the selected node.
                obj.isHighlighted = true;

                // Recurse through and highlight based on the atomic (non-grouped) links.
                this.helper_HighlightNodeImpacts(obj.key.toString(), AssetBrowserApiHopDirection.Both, undefined, null);
            }
            else {
                // You are clicking on a link instead.                
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
            console.log(e);
        }
    }

    private helper_InitializeDiagram() {
        this.template_BadgeShapes();

        this.diagram = this.template_Diagram();


        let forelayer = this.diagram.findLayer("Foreground");
        this.diagram.addLayerBefore(this.g(go.Layer, { name: "Links" }), forelayer);

        this.diagram.groupTemplateMap.add("FocalPortGroup", this.template_FocalRootNode());
        this.diagram.groupTemplateMap.add("PortGroup", this.template_RootNode());
        this.diagram.groupTemplateMap.add("Group", this.template_AncestorNode());
        this.diagram.groupTemplateMap.add("AncestorGroupNodeOnlyText", this.template_AncestorGroupNodeOnlyText());
        this.diagram.nodeTemplateMap.add("AncestorLeafNodeOnlyText", this.template_AncestorLeafNodeOnlyText());

        this.diagram.nodeTemplateMap.add("MoreData", this.template_RevealNode());
        this.diagram.groupTemplateMap.add("HiddenDisabledNode", this.template_HiddenDisabledNode());
        this.diagram.groupTemplateMap.add("HiddenNode", this.template_HiddenNode());
        this.diagram.groupTemplateMap.add("HiddenSubNode", this.template_HiddenSubNode());
        this.diagram.nodeTemplateMap.add("HiddenLeafNode", this.template_HiddenLeafNode());
        this.diagram.nodeTemplateMap.add("HiddenStreamNode", this.template_HiddenStreamNode());

        this.diagram.groupTemplateMap.add("Owners", this.template_OwnersRootNode());
        this.diagram.nodeTemplateMap.add("Owner", this.template_LeafOwnerNode());
        this.diagram.nodeTemplate = this.template_LeafAssetNode();

        if (this.helper_LineageDiagramApplies()) {
            this.diagram.linkTemplateMap.add("", this.template_LineageLink());
        }
        else {
            this.diagram.linkTemplateMap.add("", this.template_ImpactLink());
        }

        this.diagram.linkTemplateMap.add("NoAvoid", this.template_LineageLinkNoAvoid());

        this.diagram.addDiagramListener('ChangedSelection', e => this.event_DiagramSelectionChanged(e));
        this.diagram.addDiagramListener('ViewportBoundsChanged', e => this.event_ViewportBoundsChanged(e));

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.helper_PopulateDiagram().subscribe(bComplete => {
            this.helper_HideDeselectedAssetTypes();
            this.helper_HideDeselectedPredicates();
            this.helper_HideDeselectedResponsibilityTypes();
            if (this.searchText !== '') {
                this.search_Execute(this.searchText);
            }

            this.overviewControlRef.initialize(this.diagram);
        });
    }

    /**
    * Determines whether the Lineage view is currently selected.
    * @returns A boolean value on whether the lineage view is selected.
    */
    helper_LineageDiagramApplies(): boolean {
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

    private helper_ParseTranslatedData(trans: AssetBrowserResponseModel, append: boolean = false, badgeIdentifier: string = null) {//(trans: AssetBrowserTranslation, append: boolean = false) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;

        //#region add data to diagram model

        trans.nodes.forEach(n => {
            n.showIcon = this.displayConfiguration.DisplayIcons;
        });

        if (this.performanceLinkMode) {
            trans.links.forEach((l) => l['category'] = 'NoAvoid');
        }

        if (append) {
            trans.nodes.forEach(n => {
                dm.addNodeData(n);
            });
            trans.links.forEach(l => {
                l.badgeIdentifier = badgeIdentifier;
                dm.addLinkData(l);
            });
        }
        else {
            dm.nodeDataArray = trans.nodes;
            dm.linkDataArray = trans.links;
        }

        if (trans.reveals) {
            trans.reveals.forEach(reveal => {

                let linkedHeirarchyNode = dm.findNodeDataForKey(reveal.hierarchyKey);

                dm.addNodeData({
                    template: 'MoreData',
                    hierarchyKey: reveal.hierarchyKey,
                    key: reveal.hierarchyKey,// + '_Reveal',
                    back: (linkedHeirarchyNode) ? linkedHeirarchyNode.back : "#cccccc",
                    direction: reveal.direction,
                });

                dm.addLinkData({
                    from: reveal.from,
                    to: reveal.to,
                    badgeIdentifier: badgeIdentifier
                });
            });
        }

        //#endregion

        this.diagram.nodes.each(n => {
            n.isHighlighted = false;
        });

        this.diagram.findTopLevelGroups().each(g => {
            this.diagram.model.setDataProperty(g.data, "showBadges", this.displayConfiguration.DisplayBadges);
        });

        this.diagram.commitTransaction("load_all_data");
        this.helper_UpdateDiagramLayout();

        this.helper_CalculateAlertCount();
    }

    private helper_PopulateDiagram(): Observable<boolean> {
        let dgmObs: Observable<boolean>;

        this.errorText = "";
        this.isError = false;
        this.cdRef.markForCheck();

        dgmObs = new Observable(obs => {
            let isLineage: boolean = this.helper_LineageDiagramApplies();

            this.isLoading = true;
            this.loadingText = $localize`Retrieving ${isLineage ? 'lineage' : 'impacts'} from Govern..`;

            this.helper_ResetDiagramData();

            let subscriber = (data: AssetBrowserResponseModel) => {
                if (data) {
                    this.diagramData = data;
                    this.loadingText = $localize`Determining links and meaning...`;
                    if (this.diagramData && this.diagramData.links && this.diagramData.links.length > this.maxLinkCountToAvoidNodesTemplate) {
                        this.performanceLinkMode = true;
                    }
                    this.cdRef.detectChanges();
                    if (isLineage && this.diagramData.dataLimitReached === true) {
                        this.errorText = $localize`Sorry, we cannot display an asset with more than 500 descendants.`;
                        this.isError = true;
                        this.isLoading = false;
                    }
                    else {
                        //if there are a lot of descendants helper_ParseTranslatedData will take too much cpu
                        //and loadingText wont change, adding a slight delay of 10ms to allow angular detecting text change
                        setTimeout(() => {
                            this.helper_ParseTranslatedData(data);

                            this.helper_ResizeDiagram();
                            this.helper_ScaleDiagram(1);
                            this.diagram.alignDocument(go.Spot.Center, go.Spot.Center);
                            this.loadingText = "";
                            this.isLoading = false;

                            this.cdRef.markForCheck();

                            obs.next(true);
                            obs.complete();
                        }, 10);
                    }
                }
                else {
                    this.errorText = $localize`Unable to retrieve ${(isLineage ? "lineage" : "impact")} content.`;
                    this.isError = true;
                    this.isLoading = false;
                }
                this.cdRef.markForCheck();
            };

            if (isLineage) {
                this.browserService.getInitialLineage(this.displayConfiguration.AncestryMode, this.assetUid, this.helper_NumberOfHops(), this.displayConfiguration.IncludeNonLeaf, this.displayConfiguration.Descendancy).subscribe(subscriber);
            }
            else {
                this.browserService.getInitialImpact(this.assetUid, this.helper_NumberOfHops()).subscribe(subscriber);
            }
        });

        return dgmObs;
    }

    /**
    * Refreshes the data and diagram to its initially loaded state.
    * @returns Nothing
    */
    private helper_RefreshDiagram(closePanels: boolean = true) {
        this.helper_ResetDiagramData(); // Clear out the current diagram data first.
        this.assetUid = this.originalAssetUid;
        this.isLoading = true;
        this.selectedDiagramAsset = null;
        if (closePanels) {
            this.helper_SetVisiblePanel(AssetBrowserPanelCommand.None);
        }
        this.helper_PopulateDiagram().subscribe(bComplete => {
            this.isLoading = false;
            this.helper_SetFilterWindow();
            this.helper_HideDeselectedAssetTypes();
            this.helper_HideDeselectedPredicates();
            this.helper_HideDeselectedResponsibilityTypes();
            this.helper_CalculateAlertCount();
            if (this.overviewControlRef) {
                this.overviewControlRef.clear();
                this.overviewControlRef.initialize(this.diagram);
            }
        });
    }

    /**
    * Refreshes the diagram parts. Used when changing object properties and change detection on gojs is not triggered.
    * @returns Nothing
    */
    private helper_UpdateParts() {
        setTimeout(() => {
            this.diagram.rebuildParts();
        });
    }

    /**
    * Removes the diagram reveal node AFTER a user clicks to reveal its continuing lineage path.
    * @returns Nothing
    */
    private helper_RemoveRevealNode(data: AssetBrowserTranslationNode, direction: AssetBrowserApiHopDirection) {
        this.diagram.startTransaction('reveal');

        let selectedRevealNode = this.diagram.findNodeForKey(data.key);
        if (selectedRevealNode) {
            selectedRevealNode.findLinksInto().each(l => {
                this.diagramModelAsGraph().removeLinkData(l.data);
            });
            selectedRevealNode.findLinksOutOf().each(l => {
                this.diagramModelAsGraph().removeLinkData(l.data);
            });
        }
        this.diagramModelAsGraph().removeNodeData(data);

        this.diagram.commitTransaction('reveal');
    }

    private helper_ResetDiagramData() {
        let dm = this.diagramModelAsGraph();
        this.diagram.nodes.each(n => { this.diagram.remove(n); });
        this.diagram.links.each(l => { this.diagram.remove(l); });
        this.diagramData = new AssetBrowserResponseModel();
        this.diagramData.hierarchy = [];
        this.diagramData.links = [];
        this.diagramData.nodes = [];
        this.diagramData.reveals = [];
    }

    /**
    * Resizes the diagram according to the current height of the containing HTML element.
    * @returns Nothing
    */
    private helper_ResizeDiagram() {
        if (this.displayConfiguration.DiagramType == 3)
            return;

        let height = window.innerHeight;
        if (this.isFullScreen)
            this.diagramRef.nativeElement.style.height = (height - 55) + 'px';
        else
            this.diagramRef.nativeElement.style.height = (height - 240) + 'px';
        setTimeout(() => {
            if (this.diagram) {
                this.diagram.redraw();
            }
        }, 20);
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
            let currentHighlightedNode = this.highlightedPart;
            // Get relations to ignore.
            let assets: AssetBrowserApiHopAssetRequestModel[] = [];

            let isBackward: boolean = (data.direction === "Backward");

            let hierarchyKey: string = "";
            this.diagram.links.each(function (l: go.Link) {
                if (hierarchyKey === "") {
                    if (l.fromNode.data.key === data.key) {
                        hierarchyKey = l.toNode.data.hierarchyKey;
                    }
                    if (l.toNode.data.key === data.key) {
                        hierarchyKey = l.fromNode.data.hierarchyKey;
                    }
                }
            });
            this.diagramData.nodes.forEach(o => {
                if (o.hierarchyKey == hierarchyKey && o.assetUid !== this.emptyUid && o.assetUid) {
                    //if (this.displayConfiguration.IncludeNonLeaf || n.leaf) {
                    assets.push({
                        Key: "",
                        Uid: o.assetUid
                    });
                    //}
                }
            });

            let preloadedIntersects = this.helper_GetDiagramIntersectIds(null);

            let direction: AssetBrowserApiHopDirection = isBackward ? AssetBrowserApiHopDirection.Backward : AssetBrowserApiHopDirection.Forward;

            let currentHop: number = +hierarchyKey.substring(hierarchyKey.indexOf("|") + 1, hierarchyKey.lastIndexOf("|"));
            let requestModel: AssetBrowserLineageRequest = {
                ancestry: this.displayConfiguration.AncestryMode,
                descendancy: this.displayConfiguration.Descendancy,
                direction: direction,
                assets: assets,
                currentHop: currentHop,
                includeNonLeaf: this.displayConfiguration.IncludeNonLeaf,
                preloadedIntersects: preloadedIntersects,
                hierarchyKey: hierarchyKey
            };
            this.browserService.getLineageHop(requestModel)
                .subscribe((response: AssetBrowserResponseModel) => {

                    if (response.hierarchy && response.hierarchy.length > 0) {

                        // Save a copy of the original return models so we can re-parse of filters or ancestry view changes.
                        response.hierarchy.forEach(o => {
                            this.diagramData.hierarchy.push(o);
                        });
                        response.links.forEach(o => {
                            this.diagramData.links.push(o);
                        });
                        response.nodes.forEach(o => {
                            this.diagramData.nodes.push(o);
                        });
                        if (response.reveals) {
                            response.reveals.forEach(o => {
                                this.diagramData.reveals.push(o);
                            });
                        }

                        this.helper_ParseTranslatedData(response, true);

                        this.helper_RemoveRevealNode(data, direction);

                        this.helper_SetFilterWindow();

                        this.helper_HideDeselectedAssetTypes();
                        this.helper_HideDeselectedPredicates();
                        this.helper_HideDeselectedResponsibilityTypes();
                        this.helper_HighlightPath(null, currentHighlightedNode);
                    }
                    else {
                        this.helper_RemoveRevealNode(data, direction);
                        this.helper_HighlightPath(null, currentHighlightedNode);
                    }
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

        //#endregion

        this.cdRef.markForCheck();
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
        if (assetUid === this.emptyUid || assetUid == null) {
            return;
        }
        this.panel_TabIndex = 0;
        if (!this.panel_InformationHasReadAccess)
            return;

        this.panel_Loading = true;

        let diagramAsset = new AssetBrowserDiagramAsset();
        this.objectStatisticsService.getSearchDetails(assetUid)
            .subscribe((asset) => {
                diagramAsset.Url = "/asset/" + assetUid;
                diagramAsset.Uid = assetUid;
                diagramAsset.Id = asset.AssetDetail.Id;
                diagramAsset.Object = asset.AssetDetail.Object;
                diagramAsset.ObjectId = asset.AssetDetail.ObjectId;
                diagramAsset.DisplayValue = asset.AssetDetail.DisplayValue;
                diagramAsset.TypeName = asset.AssetDetail.TypeName;
                diagramAsset.Scores = [];

                asset.Scores.forEach((s) => {
                    let scoreClass = '';
                    if ((s.Value * 100) <= s.LowerThreshold) {
                        scoreClass = 'poor';
                    }
                    else if ((s.Value * 100) > s.LowerThreshold && (s.Value * 100) <= s.UpperThreshold) {
                        scoreClass = 'average';
                    }
                    else {
                        scoreClass = 'good';
                    }

                    diagramAsset.Scores.push({
                        Name: s.ScoreType === "DataQuality" ? "DQ" : "GV",
                        LowerThreshold: s.LowerThreshold,
                        UpperThreshold: s.UpperThreshold,
                        Value: s.Value,
                        ScoreClass: scoreClass
                    });


                });

                diagramAsset.Loaded = true;
                this.selectedDiagramAsset = diagramAsset;
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

        let aAssetUid = a.data.assetUid;
        let bAssetUid = b.data.assetUid;

        if (al > bl) {
            return 1;
        }
        else if (al < bl) {
            return -1;
        }
        if (aAssetUid > bAssetUid) {
            return 1;
        }
        else if (aAssetUid < bAssetUid) {
            return -1;
        }
        else {
            return 0;
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
        this.cdRef.detectChanges();
    }

    panels_Click(e: AssetBrowserPanelCommand) {
        switch (e) {
            case AssetBrowserPanelCommand.Add:
                this.helper_SetVisiblePanel(e);
                break;
            case AssetBrowserPanelCommand.Alerts:
                this.panel_TabIndex = 0;
                this.helper_SetVisiblePanel(e);
                if (this.selectedDiagramAsset) {
                    this.selectedAssetsWithAlerts = [this.selectedDiagramAsset.Uid];
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
                    maxSize: new go.Size(Infinity, Infinity),
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
                if (this.processDiagramRef) {
                    this.processDiagramRef.onResize(null);
                }
                break;
            case AssetBrowserPanelCommand.Information:
                this.helper_SetVisiblePanel(e);

                if (this.selectedDiagramAsset != null) {
                    this.helper_ShowDetail(this.selectedDiagramAsset.Uid);
                } else {
                    this.cdRef.markForCheck();
                    this.diagram.requestUpdate();
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

    //#region Search

    search_AddHighlightToNode(node: go.Node, phrase: string) {
        this.diagram.model.commit(function (m) {
            var data = m.findNodeDataForKey(node.key);

            var idx = phrase.length;
            var highlight = data.text.substring(0, idx);
            var text = data.text.substring(idx, data.text.length);

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
                if (phrase != '') {
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
                m.set(data, 'text', fullText);
            }, 'update_highlight');
        } catch (e) {
            console.log(e);
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

    //#endregion

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

    //#region Templates

    private template_GetContrast(back: string, backAmount: number): string {
        let brush = new go.Brush();
        brush.color = back;
        brush.lightenBy(backAmount);
        return brush.isDark() ? '#ffffff' : '#000000';
    }

    private template_AncestorNode(): go.Group {
        var self = this;
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
                movable: false,
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
            new go.Binding("isSubGraphExpanded", "", this.isSubGraphExpanded.bind(this)).ofObject(),
            this.g(
                go.Shape,
                "Rectangle",
                { fill: null, strokeWidth: 2, stretch: go.GraphObject.Horizontal },
                new go.Binding("stroke", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount))
            ),
            this.g(
                go.Panel,
                "Vertical",  // title above Placeholder
                new go.Binding("desiredSize", "", function (obj: go.GraphObject, target: go.GraphObject) {
                    var part = target.part;
                    return new go.Size(self.calculateNodeWidth(part), NaN);
                }).ofObject(),
                this.g(
                    go.Panel,
                    "Table",
                    // button next to TextBlock
                    {
                        stretch: go.GraphObject.Horizontal,
                    },
                    new go.Binding("background", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount)),
                    new go.Binding("background", "", v => (v.isHighlighted) ?
                        go.Brush.mix(this.selectionPathHighlightColor, this.selectionPathHighlightColor, v.backAmount) :
                        go.Brush.mix(v.data.back, this.lightenBoxColor, v.data.backAmount)
                    ).ofObject(),
                    this.g(
                        "SubGraphExpanderButton",
                        {
                            row: 1,
                            column: 1,
                            margin: new go.Margin(0, 5, 0, 3)
                        }
                    ),
                    //icon
                    this.g(
                        go.TextBlock,
                        {
                            row: 1,
                            column: 2,
                            editable: false,
                            font: this.fontLabelIcon
                        },
                        new go.Binding("stroke", "", (v) => this.template_GetContrast(v.back, v.backAmount)),
                        new go.Binding("text", "icon"),
                        new go.Binding("visible", "showIcon")
                    ),
                    //This TextBlock is placeholder for highlighted text
                    this.g(
                        go.TextBlock,
                        {
                            row: 1,
                            column: 3,
                            editable: false,
                            font: this.fontLabel,
                            stroke: this.fontLabelColor,
                            visible: false,
                            maxLines: this.textMaxLines,
                            overflow: this.textOverflowStyle,
                            margin: new go.Margin(5, 0, 5, 5),
                        },
                        new go.Binding("text", "highlight").makeTwoWay(),
                        new go.Binding("visible", "highlight_visible").makeTwoWay(),
                        new go.Binding("background", "highlight_background").makeTwoWay()
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 1,
                            column: 4,
                            editable: false,
                            margin: new go.Margin(5, 5, 5, 0),
                            font: this.fontLabel,
                            maxLines: this.textMaxLines,
                            maxSize: this.textMaxSize,
                            overflow: this.textOverflowStyle,
                            toolTip: this.template_Tooltip(),
                            stretch: go.GraphObject.Horizontal
                        },
                        new go.Binding("stroke", "", (v) => this.template_GetContrast(v.back, v.backAmount)),
                        new go.Binding("text", "text").makeTwoWay(),
                        new go.Binding("margin", "", (obj: go.GraphObject) => {
                            return obj.part.data["highlight_visible"] ? new go.Margin(5, 5, 5, 0) : 5;
                        }).ofObject()
                    ),// end of Title containing Panel (higlighted + normal text)
                    this.template_nodeCount()
                ),  // end Horizontal Panel
                this.g(
                    go.Placeholder,
                    { padding: 2, alignment: go.Spot.TopLeft }
                )
            ),

            // end Vertical Panel
        );
    }

    private template_AncestorGroupNodeOnlyText(): go.Group {
        var self = this;
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
                movable: false,
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
            this.g(
                go.Shape,
                "Rectangle",
                { fill: null, strokeWidth: 2, stretch: go.GraphObject.Horizontal },
                new go.Binding("stroke", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount))
            ),
            this.g(
                go.Panel,
                "Vertical",   // title above Placeholder
                new go.Binding("desiredSize", "", function (obj: go.GraphObject, target: go.GraphObject) {
                    target.part.findTopLevelPart().part.data["predicateWidth"] = 265;
                }).ofObject(),
                new go.Binding("background", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount)),
                new go.Binding("background", "", v => (v.isHighlighted) ?
                    go.Brush.mix(this.selectionPathHighlightColor, this.selectionPathHighlightColor, v.backAmount) :
                    go.Brush.mix(v.data.back, this.lightenBoxColor, v.data.backAmount)
                ).ofObject(),
                this.g(
                    go.TextBlock,
                    {
                        margin: 5,
                        editable: false,
                        font: this.fontLabel,
                        textAlign: "center",
                        overflow: go.TextBlock.OverflowClip,
                        width: 240
                    },
                    new go.Binding("stroke", "", (v) => this.template_GetContrast(v.back, v.backAmount)),
                    new go.Binding("text", "text").makeTwoWay()
                ),// end of Title containing Panel
            ),  // end Horizontal Panel
            // end Vertical Panel
        );
    }


    private template_AncestorLeafNodeOnlyText(): go.Node {
        var self = this;
        return this.g(
            go.Node,
            "Auto",
            this.g(
                go.Shape,
                "Rectangle",
                { fill: null, strokeWidth: 2, stretch: go.GraphObject.Horizontal },
                new go.Binding("stroke", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount))
            ),
            this.g(
                go.Panel,
                "Vertical",   // title above Placeholder
                new go.Binding("desiredSize", "", function (obj: go.GraphObject, target: go.GraphObject) {
                    target.part.findTopLevelPart().part.data["predicateWidth"] = 265;
                }).ofObject(),
                new go.Binding("background", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount)),
                new go.Binding("background", "", v => (v.isHighlighted) ?
                    go.Brush.mix(this.selectionPathHighlightColor, this.selectionPathHighlightColor, v.backAmount) :
                    go.Brush.mix(v.data.back, this.lightenBoxColor, v.data.backAmount)
                ).ofObject(),
                this.g(
                    go.TextBlock,
                    {
                        margin: 5,
                        editable: false,
                        font: this.fontLabel,
                        textAlign: "center",
                        overflow: go.TextBlock.OverflowClip,
                        width: 240
                    },
                    new go.Binding("stroke", "", (v) => this.template_GetContrast(v.back, v.backAmount)),
                    new go.Binding("text", "text").makeTwoWay()
                ),// end of Title containing Panel
            ),  // end Horizontal Panel
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
                this.g(go.TextBlock, { text: "Open", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                {
                    click: (e, obj) => {
                        let assetUidRedirect: string = '';
                        assetUidRedirect = obj.part.data.assetUid;
                        if (assetUidRedirect == this.assetUid)
                            return;

                        if (obj.part.data.class && obj.part.data.class.toString() == 'DiagramAsset') {

                            this.processService.getProcessUrlByDiagramAssetUid(obj.part.data.assetUid).subscribe(res => {
                                this.router.navigateByUrl('/bla', { skipLocationChange: true }).then(() => {
                                    this.router.navigateByUrl(res);
                                });
                            })
                            return;
                        }

                        this.router.navigateByUrl('/bla', { skipLocationChange: true }).then(() => {
                            this.router.navigate([SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT, 'browser', assetUidRedirect]);
                        });
                    }
                },
                new go.Binding("visible", "", (o) => (
                    !(o.part.data.template && o.part.data.template == 'Owner') &&
                    o.part.data.assetUid != this.assetUid &&
                    (o.part.data.assetUid !== this.emptyUid && o.part.data.hasAssetReadAccess)
                )).ofObject()
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Open in New Tab", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                {
                    click: (e, obj) => {
                        let assetUidRedirect: string = '';
                        assetUidRedirect = obj.part.data.assetUid;
                        if (assetUidRedirect == this.assetUid)
                            return;

                        if (obj.part.data.class && obj.part.data.class.toString() == 'DiagramAsset') {

                            this.processService.getProcessUrlByDiagramAssetUid(obj.part.data.assetUid).subscribe(res => {
                                window.open(res, '_blank');
                            })
                            return;
                        }

                        var url = window.location.protocol + '//' + window.location.hostname + '/' + SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT + '/' + 'browser' + '/' + assetUidRedirect;
                        window.open(url, '_blank');
                    }
                },
                new go.Binding("visible", "", (o) => (
                    !(o.part.data.template && o.part.data.template == 'Owner') &&
                    (o.part.data.assetUid !== this.emptyUid && o.part.data.assetUid != this.assetUid && o.part.data.hasAssetReadAccess)
                )).ofObject()
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
                { click: (e, obj) => this.context_Hide(e, obj) },
                new go.Binding("visible", "", (o) => (!o.part.data.group)).ofObject()
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Upstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.context_Hide(e, obj, AssetBrowserApiHopDirection.Backward) },
                new go.Binding("visible", "", (o) => (!o.part.data.group && this.displayConfiguration.DiagramType !== DiagramType.Impact && !o.part.data.upstreamHidden)).ofObject()
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Downstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.context_Hide(e, obj, AssetBrowserApiHopDirection.Forward) },
                new go.Binding("visible", "", (o) => (!o.part.data.group && !o.part.data.downstreamHidden)).ofObject()
            )
        );
    }

    private template_Diagram(): go.Diagram {

        let layout: go.Layout;

        this.helper_ResizeDiagram();

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
            initialDocumentSpot: go.Spot.Center,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            scrollMode: go.Diagram.DocumentScroll,
            layout: layout,
            "undoManager.isEnabled": true,
            "commandHandler.archetypeGroupData": { isGroup: true, category: "Normal" },
            "animationManager.isEnabled": false
        });

        let model = (dg.model as go.GraphLinksModel);

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

    private template_HiddenDisabledNode(): go.Group {
        return this.g(go.Group, "Auto",
            new go.Binding("visible", "visible"),
            new go.Binding("opacity", "opacity"),
            this.g(
                go.Panel,
                "Horizontal",
                { stretch: go.GraphObject.Horizontal, padding: 10, type: go.Panel.Spot },
                this.g(
                    "Shape",
                    {
                        alignment: go.Spot.Center,
                        width: 25,
                        height: 25,
                        fill: this.disabledNodeBackColor,
                        stroke: this.disabledNodeBackColor
                    }
                ),
                this.g(
                    go.TextBlock,
                    {
                        row: 0,
                        alignment: go.Spot.Center,
                        editable: false,
                        font: this.fontLabelIcon,
                        stroke: this.fontLabelColor,
                        text: this.hideIcon
                    },
                ),
                this.g(
                    go.Placeholder,
                    { padding: 0, alignment: go.Spot.TopLeft },
                )
            )  // end Horizontal Panel
        );
    }

    private template_HiddenNode(): go.Group {
        return this.g(go.Group, "Auto",
            {
                click: (e, obj) => this.context_UnhideNode(e, obj),
                cursor: 'pointer'
            },
            new go.Binding("visible", "visible"),
            new go.Binding("opacity", "opacity"),
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
                        text: this.hideIcon
                    },
                ),
                this.g(
                    go.Placeholder,
                    { padding: 0, alignment: go.Spot.TopLeft },
                )
            )  // end Horizontal Panel
        );
    }

    private template_HiddenStreamNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                click: (e, obj) => this.context_UnhideDirection(e, obj),
                cursor: 'pointer'
            },
            new go.Binding("opacity", "opacity"),
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
                        text: this.hideIcon
                    },
                )
            )  // end Horizontal Panel
        );
    }

    private template_HiddenSubNode(): go.Group {
        return this.g(go.Group, "Auto",
            {
                cursor: 'pointer'
            },
            new go.Binding("visible", "visible"),
            new go.Binding("opacity", "opacity"),
            this.g(
                go.Placeholder,
                { padding: 0, alignment: go.Spot.TopLeft },
            )
        );
    }

    private template_HiddenLeafNode(): go.Node {
        return this.g(go.Node, "Auto",
            new go.Binding("visible", "visible"),
            new go.Binding("opacity", "opacity")
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
                movable: false,
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
                        overflow: this.textOverflowStyle,
                        margin: new go.Margin(0, -1, 0, 0)
                    },
                    new go.Binding("text", "highlight").makeTwoWay(),
                    new go.Binding("visible", "highlight_visible").makeTwoWay(),
                    new go.Binding("background", "highlight_background").makeTwoWay()
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
                movable: false,
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
                        overflow: this.textOverflowStyle,
                        margin: new go.Margin(0, -1, 0, 0)
                    },
                    new go.Binding("text", "highlight").makeTwoWay(),
                    new go.Binding("visible", "highlight_visible").makeTwoWay(),
                    new go.Binding("background", "highlight_background").makeTwoWay()
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

    private template_LineageLinkNoAvoid(): go.Link {
        return this.g(
            go.Link, {
            routing: go.Link.Default,
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
                                {
                                    alignment: go.Spot.Right,
                                    margin: 5
                                }
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
            new go.Binding("opacity", "opacity"),
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
            new go.Binding("isSubGraphExpanded", "", this.isSubGraphExpanded.bind(this)).ofObject(),
            this.g(go.Panel,
                "Auto",

                this.g(
                    go.Shape,
                    "Border",
                    { strokeWidth: 2, isPanelMain: true, spot1: go.Spot.TopLeft, spot2: go.Spot.BottomRight },
                    new go.Binding("fill", "", (v) => go.Brush.mix("#ebebeb", this.lightenBoxColor, 0.7)),
                    new go.Binding("stroke", "", (v) => this.linkBackColor)
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
                    ),
            },
            new go.Binding("isSubGraphExpanded", "", this.isSubGraphExpanded.bind(this)).ofObject(),
            this.template_RootNodeContent()
        );
    }

    private template_RootNodeContent(): go.Panel {
        return this.g(go.Panel, "Vertical",
            this.template_relationshipTopPanel("relations"),
            this.template_ownersTopPanel("owners"),
            new go.Binding("", "", (obj: go.GraphObject, target: go.GraphObject) => {
                let longestPredicate: string = "";

                if (obj.part.data && obj.part.data["relations"]) {
                    var arr = obj.part.data["relations"] as Array<any>;
                    if (arr.length < this.autoCollapseRelationshipCount) {
                        arr.forEach(rel => {
                            if (rel["predicate"].length > longestPredicate.length) {
                                longestPredicate = rel["predicate"];
                            }
                        });
                    }
                    else if (arr.length > 0 && longestPredicate.length < "Relationships".length) {
                        longestPredicate = "Relationships";
                    }
                }
                if (obj.part.data && obj.part.data["owners"]) {
                    var arr = obj.part.data["owners"] as Array<any>;
                    if (arr.length < this.autoCollapseRelationshipCount) {
                        arr.forEach(rel => {
                            if (rel["text"].length > longestPredicate.length) {
                                longestPredicate = rel["text"];
                            }
                        });
                    }
                    else if (arr.length > 0 && longestPredicate.length < "Responsibilities".length) {
                        longestPredicate = "Responsibilities";
                    }
                }
                if (longestPredicate !== "") {
                    if (!obj.part.data["predicateWidth"]) {
                        obj.part.data["predicateWidth"] = this.calculateBadgeWidthByText(longestPredicate);
                    }
                    var partWidth = obj.part.getDocumentBounds().width;
                    if (obj.part.data["template"] === "FocalPortGroup") {
                        partWidth -= 42;
                    }
                    var diff = partWidth - (+obj.part.data["predicateWidth"]);
                    if (diff > 5) {
                        obj.part.data["predicateWidth"] = partWidth;
                    }
                }
            }).ofObject(),
            this.g(
                go.Panel,
                "Vertical",
                this.template_fixedBadge("relations"),
                this.template_fixedBadge("owners"),
                this.template_NodeContent()
            ) //end Vertical Panel
        );
    }

    private template_NodeContent(): go.Panel {
        var self = this;
        return this.g(go.Panel, "Auto",
            {
                name: "node-content"
            },
            this.g(
                go.Shape,
                "Rectangle",
                {
                    strokeWidth: 2,
                    isPanelMain: true,
                    margin: new go.Margin(2, 0, 0, 0),
                },
                new go.Binding("fill", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, 0.9)),
                new go.Binding("stroke", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount))
            ),
            this.g(go.Panel, "Vertical",
                new go.Binding("desiredSize", "", function (obj: go.GraphObject, target: go.GraphObject) {
                    var part = target.part;
                    return new go.Size(self.calculateNodeWidth(part), NaN);
                }).ofObject(),
                // title above Placeholder
                this.g(
                    go.Panel,
                    "Table",
                    // button next to TextBlock
                    {
                        stretch: go.GraphObject.Horizontal,
                    },
                    new go.Binding("background", "", v => (v.isHighlighted) ?
                        go.Brush.mix(this.selectionPathHighlightColor, this.selectionPathHighlightColor, v.backAmount) :
                        go.Brush.mix(v.data.back, this.lightenBoxColor, v.data.backAmount)
                    ).ofObject(),
                    this.g(
                        "SubGraphExpanderButton",
                        {
                            row: 1,
                            column: 1,
                            margin: new go.Margin(0, 5, 0, 3)
                        }
                    ),
                    //icon
                    this.g(
                        go.TextBlock,
                        {
                            editable: false,
                            font: this.fontLabelIcon,
                            row: 1,
                            column: 2
                        },
                        new go.Binding("stroke", "", (v) => this.template_GetContrast(v.back, v.backAmount)),
                        new go.Binding("text", "icon"),
                        new go.Binding("visible", "showIcon")
                    ),
                    //This TextBlock is placeholder for highlighted text
                    this.g(
                        go.TextBlock,
                        {
                            row: 1,
                            column: 3,
                            editable: false,
                            font: this.fontLabel,
                            stroke: this.fontLabelColor,
                            visible: false,
                            maxLines: this.textMaxLines,
                            overflow: this.textOverflowStyle,
                            margin: new go.Margin(5, 0, 5, 5),
                        },
                        new go.Binding("text", "highlight").makeTwoWay(),
                        new go.Binding("visible", "highlight_visible").makeTwoWay(),
                        new go.Binding("background", "highlight_background").makeTwoWay()
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 1,
                            column: 4,
                            editable: false,
                            margin: new go.Margin(5, 5, 5, 0),
                            font: this.fontLabel,
                            maxLines: this.textMaxLines,
                            maxSize: this.textMaxSize,
                            overflow: this.textOverflowStyle,
                            toolTip: this.template_Tooltip(),
                            stretch: go.GraphObject.Horizontal
                        },
                        new go.Binding("stroke", "", (v) => this.template_GetContrast(v.back, v.backAmount)),
                        new go.Binding("text", "text").makeTwoWay(),
                        new go.Binding("margin", "", (obj: go.GraphObject) => {
                            return obj.part.data["highlight_visible"] ? new go.Margin(5, 5, 5, 0) : 5;
                        }).ofObject()
                    )// end of Title containing Panel (higlighted + normal text)
                    ,
                    this.template_nodeCount()
                ),
                // end Horizontal Panel
                this.g(
                    go.Panel,
                    "Horizontal",
                    // button next to TextBlock
                    { stretch: go.GraphObject.Horizontal },
                    this.g(
                        go.Placeholder,
                        { padding: 2, alignment: go.Spot.TopLeft },
                    )
                )
                //end Horizontal Panel
            ) //end Vertical Panel,
        ); //end Auto Panel (main group Panel)
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

    private template_nodeCount(): go.Panel {
        var self = this;
        var badge = this.g(go.Panel,
            "Position",
            {
                row: 1,
                column: 100
            },
            new go.Binding("visible", "", function (v) {
                return self.showNodeCount;
            }),
            this.g(go.Shape, "Rectangle",
                {
                    position: new go.Point(0, 0),
                    maxSize: new go.Size(48, 16),
                    margin: new go.Margin(0, 3, 0, 0),
                    strokeWidth: 1,
                    stroke: "white",
                    fill: "white"
                },
                new go.Binding("maxSize", "", function (obj: go.GraphObject) {
                    if (obj.part.data.childCount) {
                        var lng = obj.part.data.childCount.toString().length - 1;
                        if (lng > 0) {
                            return new go.Size(16 + lng * 6, 16);
                        }
                    }
                    return new go.Size(16, 16);
                }).ofObject()
            ),
            this.g(go.TextBlock,
                {
                    editable: false,
                    margin: new go.Margin(0, 0, 0, 6),
                    font: this.fontLabel,
                    maxLines: this.textMaxLines,
                    overflow: this.textOverflowStyle,
                    background: "white",
                },
                new go.Binding("text", "", function (obj: go.GraphObject) {
                    return self.getPartChildrenCount(obj);
                }).ofObject()
            ));
        return badge;
    }

    private getPartChildrenCount(obj: go.GraphObject) {

        if (!obj || !obj.part || !obj.part.data)
            return 0;

        var value = +obj.part.data.childCount;

        return isNaN(value) ? 0 : value;
    }

    //#endregion

    /**
    * Responds to the change event from the shared Asset Browser ViewChange control.
    * @returns The DiagramType.
    */
    viewchange_Apply(e: DiagramType) {
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


    isProcessDiagramInEditMode: boolean = false;
    editProcess() {
        this.isFullScreen = false;
        this.isProcessDiagramInEditMode = true;
    }

    processDiagramSavedState($event) {
        if (this.displayConfiguration.DiagramType == DiagramType.Process)
            this.saveStateChanged.emit($event);
        else this.saveStateChanged.emit(null);
    }

    openProcessDiagramInfo() {
        if (this.processDiagramRef) {
            this.processDiagramRef.changeInfoPanelMode();
            this.processDiagramRef.myDiagram.requestUpdate();
        }

    }

    isProcessDiagramEmpty() {
        if (this.processDiagramRef) {
            if (this.processDiagramRef) {
                return this.processDiagramRef.isCanvasEmpty;
            }
        }
        return true;
    }

    getProcessDiagramViewType() {
        if (this.processDiagramRef) {
            if (this.processDiagramRef) {
                return this.processDiagramRef.viewType;
            }
        }
        return 'diagram';
    }

    private template_relationshipTopPanel(propertyName: string): go.Panel {
        return this.g(go.Panel, "Vertical",
            {
                alignment: go.Spot.Left,
            },
            new go.Binding("visible", "", (obj: go.GraphObject) => {
                var arrData = (obj.part.data[propertyName] as Array<any>)
                    .filter((x) => x["showBadge"] != false);
                return arrData.length < this.autoCollapseRelationshipCount;
            }).ofObject(),
            new go.Binding("itemArray", propertyName),
            {
                itemTemplate: this.template_relationshipBadge()
            });
    }

    private template_ownersTopPanel(propertyName: string): go.Panel {
        return this.g(go.Panel, "Vertical",
            {
                alignment: go.Spot.Left
            },
            new go.Binding("visible", "", (obj: go.GraphObject) => {
                var arrData = (obj.part.data[propertyName] as Array<any>)
                    .filter((x) => x["showBadge"] != false);
                return arrData.length < this.autoCollapseRelationshipCount;
            }).ofObject(),
            new go.Binding("itemArray", propertyName),
            {
                itemTemplate: this.template_ownershipBadge()
            });
    }

    private template_relationshipBadge(): go.Panel {
        return this.g(go.Panel, "Auto", {
            stretch: go.GraphObject.Horizontal,
            cursor: "pointer",
            name: "badge",
            click: (e, obj) => this.badge_ClickImpact(e, obj)
        },
            this.g(go.Panel, "Auto",
                this.g(go.Shape,
                    {
                        figure: "RoundedRectangle",
                        parameter1: 2,
                        strokeWidth: 1,
                        fill: "white",
                        stroke: this.badgeStrokeColor,
                        margin: new go.Margin(0, 0, -1, 0)
                    },
                    new go.Binding("fill", "", (obj) => {
                        return obj.data['expanded'] == true ? "#f1f1f3" : "white";
                    }).ofObject()
                ),
                new go.Binding("visible", "", function (part: go.Part) {
                    if (part.data['showBadge']) {
                        part.height = 26;
                    }
                    else {
                        part.height = 0;
                    }

                    return part.data['showBadge'];
                }).ofObject(),
                this.g(go.Panel, "Horizontal",
                    {
                        alignment: go.Spot.Left
                    },
                    this.template_badgeIconHolder(),
                    this.g(
                        go.TextBlock,
                        {
                            margin: new go.Margin(2, 4, 0, 0),
                            editable: false,
                            font: this.badgeFont,
                            stroke: this.badgeTextColor
                        },
                        new go.Binding("text", "predicate"),
                        new go.Binding("minSize", "", (obj: go.GraphObject, target: go.GraphObject) => {
                            if (obj.part.data["predicateWidth"]) {
                                var predicateWidth = +obj.part.data["predicateWidth"]
                                var width = this.calculateBadgeTextWidth(predicateWidth);
                                return new go.Size(width, NaN);
                            }
                        }).ofObject()
                    ),
                    this.g(go.Panel, "Auto",
                        {
                            alignment: go.Spot.Right,
                            margin: new go.Margin(0, 2, 0, 0)
                        },
                        new go.Binding("mouseEnter", "", (val) => {
                            return (ev: go.InputEvent, obj: go.GraphObject) => {
                                this.setRelationshipBadgeHoverState(val, ev, val.data, true, this);
                            }
                        }).ofObject(),
                        new go.Binding("mouseLeave", "", (val) => {
                            return (ev: go.InputEvent, obj: go.GraphObject) => {
                                this.setRelationshipBadgeHoverState(val, ev, val.data, false, this);
                            }
                        }).ofObject(),
                        this.g(go.Shape, "RoundedRectangle",
                            {
                                strokeWidth: 0,
                                parameter1: 2,
                                minSize: new go.Size(32, 16),
                                margin: new go.Margin(0, 0, 0, 4),
                                fill: this.badgeTextColor
                            },
                            new go.Binding("fill", "", (obj) => {
                                return obj.data['expanded'] == true ? "#7690a9" : this.badgeTextColor;
                            }).ofObject()
                        ),
                        this.g(
                            go.TextBlock,
                            {
                                margin: new go.Margin(1, 0, 0, 0),
                                editable: false,
                                font: this.badgeFont,
                                stroke: "white",
                            },
                            new go.Binding("text", "", (obj) => {
                                return this.formatBadgeNumber(+obj.data["count"]);
                            }).ofObject()
                        )
                    )
                )
            )
        );
    }

    private template_ownershipBadge(): go.Panel {
        return this.g(go.Panel, "Auto", {
            stretch: go.GraphObject.Horizontal,
            cursor: "pointer",
            name: "badge",
            click: (e, obj) => this.badge_ClickOwner(e, obj)
        },
            this.g(go.Panel, "Auto",
                this.g(go.Shape,
                    {
                        figure: "RoundedRectangle",
                        parameter1: 2,
                        strokeWidth: 1,
                        fill: "white",
                        stroke: this.badgeStrokeColor,
                        margin: new go.Margin(0, 0, -1, 0)
                    },
                    new go.Binding("fill", "", (obj) => {
                        return obj.data['expanded'] == true ? "#f1f1f3" : "white";
                    }).ofObject()
                ),
                new go.Binding("visible", "", function (part: go.Part) {
                    if (part.data['showBadge']) {
                        part.height = 26;
                    }
                    else {
                        part.height = 0;
                    }

                    return part.data['showBadge'];
                }).ofObject(),
                this.g(go.Panel, "Horizontal",
                    {
                        alignment: go.Spot.Left
                    },
                    this.template_badgeIconHolder(),
                    this.g(
                        go.TextBlock,
                        {
                            margin: new go.Margin(2, 4, 0, 0),
                            editable: false,
                            font: this.badgeFont,
                            stroke: this.badgeTextColor
                        },
                        new go.Binding("text", "text"),
                        new go.Binding("minSize", "", (obj: go.GraphObject, target: go.GraphObject) => {
                            if (obj.part.data["predicateWidth"]) {
                                var predicateWidth = +obj.part.data["predicateWidth"]
                                var width = this.calculateBadgeTextWidth(predicateWidth);
                                return new go.Size(width, NaN);
                            }
                        }).ofObject()
                    ),
                    this.g(go.Panel, "Auto",
                        {
                            alignment: go.Spot.Right,
                            margin: new go.Margin(0, 2, 0, 0)
                        },
                        new go.Binding("mouseEnter", "", (val) => {
                            return (ev: go.InputEvent, obj: go.GraphObject) => {
                                this.setRelationshipBadgeHoverState(val, ev, val.data, true, this);
                            }
                        }).ofObject(),
                        new go.Binding("mouseLeave", "", (val) => {
                            return (ev: go.InputEvent, obj: go.GraphObject) => {
                                this.setRelationshipBadgeHoverState(val, ev, val.data, false, this);
                            }
                        }).ofObject(),
                        this.g(go.Shape, "RoundedRectangle",
                            {
                                strokeWidth: 0,
                                parameter1: 2,
                                minSize: new go.Size(32, 16),
                                margin: new go.Margin(0, 0, 0, 4),
                                fill: this.badgeTextColor
                            },
                            new go.Binding("fill", "", (obj) => {
                                return obj.data['expanded'] == true ? "#7690a9" : this.badgeTextColor;
                            }).ofObject()
                        ),
                        this.g(
                            go.TextBlock,
                            {
                                margin: new go.Margin(1, 0, 0, 0),
                                editable: false,
                                font: this.badgeFont,
                                stroke: "white",
                            },
                            new go.Binding("text", "", (obj) => {
                                return this.formatBadgeNumber(+obj.data["count"]);
                            }).ofObject()
                        )
                    )
                )
            )
        );
    }

    //relationship panel
    private isRelationshipSelectorAvailable: boolean = false;
    private relationshipSelectorType: string = "";
    private followPart: go.Part = null;
    private relationshipData: any[];
    private transformOrigin: string = "0% 100%";

    isRelationshipBadgeTooltipVisible: boolean = false;
    relationshipBadgeHtml = "The item in this collection has 'xx' relationships to x other item.";
    private showTooltipTimeout;
    private setRelationshipBadgeHoverState(goObj: go.GraphObject, ev: go.InputEvent, data: go.ObjectData, isHover: boolean, self: AssetBrowserComponent) {
        try {
            self.isRelationshipBadgeTooltipVisible = isHover;
            let hierarchyKey: string = goObj.part.data["hierarchyKey"];
            var refHtmlElement = self.relationshipBadgesTooltipRef.nativeElement as HTMLElement;
            if (!refHtmlElement) return;
            if (isHover) {
                self.relationshipBadgeHtml = self.getRelBadgeTooltip(data, hierarchyKey);

                self.showTooltipTimeout = setTimeout(() => {
                    refHtmlElement.style.display = "block";
                    var diagramPos = (self.diagramRef as ElementRef).nativeElement.getBoundingClientRect();
                    var position = self.diagram.transformDocToView(ev.targetObject.getDocumentBounds().position);

                    var positionX = (diagramPos.x + position.x);

                    var leftDiff = positionX - refHtmlElement.clientWidth / 2;
                    if (leftDiff < 0) {
                        positionX += Math.abs(leftDiff);
                    }

                    var rightDiff = window.innerWidth - (positionX + refHtmlElement.clientWidth / 2);
                    if (rightDiff < 0) {
                        positionX -= Math.abs(rightDiff);
                    }

                    refHtmlElement.style.left = positionX + "px";
                    refHtmlElement.style.top = (diagramPos.y + position.y) + "px";

                    refHtmlElement.style.transform = "translateY(-100%) translateX(14px) translateX(-50%)";
                    refHtmlElement.style.position = "fixed";
                    self.cdRef.detectChanges();
                }, 100);
            }
            else {
                refHtmlElement.style.display = "none";
                if (self.showTooltipTimeout) {
                    window.clearTimeout(self.showTooltipTimeout);
                }
            }
        }
        catch (ex) {
            //using try|catch as functions running under gojs scope can swallow expections but stop working making debugging difficult
            console.log(ex);
        }
    }

    private updatePredicateSelectorPosition() {
        if (this.isRelationshipSelectorAvailable && this.relationshipBadgesRef) {
            var refHtmlElement = this.relationshipBadgesRef.nativeElement as HTMLElement;
            let showOnSide: string = "right";
            var position = this.diagram.transformDocToView(this.followPart.position);
            var posX = position.x;

            var correctionX = 0;
            var correctionY = 0;

            if (this.followPart.data["template"] === "FocalPortGroup") {
                correctionX = -23 * this.scale;
                correctionY = -12 * this.scale;
            }

            var parentEl = refHtmlElement.parentElement;
            if (parentEl.className.indexOf("diagram-container") !== -1) {
                var rightBound = parentEl.getBoundingClientRect().y + parentEl.getBoundingClientRect().width;
                var rightLocation = posX + this.followPart.getDocumentBounds().width + refHtmlElement.offsetWidth + parentEl.getBoundingClientRect().y + correctionX + 10;
                if (rightLocation > rightBound) {
                    showOnSide = "left";
                }
            }

            if (showOnSide == "left") {
                refHtmlElement.style.left = (posX - refHtmlElement.offsetWidth - correctionX - 2) + "px";
                refHtmlElement.style.top = (position.y - correctionY) + "px";
                this.transformOrigin = "100% 0%";
            }
            else {
                this.transformOrigin = "0% 0%";
                refHtmlElement.style.left = (posX + (this.followPart.getDocumentBounds().width * this.scale) + 2 + correctionX) + "px";
                refHtmlElement.style.top = (position.y - correctionY) + "px";
            }
        }
    }
    private groupedBadgeClick(obj: go.GraphObject, propName: string) {
        let lastHighlightedPart = this.highlightedPart;
        setTimeout(() => {
            this.followPart = obj.part;
            this.isRelationshipSelectorAvailable = obj.part.data["relExpanded" + propName];
            this.relationshipData = (obj.part.data[propName] as Array<any>)
                .filter((x) => x["showBadge"] != false);
            this.relationshipSelectorType = propName;
            if (propName === "relations") {
                this.relationshipData.forEach(rel => {
                    rel.text = rel.predicate;
                })
            }
            else {
                this.relationshipData.forEach(rel => {
                    rel.text = rel.responsibilityType;
                })
            }
            this.helper_HighlightPath(null, lastHighlightedPart);
            this.cdRef.markForCheck();
        }, 10);
    }

    private template_badgeIconHolder(textPartName: string = "badge-icon"): go.Panel {
        return this.g(go.Panel, "Spot",
            this.g(go.Shape, "Rectangle",
                {
                    width: 20,
                    height: 20,
                    fill: "transparent",
                    stroke: "transparent",
                    margin: new go.Margin(0, 0, 0, 0)
                }
            ),
            this.g(
                go.TextBlock,
                {
                    editable: false,
                    font: '14px FontAwesome',
                    stroke: this.badgeTextColor,
                    name: textPartName,
                    maxSize: new go.Size(14, 14),
                    alignment: new go.Spot(0.5, 0.5)
                },
                new go.Binding("text", "", function (obj: go.Part) {
                    if (obj.data['showLoading'] == true)
                        return FontAwesomeHelper.GetHtmlCode("fa-spinner");

                    if (obj.data['expanded'] == true)
                        return FontAwesomeHelper.GetHtmlCode("fa-minus-square");
                    else
                        return FontAwesomeHelper.GetHtmlCode("fa-plus-square");

                }).ofObject()
            )
        );
    }

    private template_fixedBadge(propertyName: string): go.Panel {
        return this.g(go.Panel, "Auto", {
            alignment: go.Spot.Left,
            cursor: "pointer",
            click: (e, obj) => {
                obj.part.data['relExpanded' + propertyName] = !obj.part.data['relExpanded' + propertyName];
                this.groupedBadgeClick(obj, propertyName);
            }
        },
            new go.Binding("visible", "", (obj: go.GraphObject) => {
                var arrData = (obj.part.data[propertyName] as Array<any>)
                    .filter((x) => x["showBadge"] != false);
                return arrData.length >= this.autoCollapseRelationshipCount;
            }).ofObject(),
            this.g(go.Panel, "Auto",
                this.g(go.Shape,
                    {
                        figure: "RoundedRectangle",
                        parameter1: 2,
                        strokeWidth: 1,
                        fill: "white",
                        stroke: this.badgeStrokeColor,
                        margin: new go.Margin(0, 0, 0, 0)
                    }
                ),
                new go.Binding("visible", "", function (part: go.Part) {
                    return true;
                }).ofObject(),
                this.g(go.Panel, "Horizontal",
                    {
                        alignment: go.Spot.Left
                    },
                    this.g(go.Panel, "Spot",
                        this.g(go.Shape, "Rectangle",
                            {
                                width: 20,
                                height: 20,
                                fill: "transparent",
                                stroke: "transparent",
                                margin: new go.Margin(0, 0, 0, 0)
                            }
                        ),
                        this.g(
                            go.TextBlock,
                            {
                                editable: false,
                                font: '14px FontAwesome',
                                stroke: this.badgeTextColor,
                                maxSize: new go.Size(14, 14),
                                alignment: new go.Spot(0.5, 0.5)
                            },
                            new go.Binding("text", "", function (obj: go.Part) {
                                if (obj.data['relExpanded' + propertyName] == true)
                                    return FontAwesomeHelper.GetHtmlCode("fa-minus-square");
                                else
                                    return FontAwesomeHelper.GetHtmlCode("fa-plus-square");

                            }).ofObject()
                        )
                    ), this.g(
                        go.TextBlock,
                        {
                            margin: new go.Margin(2, 4, 0, 0),
                            editable: false,
                            font: this.badgeFont,
                            stroke: this.badgeTextColor,
                        },
                        new go.Binding("text", "", (obj) => {
                            return propertyName === "relations" ? "Relationships" : "Responsibilities";
                        }),
                        new go.Binding("width", "", (obj: go.GraphObject, target: go.GraphObject) => {
                            if (obj.part.data["predicateWidth"]) {
                                var predicateWidth = +obj.part.data["predicateWidth"]
                                var width = this.calculateBadgeTextWidth(predicateWidth);
                                return width;
                            }
                        }).ofObject()
                    ),
                    this.g(go.Panel, "Auto",
                        {
                            alignment: go.Spot.Right,
                            margin: new go.Margin(0, 2, 0, 0)
                        },
                        new go.Binding("mouseEnter", "", (val) => {
                            return (ev: go.InputEvent, obj: go.GraphObject) => {
                                let totalCount: number = 0;
                                (obj.part.data[propertyName] as Array<any>).forEach(d => totalCount += d.count);
                                var data = { consolidated: true, count: totalCount };
                                this.setRelationshipBadgeHoverState(val, ev, data, true, this);
                            }
                        }).ofObject(),
                        new go.Binding("mouseLeave", "", (val) => {
                            return (ev: go.InputEvent, obj: go.GraphObject) => {
                                this.setRelationshipBadgeHoverState(val, ev, val.data, false, this);
                            }
                        }).ofObject(),
                        this.g(go.Shape, "RoundedRectangle",
                            {
                                strokeWidth: 0,
                                parameter1: 2,
                                minSize: new go.Size(32, 16),
                                margin: new go.Margin(0, 0, 0, 4),
                                fill: this.badgeTextColor
                            },
                        ),
                        this.g(
                            go.TextBlock,
                            {
                                margin: new go.Margin(1, 0, 0, 0),
                                editable: false,
                                font: this.badgeFont,
                                stroke: "white",
                            },
                            new go.Binding("", "", (obj: go.GraphObject, target: go.TextBlock) => {
                                let totalCount: number = 0;

                                (obj.part.data[propertyName] as Array<any>).filter((x) => x["showBadge"] != false).forEach(d => totalCount += d.count);
                                if (isNaN(totalCount)) {
                                    target.text = '-';
                                    return;
                                }
                                target.text = totalCount.toString();
                            }).ofObject()
                        )
                    )
                )
            )
        );
    }


    private htmlBadgeClick(idx: number) {
        if (this.relationshipSelectorType === "relations") {
            this.badge_ClickImpact(null, this.followPart.part, idx);
        }
        else {
            this.badge_ClickOwner(null, this.followPart.part, idx);
        }

    }

    private calculateBadgeWidthByText(txt: string): number {
        return 72 + txt.length * 6;
    }
    private calculateBadgeTextWidth(width: number): number {
        var ret = width - 66;
        if (ret > 300)
            ret = 300;
        return ret;
    }
    private calculateBadgeTextWidthForGroupNode(width: number): number {
        return width - 60;
    }

    private formatBadgeNumber(countValue: number): string {
        if (isNaN(countValue)) {
            return "---";
        }

        if (countValue > 100000000) {
            countValue = (countValue / 1000000000)
            return countValue.toFixed(1) + "B";
        }
        if (countValue > 100000) {
            countValue = (countValue / 1000000)
            return countValue.toFixed(1) + "M";
        }
        if (countValue > 1000) {
            countValue = (countValue / 1000)
            return countValue.toFixed(1) + "K";
        }
        return countValue.toString();
    }
    private calculateNodeWidth(object: go.Part) {
        var maxWidth: number = 0;
        try {
            var part = object.findTopLevelPart();
            if (part.data["predicateWidth"]) {
                var predicateWidth = +part.part.data["predicateWidth"];
                if (!isNaN(predicateWidth)) {
                    maxWidth = predicateWidth - 2;
                }
            }

            var data = part.diagram.nodes.filter(x => x.data['hierarchyKey'] == part.data['hierarchyKey']);
            var maxCharCount = 0;
            data.each(d => {
                if (d.data && d.data["text"]) {
                    let currentCharCount: number = d.data["text"].length;

                    //Additional size for leaf assets for padding
                    if (d.data["leaf"]) {
                        currentCharCount += 6;
                    }

                    if (currentCharCount > maxCharCount) {
                        maxCharCount = currentCharCount;
                    }
                }
            });

            //set max top width depending on max character count withing hierarchy
            var nodeWidth = 80 + maxCharCount * 6;
            if (!isNaN(nodeWidth) && nodeWidth > maxWidth) {
                maxWidth = nodeWidth;
            }

            if (maxWidth > 260)
                maxWidth = 260;

            if (object != part) {
                var depth = object.findSubGraphLevel();
                maxWidth = maxWidth - depth * 6;
            }

            if (maxWidth < 100) {
                maxWidth = 100;
            }
        }
        catch (ex) {
            console.log(ex);
            maxWidth = 260;
        }

        return maxWidth;
    }

    private isSubGraphExpanded(obj: go.GraphObject, target: go.GraphObject) {
        try {
            if (!target['autoExpandSet']) {
                var nodeCount = this.getPartChildrenCount(target);
                if (!isNaN(nodeCount)) {
                    var setValue = this.autoCollapseNodeCount > nodeCount
                    target['autoExpandSet'] = true;
                    return setValue;
                }
                else {
                    return false;
                }
            }
            return target["isSubGraphExpanded"];
        }
        catch (ex) {
            console.log(ex);
            return true;
        }

    }

    private getRelBadgeTooltip(rel: any, hierarchyKey: string) {
        var title = "";

        var dataCount = this.diagram.nodes.filter(x =>
            x.data['hierarchyKey'] == hierarchyKey
        ).count - 1;

        if (rel.text) {
            title = rel.text;
        }

        if (rel.predicate) {
            title = rel.predicate;
        }

        if (rel.responsibilityType) {
            title = rel.responsibilityType;
        }

        if (!rel.consolidated) {
            if (dataCount > 1) {
                return `Items in this collection have '${title}' relationships to ${rel.count} other items.<br/>Click to toggle the display of related items.`
            }
            else {
                return `The item in this collection has '${title}' relationships to ${rel.count} other item.<br/>Click to toggle the display of related item.`
            }
        }
        else {
            if (dataCount > 1) {
                return `Items in this collection have relationships to ${rel.count} other items.<br/>Click to toggle the display of relationships.`
            }
            else {
                return `The item in this collection has relationships to ${rel.count} other items.<br/>Click to toggle the display of relationships.`
            }
        }
    }

    onEditClick($event) {
        this.selectedDiagramAsset.AssetTypeUid = $event.assetTypeUid;
        this.showEditor = true;
    }

    saveClicked($event) {
        this.showEditor = false;
        var uid = $event.assetUid as string;
        var keys: string[] = [];
        this.diagram.nodes.each((node) => {
            var assetUid = (node.data.assetUid as string).toLowerCase()
            if (assetUid === uid.toLowerCase()) {
                keys.push(node.data.key);
            }
        });

        if (uid) {
            this.assetService.getAssetPath(uid).subscribe((res) => {
                var assetPath = res[0].DisplayPath;
                var currentPath = "";
                if (assetPath) {
                    var startIdx = assetPath.lastIndexOf('>') + 1;
                    var newPath = assetPath.substring(startIdx, assetPath.length);
                    try {
                        var model = this.diagram.model;

                        keys.forEach((key) => {
                            var data = model.findNodeDataForKey(key);
                            currentPath = data.text;

                            model.startTransaction("modified property");
                            model.set(data, "text", newPath);
                            model.commitTransaction("modified property");
                        });
                    } catch (e) {
                    }

                    this.diagram.redraw();

                    if (this.assetUid.toLowerCase() === uid.toLowerCase()) {
                        this.secondaryNavService.updateObject('areaTitle', newPath);
                    }
                    this.breadcrumbsService.updateCurrentPath(currentPath, newPath);
                    this.assetDetailComponent.load();
                }
            });
        }
    }

    get hasAnySidePanelAsset(): boolean {
        return this.selectedDiagramAsset || this.selectedAsset || this.selectedReferenceItem || this.selectedTag;
    }
}
