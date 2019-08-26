import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-comments',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail" style="margin-top: 5px; margin-bottom: 5px;">
                        <d3s-social-board *ngIf="showBoard" [objectType]="objectType" [objectID]="objectId" [daysToLookBack]="daysToLookBack"></d3s-social-board>
                    </div>
                </div>
            </div>
        `
})

export class CommentsComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() objectId: number = 0;
    @Input() objectType: string="";

    private sub: any;
    daysToLookBack: number = 365;
    hasCloseButton: boolean = false;
    showBoard: boolean = false;

    constructor(private route: ActivatedRoute, private router: Router) { super(); }

    ngOnInit() {

        this.isLoading = true;
        this.showBoard = false;

        this.sub = this.route.params.subscribe(params => {
            this.objectId = +params['objectId'];
            this.objectType = params['objectType'];
            this.isLoading = false;
            this.showBoard = true;
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}