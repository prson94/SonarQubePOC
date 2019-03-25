import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionRule, FusionRuleEditorModel} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';
import {MessagesService} from '../../../services/messages.service';

import {BaseComponent} from '../../shared/base.component';

@Component({
    selector: 'd3s-fusion-rule-editor',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <form #ruleForm="ngForm" (ngSubmit)="save()">
                <header>{{mode}} Fusion Rule</header>
                <div class="row">
                    <div class="col s12">
                        <div class="FieldName" style="display:block;">Promote</div>
                        <select [(ngModel)]="selectedAttributeType" required name="object">
                            <option *ngFor="let i of attributeTypes" [value]="i.id">{{i.name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <div class="FieldName" style="display:block;">Description</div>
                        <input type="text" [(ngModel)]="model.Rule.Description" style="width:80%" name="description"/>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12" style="padding-top:8px;">
                        <input type="checkbox" [(ngModel)]="model.Rule.Enabled" name="enabled"/> Enabled?
                    </div>
                </div>
                <div class="row">&nbsp;</div>
                <div class="row">
                    <div class="col s12">
                        <button pButton type="submit" label="Save"
                                [disabled]="!ruleForm.form.valid || isLoading"></button>
                        <button pButton type="button" label="Cancel" (click)="onClose.emit()"></button>
                    </div>
                </div>
            </form>
        </div>
    `,
    providers: [FusionService]
})

export class FusionRuleEditorComponent extends BaseComponent implements OnInit {
    @Input() fusionRule: FusionRule;
    @Input() fusionTypeID: number;
    @Input() fusionID: number;
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();
    @Output() onError = new EventEmitter();

    model: FusionRuleEditorModel;
    mode = "Add";
    attributeTypes: any[] = [];
    selectedAttributeType: string;

    destroySubject$: Subject<void> = new Subject();

    constructor(
        private fusionService: FusionService,
        private messagesService: MessagesService
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        if (this.fusionTypeID != null && this.fusionTypeID != 0) {
            this.fusionService
                .getAddFusionRule(this.fusionTypeID, this.fusionID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.model = new FusionRuleEditorModel();
                        this.model.Rule = new FusionRule();
                        this.model.Rule.FusionID = this.fusionID;
                        this.model.Rule.Description = "";
                        this.model.AttributeTypes = r;
                        this.mode = "Add";

                        this.attributeTypes = [];
                        this.model.AttributeTypes.forEach(i => {
                            this.attributeTypes.push({id: i.ID + '|' + i.Type, name: i.Name});
                        });

                        this.isLoading = false;
                    });
        } else {
            this.fusionService
                .getEditFusionRule(this.fusionRule.ID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.model = r;
                        this.attributeTypes = [];
                        this.model.AttributeTypes.forEach(i => {
                            this.attributeTypes.push({id: i.ID + '|' + i.Type, name: i.Name});
                        });

                        this.selectedAttributeType = this.model.Rule.ObjectID + '|' + this.model.Rule.ObjectType;
                        this.mode = "Edit";

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

        this.isLoading = true;

        if (this.selectedAttributeType != null && this.selectedAttributeType.indexOf('|') > -1) {
            this.model.Rule.ObjectID = +this.selectedAttributeType.split('|')[0];
            this.model.Rule.ObjectType = this.selectedAttributeType.split('|')[1];
        }

        if (this.fusionTypeID != null && this.fusionTypeID != 0) {
            this.fusionService
                .postAddFusionRule(this.model.Rule)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.showMessageForResult(this.messagesService, r);

                        this.isLoading = false;
                        this.onSave.emit();
                    }
                )
                .catch(() => this.onError.emit());
        } else {
            this.fusionService
                .postEditFusionRule(this.model.Rule)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.showMessageForResult(this.messagesService, r);

                        this.isLoading = false;
                        this.onSave.emit();
                    })
                .catch(() => this.onError.emit());
        }
    }
}
