import { Component, OnInit, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, SimpleChanges } from '@angular/core';
import { WorkflowStepDetail } from '../../../models/workflow.model';
import { BaseComponent } from '../base.component';
import { WorkflowHelpers } from '../../../static/workflow-helpers';
import { CompanySettingsService } from '../../../services/settings.service';


@Component({
    selector: 'd3s-workflow-monitor-step-email-details',
    templateUrl: `./workflowmonitor-step-email-details.component.html`,
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepEmailDetailsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() step: WorkflowStepDetail = null;
    @Input() isAggregate: boolean = false;
    displayEmails: any[] = [];
    showAll = false;
    emailSettings: any;
    helper = WorkflowHelpers;
    

    constructor(
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges) {
        this.load();
    }

    load() {
        if (this.step != null) {
            if (!this.isAggregate && this.step.ItemSettings.emails.email != null) {
                let sorted = this.step.ItemSettings.emails.email.slice();

                sorted.sort((a, b) => {
                    if (a['@address'] < b['@address']) return -1
                    if (a['@address'] > b['@address']) return 1
                    return 0;
                });

                this.displayEmails = sorted;
            }

            if (this.isAggregate)
                this.emailSettings = this.step.EventSettings;
            else
                this.emailSettings = this.step.Settings;

        }
        this.ref.markForCheck();
    }

    toggleShowAll() {
        this.showAll = !this.showAll;
        this.ref.markForCheck();
    }
}