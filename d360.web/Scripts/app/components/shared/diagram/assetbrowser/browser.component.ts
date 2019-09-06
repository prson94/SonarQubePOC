import * as go from 'gojs';
import * as _ from 'lodash';
import {AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild} from '@angular/core';
import {DiagramObjectType } from '../../../../models/lineage.model';
import {PermissionsService} from '../../../../services/permissions.service';
import {DiagramService} from '../../../../services/diagram.service';
import {DiagramBaseComponent} from '../diagram-base.component';
import { AssetBrowserLayout } from './assetbrowserlayout.component';

declare var window: any;

@Component({
    selector: 'd3s-assetbrowser',
    templateUrl: './browser.component.html',
    providers: [PermissionsService, DiagramService]
})
export class AssetBrowserComponent extends DiagramBaseComponent implements OnInit, AfterViewInit {
    @Input() objectId: number = 0;
    @Input() object: string;
    @Input() readonly: boolean = true;

    @ViewChild('diagram') diagramRef;

    DiagramObjectType = DiagramObjectType;

    private originalObject: string;
    private originalObjectId: number;

    //#region control properties

    private isWindowVisible = true;

    constructor(
        private myElement: ElementRef,
        protected permissionsService: PermissionsService,
        private diagramService: DiagramService
    ) {
        super();
    }

    public ngOnInit() {

        this.originalObject = this.object;
        this.originalObjectId = this.objectId;

        //this.loadPermissions(this.permissionsService, this.objectType, this.objectID);

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
        let windowVisible = this.isWindowVisible;

        this.isWindowVisible = false;

        //this.diagramService.getLineageDiagram(
        //    this.objectType,
        //    this.objectID
        //).subscribe(data => {
        this.parseData(null);//data

        //this.reOrderLayout();
        //this.diagram.zoomToFit();
        this.isLoading = false;
        //this.isWindowVisible = windowVisible;
        //});
    }

    private parseData(data: any) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        //dm.nodeDataArray = [];
        //dm.linkDataArray = [];

        let color: string = "#B9F1AF";
        let transformColor: string = "#FAE7BC";
        let sysColor: string = "#DAAADB";
        let btColor: string = "#E0EAF7";

        dm.nodeDataArray = [

            { key: "btType1", isGroup: true, text: "Business Terms", template: "PortGroup", back: btColor, loc: "600 0", layer: -2, icon: "\uf02d" },
            { key: "bt1", group: "btType1", text: "Member Name", back: this.shadeColor(btColor, 15), icon: "\uf02d" },

            { key: "sys1", isGroup: true, text: "Enrollment System", template: "PortGroup", back: sysColor, loc: "300 200", layer: -1, icon: "\uf233" },
            { key: "sysTerm1", group: "sys1", text: "Member Name", back: this.shadeColor(sysColor, 15), icon: "\uf02d" },

            { key: "sys2", isGroup: true, text: "Claims Adjudication", template: "PortGroup", back: sysColor, loc: "900 200", layer: -1, icon: "\uf233" },
            { key: "sysTerm2", group: "sys2", text: "Member Name", back: this.shadeColor(sysColor, 15), icon: "\uf02d" },

            { key: "tran1", isGroup: true, text: "BosEtlServer", template: "PortGroup", back: transformColor, loc: "560 600", layer: 0, icon: "\uf085" },
            { key: "job1", isGroup: true, group: "tran1", text: "ETL_MEMBER_TO_CLAIM", template: "Group", back: this.shadeColor(transformColor, 15), icon: "\uf542" },
            { key: "jobStep1", group: "job1", text: "LOAD_MEMBER_NAME", back: this.shadeColor(transformColor, 30), icon: "\uf085" },

            { key: "h1", isGroup: true, text: "DWH", template: "PortGroup", back: color, loc: "300 400", layer: 0, icon: "\uf1c0" },
            { key: "s1", isGroup: true, group: "h1", text: "fact", template: "Group", back: this.shadeColor(color, 15), icon: "\uf007" },
            { key: "t1", isGroup: true, group: "s1", text: "MEMBERS", template: "Group", back: this.shadeColor(color, 30), icon: "\uf0ce" },
            { key: "c1_1", group: "t1", text: "FIRST_NAME", back: this.shadeColor(color, 45), intersects: [1, 2, 3, 4, 5], icon: "\uf0db" },
            { key: "c1_2", group: "t1", text: "LAST_NAME", back: this.shadeColor(color, 45), intersects: [1, 2, 3, 4, 5], icon: "\uf0db" },

            { key: "h2", isGroup: true, text: "EGL", template: "PortGroup", back: color, loc: "900 400", layer: 0, icon: "\uf1c0" },
            { key: "s2", isGroup: true, group: "h2", text: "dbo", template: "Group", back: this.shadeColor(color, 15), icon: "\uf007" },
            { key: "t2", isGroup: true, group: "s2", text: "MEMBERS", template: "Group", back: this.shadeColor(color, 30), icon: "\uf0ce" },
            { key: "c2_1", group: "t2", text: "FIRST_NAME", back: this.shadeColor(color, 45), intersects: [1, 2, 3, 4, 5], icon: "\uf0db" },
            { key: "c2_2", group: "t2", text: "LAST_NAME", back: this.shadeColor(color, 45), intersects: [1, 2, 3, 4, 5], icon: "\uf0db" },
        ];

        dm.linkDataArray = [
            { from: "sys1", fromPort: "T", to: "btType1", toPort: "B", text: "see also", back: sysColor, intersects: [1, 2, 3, 4, 5] },
            { from: "sys2", fromPort: "T", to: "btType1", toPort: "B", text: "see also", back: sysColor, intersects: [1, 2, 3, 4, 5] },
            { from: "h1", fromPort: "T", to: "sys1", toPort: "B", text: "maps to", back: color, intersects: [1, 2, 3, 4, 5] },
            { from: "h2", fromPort: "T", to: "sys2", toPort: "B", text: "maps to", back: color, intersects: [1, 2, 3, 4, 5] },
            { from: "h1", fromPort: "R", to: "tran1", toPort: "L", text: "transformed by", back: transformColor, intersects: [1, 2, 3, 4, 5] },
            { from: "tran1", fromPort: "R", to: "h2", toPort: "L", text: "transforms into", back: transformColor, intersects: [6, 8, 13] }
        ];

        this.diagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private reOrderLayout() {
        this.diagram.layout.invalidateLayout();
        this.diagram.requestUpdate();
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

    //#region templates

    private createDiagram(): go.Diagram {
        let dg = this.g(go.Diagram, 'LineageDiagram', {
            initialContentAlignment: go.Spot.Center,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            //scrollMode: go.Diagram.DocumentScroll,
            //initialPosition: new go.Point(125, 125),
            layout: this.g(go.LayeredDigraphLayout, { layerSpacing: 50, setsPortSpots: false }), //direction: 270, 
            //layout: this.g(AssetBrowserLayout, {}),//layout: this.g(go.LayeredDigraphLayout, {direction: 0, columnSpacing: 50, layerSpacing: 50}),
            "undoManager.isEnabled": true,
            "commandHandler.archetypeGroupData": { isGroup: true, category: "Normal" },
        });

        let model = (dg.model as go.GraphLinksModel);

        //model.linkFromPortIdProperty = "fromPort";
        //model.linkToPortIdProperty = "toPort",
        model.nodeCategoryProperty = "template";
        model.nodeDataArray = [];
        model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = !this.readonly;
        dg.model.isReadOnly = this.readonly;

        return dg;
    }

    private createPortGroupNode(): go.Group {

        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                contextMenu: this.g(
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
                        { click: function (e, obj) { alert("Not yet implemented") } }
                    ),
                    this.g(
                        "ContextMenuButton",
                        this.g(go.TextBlock, { text: "Hide Upstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                        { click: function (e, obj) { alert("Not yet implemented") } }
                    ),
                    this.g(
                        "ContextMenuButton",
                        this.g(go.TextBlock, { text: "Hide Downstream", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                        { click: function (e, obj) { alert("Not yet implemented") } }
                    ),
                    this.g(
                        "ContextMenuButton",
                        this.g(go.TextBlock, { text: "Isolate", background: "transparent", alignment: go.Spot.Left, margin: 8, font: "12px sans-serif" }),
                        { click: function (e, obj) { alert("Not yet implemented") } }
                    )
                ),
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
            //this.g("Button", { alignment: go.Spot.Center }, this.g(go.TextBlock, "maps to (1)") ),
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
            this.g(
                go.Panel,
                "Horizontal",
                { stretch: go.GraphObject.Horizontal, padding: 5 },
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

    private createDefaultLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.AvoidsNodes,
                corner: 5,
                curve: go.Link.JumpGap,
                relinkableFrom: false,
                relinkableTo: false
            }, // the whole link panel
            new go.Binding("curve", "curve", go.Binding.parseEnum(go.Link, go.Link.JumpOver)),
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
