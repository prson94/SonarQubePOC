import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FusioRuleStepBaseComponent } from './fusion-rule-step-base.component';
import { FusionService } from '../../../services/fusion.service';
import { FusionRuleStep, FusionRuleStepEditorModel, PromotionObject, FusionRule } from '../../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rule-step-find',
    templateUrl: './fusion-rule-step-find.component.html',
    providers: [FusionService] 
})

export class FusionRuleStepFindComponent extends FusioRuleStepBaseComponent implements OnInit {
    @Input() fusionID: number;
    @Input() ruleID: number;
    @Input() ruleStepID: number = 0;
    @Input() settings: any;
    @Input() showErrors = false;
    @Input() isValid = false;
    @Output() isValidChange = new EventEmitter();

    @Output() settingsChange = new EventEmitter(); 

    showTargetField = false;

    searchTypes: any[] = [
        { value: "Fusion", text: "Fusion" },
        { value: "FusionOwner", text: "Fusion Owner" },
        { value: "Glossary", text: "Glossary" },
        { value: "Promotion", text: "Previous Promotion" },
        { value: "ResultFromStep", text: "Result From Step" }
    ];

    glossaryFindObjectTypes: any[] = [
        { value: "ArtifactType", text: "Artifact" },
        { value: "TaxonomyType", text: "Model" }
    ];

    sourceFields: any[] = [];
    targetFields: any[] = [];
    steps: any[] = [];
    objects: any[] = [];
    owners: any[] = [];

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        let s = this.settings;
        
        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "Find");

        switch (s.ObjectSearch) {
            case "Fusion":
                this.fusionService.getFindFusionAttributeTypes()
                    .then(r => {
                        this.objects = r;
                    });
            case "FusionOwner":
                this.loadFusionOwners();
                break;
            case "Glossary":
                this.changeGlossaryType()
                    .then(() => this.changeGlossaryTypeFields());
                break;
            case "Promotion":
                this.fusionService.getFindAttributeTypes()
                    .then(r => {
                        this.objects = r;
                    });
                break;
            case "ResultFromStep":
                this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.steps = r;
                    });
                break;
        }

        this.fusionService.getFusionRules(this.fusionID)
            .then(r => {
                this.rule = r.find(i => i.ID == this.ruleID);
            })
            .then(() => this.fusionService.getFindSourceFields(this.rule.ObjectType, this.rule.ObjectID))
            .then(r => {
                this.sourceFields = r;
                this.sourceFields.push({ ID: 0, FriendlyName: 'Name' });
                this.sourceFields.push({ ID: -2, FriendlyName: 'ParentID' });
                this.validate();
            });

    }

    changeFindSearchType(search): Promise<any> {

        //Clear out values
        delete this.settings.Object;
        delete this.settings.ObjectID;
        delete this.settings.FilterField;
        delete this.settings.TargetField;

        switch (search) {
            case 'Glossary':
                this.validate();
                return Promise.resolve();
            case 'ResultFromStep':
                this.steps = [];
                return this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.steps = r;
                        this.validate();
                    });
            case 'FusionOwner':
                return this.loadFusionOwners();
            case 'Fusion':
                this.objects = [];
                return this.fusionService.getFindFusionAttributeTypes()
                    .then(r => {
                        this.objects = r;
                        this.validate();
                    });
            case 'Promotion':
                this.objects = [];
                return this.fusionService.getFindAttributeTypes()
                    .then(r => {
                        this.objects = r;
                        this.validate();
                    });
            default:
                this.validate();
                return Promise.resolve();
        }
    }

    loadFusionOwners(): Promise<any> {
        return this.fusionService.getPromotionFusionOwnerRules(this.fusionID)
            .then(r => {
                this.owners = r;
                this.validate();
            });
    }

    changeGlossaryType(): Promise<any> {
        this.objects = [];
        if (this.settings.Object == 'ArtifactType')
            return this.fusionService.getFindArtifactTypes()
                .then(r => {
                    this.objects = r;
                    this.validate();
                });
        if (this.settings.Object == 'TaxonomyType')
           return  this.fusionService.getFindModels()
                .then(r => {
                    this.objects = r;
                    this.validate();
                });
        this.validate();
        return Promise.resolve();
    }

    changeGlossaryTypeFields(): Promise<any> {
        //let item = this.findObjects.find(i => i.ID == this.selectedFindObject);

        this.targetFields = [];
        if (this.settings.Object == 'ArtifactType') {
            return this.fusionService.getFindSourceFields('ArtifactType', this.settings.ObjectID)
                .then(r => {
                    this.targetFields = r;
                    this.targetFields.push({
                        ID: 0,
                        FriendlyName: 'Name'
                    });
                    this.showTargetField = true;
                    this.validate();
                });
        } else if (this.settings.Object == 'TaxonomyType') {
            return this.fusionService.getFindSourceFields('TaxonomyType', this.settings.ObjectID)
                .then(r => {
                    this.targetFields = r;
                    this.targetFields.push({
                        ID: 0,
                        FriendlyName: 'Name'
                    });
                    this.showTargetField = true;
                    this.validate();
                });
        } else {
            this.validate();
            this.showTargetField = false;
        }
        return Promise.resolve();

    }

    validate() {
        this.isValid = true;
        if (this.settings.ObjectSearch == null)
            this.isValid = false;
        else if (this.settings.ObjectSearch == 'Fusion') {
            if (this.settings.FilterField == null || this.settings.ObjectID == null)
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'FusionOwner') {
            if (this.settings.ObjectID == null)
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'Glossary') {
            if (this.settings.FilterField == null || this.settings.Object == null || this.settings.ObjectID == null || this.settings.TargetField == null)
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'Promotion') {
            if (this.settings.FilterField == null || this.settings.ObjectID == null)
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'ResultFromStep') {
            if (this.settings.ObjectID == null)
                this.isValid = false;
        }

        this.isValidChange.emit(this.isValid);
    }

};

