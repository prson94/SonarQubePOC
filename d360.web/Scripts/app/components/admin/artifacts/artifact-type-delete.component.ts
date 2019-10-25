import { Input, Output, Component, EventEmitter, OnInit } from '@angular/core';
import { ArtifactTypeService } from '../../../services/artifact-type.service';
import { ArtifactService } from '../../../services/artifacts.service';
import { ArtifactType } from '../../../models/artifact-type.model';
import { SortOrder } from '../../../models/enums.model';
import { BaseComponent } from '../../shared/base.component';
import { forkJoin } from 'rxjs';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'd3s-artifact-type-delete',
    templateUrl: './artifact-type-delete.component.html',
    providers: [ArtifactTypeService, ArtifactService]
})

export class ArtifactTypeDeleteComponent extends BaseComponent implements OnInit {
    @Input() callback: Function;
    @Input() artifactTypeId: number;
    @Input() assetTypeId: number;
    @Output() onCancel = new EventEmitter();

    public artifactType: ArtifactType;
    assetTypeClass: AssetTypeClass;
    private count: number = 0;
    private signoff: boolean = false;
    private className: string;
    private sub: any;

    constructor(
        private artifactTypeService:ArtifactTypeService,
        private artifactService: ArtifactService,
        private messagesService: MessagesObservableService,
        private route: ActivatedRoute,
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            try {
                let assetTypeClassString: keyof typeof AssetTypeClass = params['class'];
                this.assetTypeClass = AssetTypeClass[assetTypeClassString];
                if (!this.assetTypeClass) {
                    this.assetTypeClass = AssetTypeClass.BusinessAsset;
                }
            } catch (e) {
                this.assetTypeClass = AssetTypeClass.BusinessAsset;
            }

            let name: string = this.assetTypeClass == AssetTypeClass.BusinessAsset ? 'Business Asset' : 'Technical Asset';
            this.className = name;
        });
        this.load();
    }

    private load() {
        forkJoin(
            this.artifactTypeService.getArtifactTypeDetails(this.artifactTypeId),
            this.artifactService.getArtifacts(this.assetTypeId, 10, 1, '', SortOrder.Ascending)
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
