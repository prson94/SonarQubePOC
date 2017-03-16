import { Component, Input, OnInit, AfterViewInit, ElementRef, OnDestroy, ViewChild, Renderer, HostListener, Output, EventEmitter } from '@angular/core';
import { PermissionsService } from '../../../services/permissions.service';
import { BaseComponent } from '../base.component';
import { WorkflowService } from '../../../services/workflow.service';
import {
    WorkflowDiagramModel,
    WorkflowDiagramNode,
    WorkflowDiagramLink,
    NodeModel,
    LinkModel,
    DiagramObjectType,
    StepType,
    TransitionType,
    LinkType,
    ActivityTypeInfo,
} from '../../../models/workflow.model';

import { MenuItem } from 'primeng/primeng';

import * as go from 'gojs';
import * as _ from 'lodash';

declare var window: any;

@Component({
    selector: 'd3s-workflow-diagram',
    templateUrl: './workflow-diagram.component.html',
    providers: [ PermissionsService, WorkflowService ]
})

export class WorkflowDiagramComponent extends BaseComponent implements OnInit, AfterViewInit {
    @Input() id: number = 0;
    @Input() readonly: boolean = true;
    @Output() onCloseClick = new EventEmitter();
    @ViewChild('workflowDiagram') diagramRef;
    @ViewChild('workflowPalette') paletteRef;

    private model: WorkflowDiagramModel;
    private activityTypes: ActivityTypeInfo[] = [];
    DiagramObjectType = DiagramObjectType;
    StepType = StepType;
    TransitionType = TransitionType;
    LinkType = LinkType;

    //diagram properties
    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;
    private myPalette: go.Palette;
    private initialLinks: go.Link[] = [];
    private initialNodes: go.Node[] = [];
    private selectedData = null;
    private selection: any;

    private menuItems: MenuItem[] = [];
    private isWindowVisible = false;




    constructor(private myElement: ElementRef, protected permissionsService: PermissionsService, private renderer: Renderer, private workflowService: WorkflowService) {
        super();
    }

    public ngOnInit() {
        this.initializeDiagram();

    }

    public ngAfterViewInit() {
        this.resizeDiagram();
        this.loadMenuItems();
    }

    public ngOnDestroy() {
        //garbage collection
        this.myDiagram.div = null;
    }

    //#region helper methods


    private unsubscribe() {

    }


    private initializeDiagram() {
        this.myDiagram = this.createDiagram();

        this.myDiagram.nodeTemplateMap.add('task', this.createTaskNode());
        this.myDiagram.nodeTemplateMap.add('start', this.createTerminalNode(true));
        this.myDiagram.nodeTemplateMap.add('finish', this.createTerminalNode(false));
        this.myDiagram.linkTemplateMap.add('', this.createDefaultLink());

        this.myDiagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.myDiagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        
        this.myDiagram.grid.visible = false;
        this.myDiagram.grid.gridCellSize = new go.Size(8, 8);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;
        
        this.getActivityTypes().then(() => this.populateDiagram()).then(() => this.initializePalette());
    }

    private initializePalette() {
        this.myPalette = this.createPalette();
    }

    private getActivityTypes(): Promise<any> {
        return this.workflowService.getActivityTypes()
            .then(r => {
                this.activityTypes = r;
                console.log(r);
            });

    }

    private populateDiagram(): Promise<any> {
        this.isLoading = true;

        this.workflowService.getWorkflowDiagram(this.id)
            .then(r => {
                this.model = r;
                if (this.model.Nodes != null)
                    this.model.Nodes.forEach(n => n.ActivityTypeInfo = this.activityTypes.find(a => a.ID == n.ActivityType));
                console.log(this.model);
                this.parseData(this.model);
                this.isLoading = false;
            });

        return null;
    }

    private parseData(data: WorkflowDiagramModel) {
        this.myDiagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.myDiagram.model;
        dm.nodeDataArray = [];
        dm.linkDataArray = [];
        this.initialNodes = [];
        this.initialLinks = [];
        var modelList = [];
        var linkList = [];

        if (data.Nodes) {
            for (var i = 0; i < data.Nodes.length; i++) {
                let d = data.Nodes[i];
                let node = new NodeModel();
                node.key = d.Key;
                node.name = d.Name;
                node.pos = `${d.XPosition} ${d.YPosition}`;
                node.x = d.XPosition;
                node.y = d.YPosition;
                node.activityType = d.ActivityType;
                node.stepType = d.StepType;

                if (d.ActivityTypeInfo != null) {
                    node.fore = d.ActivityTypeInfo.ForeColor;
                    node.back = d.ActivityTypeInfo.BackColor;
                    node.icon = d.ActivityTypeInfo.Icon;
                    node.activityName = d.ActivityTypeInfo.Name;
                    node.activityDescription = d.ActivityTypeInfo.Description;
                }

                if (d.SettingsObject != null && d.SettingsObject.settings != null)
                    node.settings = d.SettingsObject.settings;

                if (d.StepType == StepType.Start)
                    node.template = 'start';
                else if (d.StepType == StepType.Finish)
                    node.template = 'finish';

                modelList.push(node);
            }
        }

        if (data.Links) {
            for (var i = 0; i < data.Links.length; i++) {
                let d = data.Links[i];
                let link = new LinkModel();
                link.key = d.Key;
                link.from = d.FromKey;
                link.to = d.ToKey;
                link.name = d.Name;
                link.condition = d.Condition;
                link.linkType = d.LinkType;
                link.transitionType = d.TransitionType;

                linkList.push(link);
            }
        }

        for (var i = 0; i < modelList.length; i++) {
            this.myDiagram.model.addNodeData(modelList[i]);
        }


        for (var i = 0; i < linkList.length; i++) {
            dm.addLinkData(linkList[i]);
            dm.setCategoryForLinkData(linkList[i], linkList[i].category);
        }

        //get deep copy of lists
        this.initialLinks = _.cloneDeep(linkList);
        this.initialNodes = _.cloneDeep(modelList);

        this.myDiagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private loadMenuItems() {
        this.menuItems = [];

        this.menuItems.push({
            icon: 'fa-info-circle'
        });

        if (this.readonly)
            this.menuItems.push({
                icon: 'fa-remove'
            });
       
    }

    //#endregion

    //#region events

    private menuClick(e: any) {
        console.log(e);
        if (e.icon == 'fa-info-circle')
            this.isWindowVisible = !this.isWindowVisible;
        if (e.icon == 'fa-remove')
            this.onCloseClick.emit();
    }

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

    private reOrderLayout() {
        this.myDiagram.layout.invalidateLayout();
        this.myDiagram.requestUpdate();
    }

    private ChangedSelection(e: any) {
        this.selection = e.diagram.selection;

        if (this.selection.count == 0) {
            this.selectedData = null;
        } else {
            var sel = _.cloneDeep(this.selection.toArray());

            if (sel != null && sel.length != 0) {
                this.selectedData = sel[0].data;
            }
        }
        console.log(e);
        console.log(this.selectedData);
    }

    private ObjectDoubleClicked(e: any) {
        console.log('double clicked', e);
        //var obj = e.diagram.selection.first().data;
    }


    //#endregion

    //#region templates

    private createPalette(): go.Palette {

        let paletteModel = [];

        this.activityTypes.forEach(a => {
            paletteModel.push({
                template: 'task',
                category: 'task',
                fore: a.ForeColor,
                back: a.BackColor,
                activityName: a.Name,
                icon: a.Icon,
                activityDescription: a.Description,
                pos: "0 0"
            });
        });

        let pt = this.g(go.Palette, "WorkflowPalette",
            {
                "animationManager.duration": 800,
                nodeTemplateMap: this.myDiagram.nodeTemplateMap,
                model: new go.GraphLinksModel(paletteModel),
                layout: this.g(go.GridLayout, { alignment: go.GridLayout.Location })
            });

        return pt;
    }

    private createDiagram(): go.Diagram {

        //let offset = this.diagramRef.nativeElement.offsetTop;
        //let height = window.innerHeight;

        //if (this.diagramRef.nativeElement.offsetParent) {
        //    offset += this.diagramRef.nativeElement.offsetParent.offsetTop;
        //}

        //let offsetLeft = this.diagramRef.nativeElement.offsetLeft;
        //if (this.diagramRef.nativeElement.offsetParent) {
        //    offsetLeft += this.diagramRef.nativeElement.offsetParent.offsetLeft;
        //}
        //let width = window.innerWidth;

        let dg = this.g(go.Diagram, 'WorkflowDiagram', {
            //fixedBounds: new go.Rect(offset, offsetLeft, (height - offset - 50), (width - offsetLeft - 50)),
            initialContentAlignment: go.Spot.Left,
            allowDrop: true,
            //allowHorizontalScroll: false,  // disallow scrolling or panning
            //allowVerticalScroll: false,
            //allowZoom: false,   
            //initialAutoScale: go.Diagram.UniformToFill,
            //scrollMode: go.Diagram.DocumentScroll,
            //initialPosition: new go.Point(125, 125),
            //layout: this.g(go.LayeredDigraphLayout, { direction: 0, columnSpacing: 50, layerSpacing: 50 }),
            "undoManager.isEnabled": !this.readonly
        });

        dg.model.class = go.GraphLinksModel;
        dg.model.nodeCategoryProperty = "template";
        dg.model.linkFromPortIdProperty = "frompid";
        dg.model.linkToPortIdProperty = "topid";
        dg.model.nodeDataArray = [];
        dg.model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = !this.readonly;
        //dg.model.isReadOnly = this.readonly;

        return dg;
    }

    private createTaskNode(): go.Node {
        let nodeWidth = 150;
        let nodeHeight = 75;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 10;

        return this.g(go.Node, "Spot",
            new go.Binding("location", "pos", go.Point.parse).makeTwoWay(go.Point.stringify),
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 2,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape",
                },
                    new go.Binding("fill", "back").makeTwoWay()),
                this.g(go.Panel, go.Panel.Horizontal, {
                    alignment: go.Spot.BottomLeft,
                    margin: 5
                },
                    this.makeIconPanel(nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
                        row: 0,
                        margin: 3,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: "bold " + nodeFontSize + "pt sans-serif",
                    },
                        new go.Binding("text", "activityName").makeTwoWay(),
                        new go.Binding("stroke", "fore").makeTwoWay()
                    )
                )
            )
        );
    }

    private createTerminalNode(isStart: boolean): go.Node {
        let nodeWidth = 200;
        let nodeHeight = 105;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 10;
        let backColor = isStart ? '#216b23' : '#6b2121';

        return this.g(go.Node, "Spot",
            new go.Binding("location", "pos", go.Point.parse).makeTwoWay(go.Point.stringify),
            this.g(go.Shape, "Circle", {
                stroke: nodeBorderColor,
                strokeWidth: 2,
                width: 64,
                height: 64,
                name: "NodeShape",
                fill: backColor
            }),
            this.g(go.Panel, "Table",
                this.g(go.TextBlock, {
                    row: 0,
                    margin: 0,
                    alignment: go.Spot.Center,
                    editable: false,
                    font: "bold " + nodeFontSize + "pt sans-serif",
                    stroke: "#fff"
                },
                new go.Binding("text", "name").makeTwoWay()
            ))
        );
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


    private makeIconPanel(fontSize) {
        fontSize -= 2;
        let iconPanel = this.g(go.Panel,
            "Auto",
            {
                alignment: go.Spot.Center,
                margin: 2
            },
            //this.g(go.Shape, "Circle",
            //    {
            //        stroke: null,
            //        toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, { fill: "lightyellow" }), this.g(go.Panel, "Vertical", this.g(go.TextBlock, { margin: 3, text: tooltip })))
            //    })
            //    ,
                //new go.Binding("fill", "fore")),
            this.g(go.TextBlock,
                {
                    row: 0,
                    margin: 0,
                    alignment: go.Spot.Center,
                    editable: false,
                    font: (fontSize) + "pt FontAwesome",
                },
                new go.Binding("text", "icon").makeTwoWay(),
                new go.Binding("stroke", "fore").makeTwoWay()
                //,
                //new go.Binding("stroke", "back")
            )
            //,
            //new go.Binding("visible", binding)
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

