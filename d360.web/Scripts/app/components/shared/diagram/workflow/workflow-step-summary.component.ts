import { Component, NgZone, ChangeDetectionStrategy, Input, OnChanges, ChangeDetectorRef, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    WorkflowObjectType,
    WorkflowChangeType,
    NodeModel,
    WorkflowActivityType,
    StepType
} from '../../../../models/workflow.model';


import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';
import { WorkflowService } from '../../../../services/workflow.service';

import * as _ from 'lodash';


@Component({
    selector: 'd3s-workflow-step-summary',
    templateUrl: './workflow-step-summary.component.html',
    providers: [ResponsibilityTypeService, WorkflowService ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowStepSummaryComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() object: string;
    @Input() objectId: number;
    @Input() step: NodeModel;
    @Input() issueObject: string;

    WorkflowActivityType = WorkflowActivityType;
    StepType = StepType;
    private states = [
        //'Unknown',
        { value: '0', label: 'Pending Add' },
        { value: '1', label: 'Active' },
        { value: '2', label: 'Pending Delete' },
        { value: '3', label: 'Deleted' },
    ];

    private responsibilities = [];
    private fields = [];

    private lookups = [];
    private intersectTypes = [];

    constructor(private responsibilityService: ResponsibilityTypeService, private ref: ChangeDetectorRef, private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        this.responsibilityService.getResponsibilityTypes()
            .then(r => this.responsibilities = r)
            .then(() => this.workflowService.getWorkflowFieldTypes(this.objectId, this.object, true, this.issueObject))
            .then(r => this.fields = r)
            .then(() => this.load());
    }
    ngOnChanges() {
        this.load();
    }

    load() {
        this.isLoading = false;

        if (this.step != null && this.step.settings != null) {
            if (this.step.activityType == WorkflowActivityType.EmailNotification || this.step.activityType == WorkflowActivityType.Form) {
                if (this.step.settings['MessageRecipientType'] == 'Responsibility') {
                    if (this.step.settings.ResponsibilityTypeID != null) {
                        if (!_.isArray(this.step.settings.ResponsibilityTypeID)) {
                            let id = this.step.settings.ResponsibilityTypeID
                            delete this.step.settings.ResponsibilityTypeID;
                            this.step.settings.ResponsibilityTypeID = [];
                            this.step.settings.ResponsibilityTypeID.push(id);
                        }
                    }
                }
                if (this.step.activityType == WorkflowActivityType.Form) {
                    this.getLookups();
                }
            }
        }
        this.isLoading = false
        this.ref.markForCheck();
    }

    renderFieldChangeTableName(item: any): string {
        if (this.issueObject == "") return item['@FieldName'];
        if (typeof item['@ObjectType'] == 'undefined' || item['@ObjectType'] == 'Issue')
            return "Action Field::" + item['@FieldName'];
        else {
            let f = this.fields.find(f => f.ID == +item['@FieldId']);
            return "Asset Field::" + f.FriendlyName;
        }
    }

    getResponsibilityName(i: number): string {
        let id = this.step.settings.ResponsibilityTypeID[i];
        if (id == null || +id < 0)
            return "";

        let r = this.responsibilities.find(r => r.ID == +id);

        if (r != null)
            return r.Name;
        return "";
    }

    getLookups() {
        this.workflowService.getAllowIntersectTypes(this.object, this.objectId)
            .then(r => {
                this.intersectTypes = r;
            });

        this.workflowService.getWorkflowVersionStepFormLookups(this.object, this.objectId)
            .then(r => {
                this.lookups = r;
            });
    }

    isHtml(i: any): boolean {
        //console.log('isHtml', i, this.fields);
        if (i == null) return false;
        let f = this.fields.find(f => f.ID == +i['@FieldId']);
        if (f == null) return false;
        return f.Type == 'Html';
    }


    getValue(i: any): string {
        let val = "";
        if (i != null) {
            if (i['@ValueLabel'] != null)
                val = i['@ValueLabel'];
            else
                val = i['@Value'];
        }

        if (val != undefined && val.length > 50) {
            val = val.substr(0, 47) + '...';
        }

        return val;
    }

    private getTypeLabel(i: any) {
        switch (i['@type']) {
            case 'list':
                if (this.lookups == null)
                    return 'List';
                let list = this.lookups.find(l => l.value.toString() == i['@referenceFieldId']);
                return 'List' + (list == null ? '' : ' :: ' + list.label);
            case 'relationshipType':
                if (this.intersectTypes == null)
                    return 'Relationship';
                let rel = this.intersectTypes.find(l => l.IntersectTypeID.toString() == i['@intersectTypeId']);
                return 'Relationship' + (rel == null ? '' : (' :: ' + ((rel.PredicateName != null && rel.PredicateName.length > 0) ? `[${rel.PredicateName}] ` : ' ') + rel.TargetName));
            default:
                return (i['@type'].charAt(0).toUpperCase() + i['@type'].substr(1));
        }
    }

}