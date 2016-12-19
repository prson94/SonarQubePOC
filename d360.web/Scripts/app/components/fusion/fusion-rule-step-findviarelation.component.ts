import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FusioRuleStepBaseComponent } from './fusion-rule-step-base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionRuleStep, FusionRuleStepEditorModel, PromotionObject, FusionRule } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

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
        { value: "Self", text: "Self" },
        { value: "ResultFromStep", text: "Result From Step" }
    ];

    rule: FusionRule;

    steps: any[] = [];
    relations: any[] = [];

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "FindViaRelation");

        this.fusionService.getFusionRelationIntersectTypes()
            .then(r => {
                this.relations = r;
            })
            .then(r => {
                this.fusionService
                    .getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.steps = r;
                        this.validate();
                    });
            });
    }

    validate() {
        this.isValid = true;
        if (this.settings.IntersectType == null || this.settings.Search == null)
            this.isValid = false;
        if (this.settings.Search != null && this.settings.Search != 'Self' && this.settings.ID == null) 
            this.isValid = false;
        this.isValidChange.emit(this.isValid);
    }

};

