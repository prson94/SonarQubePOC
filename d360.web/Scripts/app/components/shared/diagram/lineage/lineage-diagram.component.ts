import { Component, Input, OnInit, AfterViewInit, ElementRef, OnDestroy, ViewChild, Renderer, HostListener, SimpleChanges } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { PermissionsService } from '../../../../services/permissions.service';
import { DiagramService } from '../../../../services/diagram.service';
import { LineageService } from '../../../../services/lineage.service';
import { MessagesService } from '../../../../services/messages.service';
//import { JsonResult } from '
import {
    DiagramObjectType,
    LinkModelV2,
    NodeModelV2,
    MapItem,
    Responsibility,
    TechnicalRelation,
    LineageView,
    LineageEditorModelV2,
    LineageNodeModel,
    LineageLinkModel,
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

export class LineageDiagramComponent extends DiagramBaseComponent implements OnInit, AfterViewInit {
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
    private isWindowVisible = false;
    private showNodeTabs = false;
    private showLinkTabs = false;
    private showEditTab = false;
    private showInfoTab = false;
    private menuItems: MenuItem[] = [];
    private editorMenuItems: MenuItem[] = [];
    private tab: string = 'info';
    private headerText = '';
    private diagramOffset = 291;
    private overlayOffset = 391;
    private overlayMaxHeight = 500;
    private hasHeader = false;

    //diagram properties
    //private g = go.GraphObject.make;
    //private palette: go.Palette;

    constructor(
        private myElement: ElementRef,
        protected permissionsService: PermissionsService,
        private diagramService: DiagramService,
        private lineageService: LineageService,
        private messagesService: MessagesService,
        private renderer: Renderer) {
        super();
    }

    public ngOnInit() {
        this.readonly = true;
        this.hasHeader = false;
        
        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);

    }

    public ngOnChanges(changes: SimpleChanges) {
        if ((changes['objectId'] != null && changes['objectId'].currentValue != changes['objectId'].previousValue) ||
            (changes['objectType'] != null && changes['objectType'].currentValue != changes['objectType'].previousValue)) {
            if (this.diagram != null && this.diagram.div != null)
                this.diagram.div = null;
            if (this.palette != null && this.palette.div != null)
                this.palette.div = null;

            this.selectedData = null;
            this.initializeDiagram();
            this.resizeDiagram();

        }
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        if (this.diagram != null)
            this.diagram.div = null;
        if (this.palette != null)
            this.palette.div = null;


    }

    //#region helper methods

    private initializeDiagram(): Promise<any> {
        if (this.diagram != null) {
            return Promise.resolve();
        }

        this.diagram = this.createDiagram();

        this.diagram.nodeTemplateMap.add('object', this.createObjectNode());
        this.diagram.nodeTemplateMap.add('focal', this.createFocalNode());
        this.diagram.nodeTemplateMap.add('palette', this.createPaletteNode());

        this.diagram.linkTemplateMap.add('', this.createDefaultLink());
        this.diagram.linkTemplateMap.add('adding', this.createPendingAddLink());
        this.diagram.linkTemplateMap.add('deleting', this.createPendingDeleteLink());

        this.diagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.diagram.toolManager.linkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.diagram.toolManager.relinkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.diagram.toolManager.linkingTool.isEnabled = !this.readonly;
        this.diagram.toolManager.linkingTool.archetypeLinkData = new LinkModelV2();

        this.diagram.allowDrop = true;

        return this.populateDiagram();
    }

    private initializePalette(): Promise<any> {
        if (this.palette != null) {
            this.palette.layout.invalidateLayout();
            this.reOrderLayout();
            return Promise.resolve();
        }

        return this.lineageService.getLineageObjectTypes()
            .then(r => {
                this.objectTypes = r;
                this.objectTypes.forEach(o => {
                    if (o.template != null) {
                        o.template = JSON.parse(o.template);
                    }
                })
            })
            .then(() => {
                this.palette = this.createPalette();
                this.reOrderLayout();
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
                this.reOrderLayout();
            });
    }

    private parseData(data: any) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
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
                model.key = d.id;
                model.assetId = d.assetId;
                model.object = d.object;
                model.objectId = d.objectId;

                model.objectTypeName = d.type;
                model.name = d.name;
                model.foreColor = d.fore;
                model.backColor = d.back;
                model.category = 'object';

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
                link.state = d.state;
                if (link.state == 0)
                    link.category = 'adding';
                else if (link.state == 2)
                    link.category = 'deleting';
                else
                    link.category = '';
                
                linkList.push(link);

            }
        }

        //console.log('parseData', modelList);

        for (var i = 0; i < modelList.length; i++) {
            this.diagram.model.addNodeData(modelList[i]);
        }

        for (var i = 0; i < linkList.length; i++) {
            dm.addLinkData(linkList[i]);
        }

        //get deep copy of lists
        this.initialLinks = _.cloneDeep(linkList);
        this.initialNodes = _.cloneDeep(modelList);

        this.refreshControls(null);  //set buttons/expanders to defaults

        this.diagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private refreshControls(data: any) {
        this.setSourceValues(data);
        this.toggleTabs(data);
        this.loadMenuItems();
    }

    private toggleTabs(data: NodeModelV2 | LinkModelV2) {
        //console.log(this.tab, data);
        if (data) {
            this.showNodeTabs = data.diagramObjectType == DiagramObjectType.Node;
            this.showLinkTabs = false; // there's nothing to show currently
            this.showEditTab = (data != null && (<any>data).key != null && (<any>data).key.toString().indexOf('-') > -1);
            this.showInfoTab = (this.showLinkTabs || ((<any>data).object != null && (<any>data).objectId != null) || (data.category == 'map' && (<any>data).template != null));

            if (this.tab != 'info' && this.tab != 'edit')
                this.tab = 'info';

            if (!this.showNodeTabs) {
                this.isWindowVisible = false;
                return;
            }

            if (this.tab == 'edit' && !this.showEditTab) {
                if (this.showInfoTab) {
                    this.tab = 'info';
                } else {
                    this.tab = '';
                    this.isWindowVisible = false;
                }
            } else if (this.tab == 'info' && !this.showInfoTab) {
                if (this.showEditTab) {
                    this.tab = 'edit';
                } else {
                    this.tab = '';
                    this.isWindowVisible = false;
                }
            }

            if (this.showEditTab || this.showInfoTab)
                this.isWindowVisible = true;

        } else {
            this.showNodeTabs = false;
            this.showLinkTabs = false;
            this.showEditTab = false;
            this.showInfoTab = false;
            this.isWindowVisible = false;
            this.tab = '';
        }
    }

    private loadMenuItems() {
        this.menuItems = []; 

        this.menuItems.push({
            icon: 'fa-info-circle',
            items: null
        });
    }

    private reOrderLayout() {
        this.diagram.layout.invalidateLayout();
        this.diagram.requestUpdate();
    }

    private selectTab(val: string) {
        this.headerText = '';
        this.tab = val;
    }

    private validateNode(n: NodeModelV2) {
        return true;
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

                var from = this.diagram.model.findNodeDataForKey(data.from);
                var to = this.diagram.model.findNodeDataForKey(data.to);

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
    //#endregion

    //#region events

    private changeNode(e: NodeModelV2) {
        
        let node: NodeModelV2 = this.diagram.model.findNodeDataForKey(e.key);
        //console.log('changeNode', e, node, this.myDiagram);
        if (node == null)
            return;

        this.diagram.startTransaction('changeNode');

        let objChanged = (node.object != e.object || node.objectId != e.objectId);

        node.object = e.object;
        node.objectId = e.objectId;

        this.diagram.model.setDataProperty(node, 'name', e.name);
        this.validateNode(node);

        this.diagram.commitTransaction('changeNode');
    }

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.resizeDiagram();
    }

    private resizeDiagram() {
        this.diagramRef.nativeElement.style.height = (window.innerHeight - 142) + 'px';
        this.paletteRef.nativeElement.style.height = (window.innerHeight - 142) + 'px';

        let dOffset = (this.hasHeader ? this.diagramOffset : this.diagramOffset - 125);
        let oOffset = (this.hasHeader ? this.overlayOffset : this.overlayOffset - 125);
        this.diagramRef.nativeElement.style.height = (window.innerHeight - dOffset) + 'px';
        this.paletteRef.nativeElement.style.height = (window.innerHeight - dOffset) + 'px';
        this.overlayMaxHeight = window.innerHeight - oOffset;

    }

    private zoomDiagram(v: number) {
        this.diagram.scale = v;
        //console.log('zoomDiagram', v, this.myDiagram);
    }

    private ChangedSelection(e: any) {
        if (e == null)
            this.selection = this.diagram.selection;
        else
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
                this.objectType = obj.object;
                this.objectID = obj.objectId;

                this.populateDiagram();
            }
        }
        return;
    }

    private SelectionDeleted(e: any) {

    }

    private ExternalObjectsDropped(e: any) {
        //console.log(e, this.myDiagram.selection);
    }

    private menuClick(e: MenuItem) {
        if (e.icon == 'fa-info-circle') {
            this.isWindowVisible = !this.isWindowVisible;
        }
    }

    private closeEditor() {
        this.headerText = 'Lineage';
        this.diagramMode = DiagramMode.Diagram;
        this.loadMenuItems();
    }

    private ungroupSelection() {
        let selection = this.diagram.selection;
        let nodes = [];
        let maps = [];

        selection.each(s => {
            let data = s.data;

            if (data.category == 'map') {
                maps.push(data);
            }

        });

        maps.forEach(m => {
            let mapNodes = this.diagram.model.nodeDataArray.filter(n => (<any>n).group == m.group);

            mapNodes.forEach(n => {
                this.diagram.model.setDataProperty(n, 'group', null);
            });
        });
        this.reOrderLayout();
    }

    private groupSelection() {
        let selection = this.diagram.selection;
        let maps = [];

        //console.log('groupSelection',selection);

        //find all selected maps
        selection.each(s => {
            let data = s.data;

            if (data.category == 'map') {
                maps.push(data);
            }
        });


        if (maps.length > 1) {
            let group = new NodeModelV2();
            group.category = 'transform';
           // group.isGroup = true;
            this.diagram.model.addNodeData(group);
            this.diagram.model.setDataProperty(group, 'name', '');

            maps.forEach(m => {
                this.diagram.model.setDataProperty(m, 'group', group.key);
            });
        }

        //console.log(maps);
    }

    //#endregion

    //#region templates

    private createDiagram(): go.Diagram {


        let dg = this.g(go.Diagram, 'LineageDiagram', {
            initialContentAlignment: go.Spot.Center,
            allowDrop: true,
            initialAutoScale: go.Diagram.UniformToFill,
            scrollMode: go.Diagram.DocumentScroll,
            initialPosition: new go.Point(go.Spot.Center.x, go.Spot.Center.y),
            layout: this.g(go.LayeredDigraphLayout, {
                //angle: 0,
                layerSpacing: 12,
                columnSpacing: 12
                //rowSpacing: 10
            }),
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

        //dg.isReadOnly = this.readonly;

        //the readonly property disallows dragging and expand/collapse, so we need to manually disable everything else here instead to prevent keyboard shortcuts
        let dt = dg.toolManager.diagram;
        dt.allowDelete = !this.readonly;
        dt.allowClipboard = !this.readonly;
        dt.allowCopy = !this.readonly;
        dt.allowInsert = !this.readonly;
        dt.allowLink = !this.readonly;
        dt.allowRelink = !this.readonly;
        dt.allowGroup = !this.readonly;
        dt.allowTextEdit = !this.readonly;

        return dg;
    }

    private createPalette(): go.Palette {
        let paletteModel = [];

        this.objectTypes.forEach(o => {
            let isMap = (o.object == 'MapType');

            paletteModel.push({
                category: isMap ? 'map' : 'object',
                name: o.name,
                objectTypeName: o.objectTypeName,
                objectType: o.object,
                objectTypeId: o.objectId,
                foreColor: o.foreColor,
                backColor: o.backColor,
                isGroup: isMap,
                diagramObjectType: DiagramObjectType.Node,
                visible: true,
                order: o.order,
                template: o.template,
                templateId: o.templateId
            });
        });

        let pt: go.Palette = this.g(go.Palette, "LineagePalette",
            {
                "animationManager.duration": 400,
                nodeTemplateMap: this.diagram.nodeTemplateMap,
                groupTemplateMap: this.diagram.groupTemplateMap,
                model: new go.GraphLinksModel(paletteModel),
                layout: this.g(go.GridLayout, {
                    sorting: go.GridLayout.Ascending
                })
            });

        return pt;
    }

    private createObjectNode(): go.Node {
        let nodeWidth = 150;
        let nodeHeight = 75;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 8;

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
                    new go.Binding("fill", "backColor"),
                    new go.Binding("stroke", "valid", (v, m) => {
                        let data = m.panel.panel.data;
                        if (data == null) return 'transparent';
                        if (data.valid == false) return '#f00';
                        return data.foreColor;
                    })
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
                        row: 0,
                        margin: 2,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: "bold " + nodeFontSize + "pt sans-serif"
                    },
                        new go.Binding("text", "name").makeTwoWay(),
                        new go.Binding("stroke", "foreColor")
                    ),
                    this.g(go.TextBlock, {
                        row: 1,
                        margin: 2,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: nodeFontSize - 2 + "pt sans-serif"
                    },
                        new go.Binding("text", "objectTypeName").makeTwoWay(),
                        new go.Binding("stroke", "foreColor")
                    )
                )
            ));
    }

    private createFocalNode(): go.Node {
        let nodeWidth = 150;
        let nodeHeight = 75;
        let nodeBorderColor = '#000';
        let nodeFontSize = 8;

        return this.g(go.Node, "Spot",
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    strokeWidth: 3,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape"
                },
                    new go.Binding("fill", "backColor"),
                    new go.Binding("stroke", "valid", (v, m) => {
                        let data = m.panel.panel.data;
                        if (data == null) return 'transparent';
                        if (data.valid == false) return '#f00';
                        return data.foreColor;
                    })
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
                    ),
                    this.g(go.TextBlock, {
                        row: 1,
                        margin: 2,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: nodeFontSize - 2 + "pt sans-serif"
                    },
                        new go.Binding("text", "objectTypeName").makeTwoWay(),
                        new go.Binding("stroke", "foreColor")
                    )
                )
            ));
    }

    private createDefaultLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.Orthogonal,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false,
                //curve: go.Link.Bezier
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

    private createPendingAddLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.Orthogonal,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false,
                //curve: go.Link.Bezier
            }, // the whole link panel
            this.g(go.Shape, {
                stroke: "gray", strokeWidth: 2, strokeDashArray: [3, 2]
            },
                new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
                new go.Binding("stroke", "hasProperties", function (h) { return h ? "black" : "gray" }),
                {
                    toolTip: this.showTooltip("Pending Add")
                }
            ), 
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
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4,
                },
                    // the label
                    new go.Binding("text", "text").makeTwoWay()
                )
            )
        );
    }

    private createPendingDeleteLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.Orthogonal,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false,
                //curve: go.Link.Bezier
            }, // the whole link panel
            this.g(go.Shape, {
                stroke: "#900", strokeWidth: 2, strokeDashArray: [3, 2]
            },
                new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
                new go.Binding("stroke", "hasProperties", function (h) { return h ? "black" : "gray" }),
                {
                    toolTip: this.showTooltip("Pending Delete")
                }
            ),
            this.g(go.Shape, { toArrow: "standard", fill: "#900", stroke: "#900" }),
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

    private makePort(name, spot, output, input) {
        return this.g(go.Shape, "Circle",
            {
                fill: "transparent",
                stroke: null,
                desiredSize: new go.Size(9, 9),
                alignment: spot, alignmentFocus: spot,
                portId: name,
                fromSpot: spot, toSpot: spot,
                fromLinkable: output, toLinkable: input,
                cursor: "pointer"
            });
    }

    private showPorts(node, show) {
        let diagram = node.diagram;
        if (!diagram || diagram.isReadOnly || !diagram.allowLink) return;
        node.ports.each((port) => {
            port.stroke = (show ? "#000" : null);
        });
    }

    private showTooltip(text: string): go.Adornment {
        return this.g(go.Adornment, "Auto",
            this.g(go.Shape, { fill: "#333" }),
            this.g(go.TextBlock, { margin: 4, text: text, stroke: "#fff" }
        ));
    }

    //#endregion
}

enum DiagramMode {
    Diagram,
    SourceRuleEditor,
    BusinessLineageEditor,
    TechnicalLineageEditor
}
