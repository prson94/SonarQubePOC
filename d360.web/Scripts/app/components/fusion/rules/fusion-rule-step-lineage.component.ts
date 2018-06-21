import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FusioRuleStepBaseComponent } from './fusion-rule-step-base.component';
import { FusionService } from '../../../services/fusion.service';
import { FusionRuleStep, FusionRuleStepEditorModel, PromotionObject, FusionRule } from '../../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

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

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "Lineage");

        this.fusionService.getLineageRoles()
            .then(r => {
                this.roles = r;
            })
            .then(() => {
                this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.steps = r.slice(0);            //take a copy of the r array
                        this.technicalsteps = r.slice(0);   //take a copy of the r array
                        this.technicalsteps.unshift({ ID: null, Description: '' });
                        this.validate();
                    });
            });
    }

    validate() {
        this.isValid = true;
        if (this.settings.Role == null
            || this.settings.SubjectID == null
            || this.settings.ObjectID == null)
            this.isValid = false;

        this.isValidChange.emit(this.isValid);
    }
};