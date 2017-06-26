import { Component, OnInit, Output, EventEmitter, Input} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/social.service';
import { Count } from '../../models/counts.model';

@Component({
    selector: 'd3s-board-tile',
    template: `
                <div class="tile tile-detail">
                   <header>Board<span style="color:#999;font-size:60%;vertical-align:middle;">{{timeFrameMessage()}}</span>
                    <d3s-tile-actions [hasAdd]="false" [hasDate]="true" (dateClick)="changeDates($event);"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <p-dataTable *ngIf="!isLoading && counts.length > 0"  sortField="Name" [sortOrder]="1" [value]="counts" selectionMode="single" [(selection)]="selected" (onRowDblclick)="selected=$event.data;doSelect(selected)">                    
                        <p-column field="Name" header="Name" [sortable]="true">
                            <ng-template let-item="rowData" pTemplate type="body">
                                    <a (click)="doSelect(item)">{{item.Name}}</a>
                            </ng-template>
                        </p-column>                                                                           
                        <p-column field="Total" header="Total" [sortable]="true" [style]="{'text-align':'center'}"></p-column>
                    </p-dataTable>   
                    <div *ngIf="counts.length == 0 && !isLoading" style="padding:10px">No board activity for this timeframe</div>
                </div>
                `,
    providers: [SocialService],
})

export class BoardTile extends BaseComponent implements OnInit {
    private counts: Count[] = [];
    private selected: any;    
    @Input() daysToLookBack: number = 7;
    @Output() daysToLookBackChange = new EventEmitter();

    @Output() showItemDetail = new EventEmitter();

    constructor(private socialService: SocialService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;

        this.socialService.getMyCounts(this.daysToLookBack).then(
            res => {
                this.counts = res.filter(item => item.Total > 0);
                this.isLoading = false;                
            });
    }

    private doSelect(item: Count) {        
        this.showItemDetail.emit({
            selected: item
        });
    }

    private changeDates(event) {
        this.daysToLookBack = event.days;
        this.daysToLookBackChange.emit( this.daysToLookBack );
        this.load();
    }

    private timeFrameMessage() {
        switch (this.daysToLookBack) {
            case 7:
                return ' (Past week)';
            case 30:
                return ' (Past month)';
            case 365:
                return ' (Past year)';
        }
        return ' (All)'
    }
}


