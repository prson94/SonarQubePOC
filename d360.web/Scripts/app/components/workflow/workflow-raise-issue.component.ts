import { debounceTime } from 'rxjs/operators';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { WorkflowService } from '../../services/workflow.service';
import { HeaderBreadcrumbService}  from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { ObjectDetailService } from '../../services/object-detail.service';
import { TagService } from '../../services/tag.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowIssueType, ActionEditorModel } from '../../models/workflow.model';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { ObjectDetail } from '../../models/object-detail.model';
import {Tag } from '../../models/tag.model';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';
import { HeaderActionsService } from '../../services/header-actions.service';
import { HeaderActions } from '../../models/header.model';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { StringConstants } from '../../static/string-constants';

declare var CompanySettings;

@Component({
    selector: 'd3s-workflow-raise-issue',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div class="row"
             *ngIf="!isLoading">
            <div class="col s12">
                <div class="tile tile-detail">
                    <header>Take Action</header>
                    <div class="row">
                        <div class="col s12">
                            <div class="FieldName">{{actionMessage}}</div>
                            <div *ngIf="objectDetail"
                                 style="padding-left:20px"><label><input name="selObject"
                                                                         type="radio"
                                                                         [(ngModel)]="selectedOption"
                                                                         (click)="selectCurrent()"
                                                                         value="current">{{objectDetail.DisplayValue ? objectDetail.DisplayValue : objectDetail.Name}}
                            </label></div>
                            <div>
                                <label style="padding-left:20px"><input name="selObject"
                                                                        type="radio"
                                                                        value="other"
                                                                        [(ngModel)]="selectedOption"
                                                                        (click)="selectOther()">Other item</label>
                                <div *ngIf="selectedOption=='other'"
                                     style="padding-left:40px">
                                    <p-autoComplete size="100"
                                                    scrollHeight="400px"
                                                    name="other"
                                                    [inputStyle]="{width:'100%'}"
                                                    [(ngModel)]="term"
                                                    [suggestions]="terms"
                                                    (completeMethod)="search($event)"
                                                    placeholder="Select an item"
                                                    field="TextPath"
                                                    (onSelect)="selectItem()">
                                        <ng-template let-item
                                                     pTemplate="item">
                                            <span style="color:#999999;">{{userFriendlyObjectName(item.Displayobject)}}
                                                - <span *ngIf="item.ObjectTypeName">{{item.ObjectTypeName}}
                                                    -</span></span> {{item.TextPath}}
                                            <span *ngIf="item.GoverningDomain">({{item.GoverningDomain}})</span>
                                        </ng-template>
                                    </p-autoComplete>
                                </div>
                            </div>
                        </div>
                        <div class="col s12"
                             *ngIf="selectedObjectId != null && selectedObjectType != null">
                            <div>&nbsp;</div>
                            <div class="FieldName">What are you reporting?</div>
                        </div>
                        <div class="col s12"
                             *ngIf="selectedObjectId != null && selectedObjectType != null">
                            <div style="padding-left:40px">
                                <select required
                                        name="availableTypes"
                                        style="width:100%"
                                        placeholder="Choose a type"
                                        [(ngModel)]="issueType">
                                    <option></option>
                                    <option *ngFor="let p of issueTypes"
                                            [ngValue]="p">{{p.Name}}</option>
                                </select>
                            </div>
                            <p *ngIf="issueType"
                               style="padding-left:40px"
                               [innerHtml]="issueType.Description"></p>
                        </div>
                        <d3s-dynamic-editor *ngIf="issueType"
                                            [hasHeader]="false"
                                            [objectID]="issueType?.ID"
                                            [useTypeUidForDefinition]="true"
                                            [objectTypeUid]="issueType?.Uid"
                                            objectType="Issue"
                                            [selectedObject]="selectedObjectType"
                                            [selectedObjectID]="selectedObjectId"
                                            (saveClick)="save($event)"
                                            (closeClick)="cancel()"></d3s-dynamic-editor>
                    </div>
                </div>
            </div>
        </div>
    `,
    providers: [WorkflowService, ObjectDetailService, TagService]
})

export class WorkflowRaiseIssueComponent extends BaseComponent implements OnInit, OnDestroy {

    private issue: string;
    private selectedObjectType: string;
    private selectedObjectId: number;
    private selectedAssetUid: string;
    private selectedAssetTypeUid: string;
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
        private headerActionsService: HeaderActionsService,
        webAnalyticsService: WebAnalyticsService,
        private messagesService: MessagesObservableService,
        secondaryNavService: SecondaryNavService) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.webAnalyticsService = webAnalyticsService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Take Action');
        this.showHideFollow(false);
        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.id)
            this.objectID = this.headerBreadcrumbService.currentObject.id;

        if (this.headerBreadcrumbService.currentObject && this.headerBreadcrumbService.currentObject.type)
            this.objectType = this.headerBreadcrumbService.currentObject.type;
                
        this.loadDetails(this.objectID, this.objectType);
        
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Take Action'));
        this.clearSidebar();
        this.secondaryNavService.setCurrentArea('Take Action', 'fa-paper-plane-o', null);
        this.secondaryNavService.showHeader(true);
    }

    ngOnDestroy(): void {
        this.showHideFollow(true);
        if (this.searchSub) this.searchSub.unsubscribe();
    }

    private showHideFollow(show: boolean) {
        let headerActions: HeaderActions = new HeaderActions();
        headerActions.showFollow = show;
        this.headerActionsService.setCurrentHeaderActions(headerActions);
    }

    private loadDetails(objectId, objectType) {
        if (objectId == undefined || objectType == undefined) {
            return;
        }

        this.isLoading = true;

        this.objectDetailService.getObject(objectId, objectType).subscribe(
            res => {
                this.objectDetail = res;
                this.selectedOption = 'current';
                this.selectedObjectId = this.objectID;
                this.selectedObjectType = this.objectType;
                if (this.selectedObjectType == StringConstants.ObjectArtifact || this.selectedObjectType == StringConstants.ObjectTaxonomy || this.selectedObjectType == StringConstants.ObjectRule || this.selectedObjectType == StringConstants.ObjectPolicy) {
                    this.selectedAssetUid = res.UID ?? res['Uid'];
                }              
                if (this.selectedObjectType == StringConstants.ObjectArtifactType || this.selectedObjectType == StringConstants.ObjectTaxonomyType || this.selectedObjectType == StringConstants.ObjectRuleType || this.selectedObjectType == StringConstants.ObjectPolicyType) {
                    this.selectedAssetTypeUid = res.UID ?? res['Uid'];
                } 

                this.isLoading = false;

                this.loadIssueTypes()   
            }
        );
    }

    private loadIssueTypes() {
        this.isLoading = true;
        let params = { _assetUid: "", _assetTypeUid: "" };
        if (this.selectedAssetUid) {
            params._assetUid = this.selectedAssetUid;            
        }        
        this.workflowService.getWorkflowIssueTypes(this.selectedObjectType, this.selectedObjectId, params)
            .subscribe(result => {
                this.issueTypes = result;
                if (this.issueTypes != null && this.issueTypes.length == 1) this.issueType = this.issueTypes[0];
                this.isLoading = false;
            });
    }

    private save(data) {
        this.isLoading = true;        
        let values: any = {};
        let action: ActionEditorModel = new ActionEditorModel();
        action.Fields = {};

        if (this.selectedAssetTypeUid) {
            action.AssetTypeUid = this.selectedAssetTypeUid;
        } else {
            action.AssetUid = this.selectedAssetUid;
        }

        //takes the form and convert any array values to , separated string values
        for (var p in data.item) {
            if (data.item.hasOwnProperty(p)) {
                if (Array.isArray(data.item[p])) {
                    values[p] = data.item[p].join();
                } else {
                    values[p] = data.item[p];
                }
            }
        }

        //populate field collection
        for (var p in values) {
            if (p.toUpperCase() == "ISSUETYPEID") {
                //ignore
            }            
            else {
                action.Fields[p] = values[p];
            }
        }

        this.workflowService.raiseIssues(this.issueType.Uid, action)
            .subscribe(res => {
                this.showMessageForApiResponse(this.messagesService, res);
                this.isLoading = false;
                this.location.back();
            });
    }

    private cancel() {
        this.location.back();
    }

    private search(event) {
        this.searchSub = this.tagService.getTags(event.query, 'Resource').pipe(
            debounceTime(400))
            .subscribe(data => {
                this.terms = data;
            });
    }

    private selectItem() {
        this.selectedObjectType = this.term.Object;
        this.selectedObjectId = this.term.ObjectID;
        this.selectedAssetUid = this.term.AssetUid;
        this.loadIssueTypes();
    }

    private userFriendlyObjectName(objectType: string) {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
    }

    private selectCurrent() {
        this.issueType = null;
        this.selectedOption = 'current';
        this.selectedObjectId = this.objectID;
        this.selectedObjectType = this.objectType;
        this.loadIssueTypes()
    }

    private selectOther() {
        this.selectedOption = 'other';
        this.issueType = null;
        if (this.term != null && this.term.Object != null && this.term.ObjectID != null) {
            this.selectItem();
        } else {
            this.selectedObjectId = null;
            this.selectedObjectType = null;
        }
    }
}
