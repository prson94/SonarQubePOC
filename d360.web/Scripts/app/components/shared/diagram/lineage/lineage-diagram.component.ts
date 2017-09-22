import { Component, Input, OnInit, AfterViewInit, ElementRef, OnDestroy, ViewChild, Renderer, HostListener } from '@angular/core';
import { PermissionsService } from '../../../../services/permissions.service';
import { DiagramService } from '../../../../services/diagram.service';
import { LineageService } from '../../../../services/lineage.service';
import { BaseComponent } from '../../base.component';
import {
    DiagramObjectType,
    LinkModelV2,
    NodeModelV2,
    MapItem,
    Responsibility,
    TechnicalRelation,
    LineageView,
} from '../../../../models/lineage.model';

import { MenuItem } from 'primeng/primeng';

import * as go from 'gojs';
import * as _ from 'lodash';

declare var window: any;

@Component({
    selector: 'd3s-lineage-diagram',
    templateUrl: './lineage-diagram.component.html',
    providers: [PermissionsService, DiagramService, LineageService]
})

export class LineageDiagramComponent extends BaseComponent implements OnInit, AfterViewInit {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() readonly: boolean = true;
    @ViewChild('diagram') diagramRef;
    @ViewChild('palette') paletteRef;

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

    private objectTypes = [];

    private diagramMode: DiagramMode = DiagramMode.Diagram;
    DiagramMode = DiagramMode;

    //control properties
    private isWindowVisible = true;
    private showNodeTabs = false;
    private showLinkTabs = false;
    private menuItems: MenuItem[] = [];
    private tab: string = 'info';
    private headerText = 'Info';

    //diagram properties
    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;
    private myPalette: go.Palette;

    constructor(
        private myElement: ElementRef,
        protected permissionsService: PermissionsService,
        private diagramService: DiagramService,
        private lineageService: LineageService,
        private renderer: Renderer) {
        super();
    }

    public ngOnInit() {
        this.readonly = this.readonly.toString() == 'true' ? true : false;

        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
        this.initializeDiagram();
        if (!this.readonly) this.initializePalette();
    }

    public ngOnChanges() {
        if (!this.readonly && this.myPalette == null)
            this.initializePalette();
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        if (this.myDiagram != null)
            this.myDiagram.div = null;
        if (this.myPalette != null)
            this.myPalette.div = null;
    }

    //#region helper methods

    private initializeDiagram() {
        this.myDiagram = this.createDiagram();

        this.myDiagram.nodeTemplateMap.add('map', this.createMapNode());
        this.myDiagram.nodeTemplateMap.add('object', this.createObjectNode());
        this.myDiagram.nodeTemplateMap.add('palette', this.createPaletteNode());

        this.myDiagram.linkTemplateMap.add('', this.createDefaultLink());

        this.myDiagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.myDiagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));

        this.myDiagram.grid.visible = false;
        this.myDiagram.grid.gridCellSize = new go.Size(8, 8);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.populateDiagram();
    }

    private initializePalette() {
        this.lineageService.getLineageObjectTypes()
            .then(r => {
                this.objectTypes = r;
            })
            .then(() => {
                this.myPalette = this.createPalette();
            });
    }

    private populateDiagram(): Promise<any> {
        this.isLoading = true;
        let windowVisible = this.isWindowVisible;

        this.isWindowVisible = false;

        return this.lineageService.getLineageDiagram(this.objectType, this.objectID)
            .then(data => {
                console.log(data);
                this.parseData(data);
            })
            .then(() => {
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
                var model = new NodeModelV2();
                model.key = d.key;
                model.object = d.object;
                model.objectId = d.objectId;
                model.name = d.object == 'Map' ? d.objectId : d.name;
                model.foreColor = d.foreColor;
                model.backColor = d.backColor;

                model.category = (d.object == 'Map' ? 'map' : 'object');
                

                //if (model.category == 'map')
                    modelList.push(model);
            }
        }

        if (data.links) {
            for (var i = 0; i < data.links.length; i++) {
                var d = data.links[i];
                var link = new LinkModelV2();
                link.intersectId = d.intersectId;
                link.from = d.from;
                link.to = d.to;
                link.category = '';
                linkList.push(link);
            }
        }

        for (var i = 0; i < modelList.length; i++) {
            this.myDiagram.model.addNodeData(modelList[i]);
        }

        for (var i = 0; i < linkList.length; i++) {
            dm.addLinkData(linkList[i]);
        }

        //get deep copy of lists
        this.initialLinks = _.cloneDeep(linkList);
        this.initialNodes = _.cloneDeep(modelList);

        this.refreshControls(null);  //set buttons/expanders to defaults

        this.myDiagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private refreshControls(data: any) {
        this.setSourceValues(data);
        this.toggleTabs(data);
        this.loadMenuItems();
    }

    private toggleTabs(data: NodeModelV2 | LinkModelV2) {
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

    private loadMenuItems() {
        this.menuItems = [];

        this.menuItems.push({
            icon: 'fa-pencil',
            items: null
        });

        this.menuItems.push({
            icon: 'fa-object-group',
            items: null
        });

        this.menuItems.push({
            icon: 'fa-object-ungroup',
            items: null
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
        this.diagramRef.nativeElement.style.height = (window.innerHeight - 142) + 'px';
        this.paletteRef.nativeElement.style.height = (window.innerHeight - 142) + 'px';
        //this.overlayMaxHeight = window.innerHeight - oOffset;
    }

    private zoomDiagram(v: number) {
        this.myDiagram.scale = v;
        //console.log('zoomDiagram', v, this.myDiagram);
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
        //TODO: this is a hack, need a better way to handle these clicks
        if (e.icon == 'fa-pencil') {
            this.readonly = !this.readonly;
            if (!this.readonly && this.myPalette == null)
                this.initializePalette();
            this.resizeDiagram();
        }
        else if (e.icon == 'fa-object-group') {
            this.groupSelection();
        }
        else if (e.icon == 'fa-object-ungroup') {
            this.ungroupSelection();
        }
    }

    private closeEditor() {
        this.headerText = 'Lineage';
        this.diagramMode = DiagramMode.Diagram;
        this.loadMenuItems();
    }

    private ungroupSelection() {
        let selection = this.myDiagram.selection;
        let maps = [];

        selection.each(s => {
            let data = s.data;
            if (data.category == 'map' && data.isGroup == false && data.group != null) {
                this.myDiagram.model.setDataProperty(data, 'group', null);
                maps.push(data);
            }
        });

        this.removeEmptyGroups()
        this.reOrderLayout();
    }

    private groupSelection() {
        let group = new NodeModelV2();
        let selection = this.myDiagram.selection;
        let maps = [];

        selection.each(s => {
            let data = s.data;
            if (data.category == 'map')
                maps.push(data);
        });

        if (maps.length > 1) {

            group.isGroup = true;
            this.myDiagram.model.addNodeData(group); //generates a temp group key
            this.myDiagram.model.setDataProperty(group, 'name', 'New Group');

            maps.forEach(m => {
                let groupKey = m.group == null ? group.key : null;
                this.myDiagram.model.setDataProperty(m, 'group', group.key);
            });

            this.removeEmptyGroups();
            this.reOrderLayout();
        }

        
        //console.log(maps, group);
    }

    private removeEmptyGroups() {
        //remove any groups which are empty or contain only 1 item
        let removes = [];
        this.myDiagram.findTopLevelGroups().each(g => {
            let nodes = this.myDiagram.model.nodeDataArray;
            if (nodes.filter(n => (<any>n).group == g.key && (<any>n).isGroup == false).length < 2) {
                removes.push(g.data);
            }
        });

        removes.forEach(r => this.myDiagram.model.removeNodeData(r));
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
            layout: this.g(go.ForceDirectedLayout, { arrangementSpacing: new go.Size(50, 50) }),
            "undoManager.isEnabled": true
        });

        dg.model.class = go.GraphLinksModel;
        dg.model.nodeCategoryProperty = 'category';
        dg.model.linkFromPortIdProperty = 'frompid';
        dg.model.linkToPortIdProperty = 'topid';
        dg.model.nodeDataArray = [];
        dg.model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = !this.readonly;
        dg.model.isReadOnly = this.readonly;

        return dg;
    }

    private createPalette(): go.Palette {
        let paletteModel = [];

        this.objectTypes.forEach(o => {
            paletteModel.push({
                category: 'palette',
                name: o.name,
                object: o.object,
                objectId: o.objectId,
                foreColor: o.foreColor,
                backColor: o.backColor
            });
        });

        let pt = this.g(go.Palette, "LineagePalette",
            {
                "animationManager.duration": 400,
                nodeTemplateMap: this.myDiagram.nodeTemplateMap,
                model: new go.GraphLinksModel(paletteModel),
                layout: this.g(go.GridLayout, { alignment: go.GridLayout.Location })
            });

        return pt;
    }

    private createObjectNode(): go.Node {
        let nodeWidth = 150;
        let nodeHeight = 70;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 10;

        return this.g(go.Node, "Spot",
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
                    new go.Binding("fill", "backColor").makeTwoWay()
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
                        new go.Binding("stroke", "foreColor").makeTwoWay()
                    ))
            ));
    }

    private createMapNode(): go.Node {
        let nodeWidth = 50;
        let nodeHeight = 50;
        let nodeBorderColor = '#000';
        let nodeFontSize = 12;

        return this.g(go.Node, "Spot",
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "Circle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 2,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape"
                },
                    new go.Binding("fill", "backColor").makeTwoWay()
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
                        new go.Binding("stroke", "foreColor").makeTwoWay()
                    ))
            ));
    }

    private createDefaultLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.Normal,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false,
                curve: go.Link.Bezier
            }, // the whole link panel
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

    private createPaletteNode(): go.Node {
        let nodeWidth = 150;
        let nodeHeight = 35;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 10;

        return this.g(go.Node, "Spot",
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
                    new go.Binding("fill", "backColor").makeTwoWay()
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
                        new go.Binding("stroke", "foreColor").makeTwoWay()
                    ))
            ));
    }

    private createDefaultGroup(): go.Group {
         return this.g(go.Group, "Auto",
          { // define the group's internal layout
              layout: this.g(go.TreeLayout,
                      { angle: 90, arrangement: go.TreeLayout.ArrangementHorizontal, isRealtime: false }),
            // the group begins unexpanded;
            // upon expansion, a Diagram Listener will generate contents for the group
            isSubGraphExpanded: false
          },
          this.g(go.Shape, "Rectangle",
            { fill: null, stroke: "gray", strokeWidth: 2 }),
          this.g(go.Panel, "Vertical",
            { defaultAlignment: go.Spot.Left, margin: 4 },
            this.g(go.Panel, "Horizontal",
              { defaultAlignment: go.Spot.Top },
              // the SubGraphExpanderButton is a panel that functions as a button to expand or collapse the subGraph
              this.g("SubGraphExpanderButton"),
              this.g(go.TextBlock,
                { font: "Bold 18px Sans-Serif", margin: 4 },
                new go.Binding("text", "name"))
            ),
            // create a placeholder to represent the area where the contents of the group are
            this.g(go.Placeholder,
              { padding: new go.Margin(0, 10) })
          )  // end Vertical Panel
        );  // end Group
    }
    //#endregion
}

enum DiagramMode {
    Diagram,
    SourceRuleEditor,
    BusinessLineageEditor,
    TechnicalLineageEditor
}
