import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { NgForm } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService, WorkflowService, WebAnalyticsService, ObjectDetailService, TagService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SocialCommentType } from '../../models/social.model';
import { WorkflowType } from '../../models/workflow.model';
import { Subscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { ObjectDetail } from '../../models/object-detail.model';
import { Tag } from '../../models/tag.model';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';

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
                                    <div class="FieldName">What would you like to raise an issue for?</div>
                                    <div *ngIf="objectDetail"><label><input name="selObject" type="radio"  [(ngModel)]="selectedOption" (click)="selectedObjectId=objectId;selectedObjectType=objectType;" value="current">{{objectDetail.Name}}</label></div>
                                    <div>
                                        <label><input name="selObject" type="radio" value="other" [(ngModel)]="selectedOption" (click)="showObjectSearch=true">Other item</label>
                                        <div *ngIf="showObjectSearch && selectedOption=='other'" style="padding-left:20px"><p-autoComplete size="100"                                                
                                                scrollHeight="400px"
                                                name="other"
                                                [(ngModel)]="term" 
                                                [suggestions]="terms" 
                                                (completeMethod)="search($event)"                                                 
                                                placeholder="Select an item"
                                                field="TextPath" 
                                                (onSelect)="selectItem()">     
                                            <template let-item>
                                                <span style="color:#999999;">{{userFriendlyObjectName(item.Object)}} - <span *ngIf="item.ObjectTypeName">{{item.ObjectTypeName}} -</span></span> {{item.TextPath}}
                                            </template>                  
                                        </p-autoComplete></div>                                        
                                    </div>
                                </div>                            
                                <div class="col s12" *ngIf="selectedObjectId&&selectedObjectType">
                                    <div class="FieldName">Issue Description</div>
                                    <div><p-editor name="Issue" [style]="{'height':'400px'}" [(ngModel)]="issue" #issueText="ngModel"></p-editor></div>                                                        
                                    <div [hidden]="issueText.valid || issueText.pristine">Issue details are required</div>
                                </div>       
                                <div class="col s12">&nbsp;</div>
                                <div class="col s12">
                                    <button pButton type="submit" [disabled]="!issueForm.form.valid" label="Save"></button>                            
                                    <button pButton type="button" (click)="cancel();" label="Cancel"></button>
                                </div>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        `,
    providers: [WorkflowService, ObjectDetailService, TagService]
})

export class WorkflowRaiseIssueComponent extends BaseComponent implements OnInit {
    private issue: string;
    private objectType: string;
    private objectId: number;
    private selectedObjectType: string;
    private selectedObjectId: number;
    private showObjectSearch: boolean = false;
    private objectDetail: ObjectDetail;
    private terms: Tag[] = [];
    private term: Tag;
    private selectedOption: string;

    constructor(
        private tagService: TagService,
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
        this.isLoading = true;            
        this.workflowService.raiseIssue(this.selectedObjectId, this.selectedObjectType, this.issue)
            .then(res => {
                this.isLoading = false;
                this.location.back();
            });
    }

    private cancel() {
        this.location.back();
    }

    private search(event) {
        this.tagService.getTags(event.query).then(data => {
            this.terms = data;
        });
    }

    private selectItem() {
        this.selectedObjectType = this.term.Object;
        this.selectedObjectId = this.term.ObjectID;       
    }

    private userFriendlyObjectName(objectType: string) {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
    }

}