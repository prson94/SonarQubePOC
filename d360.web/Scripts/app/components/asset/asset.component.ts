import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AssetService } from '../../services/asset.service';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-asset',
    template: `<div id="main"><router-outlet> </router-outlet></div>`,
    providers: [AssetService],
})

export class AssetComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    constructor(
        private assetService: AssetService,
        private route: ActivatedRoute,
        private router: Router) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            let assetUid = params['assetUid'];
            let currentUrl = this.router.url;
            if (currentUrl.toLowerCase().indexOf("assettype") == -1) {
                this.assetService.getAssetLegacyUri(assetUid).subscribe(uri => {
                    if (uri !== '') {
                        if (uri.startsWith("reference;")) {
                            this.router.navigateByUrl(uri);
                        }
                        else {
                            this.router.navigate([uri]);
                        }
                    }
                    else {
                        this.router.navigate(['/home']);
                    }
                });
            } else {
                //check for asset types 
                this.assetService.getAssetTypeLegacyUri(assetUid).subscribe(uri => {
                    if (uri !== '') {
                        this.router.navigate([uri]);
                    } else {
                        this.router.navigate(['/home']);
                    }
                });
            }
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
