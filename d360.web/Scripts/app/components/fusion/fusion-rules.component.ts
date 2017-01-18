import { Input, Component } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { MessagesService } from '../../services/messages.service';
import { FusionRule, FusionRuleStep, FusionRuleItem, FusionRuleMapping } from '../../models/fusion.model';


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
    selectedFusionRuleItem: FusionRuleItem = null;
    selectedFusionRuleStepMapping: FusionRuleMapping = null;

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

    addItem() {
        if (this.selectedFusionRule != null && this.selectedFusionRule.ObjectType != 'FusionQueryAttributeType')
            this.formMode = FormMode.AddItem;
    }

    deleteRule() {
        this.selectedFusionRuleStepMapping = null;
        this.selectedFusionRuleStep = null;
        this.selectedFusionRuleItem = null;
        this.selectedFusionRule = null;
    }

    deleteStep() {
        this.selectedFusionRuleStepMapping = null;
        this.selectedFusionRuleStep = null;
    }

    deleteItem() {
        if (this.selectedFusionRule.ObjectType != 'FusionQueryAttributeType')
            this.formMode = FormMode.DeleteItem;
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
    DeleteItem,
    AddItem,
    EditMapping,
    AddMapping,
    DeleteMapping,
}