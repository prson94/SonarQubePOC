import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    LinkModel,
    WorkflowActivityType,
    TransitionType,
} from '../../../../models/workflow.model';
import { CompanySettingsService } from '../../../../services/settings.service';


@Component({
    selector: 'd3s-workflow-transition-summary',
    templateUrl: './workflow-transition-summary.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowTransitionSummaryComponent extends BaseComponent {
    @Input() link: LinkModel;

    WorkflowActivityType = WorkflowActivityType;
    TransitionType = TransitionType;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }
}