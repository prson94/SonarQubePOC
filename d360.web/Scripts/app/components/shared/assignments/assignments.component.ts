import { Component, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../base.component';
import { WorkflowService } from '../../../services/workflow.service';
import { ResourcesService } from '../../../services/resources.service';
import { Count } from '../../../models/counts.model';
import { WorkflowType } from '../../../models/workflow.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-assignments',
    providers: [WorkflowService, ResourcesService],
    templateUrl: `assignments.component.html`
})

export class AssignmentsComponent extends BaseComponent implements OnInit {
    @Input() resourceId = -1;
    @Output() showItemDetail = new EventEmitter();
    @Input() isSidePanel: boolean = false;

    counts: Count[] = [];
    private selected: Count;
    private daysToLookBack: number = 7;
    private isLoaded: boolean = false;
    private items: any[] = [];
    private resource: any = null;


    constructor(
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService) {
        super(settingsService);
    }

    ngOnInit() {
        if (!this.isLoaded) this.load();
    }

    private load() {
        this.isLoading = true;
        let loadResource = (this.resourceId != null && this.resourceId >= 0);

        this.workflowService.getMyCounts(this.daysToLookBack, (loadResource ? this.resourceId : null))
            .subscribe(res => {
                this.counts = res.filter(item => (item.Total > 0));
                if (loadResource)
                    this.resourcesService.getResource(this.resourceId)
                        .subscribe(r => {
                            this.items = r.items;
                            if (this.items.length > 0) {
                                this.resource = this.items[0];
                            }
                            this.isLoading = false;
                            this.isLoaded = true;
                        });
                else {
                    this.isLoading = false;
                    this.isLoaded = true;
                }
            });
    }

    private doSelect(item) {

        this.showItemDetail.emit({
            workflowType: this.getWorkflowType(item),
            resourceID: this.resourceId,
            workflowId: item.Id,
            version: item.Version,
            stepId: item.StepId
        });
    }

    private getWorkflowType(item): WorkflowType {
        if (!item) return null;

        switch (item.Name.toUpperCase()) {
            case "CERTIFY ARTIFACT":
                return WorkflowType.CertifyArtifact;
            case "CHALLENGE":
                return WorkflowType.ChallengeArtifact;
            case "PROPOSE NEW ARTIFACT":
                return WorkflowType.SuggestNewArtifact;
            case "ACTIONS":
                return WorkflowType.WorkIssue;
        }
        return WorkflowType.None;
    }
}


