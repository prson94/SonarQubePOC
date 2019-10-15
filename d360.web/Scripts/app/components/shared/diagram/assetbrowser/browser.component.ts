import * as go from 'gojs';
import * as _ from 'lodash';
import {AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild} from '@angular/core';
import {DiagramObjectType, AssetBrowserLineageApiRequestModel, AssetBrowserTranslation, AssetBrowserDirection, AssetBrowserTranslationNode } from '../../../../models/lineage.model';
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

    DiagramObjectType = DiagramObjectType;

    private requestModel: AssetBrowserLineageApiRequestModel;
    private originalAssetUid: string;
    private menuItems: MenuItem[]=[];
    
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

    //#region helper methods

    public menuAction(e: MenuItem) {
        if (e.icon == 'fa fa-refresh') {
            this.refreshDiagram();
        } else if (e.icon == 'fa fa-search-plus') {
            this.diagram.scale += .1;

            if (this.diagram.scale > 2.5) {
                this.diagram.scale = 2.5;
            }
        } else if (e.icon == 'fa fa-search-minus') {
            this.diagram.scale -= .1;

            if (this.diagram.scale < .1) {
                this.diagram.scale = .1;
            }
        }
    }

    private getMoreData(e: go.InputEvent, obj: go.Part) {
        if (obj.data) {
            if (obj.data.retrieveDataFor) {
                let diagramModel: go.GraphLinksModel = <go.GraphLinksModel>obj.diagram.model;

                let prefix = obj.data.retrieveDataFor + "_";
                let color: string = "#B9F1AF";
                let transformColor: string = "#FAE7BC";

                obj.diagram.startTransaction("get_data");

                diagramModel.addNodeData({ key: prefix + "pg", isGroup: true, text: "PG1", template: "PortGroup", back: color, loc: "300 400", layer: 0, icon: "\uf1c0", impacts: ["."] });
                diagramModel.addNodeData({ key: prefix + "s", isGroup: true, group: prefix + "pg", text: "fact", template: "Group", back: color, icon: "\uf007", impacts: ["."] });
                diagramModel.addNodeData({ key: prefix + "t", isGroup: true, group: prefix + "s", text: "MEMBERS", template: "Group", back: color, icon: "\uf0ce", impacts: ["."] });
                diagramModel.addNodeData({ key: prefix + "c", group: prefix + "t", text: "SOME_COLUMN", back: color, icon: "\uf0db", impacts: ["c1_1", "c2_1", "jobStep1"] });

                diagramModel.addLinkData({ from: obj.data.retrieveDataFor, fromPort: "R", to: prefix + "pg", toPort: "L", text: "sample link", back: transformColor, impacts: [] });

                var linksToRemove = diagramModel.linkDataArray.filter(l => l.to === obj.key);
                linksToRemove.forEach(i => {
                    diagramModel.removeLinkData(i);
                });
                diagramModel.removeNodeData(obj); 
                obj.visible = false;
                //obj.diagram.remove(obj);

                obj.diagram.commitTransaction("get_data");
            }
        }

        //this.reOrderLayout();
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
        this.requestModel.Direction = AssetBrowserDirection.Both;
        this.requestModel.Hops = 3;
        this.requestModel.StartFromAssets = [];

        //#region Testing with static data
        let translationModel: AssetBrowserTranslation = this.browserService.getStaticDataForTesting();
        this.parseData(translationModel);
        this.isLoading = false;
        //#endregion

        //this.browserService.getAssetLineage(this.assetUid, this.requestModel)
        //    .subscribe(data => {
        //        let translationModel: AssetBrowserTranslation = this.browserService.translateAssetLineageResponseModel(data);
        //        this.parseData(translationModel);
        //    });

        this.isLoading = false;
    }

    private parseData(data: AssetBrowserTranslation) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        dm.nodeDataArray = data.nodes;
        dm.linkDataArray = data.links;
        this.diagram.commitTransaction("load_all_data");

        console.log('parseData', data);;

        this.reOrderLayout();
        //this.diagram.autoScale = go.Diagram.UniformToFill;

    }

    private reOrderLayout() {
        this.diagram.layout.invalidateLayout();
        this.diagram.requestUpdate();
    }

    private refreshDiagram() {
        this.assetUid = this.originalAssetUid;
        this.populateDiagram();
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

        this.diagramRef.nativeElement.style.height = (height - offset - 200) + 'px';
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
                this.objectType = obj.obj;
                this.objectID = obj.objid;

                this.populateDiagram();
            }
        }
    }

    //#endregion

    //#region context menu actions

    private hide(e, obj, direction: AssetBrowserDirection = null) {
        console.log('diagm', this.diagram.model.nodeDataArray, this.diagramModelAsGraph().linkDataArray);
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: AssetBrowserTranslationNode = obj.part.data;

            if (node.group != null) {
                let group: any = this.diagram.findNodeForKey(node.group);
                group.isSubGraphExpanded = false;
            } else if (node.isGroup) { //top level item

                this.diagram.startTransaction('hide');
                let group: any = this.diagram.findNodeForKey(node.key);

                if (direction == null) {

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
                } else {
                    let subgraph = this.findSubGraph(group.key, direction);
                    //console.log('subgraph', subgraph);

                    let hideNode = new AssetBrowserTranslationNode();

                    hideNode.subgraph = subgraph;
                    hideNode.template = "HiddenData";
                    hideNode.back = node.back;

                    this.diagramModelAsGraph().removeLinkDataCollection(subgraph.links);
                    this.diagram.model.removeNodeDataCollection(subgraph.nodes);

                    this.diagram.model.addNodeData(hideNode);
                    this.diagramModelAsGraph().addLinkData({ from: group.key, to: hideNode.key });

                }

                this.diagram.commitTransaction('hide');
            }
        }
    }

    private findSubGraph(startKey: string, direction: AssetBrowserDirection): AssetBrowserTranslation {
        let subgraph = new AssetBrowserTranslation();

        subgraph.nodes = [];
        subgraph.links = [];

        let node = this.diagram.findNodeForKey(startKey);

        if (node != null) {
            let currentNodes = [];
            let nextLinks = [];
            let excludeStart = true;
            let iteration = 1;
            currentNodes.push(node.data);

            if (direction == AssetBrowserDirection.Forward || direction == AssetBrowserDirection.Both) {

                while (currentNodes.length > 0) {
                    nextLinks = [];
                    console.log('iteration: ', iteration, ', currentNodes: ', currentNodes.length);
                    currentNodes.forEach(n => {
                        if (subgraph.nodes.find(s => s.key == n.key)) {
                            //already in the subgraph, skip
                        } else {
                            let l = this.diagramModelAsGraph().linkDataArray.filter(l => l.from == n.key);
                            nextLinks = nextLinks.concat(l);
                            if (!(excludeStart && n.key == startKey)) {
                                subgraph.nodes.push(n);

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
                    console.log('iteration: ', iteration, ', nextLinks: ', nextLinks.length);
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
                    iteration++;
                }

            }
            if (direction == AssetBrowserDirection.Backward || direction == AssetBrowserDirection.Both) {

                while (currentNodes.length > 0) {
                    nextLinks = [];
                    console.log('iteration: ', iteration, ', currentNodes: ', currentNodes.length);
                    currentNodes.forEach(n => {
                        if (subgraph.nodes.find(s => s.key == n.key)) {
                            //already in the subgraph, skip
                        } else {
                            let l = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == n.key);
                            nextLinks = nextLinks.concat(l);
                            if (!(excludeStart && n.key == startKey)) {
                                subgraph.nodes.push(n);

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
                    console.log('iteration: ', iteration, ', nextLinks: ', nextLinks.length);
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
                    iteration++;
                }

            }
        }
        console.log(subgraph);

        return subgraph;
    }



    private reveal(e, obj) {
        if (obj != null && obj.part != null && obj.part.data != null) {
            let node: AssetBrowserTranslationNode = obj.part.data;
            if (node.template == "HiddenData") {
                let upstreamLinks = this.diagramModelAsGraph().linkDataArray.filter(l => l.to == node.key);
                let downstreamLinks = this.diagramModelAsGraph().linkDataArray.filter(l => l.from == node.key);

                this.diagram.model.addNodeDataCollection(node.subgraph.nodes);
                this.diagramModelAsGraph().addLinkDataCollection(node.subgraph.links);

                this.diagramModelAsGraph().removeLinkDataCollection(upstreamLinks);
                this.diagramModelAsGraph().removeLinkDataCollection(downstreamLinks);

                this.diagram.model.removeNodeData(node);
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
                { click: function (e, obj) { alert("Not yet implemented") } }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                { click: (e, obj) => this.hide(e, obj) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Upstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                { click: (e, obj) => this.hide(e, obj, AssetBrowserDirection.Forward) }
            ),
            this.g(
                "ContextMenuButton",
                this.g(go.TextBlock, { text: "Hide Downstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                { click: (e, obj) => this.hide(e, obj, AssetBrowserDirection.Backward) }
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
                    function (h) { return h ? "#F5C2FF" : "transparent"; }
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
                click: this.getMoreData
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
                click: (e, obj) => this.reveal(e, obj)
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
