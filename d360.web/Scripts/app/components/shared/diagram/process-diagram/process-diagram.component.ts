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
import { AssetType, FlowObjectType } from '../../../../models/asset.model';

declare var window: any;

@Component({
    selector: 'd3s-process-diagram',
    templateUrl: './process-diagram.component.html',
    providers: [BrowserService, PermissionsService, PredicatesService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramComponent extends DiagramBaseComponent implements OnInit {

    private assetTypeNodes: AssetType[] = [];


    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }

    myDiagram: any;
    myPalette: any;

    ngOnInit() {
        this.loadDiagram();
      


        //// Show the diagram's model in JSON format that the user may edit
        //function save() {
        //    saveDiagramProperties();  // do this first, before writing to JSON
        //    document.getElementById("mySavedModel").value = myDiagram.model.toJson();
        //    this.myDiagram.isModified = false;
        //}
        //function load() {
        //    myDiagram.model = go.Model.fromJson(document.getElementById("mySavedModel").value);
        //    loadDiagramProperties();  // do this after the Model.modelData has been brought into memory
        //}

        //function saveDiagramProperties() {
        //    myDiagram.model.modelData.position = go.Point.stringify(myDiagram.position);
        //}
        //function loadDiagramProperties(e) {
        //    // set Diagram.initialPosition, not Diagram.position, to handle initialization side-effects
        //    var pos = myDiagram.model.modelData.position;
        //    if (pos) myDiagram.initialPosition = go.Point.parse(pos);
        //}

    }


    loadDiagram() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        this.myDiagram =
            $(go.Diagram, "myDiagramDiv",  // must name or refer to the DIV HTML element
                {
                    grid: $(go.Panel, "Grid",
                        $(go.Shape, "LineH", { stroke: "lightgray", strokeWidth: 0.5 }),
                        $(go.Shape, "LineH", { stroke: "gray", strokeWidth: 0.5, interval: 10 }),
                        $(go.Shape, "LineV", { stroke: "lightgray", strokeWidth: 0.5 }),
                        $(go.Shape, "LineV", { stroke: "gray", strokeWidth: 0.5, interval: 10 })
                    ),
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
                    "undoManager.isEnabled": true
                });


        function makePort(name, spot, output, input) {
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

        var nodeSelectionAdornmentTemplate =
            $(go.Adornment, "Auto",
                $(go.Shape, { fill: null, stroke: "deepskyblue", strokeWidth: 1.5, strokeDashArray: [4, 2] }),
                $(go.Placeholder)
            );

        var nodeResizeAdornmentTemplate =
            $(go.Adornment, "Spot",
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

        var nodeRotateAdornmentTemplate =
            $(go.Adornment,
                { locationSpot: go.Spot.Center, locationObjectName: "CIRCLE" },
                $(go.Shape, "Circle", { name: "CIRCLE", cursor: "pointer", desiredSize: new go.Size(7, 7), fill: "lightblue", stroke: "deepskyblue" }),
                $(go.Shape, { geometryString: "M3.5 7 L3.5 30", isGeometryPositioned: true, stroke: "deepskyblue", strokeWidth: 1.5, strokeDashArray: [4, 2] })
            );

        this.myDiagram.nodeTemplate =
            $(go.Node, "Spot",
                { locationSpot: go.Spot.Center },
                new go.Binding("location", "loc", go.Point.parse).makeTwoWay(go.Point.stringify),
                { selectable: true, selectionAdornmentTemplate: nodeSelectionAdornmentTemplate },
                { resizable: true, resizeObjectName: "PANEL", resizeAdornmentTemplate: nodeResizeAdornmentTemplate },
                { rotatable: true, rotateAdornmentTemplate: nodeRotateAdornmentTemplate },
                new go.Binding("angle").makeTwoWay(),
                // the main object is a Panel that surrounds a TextBlock with a Shape
                $(go.Panel, "Auto",
                    { name: "PANEL" },
                    new go.Binding("desiredSize", "size", go.Size.parse).makeTwoWay(go.Size.stringify),
                    $(go.Shape, "Rectangle",  // default figure
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
                makePort("T", go.Spot.Top, false, true),
                makePort("L", go.Spot.Left, true, true),
                makePort("R", go.Spot.Right, true, true),
                makePort("B", go.Spot.Bottom, true, false),
                { // handle mouse enter/leave events to show/hide the ports
                    mouseEnter: function (e, node) { showSmallPorts(node, true); },
                    mouseLeave: function (e, node) { showSmallPorts(node, false); }
                }
            );

        function showSmallPorts(node, show) {
            node.ports.each(function (port) {
                if (port.portId !== "") {  // don't change the default port, which is the big shape
                    port.fill = show ? "rgba(0,0,0,.3)" : null;
                }
            });
        }

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
                            editable: true
                        },
                        new go.Binding("text").makeTwoWay())
                )
            );

        this.loadPalette();
    }



    loadPalette() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        // initialize the Palette that is on the left side of the page
        this.myPalette =
            $(go.Palette, "myPaletteDiv",  // must name or refer to the DIV HTML element
                {
                    maxSelectionCount: 1,
                    nodeTemplateMap: this.myDiagram.nodeTemplateMap,  // share the templates used by myDiagram
                    linkTemplate: // simplify the link template, just in this Palette
                        $(go.Link,
                            { // because the GridLayout.alignment is Location and the nodes have locationSpot == Spot.Center,
                                // to line up the Link in the same manner we have to pretend the Link has the same location spot
                                locationSpot: go.Spot.Center,
                                selectionAdornmentTemplate:
                                    $(go.Adornment, "Link",
                                        { locationSpot: go.Spot.Center },
                                        $(go.Shape,
                                            { isPanelMain: true, fill: null, stroke: "deepskyblue", strokeWidth: 0 }),
                                        $(go.Shape,  // the arrowhead
                                            { toArrow: "Standard", stroke: null })
                                    )
                            },
                            {
                                routing: go.Link.AvoidsNodes,
                                curve: go.Link.JumpOver,
                                corner: 5,
                                toShortLength: 4
                            },
                            new go.Binding("points"),
                            $(go.Shape,  // the link path shape
                                { isPanelMain: true, strokeWidth: 2 }),
                            $(go.Shape,  // the arrowhead
                                { toArrow: "Standard", stroke: null })
                        ),
                    model: new go.GraphLinksModel([  // specify the contents of the Palette
                        { text: "Start", figure: "Circle", fill: "#00AD5F" },
                        { text: "Event", figure: "Circle", fill: "#00AD5F" },
                        { text: "Activity", figure: "Rectangle", fill: "lightgray" },
                        { text: "Gateway", figure: "Diamond", fill: "lightskyblue" },
                        { text: "End", figure: "Circle", fill: "#CE0620" }
                    ], [
                        // the Palette also has a disconnected Link, which the user can drag-and-drop
                        { points: new go.List(/*go.Point*/).addAll([new go.Point(0, 0), new go.Point(30, 0), new go.Point(30, 40), new go.Point(60, 40)]) }
                    ])
                });

    }

    private getData() {
        this.myDiagram.model.toJson();
    }

}