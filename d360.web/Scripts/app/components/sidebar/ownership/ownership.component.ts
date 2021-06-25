import {Component, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {Subject} from "rxjs";
import {takeUntil} from "rxjs/operators";

import {ObjectDetailService} from '../../../services/object-detail.service';

import {BaseComponent} from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';

@Component({
    selector: 'd3s-ownership',
    template: `
        <div class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-people-responsibilities-tile [assetID]="assetID" [assetUid]="uid"
                                                      [title]="'Responsibilities of ' + [objectName]"></d3s-people-responsibilities-tile>
                </div>
            </div>
        </div>
    `,
    providers: [
        ObjectDetailService,
    ]
})

export class OwnershipComponent extends BaseComponent implements OnInit {
    destroySubject$: Subject<void> = new Subject();

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private objectDetailService: ObjectDetailService,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.route.params.subscribe(
            params => {    
                this.assetID = +params['assetID'];
                this.objectDetailService.getAsset(this.assetID).subscribe(
                    res => {
                        this.objectName = res.DisplayValue;
                        this.uid = res.uid;
                        let reloadNav = params['isAdminPage'] && params['isAdminPage'] == 'false' ? false : true;

                        if (reloadNav)
                            this.buildSecondaryNavigation(null, +res["ObjectID"], res["Object"]);
                    }
                );
            }
        );
    }
}
