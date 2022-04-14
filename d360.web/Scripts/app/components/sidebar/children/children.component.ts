import {Input, Component, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../../shared/base.component';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';
import { ObjectStatisticChildItem } from '../../../models/object-statistics.model';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-children',
    template: `
        <div class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <header i18n>Children of {{objectName}}</header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div *ngIf="!isLoading"
                         class="row">
                        <div class="col l3 s12 child-container"><!--left nav-->
                            <div class="row child"
                                 *ngFor="let child of children"
                                 [ngClass]="{'active' : selected==child}"
                                 (click)="selected=child;">
                                <div class="col s10 name">{{child.Name}}</div>
                                <div class="col s2 count center">{{child.Count}}</div>
                            </div>
                        </div>
                        <div class="col l9 s12" *ngIf="parentUid">
                            <d3s-artifact-item-child-grid [parentUid]="parentUid" [displayName]="displayName"
                                                          [artifactTypeId]="selected?.TypeID"
                                                          [assettypename]="selected?.Name"></d3s-artifact-item-child-grid>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    providers: [ObjectStatisticsService, ObjectDetailService],
})

export class ChildrenComponent extends BaseComponent implements OnInit, OnDestroy {

    private children: ObjectStatisticChildItem[] = [];
    private selected: ObjectStatisticChildItem;
    private sub: any;
    private displayName: string;
    private parentUid: string;

    constructor(
        protected objectStatisticsService: ObjectStatisticsService,
        protected objectDetailService: ObjectDetailService,
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        breadcrumbService: HeaderBreadcrumbService   
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];

            this.load();

            this.buildSecondaryNavigationForObject(this.objectID, this.objectType);
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }


    private load() {
        this.isLoading = true;

        this.objectStatisticsService.getObjectStatistics(this.objectID, this.objectType).subscribe(
            res => {
                this.children = res.Items;
                this.selected = this.children.length > 0 ? this.children[0] : null;

                this.isLoading = false;
            });

        this.objectDetailService.getObject(this.objectID, this.objectType).subscribe(
            res => {
                this.displayName = res.DisplayValue;
                this.objectName = res.DisplayValue;
                this.parentUid = res['Uid']; 
            }
        );

    }
}
