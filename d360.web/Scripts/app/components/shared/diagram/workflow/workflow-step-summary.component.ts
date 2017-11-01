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

import * as _ from 'lodash';


@Component({
    selector: 'd3s-workflow-step-summary',
    templateUrl: './workflow-step-summary.component.html',
    providers: [ ResponsibilityTypeService ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowStepSummaryComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() step: NodeModel;

    WorkflowActivityType = WorkflowActivityType;
    StepType = StepType;

    private responsibilities = [];

    constructor(private responsibilityService: ResponsibilityTypeService, private ref: ChangeDetectorRef) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        this.responsibilityService.getResponsibilityTypes()
            .then(r => this.responsibilities = r)
            .then(() => this.load());
    }
    ngOnChanges() {
        this.load();
    }

    load() {
        this.isLoading = true;
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
}