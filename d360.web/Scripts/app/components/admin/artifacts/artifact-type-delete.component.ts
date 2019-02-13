import { CommonModule } from '@angular/common';
import { NgModule, Input, Output, Component, EventEmitter, OnInit } from '@angular/core';

import { ArtifactTypeService } from '../../../services/artifact-type.service';
import { ArtifactService } from '../../../services/artifacts.service';
import { ArtifactType } from '../../../models/artifact-type.model';
import { SortOrder } from '../../../models/enums.model';
import { BaseComponent } from '../../shared/base.component';

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
        private artifactService:ArtifactService            
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this
            .artifactTypeService
            .getArtifactTypeDetails(this.artifactTypeId)
            .subscribe(
                result=>{
                    this.artifactType = result;            
                }
            )
        ;

        this
            .artifactService
            .getArtifacts(this.artifactTypeId, 10, 1, '', SortOrder.Ascending)
            .subscribe(
                result => {
                    this.count = result.total;
                }
            )
        ;
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
