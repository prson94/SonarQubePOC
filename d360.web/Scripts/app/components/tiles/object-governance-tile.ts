///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ObjectStatisticsService } from '../../services/index';
import { ObjectStatistics } from '../../models/object-statistics.model';

@Component({
    selector: 'd3s-object-governance-tile',    
    template: `     
                    <div *ngIf="isLoading">
                        <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    </div>       
                    <div class="row" *ngIf="!isLoading" [ngClass]="{'activeTab':hasActiveTab()}">
                        <div class="col l4 s12" [ngClass]="{'inactive': (hasActiveTab() && !showHealthDetails), 'active':showHealthDetails}">                                                        
                            <d3s-object-health [score]="statistics?.Score" [showDetails]="showHealthDetails" (showDetailsChange)="showHealthDetails=$event;showIssueDetails=false;showBoardDetails=false;"></d3s-object-health>                            
                        </div>
                        <div class="col l4 s12" [ngClass]="{'inactive': (hasActiveTab() && !showIssueDetails), 'active':showIssueDetails}">                                                        
                            <d3s-object-issues [issueCount]="statistics?.IssueCount" [showDetails]="showIssueDetails" (showDetailsChange)="showIssueDetails=$event;showHealthDetails=false;showBoardDetails=false;"></d3s-object-issues>
                        </div>                      
                        <div class="col l4 s12"  [ngClass]="{'inactive': (hasActiveTab() && !showBoardDetails), 'active':showBoardDetails}">
                            <d3s-object-board [commentCount]="statistics?.CommentCount"  [showDetails]="showBoardDetails" (showDetailsChange)="showBoardDetails=$event;showIssueDetails=false;showHealthDetails=false;"></d3s-object-board>                            
                        </div>
                    </div>
                    <d3s-object-health-details *ngIf="showHealthDetails" [objectType]="objectType" [objectID]="objectID" [objectName]="objectName"></d3s-object-health-details>                    
                    <d3s-object-issue-details *ngIf="showIssueDetails" [objectType]="objectType" [objectID]="objectID" [objectName]="objectName"></d3s-object-issue-details>
                    <d3s-object-board-details *ngIf="showBoardDetails" [objectType]="objectType" [objectID]="objectID" [objectName]="objectName"></d3s-object-board-details>
                `,
    styles: [`
                div.active{
                    border-left: 1px solid #cbcaca;
                    border-right: 1px solid #cbcaca;
                    border-top: 1px solid #cbcaca;
                    border-top-left-radius: 5px;
                    border-top-right-radius: 5px;
                    background:white;
                }
                div.inactive{
                    border-bottom: 1px solid #cbcaca;                                        
                }
                div.activeTab{
                    background: #f0f3f8;
                }
            `],
    providers: [ObjectStatisticsService]
})

export class ObjectGovernanceTile extends BaseComponent implements OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectName: string;

    statistics: ObjectStatistics;

    showHealthDetails: boolean = false;
    showIssueDetails: boolean = false;
    showBoardDetails: boolean = false;

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

    private hasActiveTab() {
        return this.showBoardDetails || this.showHealthDetails || this.showIssueDetails;
    }
}
