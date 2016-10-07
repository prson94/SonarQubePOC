import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, GroupService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { GroupSearchResultModel } from '../../models/group.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-group-list',
    providers: [GroupService],  
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" >    
                            <div class="row" *ngIf="!isLoading">                        
                                <div class="col s12">
                                    <header>Groups                                
                                        <d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                                                     
                                    </header>      
                                    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                                                                     
                                    <p-dataTable [globalFilter]="gb" [value]="groups" selectionMode="single" [rows]="10" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showGroup();" >
                                        <p-column field="ID" header="ID" [sortable]="true" [style]="{width:'10%'}" [filter]="!showSimpleFilter"></p-column>                                                                                                                        
                                        <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'60%'}" [filter]="!showSimpleFilter"></p-column>                                                                                                                                                                
                                        <p-column field="NumberOfMembers" header="Member Count" [sortable]="true" [style]="{width:'20%'}" [filter]="!showSimpleFilter"></p-column>
                                        <p-column [style]="{ 'width': '30px' }">
                                            <template let-col let-item="rowData" pTemplate type="body">
                                                <div class="RowTools">
                                                    <a [routerLink]="groupUrl(item.ID)" style="cursor:pointer;"><i class="fa fa-info"></i></a>
                                                </div>
                                            </template>
                                        </p-column>
                                    </p-dataTable>      
                                </div>
                            </div>                            
                        </div>                        
                    </div>
                </div>
                `
})

export class GroupListComponent extends BaseComponent implements OnInit {

    private groups: GroupSearchResultModel[] = [];
    private selected: GroupSearchResultModel;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private groupService: GroupService,
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();        
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Groups');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Groups'));

        this.load();
    }

    private groupUrl(id): string {
        return SiteUrlHelpers.SITE_URL_GROUP_ROOT + '/' + id;
    }

    private load() {
        this.isLoading = true;
        this.groupService.getGroupList()
            .then(res => {
                this.isLoading = false;
                this.groups = res;
            });
    }

    private showGroup() {
        if (!this.selected) {
            console.log("ERROR : NO GROUP SELECTED TO NAVIGATE TO.");

            return;
        }
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_GROUP_ROOT}/${this.selected.ID}`);
    }
    
};