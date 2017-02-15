import { Component, Input, OnInit, AfterViewInit, ElementRef, OnDestroy, ViewChild, Renderer, HostListener } from '@angular/core';
import { PermissionsService } from '../../../services/permissions.service';
import { DiagramService } from '../../../services/diagram.service';
import { BaseComponent } from '../base.component';
import {
    DiagramObjectType,
    LinkModel,
    NodeModel,
    MapItem,
    Responsibility,
    TechnicalRelation,
    LineageView,
} from '../../../models/lineage.model';

import { MenuItem } from 'primeng/primeng';

import * as go from 'gojs';
import * as _ from 'lodash';

declare var window: any;

@Component({
    selector: 'd3s-lineage',
    templateUrl: './lineage.component.html',
    providers: [ PermissionsService, DiagramService ]
})

export class LineageComponent extends BaseComponent implements OnInit, AfterViewInit  {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() readonly: boolean = true;
    @ViewChild('diagram') diagramRef;

    DiagramObjectType = DiagramObjectType;

    private originalObject: string;
    private originalObjectID: number;
    private view: LineageView = LineageView.SystemFlow;
    private fullscreen: boolean = false;
    private selectedData = null;

    private initialLinks: go.Link[] = [];
    private initialNodes: go.Node[] = [];
    private newLink: go.Link = null;
    private overlayEditLinkKey = null;
    private selection = null;

    private source: string;
    private sourceId: number;
    private target: string;
    private targetId: string;

    private diagramMode: DiagramMode = DiagramMode.Diagram;
    DiagramMode = DiagramMode;

    //control properties
    private isWindowVisible = true;
    private showNodeTabs = false;
    private showLinkTabs = false;
    private menuItems: MenuItem[] = [];
    private tab: string = 'info';
    private headerText = 'Info';
    private zoomLevel: number = 50;

    //diagram properties
    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;

    constructor(private myElement: ElementRef, protected permissionsService: PermissionsService, private diagramService: DiagramService, private renderer: Renderer) {
        super();
    }

    public ngOnInit() {

        this.originalObject = this.objectType;
        this.originalObjectID = this.objectID;

        if (this.objectType == 'FusionAttribute') this.viewID = 3;//start fusion at the technical view.

        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);

        this.initializeDiagram();
        
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        this.myDiagram.div = null;
    }

    //#region helper methods

    private sizePanel() {
        //var windowHeight = $(window).innerHeight();
        //var tileTopOffset = $(w).offset();
        //var height = windowHeight - tileTopOffset.top - 75; //height();
        //$('#LineageDiagram').height(height);
    }

    private unsubscribe() {
        
    }

    private initializeDiagram() {
        this.myDiagram = this.createDiagram();

        this.myDiagram.nodeTemplateMap.add("Focal", this.createFocalNode());
        this.myDiagram.nodeTemplateMap.add("Normal", this.createNormalNode());
        this.myDiagram.nodeTemplateMap.add("SupportFocal", this.createSupportFocalNode());
        this.myDiagram.nodeTemplateMap.add("SupportNormal", this.createSupportNormalNode());
        this.myDiagram.nodeTemplateMap.add("Fusion", this.createFusionNode());

        this.myDiagram.linkTemplateMap.add("", this.createDefaultLink());
        this.myDiagram.linkTemplateMap.add("Support", this.createSupportLink());

        this.myDiagram.addDiagramListener('ViewPortBoundsChanged', () => this.ViewPortBoundsChanged());
        this.myDiagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.myDiagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));

        this.myDiagram.grid.visible = false;
        this.myDiagram.grid.gridCellSize = new go.Size(8, 8);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.populateDiagram();
    }

    private populateDiagram(): Promise<any> {
        this.isLoading = true;
        let windowVisible = this.isWindowVisible;

        this.isWindowVisible = false;
        return this.diagramService.getLineageDiagram(this.objectType, this.objectID, this.view)
            .then(data => {
                //console.log(data);
                this.parseData(data);
            })
            .then(() => {
                this.reOrderLayout();
                this.myDiagram.zoomToFit();
                this.zoomLevel = _.clamp(this.myDiagram.scale * 75, 0, 100);
                this.isLoading = false;
                this.isWindowVisible = windowVisible;
            });
    }

    private parseData(data: any) {
        this.myDiagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.myDiagram.model;
        dm.nodeDataArray = [];
        dm.linkDataArray = [];
        this.initialNodes = [];
        this.initialLinks = [];
        var modelList = [];
        var linkList = [];

        if (data.nodes) {
            for (var i = 0; i < data.nodes.length; i++) {

                var d = data.nodes[i];
                var model = new NodeModel();

                var isFocalPoint = (d.obj == this.objectType && d.objid == this.objectID);

                model.template = d.template;
                model.key = d.key;
                model.obj = d.obj;
                model.objid = d.objid;
                model.type = d.obj;
                model.name = this.htmlDecode(d.name);
                model.typeName = d.typeName;
                model.fore = d.fore;
                model.back = d.back;
                model.diagramObjectType = DiagramObjectType.Node;
                model.intersectId = d.intersectId;

                model.sourceRuleCount = d.sourceRuleCount;
                model.mappingRuleCount = d.mappingRuleCount;
                model.hasSourceRules = d.HasSourceRules;
                model.hasMappingRules = (d.mappingRuleCount > 0);
                model.actionCount = d.actions;
                model.hasActions = (d.actions > 0);
                model.hasTransformations = (d.transformationCount > 0);

                model.mapItems = d.mapItems;

                if (d.other)
                    model.other = this.htmlDecode(d.other);

                modelList.push(model);
            }
        }

        if (data.links) {
            for (var i = 0; i < data.links.length; i++) {
                var d = data.links[i];
                var link = new LinkModel();
                link.Category = d.category;
                link.from = d.from;
                link.to = d.to;
                link.diagramObjectType = DiagramObjectType.Link;
                link.sourceMappingCount = d.mappingRuleCount;
                link.hasMappingRules = (d.mappingRuleCount > 0);
                link.hasTransformations = (d.transformation);
                link.hasProperties = (link.hasTransformations || link.hasMappingRules);
                link.mapItems = d.mapItems;
                linkList.push(link);
            }
        }

        for (var i = 0; i < modelList.length; i++) {
            this.myDiagram.model.addNodeData(modelList[i]);
        }

        dm.linkCategoryProperty = "Category";

        for (var i = 0; i < linkList.length; i++) {
            dm.addLinkData(linkList[i]);
            dm.setCategoryForLinkData(linkList[i], linkList[i].Category);
        }

        //get deep copy of lists
        this.initialLinks = _.cloneDeep(linkList);
        this.initialNodes = _.cloneDeep(modelList);

        this.refreshControls(null);  //set buttons/expanders to defaults

        this.myDiagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private htmlDecode(val: string): string {
        val = val.replace(/&#39;/g, '\'');
        val = val.replace(/&amp;/g, '&')
        val = val.replace(/&lt;/g, '<')
        val = val.replace(/&gt;/g, '>')
        val = val.replace(/&#34;/g, '"');

        return val;
    }

    private refreshControls(data: any) {
        this.setSourceValues(data);
        this.toggleTabs(data);
        this.toggleMenuItems(data);
    }

    private toggleTabs(data: NodeModel | LinkModel) {
        if (data) {
            this.showNodeTabs = data.diagramObjectType == DiagramObjectType.Node;
            this.showLinkTabs = data.diagramObjectType == DiagramObjectType.Link;

            if (this.showLinkTabs) this.selectTab('exchange');
            else if (this.showNodeTabs) this.selectTab('info');
        } else {
            this.showNodeTabs = false;
            this.showLinkTabs = false;
            this.tab = '';
        }
    }

    private toggleMenuItems(data: NodeModel | LinkModel) {
        this.menuItems = [];

        let gears: MenuItem = {
            icon: 'fa-gears',
            items: []
        }

        gears.items.push({
            label: 'Source Rules'
        });
        gears.items.push({
            label: 'Edit Lineage'
        });

        let eye: MenuItem = {
            icon: 'fa-eye',
            items: []
        }

        eye.items.push({
            label: 'Business System Flow'
        });
        eye.items.push({
            label: 'Business Data Flow'
        });
        eye.items.push({
            label: 'Technical Lineage'
        });

        this.menuItems.push(gears);
        this.menuItems.push(eye); 

        this.menuItems.push({
            icon: 'fa-search-minus'
        });

        this.menuItems.push({
            icon: 'fa-search-plus'
        });

        this.menuItems.push({
            icon: 'fa-refresh'
        });

        this.menuItems.push({
            icon: 'fa-info-circle'
        });


    }

    private setSourceValues(data: any) {
        if (!data || data == null) {
            this.source = null;
            this.sourceId = null;
            this.target = null;
            this.targetId = null;
        } else {
            if (data.diagramObjectType == DiagramObjectType.Node) {
                this.source = this.objectType;
                this.sourceId = this.objectID;

                if (data.obj && data.objid) {                   
                    this.target = data.obj;
                    this.targetId = data.objid;
                }

            } else if (data.diagramObjectType == DiagramObjectType.Link) {

                var from = this.myDiagram.model.findNodeDataForKey(data.from);
                var to = this.myDiagram.model.findNodeDataForKey(data.to);

                if (from.obj && from.objid) {
                    this.source = from.obj;
                    this.sourceId = from.objid;
                }
                if (to.obj && to.objid) {
                    this.target = to.obj;
                    this.targetId = to.objid;
                }
            }
        }
    }

    private reOrderLayout() {
        this.myDiagram.layout.invalidateLayout();
        this.myDiagram.requestUpdate();
    }
    
    private selectTab(val: string) {
        switch (val) {
            case 'info': this.headerText = 'Info'; break;
            case 'code': this.headerText = 'Source Rules'; break;
            case 'user': this.headerText = 'Responsibilities'; break;
            case 'database': this.headerText = 'Fusion Relationships'; break;
            case 'exchange': this.headerText = 'Mapping Rules'; break;
            default: this.headerText = ''; break;
        }
        this.tab = val;
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

        this.diagramRef.nativeElement.style.height = (height - offset - 50) + 'px';
    }

    private onMouseEnterNode(e: any, node: go.Node) {
        node.isShadowed = true;
    }

    private onMouseLeaveNode(e: any, node: go.Node) {
        node.isShadowed = false;
    }

    private zoomDiagram(v: number) {
        this.myDiagram.scale = v;
        //console.log('zoomDiagram', v, this.myDiagram);
    }

    private ViewPortBoundsChanged() {
        //var s = this.myDiagram.scale;
        //var h = 500;
        //if (s > 1) {
        //    h = h * s;
        //}
        //this.zoomLevel = this.myDiagram.scale;
    }

    private ChangedSelection(e: any) {
        this.selection = e.diagram.selection;

        if (this.selection.count == 0) {
            this.selectedData = null;
        } else {
            //get a deep copy of the selection as an array
            var sel = _.cloneDeep(this.selection.toArray());

            if (sel != null && sel.length != 0) {
                this.selectedData = sel[0].data;
            }
        }

        this.refreshControls(this.selectedData);
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

    private menuClick(e: MenuItem) {
        if (e.icon == 'fa-refresh') {
            this.objectType = this.originalObject;
            this.objectID = this.originalObjectID;
            this.populateDiagram();
        } else if (e.icon == 'fa-search-plus') {
            this.myDiagram.scale += .1;
            if (this.myDiagram.scale > 2.5)
                this.myDiagram.scale = 2.5;
        } else if (e.icon == 'fa-search-minus') {
            this.myDiagram.scale -= .1;
            if (this.myDiagram.scale < .1)
                this.myDiagram.scale = .1;
        } else if (e.icon == 'fa-info-circle') {
            this.isWindowVisible = !this.isWindowVisible;
        } else if (e.label == 'Business System Flow') {
            this.view = LineageView.SystemFlow;
            this.populateDiagram();
        } else if (e.label == 'Business Data Flow') {
            this.view = LineageView.DataFlow;
            this.populateDiagram();
        } else if (e.label == 'Technical Lineage') {
            this.view = LineageView.Technical;
            this.populateDiagram();
        } else if (e.label == 'Source Rules') {
            this.headerText = 'Manage Source Rules';
            this.diagramMode = DiagramMode.SourceRuleEditor;
        } else if (e.label == 'Edit Lineage') {
            this.headerText = 'Edit Lineage';
            this.diagramMode = DiagramMode.LineageEditor;
        }
    }

    private closeEditor() {
        this.headerText = 'Lineage';
        this.diagramMode = DiagramMode.Diagram;
    }

    //#endregion

    //#region templates

    private createDiagram(): go.Diagram {


        let dg = this.g(go.Diagram, 'LineageDiagram', {
            initialContentAlignment: go.Spot.Left,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            scrollMode: go.Diagram.DocumentScroll,
            initialPosition: new go.Point(125, 125),
            layout: this.g(go.LayeredDigraphLayout, { direction: 0, columnSpacing: 50, layerSpacing: 50 }),
            "undoManager.isEnabled": true
        });

        dg.model.class = go.GraphLinksModel;
        dg.model.nodeCategoryProperty = "template";
        dg.model.linkFromPortIdProperty = "frompid";
        dg.model.linkToPortIdProperty = "topid";
        dg.model.nodeDataArray = [];
        dg.model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = !this.readonly;
        dg.model.isReadOnly = this.readonly;

        return dg;
    }


    private createFocalNode(): go.Node {
        let nodeWidth = 200;
        let nodeHeight = 150;
        let nodeBorderColor = '#000000';
        let nodeFontSize = 14;
        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 2,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape"
                },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel,
                    go.Panel.Horizontal,
                    {
                        alignment: go.Spot.BottomLeft,
                        margin: 5
                    },
                    this.makeIconPanel("\uf128", "Has open actions", "hasActions", nodeFontSize),
                    this.makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
                    this.makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
                    this.makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize)
                    //this.makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
                    //this.makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
                    //this.makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
                        row: 0,
                        margin: 3,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: "bold " + nodeFontSize + "pt sans-serif"
                    },
                        new go.Binding("text", "name").makeTwoWay(),
                        new go.Binding("stroke", "fore").makeTwoWay()
                    ),
                    this.g(go.TextBlock, {
                        row: 1,
                        margin: 3,
                        maxSize: new go.Size(180, NaN),
                        font: (nodeFontSize - 2) + "pt sans-serif"
                    },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Left,
                alignmentFocus: new go.Spot(0, 0.5, -8, 0)
            },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Right,
                alignmentFocus: new go.Spot(1, 0.5, 8, 0)
            },
                [this.makePort("OUT", false)]));
    }

    private createNormalNode(): go.Node {
        let nodeWidth = 200;
        let nodeHeight = 105;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 10;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 2,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape"
                },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel,
                    go.Panel.Horizontal,
                    {
                        alignment: go.Spot.BottomLeft,
                        margin: 5
                    },
                    this.makeIconPanel("\uf128", "Has open actions", "hasActions", nodeFontSize),
                    this.makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
                    this.makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
                    this.makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize)
                    //this.makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
                    //this.makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
                    //this.makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
                        row: 0,
                        margin: 3,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: "bold " + nodeFontSize + "pt sans-serif"
                    },
                        new go.Binding("text", "name").makeTwoWay(),
                        new go.Binding("stroke", "fore").makeTwoWay()
                    ),
                    this.g(go.TextBlock, {
                        row: 1,
                        margin: 3,
                        maxSize: new go.Size(180, NaN),
                        font: (nodeFontSize - 2) + "pt sans-serif"
                    },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Left,
                alignmentFocus: new go.Spot(0, 0.5, -8, 0)
            },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Right,
                alignmentFocus: new go.Spot(1, 0.5, 8, 0)
            },
                [this.makePort("OUT", false)]));
    }

    private createSupportFocalNode(): go.Node {
        let nodeWidth = 140;
        let nodeHeight = 80;
        let nodeBorderColor = '#000000';
        let nodeFontSize = 9;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 2,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape"
                },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel,
                    go.Panel.Horizontal,
                    {
                        alignment: go.Spot.BottomLeft,
                        margin: 5
                    },
                    this.makeIconPanel("\uf128", "Has open actions", "hasActions", nodeFontSize),
                    this.makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
                    this.makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
                    this.makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize)
                    //this.makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
                    //this.makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
                    //this.makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
                        row: 0,
                        margin: 3,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: "bold " + nodeFontSize + "pt sans-serif"
                    },
                        new go.Binding("text", "name").makeTwoWay(),
                        new go.Binding("stroke", "fore").makeTwoWay()
                    ),
                    this.g(go.TextBlock, {
                        row: 1,
                        margin: 3,
                        maxSize: new go.Size(180, NaN),
                        font: (nodeFontSize - 2) + "pt sans-serif"
                    },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Left,
                alignmentFocus: new go.Spot(0, 0.5, -8, 0)
            },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Right,
                alignmentFocus: new go.Spot(1, 0.5, 8, 0)
            },
                [this.makePort("OUT", false)]));

    }

    private createSupportNormalNode(): go.Node {
        let nodeWidth = 130;
        let nodeHeight = 70;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 9;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 2,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape"
                },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel,
                    go.Panel.Horizontal,
                    {
                        alignment: go.Spot.BottomLeft,
                        margin: 5
                    },
                    this.makeIconPanel("\uf128", "Has open actions", "hasActions", nodeFontSize),
                    this.makeIconPanel("\uf126", "Source rule defined", "hasSourceRules", nodeFontSize),
                    this.makeIconPanel("\uf0ec", "Mapping rule defined", "hasMappingRules", nodeFontSize),
                    this.makeIconPanel("\uf074", "Transformation rule defined", "hasTransformations", nodeFontSize)
                    ///this.makeIconPanel("\uf059", "Challenge exists on this item", "hasChallenges", nodeFontSize),
                    ///this.makeIconPanel("\uf188", "Item has open events", "hasOpenEvents", nodeFontSize),
                    ///this.makeIconPanel("\uf071", "Item has open issues", "hasOpenIssues", nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
                        row: 0,
                        margin: 3,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: "bold " + nodeFontSize + "pt sans-serif"
                    },
                        new go.Binding("text", "name").makeTwoWay(),
                        new go.Binding("stroke", "fore").makeTwoWay()
                    ),
                    this.g(go.TextBlock, {
                        row: 1,
                        margin: 3,
                        maxSize: new go.Size(180, NaN),
                        font: (nodeFontSize - 2) + "pt sans-serif"
                    },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Left,
                alignmentFocus: new go.Spot(0, 0.5, -8, 0)
            },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Right,
                alignmentFocus: new go.Spot(1, 0.5, 8, 0)
            },
                [this.makePort("OUT", false)]));
    }

    private createFusionNode(): go.Node {
        let nodeWidth = 225;
        let nodeHeight = 80;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 9;

        return this.g(go.Node, "Spot",
            {
                mouseEnter: this.onMouseEnterNode,
                mouseLeave: this.onMouseLeaveNode
            },
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 2,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape"
                },
                    new go.Binding("fill", "back").makeTwoWay()
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
                        row: 0,
                        margin: 3,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: "bold " + nodeFontSize + "pt sans-serif"
                    },
                        new go.Binding("text", "name").makeTwoWay(),
                        new go.Binding("stroke", "fore").makeTwoWay()
                    ),
                    this.g(go.TextBlock, {
                        row: 1,
                        margin: 3,
                        maxSize: new go.Size(180, NaN),
                        font: (nodeFontSize - 2) + "pt sans-serif"
                    },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "typeName").makeTwoWay()
                    ),
                    this.g(go.TextBlock, {
                        row: 2,
                        margin: 3,
                        maxSize: new go.Size(180, NaN),
                        font: 'bold ' + (nodeFontSize - 2) + "pt sans-serif"
                    },
                        new go.Binding("stroke", "fore").makeTwoWay(),
                        new go.Binding("text", "other").makeTwoWay()
                    )
                )),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Left,
                alignmentFocus: new go.Spot(0, 0.5, -8, 0)
            },
                [this.makePort("IN", false)]),
            this.g(go.Panel, "Vertical", {
                alignment: go.Spot.Right,
                alignmentFocus: new go.Spot(1, 0.5, 8, 0)
            },
                [this.makePort("OUT", false)]));
    }


    private createDefaultLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.AvoidsNodes,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false
            }, // the whole link panel
            new go.Binding("curve", "curve", go.Binding.parseEnum(go.Link, go.Link.JumpOver)),
            this.g(go.Shape, {
                stroke: "gray", strokeWidth: 2
            },
                new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
                new go.Binding("stroke", "hasProperties", function (h) { return h ? "black" : "gray" })), // the link shape
            this.g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" }), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(go.Shape, {
                    visible: false,
                    fill: this.g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                    stroke: '#999',
                    strokeDashArray: [3, 2]
                },
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) { return (a ? true : false) })
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

    private createSupportLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.AvoidsNodes,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false
            }, // the whole link panel
            this.g(go.Shape, {
                stroke: "blue", strokeWidth: 2
            },
                new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
                new go.Binding("stroke", "hasProperties", function (h) { return h ? "black" : "gray" })), // the link shape
            this.g(go.Panel, "Auto",
                this.g(go.Shape, {
                    visible: false,
                    fill: this.g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                    stroke: '#999',
                    strokeDashArray: [3, 2]
                },
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) { return (a ? true : false) })
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


    private makeIconPanel(icon, tooltip, binding, fontSize) {
        fontSize -= 2;
        let iconPanel = this.g(go.Panel,
            "Auto",
            {
                alignment: go.Spot.Center,
                margin: 2
            },
            this.g(go.Shape, "Circle",
                {
                    stroke: null,
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, { fill: "lightyellow" }), this.g(go.Panel, "Vertical", this.g(go.TextBlock, { margin: 3, text: tooltip })))
                },
                new go.Binding("fill", "fore")),
            this.g(go.TextBlock,
                {
                    row: 0,
                    margin: 0,
                    alignment: go.Spot.Center,
                    editable: false,
                    font: (fontSize) + "pt FontAwesome",
                    text: icon,
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, { fill: "lightyellow" }), this.g(go.Panel, "Vertical", this.g(go.TextBlock, { margin: 3, text: tooltip })))
                },
                new go.Binding("stroke", "back")
            ),
            new go.Binding("visible", binding)
        );

        return iconPanel;
    }

    private makePort(name: string, leftside: boolean) {
        var port = this.g(go.Shape, "Circle", {
            fill: "white",
            stroke: "gray",
            strokeWidth: 3,
            desiredSize: new go.Size(9, 9),
            portId: name, // declare this object to be a "port"
            cursor: "pointer" // show a different cursor to indicate potential link point
        });

        var panel = this.g(go.Panel, "Horizontal", {
            margin: new go.Margin(2, 0)
        });

        if (leftside) {
            port.toSpot = go.Spot.Left;
            port.toLinkable = true;
            panel.alignment = go.Spot.TopLeft;
            panel.add(port);
        } else {
            port.fromSpot = go.Spot.Right;
            port.fromLinkable = true;
            panel.alignment = go.Spot.TopRight;
            panel.add(port);
        }
        return panel;
    }

    //#endregion
}

enum DiagramMode {
    Diagram,
    SourceRuleEditor,
    LineageEditor
}
