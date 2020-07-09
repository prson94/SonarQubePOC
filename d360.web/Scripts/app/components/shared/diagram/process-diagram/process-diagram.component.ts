import * as go from 'gojs';
import * as _ from 'lodash';
import { Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, AfterViewChecked, Output, EventEmitter, HostListener, ViewChild, OnDestroy, Renderer2 } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { FlowObjectType, } from '../../../../models/asset.model';
import { FontAwesomeHelper } from '../../../../static/font-awesome-helper';
import { ProcessDiagramTemplates } from './process-diagram.templates';
import { ProcessService } from '../../../../services/process.service';
import { DiagramNodeBase } from '../../../../models/process.model';
import { CanDeactivate, Router } from '@angular/router';

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
    myDiagram: go.Diagram;

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

    private isLoaded = false;
    private isDiagramLoaded = false;
    private isSaveDisabled: boolean = true;
    private isCanvasEmpty: boolean = true;
    private isSaving: boolean = false;
    private isExporting: boolean = false;
    private defaultStrokeColor: string = '#708EA6';

    private selectedNodeData: any;
    private loadedEditors: any[] = [];

    private isErrorModalOpened: boolean = false;
    private isSavingChangesModalOpened: boolean = false;
    private promptDeleteOpened: boolean = false;
    private isRelatedAssetsVisible: boolean = false;


    private isInfoPanelOpened: boolean = false;
    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private processService: ProcessService,
        private cdRef: ChangeDetectorRef,
        private router: Router,
        private renderer: Renderer2
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }


    ngOnInit() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates
        this.processService.getProcessDiagramColors()
            .subscribe(colors => {
                this.colors = colors;
            });
        this.processService.getAvailableNodes(this.assetUid)
            .subscribe(res => {
                this.assetTypeNodes = res;
                this.events = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Event);
                this.activities = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Activity);
                this.gateways = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Gateway);

                var nodeHeight = 150;
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
    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        if (!this.diagramRef) return;
        let height = window.innerHeight;
        if (this.isEditMode)
            this.diagramRef.nativeElement.style.height = (height - 120) + 'px';
        else if (this.isFullScreen)
            this.diagramRef.nativeElement.style.height = (height - 40) + 'px';
        else
            this.diagramRef.nativeElement.style.height = (height - 240) + 'px';
    }

    @HostListener('click', ['$event.target'])
    onClick(btn) {
        if (this.myDiagram) {
            if (this.myDiagram.selection.count == 0) {
                this.selectedNodeData = null;
            }
        }
        this.myDiagram.requestUpdate();
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
            if (this.myDiagram.selection.count == 0 || this.myDiagram.selection.count > 1) {
                this.selectedNodeData = null;
            }
            this.saveState.emit(this.isCurrentStateSaved());
        }
        this.cdRef.detectChanges();
    }


    ngOnDestroy() {
        if (this.cdRef)
            this.cdRef.detach();
        if (this.myDiagram)
            this.myDiagram = null;
    }

    private applyEditMode(state: boolean) {
        if (!this.myDiagram) return;
        this.myDiagram.nodes.each(function (n) {
            if (n instanceof go.Node) {
                n.isEnabled = state;
                n.movable = state;
            }
        });
        this.myDiagram.links.each(function (n) {
            if (n instanceof go.Link) {
                n.isEnabled = state;
                n.movable = state;
            }
        });
        this.myDiagram.isModelReadOnly = !state;
        if (this.isEditMode && !this.isPalleteLoaded) {
            this.loadPallete();
        }
    }

    private discardChanged() {
        this.load();
        if (this.actionAfterSaved) {
            this.actionAfterSaved();
            this.isSavingChangesModalOpened = false;
            this.isErrorModalOpened = false;
            this.actionAfterSaved = null;
        }
    }

    private isDeleteEnabled() {
        if (this.myDiagram && this.myDiagram.selection.count > 0) {
            return this.myDiagram.selection.any(x => x.data.Name);
        }
        else return false;
    }

    private isRelatedAssetsEnabled() {
        if (!this.selectedNodeData)
            return false;

        if (this.selectedNodeData.isNew)
            return false;
        return true;
    }

    private getSelectedNodeCount() {
        if (!this.myDiagram)
            return 0;

        return this.myDiagram.selection.filter(x => x.data.Name).count;
    }

    private get deleteModelTitle(): string {
        return this.getSelectedNodeCount() > 1 ? 'Delete Selected Items' : 'Delete Selected Item';
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
                    }
                });

        this.myDiagram.commandHandler.canDeleteSelection = () => {
            try {
                if (this.isEditMode) {
                    if (this.myDiagram.selection.any(x => x.category == 'activity' || x.category == 'event' || x.category == 'gateway')) {
                        this.promptDeleteOpened = true;
                        this.cdRef.detectChanges();
                        return false;
                    }
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
        })

        var activityNodeTemplate = ProcessDiagramTemplates.activityTemplate();
        var eventNodeTemplate = ProcessDiagramTemplates.eventTemplate();
        var gatewayNodeTemplate = ProcessDiagramTemplates.gatewayTemplate();

        activityNodeTemplate.selectionChanged = (node) => { this.onSelectionChanged(node); }
        eventNodeTemplate.selectionChanged = (node) => { this.onSelectionChanged(node); }
        gatewayNodeTemplate.selectionChanged = (node) => { this.onSelectionChanged(node); }

        var templmap = new go.Map<string, go.Node>();
        templmap.add("activity", activityNodeTemplate);
        templmap.add("event", eventNodeTemplate);
        templmap.add("gateway", gatewayNodeTemplate);
        templmap.add("", activityNodeTemplate);
        this.myDiagram.nodeTemplateMap = templmap;

        this.myDiagram.linkTemplate = ProcessDiagramTemplates.linkTemplate;
        var self = this;

        this.myDiagram.addDiagramListener("ExternalObjectsDropped", function (e) {
            // stop any ongoing text editing

            e.diagram.selection.each(data => {
                try {
                    var nodeData = data.data;
                    e.diagram.model.commit(function (m) {
                        var data = m.findNodeDataForKey(nodeData.key);
                        m.set(data, 'Name', self.getNewNodeName(nodeData));
                        m.set(data, 'key', self.newGuid());
                    }, 'update__new_model');
                } catch (e) {
                    console.log(e);
                }

            })
        });

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
        return JSON.stringify(m);
    }

    private validationErrors: any = {};
    private save(closeEditorAfterSave: boolean = false) {
        this.isSaving = true;

        this.processService.putProcessDiagram(this.assetUid, JSON.parse(this.myDiagram.model.toJson()))
            .subscribe(res => {
                if (res.hasError) {
                    this.isSaving = false;
                    this.validationErrors = res;
                    this.updateValidationData();
                    this.isErrorModalOpened = true;
                    this.cdRef.detectChanges();
                }
                else {
                    this.isSaving = false;
                    this.isErrorModalOpened = false;
                    this.validationErrors = [];
                    this.cdRef.detectChanges();
                    this.processDiagramBase64 = this.myDiagram.makeImageData({
                        scale: 1
                    }).toString();

                    if (this.actionAfterSaved) {
                        window.setTimeout(() => {
                            this.actionAfterSaved();
                            this.actionAfterSaved = null;
                            this.isSavingChangesModalOpened = false;
                        }, 100)
                    }
                }
            },
                err => {


                });
    }
    private clear() {
        this.myDiagram.clear();
        this.diagramStateChanged();
    }
    private load() {
        this.processService.getProcessDiagram(this.assetUid)
            .subscribe(res => {
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
                this.saveState.emit(this.isCurrentStateSaved());
                this.cdRef.detectChanges();
                this.processDiagramBase64 = this.myDiagram.makeImageData({
                    scale: 1
                }).toString();
            });
    }

    private updateNodeFromForm(formData) {
        try {
            var self = this;
            this.myDiagram.model.commit(function (m) {
                var data = m.findNodeDataForKey(formData.key);
                for (var propertyName in formData) {
                    if (propertyName != 'key') {
                        m.set(data, propertyName, formData[propertyName]);
                    }
                }
                m.set(data, 'refItemColor', self.getNodeColor(data));
                m.set(data, 'governanceDisplayValue', self.getNodeRoleName(data));
            }, 'update_model');
        } catch (e) {
            console.log(e);
        }
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
        if (!this.isEditMode) return;
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
            }
        }
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
                                wrappingColumn: 2
                            }
                        ),

                });

        var eventArr = [];
        // now add the initial contents of the Palette
        this.events.forEach(ev => {
            eventArr.push({
                category: 'event',
                refItemColor: this.defaultStrokeColor,
                icon: FontAwesomeHelper.GetHtmlCode(ev.Icon),
                Name: ev.Name,
                key: 'new_instance_' + this.newGuid(),
                assetTypeName: ev.Name,
                assetTypeUid: ev.uid,
                isNew: true
            });
        })

        this.myEventPalette.model.nodeDataArray = eventArr;

        var templmap = new go.Map<string, go.Node>();
        templmap.add("event", ProcessDiagramTemplates.eventTemplate_pallete());
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

                });

        var eventArr = [];
        // now add the initial contents of the Palette
        this.activities.forEach(ev => {
            eventArr.push({
                category: 'activity',
                refItemColor: this.defaultStrokeColor,
                icon: FontAwesomeHelper.GetHtmlCode(ev.Icon),
                Name: ev.Name,
                key: 'new_instance_' + this.newGuid(),
                assetTypeName: ev.Name,
                assetTypeUid: ev.uid,
                isNew: true
            });
        })

        this.myActivityPallete.model.nodeDataArray = eventArr;

        var templmap = new go.Map<string, go.Node>();
        templmap.add("activity", ProcessDiagramTemplates.activityTemplate_pallete());
        this.myActivityPallete.nodeTemplateMap = templmap;


        this.myGatewayPallete =
            $(go.Palette, "gateway-pallete",
                {
                    layout:
                        $(go.GridLayout,
                            {
                                wrappingColumn: 2
                            }
                        ),

                });

        var eventArr = [];
        // now add the initial contents of the Palette
        this.gateways.forEach(ev => {
            eventArr.push({
                category: 'gateway',
                refItemColor: this.defaultStrokeColor,
                icon: FontAwesomeHelper.GetHtmlCode(ev.Icon),
                Name: ev.Name,
                key: 'new_instance_' + this.newGuid(),
                assetTypeName: ev.Name,
                assetTypeUid: ev.uid,
                isNew: true
            });
        })

        this.myGatewayPallete.model.nodeDataArray = eventArr;

        var templmap = new go.Map<string, go.Node>();
        templmap.add("gateway", ProcessDiagramTemplates.gatewayTemplate_pallete());
        this.myGatewayPallete.nodeTemplateMap = templmap;

        this.isPalleteLoaded = true;
    }

    private downloadProcessDiagram() {
        var fileName = 'Filename';
        this.isExporting = true;
        this.processService.downloadProcessExcel(this.assetUid, this.processDiagramBase64)
            .subscribe(data => {
                this.isExporting = false;
                this.processService.downloadFile(data, fileName);
            });
    }

    private actionAfterSaved: Function;
    private actionMessage: string = '';
    private showDiscardChanges: boolean = false;
    private doControlledAction(actionName: string) {

        if (this.isEditMode && !this.isCurrentStateSaved()) {
            this.isSavingChangesModalOpened = true;
            switch (actionName) {
                case 'switchModes':
                    this.actionMessage = 'Would you like to save your changes to the diagram before leaving the Diagram Designer?';
                    this.actionAfterSaved = this.switchModes;
                    this.showDiscardChanges = true;
                    break;
                case 'open-related-assets':
                    this.actionMessage = 'Please save your changes to the diagram before opening Related Assets?';
                    this.showDiscardChanges = false;
                    var self = this;
                    this.actionAfterSaved = () => {
                        console.log("here");
                        self.isRelatedAssetsVisible = !self.isRelatedAssetsVisible;
                    }
                    break;
                case 'export':
                    this.actionMessage = 'Please save your changes to the diagram before exporting process diagram?';
                    this.actionAfterSaved = this.downloadProcessDiagram;
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
                    break;
                case 'export':
                    this.actionMessage = 'Please save your changes to the diagram before exporting process diagram?';
                    this.downloadProcessDiagram();
                    break;
            }
        }


    }
}