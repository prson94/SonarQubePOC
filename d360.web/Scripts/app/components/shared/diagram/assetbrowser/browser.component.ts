import * as go from 'gojs';
import * as _ from 'lodash';
import { AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChange, SimpleChanges, EventEmitter, Output, AfterViewChecked } from '@angular/core';
import {
    DiagramObjectType,
    AssetBrowserLineageApiRequestModel,
    AssetBrowserTranslation,
    AssetBrowserDirection,
    AssetBrowserDiagramAsset,
    AssetBrowserTranslationNode,
    AssetBrowserTranslationLink,
    AssetBrowserLineageApiResponseModel,
    AssetBrowserLineageApiRelationshipModel,
    AssetBrowserTranslationRelationCount,
    AssetBrowserImpactApiRequestModel,
    AssetBrowserImpactApiAssetRequestModel,
    AssetBrowserLineageApiItemModel,
    FilterAncestryMode,
    FilterAncestryOption,
    AssetBrowserFilterModel,
    AssetTypeFilter,
    FilterSelectionsModel
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

    @ViewChild('diagram', { static: false }) diagramRef;

    DiagramObjectType = DiagramObjectType;

    private requestModel: AssetBrowserLineageApiRequestModel;
    private responseModel: AssetBrowserLineageApiResponseModel;
    private revealedKeys: string[] = [];
    private originalAssetUid: string;
    private menuItems: MenuItem[] = [];

    private isInfoWindowVisible: boolean = false;
    private isInfoTabDisabled: boolean = true;
    private isWindowLoading = false;
    private isAddRelationshipWindowVisible: boolean = false;
    private showWindowTabs: boolean = false;
    private tab: string = "info";
    private selectedDiagramAsset: AssetBrowserDiagramAsset;
    private isFullScreen: boolean = false;
    private loadingText: string = "";
    private fromRefresh: boolean = false;

    //#region Filters

    isFilterWindowVisible: boolean = false;
    filterModel: AssetBrowserFilterModel = new AssetBrowserFilterModel();
    private readonly filterKey = 'asset-browser-filter';
    private storage = window.sessionStorage;

    filtersLoading: boolean = true;
    selectedFilterAssetTypes: TreeNode[] = [];
    selectedFilterPredicates: TreeNode[] = [];
    filterSelectionsModel: FilterSelectionsModel = new FilterSelectionsModel([], []);

    //#endregion

    //#region Constants

    private readonly fontContextMenu: string = "12px 'Source Sans Pro'";
    private readonly fontContextMenuShowDetails: string = "bold 12px 'Source Sans Pro'";
    private readonly fontRelationBadge: string = "8pt 'Source Sans Pro'";
    private readonly fontRelationBadgeColor: string = "#404040";
    private readonly fontRelationBadgeCountColor: string = "white";
    private readonly fontLabelIcon: string = "12px FontAwesome";
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
    private readonly leafBackColor: string = '#fff';
    private zoomText: string = '100%';


    //#endregion

    //#region Control Properties

    constructor(
        private myElement: ElementRef,
        private browserService: BrowserService,
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
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
        this.cdRef.markForCheck();
    }

    public ngAfterViewChecked() {

        var panelElements: HTMLElement[] = this.myElement.nativeElement.querySelectorAll('.asset-browser-window-content');
        (function () {
            if (typeof NodeList.prototype.forEach === "function") return false;
            panelElements.forEach = Array.prototype.forEach;
        })();
        panelElements.forEach(el => {
            var diagramSize = +this.diagramRef.nativeElement.style.height.replace('px', '');
            el.style.height = (diagramSize - 120) + 'px';
            el.style.maxHeight = (diagramSize - 120) + 'px';
        });

    }

    public ngOnDestroy() {
        this.diagram.div = null;    // Garbage collection.
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
        alert('Alerts coming soon');
    }

    private panelButtonClick(name: string) {
        switch (name) {
            case 'add':
                this.isAddRelationshipWindowVisible = !this.isAddRelationshipWindowVisible;
                this.isInfoWindowVisible = false;
                this.isFilterWindowVisible = false;
                break;
            case 'filter':
                this.isAddRelationshipWindowVisible = false;
                this.isInfoWindowVisible = false;
                this.isFilterWindowVisible = !this.isFilterWindowVisible;
                break;
            case 'info':
                this.isAddRelationshipWindowVisible = false;
                this.isFilterWindowVisible = false;
                this.isInfoWindowVisible = !this.isInfoWindowVisible;
                break;
        }
    }

    private infoButtonClick(e) {
        this.panelButtonClick('info');

        if (this.isInfoWindowVisible && this.selectedDiagramAsset != null && this.selectedDiagramAsset.Loaded == false) {
            this.showDetails(this.selectedDiagramAsset.Uid);
        }

        this.cdRef.markForCheck();
    }

    private setFilterWindow(options: FilterSelectionsModel) {
        this.filterSelectionsModel = options;

        //#region Asset Types

        var assetTypes: TreeNode[] = new Array();
        let classIDs: number[] = [
            +AssetTypeClass.BusinessAsset,
            +AssetTypeClass.Model,
            +AssetTypeClass.Policy,
            +AssetTypeClass.Rule,
            +AssetTypeClass.TechnicalAsset
        ];
        this.filterSelectionsModel.AssetTypeOptions.forEach(at => {
            if (classIDs.findIndex(c => c == at.ClassId) > -1) {

                let thisClassNode = assetTypes.find(c => c.data == 'C' +at.ClassId);
                let nodeExists: boolean = (thisClassNode != undefined);
                if (!nodeExists) {
                    thisClassNode = {
                        label: at.Class,
                        data: 'C'+at.ClassId,
                        children: []
                    };
                }
                thisClassNode.children.push({
                    label: at.Path,
                    data: at.AssetTypeId
                });

                if (!nodeExists) {
                    assetTypes.push(thisClassNode);
                }
            }
        });
        assetTypes.sort((a, b) => (a.label > b.label) ? 1 : -1);
        this.filterSelectionsModel.FilterAssetTypes = assetTypes;
        this.selectedFilterAssetTypes = this.getTreeNodeSelectionNodes(this.filterModel.SelectedAssetTypes, this.filterSelectionsModel.FilterAssetTypes);

        //#endregion

        //#region Predicates

        this.filterSelectionsModel.PredicateOptions.forEach(p => {
            let thisPredicateTypeNode = this.filterSelectionsModel.FilterPredicates.find(c => c.data == 'F' +p.TypeId);
            let nodeExists: boolean = (thisPredicateTypeNode != undefined);
            if (!nodeExists) {
                thisPredicateTypeNode = {
                    label: p.Type,
                    data: 'F'+p.TypeId, 
                    children: []
                };
            }
            thisPredicateTypeNode.children.push({
                label: p.Name.substring(0, 50) + ' / ' + p.Inverse.substring(0, 50),
                data: p.Id
            });

            if (!nodeExists) {
                this.filterSelectionsModel.FilterPredicates.push(thisPredicateTypeNode);
            }
        });
        this.filterSelectionsModel.FilterPredicates.sort((a, b) => (a.label > b.label) ? 1 : -1);
        this.selectedFilterPredicates = this.getTreeNodeSelectionNodes(this.filterModel.SelectedPredicates, this.filterSelectionsModel.FilterPredicates);

        //#endregion

        this.filtersLoading = false;
        this.panelButtonClick('filter');
        this.cdRef.markForCheck();
    }


    private filterButtonClick(e) {
        if (this.filterSelectionsModel.AssetTypeOptions.length == 0) {
            this.filtersLoading = true;
            this.browserService
                .getFilterOptions()
                .subscribe(options => this.setFilterWindow(options));
        }
        else {
            this.filtersLoading = false;
            this.panelButtonClick('filter');
            this.cdRef.markForCheck();
        }
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

    //#region helper methods

    private OwnershipTabEnabled() {
        let enabled: boolean = false;

        if (this.selectedDiagramAsset) {
            enabled = (this.selectedDiagramAsset.Owners.length > 0);
        }

        return enabled;
    }

    private addButtonSelectedClass() {
        return this.isAddRelationshipWindowVisible ? "selected" : "";
    }

    private filterButtonSelectedClass() {
        return this.isFilterWindowVisible ? "right-margin-4 selected" : "right-margin-4";
    }

    private infoButtonSelectedClass() {
        return this.isInfoWindowVisible ? "selected" : (this.isInfoTabDisabled ? "disabled" : "");
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

    private hideDeselectedAssetTypes() {
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
                    this.unhideNode(tn);
                }
            });

        if (nodesToHide.length > 0) {
            nodesToHide.forEach(n => {
                let group: any = this.diagram.findNodeForKey(n.key);
                this.hideIndividualNode(n, group);
            });
        }
    }

    private hideDeselectedPredicates() {
        // Now loop through selected asset types, as those are the ones we need to hide.
        let nodesToHide: AssetBrowserTranslationNode[] = [];

        //#region Hide Badge

        this.diagram.startTransaction('predicateBadge');
        this.diagram.findTopLevelGroups().each(g => {
            let topLevelNode: AssetBrowserTranslationNode = g.data as AssetBrowserTranslationNode;
            topLevelNode.relations.forEach(rC => {
                if (this.filterModel.SelectedPredicates.findIndex(v => { return v == rC.predicateId; }) > -1) {
                    this.diagram.model.setDataProperty(rC, "showBadge", false);
                }
                else {
                    this.diagram.model.setDataProperty(rC, "showBadge", true);
                }
            });
        });
        this.diagram.commitTransaction('predicateBadge');

        //#endregion Badge

        //#region Hide Node

        this.diagram.links.each(link => {
            let linkData: AssetBrowserTranslationLink = link.data as AssetBrowserTranslationLink;
            if (linkData.predicateIds) {
                let g: any = this.diagram.findNodeForKey(linkData.to);

                if (linkData.predicateIds.filter(l => {
                    return this.filterModel.SelectedPredicates.findIndex(v => { return v == l; }) > -1
                }).length > 0) {
                    this.hideIndividualNode(g.data as AssetBrowserTranslationNode, g);
                }
                else {
                    if (this.filterModel.SelectedAssetTypes.findIndex(v => { return v == (g.data as AssetBrowserTranslationNode).assetTypeId; }) == -1) {
                        this.unhideNode(g.data as AssetBrowserTranslationNode);
                    }
                }
            }
        });

        //#endregion
    }

    private hideIndividualNode(node: AssetBrowserTranslationNode, group: any) {
        this.diagram.startTransaction('hide');

        let hideNode = new AssetBrowserTranslationNode();

        hideNode.subgraph = new AssetBrowserTranslation();
        hideNode.template = "HiddenData";
        hideNode.assetTypeId = node.assetTypeId;
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
            this.diagramModelAsGraph().addLinkData({ from: l.from, to: hideNode.key, predicateIds: l.predicateIds });
        });

        downstreamLinks.forEach(l => {
            hideNode.subgraph.links.push(l);
            this.diagramModelAsGraph().removeLinkData(l);
            this.diagramModelAsGraph().addLinkData({ from: hideNode.key, to: l.to, predicateIds: l.predicateIds });
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
        //Set all to not highlighted.
        obj.diagram.nodes.each(n => {
            n.isHighlighted = false;
        });

        if (obj.key) {
            // Highlight the selected node.
            obj.isHighlighted = true;

            // Recurse through and highlight based on the atomic (non-grouped) links.
            this.highlightNodeImpacts(obj.key.toString(), AssetBrowserDirection.Both);
        }
        else {
            // You are clicking on a link instead.
            let link = this.diagram.findLinkForData(obj.data);
            //this.diagram.nodes.iterator.each(n => {
            //    n.containingGroup
            //});
            //link.fromNode.
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
    }

    private highlightNodeImpacts(key: string, direction: AssetBrowserDirection) {

        let fwd: boolean = ((direction == AssetBrowserDirection.Both) || (direction == AssetBrowserDirection.Forward));
        let bwd: boolean = ((direction == AssetBrowserDirection.Both) || (direction == AssetBrowserDirection.Backward));

        this.responseModel.intersects.forEach(l => {

            // Loop through the links to find ones where this node is subject, then traverse each one and do the same thing, recursively.
            if (fwd) {
                if (l.subjectKey == key) {
                    let oNode = this.diagram.findNodeForKey(l.objectKey);
                    if (oNode) {
                        oNode.isHighlighted = true;
                        this.highlightNodeImpacts(l.objectKey, AssetBrowserDirection.Forward);
                    }
                }
            }

            // Loop through the links to find ones where this node is object, then traverse each one and do the same thing, recursively.
            if (bwd) {
                if (l.objectKey == key) {
                    let sNode = this.diagram.findNodeForKey(l.subjectKey);
                    if (sNode) {
                        sNode.isHighlighted = true;
                        this.highlightNodeImpacts(l.subjectKey, AssetBrowserDirection.Backward);
                    }
                }
            }
        });

    }

    private initializeDiagram() {

        this.initializeCustomShapes();

        this.diagram = this.createDiagram();

        var forelayer = this.diagram.findLayer("Foreground");
        this.diagram.addLayerBefore(this.g(go.Layer, { name: "Links" }), forelayer);

        this.diagram.groupTemplateMap.add("PortGroup", this.createPortGroupNode());
        this.diagram.groupTemplateMap.add("Group", this.createGroupNode());

        this.diagram.nodeTemplateMap.add("MoreData", this.createMoreDataNode());
        this.diagram.nodeTemplateMap.add("HiddenData", this.createHiddenDataNode());

        this.diagram.nodeTemplate = this.createListItemNode();

        this.diagram.linkTemplateMap.add("", this.createDefaultLink());

        this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.loadFilter();
        this.populateDiagram().subscribe(bComplete => {
            this.hideDeselectedAssetTypes();
            this.hideDeselectedPredicates();
        });
    }

    private populateDiagram(): Observable<boolean> {
        let dgmObs: Observable<boolean>;

        dgmObs = new Observable(obs => {
            this.isLoading = true;
            this.loadingText = "Retrieving lineage from Govern..";
            this.responseModel = null;
            this.revealedKeys = [];

            this.requestModel = new AssetBrowserLineageApiRequestModel();
            this.requestModel.AssetUids = new Array();
            this.requestModel.AssetUids.push(this.assetUid);
            this.requestModel.IsReveal = false;
            this.requestModel.StartHop = 0;
            this.requestModel.Direction = AssetBrowserDirection.Both;
            this.requestModel.Hops = this.filterModel.NumberOfHops;

            this.browserService.getAssetLineage(this.requestModel)
                .subscribe(data => {
                    this.responseModel = data;
                    this.loadingText = "Determining links and meaning...";
                    data = this.browserService.convertResponseModel(data, this.filterModel.AncestryMode);
                    let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(data);
                    this.parseData(translationModel);
                    this.resizeDiagram();
                    this.diagram.scale = 1;
                    this.diagram.alignDocument(go.Spot.Center, go.Spot.Center);
                    this.loadingText = "";
                    this.isLoading = false;

                    this.cdRef.markForCheck();

                    obs.next(true);
                    obs.complete();
                });
        });

        return dgmObs;
    }

    private parseData(data: AssetBrowserTranslation, append: boolean = false) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;

        //#region add data to diagram model
        if (append === true) {
            data.nodes.forEach(n => {
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

            data.links.forEach(l => {
                if (dm.linkDataArray.find(i => i.to == l.to && i.from == l.from) == null)
                    dm.addLinkData(l);
            });

        } else {
            data.nodes.forEach(n => {
                n.showIcon = this.filterModel.DisplayIcons;
            });
            dm.nodeDataArray = data.nodes;
            dm.linkDataArray = data.links;
        }
        //#endregion

        //#region process dynamic elements like reveal nodes and relation badges
        this.diagram.findTopLevelGroups().each(g => {
            let children = g.findSubGraphParts();
            let childAssets = [];
            let childRelations = [];

            childAssets.push(g.data.assetUid);
            children.each(c => {

                let data = c.data;
                childAssets.push(data.assetUid);

                if (data.relations != null && data.relations.length > 0) {
                    for (let i = 0; i < data.relations.length; i++) {
                        let r = data.relations[i];
                        let rel = childRelations.find(c => c.predicateUid == r.predicateUid);
                        if (rel != null) {
                            rel.count += r.count;
                        }
                        else if (g.data.relations.find(c => c.predicateUid == r.predicateUid) == null) {
                            childRelations.push(r);
                        }
                    }
                    data.relations = [];
                }
            });

            g.data.relations = g.data.relations.concat(childRelations);
            this.diagram.model.setDataProperty(g.data, "relations", g.data.relations.slice());
            this.diagram.model.setDataProperty(g.data, "showBadges", this.filterModel.DisplayBadges);

            if (g.data.showReveal != AssetBrowserDirection.None) {
                let dir = g.data.showReveal;
                let revealKey = g.data.key + '_reveal'



                if (dir == AssetBrowserDirection.Forward || dir == AssetBrowserDirection.Both) {
                    if (dm.findNodeDataForKey(revealKey + '_Forward') == null) {
                        dm.addNodeData({
                            template: 'MoreData',
                            key: revealKey + '_Forward',
                            back: g.data.back,
                            showReveal: AssetBrowserDirection.Forward,
                            relations: g.data.relations,
                            assetUid: g.data.assetUid,
                            hop: g.data.hop,
                            assetUids: childAssets
                        });

                        dm.addLinkData({
                            from: g.data.key,
                            to: revealKey + '_Forward'
                        });
                    }
                }

                if (dir == AssetBrowserDirection.Backward || dir == AssetBrowserDirection.Both) {
                    if (dm.findNodeDataForKey(revealKey + '_Backward') == null) {
                        dm.addNodeData({
                            template: 'MoreData',
                            key: revealKey + '_Backward',
                            back: g.data.back,
                            showReveal: AssetBrowserDirection.Backward,
                            relations: g.data.relations,
                            assetUid: g.data.assetUid,
                            hop: g.data.hop,
                            assetUids: childAssets
                        });

                        dm.addLinkData({
                            from: revealKey + '_Backward',
                            to: g.data.key
                        });
                    }
                }

                g.data.showReveal = AssetBrowserDirection.None;
            }
        });
        //#endregion

        this.diagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private getMoreData(e: go.InputEvent, obj: go.GraphObject) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let data = obj.part.data;

            if (data.showReveal != AssetBrowserDirection.None) {
                this.diagram.startTransaction('reveal');

                this.diagramModelAsGraph().linkDataArray.filter(l => l.to == data.key).forEach(l => {
                    this.revealedKeys.push(this.diagram.model.findNodeDataForKey(l.from).key);
                });

                this.diagramModelAsGraph().linkDataArray.filter(l => l.from == data.key).forEach(l => {
                    this.revealedKeys.push(this.diagram.model.findNodeDataForKey(l.to).key);
                });

                let model = new AssetBrowserLineageApiRequestModel();
                model.AssetUids = data.assetUids;
                model.IsReveal = true;
                model.StartHop = data.hop;
                model.Direction = data.showReveal;
                model.Hops = 1;

                this.browserService.getAssetLineage(model)
                    .subscribe(response => {
                        response.assets.forEach(a => {
                            if (this.responseModel.assets.find(r => r.key == a.key) == null) {
                                this.responseModel.assets.push(a);
                            }
                        });

                        response.intersects.forEach(i => {
                            if (this.responseModel.intersects.find(r => r.subjectKey == i.subjectKey && r.objectKey == i.objectKey) == null) {
                                this.responseModel.intersects.push(i);
                            }
                        });

                        let model = this.browserService.convertResponseModel(this.responseModel, this.filterModel.AncestryMode);
                        let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(model);

                        this.revealedKeys.forEach(n => {
                            let t = translationModel.nodes.find(t => t.key == n);
                            if (t != null)
                                t.showReveal = AssetBrowserDirection.None;
                        });

                        this.diagramModelAsGraph().removeNodeData(data);
                        let l = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == data.key || l.from == data.key);
                        this.diagramModelAsGraph().removeLinkDataCollection(l);

                        this.parseData(translationModel);

                        this.diagram.commitTransaction('reveal');

                        this.hideDeselectedAssetTypes();
                        this.hideDeselectedPredicates();
                    });
            }
        }
    }

    private reOrderLayout() {
        this.diagram.layout.invalidateLayout();
        this.diagram.requestUpdate();
    }

    private refreshDiagram() {
        this.assetUid = this.originalAssetUid;
        this.fromRefresh = true;
        this.populateDiagram().subscribe(bComplete => {
            this.fromRefresh = false;
            this.hideDeselectedAssetTypes();
            this.hideDeselectedPredicates();
        });
    }

    private findSubGraph(startKey: string, direction: AssetBrowserDirection): AssetBrowserTranslation {
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

            if (direction == AssetBrowserDirection.Forward || direction == AssetBrowserDirection.Both) {

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
            if (direction == AssetBrowserDirection.Backward || direction == AssetBrowserDirection.Both) {

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

                    if (data.assetUid != null && data.assetUid != '00000000-0000-0000-0000-000000000000') { //selected item is an asset
                        this.isInfoTabDisabled = false;
                        if (this.isInfoWindowVisible) {
                            if (this.selectedDiagramAsset == null || this.selectedDiagramAsset.Uid != data.assetUid) {
                                this.showDetails(data.assetUid);
                            }
                        } else {
                            this.selectedDiagramAsset = new AssetBrowserDiagramAsset();
                            this.selectedDiagramAsset.Uid = data.assetUid;
                            this.cdRef.markForCheck();
                        }
                    } else {
                        this.selectedDiagramAsset = null;
                        this.isInfoTabDisabled = true;
                        this.isInfoWindowVisible = false;
                        this.cdRef.markForCheck();
                    }
                } else if (parts.count == 0) {
                    if (this.isInfoWindowVisible) {
                        this.selectedDiagramAsset = null;
                        this.isInfoTabDisabled = true;
                        this.isInfoWindowVisible = false;
                        this.cdRef.markForCheck();
                    } else {
                        this.isInfoTabDisabled = true;
                        this.cdRef.markForCheck();
                    }
                }
            }
        }
    }

    private filterAncestryModeChange(): void {
        this.saveFilter();
        this.isLoading = true;
        this.loadingText = "Determining links and meaning...";
        let data = this.browserService.convertResponseModel(this.responseModel, this.filterModel.AncestryMode);
        let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(data);
        this.parseData(translationModel);
        this.resizeDiagram();
        this.diagram.zoomToFit();
        this.diagram.alignDocument(go.Spot.Center, go.Spot.Center);
        this.loadingText = "";
        this.isLoading = false;
        this.fromRefresh = false;
        this.cdRef.markForCheck();

        this.hideDeselectedAssetTypes();
        this.hideDeselectedPredicates();
    }

    private filterBadgesChange(): void {
        this.diagram.startTransaction();
        this.diagram.findTopLevelGroups().each(g => {
            this.diagram.model.setDataProperty(g.data, "showBadges", this.filterModel.DisplayBadges);
        });
        this.saveFilter();
        this.diagram.commitTransaction();
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
        this.filterModel.SelectedAssetTypes = this.getTreeNodeSelectionKeys(e);
        this.saveFilter();
        this.hideDeselectedAssetTypes();
    }

    private filterNumberOfHopsChange() {
        this.saveFilter();
        this.diagram.scale = 1;
        this.refreshDiagram();
        this.updateZoomText();
    }

    private filterPredicateChange(e) {
        this.filterModel.SelectedPredicates = this.getTreeNodeSelectionKeys(e);
        this.saveFilter();
        this.hideDeselectedPredicates();
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

    private showDetails(assetUid: string) {
        this.isWindowLoading = true;
        this.browserService.getAssetBrowserDiagramAsset(assetUid).subscribe(response => {
            this.selectedDiagramAsset = response;
            this.selectedDiagramAsset.Loaded = true;
            this.selectedDiagramAsset.Url = "/" + this.selectedDiagramAsset.Url;
            this.isWindowLoading = false;
            this.showWindowTabs = true;
            this.cdRef.markForCheck();
        });
    }

    private hide(e, obj, direction: AssetBrowserDirection = null) {
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
                    if (direction == AssetBrowserDirection.Forward)
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

    private clickBadge(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let ix = obj.itemIndex;
            let node: AssetBrowserTranslationNode = obj.part.data;
            let relation: AssetBrowserTranslationRelationCount = node.relations[ix];
            let requestModel: AssetBrowserImpactApiRequestModel = new AssetBrowserImpactApiRequestModel();


            requestModel.StartHop = node.hop;
            requestModel.Assets = [];
            requestModel.PredicateUid = relation.predicateUid;

            let n = node;
            if (n.isGroup) {
                (this.diagram.findNodeForData(n) as go.Group).findSubGraphParts().each(g => {
                    if (g.data.isGroup == undefined || g.data.isGroup == false) {
                        let asset = new AssetBrowserImpactApiAssetRequestModel();
                        asset.Uid = g.data.assetUid;
                        asset.Key = g.data.key
                        requestModel.Assets.push(asset);

                        //add immediate parent
                        let p = this.diagram.model.nodeDataArray.find(n => n.key == g.data.group);
                        let a = new AssetBrowserImpactApiAssetRequestModel();
                        if (p != null) {
                            a.Uid = p.assetUid;
                            a.Key = p.key
                            requestModel.Assets.push(a);
                        }
                    }
                })
            }

            this.diagram.model.removeArrayItem(node.relations, ix);

            this.browserService.getAssetImpacts(requestModel)
                .subscribe(response => {

                    response.assets.forEach(a => {
                        this.responseModel.assets.push(a);
                    });
                    response.intersects.forEach(i => {
                        this.responseModel.intersects.push(i);
                    });

                    let nodeToPull = this.findInApiModel(node.key, this.responseModel);
                    if (nodeToPull) {
                        response.assets.push(nodeToPull);
                    }

                    response = this.browserService.convertResponseModel(response, this.filterModel.AncestryMode);
                    let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(response);

                    this.parseData(translationModel, true);

                    this.hideDeselectedAssetTypes();
                    this.hideDeselectedPredicates(); 
                });
        }
    }

    private findInApiModel(key: string, model: AssetBrowserLineageApiResponseModel): AssetBrowserLineageApiItemModel {
        let found: AssetBrowserLineageApiItemModel;

        model.assets.forEach(root => {
            if (!found) {
                if (this.findInApiItemModel(key, root)) {
                    found = root;
                }
            }
        });

        return found;
    }

    private findInApiItemModel(key: string, model: AssetBrowserLineageApiItemModel): boolean {
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

    private createRelationsBadge(): go.Panel {
        return this.g(go.Panel, "TableRow", {
            alignment: go.Spot.TopCenter,
            alignmentFocus: go.Spot.Bottom,
            padding: 0,
            cursor: "pointer",
            click: (e, obj) => this.clickBadge(e, obj),
        },
            this.g(go.Panel, "Horizontal",
                new go.Binding("visible", "showBadge"),
                { alignment: go.Spot.Center },
                this.g(go.Panel, "Auto",
                    this.g(go.Shape,
                        { figure: "RoundedRectLeft", parameter1: 2, fill: this.fontRelationBadgeColor, stroke: this.fontRelationBadgeColor, strokeWidth: 1 },
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 2,
                            alignment: go.Spot.Left,
                            editable: false,
                            font: this.fontRelationBadge,
                            stroke: this.fontRelationBadgeCountColor
                        },
                        new go.Binding("text", "predicate")
                    ),
                ),
                this.g(go.Panel, "Auto",
                    this.g(go.Shape, "RoundedRectRight",
                        { parameter1: 2, fill: this.fontRelationBadgeColor, stroke: this.fontRelationBadgeColor, strokeWidth: 1 }
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 2,
                            alignment: go.Spot.Center,
                            editable: false,
                            font: this.fontRelationBadge,
                            stroke: this.fontRelationBadgeCountColor
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
                { click: (e, obj) => this.navigateTo(e, obj) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Show Details", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenuShowDetails }),
                {
                    click: (e, obj) => {
                        if (obj.part.data.assetUid != null && obj.part.data.assetUid != '00000000-0000-0000-0000-000000000000') {
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
                { click: (e, obj) => this.hide(e, obj, AssetBrowserDirection.Backward) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Downstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
                { click: (e, obj) => this.hide(e, obj, AssetBrowserDirection.Forward) } 
            )//,
            //this.g(
            //    "ContextMenuButton",
            //    this.g(go.TextBlock, { text: "Isolate", background: "transparent", alignment: go.Spot.Left, margin: 8, font: this.fontContextMenu }),
            //    { click: function (e, obj) { alert("Not yet implemented") } }
            //)
        );
    }

    private navigateTo(e: go.InputEvent, obj: go.GraphObject) {
        
        console.log("Naviga to");
        console.log(obj.name);
        console.log(obj);
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
        let dg = this.g(go.Diagram, 'LineageDiagram', {
            initialContentAlignment: go.Spot.Center,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            scrollMode: go.Diagram.DocumentScroll,
            initialPosition: new go.Point(125, 125),
            layout: this.g(go.LayeredDigraphLayout, { layerSpacing: 150, columnSpacing: 50, setsPortSpots: false }), //direction: 270, 
            //layout: this.g(AssetBrowserLayout, {}),//layout: this.g(go.LayeredDigraphLayout, {direction: 0, columnSpacing: 50, layerSpacing: 50}),
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

        //TODO: Get this to work so when you click onywhere else on diagram, the selected node highlights go away.
        //dg.addDiagramListener("BackgroundSingleClicked", function (e) {
        //    //Set all to not highlighted.
        //    this.diagram.nodes.each(n => {
        //        n.isHighlighted = false;
        //    });
        //});

        return dg;
    }

    private createPortGroupNode(): go.Group {
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
                    { width: 0, height: 0, portId: "T", toSpot: go.Spot.TopCenter, toLinkable: true, stroke: 'transparent' }//,
                    //new go.Binding("stroke", "back")
                ),
                this.g(go.Panel, "Auto",
                    this.g(
                        go.Shape,
                        "Rectangle",
                        { fill: null, strokeWidth: 2, isPanelMain: true },
                        new go.Binding("stroke", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount))
                    ),
                    this.g(go.Panel, "Vertical",
                        this.g(
                            go.Panel,
                            "Horizontal",
                            // button next to TextBlock
                            { stretch: go.GraphObject.Horizontal, alignment: go.Spot.Top },
                            new go.Binding("background", "", (v) => go.Brush.mix(v.back, this.lightenBoxColor, v.backAmount)),
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
                                    //stroke: this.fontLabelColor
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
                                    //stroke: this.fontLabelColor,
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

    private createGroupNode(): go.Group {

        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.createContextMenu(),
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
                            font: this.fontLabelIcon//,
                            //stroke: this.fontLabelColor
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
                            //stroke: this.fontLabelColor,
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

    private createMoreDataNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                click: (e, obj) => this.getMoreData(e, obj),
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

    private createDefaultLink(): go.Link {
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