import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionRule} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';

import {StringHelpers} from '../../../static/string-helpers';

import {FusioRuleStepBaseComponent} from './fusion-rule-step-base.component';

@Component({
    selector: 'd3s-fusion-rule-step-lineage',
    templateUrl: './fusion-rule-step-lineage.component.html',
    providers: [FusionService]
})

export class FusionRuleStepLineageComponent extends FusioRuleStepBaseComponent implements OnInit {
    @Input() fusionID: number;
    @Input() ruleID: number;
    @Input() ruleStepID: number = 0;
    @Input() settings: any;
    @Input() showErrors = false;
    @Input() isValid = false;
    @Output() isValidChange = new EventEmitter();

    @Output() settingsChange = new EventEmitter();

    rule: FusionRule;

    technicalsteps: any[] = [];
    steps: any[] = [];
    roles: any[] = [];

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "Lineage");

        this.fusionService
            .getLineageRoles()
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                r => {
                    this.roles = <any>r;

                    this.fusionService
                        .getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                        .pipe(takeUntil(this.destroySubject$))
                        .subscribe(
                            r => {
                                this.steps = r.slice(0);            //take a copy of the r array
                                this.technicalsteps = r.slice(0);   //take a copy of the r array
                                this.technicalsteps.unshift({ID: null, Description: ''});
                                this.validate();
                            }
                        )
                    ;
                }
            )
        ;
    }

    validate() {
        this.isValid = true;

        if (StringHelpers.isNullOrEmpty(this.settings.Role)
            || StringHelpers.isNullOrEmpty(this.settings.SubjectID)
            || StringHelpers.isNullOrEmpty(this.settings.ObjectID)
        ) {
            this.isValid = false;
        }

        this.isValidChange.emit(this.isValid);
    }
}
