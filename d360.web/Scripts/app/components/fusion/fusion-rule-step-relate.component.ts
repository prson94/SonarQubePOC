import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FusioRuleStepBaseComponent } from './fusion-rule-step-base.component';
import { FusionService } from '../../services/fusion.service';
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
    @Input() showErrors = false;
    @Input() isValid = false;
    @Output() isValidChange = new EventEmitter();

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
                        this.validate();
                    });
            });
    }

    changeObjectSearch() {
        this.changeSearch('Object');
    }

    changeSubjectSearch() {
        this.changeSearch('Subject');
    }

    changeSearch(prefix: string) {
        if (prefix != null && this.settings[prefix] == null)
            this.settings[prefix] = {};
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
        }
        this.validate();
        this.settingsChange.emit(this.settings);

    }

    validate() {
        this.isValid = true;

        if (this.settings.IntersectType == null)
            this.isValid = false;
        if (this.settings.SubjectSearch == null || this.settings.ObjectSearch == null)
            this.isValid = false;
        if (this.settings.SubjectSearch != null && this.settings.SubjectSearch != 'Self' && this.settings.SubjectID == null)
            this.isValid = false;
        if (this.settings.ObjectSearch != null && this.settings.ObjectSearch != 'Self' && this.settings.SubjectID == null)
            this.isValid = false;

        this.isValidChange.emit(this.isValid);
    }
};

