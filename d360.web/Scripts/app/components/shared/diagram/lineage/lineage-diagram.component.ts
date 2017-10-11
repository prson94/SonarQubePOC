import { Component, Input, OnInit, AfterViewInit, ElementRef, OnDestroy, ViewChild, Renderer, HostListener } from '@angular/core';
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
    private isWindowVisible = true;
    private showNodeTabs = false;
    private showLinkTabs = false;
    private menuItems: MenuItem[] = [];
    private editorMenuItems: MenuItem[] = [];
    private tab: string = 'info';
    private headerText = 'Info';

    //diagram properties
    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;
    private myPalette: go.Palette;

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
        this.readonly = this.readonly.toString() == 'true' ? true : false;

        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
        //this.initializeDiagram();
        //if (!this.readonly) this.initializePalette();
    }

    public ngOnChanges() {

        this.initializeDiagram()
            .then(() => this.toggleReadOnly())
            .then(() => this.initializePalette());

    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        if (this.myDiagram != null)
            this.myDiagram.div = null;
        if (this.myPalette != null)
            this.myPalette.div = null;


    }

    //#region helper methods

    private initializeDiagram(): Promise<any> {
        if (this.myDiagram != null) {
            return Promise.resolve();
        }

        this.myDiagram = this.createDiagram();

        this.myDiagram.groupTemplateMap.add('map', this.createMapGroup());
        this.myDiagram.groupTemplateMap.add('transform', this.createTransformationGroup());

        this.myDiagram.nodeTemplateMap.add('object', this.createObjectNode());
        this.myDiagram.nodeTemplateMap.add('focal', this.createFocalNode());
        this.myDiagram.nodeTemplateMap.add('palette', this.createPaletteNode());

        this.myDiagram.linkTemplateMap.add('', this.createDefaultLink());

        this.myDiagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.myDiagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        //this.myDiagram.addDiagramListener('ExternalObjectsDropped', e => this.ExternalObjectsDropped(e));

        this.myDiagram.grid.visible = false;
        this.myDiagram.grid.gridCellSize = new go.Size(8, 8);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.myDiagram.toolManager.linkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.myDiagram.toolManager.relinkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.myDiagram.toolManager.linkingTool.isEnabled = !this.readonly;
        this.myDiagram.toolManager.linkingTool.archetypeLinkData = new LinkModelV2();

        this.myDiagram.allowDrop = true;

        this.myDiagram.mouseDrop = e => this.finishDrop(e, null);

        //console.log('init', this.myDiagram);

        return this.populateDiagram();
    }

    private initializePalette(): Promise<any> {
        if (this.myPalette != null) {
            this.myPalette.layout.invalidateLayout();
            this.reOrderLayout();
            return Promise.resolve();
        }

        return this.lineageService.getLineageObjectTypes()
            .then(r => {
                this.objectTypes = r;
            })
            .then(() => {
                this.myPalette = this.createPalette();
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
        this.myDiagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.myDiagram.model;
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
                model.key = d.key;
                model.object = d.object;
                model.objectId = d.objectId;
                model.objectType = d.objectType;
                model.objectTypeId = d.objectTypeId;
                model.objectTypeName = d.objectTypeName;
                model.name = d.name;
                model.foreColor = d.foreColor;
                model.backColor = d.backColor;
                model.category = d.category;
                model.businessTransformation = d.businessTransformation;
                model.technicalTransformation = d.technicalTransformation;
                model.order = d.order;
                model.intersectTypeId = d.intersectTypeId;

                model.isGroup = d.isGroup
                model.group = d.group;

                //if (model.category == 'map')
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
                link.category = '';
                linkList.push(link);
            }
        }

        for (var i = 0; i < modelList.length; i++) {
            this.myDiagram.model.addNodeData(modelList[i]);
        }

        for (var i = 0; i < linkList.length; i++) {
            dm.addLinkData(linkList[i]);
        }

        //get deep copy of lists
        this.initialLinks = _.cloneDeep(linkList);
        this.initialNodes = _.cloneDeep(modelList);

        this.refreshControls(null);  //set buttons/expanders to defaults

        this.myDiagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private refreshControls(data: any) {
        this.setSourceValues(data);
        this.toggleTabs(data);
        this.loadMenuItems();
    }

    private toggleTabs(data: NodeModelV2 | LinkModelV2) {
        if (data) {
            this.showNodeTabs = data.diagramObjectType == DiagramObjectType.Node;
            this.showLinkTabs = data.diagramObjectType == DiagramObjectType.Link;

            if (this.showLinkTabs) this.selectTab('exchange');
            else if (this.showNodeTabs) this.selectTab('info');
        } else {
            this.showNodeTabs = false;
            this.showLinkTabs = false;
            this.tab = '';
        }
    }

    private toggleReadOnly(readonly?: boolean) {
        if (readonly != null) this.readonly = readonly;

        //this.myDiagram.isReadOnly = this.readonly;

        let dt = this.myDiagram.toolManager.diagram
        dt.allowDelete = !this.readonly;
        dt.allowClipboard = !this.readonly;
        dt.allowCopy = !this.readonly;
        dt.allowInsert = !this.readonly;
        dt.allowLink = !this.readonly;
        dt.allowRelink = !this.readonly;
        dt.allowGroup = !this.readonly;
        dt.allowTextEdit = !this.readonly;

        this.myDiagram.toolManager.linkingTool.isEnabled = !this.readonly;

        this.loadMenuItems();


        //this.myDiagram.model.isReadOnly = this.readonly;
    }

    private loadMenuItems() {
        this.menuItems = [];
        this.editorMenuItems = [];
        

        this.editorMenuItems.push({
            icon: 'fa-floppy-o',
            items: null
        });


        this.editorMenuItems.push({
            icon: 'fa-object-group',
            items: null
        });

        this.editorMenuItems.push({
            icon: 'fa-object-ungroup',
            items: null
        });

        this.menuItems.push({
            icon: 'fa-pencil',
            items: null
        });

        this.menuItems.push({
            icon: 'fa-plus-square-o',
            items: null
        });

        this.menuItems.push({
            icon: 'fa-minus-square-o',
            items: null
        });

        this.menuItems.push({
            icon: 'fa-info-circle',
            items: null
        });

        if (!this.readonly)
            this.editorMenuItems.forEach(e => {
                this.menuItems.push(e);
            });

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

                var from = this.myDiagram.model.findNodeDataForKey(data.from);
                var to = this.myDiagram.model.findNodeDataForKey(data.to);

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

    private reOrderLayout() {
        this.myDiagram.layout.invalidateLayout();
        this.myDiagram.requestUpdate();
    }

    private selectTab(val: string) {
        switch (val) {
            case 'info': this.headerText = 'Info'; break;
            case 'code': this.headerText = 'Source Rules'; break;
            case 'user': this.headerText = 'Responsibilities'; break;
            case 'database': this.headerText = 'Fusion Relationships'; break;
            case 'exchange': this.headerText = 'Mapping Rules'; break;
            default: this.headerText = ''; break;
        }
        this.tab = val;
    }

    private validateNode(n: NodeModelV2) {
        let valid = true;

        //n.valid = true;

        switch (n.category) {
            case 'object':
            case 'focal':
                if (n.object == null || n.objectId == null)
                    valid = false;
                if (n.group == null)
                    valid = false;
                if (n.isGroup)
                    valid = false;
                break;
            case 'map':
                if (!n.isGroup)
                    valid = false;
                if (this.myDiagram.model.nodeDataArray.filter(c => (<any>c).group == n.key).length < 1)
                    valid = false;
                break;
            case 'transform':
                if (!n.isGroup || n.group != null)
                    valid = false;
                if (this.myDiagram.model.nodeDataArray.filter(c => (<any>c).group == n.key).length < 1)
                    valid = false;
                break;
        }

        let node = this.myDiagram.model.findNodeDataForKey(n.key);
        this.myDiagram.model.setDataProperty(node, 'valid', valid);
        //console.log('validateNode', n.valid, n);

    }

    private save() {
        //convert to lineage models
        let model: LineageEditorModelV2 = new LineageEditorModelV2();
        model.Focal = this.objectType;
        model.FocalID = this.objectID;
        let valid = true;

        this.myDiagram.model.nodeDataArray.forEach(n => {
            let node = (<any>n);
            let nodeModel: LineageNodeModel = new LineageNodeModel();
            nodeModel.Group = node.group;
            nodeModel.IsGroup = node.isGroup;
            nodeModel.Key = node.key;
            nodeModel.Object = node.object;
            nodeModel.ObjectID = node.objectId;
            nodeModel.ObjectType = node.objectType;
            nodeModel.ObjectTypeID = node.objectTypeId;
            nodeModel.Category = node.category;
            nodeModel.BusinessTransformation = node.businessTransformation;
            nodeModel.TechnicalTransformation = node.technicalTransformation;
            nodeModel.IntersectTypeID = node.intersectTypeId;
            nodeModel.Order = node.order;

            if (node.valid == false)
                valid = false;

            model.Nodes.push(nodeModel);
        });

        (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.forEach(l => {
            let link = (<any>l);
            let linkModel: LineageLinkModel = new LineageLinkModel();
            linkModel.IntersectID = link.intersectId;
            linkModel.From = link.from;
            linkModel.To = link.to;

            model.Links.push(linkModel);
        });

        if (!valid) {
            this.messagesService.showError('Error', 'One or more nodes on the diagram have validation issues');
            return;
        }

        this.isLoading = true;
        console.log('save model', model);
        this.lineageService.postLineage(model)
            .then(() => {
                this.isLoading = false;
                //console.log('save complete');
            });
    }
    //#endregion

    //#region events

    private changeNode(e: NodeModelV2) {
        let node: NodeModelV2 = this.myDiagram.model.findNodeDataForKey(e.key);

        node.object = e.object;
        node.objectId = e.objectId;
        node.technicalTransformation = e.technicalTransformation;

        this.myDiagram.model.setDataProperty(node, 'businessTransformation', e.businessTransformation);
        this.myDiagram.model.setDataProperty(node, 'name', e.name);
        //this.myDiagram.model.setDataProperty(node, 'valid', true);
        this.validateNode(node);
    }

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.resizeDiagram();
    }

    private resizeDiagram() {
        this.diagramRef.nativeElement.style.height = (window.innerHeight - 142) + 'px';
        this.paletteRef.nativeElement.style.height = (window.innerHeight - 142) + 'px';
        //this.overlayMaxHeight = window.innerHeight - oOffset;
    }

    private zoomDiagram(v: number) {
        this.myDiagram.scale = v;
        //console.log('zoomDiagram', v, this.myDiagram);
    }

    private ChangedSelection(e: any) {
        if (e == null)
            this.selection = this.myDiagram.selection;
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
                this.objectType = obj.obj;
                this.objectID = obj.objid;

                this.populateDiagram();
            }
        }
    }

    private ExternalObjectsDropped(e: any) {
        console.log(e, this.myDiagram.selection);
    }

    private menuClick(e: MenuItem) {
        //TODO: this is a hack, need a better way to handle these clicks
        if (e.icon == 'fa-pencil') {
            this.readonly = !this.readonly;
            this.toggleReadOnly();

            //console.log('menuClick', this.myPalette, this.readonly);

            this.initializePalette();
            this.resizeDiagram();
        }
        else if (e.icon == 'fa-object-group') {
            this.groupSelection();
        }
        else if (e.icon == 'fa-object-ungroup') {
            this.ungroupSelection();
        }
        else if (e.icon == 'fa-info-circle') {
            this.isWindowVisible = !this.isWindowVisible;
        }
        else if (e.icon == 'fa-floppy-o') {
            this.save();
        }
        else if (e.icon == 'fa-plus-square-o') {
            this.myDiagram.findTopLevelGroups().each(g => g.isSubGraphExpanded = true);
        }
        else if (e.icon == 'fa-minus-square-o') {
            this.myDiagram.findTopLevelGroups().each(g => g.isSubGraphExpanded = false);
        }
    }

    private closeEditor() {
        this.headerText = 'Lineage';
        this.diagramMode = DiagramMode.Diagram;
        this.loadMenuItems();
    }

    private ungroupSelection() {
        let selection = this.myDiagram.selection;
        let nodes = [];
        let maps = [];

        selection.each(s => {
            let data = s.data;

            if (data.category == 'map') {
                maps.push(data);
            }

        });

        maps.forEach(m => {
            let mapNodes = this.myDiagram.model.nodeDataArray.filter(n => (<any>n).group == m.group);

            mapNodes.forEach(n => {
                this.myDiagram.model.setDataProperty(n, 'group', null);
            });
        });

        this.removeEmptyGroups()
        this.reOrderLayout();
    }

    private groupSelection() {
        let selection = this.myDiagram.selection;
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
            group.isGroup = true;
            this.myDiagram.model.addNodeData(group);
            this.myDiagram.model.setDataProperty(group, 'name', '');

            maps.forEach(m => {
                this.myDiagram.model.setDataProperty(m, 'group', group.key);
            });
        }

        //console.log(maps);
    }

    private removeEmptyGroups() {
        let removes = [];

        this.myDiagram.model.nodeDataArray.forEach(n => {
            let node = <NodeModelV2>n;

            if (node.isGroup && node.category == 'transform') {
                let children = this.myDiagram.model.nodeDataArray.filter(c => (<any>c).group == node.key);
                if (children.length < 2) {
                    removes.push(node);
                }
            }
        });

        removes.forEach(r => this.myDiagram.model.removeNodeData(r));
    }

    private highlightGroup(e, grp: go.Group, show) {
        //console.log('highlightGroup', e, grp, show);

        if (!grp) return;
        e.handled = true;
        if (show) {

            // cannot depend on the grp.diagram.selection in the case of external drag-and-drops;
            // instead depend on the DraggingTool.draggedParts or .copiedParts
            var tool = grp.diagram.toolManager.draggingTool;
            var map = tool.draggedParts || tool.copiedParts;  // this is a Map
            // now we can check to see if the Group will accept membership of the dragged Parts
            if (grp.canAddMembers(map.toKeySet())) {
                grp.isHighlighted = true;
                return;
            }
        }
        grp.isHighlighted = false;
    }

    private finishDrop(e, grp: go.Group) {
        let node = e.diagram.selection.first().data;

        //console.log('finishDrop', e, grp, node);

        if (node != null) {
            if (node.isGroup) {
                e.diagram.commandHandler.addTopLevelParts(e.diagram.selection, true);
                if (node.name == 'Map')
                    this.myDiagram.model.setDataProperty(node, 'name', '<drop objects here>');
            } else {
                if (grp == null) {
                    e.diagram.currentTool.doCancel();
                    this.messagesService.showError('Error', 'This item can only be added to maps');
                } else {
                    //if this is a new object, change the name/objName for display purposes
                    if (node.object == null && node.objectId == null && node.name != '<choose an object>') {
                        this.myDiagram.model.setDataProperty(node, 'objectTypeName', node.name);
                        this.myDiagram.model.setDataProperty(node, 'name', '<choose an object>');
                    }
                    grp.addMembers(grp.diagram.selection, true);
                    if (grp.data.name == '<drop objects here>') {
                        this.myDiagram.model.setDataProperty(grp.data, 'name', node.name);
                    }
                    //revalidate the group 
                    this.validateNode(this.myDiagram.model.findNodeDataForKey(grp.data.key));
                }
            }
            this.validateNode(node);
        } else {
            e.diagram.currentTool.doCancel();
        }
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
                layerSpacing: 7,
                columnSpacing: 7
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

        paletteModel.push({
            category: 'map',
            name: 'Map',
            object: null,
            objectId: null,
            foreColor: '#000',
            backColor: '#ddd',
            isGroup: true,
            diagramObjectType: DiagramObjectType.Node,
            visible: true
        });

        this.objectTypes.forEach(o => {
            paletteModel.push({
                category: 'object',
                name: o.name,
                objectType: o.object,
                objectTypeId: o.objectId,
                //objectTypeName: o.name,
                foreColor: o.foreColor,
                backColor: o.backColor,
                isGroup: false,
                diagramObjectType: DiagramObjectType.Node,
                visible: true
            });
        });



        let pt: go.Palette = this.g(go.Palette, "LineagePalette",
            {
                "animationManager.duration": 400,
                nodeTemplateMap: this.myDiagram.nodeTemplateMap,
                groupTemplateMap: this.myDiagram.groupTemplateMap,
                model: new go.GraphLinksModel(paletteModel),
                layout: this.g(go.GridLayout, { alignment: go.GridLayout.Forward }) //GridLayout.Forward preserves order
            });

        return pt;
    }

    private createObjectNode(): go.Node {
        let nodeWidth = 150;
        let nodeHeight = 50;
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
                    new go.Binding("stroke", "valid", v => { return v ? 'transparent' : '#f00' })
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
                    new go.Binding("stroke", "valid", v => { return v ? 'transparent' : '#f00' })
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

    private createMapGroup(): go.Group {
        return this.g(go.Group, "Auto",
            { // define the group's internal layout
                background: '#eee',
                mouseDragEnter: (e, grp, prev) => this.highlightGroup(e, grp, true),
                mouseDragLeave: (e, grp, next) => this.highlightGroup(e, grp, false),
                mouseEnter: (e, obj) => { this.showPorts(obj.part, true); },
                mouseLeave: (e, obj) => { this.showPorts(obj.part, false); },
                computesBoundsAfterDrag: true,
                // when the selection is dropped into a Group, add the selected Parts into that Group;
                // if it fails, cancel the tool, rolling back any changes
                mouseDrop: (e, grp) => this.finishDrop(e, grp),
                handlesDragDropForMembers: true,  // don't need to define handlers on member Nodes and Links
                // Groups containing Groups lay out their members horizontally
                layout:
                this.g(go.GridLayout,
                    {
                        wrappingColumn: 1, alignment: go.GridLayout.Location
                        //,cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4)
                    }),
                isSubGraphExpanded: false
            },
            new go.Binding("background", "isHighlighted", (h, p) => { return h ? "#faffad" : '#eee' }).ofObject(),
            this.g(go.Shape, "RoundedRectangle",
                { fill: null, stroke: "gray", strokeWidth: 2 }),
            this.g(go.Panel, "Vertical",
                { defaultAlignment: go.Spot.Left, margin: 4 },
                this.g(go.Panel, "Horizontal",
                    { defaultAlignment: go.Spot.Top },
                    // the SubGraphExpanderButton is a panel that functions as a button to expand or collapse the subGraph
                    this.g("SubGraphExpanderButton"),
                    this.g(go.TextBlock,
                        { font: "Bold 18px Sans-Serif", margin: 4 },
                        new go.Binding("text", "name"),
                        new go.Binding("visible", "isSubGraphExpanded", o => { return !o }).ofObject()
                    )
                ),
                // create a placeholder to represent the area where the contents of the group are
                this.g(go.Placeholder,
                    { padding: new go.Margin(0, 10) })
            )  // end Vertical Panel
            //,this.g(go.Panel, "Horizontal", { defaultAlignment: go.Spot.TopRight }, this.g("SubGraphExpanderButton"))
            , this.makePort('L', go.Spot.Left, false, true),
            this.makePort('R', go.Spot.Right, true, false)
        );  // end Group
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

    private createDefaultGroup(): go.Group {
        return this.g(go.Group, "Auto",
            { // define the group's internal layout

                layout: this.g(go.TreeLayout,
                    { angle: 90, arrangement: go.TreeLayout.ArrangementHorizontal, isRealtime: false }),
                // the group begins unexpanded;
                // upon expansion, a Diagram Listener will generate contents for the group
                isSubGraphExpanded: false
            },
            this.g(go.Shape, "Rectangle",
                { fill: null, stroke: "gray", strokeWidth: 2 }),
            this.g(go.Panel, "Vertical",
                { defaultAlignment: go.Spot.Left, margin: 4 },
                this.g(go.Panel, "Horizontal",
                    { defaultAlignment: go.Spot.Top },
                    // the SubGraphExpanderButton is a panel that functions as a button to expand or collapse the subGraph
                    this.g("SubGraphExpanderButton")
                    //,this.g(go.TextBlock,
                    //  { font: "Bold 18px Sans-Serif", margin: 4 },
                    //  new go.Binding("text", "name"))
                ),
                // create a placeholder to represent the area where the contents of the group are
                this.g(go.Placeholder,
                    { padding: new go.Margin(0, 10) })
            )  // end Vertical Panel
        );  // end Group
    }

    private createTransformationGroup(): go.Group {
        return this.g(go.Group, "Auto",
            { // define the group's internal layout
                background: '#f9f9f9',
                mouseDragEnter: (e, grp, prev) => this.highlightGroup(e, grp, true),
                mouseDragLeave: (e, grp, next) => this.highlightGroup(e, grp, false),
                computesBoundsAfterDrag: true,
                // when the selection is dropped into a Group, add the selected Parts into that Group;
                // if it fails, cancel the tool, rolling back any changes
                mouseDrop: (e, grp) => this.finishDrop(e, grp),
                handlesDragDropForMembers: true,  // don't need to define handlers on member Nodes and Links
                // Groups containing Groups lay out their members horizontally
                layout:
                this.g(go.GridLayout,
                    {
                        wrappingColumn: 1, alignment: go.GridLayout.Location
                        //,cellSize: new go.Size(1, 1), spacing: new go.Size(4, 4)
                    }),
                // the group begins unexpanded;
                // upon expansion, a Diagram Listener will generate contents for the group
                isSubGraphExpanded: true
            },
            new go.Binding("background", "isHighlighted", (h) => { return h ? "#faffad" : "#f9f9f9"; }).ofObject(),
            this.g(go.Shape, "Rectangle",
                { fill: null, stroke: "#000", strokeWidth: 1, strokeDashArray: [4,2] }),
            this.g(go.Panel, "Vertical",
                { defaultAlignment: go.Spot.Center, margin: 5 },
                this.g(go.Panel, "Horizontal",
                    { defaultAlignment: go.Spot.Top },
                    // the SubGraphExpanderButton is a panel that functions as a button to expand or collapse the subGraph
                    //this.g("SubGraphExpanderButton"),
                    this.g(go.TextBlock,
                        { font: "bold 10px Sans-Serif", margin: 2 },
                        new go.Binding("text", "businessTransformation", n => { return (n.length > 25) ? n.substring(0, 23) + '...' : n }),
                        new go.Binding("visible", "businessTransformation", n => { return (n == null || n == '') ? false : true })
                    )
                )
                ,
                // create a placeholder to represent the area where the contents of the group are
                this.g(go.Placeholder,
                    { padding: new go.Margin(0, 5) })
            )  // end Vertical Panel
        );  // end Group
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

    //#endregion
}

enum DiagramMode {
    Diagram,
    SourceRuleEditor,
    BusinessLineageEditor,
    TechnicalLineageEditor
}
