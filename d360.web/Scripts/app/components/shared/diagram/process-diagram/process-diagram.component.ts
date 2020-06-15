import * as go from 'gojs';
import * as _ from 'lodash';
import { Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, AfterViewChecked } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { FlowObjectType, AssetTypeClass, AssetTypeApiModel } from '../../../../models/asset.model';
import { AssetTypeService } from '../../../../services/asset-type.service';
import { FontAwesomeHelper } from '../../../../static/font-awesome-helper';
import { ProcessDiagramTemplates } from './process-diagram.templates';

@Component({
    selector: 'd3s-process-diagram',
    templateUrl: './process-diagram.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramComponent extends DiagramBaseComponent implements OnInit {
    @Input() isEditMode: boolean = false;
    myDiagram: go.Diagram;

    private assetTypeNodes: AssetTypeApiModel[] = [];
    private events: AssetTypeApiModel[] = [];
    private activities: AssetTypeApiModel[] = [];
    private gateways: AssetTypeApiModel[] = [];
    private isLoaded = false;
    private isSaveDisabled: boolean = false;
    private isCanvasEmpty: boolean = true;

    private defaultStrokeColor: string = '#708EA6';

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private assetTypeService: AssetTypeService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }


    ngOnInit() {
        var $ = go.GraphObject.make;  // for conciseness in defining templates
        this.assetTypeService.getAssetTypesByClass(AssetTypeClass.DiagramAsset)
            .subscribe(res => {
                this.assetTypeNodes = res;
                this.events = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Event);
                this.activities = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Activity);
                this.gateways = this.assetTypeNodes.filter(x => x.FlowObjectType == FlowObjectType.Gateway);
                this.isLoaded = true;
                this.loadDiagram();
                this.applyEditMode(this.isEditMode);
                this.cdRef.detectChanges();
            });
    }

    private applyEditMode(state: boolean) {
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

        var activityNodeTemplate = ProcessDiagramTemplates.activityTemplate;
        var eventNodeTemplate = ProcessDiagramTemplates.eventTemplate;
        var gatewayNodeTemplate = ProcessDiagramTemplates.gatewayTemplate;

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




    private getData() {
        console.log(this.myDiagram.model.toJson())
        console.log(this.myDiagram);
    }

    private dragEnd($event: AssetTypeApiModel) {
        var nodeCategory: string = '';

        switch ($event.FlowObjectType) {
            case FlowObjectType.Activity: nodeCategory = 'activity'; break;
            case FlowObjectType.Event: nodeCategory = 'event'; break;
            case FlowObjectType.Gateway: nodeCategory = 'gateway'; break;
        }
        var icon = FontAwesomeHelper.GetHtmlCode($event['IconStyle'].Icon);

        setTimeout(() => {
            this.myDiagram.startTransaction("make new node");
            var point = go.Point.stringify(this.myDiagram.lastInput.documentPoint);

            var data = {
                key: this.newGuid(),
                icon: icon,
                category: nodeCategory,
                loc: point,
                refItemColor: this.defaultStrokeColor,
                //asset data
                name: this.getNewNodeName($event),
                assetTypeName: $event.Name,
                assetTypeUid: $event.uid,
            };

            this.myDiagram.model.addNodeData(data);

            this.myDiagram.commitTransaction("make new node");
            this.myDiagram.redraw();
        }, 100);

    }

    private getNewNodeName(at: AssetTypeApiModel) {
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
                if (n.data.name.toString() == name) {
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

    private save() {
        console.log("save");
        this.saveToLocalStorage();
        this.savedState = JSON.parse(JSON.stringify(this.myDiagram.model));
        this.diagramStateChanged();
    }
    private clear() {
        this.myDiagram.clear();
        this.diagramStateChanged();
        console.log("clear");

    }
    private load() {
        console.log("load");
        this.loadFromLocalStorage();
    }

    private saveToLocalStorage() {
        localStorage.setItem('process-diagram', this.myDiagram.model.toJson());

    }
    private loadFromLocalStorage() {
        var model = localStorage.getItem('process-diagram');
        this.myDiagram.model = go.Model.fromJson(model);
        this.diagramStateChanged();
    }
    private newGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0,
                v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}