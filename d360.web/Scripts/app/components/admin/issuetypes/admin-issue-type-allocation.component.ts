import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-issue-type-allocation',
    templateUrl: 'admin-issue-type-allocation.component.html',
    providers: [WorkflowService],
})

export class AdminIssueTypeAllocationComponent extends BaseComponent implements OnChanges {
    @Input() issueTypeUid: string;
    assetTypeClass = AssetTypeClass;
    formMode = FormMode.Default;
    FormMode = FormMode;
    allocations = [];
    selection = null;
    deleteCallback: Function;
    showResponsibilities: boolean;

    searchText = $localize`Search...`;
    deleteModalTitle = $localize`Are you sure you want to delete this allocation?`;

    constructor(
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService
    ) {
        super(settingsService);
        this.deleteCallback = this.delete.bind(this);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['issueTypeUid'].currentValue !== changes['issueTypeUid'].previousValue || changes['issueTypeUid'].isFirstChange) {
            this.formMode = FormMode.Default;
            this.load();
        }
    }

    load() {
        if (this.issueTypeUid == null) {
            this.allocations = [];
            return;
        }
        this.isLoading = true;
        this.workflowService.getIssueTypeAllocations(this.issueTypeUid)
            .subscribe((r) => {
                this.allocations = r;
                this.showResponsibilities = this.allocations.some((a) => a.Responsibilities && a.Responsibilities.length > 0);
                this.isLoading = false;
            });
    }

    add() {
        this.selection = null;
        this.formMode = FormMode.Adding;
    }

    delete() {
        this.isLoading = true;
        this.workflowService.deleteIssueTypeAllocation(this.issueTypeUid, this.selection.AssetTypeUid)
            .subscribe((r) => {
                this.isLoading = false;
                this.formMode = FormMode.Default;
                this.showMessageForResult(this.messagesService, r);
                this.load();
            });
    }

    editorClose() {
        this.formMode = FormMode.Default;
        this.load();
    }

    parseClassName(className: string) {
        var name = className;
        switch (className) {
            case "BusinessAsset":
                name = $localize`Business Asset`;
                break;
            case "TechnicalAsset":
                name = $localize`Technical Asset`;
                break;
            case "DiagramAsset":
                name = $localize`Diagram Asset`;
                break;
            case "ReferenceItemType":
                name = $localize`Reference Item Type`;
                break;
        }
        return name;
    }
}
