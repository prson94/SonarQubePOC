import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-resource-groups-definition',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile">  
                        <d3s-resource-groups [resourceId]="resourceId" ></d3s-resource-groups>
                    </div>
                </div>
            </div>
        `,
    providers: []
})

export class MemberGroupComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    resourceId: number;
    
    constructor(
        private route: ActivatedRoute,
        private router: Router) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.resourceId = +params['resourceID']; // (+) converts string 'id' to a number
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }    
}