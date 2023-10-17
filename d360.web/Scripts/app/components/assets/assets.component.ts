import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetTypeService } from '../../services/asset-type.service';

@Component({
    selector: 'd3s-assets',
    template: `<div id="main"><div class="d3s"><router-outlet> </router-outlet></div></div>`,
    providers: [AssetTypeService]
})

export class AssetsComponent implements OnInit, OnDestroy {
    private sub: any;

    constructor(
        private assetTypeService: AssetTypeService,
        private route: ActivatedRoute,
        private router: Router) {
        
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe((params) => {
            const assetTypeUid = params['assetTypeUid'];

            this.assetTypeService.getAssetTypeLegacyUri(assetTypeUid).subscribe((uri) => {
                const behavior = { replaceUrl: true };
                if (uri !== '') {
                    this.router.navigate([uri], behavior);
                }
                else {
                    this.router.navigate(['/home'], behavior);
                }
            });
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
