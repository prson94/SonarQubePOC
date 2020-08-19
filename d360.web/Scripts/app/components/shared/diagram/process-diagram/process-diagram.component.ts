import * as go from 'gojs';
import * as _ from 'lodash';
import { Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, AfterViewChecked, Output, EventEmitter, HostListener, ViewChild, OnDestroy, Renderer2, ElementRef } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { FlowObjectType, } from '../../../../models/asset.model';
import { FontAwesomeHelper } from '../../../../static/font-awesome-helper';
import { ProcessDiagramTemplates } from './process-diagram.templates';
import { ProcessService } from '../../../../services/process.service';
import { DiagramNodeBase } from '../../../../models/process.model';
import { Router } from '@angular/router';
import { LinkLabelOnPathDraggingTool } from 'gojs/extensionsTS/LinkLabelOnPathDraggingTool';
import { DynEditorService } from '../../../../services/dyn-editor.service';
import { HeaderActionsService } from '../../../../services/header-actions.service';
import { HeaderActions } from '../../../../models/header.model';

@Component({
    selector: 'd3s-process-diagram',
    templateUrl: './process-diagram.component.html',
    providers: [ProcessService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramComponent extends DiagramBaseComponent implements OnInit, AfterViewChecked, OnDestroy {
    @Input() isEditMode: boolean = false;
    @Input() isFullScreen: boolean = false;
    @Input() assetUid: string = '';

    @Output() editModeClosed: EventEmitter<any> = new EventEmitter<any>();
    @Output() saveState: EventEmitter<any> = new EventEmitter<any>();

    public viewType: string = 'diagram';

    public myDiagram: go.Diagram;
    private assetDetail: any;

    isPalleteLoaded: boolean = false;

    eventPalleteHeight: number = 300;
    myEventPalette: go.Diagram;

    activityPalleteHeight: number = 300;
    myActivityPallete: go.Diagram;

    gatewayPalleteHeight: number = 300;
    myGatewayPallete: go.Diagram;

    processDiagramBase64: string = '';

    private assetTypeNodes: DiagramNodeBase[] = [];
    private events: DiagramNodeBase[] = [];
    private activities: DiagramNodeBase[] = [];
    private gateways: DiagramNodeBase[] = [];
    private colors: any[] = [];
    private diagramOriginalPosition: any = null;

    private isLoaded = false;
    public isDiagramLoaded = false;
    private isSaveDisabled: boolean = true;
    public isCanvasEmpty: boolean = true;
    private isSaving: boolean = false;
    private isExporting: boolean = false;
    private defaultStrokeColor: string = '#708EA6';

    public selectedNodeData: any;
    private loadedEditors: any[] = [];

    private isErrorModalOpened: boolean = false;
    private isSavingChangesModalOpened: boolean = false;
    private promptDeleteOpened: boolean = false;
    private isRelatedAssetsVisible: boolean = false;

    private newInstancesMap: any[] = [];

    public isInfoPanelOpened: boolean = false;

    private selectedLinkData: any;
    private nodeNames: string[] = [];

    private initialActions = new HeaderActions();

    @ViewChild('deleteCancelButton', { static: true }) deleteCancelButton: ElementRef;
    @ViewChild('closeSaveButton', { static: true }) closeSaveButton: ElementRef;
    @ViewChild('saveChangesButton', { static: true }) saveChangesButton: ElementRef;

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private headerActionService: HeaderActionsService,
        private processService: ProcessService,
        public cdRef: ChangeDetectorRef,
        private router: Router,
        private renderer: Renderer2,
        public dynEditorService: DynEditorService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }


    ngOnInit() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates


        this.processService.getProcessDiagramColors(this.assetUid)
            .subscribe(colors => {
                this.colors = colors;
            });
        this.processService.getAvailableNodes(this.assetUid)
            .subscribe(res => {
                this.assetTypeNodes = res;
                this.events = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Event);
                this.activities = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Activity);
                this.gateways = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Gateway);

                var nodeHeight = 160;
                var numberOfEventRows = this.events.length % 2 == 0 ? this.events.length / 2 : (this.events.length + 1) / 2;
                this.eventPalleteHeight = numberOfEventRows * nodeHeight;

                var numberOfActivityRows = this.activities.length % 2 == 0 ? this.activities.length / 2 : (this.activities.length + 1) / 2;
                this.activityPalleteHeight = numberOfActivityRows * nodeHeight;

                var numberOfGatewatRows = this.gateways.length % 2 == 0 ? this.gateways.length / 2 : (this.gateways.length + 1) / 2;
                this.gatewayPalleteHeight = numberOfGatewatRows * nodeHeight;

                this.isLoaded = true;
                this.loadDiagram();
            });
    }
    @ViewChild('diagram', { static: false }) diagramRef;
    @ViewChild('editors', { static: false }) editorRef;
    @HostListener('window:resize', ['$event'])
    public onResize(event) {
        if (!this.diagramRef) return;
        let height = window.innerHeight;
        if (this.isEditMode)
            this.diagramRef.nativeElement.style.height = (height - 140) + 'px';
        else if (this.isFullScreen)
            this.diagramRef.nativeElement.style.height = (height - 40) + 'px';
        else
            this.diagramRef.nativeElement.style.height = (height - 240) + 'px';

        if (this.editorRef) {
            this.editorRef.nativeElement.style.height = this.diagramRef.nativeElement.style.height;
        }
        if (this.myDiagram) {
            var diagramPlaceholderWidth = document.getElementById('process-diagram-placeholder').getBoundingClientRect().width;
            this.diagramRef.nativeElement.style.width = diagramPlaceholderWidth + 'px';
            setTimeout(() => {
                if (this.myDiagram)
                    this.myDiagram.redraw();
            }, 100);
        }
        this.cdRef.detectChanges();
    }

    @HostListener('click', ['$event.target'])
    onClick(btn) {
        if (this.myDiagram) {
            if (this.myDiagram.selection.count == 0) {
                this.selectedNodeData = null;
            }
        }
        this.cdRef.detectChanges();

    }

    @HostListener('window:beforeunload', ['$event'])
    canExitPage($event: any): boolean {
        return this.isCurrentStateSaved();
    }
    ngAfterViewChecked() {
        this.onResize(null);
        this.applyEditMode(this.isEditMode);
        if (this.myDiagram) {
            if (this.getSelectedNodeCount() != 1) {
                this.selectedNodeData = null;
            }

            if (this.myDiagram.selection.count == 1) {
                var link = this.myDiagram.selection.toArray()[0];
                if (link.data && link.data.from && link.data.to) {
                    this.selectedLinkData = link.data;
                }
                else {
                    this.selectedLinkData = null;
                }
            }
            else {
                this.selectedLinkData = null;
            }
            if (this.isDiagramLoaded && this.isEditMode)
                this.saveState.emit(this.isCurrentStateSaved());
        }

        if (this.selectedNodeData && this.selectedNodeData.key.indexOf('new_instance_') > -1) {
            var newUid = this.newInstancesMap.find(x => x.oldKey == this.selectedNodeData.key).newKey;
            this.newInstancesMap = [];
            var part = this.myDiagram.findPartForKey(newUid);
            if (part) {
                this.myDiagram.clearSelection();
                this.myDiagram.select(part);
                delete part.data['PopupDescription'];
            }
        }
        this.cdRef.detectChanges();
    }

    ngOnDestroy() {
        if (this.cdRef)
            this.cdRef.detach();
        if (this.myDiagram)
            this.myDiagram = null;
    }

    public changeInfoPanelMode() {
        this.isInfoPanelOpened = !this.isInfoPanelOpened;

        setTimeout(() => {
            this.onResize(null);
        }, 200)
    }

    private applyEditMode(state: boolean) {
        if (!this.myDiagram) return;
        this.myDiagram.nodes.each(function (n) {
            if (n instanceof go.Node) {
                n.movable = state;
            }
        });
        this.myDiagram.links.each(function (n) {
            if (n instanceof go.Link) {
                n.isEnabled = state;
                n.movable = state;
                n.reshapable = state;
            }
        });
        this.myDiagram.isModelReadOnly = !state;
        this.myDiagram.isReadOnly = !state;
        if (this.myDiagram.isReadOnly) {
            this.myDiagram.toolManager.textEditingTool.doCancel();
        }
        if (this.viewType == 'diagram' && this.isEditMode && !this.isPalleteLoaded) {
            this.loadPallete();
        }

        if (this.isEditMode) {
            this.headerActionService.showFavorite = false;
            this.headerActionService.showFollow = false;
            this.headerActionService.showHomePage = false;
            this.headerActionService.showNotifications = false;
            this.headerActionService.showRaiseIssue = false;
            this.headerActionService.showSearch = false;
            this.headerActionService.showShoppingCart = false;
        }
        else {
            this.headerActionService.showFavorite = this.initialActions.showFavorite;
            this.headerActionService.showFollow = this.initialActions.showFollow;
            this.headerActionService.showHomePage = this.initialActions.showHomePage;
            this.headerActionService.showNotifications = this.initialActions.showNotifications;
            this.headerActionService.showRaiseIssue = this.initialActions.showRaiseIssue;
            this.headerActionService.showSearch = this.initialActions.showSearch;
            this.headerActionService.showShoppingCart = this.initialActions.showShoppingCart;
        }
    }

    private discardChanged() {
        this.myDiagram.model = go.Model.fromJson(this.savedState.toJson());
        if (this.actionAfterSaved) {
            this.actionAfterSaved();
            this.isSavingChangesModalOpened = false;
            this.isErrorModalOpened = false;
            this.actionAfterSaved = null;
        }
    }

    private isDeleteEnabled() {
        if (this.myDiagram && this.myDiagram.selection.count > 0) {
            return true;
        }
        else return false;
    }

    private isRelatedAssetsEnabled() {
        if (!this.selectedNodeData)
            return false;

        return true;
    }

    private isExportEnabled() {
        if (!this.myDiagram)
            return false;

        return this.myDiagram.nodes.count > 0;
    }

    private getSelectedNodeCount() {
        if (!this.myDiagram)
            return 0;

        return this.myDiagram.selection.filter(x => x.category == 'activity' || x.category == 'event' || x.category == 'gateway').count;
    }

    private get deleteModelTitle(): string {
        return this.getSelectedNodeCount() > 1 ? 'Delete Selected Items' : 'Delete Selected Item';
    }

    onDeleteClick() {
        this.promptDeleteOpened = true;
        setTimeout(() => this.deleteCancelButton.nativeElement.focus(), 100);
        this.cdRef.detectChanges();
    }

    switchModes() {

        this.isEditMode = !this.isEditMode;
        this.applyEditMode(this.isEditMode);

        if (!this.isEditMode) {
            this.editModeClosed.emit();
        }

        this.cdRef.detectChanges();
    }

    disableDrag() {
        this.myDiagram.toolManager.panningTool.isEnabled = !this.myDiagram.toolManager.panningTool.isEnabled;
    }

    deleteSelectedNode() {
        if (this.isEditMode) {
            this.myDiagram.selection;
            this.myDiagram.selection.each(x => {
                this.myDiagram.remove(x);
            })
        }
    }

    loadDiagram() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        this.myDiagram =
            $(go.Diagram, "diagram",  // must name or refer to the DIV HTML element
                {
                    "undoManager.isEnabled": true,
                    "textEditingTool.doActivate": function () {
                        go.TextEditingTool.prototype.doActivate.call(this);
                        if (this.textBlock) this.textBlock.opacity = 0.0;
                    },
                    "textEditingTool.doDeactivate": function () {
                        if (this.textBlock) this.textBlock.opacity = 1.0;
                        go.TextEditingTool.prototype.doDeactivate.call(this);
                    },
                    allowClipboard: false,
                    allowCopy: false,
                    allowUndo: false
                });

        this.myDiagram.toolManager.mouseMoveTools.insertAt(0, new LinkLabelOnPathDraggingTool());

        this.myDiagram.commandHandler.editTextBlock = () => { return false; };
        this.myDiagram.commandHandler.canDeleteSelection = () => {
            try {
                if (this.isEditMode) {
                    this.onDeleteClick();
                    return false;
                }
                return this.isEditMode;
            }
            catch (ex) {
                return this.isEditMode;
            }
        };

        this.myDiagram.grid.gridCellSize = new go.Size(24, 24);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;

        this.myDiagram.addModelChangedListener(() => {
            this.diagramStateChanged();
        });

        var self = this;
        this.myDiagram.addDiagramListener("ViewportBoundsChanged", function (e: go.DiagramEvent) {
            if (self.diagramOriginalPosition) {
                var rect = self.diagramOriginalPosition as go.Rect;
                e.diagram.scrollToRect(rect);
                self.diagramOriginalPosition = null;
            }
        });
        var model = this.myDiagram.model as go.GraphLinksModel;

        model.linkFromPortIdProperty = "fromPort";
        model.linkToPortIdProperty = "toPort";

        var activityNodeTemplate = ProcessDiagramTemplates.activityTemplate(this);
        var eventNodeTemplate = ProcessDiagramTemplates.eventTemplate(this);
        var gatewayNodeTemplate = ProcessDiagramTemplates.gatewayTemplate(this);

        activityNodeTemplate.selectionChanged = (node) => { this.onSelectionChanged(node); }
        eventNodeTemplate.selectionChanged = (node) => { this.onSelectionChanged(node); }
        gatewayNodeTemplate.selectionChanged = (node) => { this.onSelectionChanged(node); }

        var templmap = new go.Map<string, go.Node>();
        templmap.add("activity", activityNodeTemplate);
        templmap.add("event", eventNodeTemplate);
        templmap.add("gateway", gatewayNodeTemplate);
        templmap.add("deleted-node", ProcessDiagramTemplates.deletedNodeTemplate(this));
        templmap.add("", activityNodeTemplate);
        this.myDiagram.nodeTemplateMap = templmap;

        var linkTemplate = ProcessDiagramTemplates.linkTemplate;
        linkTemplate.category = 'link';
        this.myDiagram.linkTemplate = linkTemplate;

        var self = this;

        this.myDiagram.addDiagramListener("ExternalObjectsDropped", function (e) {
            // stop any ongoing text editing
            e.diagram.selection.each(data => {
                try {
                    var nodeData = data.data;
                    var newGuid = self.newGuid();

                    //keep track of relations between new instances and existing to avoid updating template data 
                    self.newInstancesMap.push({
                        oldKey: nodeData.key,
                        newKey: newGuid
                    });

                    e.diagram.model.commit(function (m) {
                        var data = m.findNodeDataForKey(nodeData.key);
                        m.set(data, 'Name', self.getNewNodeName(nodeData));
                        m.set(data, 'key', newGuid);
                    }, 'update__new_model');
                } catch (e) {
                    console.log(e);
                }

            })
        });
        //set initial actions

        this.initialActions.showFavorite = this.headerActionService.showFavorite;
        this.initialActions.showFollow = this.headerActionService.showFollow;
        this.initialActions.showHelp = this.headerActionService.showHelp;
        this.initialActions.showHomePage = this.headerActionService.showHomePage;
        this.initialActions.showNotifications = this.headerActionService.showNotifications;
        this.initialActions.showRaiseIssue = this.headerActionService.showRaiseIssue;
        this.initialActions.showSearch = this.headerActionService.showSearch;
        this.initialActions.showShoppingCart = this.headerActionService.showShoppingCart;

        //load current asset diagram
        this.load();
    }

    private getNewNodeName(at: DiagramNodeBase) {
        return this.returnUniqueName('New ' + at.Name, 1);
    }

    private returnUniqueName(name: string, iteration: number) {
        var tempName = name;
        if (iteration != 1) {
            tempName = name + ` (${iteration})`
        }
        if (this.isUnique(tempName)) {
            return tempName;
        }

        return this.returnUniqueName(name, iteration + 1);
    }

    private isUnique(name: string) {
        var exists = false;

        this.myDiagram.nodes.each(function (n) {
            if (n instanceof go.Node) {
                if (n.data.Name.toString() == name) {
                    exists = true;
                }
            }
        });
        return !exists;
    }



    private savedState: go.Model;
    private diagramStateChanged() {
        this.isSaveDisabled = this.isCurrentStateSaved();
        this.isCanvasEmpty = this.isEmpty();

        if (this.isDiagramLoaded) {
            if (!this.isSaveDisabled) {
                this.breadcrumbsService.setCurrentObjectState('modified');
            }
            else {
                this.breadcrumbsService.setCurrentObjectState('');
            }
        }

        this.cdRef.detectChanges();
    }
    private isEmpty() {
        return this.myDiagram.nodes.count == 0 && this.myDiagram.links.count == 0;
    }

    private isCurrentStateSaved() {
        if (!this.savedState)
            return false;
        return this.getSignature(this.myDiagram.model) == this.getSignature(this.savedState);
    }

    private getSignature(model: go.Model) {
        if (!model)
            return '';
        var m = JSON.parse(model.toJson().replace(`\"isReadOnly\": true,`, ''));
        if (m && m.nodeDataArray) {
            m.nodeDataArray.forEach(d => {
                delete d['relCount'];
            });
        }
        return JSON.stringify(m);
    }

    private validationErrors: any = {};
    private areNamesUnique: boolean = true;
    private save(closeEditorAfterSave: boolean = false) {
        this.isSaving = true;
        this.diagramOriginalPosition = this.myDiagram.viewportBounds.copy();
        this.processDiagramBase64 = this.myDiagram.makeImageData({
            scale: 1,
            maxSize: new go.Size(Infinity, Infinity)
        }).toString();
        this.processService.putProcessDiagram(this.assetUid, JSON.parse(this.myDiagram.model.toJson()))
            .subscribe(res => {
                if (res.hasError) {
                    this.isSaving = false;
                    this.validationErrors = res;
                    this.areNamesUnique = this.validationErrors.errors.some(x => x.ErrorType == 'CustomUniqueName');

                    this.updateValidationData();
                    this.isErrorModalOpened = true;
                    setTimeout(() => this.closeSaveButton.nativeElement.focus(), 250);

                    this.cdRef.detectChanges();
                }
                else {
                    this.isErrorModalOpened = false;
                    this.validationErrors = [];

                    if (this.actionAfterSaved) {
                        window.setTimeout(() => {
                            this.actionAfterSaved();
                            this.actionAfterSaved = null;
                            this.isSavingChangesModalOpened = false;
                            this.load(true);
                            this.cdRef.detectChanges();

                        }, 100)
                    } else {
                        this.load(true);
                        this.cdRef.detectChanges();

                    }
                }
            },
                err => {
                    console.log(err);

                });
    }
    private clear() {
        this.myDiagram.clear();
        this.diagramStateChanged();
    }
    private load(isFromSave: boolean = false) {

        var selectedItem = this.selectedNodeData;
        this.isSaveDisabled = true;
        this.processService.getProcessDiagram(this.assetUid)
            .subscribe(response => {

                var res = response.model;
                this.assetDetail = response.assetDetail;
                if (!this.myDiagram) {
                    console.warn("Diagram placeholder not loaded.");
                    return;
                }

                if (res && res.nodeDataArray && res.nodeDataArray.length > 0) {
                    res.nodeDataArray.forEach(x => {
                        x.icon = FontAwesomeHelper.GetHtmlCode(x.icon);
                        x.refItemColor = this.getNodeColor(x);
                        x.governanceDisplayValue = this.getNodeRoleName(x);
                    });
                }
                this.myDiagram.model = go.Model.fromJson(JSON.stringify(res));
                this.savedState = go.Model.fromJson(JSON.stringify(res));
                this.diagramStateChanged();
                this.applyEditMode(this.isEditMode);
                this.loadedEditors = [];
                this.isDiagramLoaded = true;
                this.isSaving = false;
                this.saveState.emit(this.isCurrentStateSaved());
                this.processDiagramBase64 = this.myDiagram.makeImageData({
                    scale: 1,
                    maxSize: new go.Size(Infinity, Infinity)
                }).toString();

                if (selectedItem) {
                    var name = this.selectedNodeData['Name'];
                    var selectedNode = this.myDiagram.nodes.filter(x => x.data['Name'] == name).first();
                    if (selectedNode) {
                        this.myDiagram.select(selectedNode);
                    }
                }

                this.cdRef.detectChanges();
            });
    }

    private updateLinkFromForm(formData) {
        var link = this.myDiagram.findLinkForData(formData.data);
        try {
            this.myDiagram.model.commit(function (m) {
                m.setDataProperty(link.data, 'label', formData.label.Value);
                m.setDataProperty(link.data, 'labelUid', formData.label.uid);
            }, 'update_link_data');
        } catch (e) {
            console.log(e);
        }
    }

    private updateNodeFromForm(formData) {
        try {
            var self = this;
            this.myDiagram.model.commit(function (m) {
                var data = m.findNodeDataForKey(formData.key);
                if (data) {
                    for (var propertyName in formData) {
                        var currentPropValue = data[propertyName];
                        var updatedPropValue = formData[propertyName];

                        var bothEmpty = self.isObjectEmpty(currentPropValue) && self.isObjectEmpty(updatedPropValue);

                        if (propertyName != 'key' && !bothEmpty) {
                            m.set(data, propertyName, formData[propertyName].toString());
                        }
                    }
                    m.set(data, 'refItemColor', self.getNodeColor(data));
                    m.set(data, 'governanceDisplayValue', self.getNodeRoleName(data));
                }
            }, 'update_model');
        } catch (e) {
            console.log(e);
        }
        if (this.myDiagram && this.myDiagram.nodes) {
            this.nodeNames = [];
            this.myDiagram.nodes.each(node => {
                this.nodeNames.push(node.data['Name']);
            })
        }
    }

    private isObjectEmpty(obj: any): boolean {
        if (obj == null || obj == undefined || obj.toString() == '') return true;
        return false;
    }

    private newGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0,
                v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    private getNodeColor(data: any) {
        try {
            var item = this.colors.find(x => +x.ObjectID == +data.GovernanceRole);
            if (item && item.Value)
                return item.Value;
        }
        catch{
            return this.defaultStrokeColor;
        }
        return this.defaultStrokeColor;
    }

    private getNodeRoleName(data: any) {
        try {
            var item = this.colors.find(x => +x.ObjectID == +data.GovernanceRole);
            if (item)
                return item.DisplayValue;
        }
        catch{
            return '';
        }
        return '';
    }

    private onSelectionChanged(node) {
        this.selectedNodeData = JSON.parse(JSON.stringify(node.data));

        if (!this.loadedEditors.some(x => x.key == this.selectedNodeData.key)) {
            this.loadedEditors.push(this.selectedNodeData);
        }
        this.cdRef.detectChanges();
    }

    private updateValidationData() {
        if (this.validationErrors && this.validationErrors.errors) {
            var errors = this.validationErrors.errors as any[];
            let selectedKey: string = '';

            try {
                this.myDiagram.model.commit(function (m) {
                    m.nodeDataArray.forEach(data => {
                        if (errors.map(x => x.AssetUid).some(x => x == data.key)) {
                            m.set(data, 'hasError', true);
                            if (!selectedKey) {
                                selectedKey = data.key;
                            }
                        }
                        else {
                            m.set(data, 'hasError', false);
                        }
                    });
                }, 'update_model_validation');
            } catch (e) {
                console.log(e);
            }

            if (selectedKey) {
                this.isInfoPanelOpened = true;
                this.myDiagram.clearSelection();
                this.myDiagram.select(this.myDiagram.findPartForKey(selectedKey));
                this.selectFirstInvalidField();
            }
        }
    }

    private selectFirstInvalidField() {
        setTimeout(() => {
            var el = (document.querySelectorAll('.asset-editor .display .field-wrapper.invalid input')[0] as HTMLElement);
            if (el)
                el.focus();
        }, 200);
    }

    private toggleClass(event: any, cs: string) {
        var element = event.target;
        if (!element.classList.contains('gov-accordion-item')) {
            if (element.parentElement.classList.contains('gov-accordion-item')) {
                element = element.parentElement;
            }
            else {
                element = element.parentElement.parentElement;
            }
        }

        const hasClass = element.classList.contains(cs);

        if (hasClass) {
            this.renderer.removeClass(element, cs);
        } else {
            this.renderer.addClass(element, cs);
        }
    }

    private loadPallete() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates
        this.myEventPalette =
            $(go.Palette, "event-pallete",
                {
                    layout:
                        $(go.GridLayout,
                            {
                                wrappingColumn: 2,
                                arrangement: go.GridLayout.LeftToRight,

                            }
                        ),
                    'toolManager.hoverDelay': 100
                });

        var eventArr = [];
        // now add the initial contents of the Palette
        this.events.forEach(ev => {
            eventArr.push({
                category: 'event',
                refItemColor: this.defaultStrokeColor,
                icon: FontAwesomeHelper.GetHtmlCode(ev.Icon),
                Name: ev.Name,
                PopupDescription: ev.Description,
                key: 'new_instance_' + this.newGuid(),
                assetTypeName: ev.Name,
                assetTypeUid: ev.uid,
                isNew: true,
                relCount: "0"
            });
        })

        if (this.events.length == 1) {
            eventArr.push({
                category: 'blank-node'
            });
        }

        this.myEventPalette.model.nodeDataArray = eventArr;

        var templmap = new go.Map<string, go.Node>();
        templmap.add("event", ProcessDiagramTemplates.eventTemplate_pallete());
        templmap.add("blank-node", ProcessDiagramTemplates.blankTemplate_pallete());
        this.myEventPalette.nodeTemplateMap = templmap;

        this.myActivityPallete =
            $(go.Palette, "activity-pallete",
                {
                    layout:
                        $(go.GridLayout,
                            {
                                wrappingColumn: 2
                            }
                        ),
                    'toolManager.hoverDelay': 100
                });

        var activitiesArr = [];
        // now add the initial contents of the Palette
        this.activities.forEach(ev => {
            activitiesArr.push({
                category: 'activity',
                refItemColor: this.defaultStrokeColor,
                icon: FontAwesomeHelper.GetHtmlCode(ev.Icon),
                Name: ev.Name,
                PopupDescription: ev.Description,
                key: 'new_instance_' + this.newGuid(),
                assetTypeName: ev.Name,
                assetTypeUid: ev.uid,
                isNew: true,
                relCount: "0"
            });
        })

        if (this.activities.length == 1) {
            activitiesArr.push({
                category: 'blank-node'
            });
        }
        this.myActivityPallete.model.nodeDataArray = activitiesArr;

        var templmap = new go.Map<string, go.Node>();
        templmap.add("activity", ProcessDiagramTemplates.activityTemplate_pallete());
        templmap.add("blank-node", ProcessDiagramTemplates.blankTemplate_pallete());
        this.myActivityPallete.nodeTemplateMap = templmap;


        this.myGatewayPallete =
            $(go.Palette, "gateway-pallete",
                {
                    layout:
                        $(go.GridLayout,
                            {
                                wrappingColumn: 2,
                            }
                        ),
                    'toolManager.hoverDelay': 100
                });

        var gatewaysArr = [];
        // now add the initial contents of the Palette
        this.gateways.forEach(ev => {
            gatewaysArr.push({
                category: 'gateway',
                refItemColor: this.defaultStrokeColor,
                icon: FontAwesomeHelper.GetHtmlCode(ev.Icon),
                Name: ev.Name,
                PopupDescription: ev.Description,
                key: 'new_instance_' + this.newGuid(),
                assetTypeName: ev.Name,
                assetTypeUid: ev.uid,
                isNew: true,
                relCount: "0"
            });
        })
        if (this.gateways.length == 1) {
            gatewaysArr.push({
                category: 'blank-node'
            });
        }
        this.myGatewayPallete.model.nodeDataArray = gatewaysArr;

        var templmap = new go.Map<string, go.Node>();
        templmap.add("gateway", ProcessDiagramTemplates.gatewayTemplate_pallete());
        templmap.add("blank-node", ProcessDiagramTemplates.blankTemplate_pallete());
        this.myGatewayPallete.nodeTemplateMap = templmap;

        this.isPalleteLoaded = true;
    }

    private downloadProcessDiagram() {
        var fileName = this.assetDetail?.DisplayValue;
        this.isExporting = true;
        this.cdRef.detectChanges();
        this.processService.downloadProcessExcel(this.assetUid, this.processDiagramBase64)
            .subscribe(data => {
                this.isExporting = false;
                this.processService.downloadFile(data, fileName);
                this.cdRef.detectChanges();
            });
    }

    private actionAfterSaved: Function;
    private actionMessage: string = '';
    private showDiscardChanges: boolean = false;
    public doControlledAction(actionName: string) {
        if (this.isEditMode && !this.isCurrentStateSaved()) {
            this.isSavingChangesModalOpened = true;
            switch (actionName) {
                case 'switchModes':
                    this.actionMessage = 'Would you like to save your changes to the diagram before leaving the Diagram Designer?';
                    setTimeout(() => this.saveChangesButton.nativeElement.focus(), 100);
                    this.actionAfterSaved = () => {
                        this.switchModes();
                        this.actionAfterSaved = null;
                    }
                    this.showDiscardChanges = true;
                    break;
                case 'open-related-assets':
                    this.actionMessage = 'Please save your changes to the diagram before opening Related Assets.';
                    this.showDiscardChanges = false;
                    setTimeout(() => this.saveChangesButton.nativeElement.focus(), 100);
                    this.actionAfterSaved = () => {
                        this.isRelatedAssetsVisible = !this.isRelatedAssetsVisible;
                        this.actionAfterSaved = null;
                    }
                    break;
                case 'export':
                    this.actionMessage = 'Please save your changes to the diagram before exporting process diagram.';

                    this.actionAfterSaved = () => {
                        this.downloadProcessDiagram();
                        this.actionAfterSaved = null;
                    }
                    this.showDiscardChanges = false;
                    break;
            }

            return;
        }
        else {
            this.actionAfterSaved = null;
            this.isSavingChangesModalOpened = false;
            switch (actionName) {
                case 'switchModes':
                    this.switchModes();
                    break;
                case 'open-related-assets':
                    this.isRelatedAssetsVisible = !this.isRelatedAssetsVisible;
                    this.cdRef.detectChanges();
                    break;
                case 'export':
                    this.actionMessage = 'Please save your changes to the diagram before exporting process diagram.';
                    this.downloadProcessDiagram();
                    break;
            }
        }


    }

    closeRelationshipModel() {
        this.processService.getProcessDiagramBadges(this.assetUid)
            .subscribe(badges => {
                this.isRelatedAssetsVisible = false;

                try {
                    this.myDiagram.model.commit(function (m) {
                        badges.forEach(asset => {
                            var data = m.findNodeDataForKey(asset.AssetUid);
                            if (data)
                                m.set(data, 'relCount', asset.RelationshipCount.toString());
                        })
                    }, 'update_model_badge_data');
                } catch (e) {
                    console.log(e);
                }
            })
    }

    closeErrorModal() {
        this.isErrorModalOpened = false;
        if (this.validationErrors) {
            this.selectFirstInvalidField();
        }
    }

    public changeViewType(type: string) {
        if (type == 'list') {
            this.viewType = 'list';
        }
        else {
            this.viewType = 'diagram';
        }
    }
}