import { Input, Component, EventEmitter, Output, OnInit} from '@angular/core';
import { Router }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { ResourcesService } from '../../services/resources.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Resource } from '../../models/resource.model';
import { Observable } from 'rxjs';
import { AssetService } from '../../services/asset.service';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-resource-groups',
    providers: [ResourcesService, AssetService],
    template: `                 
                <div class="tile tile-detail">
                   <header><ng-container i18n>Member Groups</ng-container>
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>                   
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">                     
                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                        <p-table #dt [value]="groups" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="5" [rowsPerPageOptions]="[5,10,20]">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'Name'">
                                        <ng-container i18n>Name</ng-container>
                                        <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                    </th>
                                    <th style="width:   30px "></th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th></th>
                                    <th></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr (dblclick)="doSelect(item)" [pSelectableRow]="item">
                                    <td>
                                            <a (click)="doSelect(item)">{{item.Name}}</a>
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <d3s-preview-tooltip objectType="Group" [objectId]="item.Uid"><a [routerLink]="groupUrl(item.ID)" style="cursor:pointer;"><i class="fa fa-info"></i></a></d3s-preview-tooltip>
                                        </div>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>                                         
                    </span>
                </div>
                `
})

export class ResourceGroupsComponent extends BaseComponent implements OnInit{
    @Input() resourceUid: string;

    private groups: any[];
    private user: Observable<Resource>;
    private id: number;

    constructor(
        private assetService: AssetService,
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);        
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.resourcesService.getUserGroups(this.resourceUid)
            .subscribe(res => {
                this.groups = res.items;
                this.isLoading = false;
            });
    }

    private groupUrl(id) {
        return `${SiteUrlHelpers.SITE_URL_GROUP_ROOT}/${id}`;
    }

    private doSelect(group) {
        this.assetService.getAssetLegacyUri(group.Uid).subscribe(uri => {
                this.router.navigateByUrl(uri);
            });
       
    }
}