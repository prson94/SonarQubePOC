import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { ResourceResponsibilityTypeCount } from '../../models/responsibility-type.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-community-responsibility-count',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>      
                <span *ngIf="!isLoading">
                    <header i18n>Users Assigned As {{responsibilityTypeName}}</header>                            
                        <p-table #dt [value]="users" selectionMode="single" [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected);" [metaKeySelection]="true" sortField="OwnedItemCount" [sortOrder]="-1" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'FirstName'">
                                        <ng-container i18n>Name</ng-container>
                                        <d3s-sortIcon [field]="'FirstName'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'OwnedItemCount'">
                                        <ng-container i18n>Owned Items</ng-container>
                                        <d3s-sortIcon [field]="'OwnedItemCount'"></d3s-sortIcon>
                                    </th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr [pSelectableRow]="item">
                                    <td>
                                        <d3s-preview-tooltip objectType="Resource" [objectId]="item.ResourceID" (click)="selectResource(item)">{{item.FirstName}} {{item.LastName}}</d3s-preview-tooltip>
                                    </td>
                                    <td>{{item.OwnedItemCount}}</td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>             
                </span>
                `,
    providers: [ResponsibilityTypeService],
})

export class CommunityResponsibilityCountComponent extends BaseComponent implements OnChanges {
    @Input() responsibilityTypeUid: string;
    @Input() responsibilityTypeName: string;
    @Input() selected: ResourceResponsibilityTypeCount;

    @Output() selectedChange = new EventEmitter();

    private users: ResourceResponsibilityTypeCount[] = [];

    constructor(
        private responsibilityTypeService: ResponsibilityTypeService,
        protected settingsService: CompanySettingsService,
        private router: Router
    ) {
        super(settingsService);
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
        this.responsibilityTypeService.getResourceResponsibilityByType(this.responsibilityTypeUid).
            subscribe(result => {
                this.users = result;
                this.isLoading = false;
            });
    }
}