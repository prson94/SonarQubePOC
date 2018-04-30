import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { WorkflowService } from '../../services/workflow.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TagService } from '../../services/tag.service';

import { Breadcrumb } from '../../models/breadcrumb.model';
import { SocialCommentType } from '../../models/social.model';
import { WorkflowType, WorkflowIssueType } from '../../models/workflow.model';
import { Subscription, ISubscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { ObjectDetail } from '../../models/object-detail.model';
import { Tag } from '../../models/tag.model';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';

declare var CompanySettings;

@Component({
    selector: 'd3s-workflow-raise-issue',    
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                        <header>Take Action</header>                        
                            <div class="row">
                                <div class="col s12">
                                    <div class="FieldName">{{actionMessage}}</div>
                                    <div *ngIf="objectDetail" style="padding-left:20px"><label><input name="selObject" type="radio"  [(ngModel)]="selectedOption" (click)="selectedObjectId=objectID;selectedObjectType=objectType;" value="current">{{objectDetail.DisplayValue ? objectDetail.DisplayValue : objectDetail.Name}}</label></div>
                                    <div>
                                        <label style="padding-left:20px"><input name="selObject" type="radio" value="other" [(ngModel)]="selectedOption">Other item</label>
                                        <div *ngIf="selectedOption=='other'" style="padding-left:40px"><p-autoComplete size="100"                                                
                                                scrollHeight="400px"
                                                name="other"
                                                [inputStyle]="{width:'100%'}"
                                                [(ngModel)]="term" 
                                                [suggestions]="terms" 
                                                (completeMethod)="search($event)"                                                 
                                                placeholder="Select an item"
                                                field="TextPath" 
                                                (onSelect)="selectItem()">     
                                            <ng-template let-item pTemplate="item">
                                                <span style="color:#999999;">{{userFriendlyObjectName(item.Object)}} - <span *ngIf="item.ObjectTypeName">{{item.ObjectTypeName}} -</span></span> {{item.TextPath}} <span *ngIf="item.GoverningDomain">({{item.GoverningDomain}})</span>
                                            </ng-template>                  
                                        </p-autoComplete></div>                                        
                                    </div>
                                </div>       
                                <div class="col s12" *ngIf="selectedObjectId&&selectedObjectType">
                                    <div>&nbsp;</div>
                                    <div class="FieldName">What are you reporting?</div>                                    
                                </div>                 
                                <div class="col s12" *ngIf="selectedObjectId&&selectedObjectType">
                                    <div style="padding-left:40px"><select required name="availableTypes" style="width:100%" placeholder="Choose a type" [(ngModel)]="issueType">                                            
                                          <option></option>
                                          <option *ngFor="let p of issueTypes" [ngValue]="p">{{p.Name}}</option>
                                    </select></div>                       
                                    <p *ngIf="issueType" style="padding-left:40px" [innerHtml]="issueType.Description"></p>                   
                                </div>                                                          
                                <d3s-dynamic-editor *ngIf="issueType" [hasHeader]="false" [objectID]="issueType?.ID" objectType="Issue" (saveClick)="save($event)" (closeClick)="cancel()"></d3s-dynamic-editor>                                       
                            </div>                        
                    </div>
                </div>
            </div>
        `,
    providers: [WorkflowService, ObjectDetailService, TagService]
})

export class WorkflowRaiseIssueComponent extends BaseComponent implements OnInit,OnDestroy {
    
    private issue: string;        
    private selectedObjectType: string;
    private selectedObjectId: number;    
    private objectDetail: ObjectDetail;
    private terms: Tag[] = [];
    private term: Tag;
    private selectedOption: string = 'other';
    private issueType: WorkflowIssueType;
    private issueTypes: WorkflowIssueType[] = [];
    private actionMessage: string = CompanySettings.ActionMessage;
    private searchSub: ISubscription;

    constructor(
        private tagService: TagService,
        private workflowService: WorkflowService,
        private objectDetailService: ObjectDetailService,
        private location: Location,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,        
        webAnalyticsService: WebAnalyticsService,
        rightSidebarService: RightSidebarService) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.webAnalyticsService = webAnalyticsService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Take Action');

        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.id)
            this.objectID = this.headerBreadcrumbService.currentObject.id;

        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.type)
            this.objectType = this.headerBreadcrumbService.currentObject.type;

        this.loadDetails(this.objectID, this.objectType);

        this.loadIssueTypes();

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Take Action'));
        this.clearSidebar();     
    }

    ngOnDestroy(): void {
        if (this.searchSub) this.searchSub.unsubscribe();
    }
    
    private loadDetails(objectId, objectType) {        
        if (objectId == undefined || objectType == undefined) return;
        this.isLoading = true;
        this.objectDetailService.getObject(objectId, objectType).then(
            res => {
                this.objectDetail = res;
                this.selectedOption = 'current';
                this.selectedObjectId = this.objectID;
                this.selectedObjectType = this.objectType;
                this.isLoading = false;
            });
    }

    private loadIssueTypes() {
        this.isLoading = true;
        this.workflowService.getWorkflowIssueTypes()
            .then(result => {
                this.issueTypes = result;
                if (this.issueTypes != null && this.issueTypes.length == 1) this.issueType = this.issueTypes[0];
                this.isLoading = false;
            });
    }

    private save(data) {        
        this.isLoading = true;        
        data.item.ObjectID = this.selectedObjectId;
        data.item.ObjectType = this.selectedObjectType;        
        this.workflowService.raiseIssue(data.item)
            .then(res => {
                this.isLoading = false;
                this.location.back();
            });
    }

    private cancel() {
        this.location.back();
    }

    private search(event) {
        this.searchSub=  this.tagService.getTags(event.query)
            .debounceTime(400)
            .subscribe(data => {
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