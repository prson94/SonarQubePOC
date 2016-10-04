import { Component, Input, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ArtifactService, MessagesService } from '../../services/index';

@Component({
    selector: 'd3s-artifact-status',
    template: `
            <div>
                <header>Status</header>
                <span *ngIf="!showRequestCertification">
                    <div class="status-value" [ngClass]="{'status-value-certified':isCertified(), 'status-value-review': isUnderReview()}">{{status}}</div>            
                    <div class="row">
                        <div class="col s12">&nbsp;<a *ngIf="isDraft() && isWorkflowEnabled" (click)="showRequestCertification=true" style="cursor:pointer">Request Certification</a></div>
                    </div>
                </span>
                <span *ngIf="showRequestCertification">
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

export class ArtifactStatusComponent extends BaseComponent implements OnChanges {
    @Input() objectID: number = 0;
    @Input() status: string;
    
    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();
    
    @Input() isWorkflowEnabled: boolean = false;

    private showRequestCertification: boolean = false;

    constructor(protected artifactService: ArtifactService, protected messagesService: MessagesService) {
        super();        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        
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
        this.artifactService.requestCertification(this.objectID)
            .then(result => {
                if (result.type == 'error') {
                    this.messagesService.showError(result.title, result.message);
                }
                this.showRequestCertification = false;
            });        
    }
    
}