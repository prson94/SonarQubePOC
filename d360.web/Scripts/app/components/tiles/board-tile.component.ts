///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/index';
import { Count } from '../../models/counts.model';

@Component({
    selector: 'd3s-board-tile',
    template: `
                <div class="tile tile-detail">
                   <header>Board
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>
                    <div *ngIf="isLoading" style="width:100%; text-align:center;">
                        <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    </div>
                    <p-dataTable *ngIf="!isLoading" [value]="counts" selectionMode="single" [(selection)]="selected" >                    
                        <p-column field="Name" header="Name" [sortable]="true"></p-column>                                                                           
                        <p-column field="Total" header="Total" [sortable]="true" [style]="{'text-align':'center'}"></p-column>
                    </p-dataTable>                      
                </div>
                `,
    providers: [SocialService],
})

export class BoardTile extends BaseComponent implements OnInit {
    private counts: Count[] = [];
    private selected: any;
    private isLoaded: boolean = false;
    private daysToLookBack: number = 7;

    constructor(private socialService: SocialService) {
        super();
    }

    ngOnInit() {
        if (!this.isLoaded) this.load();
    }

    private load() {
        this.isLoading = true;

        this.socialService.getMyCounts(this.daysToLookBack).then(
            res => {
                this.counts = res;
                this.isLoading = false;
            });

        this.isLoaded = true;
    }
}


