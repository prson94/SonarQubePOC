import { Component, OnInit, Output, Input, EventEmitter} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ArtifactService } from '../../services/artifacts.service';
import { Count} from '../../models/counts.model';

@Component({
    selector: 'd3s-activity-tile',
    providers: [ArtifactService],
    template: `
                <div class="tile tile-detail">
                   <header>Activity <span style="color:#999;font-size:60%;vertical-align:middle;">{{timeFrameMessage()}}</span>
                    <d3s-tile-actions [hasAdd]="false" [hasDate]="true" (dateClick)="changeDates($event);"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <p-table #dt *ngIf="!isLoading && counts.length > 0" [value]="counts" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','New','Total']" sortField="Name" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'New'" style="text-align:center">
                                    New
                                    <d3s-sortIcon [field]="'New'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Total'" style="text-align:center">
                                    Modified
                                    <d3s-sortIcon [field]="'Total'"></d3s-sortIcon>
                                </th>
                            </tr>
                           
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr  (dblclick)="selected=item;doSelect(selected)" [pSelectableRow]="item">
                                <td>
                                     <a (click)="doSelect(item)">{{item.Name}}</a>
                                </td>
                                <td style="text-align:center">{{item.New}}</td>
                                <td style="text-align:center">{{item.Total}}</td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                     
                    <div *ngIf="counts.length == 0 && !isLoading" style="padding:10px">No activity for this timeframe</div>                    
                </div>
                `
})

export class ActivityTile extends BaseComponent implements OnInit {
    private counts: Count[] = [];
    private selected: Count;    
    private isLoaded: boolean = false;

    @Input() daysToLookBack: number = 7;
    @Output() daysToLookBackChange = new EventEmitter();

    @Output() showItemDetail = new EventEmitter();

    constructor(private artifactService: ArtifactService) {
        super();
    }

    ngOnInit() {
        if (!this.isLoaded) this.load();
    }

    private load() {
        this.isLoading = true;
        this.artifactService.getActivityCount(this.daysToLookBack)
            .then(res => {
                this.counts = res;
                this.isLoading = false;
                this.isLoaded = true;
            });
    }

    private doSelect(item) {
        this.showItemDetail.emit({
            Id: item.Id,
            name: item.Name
        });
    }

    private changeDates(event) {
        this.daysToLookBack = event.days;
        this.daysToLookBackChange.emit(this.daysToLookBack);
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
        return ' (All Activity)'
    }
}


