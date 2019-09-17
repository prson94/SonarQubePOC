import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-actions',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail" style="margin-top: 5px; margin-bottom: 5px;">
                     <d3s-workflow-issue-details
                                        [objectType]="objectType"
                                        [objectID]="objectID"></d3s-workflow-issue-details>
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
    
    constructor(private route: ActivatedRoute) { super(); }

    ngOnInit() {
        this.isLoading = true;
        
        this.sub = this.route.params.subscribe(params => {

            this.objectType = params['objectType'];
            this.objectID = +params['objectId'];
            
            this.isLoading = false;            
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}