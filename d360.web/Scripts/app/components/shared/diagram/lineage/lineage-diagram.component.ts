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
        this.hasHeader = false;// !this.readonly;

        this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
        //this.initializeDiagram();
        //if (!this.readonly) this.initializePalette();
    }

    public ngOnChanges(changes: SimpleChanges) {
        if ((changes['objectId'] != null && changes['objectId'].currentValue != changes['objectId'].previousValue) ||
            (changes['objectType'] != null && changes['objectType'].currentValue != changes['objectType'].previousValue)) {
            if (this.myDiagram != null && this.myDiagram.div != null)
                this.myDiagram.div = null;
            if (this.myPalette != null && this.myPalette.div != null)
                this.myPalette.div = null;

            this.selectedData = null;
            this.initializeDiagram();
            this.resizeDiagram();

        }

        if (changes['readonly'] != null && changes['readonly'].currentValue != changes['readonly'].previousValue) {
            this.toggleReadOnly();
        }
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
        this.myDiagram.addDiagramListener('SelectionDeleted', e => this.SelectionDeleted(e));
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
                this.objectTypes.forEach(o => {
                    if (o.template != null) {
                        o.template = JSON.parse(o.template);
                    }
                })
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

    private toggleReadOnly(readonly?: boolean) {
        if (readonly != null) this.readonly = readonly;

        //this.hasHeader = !this.readonly;

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

        //if (!this.readonly)
        //    this.createDiagram();


        //this.myDiagram.model.isReadOnly = this.readonly;
    }

    private loadMenuItems() {
        this.menuItems = [];
        this.editorMenuItems = [];   
        

        //let add = {
        //    icon: 'fa-plus',
        //    items: []
        //};

        //this.editorMenuItems.push(add);


        //if (this.selectedData != null && this.selectedData.category == 'map')
        //    add.items.push({
        //        icon: 'fa-plus',
        //        label: 'Add focal object to selected mapping',
        //        items: null
        //    });

        this.editorMenuItems.push({
            icon: 'fa-floppy-o',
            items: null
        });

        let mapCount = 0;
        if (this.selection != null)
            this.selection.each(s => {
                if (s.category == 'map')
                    mapCount++;
            });

        if (this.selection != null && mapCount > 1)
        this.editorMenuItems.push({
            icon: 'fa-object-group',
            items: null
        });

        if (this.selectedData != null)
            this.editorMenuItems.push({
                icon: 'fa-object-ungroup',
                items: null
            });

        if (this.readonly)
            this.menuItems.push({
                icon: 'fa-pencil',
                items: null
            });

        if (!this.readonly)
            this.menuItems.push({
                icon: 'fa-close',
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
        this.headerText = '';
        this.tab = val;
    }

    private validateNode(n: NodeModelV2) {
        let valid = true;

        if (n == null) {
            console.warn('NULL passed to validateNode()');
            return;
        }

        n.errors = [];

        switch (n.category) {
            case 'object':
            case 'focal':
                if (n.object == null || n.objectId == null) {
                    valid = false;
                    n.errors.push('No object has been chosen for this item');
                }
                if (n.group == null) {
                    valid = false;
                    n.errors.push('This item must be inside a map');

                }
                if (n.isGroup) {
                    valid = false;
                    n.errors.push(`This item's template is incorrect`);

                }
                //invalidate the map too
                if (!valid && n.group != null) {
                    let map = this.myDiagram.model.findNodeDataForKey(n.group);
                    if (map != null) {
                        this.myDiagram.model.setDataProperty(map, 'valid', false);
                        map.errors = [];
                        map.errors.push('One or more items in this map has validation issues');
                    }
                }
                break;
            case 'map':
                let items = this.myDiagram.model.nodeDataArray.filter(c => (<any>c).group == n.key);
                if (!n.isGroup) {
                    valid = false;
                    n.errors.push(`This item's template is incorrect`);
                }
                if (items.length < 1) {
                    valid = false;
                    n.errors.push(`This map must contain at least one object`);
                }
                if (valid && this.myDiagram.model.nodeDataArray.filter(c => (<any>c).group == n.key && (<any>c).valid == false).length > 0) {
                    valid = false;
                    n.errors.push('One or more items in this map has validation issues');
                }
                if (n.template != null) { //check for required template items
                    n.template.filter(t => t.isRequired).forEach(t => {
                        let i = items.find(i => (<any>i).objectType == t.object && (<any>i).objectTypeId == t.objectId);
                        let o = this.objectTypes.find(o => o.object == t.object && o.objectId == t.objectId);
                        if (i == null) {
                            valid = false;
                            if (o == null) {
                                n.errors.push(`This map template is missing a required object`);
                            } else
                                n.errors.push(`This map template requires a ${o.name} object`);

                        }
                    });
                }
                break;
            case 'transform':
                if (!n.isGroup || n.group != null) {
                    valid = false;
                    n.errors.push(`This item's template is incorrect`);
                }
                if (this.myDiagram.model.nodeDataArray.filter(c => (<any>c).group == n.key).length < 2) {
                    valid = false;
                    n.errors.push(`The transformation must contain at least 2 map items`);

                }
                break;
        }

        let node = this.myDiagram.model.findNodeDataForKey(n.key);
        if (node != null) {
            this.myDiagram.model.setDataProperty(node, 'valid', valid);
            if (node.group != null)
                this.validateNode(this.myDiagram.model.findNodeDataForKey(node.group));
        }

        //console.log('validateNode', n.valid, n);

    }

    private updateMapName(key: string) {
        let map = this.myDiagram.model.findNodeDataForKey(key);
        let items = this.myDiagram.model.nodeDataArray.filter(n => (<any>n).group == key && (<any>n).object != null && (<any>n).objectId != null);

        if (items.length < 1) {
            if (map != null) {
                this.myDiagram.model.setDataProperty(map, 'name', '<drop objects here>');
                this.myDiagram.model.setDataProperty(map, 'isSubGraphExpanded', false);
                this.validateNode(map);
            }
            return;
        }

        items.sort((a, b) => this.compareObjects(a, b));

        this.myDiagram.model.setDataProperty(map, 'name', (<any>items[0]).name);
    }

    private compareObjects(a: any, b: any): number {
        if (a.order < b.order) return -1;
        if (a.order > b.order) return 1;

        let aName = (a.name || '').toLowerCase();
        let bName = (b.name || '').toLowerCase();

        if (aName < bName) return -1;
        if (aName > bName) return 1;

        return 0;
      
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

        let windowState = this.isWindowVisible;
        this.isLoading = true;
        this.isWindowVisible = false;
        console.log('save model', model);
        this.lineageService.postLineage(model)
            .then(() => {
                this.isLoading = false;
                this.isWindowVisible = windowState;
                //console.log('save complete');
            });
    }
    //#endregion

    //#region events

    private changeNode(e: NodeModelV2) {
        
        let node: NodeModelV2 = this.myDiagram.model.findNodeDataForKey(e.key);
        //console.log('changeNode', e, node, this.myDiagram);
        if (node == null)
            return;

        this.myDiagram.startTransaction('changeNode');

        let objChanged = (node.object != e.object || node.objectId != e.objectId);
        //console.log('changeNode', objChanged);
        //node.name = null; //force name update
        node.object = e.object;
        node.objectId = e.objectId;
        node.technicalTransformation = e.technicalTransformation;

        this.myDiagram.model.setDataProperty(node, 'businessTransformation', e.businessTransformation);

        this.myDiagram.model.setDataProperty(node, 'name', e.name);
        this.validateNode(node);

        if (!node.isGroup && node.group != null)
            this.updateMapName(node.group);
        //console.log('changeNode', node);

        this.myDiagram.commitTransaction('changeNode');
    }

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.resizeDiagram();
    }

    private resizeDiagram() {
        this.diagramRef.nativeElement.style.height = (window.innerHeight - 142) + 'px';
        this.paletteRef.nativeElement.style.height = (window.innerHeight - 142) + 'px';
        //this.overlayMaxHeight = window.innerHeight - oOffset;

        let dOffset = (this.hasHeader ? this.diagramOffset : this.diagramOffset - 125);
        let oOffset = (this.hasHeader ? this.overlayOffset : this.overlayOffset - 125);
        this.diagramRef.nativeElement.style.height = (window.innerHeight - dOffset) + 'px';
        this.paletteRef.nativeElement.style.height = (window.innerHeight - dOffset) + 'px';
        this.overlayMaxHeight = window.innerHeight - oOffset;

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
        return;


        //var obj = e.diagram.selection.first().data;
        //if (obj != null) {
        //    if (obj.diagramObjectType == DiagramObjectType.Node) {
        //        this.objectType = obj.obj;
        //        this.objectID = obj.objid;

        //        this.populateDiagram();
        //    }
        //}
    }

    private SelectionDeleted(e: any) {
        //console.log('SelectionDeleted', e);

        //re-validate parent on delete
        e.subject.each(s => {
            let data = s.data;

            if (data.group != null) { 
                let grp = this.myDiagram.model.findNodeDataForKey(data.group);
                if (grp != null) {
                    this.validateNode(grp);
                }
            }
        });

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
        } else if (e.icon == 'fa-close') {
            this.readonly = true;
            this.toggleReadOnly();
            this.populateDiagram();
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
            this.myDiagram.nodes.each(n => {
                let g = n.containingGroup;
                if (g != null && (n.data.category == 'object' || n.data.category == 'focal')) {
                    g.isSubGraphExpanded = true;
                   // console.log(n, n.data, n.data.category);
                }
            });
        }
        else if (e.icon == 'fa-minus-square-o') {
            this.myDiagram.nodes.each(n => {
                let g = n.containingGroup;
                if (g != null && (n.data.category == 'object' || n.data.category == 'focal')) {
                    g.isSubGraphExpanded = false;
                   // console.log(n, n.data, n.data.category);

                }
            });
        } else if (e.icon == 'fa-plus') {
            //console.log('add node', e, this.selectedData, this.selection, this.myDiagram.selection);
            if (e.label.toLowerCase().indexOf('focal') > -1) {
                let mapKey = this.selectedData == null ? null : this.selectedData.key;

                if (mapKey != null) {
                    let focal = null;
                    let newFocal = new NodeModelV2();
                    let focalIndex = this.myDiagram.model.nodeDataArray.findIndex(f => (<any>f).category == 'focal');
                    let promises = [];

                    if (focalIndex > -1) {
                        focal = this.myDiagram.model.nodeDataArray[focalIndex];
                    } else {
                        //we many not have the focal info on new lineage, get it here
                        promises.push(this.lineageService.getLineageNodeDataForObject(this.objectType, this.objectID)
                            .then(r => {
                                focal = r;
                            }));
                    }

                    Promise.all(promises).then(() => {
                        if (focal != null) {
                            let objType = this.objectTypes.find(o => o.object == focal.objectType && o.objectId == focal.objectTypeId);
                            newFocal.name = focal.name;
                            newFocal.backColor = focal.backColor;
                            newFocal.foreColor = focal.foreColor;
                            newFocal.category = 'focal';
                            newFocal.object = focal.object;
                            newFocal.objectId = focal.objectId;
                            newFocal.objectType = focal.objectType;
                            newFocal.objectTypeId = focal.objectTypeId;
                            newFocal.objectTypeName = focal.objectTypeName;
                            newFocal.order = (objType == null ? null : objType.order);
                            newFocal.diagramObjectType = DiagramObjectType.Node;
                            newFocal.visible = true;
                            newFocal.isGroup = false;
                            newFocal.group = mapKey;

                            this.myDiagram.model.addNodeData(newFocal);
                            this.messagesService.showInfoMessage('Focal object added', 'The focal object has been added to the mapping.');
                        }
                    });

                }
            } else {
                let newMap = new NodeModelV2();
                let map = this.objectTypes.find(o => o.order == -1);
                if (map != null) {
                    newMap.backColor = map.backColor;
                    newMap.foreColor = map.foreColor;
                    newMap.name = '<drop objects here>';
                    newMap.category = 'map'
                    newMap.object = map.object;
                    newMap.objectId = map.objectId;
                    newMap.objectTypeId = 1
                    newMap.objectType = map.objectType;
                    newMap.order = null;
                    newMap.diagramObjectType = DiagramObjectType.Node;
                    newMap.visible = true;
                    newMap.isGroup = true;

                    this.myDiagram.model.addNodeData(newMap);
                    this.messagesService.showInfoMessage('Mapping added', 'A new mapping has been added to the diagram.');
                }
            }
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
                if (node.category == 'map' && node.order < 0) {
                    this.myDiagram.model.setDataProperty(node, 'name', '<drop objects here>');
                    this.myDiagram.model.setDataProperty(node, 'order', null);
                    if (node.template != null) {
                        //console.log('finishDrop- template', node);
                        node.template.forEach(i => {
                            let item = new NodeModelV2();
                            let type = this.objectTypes.find(o => o.object == i.object && o.objectId == i.objectId);
                            if (type == null)
                                return;
                            item.category = 'object';
                            item.backColor = type.backColor;
                            item.foreColor = type.foreColor;
                            item.isGroup = false;
                            item.visible = true;
                            item.diagramObjectType = DiagramObjectType.Node;
                            item.objectType = type.object;
                            item.objectTypeId = type.objectId;
                            item.object = null;
                            item.objectId = null;
                            item.order = type.order;
                            item.objectTypeName = type.name;
                            item.isRequired = (i.isRequired.toString() == 'true' ? true : false);
                            item.name = '<choose an object>';
                            item.group = node.key;
                            this.myDiagram.model.addNodeData(item);
                            this.validateNode(item);
                        });
                    }
                    this.validateNode(node);
                }
            } else {
                if (grp == null || (grp != null && grp.data != null && (node.category != 'map' && grp.data.category == 'transform'))) {
                    e.diagram.currentTool.doCancel();
                    this.messagesService.showError('Error', 'This item can only be added to maps');
                    this.selectedData = null;
                    this.selectTab('info');
                    this.isWindowVisible = false;
                    return;
                } else {
                    //if this is a new object, change the name/objName for display purposes
                    if (node.object == null && node.objectId == null && node.name != '<choose an object>') {
                        this.myDiagram.model.setDataProperty(node, 'objectTypeName', node.name);
                        this.myDiagram.model.setDataProperty(node, 'name', '<choose an object>');
                    }
                    grp.addMembers(grp.diagram.selection, true);
                    grp.isSubGraphExpanded = true;
                    if (grp.data.name == '<drop objects here>') {
                        this.myDiagram.model.setDataProperty(grp.data, 'name', node.name);
                    }
                    //revalidate the group 
                    this.validateNode(this.myDiagram.model.findNodeDataForKey(grp.data.key));
                }
            }
            this.validateNode(node);
            this.refreshControls(node);
        } else {
            e.diagram.currentTool.doCancel();
        }

        this.myDiagram.model.nodeDataArray.filter(n => (<any>n).category == 'map').forEach(n => {
            this.updateMapName((<any>n).key);
        });
        this.reOrderLayout();
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
                template: o.template
            });
        });

        //paletteModel.push({
        //    category: 'map',
        //    name: map != null ? map.name : 'Map',
        //    object: null,
        //    objectId: null,
        //    objectType: 'MapType',
        //    objectTypeId: 1,
        //    foreColor: '#000',
        //    backColor: '#ddd',
        //    isGroup: true,
        //    diagramObjectType: DiagramObjectType.Node,
        //    visible: true,
        //    order: -1
        //});

        //this.objectTypes.filter(o => o.order > -1).forEach(o => {
        //    paletteModel.push({
        //        category: 'object',
        //        name: o.name,
        //        objectTypeName: o.objectTypeName,
        //        objectType: o.object,
        //        objectTypeId: o.objectId,
        //        foreColor: o.foreColor,
        //        backColor: o.backColor,
        //        isGroup: false,
        //        diagramObjectType: DiagramObjectType.Node,
        //        visible: true,
        //        order: o.order
        //    });
        //});



        let pt: go.Palette = this.g(go.Palette, "LineagePalette",
            {
                "animationManager.duration": 400,
                nodeTemplateMap: this.myDiagram.nodeTemplateMap,
                groupTemplateMap: this.myDiagram.groupTemplateMap,
                model: new go.GraphLinksModel(paletteModel),
                layout: this.g(go.GridLayout, {
                    sorting: go.GridLayout.Ascending,
                    comparer: (a, b) => this.compareObjects(a.data, b.data)
                })//GridLayout.Forward preserves order
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
                        wrappingColumn: 1,
                        sorting: go.GridLayout.Ascending,
                        comparer: (a, b) => this.compareObjects(a.data, b.data)
                    }),
                isSubGraphExpanded: false
            },
            new go.Binding("background", "isHighlighted", (h, p) => { return h ? "#faffad" : '#eee' }).ofObject(),
            this.g(go.Shape, "RoundedRectangle",
                { fill: null, stroke: "gray", strokeWidth: 2 }
                , new go.Binding("stroke", "valid", v => { return v ? 'gray' : '#f00' })),
            this.g(go.Panel, "Vertical",
                { defaultAlignment: go.Spot.Left, margin: 4 },
                this.g(go.Panel, "Horizontal",
                    { defaultAlignment: go.Spot.Top },
                    // the SubGraphExpanderButton is a panel that functions as a button to expand or collapse the subGraph
                    this.g("SubGraphExpanderButton", new go.Binding("visible", "order", o => { return o != -1; })),
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
                    }),
                // the group begins unexpanded;
                // upon expansion, a Diagram Listener will generate contents for the group
                isSubGraphExpanded: true
            },
            new go.Binding("background", "isHighlighted", (h) => { return h ? "#faffad" : "#f9f9f9"; }).ofObject(),
            this.g(go.Shape, "Rectangle",
                { fill: null, stroke: "#000", strokeWidth: 1, strokeDashArray: [4, 2] },
                new go.Binding("stroke", "valid", v => { return v ? '#000' : '#f00' })),
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
