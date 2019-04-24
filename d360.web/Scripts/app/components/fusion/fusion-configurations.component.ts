import {Component, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {takeUntil} from "rxjs/operators";

import {Fusion} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {SiteUrlHelpers} from '../../static/site-url-helpers';

import {BaseComponent} from '../shared/base.component';
import {Subject} from "rxjs";

@Component({
    selector: 'd3s-fusion-configuration',
    templateUrl: './fusion-configurations.component.html',
    providers: [FusionService],
})

export class FusionConfigurationComponent extends BaseComponent implements OnInit {
    private fusions: Fusion[] = [];
    private selected: Fusion;

    destroySubject$: Subject<void> = new Subject();

    constructor(
        private fusionService: FusionService,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;

        this.fusionService
            .getFusionConfigurations()
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                res => {
                    this.fusions = res;
                    this.selected = this.fusions.length > 0 ? this.fusions[0] : null;

                    this.isLoading = false;
                }
            );
    }

    private showFusion(fusion) {
        if (!fusion) {
            console.log("ERROR NO SELECTED FUSION ITEM TO NAVIGATE TO.");

            return;
        }
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('FusionType', fusion.ID));
    }

    private doExport() {
        this.fusionService.exportFusionConfigurations();
    }
}
