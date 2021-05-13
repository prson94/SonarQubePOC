import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { ResourceResponsibilityTypeCount } from '../../models/responsibility-type.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { GridColumn, GridField} from '../../models/grid-definition.model';
import { GridDefinitionService } from '../../services/grid-definition.service';

@Component({
    selector: 'd3s-community-responsibility-count',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>      
                <span *ngIf="!isLoading">
                    <header>Users Assigned As {{responsibilityTypeName}}</header>                            
                        <p-table #dt [value]="users" [scrollable]="true"
                         selectionMode="single" [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected);" [metaKeySelection]="true" sortField="OwnedItemCount" [sortOrder]="-1" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'FirstName'" [style.width] = "columnWidth > 0 ? '250px' : null">
                                        Name
                                        <d3s-sortIcon [field]="'FirstName'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'OwnedItemCount'" [style.width] = "columnWidth > 0 ? '250px' : null">
                                        Owned Items
                                        <d3s-sortIcon [field]="'OwnedItemCount'"></d3s-sortIcon>
                                    </th>
                                    <th *ngFor="let column of columns"
                                        [pSortableColumn]="column.sortable ? column.datafield : null"
                                        [style.width]="columnWidth > 0 ? columnWidth + 'px' : null">
                                        {{column.text}}
                                        <d3s-sortIcon *ngIf="column.sortable" [field]="column.datafield"></d3s-sortIcon>
                                    </th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr [pSelectableRow]="item">
                                    <td [style.width] = "columnWidth > 0 ? '250px' : null">
                                        <d3s-preview-tooltip objectType="Resource" [objectId]="item.ResourceID" (click)="selectResource(item)">{{item.FirstName}} {{item.LastName}}</d3s-preview-tooltip>
                                    </td>
                                    <td [style.width] = "columnWidth > 0 ? '250px' : null">{{item.OwnedItemCount}}</td>
                                    <td *ngFor="let column of columns"
                                    [style.width]="columnWidth > 0 ? columnWidth + 'px' : null">
                                        <d3s-dynamic-field-value [column]="column"
                                                                 [fields]="fields" [item]="item" 
                                                                 [useApiName]="true"
                                                                 [isDateUTC]="true"></d3s-dynamic-field-value>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>             
                </span>
                `,
    providers: [ResponsibilityTypeService, GridDefinitionService],
})

export class CommunityResponsibilityCountComponent extends BaseComponent implements OnChanges {
    @Input() responsibilityTypeUid: string;
    @Input() responsibilityTypeName: string;
    @Input() selected: ResourceResponsibilityTypeCount;

    @Output() selectedChange = new EventEmitter();

    private users: ResourceResponsibilityTypeCount[] = [];
    columns: GridColumn[] = [];
    fields: GridField[] = [];
    columnWidth: number;

    constructor(private responsibilityTypeService: ResponsibilityTypeService,
        private gridDefinitionService: GridDefinitionService,
        private router: Router
    ) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes["responsibilityTypeUid"] && "" + this.responsibilityTypeUid !== "") {
            this.load();
        }
    }

    selectResource(item: ResourceResponsibilityTypeCount) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(StringConstants.ObjectResource, item.ResourceID));
    }

    load() {
        this.isLoading = true;
        this.getFieldsDefinition();
        this.responsibilityTypeService.getResourceResponsibilityByType(this.responsibilityTypeUid).
            subscribe(result => {
                this.users = result;
                this.isLoading = false;
            });
    }
    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(1,"ResourceType").subscribe(
            (result) => {
                this.columns = result.Columns.filter((x) => x.isCustomField === true);
                this.fields = result.Fields.filter((x) => x.isCustomField === true);
                if (this.columns && this.columns.length > 1) {
                    this.columnWidth = 250;
                }
                else
                {
                    this.columnWidth = 0;
                }
            }
        );
    }

}