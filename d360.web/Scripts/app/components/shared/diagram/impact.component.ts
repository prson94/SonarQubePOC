import * as go from 'gojs';
import * as _ from 'lodash';
import { AfterViewInit, Component, ElementRef, HostListener, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MenuItem } from 'primeng/api';

import { FilterType, ImpactDiagramModel, ImpactFilter, LinkModel, NodeModel } from '../../../models/impact.model';
import { PermissionsService } from '../../../services/permissions.service';
import { DiagramService } from '../../../services/diagram.service';
import { DiagramBaseComponent } from './diagram-base.component';

declare var window: any;

@Component({
    selector: 'd3s-impact',
    templateUrl: './impact.component.html',
    providers: [PermissionsService, DiagramService]
})

export class ImpactComponent extends DiagramBaseComponent implements OnInit, AfterViewInit, OnDestroy {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() readonly: boolean = true;
    @ViewChild('diagram', { static: false }) diagramRef;

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
    public selectedObject: string;
    public selectedObjectID: number;
    public selectedAssetID: number;

    private zoomLevel: number = 50;
    private tab: string = 'info';
    public headerText: string = 'Info';
    public isWindowVisible = false;
    public isFilterVisible = false;
    private allSelected = true;
    private noneSelected = false;
    public canApplyFilter = false;
    public menuItems: MenuItem[] = [];

    private showDetail = false;
    private overlayWidth = 700;
    private overlayMaxHeight = 700;

    public filters: ImpactFilter[] = [];
    FilterType = FilterType;

    constructor(
        private myElement: ElementRef,
        protected permissionsService: PermissionsService,
        private diagramService: DiagramService
    ) {
        super();
    }

    //#region angular
    public ngOnInit() {
        this.originalObject = this.objectType;
        this.originalObjectID = this.objectID;

        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);

        this.menuItems.push(
            {icon: 'fa fa-filter'},
            {icon: 'fa fa-search-minus'},
            {icon: 'fa fa-search-plus'},
            {icon: 'fa fa-refresh'},
            {icon: 'fa fa-info-circle'}
        );

        this.initializeDiagram();
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        this.diagram.div = null;
    }
    //#endregion

    private initializeDiagram() {
        this.diagram = this.createDiagram();

        this.diagram.nodeTemplateMap.add("", this.createDefaultNode());
        this.diagram.nodeTemplateMap.add("NonFocal", this.createNonFocalNode());
        this.diagram.nodeTemplateMap.add("Category", this.createCategoryNode());

        this.diagram.linkTemplateMap.add("", this.createLinkTemplate());
        this.diagram.linkTemplateMap.add("Category", this.createCategoryLinkTemplate());

        this.diagram.addDiagramListener('ViewportBoundsChanged', () => this.ViewportBoundsChanged());
        this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        this.diagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.diagram.addDiagramListener('InitialLayoutCompleted', () => this.InitialLayoutCompleted());

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        //the readonly property disallows dragging, so we need to manually disable everything else here instead to prevent keyboard shortcuts
        let dt = this.diagram.toolManager.diagram;
        dt.allowDelete = false;
        dt.allowClipboard = false;
        dt.allowCopy = false;
        dt.allowInsert = false;
        dt.allowLink = false;
        dt.allowRelink = false;
        dt.allowGroup = false;
        dt.allowTextEdit = false;

        this.populateDiagram();
    }

    private populateDiagram() {
        this.isLoading = true;
        this.filters = [];
        let focal: NodeModel = null;
        let nodes = [];

        this.diagramService.getImpactDiagram(this.objectType, this.objectID).subscribe(
            data => {
                this.model = data;

                if (this.model.nodes != null && this.model.nodes.length > 0) {
                    this.model.nodes.forEach(n => {
                        let isFocal = (n.obj == this.objectType && n.objid == this.objectID);
                        if (isFocal) focal = n;

                        n.everExpanded = isFocal;
                        n.isTreeExpanded = isFocal;
                        n.category = isFocal ? "" : "NonFocal";

                        let predicate = this.filters.find(p => p.type == FilterType.Predicate && p.key == (n.predicateid || '').toString());
                        if (predicate == null && n.predicateid != null) {
                            this.filters.push({
                                key: n.predicateid.toString(),
                                name: n.predicateLabel,
                                type: FilterType.Predicate,
                                selected: true
                            });
                        }
                    });
                }

                if (this.model.links != null && this.model.links.length > 0) {
                    this.model.links.forEach(l => {
                        l.isTreeLink = true;
                    });

                    this.aggregatePredicates(this.model.nodes, this.model.links);
                    this.addCategoryLayer(focal, this.model.nodes, this.model.links, false);
                }

                //remove duplicates
                this.model.nodes.forEach(n => {
                    if (nodes.findIndex(i => i.key == n.key) == -1) {
                        nodes.push(n);
                    }
                });

                this.diagram.model = new go.GraphLinksModel(nodes, this.model.links);

                if (this.model.nodes != null && this.model.nodes.length == 1) {
                    //there are no relationships, hide the expand/collapse
                    this.diagram.nodes.first().findObject('TREEBUTTON').visible = false;
                }

                this.diagram.nodes.each(n => {
                    if (n.data.isLeaf)
                        n.findObject('TREEBUTTON').visible = false;
                });

                this.isLoading = false;
            });
    }

    private toggleDetail() {
        this.showDetail = !this.showDetail;

        if (this.showDetail) {
            this.overlayWidth = 1000;
            this.overlayMaxHeight = 700;
        } else {
            this.overlayWidth = 500;
            this.overlayMaxHeight = 700;
        }
    }

    private refreshFilters() {
        this.diagram.nodes.each(n => {

            if (n.data.category == 'Category') {
                return;
            }

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

            existing = this.filters.findIndex(f => f.type == FilterType.Predicate && f.key == (n.data.predicateid || '').toString());
            if (existing == -1 && n.data.predicateid != null) {
                this.filters.push({
                    key: n.data.predicateid.toString(),
                    name: n.data.predicateLabel,
                    type: FilterType.Predicate,
                    selected: true
                });
            }
        });
    }

    private addCategoryLayer(root: NodeModel, nodes: NodeModel[], links: LinkModel[], append: boolean = true) {
        if (root == null || root.key == null) {
            return;
        }

        let categories: any[] = [];
        let diagramModel: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;

        if (links == null) links = [];
        if (nodes == null) nodes = [];

        nodes.forEach(n => {
            if (n.key == root.key) {
                return;
            }

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
            link.from = root.key;
            link.to = node.key;
            link.category = 'Category';
            link.isTreeLink = true;

            nodes.filter(n => n.typeId == c.id && n.type == c.type).forEach(n => {
                if (n.key == root.key) {
                    return;
                }

                let i = nodes.findIndex(i => i.key == n.key);
                let clink: LinkModel = null;

                if (append) {
                    clink = links.find(<LinkModel>(l) => l.from == root.key && l.to == n.key);
                } else {
                    clink = this.model.links.find(l => l.from == root.key && l.to == n.key);
                }

                if (clink) {
                    clink.from = node.key;
                }
            });

            this.model.nodes.push(node);
            this.model.links.push(link);

            if (append) {
                this.diagram.startTransaction("addCategoryLayer");
                diagramModel.addNodeData(node);
                diagramModel.addLinkData(link);
                this.diagram.commitTransaction("addCategoryLayer");
            }


        });

        if (append) {
            nodes.forEach(n => {
                this.model.nodes.push(n);
                diagramModel.addNodeData(n);
            });

            links.forEach(l => {
                this.model.links.push(l);
                diagramModel.addLinkData(l);
            });
        }

        this.diagram.nodes.each(n => {
            if (n.data.isLeaf) {
                n.findObject('TREEBUTTON').visible = false;
            }
        });

        this.diagram.links.each(l => {
            let k = this.model.links.find(i => i.to == l.data.to && i.from == l.data.from);

            if (k) {
                l.isTreeLink = k.isTreeLink;
            }
        });

        this.refreshFilters();
    }

    private expandNode(node) {
        var diagram = node.diagram;
        var data = node.data;

        diagram.startTransaction("CollapseExpandTree");

        let nodes = [];
        let links = [];

        this.isLoading = false;

        if (node.isTreeExpanded) {
            diagram.commandHandler.collapseTree(node);

            //need to hide/show non-tree links manually here to workaround issue with child nodes having multiple parents
            this.diagram.links.each(l => {
                if (l.data.from == node.data.key && !l.isTreeLink) {
                    l.visible = false;
                }
            });
        } else {
            diagram.commandHandler.expandTree(node);
            this.diagram.links.each(l => {
                if (l.data.from == node.data.key && !l.isTreeLink) {
                    l.visible = true;
                }
            });
        }

        diagram.commitTransaction("CollapseExpandTree");
        this.refreshFilters();
        this.zoomToFit();

        if (!data.everExpanded) {
            this.isLoading = true;

            // only create children once per node
            diagram.model.setDataProperty(data, "everExpanded", true);

            this.diagramService.getImpactDiagram(data.obj, data.objid).subscribe(
                r => {
                    let hasChildren = false;

                    if (r && r.nodes) {
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

                                nodes.forEach(d => {
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
                    }

                    if (r && r.links) {
                        r.links.forEach(l => {
                            let addLink = true;

                            l.isTreeLink = true;

                            if (l.to == this.objectType + this.objectID.toString()) {
                                addLink = false;
                            }

                            //prevent duplicate links of the same predicate between the same nodes
                            if (addLink) {
                                this.diagram.links.each(k => {
                                    if ((k.data.to == l.to && k.data.from == l.from) || (k.data.to == l.from && k.data.from == l.to)) {
                                        if (k.data.predicateid == l.predicateid) {
                                            addLink = false;

                                            return;
                                        }
                                    }
                                });
                            }

                            //if there's already a link to this node, add the link as a non-tree link to avoid breaking collapse/expand
                            let to = this.diagram.findNodeForKey(l.to);

                            if (to) {
                                l.isTreeLink = false;
                            }

                            let diagramModel: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;

                            if (addLink) {
                                hasChildren = true;
                                links.push(l);
                            }
                        });
                    }

                    //if there are no children, hide the expand/collapse button
                    if (!hasChildren) {
                        node.findObject('TREEBUTTON').visible = false;
                    } else {
                        this.addCategoryLayer(node.data, nodes, links);
                    }
                }
            )
        }
    }

    public menuAction(e: MenuItem) {
        if (e.icon == 'fa fa-refresh') {
            this.refreshDiagram();
        } else if (e.icon == 'fa fa-info-circle') {
            this.isWindowVisible = !this.isWindowVisible;
            this.isFilterVisible = false;
        } else if (e.icon == 'fa fa-search-plus') {
            this.diagram.scale += .1;

            if (this.diagram.scale > 2.5) {
                this.diagram.scale = 2.5;
            }
        } else if (e.icon == 'fa fa-search-minus') {
            this.diagram.scale -= .1;

            if (this.diagram.scale < .1) {
                this.diagram.scale = .1;
            }
        } else if (e.icon == 'fa fa-filter') {
            this.isFilterVisible = !this.isFilterVisible;
            this.isWindowVisible = false;
        }
    }

    public selectAll() {
        this.canApplyFilter = true;

        this.filters.forEach(f => {
            f.selected = true;
        });
    }

    public selectNone() {
        this.canApplyFilter = true;

        this.filters.forEach(f => {
            f.selected = false;
        });
    }

    private checkFilter() {
        this.canApplyFilter = true;

        if (this.filters.filter(f => !f.selected).length == 0) {
            this.allSelected = true;
        }

        if (this.filters.filter(f => f.selected).length == 0) {
            this.noneSelected = true;
        }
    }

    public filterView() {
        this.canApplyFilter = false;
        this.diagram.startTransaction("filterView");

        this.diagram.nodes.each(n => {
            let visible = true;

            if (n.category == '') {
                /* skip focal node */
                return;
            }
            this.filters.forEach(f => {
                switch (f.type) {
                    case FilterType.Category:
                        if ((n.data.type + '|' + n.data.typeId) == f.key && !f.selected) {
                            visible = false;
                        }
                        break;
                    case FilterType.Predicate:
                        if ((n.data.predicateid || '').toString() == f.key && !f.selected) {
                            visible = false;
                        }
                        break;
                }
            });

            n.visible = visible;

            if (!n.visible && n.isTreeExpanded) {
                this.diagram.commandHandler.collapseTree(n);
            }
        });

        this.diagram.links.each(l => {
            let visible = true;

            this.filters.forEach(f => {
                switch (f.type) {
                    case FilterType.Category:
                        let from = this.diagram.findNodeForKey(l.data.from);
                        let to = this.diagram.findNodeForKey(l.data.to);

                        if (from == null || from.category == '' || to == null || to.category == '') {
                            return;
                        }

                        if ((from.data.type + '|' + from.data.typeId) == f.key && !f.selected) {
                            visible = false;
                        }

                        if (visible && (to.data.type + '|' + to.data.typeId) == f.key && !f.selected) {
                            visible = false;
                        }
                        break;
                    case FilterType.Predicate:
                        if ((l.data.predicateid || '').toString() == f.key && !f.selected) {
                            visible = false;
                        }
                        break;
                }
            });

            l.visible = visible;
        });

        this.calculateCategoryNumbers();
        this.diagram.commitTransaction("filterView");
        this.zoomToFit();
    }

    private calculateCategoryNumbers() {
        let diagramModel: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        this.diagram.startTransaction("calculateCategoryNumbers");
        this.diagram.nodes.each(n => {
            if (n.category != 'Category') {
                return;
            }

            let children = [];
            let name = '';

            this.diagram.links.each(l => {
                if (l.isTreeLink && l.data.from == n.data.key) {
                    let node = this.diagram.findNodeForKey(l.data.to);

                    if (node && node.visible) {
                        if (children.length == 0) {
                            name = node.data.typeName;
                        } else {
                            name = node.data.typeNamePlural;
                        }

                        children.push(node);
                    }
                }
            });

            diagramModel.setDataProperty(n.data, "childCount", children.length);
            diagramModel.setDataProperty(n.data, "name", n.data.childCount + ' ' + name);

            n.visible = n.data.childCount != 0;
        });

        this.diagram.commitTransaction("calculateCategoryNumbers");
    }

    private aggregatePredicates(nodes: NodeModel[], links: LinkModel[]) {
        links.forEach(l => {
            links.forEach(k => {
                if (k.to == l.to && k.from == l.from && k.intersectid != l.intersectid) {
                    let i = this.model.links.findIndex(j => j.intersectid == k.intersectid);

                    l.text = l.text + ', ' + k.text;
                    this.model.links.splice(i, 1);
                }
            });
        });

        nodes.forEach(n => {
            nodes.forEach(m => {
                if (n.obj == m.obj && n.objid == m.objid && n.intersectid != m.intersectid) {
                    let i = this.model.nodes.findIndex(j => j.key == m.key);

                    this.model.nodes.splice(i, 1);
                }
            });
        });
    }

    private zoomToFit() {
        if (this.diagram.animationManager.isAnimating) {
            this.diagram.animationManager.stopAnimation();
        }

        this.diagram.zoomToFit();
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

    private ViewportBoundsChanged() {
        /* FIXME: what this code do? */
        var s = this.diagram.scale;
        var h = 500;

        if (s > 1) {
            h = h * s;
        }
        /* ./FIXME */

        this.zoomLevel = _.clamp(_.round(this.diagram.scale * 75), 0, 100);
    }

    private ChangedSelection(e: any) {
        let node = e.diagram.selection.first();
        let data = (node != null) ? node.data : null;

        if (data && data.obj && data.objid) {
            this.selectedObject = data.obj;
            this.selectedObjectID = data.objid;
            this.selectedAssetID = data.assetId;
        } else {
            this.selectedObject = null;
            this.selectedObjectID = null;
            this.selectedAssetID = null;
            this.selectTab('info');
        }
    }

    private ObjectDoubleClicked(e: any) {
        if (e.diagram == null || e.diagram.selection == null || e.diagram.selection.first() == null) {
            return;
        }

        var obj = e.diagram.selection.first().data;
        if (obj != null) {
            if (obj.key != null) {
                let node = this.diagram.findNodeForKey(obj.key);

                if (node && node.findObject('TREEBUTTON').visible) {
                    this.expandNode(node);
                }
            }
        }
    }

    private selectTab(val: string) {
        switch (val) {
            case 'info':
                this.headerText = 'Info';
                break;
            case 'user':
                this.headerText = 'Responsibilities';
                break;
            case 'fusion':
                this.headerText = 'Fusion Relationships';
                break;
            case 'filter':
                this.headerText = 'Filter';
                break;
            default:
                this.headerText = '';
                break;
        }
        this.tab = val;
    }

    private InitialLayoutCompleted() {
        this.zoomToFit();
        this.refreshFilters();
    }

    private SelectionMoved() {
        this.zoomToFit();
    }

    //#endregion

    //#region templates

    private createDiagram(): go.Diagram {
        return this.g(go.Diagram,
            "ImpactDiagram",
            {
                initialAutoScale: go.Diagram.UniformToFill,  // an initial automatic zoom-to-fit
                contentAlignment: go.Spot.Center,  // align document to the center of the viewport
                layout: this.g(go.ForceDirectedLayout, {
                    defaultSpringLength: 50,
                    defaultElectricalCharge: 250,
                    arrangementSpacing: new go.Size(250, 250)
                }),
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

                        if (node === null) {
                            return;
                        }

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

                        if (node === null) {
                            return;
                        }

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
                        
                        if (node === null) {
                            return;
                        }

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
                {stroke: "black"}),
            this.g(go.Shape,  // the arrowhead
                {toArrow: "standard", stroke: null}),
            this.g(go.Panel, "Auto",
                this.g(go.Shape,  // the label background, which becomes transparent around the edges
                    {
                        fill: this.g(go.Brush, "Radial", {
                            0: "rgb(240, 240, 240)",
                            0.3: "rgb(240, 240, 240)",
                            1: "rgba(240, 240, 240, 0)"
                        }),
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
                {stroke: "black"}),
            this.g(go.Shape,
                {toArrow: "standard", stroke: null})
        );
    }

    //#endregion
}
