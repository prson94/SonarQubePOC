import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionRule, FusionRuleStepEditorModel} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';

import {BaseComponent} from '../../shared/base.component';

@Component({
    selector: 'd3s-fusion-rule-step',
    template: `
        <div class="tile tile-detail">
            <header *ngIf="ruleStepID != 0">Edit Rule Step</header>
            <header *ngIf="ruleStepID == 0">Add Rule Step</header>
            <div *ngIf="isLoading">

            </div>
            <div *ngIf="!isLoading">
                <div class="row" style="margin-bottom: 20px">
                    <div class="col l6 m6 s12">
                        <div class="FieldName" style="display:block">Description</div>
                        <input type="text" style="width:100%" [(ngModel)]="model.RuleStep.Description"
                               name="description"/>
                    </div>
                    <div class="col l6 m6 s12">
                        <div class="FieldName" style="display:block">Action</div>
                        <select [(ngModel)]="model.RuleStep.Action" style="width:100%" name="action" required
                                (ngModelChange)="changeAction()">
                            <option *ngFor="let i of actionTypes" [value]="i.value">{{i.text}}</option>
                        </select>
                    </div>
                </div>
                <div [ngSwitch]="model.RuleStep.Action">
                    <div *ngSwitchCase="'promote'">
                        <d3s-fusion-rule-step-promote [ruleID]="ruleID" [ruleStepID]="ruleStepID"
                                                      [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"
                                                      [showErrors]="showErrors"
                                                      [(isValid)]="isValid"></d3s-fusion-rule-step-promote>
                    </div>
                    <div *ngSwitchCase="'find'">
                        <d3s-fusion-rule-step-find [ruleID]="ruleID" [ruleStepID]="ruleStepID"
                                                   [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"
                                                   [showErrors]="showErrors"
                                                   [(isValid)]="isValid"></d3s-fusion-rule-step-find>
                    </div>
                    <div *ngSwitchCase="'lineage'">
                        <d3s-fusion-rule-step-lineage [ruleID]="ruleID" [ruleStepID]="ruleStepID"
                                                      [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"
                                                      [showErrors]="showErrors"
                                                      [(isValid)]="isValid"></d3s-fusion-rule-step-lineage>
                    </div>
                    <div *ngSwitchCase="'relate'">
                        <d3s-fusion-rule-step-relate [ruleID]="ruleID" [ruleStepID]="ruleStepID"
                                                     [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"
                                                     [showErrors]="showErrors"
                                                     [(isValid)]="isValid"></d3s-fusion-rule-step-relate>
                    </div>
                    <div *ngSwitchCase="'findrelation'">
                        <d3s-fusion-rule-step-findviarelation [ruleID]="ruleID" [ruleStepID]="ruleStepID"
                                                              [fusionID]="model.FusionID"
                                                              [(settings)]="model.RuleStep.Settings"
                                                              [showErrors]="showErrors"
                                                              [(isValid)]="isValid"></d3s-fusion-rule-step-findviarelation>
                    </div>
                    <div *ngSwitchCase="'update'">
                        <d3s-fusion-rule-step-update [ruleID]="ruleID" [ruleStepID]="ruleStepID"
                                                     [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"
                                                     [showErrors]="showErrors"
                                                     [(isValid)]="isValid"></d3s-fusion-rule-step-update>
                    </div>
                </div>
                <div class="row" style="margin-top: 20px">
                    <div class="col s12">
                        <button type="button" label="Save" pButton (click)="save()"
                                [disabled]="!isValid || model.RuleStep.Action == null"></button>
                        <button type="button" label="Cancel" pButton (click)="onClose.emit()"></button>
                    </div>
                </div>
            </div>
        </div>`,
    providers: [FusionService]
})

export class FusionRuleStepComponent extends BaseComponent implements OnInit {
    @Input() ruleID: number;
    @Input() ruleStepID: number = 0;

    @Output() onClose = new EventEmitter();
    @Output() onSave = new EventEmitter();

    showErrors = false;
    isValid = false;

    actionTypes: any[] = [
        {text: 'Promote', value: 'promote'},
        {text: 'Find', value: 'find'},
        {text: 'Find via Relationship', value: 'findrelation'},
        {text: 'Relate', value: 'relate'},
        {text: 'Update', value: 'update'},
    ];

    model: FusionRuleStepEditorModel;
    rule: FusionRule;
    settings: any;

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;

        if (this.ruleStepID == 0) {
            this.fusionService
                .getAddFusionRuleStep(this.ruleID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.model = r;
                        this.model.RuleStep.Action = 'Promote';

                        this.isLoading = false;
                    });
        } else {
            this.fusionService
                .getEditFusionRuleStep(this.ruleID, this.ruleStepID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.model = r;

                        this.model.RuleStep.Action = this.model.RuleStep.Action.toLowerCase();

                        this.isLoading = false;
                    }
                )
            ;
        }
    }


    save() {
        if (this.isLoading) {
            return;
        }

        this.showErrors = false;

        if (!this.isValid || this.model.RuleStep.Action == null) {
            this.showErrors = true;

            return;
        }

        if (this.ruleStepID && this.ruleStepID != 0) {
            //edit
            this.isLoading = true;

            this.fusionService
                .putEditFusionRuleStep(this.model.RuleStep)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.onSave.emit(r);

                        this.isLoading = false;
                    }
                );
        } else {
            //add
            this.isLoading = true;

            this.fusionService
                .postAddFusionRuleStep(this.model.RuleStep)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.onSave.emit(r);

                        this.isLoading = false;
                    }
                )
            ;
        }
    }

    changeAction() {
        this.showErrors = false;
        this.isValid = false;
    }
}
