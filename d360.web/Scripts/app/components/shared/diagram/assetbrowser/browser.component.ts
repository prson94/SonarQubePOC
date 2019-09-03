import * as go from 'gojs';
import * as _ from 'lodash';
import {AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild} from '@angular/core';
import {DiagramObjectType } from '../../../../models/lineage.model';
import {PermissionsService} from '../../../../services/permissions.service';
import {DiagramService} from '../../../../services/diagram.service';
import {DiagramBaseComponent} from '../diagram-base.component';

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

    //control properties
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

        this.diagram.groupTemplateMap.add("Group", this.createGroupNode());
        this.diagram.groupTemplateMap.add("Normal", this.createNormalNode());
        //this.diagram.nodeTemplateMap.add("ListItem", this.createListItemNode());

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

        dm.nodeDataArray = [
            { key: "h1", isGroup: true, text: "DWH", template: "Group", back: color },
            { key: "h2", isGroup: true, text: "EGL", template: "Group", back: color },
            { key: "s1", isGroup: true, group: "h1", text: "dbo", template: "Group", back: this.shadeColor(color, 15) },
            { key: "s2", isGroup: true, group: "h2", text: "e", template: "Group", back: this.shadeColor(color, 15) },
            { key: "t1", isGroup: true, group: "s1", text: "Fact_Security", template: "Normal", back: this.shadeColor(color, 30) },
            { key: "t2", isGroup: true, group: "s2", text: "T_SECURITY_MASTER", template: "Normal", back: this.shadeColor(color, 30) },
            { key: "c1_1", group: "t1", text: "Identifier", back: this.shadeColor(color, 45) },
            { key: "c1_2", group: "t1", text: "Cusip", back: this.shadeColor(color, 45) },
            { key: "c2_1", group: "t2", text: "SEC_Identifier", back: this.shadeColor(color, 45) },
            { key: "c2_2", group: "t2", text: "Cusip_Reg", back: this.shadeColor(color, 45) },
        ];

        dm.linkDataArray = [
            { from: "h1", to: "h2" }
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
            initialContentAlignment: go.Spot.Left,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            scrollMode: go.Diagram.DocumentScroll,
            initialPosition: new go.Point(125, 125),
            layout: this.g(go.LayeredDigraphLayout, {direction: 0, columnSpacing: 50, layerSpacing: 50}),
            "undoManager.isEnabled": true,
            "commandHandler.archetypeGroupData": { isGroup: true, category: "Normal" },
        });

        let model = (dg.model as go.GraphLinksModel);

        model.nodeCategoryProperty = "template";
        model.nodeDataArray = [];
        model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;
        dg.toolManager.linkingTool.isEnabled = !this.readonly;
        dg.model.isReadOnly = this.readonly;

        return dg;
    }

    private createGroupNode(): go.Group {

        return this.g(go.Group, "Auto",
            {
                background: "transparent",
                computesBoundsAfterDrag: true,
                handlesDragDropForMembers: true,
                layout:
                    this.g(
                        go.GridLayout,
                        {
                            wrappingWidth: Infinity, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4)
                        }
                    )
            },
            //this.g(
            //    "Button",
            //    { alignment: go.Spot.Center },
            //    this.g(go.TextBlock, "maps to (1)")
            //),
            this.g(
                go.Shape,
                "Rectangle",
                { fill: null, strokeWidth: 2 },
                new go.Binding("stroke", "back"),
            ),
            this.g(go.Panel, "Vertical",  // title above Placeholder
                this.g(go.Panel, "Horizontal",  // button next to TextBlock
                    { stretch: go.GraphObject.Horizontal },
                    new go.Binding("background", "back"),
                    this.g(
                        "SubGraphExpanderButton",
                        { alignment: go.Spot.Right, margin: 5 }
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
                this.g(go.Placeholder, { padding: 5, alignment: go.Spot.TopLeft })
            )  // end Vertical Panel
        );
    }

    private createNormalNode(): go.Group {
        return this.g(
            go.Group,
            "Auto",
            {
                background: "transparent",
                ungroupable: true,
                computesBoundsAfterDrag: true,
                // when the selection is dropped into a Group, add the selected Parts into that Group;
                // if it fails, cancel the tool, rolling back any changes
                handlesDragDropForMembers: true,  // don't need to define handlers on member Nodes and Links
                // Groups containing Nodes lay out their members vertically
                layout:
                    this.g(go.GridLayout,
                        {
                            wrappingColumn: 1, alignment: go.GridLayout.Position,
                            cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4)
                        })
            },
            new go.Binding("background", "isHighlighted", function (h) { return h ? "rgba(255,0,0,0.2)" : "transparent"; }).ofObject(),
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
                    "Horizontal",  // button next to TextBlock
                    { stretch: go.GraphObject.Horizontal },
                    new go.Binding("background", "back"),
                    this.g(
                        "SubGraphExpanderButton",
                        { alignment: go.Spot.Right, margin: 5 }
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
                    go.Placeholder,
                    { padding: 5, alignment: go.Spot.TopLeft }
                )
            )  // end Vertical Panel
        );
    }

    private createListItemNode(): go.Node {
        return this.g(go.Node, "Auto",
            this.g(
                go.Shape,
                "Rectangle",
                { stroke: null, fill: "transparent" }//,
                //new go.Binding("fill", "back")
            ),
            this.g(
                go.TextBlock,
                {
                    margin: 5,
                    editable: true,
                    font: "bold 12px sans-serif",
                    opacity: 0.75,
                    stroke: "#404040"
                },
                new go.Binding("text", "text").makeTwoWay()
            )
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
                new go.Binding("strokeWidth", "hasProperties", function (h) {
                    return h ? 3 : 2;
                }),
                new go.Binding("stroke", "hasProperties", function (h) {
                    return h ? "black" : "gray"
                })), // the link shape
            this.g(go.Shape, {toArrow: "standard", fill: "gray", stroke: "gray"}), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(go.Shape, {
                        visible: false,
                        fill: this.g(go.Brush, "Radial", {
                            0: "rgb(255, 255, 255)",
                            0.3: "rgb(255, 255, 255)",
                            1: "rgba(255, 255, 255, 0)"
                        }),
                        stroke: '#999',
                        strokeDashArray: [3, 2]
                    },
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
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, {fill: "lightyellow"}), this.g(go.Panel, "Vertical", this.g(go.TextBlock, {
                        margin: 3,
                        text: tooltip
                    })))
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
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, {fill: "lightyellow"}), this.g(go.Panel, "Vertical", this.g(go.TextBlock, {
                        margin: 3,
                        text: tooltip
                    })))
                },
                new go.Binding("stroke", "back")
            ),
            new go.Binding("visible", binding)
        );

        return iconPanel;
    }

    //#endregion
}
