import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { ResourceResponsibilityTypeCount } from '../../models/responsibility-type.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-community-responsibility-count',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>      
                <span *ngIf="!isLoading">
                    <header>Users Assigned As {{responsibilityTypeName}}</header>          
                    <p-dataTable #dt [globalFilter]="gb" sortField="OwnedItemCount" sortOrder="-1" [value]="users" selectionMode="single" [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected);" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions">                    
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="FirstName" header="Name" [sortable]="true" sortable="custom" (sortFunction)="columnSort($event)" >
                            <template let-col let-item="rowData" pTemplate type="body">                            
                                <d3s-tooltip objectType="Resource" [objectId]="item.ResourceID" tooltipType="preview"><a (click)="selectResource(item)">{{item.FirstName}} {{item.LastName}}</a></d3s-tooltip>
                            </template>
                        </p-column>           
                        <p-column field="OwnedItemCount" header="Owned Items" sortable="true"></p-column>                                                                
                    </p-dataTable>                  
                </span>
                `,
    providers: [ResponsibilityTypeService],
})

export class CommunityResponsibilityCountComponent extends BaseComponent implements OnChanges {
    @Input() responsibilityTypeId: number;
    @Input() responsibilityTypeName: string;
    @Input() selected: ResourceResponsibilityTypeCount;

    @Output() selectedChange = new EventEmitter();

    private users: ResourceResponsibilityTypeCount[] = [];
    
    constructor(private responsibilityTypeService: ResponsibilityTypeService,
        private router: Router
    ) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['responsibilityTypeId'] && this.responsibilityTypeId > 0)
            this.load();
    }

    selectResource(item: ResourceResponsibilityTypeCount) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(StringConstants.ObjectResource, item.ResourceID));
    }

    load() {
        this.isLoading = true;
        this.responsibilityTypeService.getResourceResponsibilityByType(this.responsibilityTypeId).
            then(result => {
                this.users = result;
                this.isLoading = false;
            });
    }

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.users = _.orderBy(this.users, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
};