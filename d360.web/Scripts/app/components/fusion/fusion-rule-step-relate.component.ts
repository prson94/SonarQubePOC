import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FusioRuleStepBaseComponent } from './fusion-rule-step-base.component';
import { FusionService } from '../../services/index';
import { FusionRuleStep, FusionRuleStepEditorModel, PromotionObject, FusionRule } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

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

    @Output() settingsChange = new EventEmitter();

    searchTypes: any[] = [
        { value: "FusionOwner", text: "Fusion Owner" },
        { value: "ResultFromStep", text: "Result From Step" },
        { value: "Self", text: "Self" }
    ];

    rule: FusionRule;

    owners: any[] = [];
    steps: any[] = [];
    relations: any[] = [];

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "Relate");

        this.fusionService.getFusionRelationIntersectTypes()
            .then(r => {
                this.relations = r;
            })
            .then(() => {
                this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.steps = r;
                    })
            })
            .then(() => {
                this.fusionService.getPromotionFusionOwnerRules(this.fusionID)
                    .then(r => {
                        this.owners = r;
                        this.owners.forEach(i => {
                            i.text = i.FusionAttributeName + ' Owned By:' + i.OwnerObject;
                        });
                    });
            });
    }
};

