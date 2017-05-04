import { Input, Component } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { MessagesService } from '../../services/messages.service';
import { FusionRule, FusionRuleStep, FusionRuleFilter, FusionRuleItem, FusionRuleMapping } from '../../models/fusion.model';


@Component({
    selector: 'd3s-fusion-rules',
    templateUrl: './fusion-rules.component.html',
    providers: [FusionService]
})

export class FusionRulesComponent extends BaseComponent {
    @Input() fusionID: number;
    @Input() fusionTypeID: number;    
    formMode = FormMode.Default;
    FormMode = FormMode;

    selectedFusionRule: FusionRule = null;
    selectedFusionRuleStep: FusionRuleStep = null;
    selectedFusionRuleFilter: FusionRuleFilter = null;
    selectedFusionRuleStepMapping: FusionRuleMapping = null;

    showRulePromotionHistory: boolean;
    showFilterAdd: boolean = true;

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();
    }

    showMessage(e: any) {
        this.showMessageForResult(this.messagesService, e);
        this.formMode = FormMode.Default;
    }

    addStep() {
        if (this.selectedFusionRule != null)
            this.formMode = FormMode.AddStep;
    }

    editStep() {
        if (this.selectedFusionRule != null && this.selectedFusionRuleStep != null)
            this.formMode = FormMode.EditStep;
    }

    addMapping() {
        if (this.selectedFusionRuleStep != null)
            this.formMode = FormMode.AddMapping;
    }

    editMapping() {
        if (this.selectedFusionRuleStep != null && this.selectedFusionRuleStepMapping != null)
            this.formMode = FormMode.EditMapping;
    }

    addFilter() {
        if (this.selectedFusionRule != null)
            this.formMode = FormMode.AddFilter;
    }

    editFilter() {
        if (this.selectedFusionRule != null)
            this.formMode = FormMode.EditFilter;
    }

    deleteRule() {
        this.selectedFusionRuleStepMapping = null;
        this.selectedFusionRuleStep = null;
        this.selectedFusionRuleFilter = null;
        this.selectedFusionRule = null;
    }

    deleteStep() {
        this.selectedFusionRuleStepMapping = null;
        this.selectedFusionRuleStep = null;
    }

    deleteFilter() {
        this.formMode = FormMode.DeleteFilter;
    }

    selectRule(e: any) {
        this.selectedFusionRule = e;
        this.showRulePromotionHistory = false;
        this.showFilterAdd = true;

    }
};

enum FormMode {
    Default,
    EditRule,
    DeleteRule,
    AddRule,
    EditStep,
    DeleteStep,
    AddStep,
    DeleteFilter,
    EditFilter,
    AddFilter,
    EditMapping,
    AddMapping,
    DeleteMapping,
}