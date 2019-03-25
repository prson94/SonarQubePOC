import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {FusioRuleStepBaseComponent} from './fusion-rule-step-base.component';
import {FusionService} from '../../../services/fusion.service';
import {FusionRule} from '../../../models/fusion.model';
import {StringHelpers} from '../../../static/string-helpers';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

@Component({
    selector: 'd3s-fusion-rule-step-findviarelation',
    templateUrl: './fusion-rule-step-findviarelation.component.html',
    providers: [FusionService]
})

export class FusionRuleStepFindViaRelationComponent extends FusioRuleStepBaseComponent implements OnInit {
    @Input() fusionID: number;
    @Input() ruleID: number;
    @Input() ruleStepID: number = 0;
    @Input() settings: any;
    @Input() showErrors = false;
    @Input() isValid = false;
    @Output() isValidChange = new EventEmitter();

    @Output() settingsChange = new EventEmitter();

    searchTypes: any[] = [
        {value: "Self", text: "Self"},
        {value: "ResultFromStep", text: "Result From Step"}
    ];

    rule: FusionRule;

    steps: any[] = [];
    relations: any[] = [];

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "FindViaRelation");

        this.fusionService
            .getFusionRelationIntersectTypes()
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                r => {
                    this.relations = r;

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
            )
        ;
    }

    validate() {
        this.isValid = true;

        if (StringHelpers.isNullOrEmpty(this.settings.IntersectType) || StringHelpers.isNullOrEmpty(this.settings.Search)) {
            this.isValid = false;
        }

        if (this.settings.Search != null && this.settings.Search != 'Self' && StringHelpers.isNullOrEmpty(this.settings.ID)) {
            this.isValid = false;
        }

        this.isValidChange.emit(this.isValid);
    }
}
