import { Component, Input, Output, EventEmitter } from '@angular/core';

import { BaseComponent } from '../base.component';
import { ArtifactService } from '../../../services/artifacts.service';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-artifact-status',
    templateUrl: './artifact-status.component.html',
    providers: [ArtifactService]
})

export class ArtifactStatusComponent extends BaseComponent  {
    @Input() objectID: number = 0;
    @Input() status: string;
    
    @Input() showDetails: boolean = false;
    @Output() statusChanged = new EventEmitter();
    
    @Input() isWorkflowEnabled: boolean = false;

    private showRequestCertification: boolean = false;

    constructor(
        protected artifactService: ArtifactService,
        protected messagesService: MessagesService
    ) {
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

        this
            .artifactService
            .requestCertification(this.objectID)
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result);                
                    this.isLoading = false;
                    this.statusChanged.emit();
                    this.showRequestCertification = false;
                }
            )
        ;
    }   
}
