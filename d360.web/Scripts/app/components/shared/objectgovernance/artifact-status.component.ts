import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../base.component';
import { ArtifactService } from '../../../services/artifacts.service';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-artifact-status',
    template: `
            <div>   
                <d3s-loading [isLoading]="isLoading"></d3s-loading>             
                <span *ngIf="!showRequestCertification && !isLoading">
                    <div class="status-value" [ngClass]="{'status-value-certified':isCertified(), 'status-value-review': isUnderReview()}">{{status}}</div>            
                    <div *ngIf="isDraft() && isWorkflowEnabled">
                        <a (click)="showRequestCertification=true" style="cursor:pointer">Request Certification</a>
                    </div>
                    <div *ngIf="!isDraft() || !isWorkflowEnabled" class="status-note">
                        Status
                    </div>
                </span>
                <span *ngIf="showRequestCertification  && !isLoading">
                    <div class="form-instructions">Click request certification to send a certification request to the term owner.</div>
                    <div class="row">
                        <div class="col s12">
                            <button pButton type="button" (click)="requestCertification()" label="Request Certification"></button>                            
                            <button pButton type="button" (click)="showRequestCertification=false;" label="Cancel"></button>
                        </div>       
                    </div>             
                </span>
            </div>
        `,
    providers: [ArtifactService]
})

export class ArtifactStatusComponent extends BaseComponent  {
    @Input() objectID: number = 0;
    @Input() status: string;
    
    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();
    
    @Input() isWorkflowEnabled: boolean = false;

    private showRequestCertification: boolean = false;

    constructor(protected artifactService: ArtifactService, protected messagesService: MessagesService) {
        super();        
    }
    
    private isCertified(): boolean {
        return this.status && this.status.toUpperCase() == "CERTIFIED";
    }

    private isUnderReview(): boolean {
        return this.status && this.status.toUpperCase() == "UNDER REVIEW";
    }

    private isDraft(): boolean {
        return this.status && this.status.toUpperCase() == "DRAFT";
    }
        
    
    private requestCertification() {
        this.isLoading = true;
        this.artifactService.requestCertification(this.objectID)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);                
                this.isLoading = false;
                this.showRequestCertification = false;
            });        
    }
    
}