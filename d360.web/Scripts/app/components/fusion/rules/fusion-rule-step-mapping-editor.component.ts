import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {BaseComponent} from '../../shared/base.component';
import {FusionService} from '../../../services/fusion.service';
import {FusionRuleMapping, FusionRuleMappingEditorModel, FusionRuleStep} from '../../../models/fusion.model';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";
import { MessagesObservableService } from '../../../services/messages-observable.service';

declare var CompanySettings;

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-fusion-rule-step-mapping-editor',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <header>{{mode}} Fusion Rule Mapping</header>
            <form #mappingForm="ngForm"
                  (ngSubmit)="save()">
                <div class="row">
                    <div class="col s12">
                        <input type="checkbox"
                               [(ngModel)]="model.Item.IsConstantValue"
                               name="isConstant"/> Store a
                        fixed source value?
                    </div>
                </div>
                <div class="row">
                    <div class="col s6"
                         *ngIf="model.Item.IsConstantValue">
                        <div class="FieldName"
                             style="display:block;">Source
                        </div>
                        <input type="text"
                               [(ngModel)]="model.Item.ConstantValue"
                               style="width:95%"
                               name="constant"
                               required/>
                    </div>
                    <div class="col s6"
                         *ngIf="!model.Item.IsConstantValue">
                        <div class="FieldName"
                             style="display:block;">Source
                        </div>
                        <select [(ngModel)]="model.sourceValue"
                                style="width:95%"
                                name="source"
                                required>
                            <ng-container *ngFor="let i of model.SourceFields">
                                <option *ngIf="i.Text != 'ID' && i.Text != 'TextPath'"
                                        [value]="i.Value">{{i.Text}}</option>
                            </ng-container>
                        </select>
                    </div>
                    <div class="col s6">
                        <div class="FieldName"
                             style="display:block;">Target
                        </div>
                        <select [(ngModel)]="model.targetValue"
                                style="width:95%"
                                name="target"
                                required>
                            <option *ngFor="let i of model.TargetFields"
                                    [value]="i.Value">{{i.Text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12"
                         style="padding-top:10px">
                        <button pButton
                                type="submit"
                                label="Save"
                                [disabled]="isLoading || !mappingForm.form.valid"></button>
                        <button pButton
                                type="button"
                                label="Cancel"
                                (click)="onClose.emit()"></button>
                    </div>
                </div>
            </form>
        </div>
    `,
    providers: [FusionService]
})

export class FusionRuleStepMappingEditorComponent extends BaseComponent implements OnInit {
    @Input() fusionRuleStep: FusionRuleStep;
    @Input() fusionRuleStepMapping: FusionRuleMapping;
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();
    @Output() onError = new EventEmitter();

    model: FusionRuleMappingEditorModel;
    mode = "Add";

    destroySubject$: Subject<void> = new Subject();

    constructor(
        private fusionService: FusionService,
        private messagesService: MessagesObservableService
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        if (this.fusionRuleStep == null || this.fusionRuleStep.ID == null) {
            return;
        }

        this.isLoading = true;
        if (this.fusionRuleStepMapping == null) {
            this.mode = "Add";
            this.fusionService
                .getAddFusionRuleStepMapping(this.fusionRuleStep.ID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.model = r;

                        //update subject area values with company setting value
                        let subjectArea = this.model.SourceFields.find(s => s.Value == 'TaxonomyTypeID|0');

                        if (subjectArea != null && CompanySettings.ArtifactType_TaxonomyTypeID != null) {
                            subjectArea.Text = CompanySettings.ArtifactType_TaxonomyTypeID;
                        }

                        subjectArea = this.model.TargetFields.find(s => s.Value == 'TaxonomyTypeID|0');
                        if (subjectArea != null && CompanySettings.ArtifactType_TaxonomyTypeID != null) {
                            subjectArea.Text = CompanySettings.ArtifactType_TaxonomyTypeID;
                        }

                        this.isLoading = false;
                    });
        } else {
            this.mode = "Edit";
            this.fusionService
                .getEditFusionRuleStepMapping(this.fusionRuleStepMapping.ID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.model = r;

                        //update subject area values with company setting value
                        let subjectArea = this.model.SourceFields.find(s => s.Value == 'TaxonomyTypeID|0');
                        if (subjectArea != null && CompanySettings.ArtifactType_TaxonomyTypeID != null) {
                            subjectArea.Text = CompanySettings.ArtifactType_TaxonomyTypeID;
                        }

                        subjectArea = this.model.TargetFields.find(s => s.Value == 'TaxonomyTypeID|0');
                        if (subjectArea != null && CompanySettings.ArtifactType_TaxonomyTypeID != null) {
                            subjectArea.Text = CompanySettings.ArtifactType_TaxonomyTypeID;
                        }

                        this.loadMappingValues(this.model);

                        this.isLoading = false;
                    }
                )
            ;
        }
    }

    save() {
        let m = this.model.Item;
        let tv = this.model.targetValue.split('|');

        if (this.isLoading) {
            return;
        }

        this.isLoading = true;

        if (!m.IsConstantValue) {
            let sv = this.model.sourceValue.split('|');

            m.SourceFieldName = sv[0];
            m.SourceFieldTypeID = parseInt(sv[1]);
        } else {
            m.SourceFieldName = null;
            m.SourceFieldTypeID = 0;
        }

        m.TargetFieldName = tv[0];
        m.TargetFieldTypeID = parseInt(tv[1]);

        if (this.fusionRuleStepMapping == null) {
            this.fusionService
                .postAddFusionRuleStepMapping(m)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.showMessageForResult(this.messagesService, <any>r);

                        this.isLoading = false;
                        this.onSave.emit();
                    }
                );
        } else {
            this.fusionService
                .putEditFusionRuleStepMapping(m)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.showMessageForResult(this.messagesService, <any>r);

                        this.isLoading = false;
                        this.onSave.emit();
                    }
                )
            ;
        }
    }

    //#region helpers

    loadMappingValues(mapping: FusionRuleMappingEditorModel) {
        if (mapping.Item.SourceFieldTypeID == 0) {
            mapping.sourceValue = `${mapping.Item.SourceFieldName}|${mapping.Item.SourceFieldTypeID}`;
        } else {
            mapping.SourceFields.forEach(
                f => {
                    if (f.Value.indexOf('|') != -1) {
                        if (mapping.Item.SourceFieldTypeID.toString() == f.Value.split('|')[1]) {
                            mapping.sourceValue = f.Value;

                            return;
                        }
                    }
                }
            );
        }

        if (mapping.Item.TargetFieldTypeID == 0) {
            mapping.targetValue = `${mapping.Item.TargetFieldName}|${mapping.Item.TargetFieldTypeID}`;
        } else {
            mapping.TargetFields.forEach(
                f => {
                    if (f.Value.indexOf('|') != -1) {
                        if (mapping.Item.TargetFieldTypeID.toString() == f.Value.split('|')[1]) {
                            mapping.targetValue = f.Value;

                            return;
                        }
                    }
                }
            );
        }
    }

    //#endregion
}
