import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from "@angular/core";
import { WorkflowService } from "../../../services/workflow.service";
import { BaseComponent } from "../../shared/base.component";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { AssetTypeClass } from "../../../models/asset.model";
import { ResponsibilityTypeService } from "../../../services/responsibility-type.service";
import { AssetTypeService } from "../../../services/asset-type.service";
import { SelectItem } from "primeng/api";
import { AllocationAPIModel, AllocationRequestModel } from "../../../models/workflow.model";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: "d3s-admin-issue-type-allocation-editor",
    templateUrl: "admin-issue-type-allocation-editor.component.html",
    providers: [WorkflowService, AssetTypeService, ResponsibilityTypeService],
})

export class AdminIssueTypeAllocationEditorComponent extends BaseComponent {

    @Input() allocation: AllocationAPIModel;
    @Input() allocations: AllocationAPIModel[];
    @Input() issueTypeUid: string;
    @Output() closeClick = new EventEmitter();
    assetTypes: SelectItem[] = [];
    title: string = $localize`New Issue Type Allocation`;
    responsibilityList: SelectItem[] = [];
    selection: AllocationRequestModel = new AllocationRequestModel();

    labelRequired = $localize`Value required`;
    labelSave = $localize`Save`;
    labelClose = $localize`Close`;

    constructor(
        private workflowService: WorkflowService,
        protected messagesService: MessagesObservableService,
        protected assetTypeService: AssetTypeService,
        protected responsibilityTypeService: ResponsibilityTypeService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnInit() {
        this.isLoading = true;
        if (this.allocation) {
            this.title = $localize`Edit Issue Type Allocation`;
            this.selection.AssetTypeUid = this.allocation.AssetTypeUid;
            this.assetTypeChanged();
        }

        this.assetTypeService.getAssetTypes(null)
            .subscribe((result) => {
                var data = result
                    .filter((r) =>
                        (this.allocation?.AssetTypeUid === r.uid || !this.allocations.some((a) => a.AssetTypeUid === r.uid))
                        && this.isAllowedClass(r.Class.ID)
                    );

                data.forEach((r) => {
                    this.assetTypes.push({
                        label: r.Class.Name + ": " + r.Path,
                        value: r.uid
                    });
                });

                this.assetTypes.sort((a, b) => a.label.localeCompare(b.label));
                this.isLoading = false;
            });
    }

    isAllowedClass(atc: AssetTypeClass) {

        switch (atc) {
            case AssetTypeClass.DiagramAsset:
            case AssetTypeClass.Reference:
                return false;
            default:
                return true;
        }
    }

    assetTypeChanged() {
        if (this.selection.AssetTypeUid) {
            this.responsibilityTypeService.getAdminResponsibilityTypes(this.selection.AssetTypeUid).
                subscribe((res) => {
                    this.responsibilityList = [];
                    this.selection.ResponsibilityTypeUid = [];

                    res.forEach((o) => {
                        this.responsibilityList.push({
                            label: o.Name,
                            value: o.uid
                        });
                    });

                    if (this.allocation && this.selection?.ResponsibilityTypeUid.length === 0 && this.allocation.AssetTypeUid === this.selection.AssetTypeUid) {
                        this.selection.ResponsibilityTypeUid = this.allocation.Responsibilities.map((r) => r.Uid);
                    }

                    this.cdRef.detectChanges();
                });
        }

    }

    save(e: any) {
        this.isLoading = true;
        if (this.allocation) {
            this.workflowService.deleteIssueTypeAllocation(this.issueTypeUid, this.allocation.AssetTypeUid)
                .subscribe((o) => {
                    this.workflowService.postIssueTypeAllocation(this.issueTypeUid, this.selection)
                        .subscribe((r) => {
                            this.closeClick.emit();
                            this.isLoading = false;
                            r.message = $localize`Allocation Updated Successfully`;
                            this.showMessageForResult(this.messagesService, r);
                        });
                });
        } else {
            this.workflowService.postIssueTypeAllocation(this.issueTypeUid, this.selection)
                .subscribe((r) => {
                    this.closeClick.emit();
                    this.isLoading = false;
                    this.showMessageForResult(this.messagesService, r);
                });
        }
    }
}