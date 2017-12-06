import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { ObjectDetailService } from '../../../services/object-detail.service';

@Component({
    selector: 'd3s-membergroup-definition',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">  
                        <d3s-field-definition-tile [resouceID]="resouceID" ></d3s-field-definition-tile>
                    </div>
                </div>
            </div>
        `,
    providers: []
})

export class MemberGroupComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    resourceID: number;


    constructor(
        private route: ActivatedRoute,
        private router: Router) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.resourceID = +params['resourceID']; // (+) converts string 'id' to a number
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    load() {

    }
}