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
            <div *ngFor="let e of displayEmails; let i = index">
                <ng-container *ngIf="showAll == true || (showAll == false && i < 5)">
                    {{e['@address']}} <span *ngIf="e.name != null">(<d3s-preview-tooltip [objectType]="'Resource'" [objectId]="e.id"><a>{{e.name}}</a></d3s-preview-tooltip>)</span>  
                </ng-container>
            </div>
            <div *ngIf="displayEmails.length > 5 && showAll == false">
                <a (click)="toggleShowAll()" style="cursor: pointer">Show All</a>
            </div>
            <div *ngIf="displayEmails.length > 5 && showAll == true">
                <a (click)="toggleShowAll()" style="cursor: pointer">Show Less</a>
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
    displayEmails: any[] = [];
    showAll = false;
    

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
        if (this.step != null && this.step.ItemSettings.emails.email != null) {
            let sorted = this.step.ItemSettings.emails.email.slice();

            sorted.sort((a, b) => {
                if (a['@address'] < b['@address']) return -1
                if (a['@address'] > b['@address']) return 1
                return 0;
            });

            this.displayEmails = sorted;
        }
        this.ref.markForCheck();
    }

    toggleShowAll() {
        this.showAll = !this.showAll;
        this.ref.markForCheck();
    }
}