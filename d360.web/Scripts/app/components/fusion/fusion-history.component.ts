import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

import { FusionConfigurationDetails } from '../../models/fusion.model';

import { FusionService } from '../../services/fusion.service';

import { BaseComponent } from '../shared/base.component';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';

@Component({
    selector: 'd3s-fusion-history',
    templateUrl: './fusion-history.component.html',
    providers: [FusionService],
})

export class FusionHistoryComponent extends BaseComponent implements OnInit {
    private fusion: FusionConfigurationDetails;

    destroySubject$: Subject<void> = new Subject();

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        protected titleService: Title,
        private fusionService: FusionService,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService 
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.route.params
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                params => {
                    /* parseInt(params['fusionId']) more readable and has a clear understanding.
                     * no need to comment the code. */
                    let fusionId = +params['fusionId']; // (+) converts string 'id' to a number

                    this.isLoading = true;

                    this.fusionService
                        .getFusionConfiguration(fusionId)
                        .pipe(takeUntil(this.destroySubject$))
                        .subscribe(
                            result => {
                                this.fusion = result;

                                this.setBrowserTitle(this.titleService, `History of Fusion - ${this.fusion.Name}`);

                                this.isLoading = false;
                            }
                        )
                        ;

                    this.buildSecondaryNavigationForObject(fusionId, 'Fusion');
                }
            )
            ;
    }
}
