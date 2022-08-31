import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { FollowerService } from '../../../services/follower.service';
import { FollowDetail } from '../../../models/follower.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssetService } from '../../../services/asset.service';

@Component({
    selector: 'd3s-followers',
    template: `
        <div class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <header i18n>Followers of {{objectName}}</header>
                    <span *ngIf="!isLoading">

                                <input type="text"
                                       [hidden]="!showSimpleFilter"
                                       pInputText
                                       size="100"
                                       (input)="dt.filterGlobal($event.target.value, 'contains')"
                                        i18n-placeholder
                                       placeholder="Search..."
                                       class="grid-simple-filter">
                                <p-table #dt
                                         [value]="items"
                                         selectionMode="single"
                                         [metaKeySelection]="true"
                                         [globalFilterFields]="['FollowerLastName','FollowerFirstName']"
                                         sortField="FollowerLastName"
                                         [paginator]="true"
                                         [rows]="defaultInitialItemsPerPage"
                                         [rowsPerPageOptions]="defaultPagingOptions">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'FollowerLastName'">
                                                <ng-container i18n>Last Name</ng-container> 
                                                <d3s-sortIcon [field]="'FollowerLastName'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'FollowerFirstName'">
                                                <ng-container i18n>First Name</ng-container>
                                                <d3s-sortIcon [field]="'FollowerFirstName'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 28px"></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body"
                                                 let-item>
                                        <tr [pSelectableRow]="item">
                                            <td>
                                                <a (click)="doSelect(item)">{{item.FollowerLastName}}</a>
                                            </td>
                                            <td>{{item.FollowerFirstName}}</td>
                                            <td>
                                                <d3s-preview-tooltip objectType="Resource"
                                                                     [objectId]="item.ResourceID"
                                                                     icon="info"></d3s-preview-tooltip>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords"
                                                 pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first"
                                                              [rows]="dt.rows"
                                                              [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>
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
		private assetsService: AssetService,
        private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService,
        private router: Router,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
			this.uid = params['assetUid'];
			this.load();
			this.buildSecondaryNavigationByAssetUid(this.uid);
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    load() {
        this.isLoading = true;
        this.assetsService.getAsset(this.uid)
            .subscribe((res) => {
                this.objectName = res.Name ? res.Name : res.DisplayValue;
				this.followerService.getFollowers(this.uid, res.AssetTypeUid).subscribe(
					r => {
						this.items = r;

						this.isLoading = false;
					}
				);
            }
        );

    }

    private doSelect(follower: FollowDetail) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', follower.ResourceID));
    }
}
