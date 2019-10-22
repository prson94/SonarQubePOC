import * as go from 'gojs';
import * as _ from 'lodash';
import {AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild} from '@angular/core';
import {DiagramObjectType, AssetBrowserLineageApiRequestModel, AssetBrowserTranslation, AssetBrowserDirection, AssetBrowserDiagramAsset, AssetBrowserTranslationNode, AssetBrowserTranslationLink } from '../../../../models/lineage.model';
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
    private originalAssetUid: string;
    private menuItems: MenuItem[]=[];

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

        this.requestModel = new AssetBrowserLineageApiRequestModel();
        this.requestModel.AssetUids = new Array();
        this.requestModel.AssetUids.push(this.assetUid);
        this.requestModel.IsReveal = false;
        this.requestModel.StartHop = 0;
        this.requestModel.Direction = AssetBrowserDirection.Both;
        this.requestModel.Hops = 1;

        //#region Testing with static data
        //let translationModel: AssetBrowserTranslation = this.browserService.getStaticDataForTesting();
        //this.parseData(translationModel);
        //this.isLoading = false;
        //#endregion

        this.browserService.getAssetLineage(this.requestModel)
            .subscribe(data => {
                let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(data);
                this.parseData(translationModel);
            });

        this.isLoading = false;
    }

    private parseData(data: AssetBrowserTranslation, append: boolean = false) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;

        if (append === true) {
            data.nodes.forEach(n => {
                if (dm.findNodeDataForKey(n.key) == null)
                    dm.addNodeData(n);
            });

            data.links.forEach(l => {
                if (dm.linkDataArray.find(i => i.to == l.to && i.from == l.from) == null)
                    dm.addLinkData(l);
            });
        } else {
            dm.nodeDataArray = data.nodes;
            dm.linkDataArray = data.links;
        }

        //add reveal nodes
        this.diagram.findTopLevelGroups().each(g => {
            if (g.data.showReveal == true) {
                let revealKey = g.data.key + '_reveal';
                let children = g.findSubGraphParts();
                let childAssets = [];

                g.data.showReveal = false;

                childAssets.push(g.data.assetUid);
                children.each(c => {
                    let data = c.data;
                    childAssets.push(data.assetUid);
                });

                if (dm.findNodeDataForKey(revealKey) == null) {
                    dm.addNodeData({
                        template: 'MoreData',
                        key: revealKey, back: g.data.back,
                        showReveal: true,
                        relations: g.data.relations,
                        assetUid: g.data.assetUid,
                        hop: g.data.hop,
                        assetUids: childAssets
                    });

                    dm.addLinkData({
                        from: g.data.key,
                        to: revealKey
                    });
                }         
            }
        });

        this.diagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private getMoreData(e: go.InputEvent, obj: go.GraphObject) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let data = obj.part.data;

            if (data.showReveal == true) {
                this.diagram.startTransaction('reveal');

                let model = new AssetBrowserLineageApiRequestModel();
                model.AssetUids = data.assetUids;
                model.IsReveal = true;
                model.StartHop = data.hop;
                model.Direction = AssetBrowserDirection.Both;
                model.Hops = 1;

                this.browserService.getAssetLineage(model)
                    .subscribe(response => {
                        let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(response);

                        this.diagramModelAsGraph().removeNodeData(data);
                        let l = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == data.key);
                        this.diagramModelAsGraph().removeLinkDataCollection(l);

                        this.parseData(translationModel, true);

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

    //#endregion

    //#region templates

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
                go.Shape,
                "Rectangle",
                { fill: null, strokeWidth: 2 },
                new go.Binding("stroke", "back")
            ),
            this.g(
                go.Panel,
                "Vertical",  // title above Placeholder
                this.g(
                    go.Shape,  // the "top" port
                    { width: 0, height: 0, portId: "T", toSpot: go.Spot.TopCenter, toLinkable: true },
                    new go.Binding("stroke", "back")
                ),
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
                ),  // end Horizontal Panel

                this.g(
                    go.Shape,  // the "bottom" port
                    { width: 0, height: 0, portId: "B", toSpot: go.Spot.BottomCenter, toLinkable: true, stroke: "transparent" }
                ),
            ),

            // end Vertical Panel
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
                { fill: null, strokeWidth: 2 },
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
                click: this.highlightPath
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
                click: this.highlightPath
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
