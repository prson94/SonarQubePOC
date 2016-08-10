///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ObjectHealthComponent } from '../shared/object-health.component';
import { ObjectBoardComponent } from '../shared/object-board.component';
import { ObjectIssuesComponent } from '../shared/object-issues.component';
import { ObjectChallengeComponent } from '../shared/object-challenge.component';
import { ObjectStatisticsService } from '../../services/index';
import { ObjectStatistics } from '../../models/object-statistics.model';

@Component({
    selector: 'd3s-object-governance-tile',    
    template: `     
                    <div *ngIf="isLoading">
                        <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    </div>       
                    <div class="row" *ngIf="!isLoading">
                        <div class="col l3 s12">                                                        
                            <d3s-object-health [score]="statistics?.Score"></d3s-object-health>                            
                        </div>
                        <div class="col l3 s12">                                                        
                            <d3s-object-issues [issueCount]="statistics?.IssueCount"></d3s-object-issues>
                        </div>
                        <div class="col l3 s12">                                                        
                            <d3s-object-challenge [objectType]="objectType" [objectID]="objectID"></d3s-object-challenge>
                            
                        </div>
                        <div class="col l3 s12">
                            <d3s-object-board [commentCount]="statistics?.CommentCount"></d3s-object-board>                            
                        </div>
                    </div>
                `,
    directives: [ObjectHealthComponent, ObjectBoardComponent, ObjectIssuesComponent, ObjectChallengeComponent],
    providers: [ObjectStatisticsService]
})

export class ObjectGovernanceTile extends BaseComponent implements OnInit {
    @Input() objectType: string;
    @Input() objectID: number;

    statistics: ObjectStatistics;


    constructor(protected objectStatisticsService: ObjectStatisticsService) {
        super();
    }    

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.objectStatisticsService.getObjectStatistics(this.objectID, this.objectType)
            .then(result => {
                this.statistics = result;
                this.isLoading = false;
            });

    }
}
