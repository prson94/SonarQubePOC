import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-lineage-wrapper',
    template: ` <d3s-lineage-diagram [objectID]="objectID" [objectType]="objectType" [readonly]="true"></d3s-lineage-diagram>
        `
})

export class LineageComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private usageOnly: boolean = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];
            this.usageOnly = params['showUsageOnly'] == '1';
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}
