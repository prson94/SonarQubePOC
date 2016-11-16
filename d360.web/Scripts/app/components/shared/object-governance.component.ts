/// <reference path="../header/index.ts" />
import { Input, Output, Component, OnInit, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ObjectStatisticsService } from '../../services/index';
import { ObjectStatistics } from '../../models/object-statistics.model';

@Component({
    selector: 'd3s-object-governance',    
    template: `     
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div class="row" style="display:flex" *ngIf="!isLoading" [ngClass]="{'activeTab':hasActiveTab()}">
                        <div class="col s12" [ngClass]="{'inactive': (hasActiveTab() && !showHealthDetails), 'active-left':showHealthDetails, 'l3':showStatus, 'l4':!showStatus}">                                                        
                            <d3s-object-health [score]="statistics?.Score" [objectType]="objectType" [objectID]="objectID" [showDetails]="showHealthDetails" (showDetailsChange)="showHealthDetails=$event;showIssueDetails=false;showBoardDetails=false;"></d3s-object-health>                            
                        </div>
                        <div class="col s12" [ngClass]="{'inactive': (hasActiveTab() && !showIssueDetails), 'active':showIssueDetails, 'l3':showStatus, 'l4':!showStatus}">                                                        
                            <d3s-object-issues [issueCount]="statistics?.IssueCount" [lastIssueDate]="statistics?.IssueLast" [showDetails]="showIssueDetails" (showDetailsChange)="showIssueDetails=$event;showHealthDetails=false;showBoardDetails=false;"></d3s-object-issues>
                        </div>                      
                        <div class="col s12" [ngClass]="{'inactive': (hasActiveTab() && !showBoardDetails), 'active-right':showBoardDetails && !showStatus, 'active':showBoardDetails && showStatus, 'l3':showStatus, 'l4':!showStatus}">
                            <d3s-object-board [commentCount]="statistics?.CommentCount" [lastCommentDate]="statistics?.CommentLast" [showDetails]="showBoardDetails" (showDetailsChange)="showBoardDetails=$event;showIssueDetails=false;showHealthDetails=false;"></d3s-object-board>                            
                        </div>
                        <div class="col s12" *ngIf="showStatus"  [ngClass]="{'inactive': (hasActiveTab() && !showStatusDetails), 'active-right':showStatusDetails, 'l3':showStatus}">
                            <d3s-artifact-status [objectID]="objectID" [status]="status" [isWorkflowEnabled]="isWorkflowEnabled"></d3s-artifact-status>
                        </div>
                    </div>
                    <div style="padding:20px;" *ngIf="showHealthDetails || showIssueDetails || showBoardDetails">
                        <d3s-object-health-details *ngIf="showHealthDetails" [objectType]="objectType" [objectID]="objectID" [objectName]="objectName"></d3s-object-health-details>                    
                        <d3s-workflow-issue-details *ngIf="showIssueDetails" [objectType]="objectType" [objectID]="objectID" [objectName]="objectName" (countsChanged)="updateCounts()"></d3s-workflow-issue-details>
                        <d3s-social-board *ngIf="showBoardDetails" [objectType]="objectType" [objectID]="objectID" [objectName]="objectName" (countsChanged)="updateCounts()"></d3s-social-board>
                    </div>
                `,
    styles: [`
                div.active, div.active-left, div.active-right{                    
                    border-top: 1px solid #cbcaca;                    
                    background:white;
                }
                div.active{
                    border-left: 1px solid #cbcaca;
                    border-right: 1px solid #cbcaca;                    
                    border-top-left-radius: 5px;
                    border-top-right-radius: 5px;                    
                }
                div.active-left{                    
                    border-right: 1px solid #cbcaca;                                        
                    border-top-right-radius: 5px;                    
                }
                div.active-right{                    
                    border-left: 1px solid #cbcaca;                                        
                    border-top-left-radius: 5px;                    
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

export class ObjectGovernanceComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectName: string;
    @Input() status: string;
    @Input() isWorkflowEnabled: boolean;

    statistics: ObjectStatistics;

    showHealthDetails: boolean = false;
    showIssueDetails: boolean = false;
    showBoardDetails: boolean = false;
    showStatusDetails: boolean = false;
    showStatus: boolean = false;
        
    constructor(protected objectStatisticsService: ObjectStatisticsService) {
        super();
    }    

    ngOnInit() {
    
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectType && this.objectID)
            this.load();
    }

    load() {
        this.isLoading = true;
        switch (this.objectType.toUpperCase())
        {
            case "ARTIFACT":
            case "RULE":
            case "POLICY":
                this.showStatus = true;
                break;
        }
        this.objectStatisticsService.getObjectStatistics(this.objectID, this.objectType)
            .then(result => {
                this.statistics = result;
                this.isLoading = false;
            });

    }

    private hasActiveTab() {
        return this.showBoardDetails || this.showHealthDetails || this.showIssueDetails;
    }

    updateCounts() {
        this.load();
    }
}
