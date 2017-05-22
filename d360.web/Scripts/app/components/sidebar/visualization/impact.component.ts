import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-impact-wrapper',
    template: `                
                <d3s-impact [objectID]="objectID" [objectName]="objectName" [objectType]="objectType"></d3s-impact>                
        `
})

export class ImpactComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

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
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}
