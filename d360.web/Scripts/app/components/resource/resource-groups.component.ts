import { Input, Component, EventEmitter, Output, OnInit} from '@angular/core';
import { Router }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { ResourcesService } from '../../services/index';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-resource-groups',
    providers: [ResourcesService],
    template: `                 
                <div class="tile tile-detail">
                   <header>Member Groups
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>                   
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">                     
                        <p-dataTable sortField="Name" [sortOrder]="1"  [value]="groups" selectionMode="single" (onRowDblclick)="doSelect($event.data)" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3">                    
                            <p-column field="Name" header="Name" [sortable]="true">
                                <template let-item="rowData" pTemplate type="body">
                                    <a (click)="doSelect(item)">{{item.Name}}</a>
                                </template>
                            </p-column>                                   
                            <p-column [style]="{ 'width': '30px' }">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <d3s-tooltip objectType="Group" [objectId]="item.ID" tooltipType="preview"><a [routerLink]="groupUrl(item.ID)" style="cursor:pointer;"><i class="fa fa-info"></i></a></d3s-tooltip>                                        
                                    </div>
                               </template>
                            </p-column>
                        </p-dataTable>                                          
                    </span>
                </div>
                `
})

export class ResourceGroupsComponent extends BaseComponent implements OnInit{
    @Input() resourceId: number;

    private groups: any[];

    constructor(private router: Router, private resourcesService: ResourcesService) {
        super();        
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.resourcesService.getUserGroups(this.resourceId)
            .then(res => {
                this.groups = res;
                this.isLoading = false;
            });
    }

    private groupUrl(id) {
        return `${SiteUrlHelpers.SITE_URL_GROUP_ROOT}/${id}`;
    }

    private doSelect(group) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('group', group.ID));
    }
};