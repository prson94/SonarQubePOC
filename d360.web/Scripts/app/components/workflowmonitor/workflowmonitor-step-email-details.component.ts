import { Component, OnInit, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { WorkflowStepDetail } from '../../models/workflow.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-monitor-step-email-details',
    template: `
    <div class="row">
        <div class="col s6">
            <div>
                <span class="FieldName">
                    Email Recipient Type:
                </span>
                <span>
                    {{step.Settings.MessageRecipientType}}
                </span>
            </div>
            <div>
                <span class="FieldName">
                    Include Previous Form Responses: 
                </span>
                <span>
                    {{step.Settings.IncludePreviousFormResponses == 'true' ? 'Yes' : 'No'}}
                </span>
            </div>
        </div>
        <div class="col s6" *ngIf="step.ItemSettings.hasEmails == true">
            <span class="FieldName">Recipients:</span>
            <div *ngFor="let e of step.ItemSettings.emails.email">
                    {{e['@address']}} <span *ngIf="e.name != null">({{e.name}})</span>   
            </div>
        </div> 
    </div>
    <div class="row">
        <div class="col s12">
            <div class="FieldName">
                Email Subject:
            </div>
            <div [innerHtml]="step.Settings.MessageSubjectTemplate">
            </div>
        </div>
    </div>
    <div class="row">
        <div class="col s12">
            <div class="FieldName">
                Email Body:
            </div>
            <div class="panel-section" [innerHtml]="step.Settings.MessageBodyTemplate">
            </div>
        </div>
    </div>
`,
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepEmailDetailsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() step: WorkflowStepDetail = null;

    constructor(private ref: ChangeDetectorRef) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges) {
        this.load();
    }

    load() {
        this.ref.markForCheck();
    }
}