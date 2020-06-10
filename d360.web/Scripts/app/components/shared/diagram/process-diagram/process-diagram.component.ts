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
import { AssetType, FlowObjectType, AssetTypeClass, AssetTypeApiModel } from '../../../../models/asset.model';
import { AssetTypeService } from '../../../../services/asset-type.service';
import { FontAwesomeHelper } from '../../../../static/font-awesome-helper';

declare var window: any;

@Component({
    selector: 'd3s-process-diagram',
    templateUrl: './process-diagram.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramComponent extends DiagramBaseComponent implements OnInit {

    private assetTypeNodes: AssetTypeApiModel[] = [];
    private events: AssetTypeApiModel[] = [];
    private activities: AssetTypeApiModel[] = [];
    private gateways: AssetTypeApiModel[] = [];
    private isLoaded = false;

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
                console.log(res);
                this.isLoaded = true;
                this.loadDiagram();
                this.cdRef.detectChanges();
            });
    }


    loadDiagram() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        this.myDiagram =
            $(go.Diagram, "myDiagramDiv",  // must name or refer to the DIV HTML element
                {

                    "draggingTool.dragsLink": true,
                    "draggingTool.isGridSnapEnabled": true,
                    "linkingTool.isUnconnectedLinkValid": true,
                    "linkingTool.portGravity": 20,
                    "relinkingTool.isUnconnectedLinkValid": true,
                    "relinkingTool.portGravity": 20,
                    "relinkingTool.fromHandleArchetype":
                        $(go.Shape, "Diamond", { segmentIndex: 0, cursor: "pointer", desiredSize: new go.Size(8, 8), fill: "tomato", stroke: "darkred" }),
                    "relinkingTool.toHandleArchetype":
                        $(go.Shape, "Diamond", { segmentIndex: -1, cursor: "pointer", desiredSize: new go.Size(8, 8), fill: "darkred", stroke: "tomato" }),
                    "linkReshapingTool.handleArchetype":
                        $(go.Shape, "Diamond", { desiredSize: new go.Size(7, 7), fill: "lightblue", stroke: "deepskyblue" }),
                    "rotatingTool.handleAngle": 270,
                    "rotatingTool.handleDistance": 30,
                    "rotatingTool.snapAngleMultiple": 15,
                    "rotatingTool.snapAngleEpsilon": 15,
                    "undoManager.isEnabled": true,
                    mouseOver: function (e: go.InputEvent) {
                    }
                });



        var activityNodeTemplate = $(go.Node, "Auto",
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            {
                selectable: true,
                selectionAdornmentTemplate: this.nodeSelectionAdornmentTemplate(),

            },
            $(go.Shape, "RoundedRectangle",
                {
                    fill: "#EEEEEE",
                    stroke: "#708EA6",
                    portId: "",
                    fromLinkable: true,
                    toLinkable: true,
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
                        stroke: "black",
                        textAlign: "center",
                        font: "bold 12pt sans-serif",
                        editable: true,
                        minSize: new go.Size(NaN, 30),
                        margin: new go.Margin(6, 0, 0, 12)

                    },
                    new go.Binding("text", "assetname")
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
            { locationSpot: go.Spot.Center },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            { selectable: true, selectionAdornmentTemplate: this.nodeSelectionAdornmentTemplate() },
            new go.Binding("angle").makeTwoWay(),
            $(go.Panel, "Vertical",
                { name: "PANEL" },
                new go.Binding("desiredSize", "size", go.Size.parse).makeTwoWay(go.Size.stringify),
                $(go.Panel, "Auto",
                    { name: "PANEL" },
                    new go.Binding("desiredSize", "size", go.Size.parse).makeTwoWay(go.Size.stringify),
                    $(go.Shape, "Circle",
                        {
                            portId: "",
                            fromLinkable: true,
                            toLinkable: true,
                            cursor: "pointer",
                            stroke: "#708EA6",
                            fill: 'white',
                            strokeWidth: 2,

                        },
                        new go.Binding("figure"),
                        new go.Binding("fill")),
                    $(go.TextBlock,
                        {
                            alignment: go.Spot.Center,
                            stroke: '#708EA6',
                            textAlign: "center",
                            font: '48px FontAwesome',
                            margin: new go.Margin(5, 0, 0, 0),
                            minSize: new go.Size(NaN, 30),
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
                    , new go.Binding("text", "assetname").makeTwoWay())
            )
            ,
            this.makePort("T", go.Spot.Top, false, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, false),
            { // handle mouse enter/leave events to show/hide the ports
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            }
        );

        var gatewayNodeTemplate = $(go.Node, "Spot",
            { locationSpot: go.Spot.Center },
            new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
            { selectable: true, selectionAdornmentTemplate: this.nodeSelectionAdornmentTemplate() },
            new go.Binding("angle").makeTwoWay(),
            // the main object is a Panel that surrounds a TextBlock with a Shape
            $(go.Panel, "Auto",
                { name: "PANEL" },
                new go.Binding("desiredSize", "size", go.Size.parse).makeTwoWay(go.Size.stringify),
                $(go.Shape, "Diamond",  // default figure
                    {
                        portId: "", // the default port: if no spot on link data, use closest side
                        fromLinkable: true, toLinkable: true, cursor: "pointer",
                        fill: "white",  // default color
                        strokeWidth: 2
                    },
                    new go.Binding("figure"),
                    new go.Binding("fill")),
                $(go.TextBlock,
                    {
                        font: "bold 11pt Helvetica, Arial, sans-serif",
                        margin: 8,
                        maxSize: new go.Size(160, NaN),
                        wrap: go.TextBlock.WrapFit,
                        editable: true
                    },
                    new go.Binding("text").makeTwoWay())
            ),
            // four small named ports, one on each side:
            this.makePort("T", go.Spot.Top, false, true),
            this.makePort("L", go.Spot.Left, true, true),
            this.makePort("R", go.Spot.Right, true, true),
            this.makePort("B", go.Spot.Bottom, true, false),
            { // handle mouse enter/leave events to show/hide the ports
                mouseEnter: function (e, node) { showSmallPorts(node, true); },
                mouseLeave: function (e, node) { showSmallPorts(node, false); }
            }
        );

        function showSmallPorts(node, show) {
            return;
            node.ports.each(function (port) {
                if (port.portId !== "") {  // don't change the default port, which is the big shape
                    port.fill = show ? "rgba(0,0,0,1)" : null;
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
                        new go.Binding("assetname").makeTwoWay())
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
        console.log($event);


        setTimeout(() => {
            this.myDiagram.startTransaction("make new node");
            var point = go.Point.stringify(this.myDiagram.lastInput.documentPoint);
            var data = { assetname: this.getNewNodeName($event) };
            this.myDiagram.model.addNodeData({ key: new Date().toString(), icon: icon, assetname: this.getNewNodeName($event), category: nodeCategory, loc: point, data: data })
            this.myDiagram.commitTransaction("make new node");
            this.myDiagram.redraw();
        }, 100);

    }

    private getNewNodeName(at: AssetTypeApiModel) {
        return 'New ' + at.Name;
    }


    private nodeSelectionAdornmentTemplate() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        return $(go.Adornment, "Auto",
            $(go.Shape, { fill: null, stroke: "deepskyblue", strokeWidth: 1.5, strokeDashArray: [4, 2] }),
            $(go.Placeholder)
        );
    }

    private nodeResizeAdornmentTemplate() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        return $(go.Adornment, "Spot",
            { locationSpot: go.Spot.Right },
            $(go.Placeholder),
            $(go.Shape, { alignment: go.Spot.TopLeft, cursor: "nw-resize", desiredSize: new go.Size(6, 6), fill: "lightblue", stroke: "deepskyblue" }),
            $(go.Shape, { alignment: go.Spot.Top, cursor: "n-resize", desiredSize: new go.Size(6, 6), fill: "lightblue", stroke: "deepskyblue" }),
            $(go.Shape, { alignment: go.Spot.TopRight, cursor: "ne-resize", desiredSize: new go.Size(6, 6), fill: "lightblue", stroke: "deepskyblue" }),

            $(go.Shape, { alignment: go.Spot.Left, cursor: "w-resize", desiredSize: new go.Size(6, 6), fill: "lightblue", stroke: "deepskyblue" }),
            $(go.Shape, { alignment: go.Spot.Right, cursor: "e-resize", desiredSize: new go.Size(6, 6), fill: "lightblue", stroke: "deepskyblue" }),

            $(go.Shape, { alignment: go.Spot.BottomLeft, cursor: "se-resize", desiredSize: new go.Size(6, 6), fill: "lightblue", stroke: "deepskyblue" }),
            $(go.Shape, { alignment: go.Spot.Bottom, cursor: "s-resize", desiredSize: new go.Size(6, 6), fill: "lightblue", stroke: "deepskyblue" }),
            $(go.Shape, { alignment: go.Spot.BottomRight, cursor: "sw-resize", desiredSize: new go.Size(6, 6), fill: "lightblue", stroke: "deepskyblue" })
        );
    }

    private nodeRotateAdornmentTemplate() {
        var $ = go.GraphObject.make;
        return $(go.Adornment,
            { locationSpot: go.Spot.Center, locationObjectName: "CIRCLE" },
            $(go.Shape, "Circle", { name: "CIRCLE", cursor: "pointer", desiredSize: new go.Size(7, 7), fill: "lightblue", stroke: "deepskyblue" }),
            $(go.Shape, { geometryString: "M3.5 7 L3.5 30", isGeometryPositioned: true, stroke: "deepskyblue", strokeWidth: 1.5, strokeDashArray: [4, 2] })
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


}