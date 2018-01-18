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
    PredicateInfo,
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
    @ViewChild('gb') globalFilterRef;

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
    private errors = [];

    private source: string;
    private sourceId: number;
    private target: string;
    private targetId: string;

    private intersectTypes = [];
    private filteredIntersectTypes = [];
    private objectTypes = [];
    private selectedAssetTypeId;
    private objects = [];
    private selectedObjects;
    private objectsLoading = false;
    private totalRecords = 0;
    private gridIsLoading = false;

    public diagramMode: DiagramMode = DiagramMode.Diagram;
    DiagramMode = DiagramMode;

    //control properties
    private isWindowVisible = false;
    private showNodeTabs = false;
    private showLinkTabs = false;
    private showEditTab = false;
    private showInfoTab = false;
    private showDetail = false;
    public menuItems: MenuItem[] = [];
    private editorMenuItems: MenuItem[] = [];
    private tab: string = 'info';
    private headerText = '';
    private diagramOffset = 291;
    private overlayOffset = 391;
    private overlayMaxHeight = 700;
    private overlayWidth = 500;
    private history = [];

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
        this.history.push({
            object: this.objectType,
            objectId: this.objectID
        });
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

        this.diagram.linkTemplateMap.add('', this.createDefaultLink());
        this.diagram.linkTemplateMap.add('adding', this.createPendingAddLink());
        this.diagram.linkTemplateMap.add('deleting', this.createPendingDeleteLink());

        this.diagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        this.diagram.addDiagramListener('LinkDrawn', e => this.LinkDrawn(e));
        this.diagram.addDiagramListener('BackgroundSingleClicked', e => this.BackgroundSingleClicked(e));

        this.diagram.toolManager.linkingTool.linkValidation = (a, b, c, d) => this.canLink(a, b, c, d);

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.diagram.toolManager.linkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.diagram.toolManager.relinkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.diagram.toolManager.linkingTool.isEnabled = !this.readonly;
        this.diagram.toolManager.linkingTool.archetypeLinkData = new LinkModelV2();

        this.diagram.allowDrop = true;

        console.log('initializeDiagram', this.diagram);

        return this.populateDiagram();
    }

    private populateDiagram(): Promise<any> {
        this.isLoading = true;
        let windowVisible = this.isWindowVisible;

        this.isWindowVisible = false;

        return this.lineageService.getLineageDiagram(this.objectType, this.objectID)
            .then(data => {
                //console.log(data);
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

                if (d.key != null) //if the key is not passed let gojs assign it and invalidate the placeholder node
                    model.key = d.key;
                else
                    model.valid = false;

                model.assetId = d.assetId;
                model.assetTypeId = d.assetTypeId;
                model.object = d.object;
                model.objectId = d.objectId;
                model.objectType = d.objectType;
                model.objectTypeId = d.objectTypeId;

                model.objectTypeName = d.objectTypeName;
                model.name = d.name;
                model.foreColor = d.foreColor;
                model.backColor = d.backColor;
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
                link.predicate = d.predicate;
                link.intersectTypeId = d.intersectTypeId;

                if (link.state == 0)
                    link.category = 'adding';
                else if (link.state == 2)
                    link.category = 'deleting';
                else
                    link.category = '';

                linkList.push(link);

            }
        }

        //combine links with same source/target and different predicates
        for (let i = 0; i < linkList.length; i++) {
            let l = linkList[i];
            let others = linkList.filter(k => k.to == l.to && k.from == l.from && k.intersectTypeId != l.intersectTypeId);
            //console.log('dedupe', l, others);

            l.predicates.push({
                intersectTypeId: l.intersectTypeId,
                intersectId: l.intersectId,
                name: l.predicate
            });

            l.predicate = null;
            l.intersectTypeId = 0;
            l.intersectId = 0;

            if (others.length > 0) {
                for (let j = 0; j < others.length; j++) {
                    let k = others[j];

                    l.predicates.push({
                        intersectTypeId: k.intersectTypeId,
                        intersectId: k.intersectId,
                        name: k.predicate
                    });

                    let ix = linkList.findIndex(m => m.from == k.from && m.to == k.to && m.intersectTypeId == k.intersectTypeId);
                    if (ix > -1)
                        linkList.splice(ix, 1);
                }
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
            this.showLinkTabs = data.diagramObjectType == DiagramObjectType.Link;
            this.showInfoTab = this.showLinkTabs || this.showNodeTabs;

            if (this.tab != 'info' && this.tab != 'add')
                this.tab = 'info';

            if (!this.showNodeTabs && !this.showLinkTabs) {
                this.isWindowVisible = false;
                return;
            }

            if ((this.tab == 'add' || this.tab == 'edit') && this.readonly) {
                if (this.showInfoTab)
                    this.tab = 'info';
                else
                    this.tab = '';
            } else if (this.tab == 'info' && !this.showInfoTab) {
                if (!this.readonly) {
                    if (this.showLinkTabs)
                        this.tab = 'edit';
                    else
                        this.tab = 'add';

                }
                else {
                    this.tab = '';
                    this.isWindowVisible = false;
                }
            }

            if (this.showNodeTabs) {
                if (this.isWindowVisible == false)
                    this.tab = 'info';
                this.isWindowVisible = true;
            } else if ((this.showLinkTabs && this.readonly) || this.tab == '') {
                this.tab = 'info';
                this.isWindowVisible = true;
            }

            if (this.showNodeTabs && !this.readonly) {
                this.tab = 'add';
                this.isWindowVisible = true;
            }

            if (this.showLinkTabs && !this.readonly) {
                this.tab = 'edit';
                this.isWindowVisible = true;
            }


        } else {
            if (!this.readonly) {
                this.tab = 'add';
                this.showNodeTabs = false;
                this.showLinkTabs = false;
                this.showInfoTab = false;
                this.isWindowVisible = false;
            } else {
                this.showNodeTabs = false;
                this.showLinkTabs = false;
                this.showInfoTab = false;
                this.isWindowVisible = false;
                this.tab = '';
            }
        }
    }

    private toggleDetail()
    {
        this.showDetail = !this.showDetail;

        if (this.showDetail) {
            this.overlayWidth = 1000;
            this.overlayMaxHeight = 700;
        } else {
            this.overlayWidth = 500;
            this.overlayMaxHeight = 700;
        }
    }

    private loadMenuItems() {
        this.menuItems = [];

        if (this.readonly)
            this.menuItems.push({
                icon: 'fa-pencil',
                items: null,
                title: 'Edit Lineage'
            });
        if (!this.readonly)
            this.menuItems.push({
                icon: 'fa-floppy-o',
                items: null,
                title: 'Save Lineage'
            });

        this.menuItems.push({
            icon: 'fa-search-plus',
            items: null,
            title: 'Zoom in'
        });
        this.menuItems.push({
            icon: 'fa-search-minus',
            items: null,
            title: 'Zoom out'

        });
        this.menuItems.push({
            icon: 'fa-info-circle',
            items: null,
            title: 'Show/Hide Info'
        });

        if (!this.readonly)
            this.menuItems.push({
                icon: 'fa-remove',
                items: null,
                title: 'Cancel Changes'
            });

        if (this.history.length > 1) {
            let hist = {
                icon: 'fa-history',
                items: [],
                title: 'Navigation History'
            };

            this.history.forEach(h => {
                hist.items.push({
                    icon: null,
                    title: h.object + h.objectId.toString(),
                    items: null,
                    command: () => {
                        this.objectType = h.object;
                        this.objectID = h.objectId;
                        this.populateDiagram();
                    }
                });
            });
        }

    }

    private toggleReadOnly(readonly?: boolean) {
        if (readonly != null) this.readonly = readonly;
        //console.log('toggelReadOnly', this.readonly);
        let dt = this.diagram.toolManager.diagram
        dt.allowDelete = !this.readonly;
        dt.allowClipboard = !this.readonly;
        dt.allowCopy = !this.readonly;
        dt.allowInsert = !this.readonly;
        dt.allowLink = !this.readonly;
        dt.allowRelink = !this.readonly;
        dt.allowGroup = !this.readonly;
        dt.allowTextEdit = !this.readonly;

        this.diagram.toolManager.linkingTool.isEnabled = !this.readonly;

        this.loadMenuItems();

        if (this.readonly == false)
            this.loadIntersectTypes()
                .then(() => this.loadObjectTypes());

    }

    private loadObjectTypes(): Promise<any> {
        if (this.objectTypes != null && this.objectTypes.length > 0)
            return Promise.resolve();
        return this.lineageService.getLineageObjectTypes()
            .then(r => {
                this.objectTypes = r;
            })
    }

    private loadIntersectTypes(): Promise<any> {
        if (this.intersectTypes != null && this.intersectTypes.length > 0)
            return Promise.resolve();
        return this.lineageService.getLineageIntersectTypes()
            .then(r => {
                this.intersectTypes = [];
                r.forEach(i => {
                    i.name = i.predicateName;
                    i.intersectId = 0;
                    this.intersectTypes.push(i);
                });
            });
    }

    private lazyLoad(e: any) {
        //console.log('lazyLoad', e);
        this.lineageService.getLineageObjects(+this.selectedAssetTypeId, e.first, e.rows, e.globalFilter)
            .then(r => {
                this.objects = r.results;
                if ((e.globalFilter != null && e.globalFilter != "") || e.first == 0)
                    this.totalRecords = r.count;
            });
    }

    private selectObjectType(e: any) {
        this.selectedAssetTypeId = e;
        this.lazyLoad({
            first: 0,
            rows: 10,
            globalFilter: (this.globalFilterRef != null && this.globalFilterRef.nativeElement != null) ? this.globalFilterRef.nativeElement.value : '' 
        });
    }

    private loadObjects(): Promise<any> {
        if (_.isNaN(+this.selectedAssetTypeId)) {
            this.objects = [];
            this.selectedObjects = null;
            this.totalRecords = 0;
            return;
        }

        this.objectsLoading = true;
        return this.lineageService.getLineageObjects(+this.selectedAssetTypeId, 0, 25, null)
            .then(r => {
                this.selectedObjects = null;
                this.objects = r.results;
                this.totalRecords = r.count;
                this.objectsLoading = false;
                //console.log('loadObjects', this.objects);
            });
    }

    private add() {
        if (this.selectedObjects == null || this.selectedObjects.length < 1)
            return;

        this.diagram.startTransaction('Add Objects');
        //console.log('add', this.selectedObjects, this.objects);
        this.selectedObjects.forEach(s => {
            let m = new NodeModelV2();
            m.assetId = s.assetId;
            m.backColor = s.backColor;
            m.foreColor = s.foreColor;
            m.name = s.name;
            m.object = s.object;
            m.objectId = s.objectId;
            m.category = 'object';
            m.objectTypeName = s.typeName;
            m.assetTypeId = s.assetTypeId;
            m.valid = false;
            this.diagram.model.addNodeData(m);

        });

        this.selectedObjects = null;

        this.diagram.commitTransaction('Add Objects');
    }

    private save() {
        let model = new LineageEditorModelV2();

        model.Object = this.objectType;
        model.ObjectID = this.objectID;

        this.initialLinks.forEach(l => {
            let la = <any>l;
            let ln = new LinkModelV2();
            ln.intersectId = la.intersectId;
            ln.intersectTypeId = la.intersectTypeId;
            ln.from = la.from;
            ln.to = la.to;
            ln.predicates = la.predicates;

            //split back into multiple links
            if (ln.predicates.length >= 1) {
                ln.predicates.forEach(p => {
                    let lns = new LinkModelV2();
                    lns.from = ln.from;
                    lns.to = ln.to;
                    lns.intersectId = p.intersectId;
                    lns.intersectTypeId = p.intersectTypeId;
                    lns.predicate = p.name;
                    model.OriginalLinks.push(lns);
                });
            } else {
                model.OriginalLinks.push(ln);
            }

        });

        this.diagramModelAsGraph().linkDataArray.forEach(l => {
            let la = <any>l;
            let ln = new LinkModelV2();
            ln.intersectId = la.intersectId;
            ln.intersectTypeId = la.intersectTypeId;
            ln.from = la.from;
            ln.to = la.to;
            ln.predicates = la.predicates;

            //split back into multiple links
            if (ln.predicates.length >= 1) {
                ln.predicates.forEach(p => {
                    let lns = new LinkModelV2();
                    lns.from = ln.from;
                    lns.to = ln.to;
                    lns.intersectId = p.intersectId;
                    lns.intersectTypeId = p.intersectTypeId;
                    lns.predicate = p.name;
                    model.Links.push(lns);
                });
                
            } else {
                model.Links.push(ln);
            }
        });

        this.diagram.model.nodeDataArray.forEach(n => {
            let na = <any>n;
            let nn = new NodeModelV2();
            nn.key = na.key;
            nn.assetId = na.assetId;
            nn.assetTypeId = na.assetTypeId;
            nn.object = na.object;
            nn.objectId = na.objectId;
            nn.objectType = na.objectType;
            nn.objectTypeId = na.objectTypeId;

            model.Nodes.push(nn);
        });

        console.log('save', model);
        this.isLoading = true;
        this.lineageService.postLineageDiagram(model)
            .then(r => {
                console.log('save response', r);
                this.isLoading = false;
                if (r != null && r.type != null)
                    this.showMessageForResult(this.messagesService, r);
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
        let valid = true;

        let inCount = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).to == n.key).length;
        let outCount = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).from == n.key).length;

        if (inCount < 1 && outCount < 1) {
            valid = false;
        }

        this.diagram.model.setDataProperty(n, 'valid', valid);
        return valid;

    }

    private validateLink(l: LinkModelV2) {
        let valid = true;

        if (l.predicates.length < 1)
            valid = false;

        //if ((l.intersectId <= 0 || l.intersectId == null) && (l.intersectTypeId <= 0 || l.intersectTypeId == null))
        //    valid = false;

        this.diagram.model.setDataProperty(l, 'valid', !valid);
        this.diagram.model.setDataProperty(l, 'valid', valid);
        //console.log('validateLink', valid, l);
        return valid;
    }

    private valid(): boolean {
        this.errors = [];
        let invalidNodes = this.diagram.model.nodeDataArray.filter(n => (<any>n).valid == false);
        let invalidLinks = this.diagramModelAsGraph().linkDataArray.filter(l => (<any>l).valid == false);

        if (invalidNodes.length > 0) {
            this.errors.push('There are one or more invalid nodes on the diagram');
        }

        if (invalidLinks.length > 0) {
            this.errors.push('There are one or more invalid links on the diagram');
        }

        if (invalidLinks.length > 0 || invalidNodes.length > 0)
            return false;
        return true;
    }

    private setSourceValues(data: any) {
        if (!data || data == null) {
            this.source = null;
            this.sourceId = null;
            this.target = null;
            this.targetId = null;
        } else {
            if (data.diagramObjectType == DiagramObjectType.Link) {

                var from = this.diagram.model.findNodeDataForKey(data.from);
                var to = this.diagram.model.findNodeDataForKey(data.to);

                //console.log('setSourceValues', from, to);
                if (from != null && to != null) {
                    this.filteredIntersectTypes = this.intersectTypes.filter(i => (i.subjectAssetTypeId == from.assetTypeId && i.objectAssetTypeId == to.assetTypeId) || (i.objectAssetTypeId == from.assetTypeId && i.subjectAssetTypeId == to.assetTypeId));
                } else {
                    this.filteredIntersectTypes = [];
                }

                if (data.intersectTypeId == 0 && this.filteredIntersectTypes.length == 1) {
                    this.changeIntersectType(this.filteredIntersectTypes[0]);
                }
            }
        }
    }

    private canLink(fromNode: any, fromPort: any, toNode: any, toPort: any) {

        //console.log('canLink', fromNode, toNode);
        //can't link to self
        if (fromNode.data.key == toNode.data.key)
            return false;

        let intersects = this.intersectTypes.filter(i => (fromNode.data.assetTypeId == i.subjectAssetTypeId && toNode.data.assetTypeId == i.objectAssetTypeId) || (fromNode.data.assetTypeId == i.objectAssetTypeId && toNode.data.assetTypeId == i.subjectAssetTypeId));

        if (intersects == null || (intersects.length != null && intersects.length < 1)) {
            return false;
        }

        if (intersects.length > 0) {
            return true;
        }

        return false;
    }

    //#endregion

    //#region events

    private changeIntersectType(e: any) {
        if (e == null)
            e = [];
        this.selectedData.predicates = [...e];
        let link = (<go.GraphLinksModel>this.diagram.model).linkDataArray.find(l => (<any>l).from == this.selectedData.from && (<any>l).to == this.selectedData.to);
        //let intersectType = this.intersectTypes.find(i => i.intersectTypeId == +e);
        if (link != null) {
            console.log('changeIntersectType', link, e);
            this.diagram.model.setDataProperty(link, 'predicates', null);
            this.diagram.model.setDataProperty(link, 'predicates', [...e]);
            this.diagram.model.setDataProperty(link, 'text', null); //force getters to update
            this.diagram.model.setDataProperty(link, 'fullText', null);
            this.validateLink(<LinkModelV2>link);
        }
        //console.log('changeIntersectType', this.selectedData.intersectTypeId, e, link);
    }

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

        let dOffset = this.diagramOffset - 125;
        let oOffset = this.overlayOffset - 125;
        this.diagramRef.nativeElement.style.height = (window.innerHeight - dOffset) + 'px';
        this.overlayMaxHeight = window.innerHeight - oOffset;

    }

    private zoomDiagram(v: number) {
        if (v < .1 || v > 2.5)
            return;
        this.diagram.scale = v;
        //console.log('zoomDiagram', v, this.diagram);
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
                //this.history.push({
                //    object: this.objectType,
                //    objectId: this.objectID
                //});
                //this.loadMenuItems();
                this.populateDiagram();
            }
        }
        return;
    }

    private BackgroundSingleClicked(e: any) {
        this.selectedData = null;
        this.refreshControls(null);
    }

    private SelectionDeleted(e: any) {

    }

    private ExternalObjectsDropped(e: any) {
        //console.log(e, this.myDiagram.selection);
    }

    private LinkDrawn(e: any) {
        //console.log('LinkDrawn', e);
        let data = e.subject.data;

        let fromNode = this.diagram.model.findNodeDataForKey(data.from);
        let toNode = this.diagram.model.findNodeDataForKey(data.to);
        let link = (<go.GraphLinksModel>this.diagram.model).linkDataArray.find(l => (<any>l).to == data.to && (<any>l).from == data.from);

        if (link == null || fromNode == null || toNode == null) {
            return;
        }

        this.diagram.startTransaction('Link Drawn');

        this.selectedData = link;
        this.refreshControls(this.selectedData);

        this.validateNode(fromNode);
        this.validateNode(toNode);

        if ((<any>link).intersectTypeId == null || (<any>link).intersectTypeId < 1) {
            let intersects = this.intersectTypes.filter(i => (fromNode.assetTypeId == i.subjectAssetTypeId && toNode.assetTypeId == i.objectAssetTypeId) || (fromNode.assetTypeId == i.objectAssetTypeId && toNode.assetTypeId == i.subjectAssetTypeId));

            if (intersects.length == 1) {
                this.diagram.model.setDataProperty(link, 'intersectTypeId', intersects[0].intersectTypeId);
                this.diagram.model.setDataProperty(link, 'predicate', intersects[0].predicateName);
            }
            else {
                this.diagram.model.setDataProperty(link, 'intersectTypeId', 0);
                this.diagram.model.setDataProperty(link, 'predicate', null);
            }
        }

        this.validateLink(<LinkModelV2>link);

        this.diagram.commitTransaction('Link Drawn');
    }

    public menuClick(e: MenuItem) {
        if (e.icon == 'fa-info-circle') {
            this.isWindowVisible = !this.isWindowVisible;
        } else if (e.icon == 'fa-pencil') {
            this.toggleReadOnly(false);
            this.toggleTabs(this.selectedData);
            this.setSourceValues(this.selectedData);
            this.isWindowVisible = true;
        } else if (e.icon == 'fa-floppy-o') {
            if (!this.isLoading) {
                if (!this.valid()) {
                    this.messagesService.showError('', this.errors.join('\n'));
                    return;
                } else {
                    this.save();
                    this.toggleReadOnly(true);
                    this.toggleTabs(this.selectedData);
                }
            }
            
        } else if (e.icon == 'fa-remove') {
            this.toggleReadOnly(true);
            this.populateDiagram();
            this.loadMenuItems();
        } else if (e.icon == 'fa-search-plus') {
            this.zoomDiagram(this.diagram.scale + .1);
        } else if (e.icon == 'fa-search-minus') {
            this.zoomDiagram(this.diagram.scale - .1);
        }
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
                columnSpacing: 30
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

    private createObjectNode(): go.Node {
        let nodeWidth = 150;
        let nodeHeight = 75;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 8;

        return this.g(go.Node, "Spot",
            new go.Binding("location", "pos", s => go.Point.parse(s)).makeTwoWay(go.Point.stringify),
            {
                locationSpot: go.Spot.Center,
                mouseEnter: (e, obj) => { this.showPorts(obj.part, true); },
                mouseLeave: (e, obj) => { this.showPorts(obj.part, false); }
            },
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
                        if (this.readonly || data == null) return 'transparent';
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
            ),
            this.makePort('L', go.Spot.Left, false, true),
            this.makePort('R', go.Spot.Right, true, false)
        );
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
                new go.Binding("stroke", "valid", function (h) { return h || this.readonly ? "gray" : "#f00" }),
                {
                    toolTip: this.bindTooltip("fullText")
                }
            ),
            this.g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" },
                new go.Binding("stroke", "valid", function (h) { return h || this.readonly ? "gray" : "#f00" }),
                new go.Binding("fill", "valid", function (h) { return h || this.readonly ? "gray" : "#f00" })), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(go.Shape, {
                    visible: false,
                    fill: this.g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                    stroke: null,
                    //strokeDashArray: [3, 2]
                },
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) { return (a ? true : false) })
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                },
                    // the label
                    new go.Binding("text", "text")
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
                    stroke: null,
                    //strokeDashArray: [3, 2]
                },
                    //only visible if there's a label
                    new go.Binding("visible", "text", function (a) { return (a ? true : false) })
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4,
                },
                    // the label
                    new go.Binding("text", "text")
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
                    stroke: null,
                    //strokeDashArray: [3, 2]
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

    private makePort(name, spot, output, input) {
        return this.g(go.Shape, "Circle",
            {
                fill: "transparent",
                stroke: null,
                desiredSize: new go.Size(7, 7),
                alignment: spot, alignmentFocus: spot,
                portId: name,
                fromSpot: spot, toSpot: spot,
                fromLinkable: output, toLinkable: input,
                cursor: "pointer"
            });
    }

    private showPorts(node, show) {
        let diagram = node.diagram;
        if (!diagram || this.readonly || !diagram.allowLink) return;

        node.ports.each((port) => {
            port.stroke = (show ? (node.data.foreColor || '#fff') : null);
        });
    }

    private showTooltip(text: string): go.Adornment {
        return this.g(go.Adornment, "Auto",
            this.g(go.Shape, { fill: "#333" }),
            this.g(go.TextBlock, { margin: 4, text: text, stroke: "#fff" }
        ));
    }

    private bindTooltip(prop: string): go.Adornment {
        return this.g(go.Adornment, "Auto",
            this.g(go.Shape, { fill: "#333" }),
            this.g(go.TextBlock, { margin: 4, stroke: "#fff" },
                new go.Binding("text", prop)
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
