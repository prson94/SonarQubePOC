import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';


@Component({
    selector: 'd3s-actions',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail" style="margin-top: 5px; margin-bottom: 5px;">
                     <d3s-workflow-issue-details *ngIf="showBoard"
                                        [objectType]="objectType"
                                        [objectID]="objectID"
                                        (countsChanged)="updateCounts()"></d3s-workflow-issue-details>
                    </div>
                </div>
            </div>
        `
})

export class ActionsComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() objectType: string = "";
    @Input() objectID: number = 0;
    @Input() objectName: string = "";

    private sub: any;
    hasCloseButton: boolean = false;
    showBoard: boolean = false;

    constructor(private route: ActivatedRoute, private router: Router) { super(); }

    ngOnInit() {

        this.isLoading = true;
        this.showBoard = false;

        this.sub = this.route.params.subscribe(params => {

            this.objectType = params['objectType'];
            this.objectID = +params['objectId'];
            
            this.isLoading = false;
            this.showBoard = true;
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}