import { Component, Input, OnInit, AfterViewInit, ElementRef, ViewChild, HostListener, OnDestroy } from '@angular/core';
import { BaseComponent } from '../base.component';
import { PermissionsService, DiagramService } from '../../../services/index';
import { Permission } from '../../../models/permission.model';
import { ImpactDiagramModel, NodeModel, LinkModel, ImpactFilter, FilterType} from '../../../models/impact.model';
import { MenuItem } from 'primeng/primeng';

import * as go from 'gojs';
import * as _ from 'lodash';

declare var window: any;


@Component({
    selector: 'd3s-impact',
    templateUrl: './impact.component.html',
    providers: [ PermissionsService, DiagramService ]
})

export class ImpactComponent extends BaseComponent implements OnInit, AfterViewInit, OnDestroy {
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
    private headerText: string = 'Filter';
    private isWindowVisible = false;
    private menuItems: MenuItem[] = [];
    private showRelationsTable = false;

    private filters: ImpactFilter[] = [];
    FilterType = FilterType;

    constructor(private myElement: ElementRef, protected permissionsService: PermissionsService, private diagramService: DiagramService) {
        super();
    }

    //#region angular

    public ngOnInit() {
        this.originalObject = this.objectType;
        this.originalObjectID = this.objectID;

        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);

        this.menuItems.push({
            icon: 'fa-refresh menu-icon'
        });
        //this.menuItems.push({
        //    icon: 'fa-table menu-icon'
        //});
        this.menuItems.push({
            icon: 'fa-info-circle menu-icon'
        });

        this.initializeDiagram();
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        this.myDiagram.div = null;
    }

    //#endregion

    private initializeDiagram() {
        this.myDiagram = this.createDiagram();

        this.myDiagram.nodeTemplateMap.add("", this.createDefaultNode());
        this.myDiagram.nodeTemplateMap.add("NonFocal", this.createNonFocalNode());
        this.myDiagram.nodeTemplateMap.add("Category", this.createCategoryNode());

        this.myDiagram.linkTemplateMap.add("", this.createLinkTemplate());
        this.myDiagram.linkTemplateMap.add("Category", this.createCategoryLinkTemplate());

        this.myDiagram.addDiagramListener('ViewPortBoundsChanged', () => this.ViewPortBoundsChanged());
        this.myDiagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        this.myDiagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.myDiagram.addDiagramListener('InitialLayoutCompleted', () => this.InitialLayoutCompleted());

        this.myDiagram.grid.visible = false;
        this.myDiagram.grid.gridCellSize = new go.Size(8, 8);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;


        this.populateDiagram();

    }

    private populateDiagram() {
        this.isLoading = true;
        this.filters = [];
        let focal: NodeModel = null;

        this.diagramService.getImpactDiagram(this.objectType, this.objectID)
            .then(data => {
                this.model = data;

                this.model.nodes.forEach(n => {
                    let isFocal = (n.obj == this.objectType && n.objid == this.objectID);
                    if (isFocal) focal = n;

                    n.everExpanded = isFocal;
                    n.isTreeExpanded = isFocal;
                    n.category = isFocal ? "" : "NonFocal";

                    let predicate = this.filters.find(p => p.type == FilterType.Predicate && p.key == (n.predicateid || '').toString());
                    if (predicate == null && n.predicateid != null)
                        this.filters.push({
                            key: n.predicateid.toString(),
                            name: n.predicate,
                            type: FilterType.Predicate,
                            selected: true
                        });

                });

                this.addCategoryLayer(focal, this.model.nodes, this.model.links, false);

                this.myDiagram.model = new go.GraphLinksModel(this.model.nodes, this.model.links);
                this.isLoading = false;
            });
    }

    private refreshFilters() {
        this.myDiagram.nodes.each(n => {

            if (n.data.category == 'Category')
                return;

            let typeKey = n.data.type + '|' + n.data.typeId;
            let existing = this.filters.findIndex(f => f.type == FilterType.Category && f.key == typeKey);

            if (existing == -1 && n.data.type != null && n.data.typeId != null) {
                this.filters.push({
                    key: typeKey,
                    name: n.data.typeName,
                    type: FilterType.Category,
                    selected: true
                });
            }

            existing = this.filters.findIndex(f => f.type == FilterType.Predicate && f.key == (n.data.predicateid ||'').toString());
            if (existing == -1 && n.data.predicateid != null) {
                this.filters.push({
                    key: n.data.predicateid.toString(),
                    name: n.data.predicate,
                    type: FilterType.Predicate,
                    selected: true
                });
            }
            
        });
    }

    private addCategoryLayer(root: NodeModel, nodes: NodeModel[], links: LinkModel[], append: boolean = true) {
        let categories: any[] = [];
        let diagramModel: go.GraphLinksModel = <go.GraphLinksModel>this.myDiagram.model;

        if (links == null) links = [];
        if (nodes == null) nodes = [];

        nodes.forEach(n => {
            if (n.key == root.key)
                return;

            let cat = categories.find(c => c.id == n.typeId && c.type == n.type);
            if (cat == null && n.typeId != null) {
                categories.push({
                    id: n.typeId,
                    type: n.type,
                    name: n.typeName,
                    fore: n.fore,
                    back: n.back,
                    count: 1
                });
            } else {
                cat.name = n.typeNamePlural;
                cat.count++;
            }
        });

        categories.forEach(c => {

            let node = new NodeModel();
            node.key = root.key + '|' + c.type + c.id;
            node.category = 'Category';
            node.name = c.count + ' ' + c.name;
            node.fore = c.fore;
            node.back = c.back;
            node.everExpanded = true;
            node.childCount = c.count;

            let link = new LinkModel();
            link.from = root.key
            link.to = node.key;
            link.category = 'Category';

            nodes.filter(n => n.typeId == c.id && n.type == c.type).forEach(n => {
                if (n.key == root.key)
                    return;
                let i = nodes.findIndex(i => i.key == n.key);
                let clink: LinkModel = null;
                if (append)
                    clink = links.find(<LinkModel>(l) => l.from == root.key && l.to == n.key);
                else
                    clink = this.model.links.find(l => l.from == root.key && l.to == n.key);

                if (clink) {
                    clink.from = node.key;
                }
            });

            this.model.nodes.push(node);
            this.model.links.push(link);

            if (append) {
                this.myDiagram.startTransaction("addCategoryLayer");
                diagramModel.addNodeData(node);
                diagramModel.addLinkData(link);
                this.myDiagram.commitTransaction("addCategoryLayer");
            }


        });
        if (append) {
            nodes.forEach(n => { this.model.nodes.push(n); diagramModel.addNodeData(n); });
            links.forEach(l => { this.model.links.push(l); diagramModel.addLinkData(l); });
        }
        this.refreshFilters();

    }

    private expandNode(node) {
        var diagram = node.diagram;
        diagram.startTransaction("CollapseExpandTree");
        var data = node.data;

        let nodes = [];
        let links = [];

        if (!data.everExpanded) {
            // only create children once per node
            diagram.model.setDataProperty(data, "everExpanded", true);

            this.diagramService.getImpactDiagram(data.obj, data.objid)
                .then(r => {
                    let hasChildren = false;

                    r.nodes.forEach(n => {
                        if (!(n.obj == data.obj && n.objid == data.objid)) {
                            n.everExpanded = false;
                            n.category = 'NonFocal';

                            let allowAdd = true;

                            diagram.model.nodeDataArray.forEach(d => {
                                if (d.obj == n.obj && d.objid == n.objid) {
                                    allowAdd = false;
                                }
                            });

                            if (allowAdd) {
                                nodes.push(n);
                                hasChildren = true;
                            }
                        }
                    });

                    r.links.forEach(l => {
                        let addLink = true;
                        if (l.to == this.objectType + this.objectID.toString())
                            addLink = false;

                        //prevent duplicate links of the same predicate between the same nodes
                        if (addLink)
                            this.myDiagram.links.each(k => {
                                if ((k.data.to == l.to && k.data.from == l.from) || (k.data.to == l.from && k.data.from == l.to)) {
                                    if (k.data.predicateid == l.predicateid) {
                                        addLink = false;
                                        return;
                                    }
                                }
                            });

                        let diagramModel: go.GraphLinksModel = <go.GraphLinksModel>this.myDiagram.model;
                        if (addLink) {
                            hasChildren = true;
                            links.push(l);
                        }
                    });

                    //if there are no children, hide the expand/collapse button
                    if (!hasChildren) {
                        node.findObject('TREEBUTTON').visible = false;
                    } else {
                        this.addCategoryLayer(node.data, nodes, links);
                        this.filterView();
                    }

                })
                .then(() => {
                    if (node.isTreeExpanded) {
                        diagram.commandHandler.collapseTree(node);
                    } else {
                        diagram.commandHandler.expandTree(node);
                    }
                    diagram.commitTransaction("CollapseExpandTree");
                    this.refreshFilters();
                    this.myDiagram.zoomToFit();
                });
        } else {
            if (node.isTreeExpanded) {
                diagram.commandHandler.collapseTree(node);
            } else {
                diagram.commandHandler.expandTree(node);
            }
            diagram.commitTransaction("CollapseExpandTree");
            this.refreshFilters();
            this.myDiagram.zoomToFit();
        }
    }

    private menuAction(e: MenuItem) {
        if (e.icon == 'fa-refresh menu-icon') {
            this.refreshDiagram();
        } else if (e.icon == 'fa-info-circle menu-icon') {
            this.isWindowVisible = !this.isWindowVisible;
        } else if (e.icon == 'fa-table menu-icon') {
            this.showRelationsTable = !this.showRelationsTable;
        }
    }

    private filterView() {
        this.myDiagram.startTransaction("filterView");

        this.myDiagram.nodes.each(n => {
            let visible = true;

            if (n.category == '') //skip focal node
                return;
            this.filters.forEach(f => {

                switch (f.type) {
                    case FilterType.Category:
                        if ((n.data.type + '|' + n.data.typeId) == f.key && !f.selected)
                            visible = false;
                        break;
                    case FilterType.Predicate:
                        if ((n.data.predicateid || '').toString() == f.key && !f.selected)
                            visible = false;
                        break;
                }
            });

            n.visible = visible;
            if (!n.visible && n.isTreeExpanded)
                this.myDiagram.commandHandler.collapseTree(n);
        });

        this.myDiagram.links.each(l => {
            let visible = true;

            this.filters.forEach(f => {
                switch (f.type) {
                    case FilterType.Category:
                        break;
                    case FilterType.Predicate:
                        if ((l.data.predicateid || '').toString() == f.key && !f.selected)
                            visible = false;
                        break;
                }
            });
            l.visible = visible;
        });

        this.calculateCategoryNumbers();
        this.myDiagram.commitTransaction("filterView");
        this.myDiagram.zoomToFit();
    }

    private calculateCategoryNumbers() {
        let diagramModel: go.GraphLinksModel = <go.GraphLinksModel>this.myDiagram.model;
        this.myDiagram.startTransaction("calculateCategoryNumbers");
        this.myDiagram.nodes.each(n => {
            if (n.data.category != 'Category')
                return;

            
            let children = [];
            let name = '';

            //get nodes connected to this category
            diagramModel.linkDataArray.filter((l: LinkModel) => l.from == n.data.key).forEach((l: LinkModel) => {

                let node = this.myDiagram.findNodeForKey(l.to);
                if (node && node.visible) {
                    if (children.length == 0)
                        name = node.data.typeName;
                    else
                        name = node.data.typeNamePlural;
                    children.push(node);
                }
            });

            diagramModel.setDataProperty(n.data, "childCount", children.length);
            diagramModel.setDataProperty(n.data, "name", n.data.childCount + ' ' + name);


            if (n.data.childCount == 0) {
                n.visible = false;
            } else {
                n.visible = true;
            }
        });
        this.myDiagram.commitTransaction("calculateCategoryNumbers");
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

    private ObjectDoubleClicked(e: any) {
        var obj = e.diagram.selection.first().data;
        if (obj != null) {
            if (obj.key != null) {
                let node = this.myDiagram.findNodeForKey(obj.key);
                this.expandNode(node);
            }
        }
    }

    private selectTab(val: string) {
        switch (val) {
            case 'info': this.headerText = 'Info'; break;
            case 'user': this.headerText = 'Responsibilities'; break;
            case 'fusion': this.headerText = 'Fusion Relationships'; break;
            case 'filter': this.headerText = 'Filter'; break;
            case 'relations': this.headerText = 'Relationships'; break;
            default: this.headerText = ''; break;
        }
        this.tab = val;
    }

    private InitialLayoutCompleted() {
        console.log('initial layout complete');
        this.myDiagram.zoomToFit();
        this.refreshFilters();
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
                isTreeExpanded: true,
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

    private createCategoryNode(): go.Node {
        let nodeWidth = 200;
        let nodeHeight = 85;
        let nodeFontSize = 13;

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

    private createCategoryLinkTemplate(): go.Link {
        return this.g(go.Link,
            this.g(go.Shape,
                { stroke: "black" }),
            this.g(go.Shape,
                { toArrow: "standard", stroke: null })
        );
    }

    //#endregion
}
