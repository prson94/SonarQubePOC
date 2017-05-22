import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-lineage-wrapper',
    template: ` <d3s-lineage [objectID]="objectID" [objectName]="objectName" [objectType]="objectType" [usageOnly]="usageOnly"></d3s-lineage>
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
