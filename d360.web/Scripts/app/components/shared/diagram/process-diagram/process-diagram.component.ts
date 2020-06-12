import * as go from 'gojs';
import * as _ from 'lodash';
import { Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { FlowObjectType, AssetTypeClass, AssetTypeApiModel } from '../../../../models/asset.model';
import { AssetTypeService } from '../../../../services/asset-type.service';
import { FontAwesomeHelper } from '../../../../static/font-awesome-helper';

@Component({
    selector: 'd3s-process-diagram',
    templateUrl: './process-diagram.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramComponent extends DiagramBaseComponent implements OnInit {
    @Input() isEditMode: boolean = false;

    private assetTypeNodes: AssetTypeApiModel[] = [];
    private events: AssetTypeApiModel[] = [];
    private activities: AssetTypeApiModel[] = [];
    private gateways: AssetTypeApiModel[] = [];
    private isLoaded = false;
    private isSaveDisabled: boolean = false;
    private isCanvasEmpty: boolean = true;


    private fontColor: string = '#202020';

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private assetTypeService: AssetTypeService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }

    myDiagram: go.Diagram;

    ngOnInit() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates
        this.assetTypeService.getAssetTypesByClass(AssetTypeClass.DiagramAsset)
            .subscribe(res => {
                this.assetTypeNodes = res;
                this.events = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Event);
                this.activities = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Activity);
                this.gateways = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Gateway);
                this.isLoaded = true;
                this.loadDiagram();
                this.applyEditMode(this.isEditMode);
                this.cdRef.detectChanges();
            });
    }

    private applyEditMode(state: boolean) {
        this.myDiagram.nodes.each(function (n) {
            if (n instanceof go.Node) {
                n.isEnabled = state;
                n.movable = state;
            }
        });
        this.myDiagram.links.each(function (n) {
            if (n instanceof go.Link) {
                n.isEnabled = state;
                n.movable = state;
            }
        });
        this.myDiagram.isModelReadOnly = !state;

    }

    switchModes() {
        this.isEditMode = !this.isEditMode;
        this.applyEditMode(this.isEditMode);
        this.cdRef.detectChanges();
    }

    disableDrag() {
        this.myDiagram.toolManager.panningTool.isEnabled = !this.myDiagram.toolManager.panningTool.isEnabled;
    }

    loadDiagram() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        this.myDiagram =
            $(go.Diagram, "diagram",  // must name or refer to the DIV HTML element
                {
                    "draggingTool.dragsLink": true,
                    "draggingTool.isGridSnapEnabled": true,
                    "linkingTool.portGravity": 20,
                    "relinkingTool.portGravity": 20,
                    "rotatingTool.handleAngle": 270,
                    "rotatingTool.handleDistance": 30,
                    "rotatingTool.snapAngleMultiple": 15,
                    "rotatingTool.snapAngleEpsilon": 15,
                    "undoManager.isEnabled": true
                });


        this.myDiagram.grid.gridCellSize = new go.Size(20, 20);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.draggingTool.gridSnapCellSpot = go.Spot.Center;

        this.myDiagram.addModelChangedListener(() => {
            this.diagramStateChanged();
        })

        var activityNodeTemplate = $(go.Node, "Auto",
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            {
                selectable: true,
                selectionAdornmentTemplate: this.nodeSelectionAdornmentTemplate("RoundedRectangle"),

            },
            $(go.Shape, "RoundedRectangle",
                {
                    fill: "white",
                    stroke: "#708EA6",
                    portId: "",
                    strokeWidth: 2,
                    fromLinkable: true,
                    toLinkable: true,
                    margin: new go.Margin(1, 1, 1, 1),
                    cursor: "pointer",
                }),

            $(go.Panel, "Vertical",
                $(go.Panel, "Auto",
                    { stretch: go.GraphObject.Horizontal },  // as wide as the whole node
                    $(go.Shape,
                        {
                            fill: "#708EA6",
                            stroke: "#708EA6",
                            strokeWidth: 2,

                            minSize: new go.Size(200, NaN)
                        }),
                    $(go.TextBlock,
                        {
                            alignment: go.Spot.LeftCenter,
                            stroke: "white",
                            textAlign: "center",
                            font: '18px FontAwesome',
                            margin: new go.Margin(6, 0, 0, 12),
                            minSize: new go.Size(NaN, 24)
                        }
                        , new go.Binding("text", "icon").makeTwoWay()
                    )),
                $(go.TextBlock,
                    {
                        alignment: go.Spot.LeftCenter,
                        stroke: this.fontColor,
                        textAlign: "center",
                        font: "bold 12pt sans-serif",
                        editable: true,
                        minSize: new go.Size(NaN, 30),
                        margin: new go.Margin(12, 0, 0, 5)

                    },
                    new go.Binding("text", "name").makeTwoWay()
                )
            ),
            this.makePort("T", go.Spot.Top, false, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, false),
            { // handle mouse enter/leave events to show/hide the ports
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            }
        );

        var eventNodeTemplate = $(go.Node, "Spot",
            {
                locationSpot: go.Spot.Center,
                selectable: true,
                selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate()
            },
            this.makePort("T", go.Spot.Top, false, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, false),
            {
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            $(go.Panel, "Vertical",
                {
                    name: "PANEL"
                },
                $(go.Panel, "Auto",
                    {
                        name: "PANEL"
                    },
                    $(go.Shape, "Circle",
                        {
                            fill: 'transparent',
                            stroke: "black",
                            strokeWidth: 2,
                            visible: false
                        },
                        new go.Binding('visible', 'isSelected').ofObject()),
                    $(go.Shape, "Circle",
                        {
                            portId: "",
                            fromLinkable: true,
                            toLinkable: true,
                            cursor: "pointer",
                            stroke: "#708EA6",
                            fill: 'white',
                            strokeWidth: 2,
                            width: 70,
                            height: 70,
                        }),
                    $(go.TextBlock,
                        {
                            alignment: go.Spot.Center,
                            stroke: '#708EA6',
                            textAlign: "center",
                            font: '48px FontAwesome',
                            margin: new go.Margin(5, 0, 0, 0)
                        },
                        new go.Binding("text", "icon").makeTwoWay())
                ),
                $(go.TextBlock,
                    {
                        font: "bold 11pt Helvetica, Arial, sans-serif",
                        margin: 8,
                        wrap: go.TextBlock.WrapFit,
                        editable: true,
                        stroke: 'black'
                    }
                    , new go.Binding("text", "name").makeTwoWay())
            )
        );

        var gatewayNodeTemplate = $(go.Node, "Spot",
            { locationSpot: go.Spot.Center },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            { selectable: true, selectionAdornmentTemplate: this.nodeSelectionEmptyTemplate() },
            new go.Binding("angle").makeTwoWay(),
            $(go.Panel, "Vertical",
                { name: "PANEL" },
                new go.Binding("desiredSize", "size", go.Size.parse).makeTwoWay(go.Size.stringify),
                $(go.Panel, "Auto",
                    { name: "PANEL" },
                    new go.Binding("desiredSize", "size", go.Size.parse).makeTwoWay(go.Size.stringify),
                    $(go.Shape, "Rectangle",
                        {
                            angle: 45,
                            width: 58,
                            height: 58,
                            fill: 'transparent',
                            stroke: "black",
                            strokeWidth: 2,
                            visible: false
                        },
                        new go.Binding('visible', 'isSelected').ofObject()),
                    $(go.Shape, "Rectangle",
                        {
                            angle: 45,
                            width: 50,
                            height: 50,
                            portId: "",
                            fromLinkable: true,
                            toLinkable: true,
                            cursor: "pointer",
                            stroke: "#708EA6",
                            fill: 'white',
                            strokeWidth: 2
                        },
                        new go.Binding("figure"),
                        new go.Binding("fill")),
                    $(go.TextBlock,
                        {
                            alignment: go.Spot.Center,
                            margin: new go.Margin(5, 0, 0, 0),
                            stroke: '#708EA6',
                            textAlign: "center",
                            font: '32px FontAwesome'
                        },
                        new go.Binding("text", "icon").makeTwoWay())
                ),
                $(go.TextBlock,
                    {
                        font: "bold 11pt Helvetica, Arial, sans-serif",
                        margin: 8,
                        maxSize: new go.Size(160, NaN),
                        wrap: go.TextBlock.WrapFit,
                        editable: true,
                        stroke: 'black'
                    }
                    , new go.Binding("text", "name").makeTwoWay())
            )
            ,
            this.makePort("T", go.Spot.Top, false, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, false)
        );

        function showSmallPorts(node, show) {
            node.ports.each(function (port) {
                if (port.portId !== "") {  // don't change the default port, which is the big shape
                    port.fill = show ? "rgba(0,0,0,0.3)" : null;
                }
            });
        }


        var templmap = new go.Map<string, go.Node>();
        templmap.add("activity", activityNodeTemplate);
        templmap.add("event", eventNodeTemplate);
        templmap.add("gateway", gatewayNodeTemplate);
        templmap.add("", activityNodeTemplate);
        this.myDiagram.nodeTemplateMap = templmap;

        var linkSelectionAdornmentTemplate =
            $(go.Adornment, "Link",
                $(go.Shape,
                    // isPanelMain declares that this Shape shares the Link.geometry
                    { isPanelMain: true, fill: null, stroke: "deepskyblue", strokeWidth: 0 })  // use selection object's strokeWidth
            );

        this.myDiagram.linkTemplate =
            $(go.Link,  // the whole link panel
                { selectable: true, selectionAdornmentTemplate: linkSelectionAdornmentTemplate },
                { relinkableFrom: true, relinkableTo: true, reshapable: true },
                {
                    routing: go.Link.AvoidsNodes,
                    curve: go.Link.JumpOver,
                    corner: 5,
                    toShortLength: 4
                },
                new go.Binding("points").makeTwoWay(),
                $(go.Shape,  // the link path shape
                    { isPanelMain: true, strokeWidth: 2 }),
                $(go.Shape,  // the arrowhead
                    { toArrow: "Standard", stroke: null }),
                $(go.Panel, "Auto",
                    new go.Binding("visible", "isSelected").ofObject(),
                    $(go.Shape, "RoundedRectangle",  // the link shape
                        { fill: "#F8F8F8", stroke: null }),
                    $(go.TextBlock,
                        {
                            textAlign: "center",
                            font: "10pt helvetica, arial, sans-serif",
                            stroke: "#919191",
                            margin: 2,
                            minSize: new go.Size(10, NaN),
                            editable: true,

                        },
                        new go.Binding("name").makeTwoWay())
                )
            );

    }




    private getData() {
        console.log(this.myDiagram.model.toJson())
        console.log(this.myDiagram);
    }

    private dragEnd($event: AssetTypeApiModel) {
        var nodeCategory: string = '';

        switch ($event.FlowObjectType) {
            case FlowObjectType.Activity: nodeCategory = 'activity'; break;
            case FlowObjectType.Event: nodeCategory = 'event'; break;
            case FlowObjectType.Gateway: nodeCategory = 'gateway'; break;
        }
        var icon = FontAwesomeHelper.GetHtmlCode($event['IconStyle'].Icon);

        setTimeout(() => {
            this.myDiagram.startTransaction("make new node");
            var point = go.Point.stringify(this.myDiagram.lastInput.documentPoint);

            var data = {
                key: this.newGuid(),
                icon: icon,
                category: nodeCategory,
                loc: point,
                //asset data
                name: this.getNewNodeName($event),
                assetTypeUid: $event.uid,
            };

            this.myDiagram.model.addNodeData(data);
            this.myDiagram.commitTransaction("make new node");
            this.isCanvasEmpty = false;
            this.myDiagram.redraw();
        }, 100);

    }

    private getNewNodeName(at: AssetTypeApiModel) {
        return this.returnUniqueName('New ' + at.Name, 1);
    }

    private returnUniqueName(name: string, iteration: number) {
        var tempName = name;
        if (iteration != 1) {
            tempName = name + ` (${iteration})`
        }
        if (this.isUnique(tempName))
            return tempName;
        return this.returnUniqueName(name, iteration + 1);
    }

    private isUnique(name: string) {
        var exists = false;

        this.myDiagram.nodes.each(function (n) {
            if (n instanceof go.Node) {
                if (n.data.name.toString() == name) {
                    exists = true;
                }
            }
        });
        return !exists;
    }


    private nodeSelectionAdornmentTemplate(shape: string) {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        return $(go.Adornment, "Auto",
            $(go.Shape, shape, { fill: null, stroke: "black", strokeWidth: 2 }),
            $(go.Placeholder)
        );
    }

    private nodeSelectionEmptyTemplate() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        return $(go.Adornment, "Auto",
            $(go.Shape, { fill: null, stroke: null, strokeWidth: 0 }),
            $(go.Placeholder)
        );
    }


    private makePort(name, spot, output, input) {
        var $ = go.GraphObject.make;
        // the port is basically just a small transparent square
        return $(go.Shape, "Circle",
            {
                fill: null,  // not seen, by default; set to a translucent gray by showSmallPorts, defined below
                stroke: null,
                desiredSize: new go.Size(7, 7),
                alignment: spot,  // align the port on the main Shape
                alignmentFocus: spot,  // just inside the Shape
                portId: name,  // declare this object to be a "port"
                fromSpot: spot, toSpot: spot,  // declare where links may connect at this port
                fromLinkable: output, toLinkable: input,  // declare whether the user may draw links to/from here
                cursor: "pointer"  // show a different cursor to indicate potential link point
            });
    }
    private savedState: go.Model;
    private diagramStateChanged() {
        this.isSaveDisabled = this.isCurrentStateSaved();
        this.cdRef.detectChanges();
    }

    private isCurrentStateSaved() {
        return JSON.stringify(this.myDiagram.model) == JSON.stringify(this.savedState);
    }

    private save() {
        console.log("save");
        this.saveToLocalStorage();
        this.savedState = JSON.parse(JSON.stringify(this.myDiagram.model));
        this.diagramStateChanged();
    }
    private clear() {
        this.myDiagram.clear();
        this.diagramStateChanged();
        console.log("clear");

    }
    private load() {
        console.log("load");
        this.loadFromLocalStorage();
        this.isCanvasEmpty = false;
    }

    private saveToLocalStorage() {
        localStorage.setItem('process-diagram', this.myDiagram.model.toJson());

    }
    private loadFromLocalStorage() {
        var model = localStorage.getItem('process-diagram');
        this.myDiagram.model = go.Model.fromJson(model);
        this.diagramStateChanged();
    }
    private newGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0,
                v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}