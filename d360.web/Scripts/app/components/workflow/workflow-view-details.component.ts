import { Input, Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';
import { StepType, WorkflowActivityType } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { HeaderActionsService } from '../../services/header-actions.service';
import { HeaderActions } from '../../models/header.model';
import { CompanySettingsService } from '../../services/settings.service';


@Component({
    selector: 'd3s-workflow-view-detail',
    templateUrl: 'workflow-view-details.component.html',
    providers: [WorkflowService]
})

export class WorkflowViewDetailsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private workflowInstance: string;
    workflowId: number;
    private workflowUid: string;
    private details: any;
    private item: any;
    itemStepId: any;
    detailVisible: boolean;
    private workflowTypeId: number;

    constructor(
        private headerActionsService: HeaderActionsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        protected titleService: Title,
        protected workflowService: WorkflowService,
        private route: ActivatedRoute,
        private router: Router
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    ngOnInit() {
        this.showHideFollow(false);

        this.breadcrumbsService.clearCurrentObjectInfo();
        this.breadcrumbsService.showBreadcrumb(new Breadcrumb('Workflow Item Status'));

        this.setBrowserTitle(this.titleService, 'Workflow Item Status');

        this.sub = this.route.params.subscribe(params => {
            this.workflowInstance = params['workflowInstance'];
            if (this.isUid(this.workflowInstance)) {
                this.workflowUid = this.workflowInstance;
            }
            else
                this.workflowId = +this.workflowInstance;

            if (!this.workflowId) {
                this.workflowService.getWorkflowId(this.workflowUid)
                    .subscribe(res => {
                        this.workflowId = res;
                        this.load();
                    })
            }
            else {
                this.load();
            }
        });
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowDetailsV2(this.workflowId)
            .subscribe(res => {
                for (let item of res.ItemSteps) {
                    if (!res.Steps) {
                        item.StepName = "";
                        continue;
                    }
                    var step = res.Steps.filter(x => x.ID == item.StepID);

                    if (!step || step.length == 0) {
                        item.StepName = "(unresolved)";
                        continue;
                    }
                    item.StepName = step[0].Name;
                    item.StepType = StepType[step[0].StepType];
                    item.ActivityType = WorkflowActivityType[step[0].ActivityType];
                }

                this.details = res;
                if (res && res.Workflow && res.Workflow.ID)
                    this.workflowTypeId = res.Workflow.ID;
                this.isLoading = false;
                if (res.ActionAsset && res.ActionAsset.Object) {
                    this.buildSecondaryNavigationForObject(res.ActionAsset.ObjectID, res.ActionAsset.Object);
                } else if (res.Item) {
                    if (res.Item.Object) {
                        this.buildSecondaryNavigationForObject(res.Item.ObjectID, res.Item.Object);
                    }
                }

            });
    }

    private isUid(value: string) {
        let regex = /[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}/;
        return regex.test(value);
    }

    private showHideFollow(show: boolean) {
        let headerActions: HeaderActions = new HeaderActions();
        headerActions.showFollow = show;
        this.headerActionsService.setCurrentHeaderActions(headerActions);
    }

    ngOnDestroy() {
        this.showHideFollow(true);
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    private getStepName(itemStep: any): string {
        if (!this.details || !this.details.Steps) return "";
        var step = this.details.Steps.filter(x => x.ID == itemStep.StepID);

        if (!step || step.length == 0) return "";
        return step[0].Name;
    }

    private showForm(item: any) {
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${this.workflowTypeId}/${item.ID}/${item.ItemID}`);
    }

    stepChange(event) {
        if (event) {
            this.itemStepId = event.ID;
            this.detailVisible = true;
        } else {
            this.itemStepId = null;
            this.detailVisible = false;
        }
    }
}