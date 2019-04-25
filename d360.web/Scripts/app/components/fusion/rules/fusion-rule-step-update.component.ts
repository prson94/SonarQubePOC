import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionService} from '../../../services/fusion.service';

import {StringHelpers} from '../../../static/string-helpers';

import {FusioRuleStepBaseComponent} from './fusion-rule-step-base.component';

@Component({
    selector: 'd3s-fusion-rule-step-update',
    templateUrl: './fusion-rule-step-update.component.html',
    providers: [FusionService]
})

export class FusionRuleStepUpdateComponent extends FusioRuleStepBaseComponent implements OnInit {
    @Input() fusionID: number;
    @Input() ruleID: number;
    @Input() ruleStepID: number = 0;
    @Input() settings: any;
    @Input() showErrors = false;
    @Input() isValid = false;
    @Output() isValidChange = new EventEmitter();

    @Output() settingsChange = new EventEmitter();

    steps: any[] = [];

    showTargetField = false;

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.removeIrrelevantSettings(this.settings, "Update");

        this.fusionService
            .getPromotionRuleSteps(this.ruleID, this.ruleStepID)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                r => {
                    this.steps = <any>r;
                    this.validate();
                }
            )
        ;
    }


    validate() {
        this.isValid = true;

        if (StringHelpers.isNullOrEmpty(this.settings.SubjectID)) {
            this.isValid = false;
        }

        this.isValidChange.emit(this.isValid);
    }
}
