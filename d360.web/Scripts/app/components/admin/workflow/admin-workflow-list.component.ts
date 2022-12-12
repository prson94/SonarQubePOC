import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ChangeTypeInfo, WorkflowChangeType, WorkflowListItem } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { State } from '../../../models/asset.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-workflow-list',
    providers: [WorkflowService],
    templateUrl: 'admin-workflow-list.component.html'
})

export class AdminWorkflowListComponent extends BaseComponent implements OnInit {
    @Output() onViewClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();
    @Output() onEditClick = new EventEmitter();
    @Output() onAddClick = new EventEmitter();

    private items: WorkflowListItem[] = [];
    public selection: WorkflowListItem;

    private changeTypes: ChangeTypeInfo[] = [];

    private columns: any[] = [
        { datafield: 'Name', text: $localize`Name`, type: 'text' },
        { datafield: 'TypeName', text: $localize`Type Name`, type: 'text' },
        { datafield: 'Type', text: $localize`Type`, type: 'text' },
        { datafield: 'ChangeTypeName', text: $localize`Change Type`, type: 'text' },
        { datafield: 'State', text: $localize`Active`, type: 'State' },
        { datafield: 'UpdatedOn', text: $localize`Updated On`, type: 'date' },
        { datafield: 'UpdatedBy', text: $localize`Updated By`, type: 'text' },
        { datafield: 'Published', text: $localize`Status`, type: 'text' },
    ];

    get globalFilterFields(): string[] {
        return this.columns.map((c) => c.datafield);
    }

    constructor(
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService,
        protected router: Router
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    cloneWorkflow(uid) {

        this.isLoading = true;
        this.workflowService.cloneWorkflowDiagramModel(uid)
            .subscribe((x) => {
                this.isLoading = false;
                this.onEditClick.emit({ uid: x, isClone: true });
            });

    }





    onNavigate(uid: string) {
        this.isLoading = true;
        this.workflowService.getWorkflowTypeId(uid).subscribe(
            (x) => this.navigate(x)
        );
    }

    load() {
        this.isLoading = true;

        this.workflowService.getChangeTypes()
            .pipe(
                map((r) => this.changeTypes = r),
                map(() =>
                    this.workflowService.getAdminTypes()
                        .subscribe((r) => {
                            const workflowItems: WorkflowListItem[] = [];


                            r.filter((x) => x.State === 'Active' || x.State === 'InActive').forEach((x) => {
                                const workflowItem: WorkflowListItem = new WorkflowListItem();

                                workflowItem.Name = x.Name;
                                workflowItem.TypeName = x.ActionTypeUid ? x.ActionType : x.AssetType ? x.AssetType : x.RelationshipType;
                                workflowItem.ChangeType = WorkflowChangeType[x.ChangeType];
                                workflowItem.State = State[x.State];
                                workflowItem.Type = x.Type;
                                workflowItem.UpdatedOn = x.UpdatedOn;
                                workflowItem.UpdatedBy = x.UpdatedBy;
                                workflowItem.Published = x.PublishedVersionUid ? `Version ${x.PublishedVersion} Published` : `Unpublished`;
                                workflowItem.Uid = x.WorkflowTypeUid;
                                workflowItems.push(workflowItem);

                            });
                            this.items = workflowItems;
                            if (this.items.length > 0) {
                                this.selection = this.items[0];
                            }
                            this.items.forEach((i) => {
                                var ChangeTypeDescription = this.changeTypes.find((c) => c.ID === i.ChangeType);
                                if (ChangeTypeDescription) {
                                    i.ChangeTypeName = ChangeTypeDescription.Description;
                                }
                                else {
                                    i.ChangeTypeName = "";
                                }
                            });
                        })),
                map(() => this.isLoading = false))
            .subscribe();

    }

    navigate(id: number) {
        this.router.navigateByUrl(`/monitor/type/${id}?tab=monitor`);
    }
}

