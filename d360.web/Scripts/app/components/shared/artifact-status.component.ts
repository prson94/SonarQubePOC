import { Component, Input, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-artifact-status',
    template: `
            <div (click)="toggleDetails()" >
                <header>Status</header>
                <div class="status-value" [ngClass]="{'status-value-certified':isCertified(), 'status-value-review': isUnderReview()}">{{status}}</div>            
                <div class="row">
                    &nbsp;<a *ngIf="showRequestCertificationLink" (click)="requestCertification()" style="cursor:pointer">Request Certification</a>
                </div>
            </div>
        `
})

export class ArtifactStatusComponent extends BaseComponent implements OnChanges {
    @Input() objectID: number = 0;
    @Input() status: string;
    
    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();


    private showRequestCertificationLink: boolean = false;
    
    constructor() {
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
        
    
    private requestCertification() {

    }
    
}