import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService, WorkflowService, WebAnalyticsService, ObjectDetailService, TagService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SocialCommentType } from '../../models/social.model';
import { WorkflowType, WorkflowIssueType } from '../../models/workflow.model';
import { Subscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { ObjectDetail } from '../../models/object-detail.model';
import { Tag } from '../../models/tag.model';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';

@Component({
    selector: 'd3s-workflow-raise-issue',    
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                        <header>Report a problem</header>                        
                            <div class="row">
                                <div class="col s12">
                                    <div class="FieldName">What item would you like to report a problem with?</div>
                                    <div *ngIf="objectDetail" style="padding-left:20px"><label><input name="selObject" type="radio"  [(ngModel)]="selectedOption" (click)="selectedObjectId=objectId;selectedObjectType=objectType;" value="current">{{objectDetail.Name}}</label></div>
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
                                            <template let-item>
                                                <span style="color:#999999;">{{userFriendlyObjectName(item.Object)}} - <span *ngIf="item.ObjectTypeName">{{item.ObjectTypeName}} -</span></span> {{item.TextPath}}
                                            </template>                  
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

export class WorkflowRaiseIssueComponent extends BaseComponent implements OnInit {
    private issue: string;
    private objectType: string;
    private objectId: number;
    private selectedObjectType: string;
    private selectedObjectId: number;    
    private objectDetail: ObjectDetail;
    private terms: Tag[] = [];
    private term: Tag;
    private selectedOption: string = 'other';
    private issueType: WorkflowIssueType;
    private issueTypes: WorkflowIssueType[] = [];

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
        this.setBrowserTitle(this.titleService, 'Take Action');

        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.id)
            this.objectId = this.headerBreadcrumbService.currentObject.id;

        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.type)
            this.objectType = this.headerBreadcrumbService.currentObject.type;

        this.loadDetails(this.objectId, this.objectType);

        this.loadIssueTypes();

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Take Action'));
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
                this.selectedOption = 'current';
                this.selectedObjectId = this.objectId;
                this.selectedObjectType = this.objectType;
                this.isLoading = false;
            });
    }

    private loadIssueTypes() {
        this.isLoading = true;
        this.workflowService.getWorkflowIssueTypes()
            .then(result => {
                this.issueTypes = result;                
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