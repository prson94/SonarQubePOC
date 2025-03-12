import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChangeTypeInfo, WorkflowChangeType, WorkflowListItem } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { State } from '../../../models/asset.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../../../components/shared/base.component';
import { LoadingComponent } from '../../../_shared/components/loading';
import { ColumnFilterComponent } from '../../../_shared/components/column-filter';
import { SortIconComponent } from '../../../_shared/components/sort-icon';
import { GridPagingInfoComponent } from '../../../_shared/components/grid-paging-info';
import { TilesModule } from '../../../components/shared/tiles/tiles.module';
import { TableModule } from 'primeng/table';

@Component({
	selector: 'workflow-type-list',
	templateUrl: 'list.html',
	standalone: true,
	imports: [
		CommonModule, TableModule, TilesModule,
		LoadingComponent, ColumnFilterComponent, SortIconComponent, GridPagingInfoComponent
	]
})
export class WorkflowTypeList extends BaseComponent implements OnInit {
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
                                const ChangeTypeDescription = this.changeTypes.find((c) => c.ID === i.ChangeType);
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
}

