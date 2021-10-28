import { Component, OnInit, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import {  WorkflowStepDetail } from '../../../models/workflow.model';
import * as _ from 'lodash';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-workflow-monitor-step-form-details',
    template: `
<ng-container *ngIf="step != null">
    <div class="row">
        <div class="col s12">
            <div>
                Completed Forms ({{step.ItemFields['@NumberOfResponses'] ? step.ItemFields['@NumberOfResponses'] : 0 }}/{{step.ItemFields['@TotalResources']}}):
            </div>
            <ng-container *ngFor="let form of step.ItemFields.form">
                <div class="panel-section">        
                    <div *ngIf="form.resourceName != null">
                        Form completed by {{form.resourceName}}
                    </div>
                    <div *ngFor="let f of form.field">
                        <ng-container [ngSwitch]="f['@fieldtype']">
                            <ng-container *ngSwitchCase="'date'">
                                <strong>{{f['@label']}}</strong>: {{(f['@displayvalue'] == null ? getDate(f['@value']) : getDate(f['@displayvalue']))}}
                            </ng-container>
                            <ng-container *ngSwitchCase="'html'">
                                <strong>{{f['@label']}}</strong>: <div [innerHtml]="f['@value'] | safeHtml"></div>
                            </ng-container>
                            <ng-container *ngSwitchCase="'link'">
                                <strong>{{f['@label']}}</strong>: <a href="{{getUrl(f['@value'])}}" target="_blank" style="font-weight:bold">{{getName(f['@value'])}}</a>
                            </ng-container>
                            <ng-container *ngSwitchDefault>
                                <strong>{{f['@label']}}</strong>: {{(f['@displayvalue'] == null ? f['@value'] : f['@displayvalue'])}}
                            </ng-container>
                        </ng-container>
                    </div>
                </div>
            </ng-container>
            <ng-container *ngIf="step.ItemSettings.hasPendingForms == true && pendingFormList !=''">
               <div class="row panel-section warning">
                        <div class="col s11">
                                    Awaiting forms from: {{pendingFormList}}
                        </div>   
			            <div class="col s1" style="align:right;">
                             <a  *ngIf="step.IsAssignedLoginUser" style="cursor:pointer;color:#000000;" (click)="doSelect()"><i class="fa fa-edit"></i></a>
                        </div>  
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
                    let completedForms: any[] = this.step.ItemFields.form.map(f => f['@ResourceID']);

                    this.pendingFormList = this.step.AssignedUsers
                        .filter(a => completedForms.indexOf(a.ResourceID.toString()) == -1)
                        .map(a => a.FirstName + ' ' + a.LastName)
                        .join(', ');
                }
            }
        }
        this.ref.markForCheck();
    }

    getDate(val: string): string {
       if (!isNaN(Date.parse(val)))
            return new Date(val).toLocaleDateString();
        else
            return "";
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