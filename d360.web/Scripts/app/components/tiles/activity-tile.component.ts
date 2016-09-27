
import { Component, OnInit, Output, Input, EventEmitter} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ArtifactService } from '../../services/index';
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
                    <p-dataTable *ngIf="!isLoading && counts.length > 0" [value]="counts" selectionMode="single" [(selection)]="selected" (onRowDblclick)="selected=$event.data;doSelect()" [rows]="10" [paginator]="true" [pageLinks]="3">                    
                        <p-column field="Name" header="Name" [sortable]="true"></p-column>                                                                           
                        <p-column field="New" header="Total" [sortable]="true" [style]="{'text-align':'center'}"></p-column>                          
                    </p-dataTable>                      
                    <div *ngIf="counts.length == 0 && !isLoading">
                        No recent activity
                    </div>
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

    private doSelect() {
        this.showItemDetail.emit({
            Id: this.selected.Id,
            name: this.selected.Name
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


