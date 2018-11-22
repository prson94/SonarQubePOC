import { Component, OnInit, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { WorkflowHelpers } from '../../static/workflow-helpers';
import { WorkflowStepDetail, EmailTaskRecipientType } from '../../models/workflow.model';

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
                    {{helper.recipientTypeName(emailSettings.MessageRecipientType)}}
                </span>
            </div>
            <div *ngIf="emailSettings.MessageRecipientType == 'Responsibility'">
                <div class="FieldName">
                    Email Responsibilities:
                </div>
                <div *ngFor="let res of step.ItemSettings.Responsibilities">
                    {{res.name}}
                </div>
            </div>
        </div>
        <div class="col s6" *ngIf="step.ItemSettings.hasEmails == true">
            <span class="FieldName">Recipients:</span>
            <div *ngFor="let e of displayEmails; let i = index">
                <ng-container *ngIf="showAll == true || (showAll == false && i < 5)">
                    {{e['@address']}} <span *ngIf="e.name != null">(<d3s-preview-tooltip [objectType]="'Resource'" [objectId]="e.id"><a>{{e.name}}</a></d3s-preview-tooltip>)
                    <i *ngIf="e.responsibility">({{e.responsibility}})</i>
                    </span>  
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
    <div *ngIf="!isAggregate" class="row">
        <div class="col s12">
            <div>
                <span class="FieldName">
                    Include Previous Form Responses: 
                </span>
                <span>
                    {{emailSettings.IncludePreviousFormResponses == 'true' ? 'Yes' : 'No'}}
                </span>
            </div>
        </div>
    </div>
    <div class="row">
        <div class="col s12">
            <div class="FieldName">
                Email Subject:
            </div>
            <div [innerHtml]="emailSettings.MessageSubjectTemplate">
            </div>
        </div>
    </div>
    <div class="row">
        <div class="col s12">
            <div class="FieldName">
                Email Body:
            </div>
            <div class="panel-section" [innerHtml]="emailSettings.MessageBodyTemplate">
            </div>
        </div>
    </div>
`,
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