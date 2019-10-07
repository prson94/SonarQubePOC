import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-browser',
    template: `
        <ng-container>
            <d3s-assetbrowser [assetUid]="uid" [readonly]="true"></d3s-assetbrowser>
        </ng-container>
        `
})
     
export class BrowserComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    constructor(
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.uid = params['assetUid'];
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}
