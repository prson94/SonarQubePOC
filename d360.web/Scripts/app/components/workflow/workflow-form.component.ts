
import { debounceTime } from 'rxjs/operators';
import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { NgForm, FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { close } from 'fs';

import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowFormField, WorkflowFormFieldType } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Tag } from '../../models/tag.model';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';
import { TagService } from '../../services/tag.service';
import { ResourcesService } from '../../services/resources.service';
import { Resource } from '../../models/resource.model';
import { MessagesService } from '../../services/messages.service';

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

    fieldType = WorkflowFormFieldType;
    private isCompleted: boolean = false;
    private isUserAllowedToComplete: boolean = false;
    private isItemDeleted: boolean = false;
    private isReassignEnabled: boolean = false;
    private reassignType: string;
    private reassignAvailableTypes = [];
    private term: Tag;
    private terms: Tag[] = [];
    private resources: Resource[] = [];

    private selectedReassignObjectId: number;
    private selectedReassignObjectType: string;
    private selectedReassignResource: number;
    private searchSub: ISubscription;
    @Input() hasCloseButton: boolean = true;

    constructor(private route: ActivatedRoute,
        private location: Location,
        private router: Router,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected workflowService: WorkflowService,
        protected tagService: TagService,
        protected resourcesService: ResourcesService,
        protected messagesService: MessagesService
    ) {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();

        this.sub = this.route.params.subscribe(params => {
            this.workflowId = +params['workflowId'];
            this.workflowItemStepId = +params['stepId'];
            this.workflowItemId = +params['itemId'];
            if (!window.history || window.history.length <= 2) this.hasCloseButton = false;
            this.load();
        });
    }


    ngOnDestroy() {
        this.sub.unsubscribe();
        if (this.searchSub) this.searchSub.unsubscribe();
    }

    get objectUrl() {
        let path = SiteUrlHelpers.getObjectUrl(this.objectType, this.objectID, this.objectTypeID);
        return path == null ? null : '/' + path;
    }

    private onSubmit() {
        for (var i = 0; i < this.fields.length; i++) {
            if (Array.isArray(this.fields[i].Value)) {
                this.fields[i].Value = this.fields[i].Value.join();
            }
        }
        //save form values with stepid and itemid
        this.workflowService.submitWorkflowForm(this.workflowItemId, this.workflowItemStepId, this.fields);

        this.isCompleted = true;
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowForm(this.workflowId, this.workflowItemStepId)
            .then(res => {
                this.title = res.Title;
                this.description = res.Description;
                this.fields = res.Fields;
                this.isLoading = false;
                this.isCompleted = res.IsCompleted;
                this.isItemDeleted = res.IsItemDeleted;
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
                if (res.AllowReassignObject)
                    this.reassignAvailableTypes.push({ value: 'object', text: 'Object' });
                if (res.AllowReassignResource) {
                    this.reassignAvailableTypes.push({ value: 'resource', text: 'Resource' });
                    this.loadResources();
                }
                this.hasObjectReassign = (this.reassignAvailableTypes.length > 0);
            }).catch(res => {
                this.isLoading = false;
                this.isCompleted = false;
                this.isItemDeleted = true;
                this.title = "Cannot find the requested item.";
            });
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
            this.workflowService.reassignObject(this.workflowItemId, this.workflowId, this.selectedReassignObjectId, this.selectedReassignObjectType, this.workflowItemStepId).then(result => {
                this.showMessageForResult(this.messagesService, result, 'Successfully Assigned');
                this.isLoading = false;
                this.isCompleted = true;
            });
        }
        else if (this.reassignType == 'resource') {
            this.workflowService.reassignUser(this.workflowItemStepId, this.selectedReassignResource).then(result => {
                this.showMessageForResult(this.messagesService, result, 'Successfully Assigned');
                this.isLoading = false;
                this.isCompleted = true;
            });
        }
        else {
            this.isLoading = false;
        }
    }

    private search(event) {
        this.searchSub = this.tagService.getTags(event.query, 'Resource').pipe(
            debounceTime(400))
            .subscribe(data => {
                this.terms = data;
            });
    }

    private userFriendlyObjectName(objectType: string) {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
    }

    private selectItem() {
        this.selectedReassignObjectType = this.term.Object;
        this.selectedReassignObjectId = this.term.ObjectID;
    }

    private loadResources() {
        this.resourcesService.getResources().then(result => {
            this.resources = result;
        });
    }
};
