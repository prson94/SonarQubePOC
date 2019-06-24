import { Input, Output, Component, EventEmitter, OnInit } from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { ArtifactTypeService } from '../../../services/artifact-type.service';
import { ArtifactService } from '../../../services/artifacts.service';
import { ArtifactType } from '../../../models/artifact-type.model';
import { SortOrder } from '../../../models/enums.model';
import { BaseComponent } from '../../shared/base.component';
import { forkJoin } from 'rxjs';

@Component({
    selector: 'd3s-artifact-type-delete',
    templateUrl: './artifact-type-delete.component.html',
    providers: [ArtifactTypeService, ArtifactService]
})

export class ArtifactTypeDeleteComponent extends BaseComponent implements OnInit {
    @Input() callback: Function;
    @Input() artifactTypeId: number;
    @Output() onCancel = new EventEmitter();

    public artifactType: ArtifactType;
    private count: number = 0;
    private signoff: boolean = false;

    constructor(
        private artifactTypeService:ArtifactTypeService,
        private artifactService: ArtifactService,
        private messagesService: MessagesService,
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        forkJoin(
            this.artifactTypeService.getArtifactTypeDetails(this.artifactTypeId),
            this.artifactService.getArtifacts(this.artifactTypeId, 10, 1, '', SortOrder.Ascending)
        )
        .subscribe(
            (
                [
                    getArtifactTypeDetailsResponse,
                    getArtifactsResponse
                ]
            ) => {
                this.artifactType = getArtifactTypeDetailsResponse;
                this.count = getArtifactsResponse.total;
            },
            err => {
                this.isLoading = false;
                this.messagesService.showError("Error", err.message);
            }
        );
    }

    private delete(): void {
        if (this.isLoading) {
            return;
        }

        this.isLoading = true;
        
        if (this.callback) {
            this.callback(this.artifactTypeId);
        }

        this.isLoading = false;
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }
}
