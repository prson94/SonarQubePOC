import * as go from 'gojs';
import * as _ from 'lodash';
import {AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild} from '@angular/core';
import {DiagramObjectType, AssetBrowserLineageApiRequestModel, AssetBrowserTranslation, AssetBrowserDirection, AssetBrowserDiagramAsset, AssetBrowserTranslationNode, AssetBrowserTranslationLink, AssetBrowserLineageApiResponseModel, AssetBrowserLineageApiRelationshipModel } from '../../../../models/lineage.model';
import {PermissionsService} from '../../../../services/permissions.service';
import {BrowserService} from '../../../../services/browser.service';
import {DiagramBaseComponent} from '../diagram-base.component';
import { AssetBrowserLayout } from './assetbrowserlayout.component';
import { MenuItem } from 'primeng/api';

declare var window: any;

@Component({
    selector: 'd3s-assetbrowser',
    templateUrl: './browser.component.html',
    providers: [PermissionsService, BrowserService]
})
export class AssetBrowserComponent extends DiagramBaseComponent implements OnInit, AfterViewInit {
    @Input() readonly: boolean = true;
    @Input() assetUid: string;

    @ViewChild('diagram') diagramRef;
    @ViewChild('bottomCommandBar') bottomCommandBarRef;

    DiagramObjectType = DiagramObjectType;

    private requestModel: AssetBrowserLineageApiRequestModel;
    private responseModel: AssetBrowserLineageApiResponseModel;
    private revealedKeys: string[] = [];
    private originalAssetUid: string;
    private menuItems: MenuItem[] = [];
    isWindowVisible: boolean = false;
    isWindowLoading = false;
    showWindowTabs: boolean = false;
    tab: string = "info";
    selectedDiagramAsset: AssetBrowserDiagramAsset;

    //#region control properties

    constructor(
        private myElement: ElementRef,
        protected permissionsService: PermissionsService,
        private browserService: BrowserService
    ) {
        super();
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
    
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
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
            callback: (image_data) => this.savePngButtonClickCallback(image_data,this.assetUid)
        });
    }
    private alertButtonClick(e) {
        alert('Alerts coming soon');
    }
    private infoButtonClick(e) {
        this.isWindowVisible = !this.isWindowVisible;
    }
    private refreshButtonClick(e) {
        this.refreshDiagram();
    }
    private zoomInButtonClick(e) {
        this.diagram.scale += .1;

        if (this.diagram.scale > 2.5) {
            this.diagram.scale = 2.5;
        }
    }
    private zoomOutButtonClick(e) {
        this.diagram.scale -= .1;

        if (this.diagram.scale < .1) {
            this.diagram.scale = .1;
        }
    }
    private fullScreenButtonClick(e) {
        alert('Full screen coming soon');
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

    private infoButtonSelectedClass() {
        return this.isWindowVisible ? "selected" : "";
    }
    private ownerRowClass(icon: string) {
        return "fa " + icon;
    }
    private scoreClass(value: number) {
        let css: string = "asset-browser-window-tabs-content-score-";
        if (+value < 60) {
            css += "low";
        }
        else if (+value > 60 && +value < 75) {
            css += "medium";
        }
        else { 
            css += "high";
        } 
        return css;
    } 

    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch {
            return "Error";
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

    private shadeColor(col, amt) {

        var usePound = false;
        if (col[0] == "#") {
            col = col.slice(1);
            usePound = true;
        }

        var num = parseInt(col, 16);

        var r = (num >> 16) + amt;

        if (r > 255) r = 255;
        else if (r < 0) r = 0;

        var b = ((num >> 8) & 0x00FF) + amt;

        if (b > 255) b = 255;
        else if (b < 0) b = 0;

        var g = (num & 0x0000FF) + amt;

        if (g > 255) g = 255;
        else if (g < 0) g = 0;

        return (usePound ? "#" : "") + (g | (b << 8) | (r << 16)).toString(16);
    }

    private initializeDiagram() {

        this.initializeCustomShapes();


        this.diagram = this.createDiagram();

        this.diagram.groupTemplateMap.add("PortGroup", this.createPortGroupNode());
        this.diagram.groupTemplateMap.add("Group", this.createGroupNode());

        this.diagram.nodeTemplateMap.add("MoreData", this.createMoreDataNode());
        this.diagram.nodeTemplateMap.add("HiddenData", this.createHiddenDataNode());

        this.diagram.nodeTemplate = this.createListItemNode();

        this.diagram.linkTemplateMap.add("", this.createDefaultLink());

        this.diagram.addDiagramListener('ViewportBoundsChanged', () => this.ViewportBoundsChanged());
        this.diagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        //this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        

        this.populateDiagram();
    }

    private populateDiagram() {
        this.isLoading = true;

        this.responseModel = null;
        this.revealedKeys = [];

        this.requestModel = new AssetBrowserLineageApiRequestModel();
        this.requestModel.AssetUids = new Array();
        this.requestModel.AssetUids.push(this.assetUid);
        this.requestModel.IsReveal = false;
        this.requestModel.StartHop = 0;
        this.requestModel.Direction = AssetBrowserDirection.Both;
        this.requestModel.Hops = 3;

        //#region Testing with static data
        //let translationModel: AssetBrowserTranslation = this.browserService.getStaticDataForTesting();
        //this.parseData(translationModel);
        //this.isLoading = false;
        //#endregion

        this.browserService.getAssetLineage(this.requestModel)
            .subscribe(data => {
                this.responseModel = data;
                let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(data);
                this.parseData(translationModel);
            });

        this.isLoading = false;
    }

    private parseData(data: AssetBrowserTranslation, append: boolean = false) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;

        //#region add data to diagram model
        if (append === true) {
            data.nodes.forEach(n => {
                let x = dm.findNodeDataForKey(n.key);
                if (x == null) {
                    dm.addNodeData(n);
                }
            });

            data.links.forEach(l => {
                if (dm.linkDataArray.find(i => i.to == l.to && i.from == l.from) == null)
                    dm.addLinkData(l);
            });

        } else {
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
                        r.key = `${data.key}_${r.predicateUid}`;
                        if (g.data.relations.find(c => c.key == r.key) == null) {
                            childRelations.push(r);
                        }
                        data.relations.splice(i, 1);
                    }
                }
            });

            g.data.relations = g.data.relations.concat(childRelations);
            this.diagram.model.setDataProperty(g.data, "relations", g.data.relations.slice());


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
                            if (this.responseModel.assets.find(r => r.assetUid == a.assetUid) == null) {
                                this.responseModel.assets.push(a);
                            }
                        });

                        response.intersects.forEach(i => {
                            if (this.responseModel.intersects.find(r => r.intersectUid == i.intersectUid) == null) {
                                this.responseModel.intersects.push(i);
                            }
                        });

                        let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(this.responseModel);

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

        this.populateDiagram();
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

    //#region events

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.resizeDiagram();
    }

    private resizeDiagram() {
        //set the diagram div to a specific height
        //required for GoJS

        let offset = this.diagramRef.nativeElement.offsetTop;
        let height = window.innerHeight;

        if (this.diagramRef.nativeElement.offsetParent) {
            offset += this.diagramRef.nativeElement.offsetParent.offsetTop;
        }

        this.diagramRef.nativeElement.style.height = (height - offset - 275) + 'px';

        //alert(this.bottomCommandBarRef.nativeElement.offsetParent.offsetTop);
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

    private ViewportBoundsChanged() {
    }

    private ObjectDoubleClicked(e: any) {
        var obj = e.diagram.selection.first().data;

        if (obj != null) {
            if (obj.diagramObjectType == DiagramObjectType.Node) {
                //this.assetUid = obj.;

                this.populateDiagram();
            }
        }
    }

    //#endregion

    //#region context menu actions

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
                    this.diagram.startTransaction('hide');
                    let hideNode = new AssetBrowserTranslationNode();

                    hideNode.subgraph = new AssetBrowserTranslation();
                    hideNode.template = "HiddenData";
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
                        this.diagramModelAsGraph().addLinkData({ from: l.from, to: hideNode.key });
                    });

                    downstreamLinks.forEach(l => {
                        hideNode.subgraph.links.push(l);
                        this.diagramModelAsGraph().removeLinkData(l);
                        this.diagramModelAsGraph().addLinkData({ from: hideNode.key, to: l.to });
                    });

                    this.diagram.remove(group);
                } else { //hide upstream or downstream
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

                }

                this.diagram.commitTransaction('hide');
            }
        }
    }

    private unhide(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: AssetBrowserTranslationNode = obj.part.data;
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
                    }
                })
            }

            this.diagram.model.removeArrayItem(node.relations, ix);

            this.browserService.getAssetImpacts(requestModel)
                .subscribe(response => {
                    let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(response);
                    //testing console.log(requestModel, response, translationModel);
                    this.parseData(translationModel, true);
                });
        }
    }

    //#endregion

    //#region templates

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
            this.g(go.Panel, "Horizontal", {alignment: go.Spot.Center},
                this.g(go.Panel, "Auto",
                    this.g(go.Shape, 
                        { figure: "RoundedRectLeft", parameter1: 2, fill: "white", stroke: "#404040", strokeWidth: 1 },
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 2,
                            alignment: go.Spot.Left,
                            editable: false,
                            font: "8pt helvetica, arial, sans-serif",
                            stroke: "#404040"
                        },
                        new go.Binding("text", "predicate")
                    ),
                ),
                this.g(go.Panel, "Auto",
                    this.g(go.Shape, "RoundedRectRight",
                        { parameter1: 2, stroke: "#404040", strokeWidth: 1, fill: "#404040" }
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            row: 0,
                            margin: 2,
                            alignment: go.Spot.Center,
                            editable: false,
                            font: "8pt helvetica, arial, sans-serif",
                            stroke: "white"
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
                this.g(go.TextBlock, { text: "Show Details", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "bold 12px sans-serif" }),
                {
                    click: (e, obj) => {
                        this.isWindowLoading = true;
                        this.browserService.getAssetBrowserDiagramAsset(obj.part.data.assetUid).subscribe(response => {
                            this.selectedDiagramAsset = response;
                            this.selectedDiagramAsset.Url = "/" + this.selectedDiagramAsset.Url;
                            this.isWindowVisible = true;
                            this.isWindowLoading = false;
                            this.showWindowTabs = true;
                        });
                    }
                }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                { click: (e, obj) => this.hide(e, obj) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Upstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                { click: (e, obj) => this.hide(e, obj, AssetBrowserDirection.Backward) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Downstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                { click: (e, obj) => this.hide(e, obj, AssetBrowserDirection.Forward) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Isolate", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                { click: function (e, obj) { alert("Not yet implemented") } }
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
            layout: this.g(go.LayeredDigraphLayout, { layerSpacing: 50, setsPortSpots: false }), //direction: 270, 
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
                    {
                        itemTemplate: this.createRelationsBadge()
                    }
                ),
                this.g(
                    go.Shape,  // the "top" port
                    { width: 0, height: 0, portId: "T", toSpot: go.Spot.TopCenter, toLinkable: true },
                    new go.Binding("stroke", "back")
                ),
                this.g(go.Panel, "Auto",
                    this.g(
                        go.Shape,
                        "Rectangle",
                        { fill: null, strokeWidth: 2, isPanelMain: true },
                        new go.Binding("stroke", "back")
                    ),
                    this.g(go.Panel, "Vertical",
                        this.g(
                            go.Panel,
                            "Horizontal",
                            // button next to TextBlock
                            { stretch: go.GraphObject.Horizontal, alignment: go.Spot.Top },
                            new go.Binding("background", "back"),
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
                                    font: "12px FontAwesome",
                                    stroke: "#404040"
                                },
                                new go.Binding("text", "icon")
                            ),
                            this.g(
                                go.TextBlock,
                                {
                                    alignment: go.Spot.Left,
                                    editable: true,
                                    margin: 5,
                                    font: "bold 12px sans-serif",
                                    opacity: 0.75,
                                    stroke: "#404040"
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
                new go.Binding("stroke", "back"),
            ),
            this.g(
                go.Panel,
                "Vertical",  // title above Placeholder
                this.g(
                    go.Panel,
                    "Horizontal",
                    // button next to TextBlock
                    { stretch: go.GraphObject.Horizontal },
                    new go.Binding("background", "back"),
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
                            font: "12px FontAwesome",
                            stroke: "#404040"
                        },
                        new go.Binding("text", "icon")
                    ),
                    this.g(
                        go.TextBlock,
                        {
                            alignment: go.Spot.Left,
                            editable: true,
                            margin: 5,
                            font: "bold 12px sans-serif",
                            opacity: 0.75,
                            stroke: "#404040"
                        },
                        new go.Binding("text", "text").makeTwoWay()
                    )
                ),  // end Horizontal Panel
                this.g(go.Placeholder, { padding: 2, alignment: go.Spot.TopLeft })
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
                new go.Binding("background", "isHighlighted",
                    function (h) { return h ? "#F5C2FF" : "#FFFFFF"; }
                ).ofObject(),
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
                        font: "12px FontAwesome",
                        stroke: "#404040"
                    },
                    new go.Binding("text", "icon")
                ),
                this.g(
                    go.Shape,
                    { width: 10, height: 0, stroke: "transparent" }
                ),
                this.g(
                    go.TextBlock,
                    {
                        editable: true,
                        font: "bold 12px sans-serif",
                        opacity: 0.75,
                        stroke: "#404040"
                    },
                    new go.Binding("text", "text").makeTwoWay()
                )
            )  // end Horizontal Panel
        );
    }

    private createMoreDataNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                click: (e, obj) => this.getMoreData(e, obj)
            },
            this.g(
                go.Panel,
                "Horizontal",
                { stretch: go.GraphObject.Horizontal, padding: 10, type: go.Panel.Spot },
                this.g(
                    "Shape",
                    { alignment: go.Spot.Center, width: 25, height: 25 },
                    new go.Binding("fill", "back"),
                    new go.Binding("stroke", "back", function (v) { return this.shadeColor(v, -15); }),
                ),
                this.g(
                    go.TextBlock,
                    {
                        row: 0,
                        alignment: go.Spot.Center,
                        editable: false,
                        font: "12px FontAwesome",
                        stroke: "#404040",
                        text: "\uf067"
                    }
                )
            )  // end Horizontal Panel
        );
    }

    private createHiddenDataNode(): go.Node {
        return this.g(go.Node, "Auto",
            {
                click: (e, obj) => this.unhide(e, obj)
            },
            this.g(
                go.Panel,
                "Horizontal",
                { stretch: go.GraphObject.Horizontal, padding: 10, type: go.Panel.Spot },
                this.g(
                    "Shape",
                    { alignment: go.Spot.Center, width: 25, height: 25 },
                    new go.Binding("fill", "back"),
                    new go.Binding("stroke", "back", function (v) { return this.shadeColor(v, -15); }),
                ),
                this.g(
                    go.TextBlock,
                    {
                        row: 0,
                        alignment: go.Spot.Center,
                        editable: false,
                        font: "12px FontAwesome",
                        stroke: "#404040",
                        text: "\uf067"
                    }
                )
            )  // end Horizontal Panel
        );
    }


    private createDefaultLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.Orthogonal,
                corner: 5,
                relinkableFrom: false,
                relinkableTo: false,
                click: (e, obj) => this.highlightPath(e, obj as any)
            }, // the whole link panel
            this.g(go.Shape, {
                    stroke: "gray", strokeWidth: 2
                },
                new go.Binding("strokeWidth", "hasProperties", function (h) {
                    return h ? 3 : 2;
                }),
                new go.Binding("stroke", "hasProperties", function (h) {
                    return h ? "black" : "gray"
                })), // the link shape
            this.g(go.Shape, {toArrow: "standard", fill: "gray", stroke: "gray"}), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(
                    go.Shape,
                    {
                        visible: false,
                        fill: this.g(go.Brush, "Radial", {
                            0: "rgb(255, 255, 255)",
                            0.3: "rgb(255, 255, 255)",
                            1: "rgba(255, 255, 255, 0)"
                        }),
                        stroke: '#999',
                        strokeDashArray: [3, 2]
                    },
                    new go.Binding("background", "back"),
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) {
                        return !!a
                    })
                ), // the link shape
                this.g(go.TextBlock, {
                        textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                    },
                    // the label
                    new go.Binding("text", "text").makeTwoWay()
                )
            )
        );
    }

    //#endregion
}
