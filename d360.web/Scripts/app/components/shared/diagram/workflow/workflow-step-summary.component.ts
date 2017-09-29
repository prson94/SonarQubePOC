import { Component, NgZone, ChangeDetectionStrategy, Input, OnChanges, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    WorkflowObjectType,
    WorkflowChangeType,
    NodeModel,
    WorkflowActivityType,
    StepType
} from '../../../../models/workflow.model';

import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';


@Component({
    selector: 'd3s-workflow-step-summary',
    templateUrl: './workflow-step-summary.component.html',
    providers: [ ResponsibilityTypeService ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowStepSummaryComponent extends BaseComponent implements OnChanges {
    @Input() step: NodeModel;

    WorkflowActivityType = WorkflowActivityType;
    StepType = StepType;

    private responsibilityTypeName = '';

    constructor(private responsibilityTypeService: ResponsibilityTypeService, private ref: ChangeDetectorRef) {
        super();
    }

    ngOnChanges() {
        //console.log('ngOnChanges', this.step);

        if (this.step != null && this.step.settings != null) {
            if (this.step.activityType == WorkflowActivityType.EmailNotification || (this.step.activityType == WorkflowActivityType.Form && this.step.settings['SendFormEmail'] != null && this.step.settings['SendFormEmail'].toString() == 'true')) {
                if (this.step.settings['MessageRecipientType'] == 'Responsibility') {
                    if (this.step.settings['ResponsibilityTypeID'] != null && this.step.settings['ResponsibilityTypeName'] == null) {
                        this.responsibilityTypeService.getResponsibilityType(+this.step.settings['ResponsibilityTypeID'])
                            .then(r => this.step.settings['ResponsibilityTypeName'] = r.Name)
                            .then(() => this.ref.markForCheck());
                    }
                }
            }
        }
    }
}