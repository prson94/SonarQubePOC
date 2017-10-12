import { CommonModule } from '@angular/common';
import { NgModule, Input, Output, Component, EventEmitter, OnInit } from '@angular/core';
import { ArtifactTypeService } from '../../../services/artifact-type.service';
import { ArtifactService } from '../../../services/artifacts.service';
import { ArtifactType } from '../../../models/artifact-type.model';
import { SortOrder } from '../../../models/enums.model';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-artifact-type-delete',
    template: `
                    <header>Confirm Deletion of {{artifactType?.Name}} Artifact Type</header>                
                    <div class="row">
                        <div *ngIf="isLoading;else notLoading" style="text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        <ng-template #notLoading>
                            <div class="col s12">Are you sure you want to delete the artifact type {{artifactType?.Name}}.</div>
                            <div class="col s12" *ngIf="count > 0">&nbsp;</div>
                            <div class="col s12" *ngIf="count > 0"><p-checkbox [(ngModel)]="signoff" binary="true"></p-checkbox><i class="fa fa-exclamation-triangle" aria-hidden="true"></i> The selected Artifact Type contains <b>{{count}}</b> artifacts that will also be deleted.  These actions cannot be undone.  Please check this box if you would like to continue.</div>
                        </ng-template>
                    </div>    
                    <div class="row"><div class="col s12">&nbsp;</div></div>
                    <div class="row">                        
                        <button pButton (click)="delete()" label="Delete" [disabled]="isLoading || (count > 0 && !signoff)"></button>
                        <button pButton (click)="cancel()" label="Cancel"></button>
                    </div>                
                `,
    providers: [ArtifactTypeService, ArtifactService]
})

export class ArtifactTypeDeleteComponent extends BaseComponent implements OnInit {
    
    @Input() callback: Function;
    @Input() artifactTypeId: number;

    @Output() onCancel = new EventEmitter();

    private artifactType: ArtifactType;
    
    private count: number = 0;
    private signoff: boolean = false;

    constructor(private artifactTypeService:ArtifactTypeService,
        private artifactService:ArtifactService            
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.artifactTypeService.getArtifactTypeDetails(this.artifactTypeId).then(result=>{
            this.artifactType = result;            
        });        
        this.artifactService.getArtifacts(this.artifactTypeId, 10, 1, '', SortOrder.Ascending).then(result => {
            this.count = result.total;
        })
    }


    private delete(): void {
        if (this.isLoading)
            return;

        this.isLoading = true;
        if (this.callback)
            this.callback(this.artifactTypeId);
        this.isLoading = false;
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }
}
