import { Component, Input, OnInit, AfterViewInit, ElementRef, OnDestroy, ViewChild, Renderer, HostListener, SimpleChanges } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { PermissionsService } from '../../../../services/permissions.service';
import { DiagramService } from '../../../../services/diagram.service';
import { LineageService } from '../../../../services/lineage.service';
import { MessagesService } from '../../../../services/messages.service';
//import { JsonResult } from '
import {
    DiagramObjectType,
    LineageLink,
    LineageNode,
    LineageView,
    LineageEditorModelV2,
    PredicateInfo,
} from '../../../../models/lineage.model';

import { MenuItem } from 'primeng/primeng';

import * as go from 'gojs';
import * as _ from 'lodash';
import { Subject } from 'rxjs';

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
    private addObjectsWarning = "";
    private objectSource$ = new Subject<any>();
    private objectSearchSub: any;
    private selectedDetailPredicate = null;

    public diagramMode: DiagramMode = DiagramMode.Diagram;
    DiagramMode = DiagramMode;

    //control properties
    private isWindowVisible = false;
    private showNodeTabs = false;
    private showLinkTabs = false;
    private showEditTab = false;
    private showInfoTab = false;
    private showDetail = true;
    public menuItems: MenuItem[] = [];
    private editorMenuItems: MenuItem[] = [];
    private tab: string = 'info';
    private headerText = '';
    private diagramOffset = 291;
    private overlayOffset = 391;
    private overlayMaxHeight = 700;
    private overlayWidth = 700;
    private history = [];
    private showPredicateNames = true;

    private canAdd: boolean = false;
    private canEdit: boolean = false;
    private canDelete: boolean = false;

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

        this.objectSearchSub = this.lineageService.getLineageObjects(this.objectSource$)
            .subscribe(res => {
                                this.objects = res.results.results;
                if ((res.event.globalFilter != null && res.event.globalFilter != "") || res.event.first == 0)
                    this.totalRecords = res.results.count;

                this.objectsLoading = false;
            });

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

        this.objectSearchSub.unsubscribe();
    }

    //#region helper methods

    private initializeDiagram(): Promise<any> {    

        if (this.diagram != null) {
            this.reOrderLayout();
            return Promise.resolve();
        }

        this.diagram = this.createDiagram();

        this.diagram.nodeTemplateMap.add('object', this.createObjectNode());
        this.diagram.nodeTemplateMap.add('focal', this.createFocalNode());

        this.diagram.linkTemplateMap.add('', this.createDefaultLink());
        this.diagram.linkTemplateMap.add('adding', this.createPendingAddLink());
        this.diagram.linkTemplateMap.add('deleting', this.createPendingDeleteLink());
        this.diagram.linkTemplateMap.add('deleted', this.createDeletedLink());

        this.diagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        this.diagram.addDiagramListener('LinkDrawn', e => this.LinkDrawn(e));
        this.diagram.addDiagramListener('BackgroundSingleClicked', e => this.BackgroundSingleClicked(e));
        this.diagram.addDiagramListener('InitialLayoutCompleted', () => this.InitialLayoutCompleted());

        this.diagram.toolManager.linkingTool.linkValidation = (a, b, c, d) => this.canLink(a, b, c, d);
        this.diagram.commandHandler.deleteSelection = () => this.deleteSelection();

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(8, 8);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.diagram.toolManager.linkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.diagram.toolManager.relinkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.diagram.toolManager.linkingTool.isEnabled = !this.readonly;
        this.diagram.toolManager.linkingTool.archetypeLinkData = new LineageLink();
        this.diagram.toolManager.mouseWheelBehavior = go.ToolManager.WheelNone;

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
                
                //workaround for IE11 not respecting initial diagram scale. 
                //After layout is complete or a zoom button is pressed it is automatically set back to go.Diagram.None
                this.diagram.autoScale = go.Diagram.UniformToFill;
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
                var model = new LineageNode();

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
                model.category = (d.object == this.objectType && d.objectId == this.objectID) ? 'focal' : 'object';

                modelList.push(model);
            }
        }

        if (data.links) {
            for (var i = 0; i < data.links.length; i++) {
                var d = data.links[i];
                var link = new LineageLink();
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
                else if (link.state == 3)
                    link.category = 'deleted';
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
        this.canAdd = this.hasModifyRelationshipsPermissions();
        this.canEdit = this.hasModifyRelationshipsPermissions();
        this.canDelete = this.hasDeleteRelationshipsPermissions();

        this.setSourceValues(data);
        this.toggleTabs(data);
        this.loadMenuItems();
    }

    private toggleTabs(data: LineageNode | LineageLink) {
        //console.log(this.tab, data);
        if (data) {
            this.showNodeTabs = data.diagramObjectType == DiagramObjectType.Node;
            this.showLinkTabs = data.diagramObjectType == DiagramObjectType.Link;
            this.showInfoTab = this.showLinkTabs || this.showNodeTabs;
            this.selectedDetailPredicate = 0;

            if (this.tab != 'info' && this.tab != 'add')
                this.tab = 'info';

            if (!this.showNodeTabs && !this.showLinkTabs) {
                this.isWindowVisible = false;
                return;
            }

            if ((this.tab == 'add' || this.tab == 'edit')) {
                if (this.readonly)
                    this.tab = 'info';
                if (this.tab == 'add' && !this.canAdd )
                    this.tab = 'info';
                if (this.tab == 'edit' && (!this.canEdit || !(this.canAdd && data.isNew)))
                    this.tab = 'info';
            } else if (this.tab == 'info' && !this.showInfoTab) {
                if (!this.readonly) {
                    if (this.showLinkTabs)
                        if (this.canEdit || (this.canAdd && data.isNew))
                            this.tab = 'edit';
                        else
                            this.tab = 'info';
                    else
                        if (this.canAdd)
                            this.tab = 'add';
                        else
                            this.tab = 'info';
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
                if (this.canAdd || (this.canEdit && data.isNew))
                    this.tab = 'add';
                else
                    this.tab = 'info';
                this.isWindowVisible = true;
            }

            if (this.showLinkTabs && !this.readonly) {
                    this.tab = 'edit';
                this.isWindowVisible = true;
            }


        } else {
            if (!this.readonly) {
                if (this.tab == '')
                    this.tab = 'add';
                this.showNodeTabs = false;
                this.showLinkTabs = false;
                this.showInfoTab = false;
                this.isWindowVisible = true;
            } else {
                this.showNodeTabs = false;
                this.showLinkTabs = false;
                this.showInfoTab = false;
                this.isWindowVisible = false;
                this.tab = '';
            }
        }
    }

    private toggleDetail(showDetail?: boolean)
    {
        this.showDetail = showDetail == null ? !this.showDetail : showDetail;

        if (this.showDetail) {
            this.overlayWidth = 700;
            this.overlayMaxHeight = 700;
        } else {
            this.overlayWidth = 500;
            this.overlayMaxHeight = 700;
        }
    }

    private loadMenuItems() {
        this.menuItems = [];

        //console.log(this.permissions, this.hasModifyRelationshipsPermissions());

        if (this.readonly && (this.canAdd || this.canEdit || this.canDelete)) {
            this.menuItems.push({
                icon: 'fa-pencil',
                items: null,
                title: 'Edit Lineage'
            });
        }

        if (!this.readonly)
            this.menuItems.push({
                icon: 'fa-floppy-o',
                items: null,
                title: 'Save Lineage'
            });

        let top = {
            icon: 'fa-eye',
            items: [{
                icon: null,
                items: null,
                label: 'Toggle Predicate Names'
            }],
            title: ''
        }

        this.menuItems.push(top);

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
        else this.readonly = !this.readonly;
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

        if (this.readonly == false) {
            this.toggleDetail(false);
            this.loadIntersectTypes()
                .then(() => this.loadObjectTypes());
        } else {
            this.toggleDetail(true);
        }

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
        this.objectsLoading = true;
        this.objectSource$.next({ assetTypeId: +this.selectedAssetTypeId, event: e });
    }

    private selectObjectType(e: any) {
        //console.log('selectObjectType', e);        
        this.selectedAssetTypeId = e;
        this.lazyLoad({
            first: 0,
            rows: 10,
            globalFilter: (this.globalFilterRef != null && this.globalFilterRef.nativeElement != null) ? this.globalFilterRef.nativeElement.value : '' 
        });
    }

    private add() {
        if (this.selectedObjects == null || this.selectedObjects.length < 1)
            return;

        this.addObjectsWarning = "";

        this.diagram.startTransaction('Add Objects');
        //console.log('add', this.selectedObjects, this.objects);
        this.selectedObjects.forEach(s => {

            let ix = this.diagram.model.nodeDataArray.findIndex(i => (<any>i).object == s.object && (<any>i).objectId == s.objectId);

            if (ix > -1) {
                if (this.addObjectsWarning == "")
                    this.addObjectsWarning = "The following objects already exist on the lineage and were not added: "

                this.addObjectsWarning += s.name + ', ';
                return;
            }

            let m = new LineageNode();
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

        if (this.addObjectsWarning != "") {
            //remove trailing comma
            this.addObjectsWarning = this.addObjectsWarning.trim();
            this.addObjectsWarning = this.addObjectsWarning.substr(0, this.addObjectsWarning.length - 1);
        }
        this.selectedObjects = null;

        this.diagram.commitTransaction('Add Objects');
    }

    private save() {
        let model = new LineageEditorModelV2();

        model.Object = this.objectType;
        model.ObjectID = this.objectID;

        this.initialLinks.forEach(l => {
            let la = <any>l;
            let ln = new LineageLink();
            ln.intersectId = la.intersectId;
            ln.intersectTypeId = la.intersectTypeId;
            ln.from = la.from;
            ln.to = la.to;
            ln.predicates = la.predicates;

            //split back into multiple links
            if (ln.predicates.length >= 1) {
                ln.predicates.forEach(p => {
                    let lns = new LineageLink();
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
            let ln = new LineageLink();
            ln.intersectId = la.intersectId;
            ln.intersectTypeId = la.intersectTypeId;
            ln.from = la.from;
            ln.to = la.to;
            ln.predicates = la.predicates;

            //split back into multiple links
            if (ln.predicates.length >= 1) {
                ln.predicates.forEach(p => {
                    let lns = new LineageLink();
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
            let nn = new LineageNode();
            nn.key = na.key;
            nn.assetId = na.assetId;
            nn.assetTypeId = na.assetTypeId;
            nn.object = na.object;
            nn.objectId = na.objectId;
            nn.objectType = na.objectType;
            nn.objectTypeId = na.objectTypeId;

            model.Nodes.push(nn);
        });

        //console.log('save', model);
        this.isLoading = true;
        this.lineageService.postLineageDiagram(model)
            .then(r => {
                //console.log('save response', r);
                this.isLoading = false;
                this.populateDiagram();
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

    private validateNode(n: LineageNode) {
        let valid = true;

        let inCount = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).to == n.key).length;
        let outCount = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).from == n.key).length;

        if (inCount < 1 && outCount < 1) {
            valid = false;
        }

        this.diagram.model.setDataProperty(n, 'valid', valid);
        return valid;

    }

    private validateLink(l: LineageLink) {
        let valid = true;

        if (l.intersectTypeId <= 0)
            valid = false;

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
            this.errors.push('Predicate missing for one or more links, please highlight line(s) and select a Predicate');
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
        //console.log('changeIntersectType', e);

        //normalize the event data
        if (e == null)
            e = [];
        if (e.constructor !== Array) {
            e = [e];

        }

        //this.selectedData.predicates = [...e];
        let link: LineageLink = <LineageLink>(<go.GraphLinksModel>this.diagram.model).linkDataArray.find(l => (<any>l).from == this.selectedData.from && (<any>l).to == this.selectedData.to);
        //let intersectType = this.intersectTypes.find(i => i.intersectTypeId == +e);
        if (link != null) {
            if (e.length > 0) {
                this.diagram.model.setDataProperty(link, 'intersectTypeId', e[0].intersectTypeId);
                this.diagram.model.setDataProperty(link, 'predicate', e[0].predicateName);
            } else {
                this.diagram.model.setDataProperty(link, 'intersectTypeId', 0);
                this.diagram.model.setDataProperty(link, 'predicate', null);
            }
        }

        this.diagram.model.setDataProperty(link, 'predicates', [...e]);
        (<LineageLink>this.selectedData).predicates = [...e];
        this.diagram.model.setDataProperty(link, 'text', null);
        this.diagram.model.setDataProperty(link, 'fullText', null);
        this.validateLink(<LineageLink>link);

        this.selectedData = <LineageLink>(<go.GraphLinksModel>this.diagram.model).linkDataArray.find(l => (<any>l).from == this.selectedData.from && (<any>l).to == this.selectedData.to);

        //console.log('changeIntersectType', this.selectedData, e, <LinkModelV2>(<go.GraphLinksModel>this.diagram.model).linkDataArray.find(l => (<any>l).from == this.selectedData.from && (<any>l).to == this.selectedData.to));

    }


    private changeNode(e: LineageNode) {

        let node: LineageNode = this.diagram.model.findNodeDataForKey(e.key);
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
        this.diagram.autoScale = go.Diagram.None;
        if (v < .1 || v > 2.5)
            return;
        this.diagram.scale = v;
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
                if (sel[0].data.diagramObjectType == DiagramObjectType.Link) {
                    this.selectedData = <LineageLink>sel[0].data;
                } else {
                    this.selectedData = <LineageNode>sel[0].data;
                }

                //console.log('ChangedSelection', _.cloneDeep(this.selectedData));
                
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

    private BackgroundSingleClicked(e: any) {
        this.selectedData = null;
        this.refreshControls(null);
    }

    private deleteSelection() {
        //console.log('delete', this.diagram.selection);

        this.diagram.startTransaction("Delete");

        let sel = this.diagram.selection.toArray();
        let deleteParts: go.Part[] = [];
        
        sel.forEach(n => {
            if (n.data.diagramObjectType == DiagramObjectType.Node) {
                //focal node cannot be deleted
                if (n.data.object == this.objectType && n.data.objectId == this.objectID)
                    return;

                //user doesn't have permission to delete existing nodes
                if (!this.canDelete && !n.data.isNew)
                    return;

                deleteParts.push(n);
            }
            else if (n.data.diagramObjectType == DiagramObjectType.Link) {
                if (!this.canDelete && !n.data.isNew)
                        return;
                deleteParts.push(n);
            }
        });

        this.diagram.removeParts(deleteParts, false);
        this.diagram.commitTransaction("Delete");
    }
    
    private LinkDrawn(e: any) {
        //console.log('LinkDrawn', e);
        let data = e.subject.data;

        let fromNode = this.diagram.model.findNodeDataForKey(data.from);
        let toNode = this.diagram.model.findNodeDataForKey(data.to);
        let link: LineageLink = <LineageLink>(<go.GraphLinksModel>this.diagram.model).linkDataArray.find(l => (<any>l).to == data.to && (<any>l).from == data.from);

        if (link == null || fromNode == null || toNode == null) {
            return;
        }

        this.diagram.startTransaction('Link Drawn');

        //this.selectedData = link;
        //this.refreshControls(this.selectedData);

        this.validateNode(fromNode);
        this.validateNode(toNode);

        if ((<any>link).intersectTypeId == null || (<any>link).intersectTypeId < 1) {
            let intersects = this.intersectTypes.filter(i => (fromNode.assetTypeId == i.subjectAssetTypeId && toNode.assetTypeId == i.objectAssetTypeId) || (fromNode.assetTypeId == i.objectAssetTypeId && toNode.assetTypeId == i.subjectAssetTypeId));

            if (intersects.length == 1) {
                this.diagram.model.setDataProperty(link, 'intersectTypeId', intersects[0].intersectTypeId);
                this.diagram.model.setDataProperty(link, 'predicate', intersects[0].predicateName);
                this.diagram.model.setDataProperty(link, 'predicates', [...intersects]);
                this.diagram.model.setDataProperty(link, 'text', null);
                this.diagram.model.setDataProperty(link, 'fullText', null);
            }
            else {
                this.diagram.model.setDataProperty(link, 'intersectTypeId', 0);
                this.diagram.model.setDataProperty(link, 'predicate', null);
                this.diagram.model.setDataProperty(link, 'text', null);
                this.diagram.model.setDataProperty(link, 'fullText', null);
            }

        }

        this.validateLink(<LineageLink>link);
        this.selectedData = <LineageLink>(<go.GraphLinksModel>this.diagram.model).linkDataArray.find(l => (<any>l).to == data.to && (<any>l).from == data.from);
        this.refreshControls(this.selectedData);
        //console.log('linkdrawn', this.selectedData, link)
        this.diagram.commitTransaction('Link Drawn');
    }

    private InitialLayoutCompleted() {
        this.diagram.scrollMode = go.Diagram.InfiniteScroll;
    }

    public menuClick(e: MenuItem) {
        //console.log('menuClick', e);
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
        } else if (e.icon == null && e.label == 'Toggle Predicate Names') {
            this.showPredicateNames = !this.showPredicateNames;
            this.diagramModelAsGraph().linkDataArray.forEach(l => {
                this.diagram.model.setDataProperty(l, "text", null);
            });

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
            layout: this.g(go.LayeredDigraphLayout, {
                direction: 0,
                layerSpacing: 25,
                columnSpacing: 25
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
        let nodeWidth = 150 * 1.15;
        let nodeHeight = 75 * 1.35;
        let nodeBorderColor = '#000';
        let nodeFontSize = 10;

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
                    isPanelMain: true,
                    strokeWidth: 3,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape"
                },

                    new go.Binding("fill", "foreColor"),
                    new go.Binding("stroke", "valid", (v, m) => {
                        let data = m.panel.panel.data;
                        if (data == null) return 'transparent';
                        return data.backColor;
                    })
                ),
                this.g(go.Shape, "RoundedRectangle", {
                    strokeWidth: 0,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    desiredSize: new go.Size(nodeWidth - 10, nodeHeight - 10),
                    name: "NodeShape2",
                    fill: '#000',
                    stroke: 'transparent',
                },
                    new go.Binding("fill", "backColor"),
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
                        maxSize: new go.Size(nodeHeight, NaN),
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
                new go.Binding("stroke", "valid", function (h) { return h  ? "gray" : "#f00" }),
                {
                    toolTip: this.bindTooltip("fullText")
                }
            ),
            this.g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" },
                new go.Binding("stroke", "valid", function (h) { return h  ? "gray" : "#f00" }),
                new go.Binding("fill", "valid", function (h) { return h  ? "gray" : "#f00" })), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(go.Shape, {
                    visible: false,
                    fill: this.g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                    stroke: null,
                    //strokeDashArray: [3, 2]
                },
                    //only visible if there's a label
                    //new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) })
                    new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) })
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                },
                    // the label
                    new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) }),
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
                    new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) }),
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4,
                },
                    // the label
                    new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) }),
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
                    new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) }),
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                },
                    // the label
                    new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) }),
                    new go.Binding("text", "text").makeTwoWay()
                )
            )
        );
    }

    private createDeletedLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.Orthogonal,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false,
                //curve: go.Link.Bezier
            }, // the whole link panel
            this.g(go.Shape, {
                stroke: "#c00", strokeWidth: 2, strokeDashArray: [3, 2]
            },
                new go.Binding("strokeWidth", "hasProperties", function (h) { return h ? 3 : 2; }),
                new go.Binding("stroke", "hasProperties", function (h) { return h ? "#c00" : "#c00" }),
                {
                    toolTip: this.showTooltip("Deleted")
                }
            ),
            this.g(go.Shape, { toArrow: "standard", fill: "#c00", stroke: "#c00" }),
            this.g(go.Panel, "Auto",
                this.g(go.Shape, {
                    visible: false,
                    fill: this.g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                    stroke: null,
                    //strokeDashArray: [3, 2]
                },
                    //only visible if there's a label
                    new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) }),
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center", font: "9pt helvetica, arial, sans-serif", stroke: "#000", margin: 4
                },
                    // the label
                    new go.Binding("visible", "text", a => { return (a && this.showPredicateNames ? true : false) }),
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
