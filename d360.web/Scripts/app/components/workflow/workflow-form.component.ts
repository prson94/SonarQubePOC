
import { Input, Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { Subject, Subscription, SubscriptionLike as ISubscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { CompanySettingEnum } from '../../models/settings.model';
import { AuthenticationService } from '../../services/authentication.service';

import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowFormField, WorkflowFormFieldType, WorkflowReassignmentAsset } from '../../models/workflow.model';
import { WorkflowFormFieldsComponent } from "./workflow-form-fields.component";
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';
import { TagService } from '../../services/tag.service';
import { ResourcesService } from '../../services/resources.service';
import { Resource } from '../../models/resource.model';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-workflow-form',
    templateUrl: './workflow-form.component.html',
    providers: [WorkflowService, ResourcesService, TagService]
})

export class WorkflowFormComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private workflowId: number;
    private workflowItemStepId: number;
    private workflowItemId: number;
    private fields: WorkflowFormField[] = [];
    private description: string;
    private title: string;
    private issueObject: string;
    private issueObjectName: string;
    private issueObjectID: number;
    private issueTypeName: string;
    private objectTypeID: number;
    private typeName: string;
    private hasObjectReassign: boolean = true;
    private resourceId: number;
    private IsClearAssignementsAllowed: boolean = true;

    fieldType = WorkflowFormFieldType;
    private isCompleted: boolean = false;
    private isUserAllowedToComplete: boolean = false;
    private isItemDeleted: boolean = false;
    private isFormInvalid: boolean = false;
    private isReassignEnabled: boolean = false;
    private reassignType: string;
    private reassignAvailableTypes = [];
    private resources: Resource[] = [];

    private selectedReassignObjectId: number;
    private selectedReassignObjectType: string;
    private selectedReassignResource: number;
    private clearAssignments: boolean = false;
    private searchSub: ISubscription;
    @Input() hasCloseButton: boolean = true;

    private filteredAssetsSource = new Subject<any>();
    private filteredAssetsSub: Subscription;
    private filteredAssets: WorkflowReassignmentAsset[] = [];
    private selectedReassignmentAsset: WorkflowReassignmentAsset;
    private canViewUsers: boolean = true;

    @ViewChild('fieldsComponent', { static: false }) fieldsComponent: WorkflowFormFieldsComponent

    constructor(
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected messagesService: MessagesObservableService,
        protected resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService,
        protected tagService: TagService,
        protected titleService: Title,
        protected workflowService: WorkflowService,
        private route: ActivatedRoute,
        private location: Location,
        private router: Router,
        private authenticationService: AuthenticationService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.canViewUsers = this.authenticationService.isAdmin || this.settingsService.getSettingById(CompanySettingEnum.ShowResources).BooleanSetting.Value;

        this.sub = this.route.params.subscribe(params => {
            this.workflowId = +params['workflowId'];
            this.workflowItemStepId = +params['stepId'];
            this.workflowItemId = +params['itemId'];
            this.resourceId = +params['resourceId'];

            if (!window.history || window.history.length <= 2) this.hasCloseButton = false;
            this.load();
        });

        if (this.filteredAssetsSub) {
            this.filteredAssetsSub.unsubscribe();
        }

        this.filteredAssetsSub = this.workflowService.getWorkflowReassignmentAssets(this.filteredAssetsSource, this.workflowItemId)
            .subscribe((result) => {
                this.filteredAssets = result;
            });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
        if (this.filteredAssetsSub) {
            this.filteredAssetsSub.unsubscribe();
        }
        if (this.searchSub) {
            this.searchSub.unsubscribe();
        }
    }

    get objectUrl() {
        let path = SiteUrlHelpers.getObjectUrl(this.objectType, this.objectID, this.objectTypeID);
        return path == null ? null : '/' + path;
    }

    private onSubmit() {

        if (this.fieldsComponent.setValidators()) {
            return false;
        }
        this.fieldsComponent.prepareValuesForSubmit();

        //save form values with stepid and itemid
        this.workflowService.submitWorkflowForm(this.workflowItemId, this.workflowItemStepId, this.fields).subscribe();

        this.isCompleted = true;
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowForm(this.workflowId, this.workflowItemStepId)
            .pipe(
                map(res => {
                    this.title = res.Title;
                    this.description = res.Description;
                    this.fields = res.Fields;
                    this.isLoading = false;
                    this.isCompleted = res.IsCompleted;
                    this.isItemDeleted = res.IsItemDeleted;
                    this.isFormInvalid = res.IsFormInvalid;
                    this.objectName = res.ObjectName;
                    this.objectType = res.ObjectType;
                    this.objectID = res.ObjectID;
                    this.isUserAllowedToComplete = res.IsUserAllowedToComplete;
                    this.issueObject = res.IssueObject;
                    this.issueObjectID = res.IssueObjectID;
                    this.issueObjectName = res.IssueObjectName;
                    this.issueTypeName = res.IssueTypeName;
                    this.objectTypeID = res.ObjectTypeID;
                    this.typeName = res.TypeName;
                    this.IsClearAssignementsAllowed = res.IsClearAssignementsAllowed;
                    if (res.AllowReassignObject) {
                        this.reassignAvailableTypes.push({ value: 'object', text: 'Object' });
                    }
                    if (res.AllowReassignResource) {
                        this.reassignAvailableTypes.push({ value: 'resource', text: 'Resource' });
                        if (this.canViewUsers) {
                            this.loadResources();
                        }
                    }
                    this.hasObjectReassign = (this.reassignAvailableTypes.length > 0);
                }), map(() => {
                    window.setTimeout(() => {
                        this.fieldsComponent.setValidators();
                    }, 500);
                })).subscribe(() => { }, error => {
                    this.isLoading = false;
                    this.isCompleted = false;
                    this.isItemDeleted = true;
                    this.title = $localize`Cannot find the requested item.`;
                })

    }

    private close() {
        if (window.history.length > 1) {
            this.location.back();
        }
        else {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
        }
    }

    private reassign() {
        this.isLoading = true;
        if (this.reassignType == 'object') {
            this.workflowService.reassignObject(this.workflowItemId, this.workflowId, this.selectedReassignmentAsset.ObjectID, this.selectedReassignmentAsset.Object, this.workflowItemStepId)
                .subscribe(result => {
                    this.showMessageForResult(this.messagesService, result, $localize`Successfully Assigned`);
                    this.isLoading = false;
                    this.isCompleted = true;
                });
        }
        else if (this.reassignType == 'resource') {
            this.workflowService.reassignUser(this.workflowItemStepId, this.selectedReassignResource, this.clearAssignments).subscribe(result => {
                this.showMessageForResult(this.messagesService, result, $localize`Successfully Assigned`);
                this.isLoading = false;
                this.isCompleted = true;
            });
        }
        else {
            this.isLoading = false;
        }
    }

    private userFriendlyObjectName(objectType: string) {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
    }

    private selectItem(e: any) {
        this.selectedReassignmentAsset = e;
    }

    private loadResources() {
        this.resourcesService.getResources(false).subscribe(result => {
            this.resources = result;
        });
    }

    private filterItems(e: any) {
        this.filteredAssetsSource.next(e.query);
    }
};
