import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { NgForm } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService, WorkflowService, WebAnalyticsService, ObjectDetailService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SocialCommentType } from '../../models/social.model';
import { WorkflowType } from '../../models/workflow.model';
import { Subscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { ObjectDetail } from '../../models/object-detail.model';

@Component({
    selector: 'd3s-workflow-raise-issue',
    styles: [`
            [type="radio"]:not(:checked), [type="radio"]:checked {
                position: initial;                 
                visibility: initial;
            }
        `],
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                        <header>Raise A New Issue</header>
                        <form (ngSubmit)="onSubmit()" #issueForm="ngForm">                        
                            <div class="row">
                                <div class="col s12">
                                    <div class="FieldName">Raise An Issue For</div>
                                    <div *ngIf="objectDetail"><label><input name="objectSelection" type="radio" [(ngModel)]="issueObject" value="current">{{objectDetail.Name}}</label></div>
                                    <div><label><input name="objectSelection" type="radio" value="other" [(ngModel)]="issueObject">Other item</label></div>
                                </div>
                                <div class="col s12" *ngIf="issueObject">
                                    <div class="FieldName">Issue Details</div>
                                    <div><p-editor name="Issue" [style]="{'height':'400px'}" [(ngModel)]="issue" #issueText="ngModel"></p-editor></div>                                                        
                                    <div [hidden]="issueText.valid || issueText.pristine">Issue details are required</div>
                                </div>       
                                <div class="col s12">&nbsp;</div>
                                <div class="col s12">
                                    <button pButton type="submit" [disabled]="!issueForm.form.valid" style="width: 150px;" label="Save"></button>                            
                                    <button pButton type="button" (click)="cancel();" label="Close" style="width: 150px;"></button>
                                </div>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        `,
    providers: [WorkflowService, ObjectDetailService]
})

export class WorkflowRaiseIssueComponent extends BaseComponent implements OnInit {
    private issue: string;
    private objectType: string;
    private objectId: number;
    private issueObject: string;
    private objectDetail: ObjectDetail;

    constructor(
        private workflowService: WorkflowService,
        private objectDetailService: ObjectDetailService,
        private location: Location,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,        
        webAnalyticsService: WebAnalyticsService,
        rightSidebarService: RightSidebarService) {
        super(rightSidebarService, webAnalyticsService);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Raise Issue');

        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.id)
            this.objectId = this.headerBreadcrumbService.currentObject.id;

        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.type)
            this.objectType = this.headerBreadcrumbService.currentObject.type;

        this.loadDetails(this.objectId, this.objectType);

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Raise Issue'));
        this.clearSidebar();        
    }

    ngOnDestroy() {
    
    }
    
    private loadDetails(objectId, objectType) {        
        if (objectId == undefined || objectType == undefined) return;
        this.isLoading = true;
        this.objectDetailService.getObject(objectId, objectType).then(
            res => {
                this.objectDetail = res;
                this.isLoading = false;
            });
    }

    private onSubmit() {
        this.workflowService.raiseIssue(this.objectId, this.objectType, this.issue)
            .then(res => {
                this.location.back();
            });
    }

    private cancel() {
        this.location.back();
    }

}