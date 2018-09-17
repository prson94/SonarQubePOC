import { Component, OnInit, OnChanges, Input, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowItemStep, WorkflowActivityType, StepType, WorkflowDiagramNode, NodeModel, ActivityTypeInfo, DiagramObjectType, WorkflowStepDetail, WorkflowChangeType } from '../../models/workflow.model';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-monitor-step-details',
    templateUrl: 'workflowmonitor-step-details.component.html',
    providers: [WorkflowService, ResponsibilityTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepDetailsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() itemStepId: number;
    @Input() visible: boolean = true;
    @Output() visibleChange = new EventEmitter();
    @Output() onCloseClick = new EventEmitter();
    step: WorkflowStepDetail = null;
    StepType = StepType;
    WorkflowActivityType = WorkflowActivityType;
    WorkflowChangeType = WorkflowChangeType
    responsibilities: any[] = [];
    fields: any[] = [];
    

    constructor(private workflowService: WorkflowService, private ref: ChangeDetectorRef, private responsibilityService: ResponsibilityTypeService) {
        super();
    }

    ngOnInit() {
        this.load()
            .then(() => this.responsibilityService.getResponsibilityTypes())
            .then(r => this.responsibilities = r)
            .then(() => {
                if (this.step != null)
                    this.workflowService.getWorkflowFieldTypes(this.step.ObjectTypeID, this.step.ObjectType, true)
                        .then(r => {
                            this.fields = r;
                        });
            });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['itemStepId'] != null && (changes['itemStepId'].isFirstChange || (changes['itemStepId'].currentValue != changes['itemStepId'].previousValue))) {
            this.load();
        }
    }

    load() {
        this.step = null;

        if (this.itemStepId != null) {
            this.isLoading = true;
            return this.workflowService.getWorkflowStepDetail(this.itemStepId)
                .then(r => {
                    this.isLoading = false;
                    this.step = r;//this.convertToDiagramModel(r);
                    this.ref.markForCheck();
                    console.log('load', this.step);
                });
        }
        else
            return Promise.resolve();
    }

    private close() {
        this.visible = false;
        this.visibleChange.emit(false);
        this.ref.markForCheck();
    }

    private activityTypeName(workflowActivityType: WorkflowActivityType): string {
        switch (workflowActivityType) {
            case WorkflowActivityType.EmailNotification:
                return 'Email Notification';
            case WorkflowActivityType.FieldChange:
                return 'Field Change';
            case WorkflowActivityType.RelationshipUpdate:
                return 'Relationship Update';
            case WorkflowActivityType.StateChange:
                return 'State Change';
            case WorkflowActivityType.StatusChange:
                return 'Status Change';
            default:
                return WorkflowActivityType[workflowActivityType];

        }
    }

    private stepTypeName(stepType: StepType): string {
        return StepType[stepType];
    }

    ////TODO: replace this and pull back appropriate data from server. This is ripped from the workflow-diagram component 
    ////where the old summary panel lives
    //private convertToDiagramModel(model: WorkflowDiagramNode): NodeModel {
    //    let m: WorkflowDiagramNode = <WorkflowDiagramNode>model;
    //    let n = new NodeModel();

    //    n.key = m.Key;
    //    n.name = m.Name;
    //    n.pos = `${m.XPosition} ${m.YPosition}`;
    //    n.x = m.XPosition;
    //    n.y = m.YPosition;
    //    n.activityType = m.ActivityType;
    //    n.stepType = m.StepType;
    //    n.category = 'task';
    //    n.fields = m.FieldsObject;
    //    n.runCount = m.RunCount || 0;

    //    //special case for Form to deal with XML returning an object when field count = 1 instead of an array
    //    if (n.activityType == WorkflowActivityType.Form) {

    //        if (m.Fields != null && m.Fields.toString() === m.Fields && m.FieldsObject == null && m.Fields.startsWith('{')) {
    //            n.fields = JSON.parse(m.Fields).fields;

    //        }

    //        if (n.fields != null && n.fields.form != null && n.fields.form.field != null && n.fields.form.field.length == null) {
    //            let f = _.cloneDeep(n.fields.form.field);

    //            n.fields.form.field = [];
    //            n.fields.form.field.push(f);
    //        }
    //    }

    //    let activityType: ActivityTypeInfo;

    //    if (m.ActivityTypeInfo != null)
    //        activityType = m.ActivityTypeInfo;
    //    else
    //        activityType = null; //TODO: this.activityTypes.find(a => a.ID == n.activityType);

    //    if (activityType != null) {
    //        n.fore = activityType.ForeColor;
    //        n.back = activityType.BackColor;
    //        n.icon = activityType.Icon;
    //        n.activityName = activityType.Name;
    //        n.activityDescription = activityType.Description;
    //    }

    //    if (m.SettingsObject != null && m.SettingsObject.settings != null)
    //        n.settings = m.SettingsObject.settings;
    //    else if (m.SettingsObject != null && !_.isEmpty(m.SettingsObject) && m.SettingsObject.settings == null)
    //        n.settings = m.SettingsObject;

    //    if (n.activityType == WorkflowActivityType.FieldChange) {

    //        if (n.settings.FieldUpdate == null) n.settings.FieldUpdate = {};
    //        if (n.settings.FieldUpdate.Field == null) n.settings.FieldUpdate.Field = [];
    //        //handle obj vs array due to XML parsing
    //        //console.log('load', n.settings.FieldUpdate.Field != null, !_.isEmpty(n.settings.FieldUpdate.Field), n.settings.FieldUpdate.Field.constructor !== Array);
    //        if (n.settings.FieldUpdate.Field != null && !_.isEmpty(n.settings.FieldUpdate.Field) && n.settings.FieldUpdate.Field.constructor !== Array) {
    //            let f = _.cloneDeep(n.settings.FieldUpdate.Field);
    //            n.settings.FieldUpdate.Field = [];
    //            n.settings.FieldUpdate.Field.push(f);
    //        }

    //        //populate field names
    //        n.settings.FieldUpdate.Field.forEach(f => {
    //            let id = f['@FieldId'];
    //            let field = null; //TODO: this.fieldTypes.find(t => t.ID.toString() == id);
    //            if (field) f['@FieldName'] = field.FriendlyName;
    //        });
    //    }

    //    if (n.activityType == WorkflowActivityType.RelationshipUpdate) {
    //        if (n.settings.RelationshipUpdate == null)
    //            n.settings.RelationshipUpdate = {};
    //        if (n.settings.RelationshipUpdate.Relationship == null)
    //            n.settings.RelationshipUpdate.Relationship = {};
    //    }

    //    if (m.StepType == StepType.Start)
    //        n.category = 'start';
    //    else if (m.StepType == StepType.Finish)
    //        n.category = 'finish';
    //    else if (m.StepType == StepType.Terminate)
    //        n.category = 'finish';
    //    return n;
    //}


    //getResponsibilityName(i: number): string {
    //    let id = this.step.settings.ResponsibilityTypeID[i];
    //    if (id == null || +id < 0)
    //        return "";

    //    let r = this.responsibilities.find(r => r.ID == +id);

    //    if (r != null)
    //        return r.Name;
    //    return "";
    //}
}