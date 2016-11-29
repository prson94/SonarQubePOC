import { Component, Input, OnInit, AfterViewInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { BaseComponent } from '../base.component';
import { PermissionsService, DiagramService } from '../../../services/index';
import { Permission } from '../../../models/permission.model';
import { ImpactDiagramModel, NodeModel, LinkModel, PredicateFilter } from '../../../models/impact.model';
import { MenuItem } from 'primeng/primeng';

import * as go from 'gojs';
import * as _ from 'lodash';

declare var window: any;


@Component({
    selector: 'd3s-impact',
    templateUrl: './impact.component.html',
    providers: [ PermissionsService, DiagramService ]
})

export class ImpactComponent extends BaseComponent implements OnInit, AfterViewInit {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() readonly: boolean = true;
    @ViewChild('diagram') diagramRef;

    private originalObject: string;
    private originalObjectID: number;
    private viewID: number = 1;
    private fullscreen = false;
    private initialLinks: go.Link[] = [];
    private initialNodes: go.Node[] = [];
    private newLink: go.Link = null;
    private overlayEditLinkKey = null;
    private selection = null;
    private model: ImpactDiagramModel;
    private selectedObject: string;
    private selectedObjectID: number;

    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;

    private zoomLevel: number = 50;
    private tab: string = 'filter';
    private headerText: string = 'Filter By Predicate';
    private isWindowVisible = false;
    private menuItems: MenuItem[] = [];

    private predicates: PredicateFilter[] = [];

    constructor(private myElement: ElementRef, protected permissionsService: PermissionsService, private diagramService: DiagramService) {
        super();
    }

    public ngOnInit() {
        this.originalObject = this.objectType;
        this.originalObjectID = this.objectID;

        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);

        this.menuItems.push({
            icon: 'fa-refresh menu-icon'
        });
        this.menuItems.push({
            icon: 'fa-info-circle menu-icon'
        });

        this.initializeDiagram();
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    private initializeDiagram() {
        this.myDiagram = this.createDiagram();

        this.myDiagram.nodeTemplateMap.add("NonFocal", this.createNonFocalNode());
        this.myDiagram.nodeTemplateMap.add("", this.createDefaultNode());
        this.myDiagram.linkTemplate = this.createLinkTemplate();

        this.myDiagram.addDiagramListener('ViewPortBoundsChanged', () => this.ViewPortBoundsChanged());
        this.myDiagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));

        this.myDiagram.grid.visible = false;
        this.myDiagram.grid.gridCellSize = new go.Size(8, 8);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;


        this.populateDiagram();

    }

    private populateDiagram() {
        this.isLoading = true;
        this.predicates = [];

        this.diagramService.getImpactDiagram(this.objectType, this.objectID)
            .then(data => {
                this.model = data;

                this.model.nodes.forEach(n => {
                    let isFocal = (n.obj == this.objectType && n.objid == this.objectID);

                    n.everExpanded = isFocal;
                    n.isTreeExpanded = isFocal;
                    n.template = isFocal ? "" : "NonFocal";

                    let predicate = this.predicates.find(p => p.id == n.predicateid);
                    if (predicate == null && n.predicateid != null)
                        this.predicates.push({
                            id: n.predicateid,
                            name: n.predicate,
                            selected: true
                        });

                });

                this.myDiagram.model = new go.GraphLinksModel(this.model.nodes, this.model.links);
                this.isLoading = false;
                console.log(data);
            });
    }

    private expandNode(node) {
        var diagram = node.diagram;
        diagram.startTransaction("CollapseExpandTree");
        var data = node.data;
        if (!data.everExpanded) {
            // only create children once per node
            diagram.model.setDataProperty(data, "everExpanded", true);

            this.diagramService.getImpactDiagram(data.obj, data.objid)
                .then(r => {
                    let hasChildren = false;
                    
                    r.nodes.forEach(n => {
                        if (!(n.obj == data.obj && n.objid == data.objid)) {
                            n.everExpanded = false;
                            n.template = 'NonFocal';

                            let allowAdd = true;

                            diagram.model.nodeDataArray.forEach(d => {
                                if (d.obj == n.obj && d.objid == n.objid) {
                                    allowAdd = false;
                                }
                            });

                            if (allowAdd) {
                                this.myDiagram.model.addNodeData(n);
                                hasChildren = true;
                            }
                        }
                    });

                    r.links.forEach(l => {
                        if (l.to == this.objectType + this.objectID.toString())
                            return;
                        hasChildren = true;
                        let links: go.GraphLinksModel = <go.GraphLinksModel>this.myDiagram.model;
                        links.addLinkData(l);
                    });

                    if (!hasChildren) {
                        node.findObject('TREEBUTTON').visible = false;
                    }

                });
        }
        if (node.isTreeExpanded) {
            diagram.commandHandler.collapseTree(node);
        } else {
            diagram.commandHandler.expandTree(node);
        }
        diagram.commitTransaction("CollapseExpandTree");
        this.myDiagram.zoomToFit();
    }

    private htmlDecode(s: string): string {
        s = s.replace(/&#39;/g, '\'');
        s = s.replace(/&amp;/g, '&')
        s = s.replace(/&lt;/g, '<')
        s = s.replace(/&gt;/g, '>')
        s = s.replace(/&#34;/g, '"');

        return s;
    }

    private menuAction(e: MenuItem) {
        if (e.icon == 'fa-refresh menu-icon') {
            this.refreshDiagram();
        } else if (e.icon == 'fa-info-circle menu-icon') {
            this.isWindowVisible = !this.isWindowVisible;
        } else if (e.icon == 'fa-sitemap menu-icon') {
            this.myDiagram.layout.invalidateLayout();
            this.myDiagram.layoutDiagram();
        }
    }

    private togglePredicate(p: PredicateFilter) {
        let id = (p == null) ? 0 : p.id;
        let visible = (p == null) ? true : p.selected;

        console.log(id, visible);
        this.myDiagram.startTransaction("togglePredicate");

        this.myDiagram.nodes.each(n => {
            if (n.data.predicateid == id || id == 0) {
                n.visible = visible;
            }
        });
        this.myDiagram.links.each(l => {
            if (l.data.predicateid == id || id == 0) {
                l.visible = visible;
            }
        });

        this.myDiagram.commitTransaction("togglePredicate");
    }

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

    private refreshDiagram() {
        this.objectType = this.originalObject;
        this.objectID = this.originalObjectID;
        this.populateDiagram();
    }

    private ViewPortBoundsChanged() {
        var s = this.myDiagram.scale;
        var h = 500;
        if (s > 1) {
            h = h * s;
        }
        this.zoomLevel = _.clamp(_.round(this.myDiagram.scale * 75), 0, 100);
    }

    private ChangedSelection(e: any) {
        let node = e.diagram.selection.first();
        let data = (node != null) ? node.data : null;

        if (data && data.obj && data.objid) {
            this.selectedObject = data.obj;
            this.selectedObjectID = data.objid;
        } else {
            this.selectedObject = null;
            this.selectedObjectID = null;
            this.selectTab('filter');
        }
    }

    private selectTab(val: string) {
        switch (val) {
            case 'info': this.headerText = 'Info'; break;
            case 'user': this.headerText = 'Responsibilities'; break;
            case 'fusion': this.headerText = 'Fusion Relationships'; break;
            case 'filter': this.headerText = 'Filter By Predicate'; break;
            default: this.headerText = ''; break;
        }
        this.tab = val;
    }
    //#endregion

    //#region templates

    private createDiagram(): go.Diagram {
        return this.g(go.Diagram,
            "ImpactDiagram",
            {
                initialAutoScale: go.Diagram.UniformToFill,  // an initial automatic zoom-to-fit
                contentAlignment: go.Spot.Center,  // align document to the center of the viewport
                layout: this.g(go.ForceDirectedLayout, { defaultSpringLength: 50, defaultElectricalCharge: 250, arrangementSpacing: new go.Size(250,250) }),
                "draggingTool.dragsTree": true, //drag subtree with node
            }
        );
    }


    private createNonFocalNode(): go.Node {
        let nodeWidth = 200;
        let nodeHeight = 125;
        let nodeFontSize = 12;

        return this.g(go.Node, "Spot",
            {
                selectionObjectName: "PANEL",
                isTreeExpanded: false,
                isTreeLeaf: false
            },
            this.g(go.Panel, "Auto", {
                name: "PANEL",
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: '#000',
                    strokeWidth: 2,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape",
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
                    )
                )
            ),
            // the expand/collapse button, at the top-right corner
            this.g("TreeExpanderButton",
                {
                    name: 'TREEBUTTON',
                    width: 20, height: 20,
                    alignment: go.Spot.TopRight,
                    alignmentFocus: go.Spot.Center,
                    // customize the expander behavior to
                    // create children if the node has never been expanded
                    click: (e, obj) => {  // OBJ is the Button
                        var node = obj.part;  // get the Node containing this Button
                        if (node === null) return;
                        e.handled = true;
                        this.expandNode(node);
                    }
                }
            )  // end TreeExpanderButton
        );
    }

    private createDefaultNode(): go.Node {
        let nodeWidth = 200;
        let nodeHeight = 125;
        let nodeFontSize = 12;

        return this.g(go.Node, "Spot",
            {
                selectionObjectName: "PANEL",
                isTreeExpanded: false,
                isTreeLeaf: false
            },
            this.g(go.Panel, "Auto", {
                name: "PANEL",
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: '#000',
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
                    )
                )
            ),
            // the expand/collapse button, at the top-right corner
            this.g("TreeExpanderButton",
                {
                    name: 'TREEBUTTON',
                    width: 20, height: 20,
                    alignment: go.Spot.TopRight,
                    alignmentFocus: go.Spot.Center,
                    // customize the expander behavior to
                    // create children if the node has never been expanded
                    click: (e, obj) => {  // OBJ is the Button
                        var node = obj.part;  // get the Node containing this Button
                        if (node === null) return;
                        e.handled = true;
                        this.expandNode(node);
                    }
                }
            )  // end TreeExpanderButton
        );
    }


    private createLinkTemplate(): go.Link {
        return this.g(go.Link,  // the whole link panel
            this.g(go.Shape,  // the link shape
                { stroke: "black" }),
            this.g(go.Shape,  // the arrowhead
                { toArrow: "standard", stroke: null }),
            this.g(go.Panel, "Auto",
                this.g(go.Shape,  // the label background, which becomes transparent around the edges
                    {
                        fill: this.g(go.Brush, "Radial", { 0: "rgb(240, 240, 240)", 0.3: "rgb(240, 240, 240)", 1: "rgba(240, 240, 240, 0)" }),
                        stroke: null
                    }),
                this.g(go.TextBlock,  // the label text
                    {
                        textAlign: "center",
                        font: "10pt helvetica, arial, sans-serif",
                        stroke: "#555555",
                        margin: 4
                    },
                    new go.Binding("text", "text"))
            )
        );
    }

    //#endregion
}
