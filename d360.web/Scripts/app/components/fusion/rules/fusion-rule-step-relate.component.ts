import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {FusioRuleStepBaseComponent} from './fusion-rule-step-base.component';
import {FusionService} from '../../../services/fusion.service';
import {FusionRule} from '../../../models/fusion.model';
import {StringHelpers} from '../../../static/string-helpers';
import {forkJoin, Subject} from "rxjs";

@Component({
    selector: 'd3s-fusion-rule-step-relate',
    templateUrl: './fusion-rule-step-relate.component.html',
    providers: [FusionService]
})

export class FusionRuleStepRelateComponent extends FusioRuleStepBaseComponent implements OnInit {
    @Input() fusionID: number;
    @Input() ruleID: number;
    @Input() ruleStepID: number = 0;
    @Input() settings: any;
    @Input() showErrors = false;
    @Input() isValid = false;
    @Output() isValidChange = new EventEmitter();

    @Output() settingsChange = new EventEmitter();

    searchTypes: any[] = [
        {value: "FusionOwner", text: "Fusion Owner"},
        {value: "ResultFromStep", text: "Result From Step"},
        {value: "Self", text: "Self"}
    ];

    rule: FusionRule;

    owners: any[] = [];
    steps: any[] = [];
    relations: any[] = [];

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "Relate");

        forkJoin(
            this.fusionService.getFusionRelationIntersectTypes(),
            this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID),
            this.fusionService.getPromotionFusionOwnerRules(this.fusionID)
        ).subscribe(
            (
                [
                    getFusionRelationIntersectTypes,
                    getPromotionRuleSteps,
                    getPromotionFusionOwnerRules
                ]
            ) => {
                this.relations = <any>getFusionRelationIntersectTypes;
                this.steps = <any>getPromotionRuleSteps;
                this.owners = <any>getPromotionFusionOwnerRules;

                this.owners.forEach(i => {
                    i.text = i.FusionAttributeName + ' Owned By:' + i.OwnerObject;
                });
                this.validate();
            }
        );
    }

    changeObjectSearch() {
        this.settings.ObjectID = null;
        this.changeSearch('Object');
    }

    changeSubjectSearch() {
        this.settings.SubjectID = null;
        this.changeSearch('Subject');
    }

    changeSearch(prefix: string) {
        if (prefix != null && this.settings[prefix] == null) {
            this.settings[prefix] = {};
        }

        switch (this.settings[`${prefix}Search`]) {
            case 'Self':
                this.settings[prefix] = 'Self';
                break;
            case 'FusionOwner':
                this.settings[prefix] = 'Owner';
                break;
            case 'ResultFromStep':
                this.settings[prefix] = 'Step';
                break;
            default:
                break;
        }

        this.validate();
        this.settingsChange.emit(this.settings);
    }

    validate() {
        this.isValid = true;

        if (StringHelpers.isNullOrEmpty(this.settings.IntersectType)) {
            this.isValid = false;
        }
        if (StringHelpers.isNullOrEmpty(this.settings.SubjectSearch) || StringHelpers.isNullOrEmpty(this.settings.ObjectSearch)) {
            this.isValid = false;
        }
        if (this.settings.SubjectSearch != null && this.settings.SubjectSearch != 'Self' && StringHelpers.isNullOrEmpty(this.settings.SubjectID)) {
            this.isValid = false;
        }
        if (this.settings.ObjectSearch != null && this.settings.ObjectSearch != 'Self' && StringHelpers.isNullOrEmpty(this.settings.ObjectID)) {
            this.isValid = false;
        }

        this.isValidChange.emit(this.isValid);
    }
}
