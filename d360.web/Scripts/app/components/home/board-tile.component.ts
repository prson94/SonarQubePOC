import { Component, OnInit, Output, EventEmitter, Input} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/social.service';
import { Count } from '../../models/counts.model';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-board-tile',
    template: `
                <div class="tile tile-detail">
                   <header>Board<span style="color:#999;font-size:60%;vertical-align:middle;">{{timeFrameMessage()}}</span>
                    <d3s-tile-actions [hasAdd]="false" [hasDate]="true" (dateClick)="changeDates($event);"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <p-table #dt *ngIf="!isLoading && counts.length > 0" [value]="counts" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','Total']" sortField="Name" [sortOrder]="1" [rows]="10" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Total'" style="text-align:center">
                                    Total
                                    <d3s-sortIcon [field]="'Total'"></d3s-sortIcon>
                                </th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;doSelect(selected)" [pSelectableRow]="item">
                                <td>
                                        <a (click)="doSelect(item)">{{item.Name}}</a>
                                </td>
                                <td>{{item.Total}}</td>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
  
                    <div *ngIf="counts.length == 0 && !isLoading" style="padding:10px">No board activity for this timeframe</div>
                </div>
                `,
    providers: [SocialService],
})

export class BoardTile extends BaseComponent implements OnInit {
    counts: Count[] = [];
    selected: any;    
    @Input() daysToLookBack: number = 7;
    @Output() daysToLookBackChange = new EventEmitter();

    @Output() showItemDetail = new EventEmitter();

    constructor(
        protected settingsService: CompanySettingsService,
        private socialService: SocialService) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;

        this.socialService.getMyCounts(this.daysToLookBack).subscribe(
            res => {
                this.counts = res.filter(item => item.Total > 0);
                this.isLoading = false;                
            });
    }

    doSelect(item: Count) {        
        this.showItemDetail.emit({
            selected: item
        });
    }

    changeDates(event) {
        this.daysToLookBack = event.days;
        this.daysToLookBackChange.emit( this.daysToLookBack );
        this.load();
    }

    timeFrameMessage() {
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


