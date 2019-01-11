import { Input, Output, Component, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../base.component';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';
import { ObjectStatistics } from '../../../models/object-statistics.model';
import { ArtifactService } from '../../../services/artifacts.service';
import { HeaderActions } from '../../../models/header.model';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { OnDestroy, OnInit } from '@angular/core/src/metadata/lifecycle_hooks';

@Component({
    selector: 'd3s-object-governance',    
    template: `     
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div class="row" style="display:flex" *ngIf="!isLoading" [ngClass]="{'activeTab':hasActiveTab()}">
                        <div class="col s12" [ngClass]="{'inactive': (hasActiveTab() && !showHealthDetails), 'active-left':showHealthDetails, 'l3':showStatus, 'l4':!showStatus}">                                                        
                            <d3s-object-health [score]="statistics?.Score" [uid]="uid" [objectType]="objectType" [objectID]="objectID" [showDetails]="showHealthDetails" (showDetailsChange)="showHealthDetails=$event;showIssueDetails=false;showBoardDetails=false;"></d3s-object-health>                            
                        </div>
                        <div class="col s12" [ngClass]="{'inactive': (hasActiveTab() && !showIssueDetails), 'active':showIssueDetails, 'l3':showStatus, 'l4':!showStatus}">                                                        
                            <d3s-object-issues [issueCount]="statistics?.IssueCount" [lastIssueDate]="statistics?.IssueLast" [showDetails]="showIssueDetails" (showDetailsChange)="showIssueDetails=$event;showHealthDetails=false;showBoardDetails=false;"></d3s-object-issues>
                        </div>                      
                        <div class="col s12" [ngClass]="{'inactive': (hasActiveTab() && !showBoardDetails), 'active-right':showBoardDetails && !showStatus, 'active':showBoardDetails && showStatus, 'l3':showStatus, 'l4':!showStatus}">
                            <d3s-object-board [commentCount]="statistics?.CommentCount" [lastCommentDate]="statistics?.CommentLast" [showDetails]="showBoardDetails" (showDetailsChange)="showBoardDetails=$event;showIssueDetails=false;showHealthDetails=false;"></d3s-object-board>                            
                        </div>
                        <div class="col s12" *ngIf="showStatus"  [ngClass]="{'inactive': (hasActiveTab() && !showStatusDetails), 'active-right':showStatusDetails, 'l3':showStatus}">
                            <d3s-artifact-status (statusChanged)="updateStatus()" [objectID]="objectID" [status]="status" [isWorkflowEnabled]="isWorkflowEnabled"></d3s-artifact-status>
                        </div>
                    </div>
                    <div style="padding:20px;" *ngIf="showHealthDetails || showIssueDetails || showBoardDetails">
                        <d3s-object-health-details *ngIf="showHealthDetails" [uid]="uid" [objectName]="objectName"></d3s-object-health-details>
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
    providers: [ArtifactService, ObjectStatisticsService]
})

export class ObjectGovernanceComponent extends BaseComponent implements OnInit, OnChanges, OnDestroy {
    @Input() uid: string;
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
        
    constructor(protected objectStatisticsService: ObjectStatisticsService,
        private headerActionsService: HeaderActionsService,
        protected artifactService: ArtifactService
    ) {
        super();
    }    
    
    ngOnInit(): void {
        if (this.objectType && this.objectID) 
            this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectType && this.objectID && this.uid)
            this.load();
    }

    ngOnDestroy(): void {
        this.showHideFollow(true);
    }

    load() {
        this.isLoading = true;
        this.objectStatisticsService.getObjectStatus(this.objectID, this.objectType)
            .then(result => {
                this.status = result;
                if (this.status != undefined && this.status != null && this.status.length > 0) {
                    this.showStatus = true;
                }
            })
        this.objectStatisticsService.getObjectStatistics(this.objectID, this.objectType)
            .then(result => {
                this.statistics = result;
                this.isLoading = false;
            });

    }

    private showHideFollow(show: boolean) {
        let headerActions: HeaderActions = new HeaderActions();
        headerActions.showFollow = show;
        this.headerActionsService.setCurrentHeaderActions(headerActions);
    }

    private hasActiveTab() {
        this.showHideFollow(!this.showIssueDetails);
        return this.showBoardDetails || this.showHealthDetails || this.showIssueDetails;
    }

    private updateStatus() {
        if (this.objectType.toUpperCase() == "ARTIFACT") {
            window.setTimeout(x => {
                this.objectStatisticsService.getObjectStatus(this.objectID, this.objectType)
                    .then(result => {
                        this.status = result;
                        if (this.status != undefined && this.status != null && this.status.length > 0) {
                            this.showStatus = true;
                        }
                    });
            }, 3000);
        }
    }

    private updateCounts() {
        this.load();
    }
}
