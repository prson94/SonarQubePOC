import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-diagram-wrapper',
    template: `           
                <d3s-model-diagram *ngIf="baseAssetTypeUid" [assetTypeUid]="baseAssetTypeUid"></d3s-model-diagram>                
        `
})

export class DiagramComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    constructor(
        protected settingsService: CompanySettingsService,
        private route: ActivatedRoute
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe((params) => {
			this.baseAssetTypeUid = params['assetTypeUid']; // (+) converts string 'id' to a number       
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
