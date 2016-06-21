///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { NgForm } from '@angular/common';
import { Button, Editor, Header, InputText, Checkbox } from 'primeng/primeng';
import { ArtifactType, ArtifactTypeEditorModel } from '../../models/artifact-type.model';
import { ArtifactTypeService } from '../../services/artifact-type.service';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-artifact-type-form',
    templateUrl: 'scripts/app/components/forms/artifact-type.form.html',
    providers: [ArtifactTypeService],
    directives: [Button, Editor, Header, InputText, Checkbox],
})

export class ArtifactTypeForm implements OnInit, OnChanges {
    @Input() id: number;
    @Input() parentID: number;
    @Input() title: string = "Add Artifact Type";
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private model;
    private isLoading = false;
    private isSaving = false;
    private initialItem: ArtifactTypeEditorModel;

    constructor(private artifactTypeService: ArtifactTypeService) {
        this.model = new ArtifactTypeEditorModel();
        this.model.ArtifactType = new ArtifactType();
    }

    ngOnInit() {
        this.initialItem = _.cloneDeep(this.model);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id' || p == 'parentID') {
                this.load();
                this.initialItem = _.cloneDeep(this.model);
            }
        }
    }


    private load(): void {
        this.isLoading = true;

        this.artifactTypeService.getArtifactTypeEditor(this.id, this.parentID)
            .then(data => {
                this.model = data;
                //console.log(data);
                this.isLoading = false;
            });
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private save(): void {
        this.isSaving = true;
        if (this.model.ArtifactType.ID > 0)
            this.artifactTypeService.putArtifactType(this.model)
                .then(data => {
                    this.isSaving = false;
                    this.onSuccess.emit(null);
                    this.onComplete.emit(null);
                });
        else
            this.artifactTypeService.postArtifactType(this.model)
                .then(data => {
                    this.isSaving = false;
                    this.onSuccess.emit(null);
                    this.onComplete.emit(null);
                });


        //service call here
    }
}
