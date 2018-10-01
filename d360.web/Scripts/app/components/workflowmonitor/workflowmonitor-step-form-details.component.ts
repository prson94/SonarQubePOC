import { Component, OnInit, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import {  WorkflowStepDetail } from '../../models/workflow.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-monitor-step-form-details',
    template: `
<ng-container *ngIf="step != null">
    <div class="row">
        <div class="col s12">
            <div>
                Completed Forms ({{step.ItemFields['@NumberOfResponses']}}/{{step.ItemFields['@TotalResources']}}):
            </div>
            <ng-container *ngFor="let form of step.ItemFields.form">
                <div class="panel-section">        
                    <div *ngIf="form.resourceName != null">
                        Form completed by {{form.resourceName}}
                    </div>
                    <div *ngFor="let f of form.field">
                        <ng-container *ngIf="f['@fieldtype'] == 'date'">
                            <strong>{{f['@label']}}</strong>: {{(f['@displayvalue'] == null ? getDate(f['@value']) : getDate(f['@displayvalue']))}}
                        </ng-container>
                        <ng-container *ngIf="f['@fieldtype'] != 'date'">
                            <strong>{{f['@label']}}</strong>: {{(f['@displayvalue'] == null ? f['@value'] : f['@displayvalue'])}}
                        </ng-container>
                    </div>
                </div>
            </ng-container>
            <ng-container *ngIf="step.ItemSettings.hasPendingForms == true && step.ItemSettings.hasEmails == true">
                <div class="panel-section warning">           
                    Awaiting forms from: {{pendingFormList}}
                </div>
            </ng-container>
        </div>
    </div>
</ng-container>
`,
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepFormDetailsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() step: WorkflowStepDetail = null;
    pendingFormList: string = '';


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
        this.pendingFormList = '';
        if (this.step != null) {
            if (this.step.ItemSettings.hasPendingForms) {
                this.pendingFormList = this.step.AssignedUsers.map(a => a.FirstName + ' ' + a.LastName).join(', ');
            }
        }
        this.ref.markForCheck();
    }

    getDate(val: string): string {
        return new Date(val).toLocaleDateString();
    }
}