import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-itemown-definition',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">  
                        <d3s-resource-responsibility-tile [resourceId]="resourceId" ></d3s-resource-responsibility-tile>
                    </div>
                </div>
            </div>
        `,
    providers: []
})

export class ItemOwnComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    resourceId: number;
   

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService) {
        super();
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.resourceId = +params['resourceID']; // (+) converts string 'id' to a number
        });    

        this.checkSecondaryNavLocalStorage();
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}