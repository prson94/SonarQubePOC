import * as go from 'gojs';
import * as _ from 'lodash';
import { Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, AfterViewChecked, Output, EventEmitter, HostListener, ViewChild } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { FlowObjectType, } from '../../../../models/asset.model';
import { FontAwesomeHelper } from '../../../../static/font-awesome-helper';
import { ProcessDiagramTemplates } from './process-diagram.templates';
import { ProcessService } from '../../../../services/process.service';
import { DiagramNodeBase } from '../../../../models/process.model';

@Component({
    selector: 'd3s-process-diagram',
    templateUrl: './process-diagram.component.html',
    providers: [ProcessService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramComponent extends DiagramBaseComponent implements OnInit, AfterViewChecked {
    @Input() isEditMode: boolean = false;
    @Input() isFullScreen: boolean = false;
    @Input() assetUid: string = '';

    @Output() editModeClosed: EventEmitter<any> = new EventEmitter<any>();
    myDiagram: go.Diagram;

    private assetTypeNodes: DiagramNodeBase[] = [];
    private events: DiagramNodeBase[] = [];
    private activities: DiagramNodeBase[] = [];
    private gateways: DiagramNodeBase[] = [];
    private isLoaded = false;
    private isSaveDisabled: boolean = true;
    private isCanvasEmpty: boolean = true;
    private isSaving: boolean = false;
    private defaultStrokeColor: string = '#708EA6';

    private selectedNodeData: any;

    private isErrorModalOpened: boolean = false;

    private isInfoPanelOpened: boolean = false;
    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private processService: ProcessService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }


    ngOnInit() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates
        this.processService.getAvailableNodes(this.assetUid)
            .subscribe(res => {
                this.assetTypeNodes = res;
                this.events = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Event);
                this.activities = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Activity);
                this.gateways = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Gateway);
                this.isLoaded = true;
                this.loadDiagram();

            });
    }
    @ViewChild('diagram', { static: false }) diagramRef;
    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        let height = window.innerHeight;

        if (this.isEditMode)
            this.diagramRef.nativeElement.style.height = (height - 120) + 'px';
        else if (this.isFullScreen)
            this.diagramRef.nativeElement.style.height = (height - 40) + 'px';
        else
            this.diagramRef.nativeElement.style.height = (height - 240) + 'px';
    } 

    ngAfterViewChecked() {
        this.onResize(null);
        this.applyEditMode(this.isEditMode);
        if (this.myDiagram) {
            if (this.myDiagram.selection.count == 0) {
                this.selectedNodeData = null;
            }
        }

        this.cdRef.detectChanges();
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

    loadDiagram() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates

        this.myDiagram =
            $(go.Diagram, "diagram",  // must name or refer to the DIV HTML element
                {
                    "draggingTool.dragsLink": true,
                    "draggingTool.isGridSnapEnabled": true,
                    "linkingTool.portGravity": 20,
                    "relinkingTool.portGravity": 20,
                    "rotatingTool.handleAngle": 270,
                    "rotatingTool.handleDistance": 30,
                    "rotatingTool.snapAngleMultiple": 15,
                    "rotatingTool.snapAngleEpsilon": 15,
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


        this.myDiagram.grid.gridCellSize = new go.Size(24, 24);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.draggingTool.gridSnapCellSpot = go.Spot.Center;

        this.myDiagram.addModelChangedListener(() => {
            this.diagramStateChanged();
        })

        var activityNodeTemplate = ProcessDiagramTemplates.activityTemplate(this);
        var eventNodeTemplate = ProcessDiagramTemplates.eventTemplate(this);
        var gatewayNodeTemplate = ProcessDiagramTemplates.gatewayTemplate(this);

        var templmap = new go.Map<string, go.Node>();
        templmap.add("activity", activityNodeTemplate);
        templmap.add("event", eventNodeTemplate);
        templmap.add("gateway", gatewayNodeTemplate);
        templmap.add("", activityNodeTemplate);
        this.myDiagram.nodeTemplateMap = templmap;

        this.myDiagram.linkTemplate = ProcessDiagramTemplates.linkTemplate;


        //load current asset diagram
        this.load();
    }

    private dragEnd($event: DiagramNodeBase) {
        var nodeCategory: string = '';

        switch ($event.FlowObjectType) {
            case FlowObjectType.Activity: nodeCategory = 'activity'; break;
            case FlowObjectType.Event: nodeCategory = 'event'; break;
            case FlowObjectType.Gateway: nodeCategory = 'gateway'; break;
        }
        var icon = FontAwesomeHelper.GetHtmlCode($event.Icon);

        setTimeout(() => {
            this.myDiagram.startTransaction("make new node");
            var point = go.Point.stringify(this.myDiagram.lastInput.documentPoint);

            var data = {
                key: this.newGuid(),
                icon: icon,
                category: nodeCategory,
                loc: point,
                refItemColor: this.defaultStrokeColor,
                isNew: true,
                //asset data
                Name: this.getNewNodeName($event),
                assetTypeName: $event.Name,
                assetTypeUid: $event.uid,
            };

            this.myDiagram.model.addNodeData(data);

            this.myDiagram.commitTransaction("make new node");
            this.myDiagram.redraw();
        }, 100);

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
        return JSON.stringify(this.myDiagram.model) == JSON.stringify(this.savedState);
    }

    private validationErrors: any = {};
    private save() {
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
                    var model = res.updatedModel;
                    this.myDiagram.model = go.Model.fromJson(JSON.stringify(model));
                    this.savedState = JSON.parse(JSON.stringify(this.myDiagram.model));
                    this.diagramStateChanged();
                    this.isSaving = false;
                    this.isErrorModalOpened = false;
                    this.validationErrors = [];
                    this.cdRef.detectChanges();
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
                if (res && res.nodeDataArray && res.nodeDataArray.length > 0) {
                    res.nodeDataArray.forEach(x => {
                        x.icon = FontAwesomeHelper.GetHtmlCode(x.icon);
                    });
                }
                this.myDiagram.model = go.Model.fromJson(JSON.stringify(res));
                this.savedState = JSON.parse(JSON.stringify(this.myDiagram.model));
                this.diagramStateChanged();
                this.applyEditMode(this.isEditMode);
                this.cdRef.detectChanges();
            });
    }

    private newGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0,
                v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    private onSelectionChanged(node) {
        if (!this.isEditMode) return;
        this.selectedNodeData = JSON.parse(JSON.stringify(node.data));
        this.cdRef.detectChanges();
    }

    private updateValidationData() {
        if (this.validationErrors && this.validationErrors.errors) {
            var errors = this.validationErrors.errors as any[];
            try {
                this.myDiagram.model.commit(function (m) {
                    m.nodeDataArray.forEach(data => {
                        if (errors.map(x => x.AssetUid).some(x => x == data.key)) {
                            m.set(data, 'hasError', true);
                        }
                        else {
                            m.set(data, 'hasError', false);
                        }
                    });
                }, 'update_model_validation');
            } catch (e) {
                console.log(e);
            }
        }
    }

    private updateNodeFromForm(formData) {
        try {
            this.myDiagram.model.commit(function (m) {
                var data = m.findNodeDataForKey(formData.key);
                for (var propertyName in formData) {
                    if (propertyName != 'key') {
                        if (formData[propertyName].Value) {
                            m.set(data, propertyName, formData[propertyName].Value);

                        }
                        else {
                            m.set(data, propertyName, formData[propertyName]);
                        }
                    }
                }
            }, 'update_model');
        } catch (e) {
            console.log(e);
        }
    }
}