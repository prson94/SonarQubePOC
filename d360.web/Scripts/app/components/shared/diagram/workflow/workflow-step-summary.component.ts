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

    constructor(private responsibilityService: ResponsibilityTypeService, private ref: ChangeDetectorRef, private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        this.responsibilityService.getResponsibilityTypes()
            .then(r => this.responsibilities = r)
            .then(() => this.workflowService.getWorkflowFieldTypes(this.objectId, this.object, true))
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
            }
        }
        this.isLoading = false
        this.ref.markForCheck();
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

        if (val.length > 50) {
            val = val.substr(0, 47) + '...';
        }

        return val;
    }

}