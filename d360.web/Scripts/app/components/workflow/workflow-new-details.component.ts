import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import {
    WorkflowType,
    WorkflowAssignmentDetail,
    WorkflowAssignmentSummary,
    BulkWorkflowFormModel,
    BulkWorkflowReassignModel
} from '../../models/workflow.model';
import { Title } from '@angular/platform-browser';
import { WorkflowService } from '../../services/workflow.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AuthenticationService } from '../../services/authentication.service';
import { map } from 'rxjs/operators';
import { CompanySettingsService } from '../../services/settings.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-workflow-new-detail',
    templateUrl: 'workflow-new-details.component.html',
    providers: [WorkflowService]
})

export class WorkflowNewDetailComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() workflowTypeId: number;
    @Input() hasCloseButton: boolean = true;
    
    private resourceID: number = null;
    private version: number;
    private stepId: number;
    private assignmentSummary: WorkflowAssignmentSummary;
    private isMe:boolean = true;
    private sub: any;
    private tempWorkflowtype = WorkflowType;
    private items: WorkflowAssignmentDetail[];
    private workflow: any;
    private selection: WorkflowAssignmentDetail[] = [];
    showBulkFormEditor = false;
    showBulkReassignEditor = false;
    private bulkEditorModel: BulkWorkflowFormModel;
    private bulkReassignModel: BulkWorkflowReassignModel;
    private fromMail: boolean = false;
    private isAdmin: boolean = false;
    private clearOtherAssignments: boolean = false;

    constructor(
        protected authenticationService: AuthenticationService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        protected titleService: Title,
        protected workflowService: WorkflowService,
        private route: ActivatedRoute,
        private location: Location,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();

        this.sub = this.route.params.subscribe(params => {
            this.isLoading = true;
            this.workflowTypeId = +params['workflowTypeId'];
            this.resourceID = +params['resourceID'];
            this.version = +params['version'];
            this.stepId = +params['stepId'];
            this.fromMail = params['fromMail'] === '1' ? true : false;

            this.isMe = this.resourceID ? this.resourceID == CurrentResourceID : true;
            this.authenticationService.checkCurrentUserAdmin().subscribe((x) => {
                this.isAdmin = x;
            });
            this.headerBreadcrumbService.clearBreadcrumbs();    

            this.load();
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getAssignedWorkflowInstancesByTypeId(this.workflowTypeId, this.resourceID, this.version, this.stepId)
            .pipe(
                map(res => {
                    this.selection = [];
                    this.items = res.items;
                    this.workflow = res.workflow;
                    }),
                map(() => this.workflowService.getAssignedWorkflowInstancesSummary(this.workflowTypeId, this.resourceID, this.version, this.stepId)
                    .subscribe(res => {
                        this.isLoading = false;
                        this.assignmentSummary = res.item;
                    })))
                .subscribe();
               
    }

    private bulkRespond() {
        if (this.selection != null) {
            if (this.selection.length >= 2) {

                this.bulkEditorModel = new BulkWorkflowFormModel();
                this.bulkEditorModel.ItemStepIDs = this.selection.map(i => i.ItemStepID);

                this.showBulkFormEditor = true;
            } else if (this.selection.length == 1) {
                this.open(this.selection[0]);
            }
        }
    }

    private bulkReassign() {
        if (this.selection != null) {
            this.bulkReassignModel = new BulkWorkflowReassignModel();
            this.bulkReassignModel.ItemStepIDs = this.selection.map(i => i.ItemStepID);
            this.bulkReassignModel.OriginalAssigneeResourceID = isNaN(this.resourceID) ? CurrentResourceID : this.resourceID;
            this.bulkReassignModel.StepName = this.assignmentSummary.StepName;
            this.bulkReassignModel.StepHasFormEmails = this.assignmentSummary.SendFormEmail;
            let noOfItemsCanClearAssignments = this.selection.filter((x) => { return x.countAssigned > 1 && x.responseType.toLowerCase() === "firstresponse"; }).length;
            //only show option to bulk clear other assignments if all have the ability to clear assignments 
            this.bulkReassignModel.IsClearOtherAssignmentsAllowed = (noOfItemsCanClearAssignments === this.selection.length);
            this.showBulkReassignEditor = true;
        }
    }

    private close() {
        if (this.fromMail) {
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_HOME_ROOT}`);
        }
        this.location.back();
    }



    private open(item: WorkflowAssignmentDetail) {       
        if (isNaN(this.resourceID)) {
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${this.workflowTypeId}/${item.ItemStepID}/${item.ItemID}`);

        } else {
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${this.workflowTypeId}/${item.ItemStepID}/${item.ItemID}?resourceId=${this.resourceID}`);

        }
    }
}