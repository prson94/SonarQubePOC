import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges
} from '@angular/core';
import { BaseComponent } from '../base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { WorkflowActivityType, WorkflowItemStep } from '../../../models/workflow.model';
import { WorkflowHelpers } from '../../../static/workflow-helpers';
import { Router } from '@angular/router';
import { StateService } from '../../../services/state.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-workflow-monitor-step-grid',
    templateUrl: './workflowmonitor-step-grid.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepGridComponent extends BaseComponent implements OnChanges {
    @Input() itemSteps: WorkflowItemStep[] = [];
    @Output() selectionChange = new EventEmitter();

    helper = WorkflowHelpers;
    selection: WorkflowItemStep = null;

    showAssigneeColumn = false;
    allowSort = false;

    constructor(
        protected settingsService: CompanySettingsService,
        private stateService: StateService,
        private ref: ChangeDetectorRef,
        private router: Router
        ) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['itemSteps'] != null && (changes['itemSteps'].isFirstChange || changes['itemSteps'].currentValue !== changes['itemSteps'].previousValue)) {
            this.load();
        }
    }

    load() {
        if (this.itemSteps != null) {
            this.showAssigneeColumn = (this.itemSteps.find((i) => i.ActivityType === WorkflowActivityType.Form) != null);
            let index = this.itemSteps.findIndex((x) => x.StepID === this.stateService.workflowItemFilters.stepId && x.ItemID === this.stateService.workflowItemFilters.itemId);
            index = (index === -1) ? 0 : index;
            this.selection = this.itemSteps[index];
            this.selectionChange.emit(this.selection);
        }
        this.ref.markForCheck();
    }

	doSelect(item: WorkflowItemStep) {
		this.router.navigateByUrl(SiteUrlHelpers.federateUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${item.TypeID}/${item.ID}/${item.ItemID}`));
    }

    rowClick(item: any) {
        this.stateService.workflowItemFilters.stepId = item.StepID;
        this.selectionChange.emit(item);
    }
}