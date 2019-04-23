import {Component, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionService} from '../../services/fusion.service';

import {SiteUrlHelpers} from '../../static/site-url-helpers';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-attribute-item',
    template: '',
    providers: [FusionService],
})

export class FusionAttributeItemComponent extends BaseComponent implements OnInit {
    destroySubject$: Subject<void> = new Subject();

    constructor(
        private fusionService: FusionService,
        private route: ActivatedRoute,
        private router: Router) {
        super();
    }

    ngOnInit() {
        this.route.params
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                params => {
                    var fusionAttributeTypeId = +params['fusionAttributeTypeId'];
                    var fusionAttributeId = +params['fusionAttributeId'];

                    this.fusionService
                        .getFusionConfigurationFromAttributeId(fusionAttributeId)
                        .pipe(takeUntil(this.destroySubject$))
                        .subscribe(
                            res => {
                                this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${res.ID};fusionAttributeTypeId=${fusionAttributeTypeId};fusionAttributeId=${fusionAttributeId}`);
                            }
                        )
                    ;
                }
            )
        ;
    }
}
