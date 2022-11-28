import { Component, OnInit, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowStepDetail } from '../../../models/workflow.model';
import * as _ from 'lodash';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-workflow-monitor-step-form-details',
    templateUrl: 'workflowmonitor-step-form-details.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepFormDetailsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() step: WorkflowStepDetail = null;
    pendingFormList: string = '';


    constructor(
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges) {
        this.load();
    }

    load() {
        this.pendingFormList = '';
        if (this.step != null) {
            if (this.step.ItemSettings.hasPendingForms) {
                if (this.step.AssignedUsers != null) {
                    const completedForms: any[] = this.step.ItemFields.form.map((f) => f['@ResourceID']);

                    this.pendingFormList = this.step.AssignedUsers
                        .filter((a) => completedForms.indexOf(a.ResourceID.toString()) == -1)
                        .map((a) => a.FirstName + ' ' + a.LastName)
                        .join(', ');
                }
            }
        }
        this.ref.markForCheck();
    }

    getDate(val: string): string {
        if (!isNaN(Date.parse(val)))
            {return new Date(val).toLocaleDateString();}
        else
            {return "";}
    }

    getUrl(val: string): string {
        var url = val.split('|');
        return url[1];
    }

    getName(val: string): string {
        var name = val.split('|');
        return name[0];
    }

    doSelect() {
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${this.step.TypeID}/${this.step.ItemStepID}/${this.step.ItemID}`);

    }
}