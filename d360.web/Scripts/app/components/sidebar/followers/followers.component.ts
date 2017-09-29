import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { FollowerService } from '../../../services/follower.service';
import { FollowDetail } from '../../../models/follower.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { ObjectDetailService } from '../../../services/object-detail.service';

@Component({
    selector: 'd3s-followers',
    template: `
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <header>Followers of {{objectName}}</header>
                            <span *ngIf="!isLoading">
                                <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="FollowerLastName" sortOrder="1" [globalFilter]="gb" [value]="items" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" selectionMode="single">
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                    <p-column field="FollowerLastName" header="Last Name" sortable="true">
                                        <ng-template let-item="rowData" pTemplate type="body">
                                                <a (click)="doSelect(item)">{{item.FollowerLastName}}</a>
                                            </ng-template>
                                    </p-column>
                                    <p-column field="FollowerFirstName" header="First Name" sortable="true"></p-column>
                                    <p-column [style]="{'width':'28px'}" >
                                        <ng-template let-item="rowData" pTemplate type="body">
                                            <d3s-tooltip objectType="Resource" [objectId]="item.ResourceID" tooltipType="preview"><i class="fa fa-info"></i></d3s-tooltip>
                                        </ng-template> 
                                    </p-column>     
                                </p-dataTable>
                            </span>
                        </div>
                    </div>
                </div>
        `,
    providers: [FollowerService, ObjectDetailService],
})

export class FollowersComponent extends BaseComponent implements OnInit, OnDestroy {        
    private sub: any;

    private items: FollowDetail[] = new Array<FollowDetail>();

    constructor(
        private followerService: FollowerService,
        private objectDetailService: ObjectDetailService,
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {        
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];

            this.load();
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    
    load() {
        this.isLoading = true;
        this.objectDetailService.getObject(this.objectID, this.objectType).then(res => {
            this.objectName = res.DisplayValue;
        });

        this.followerService.getFollowers(this.objectType, this.objectID)
            .then(r => {
                this.items = r;
                this.isLoading = false;
            });
    }

    private doSelect(follower: FollowDetail) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', follower.ResourceID));
    }
}