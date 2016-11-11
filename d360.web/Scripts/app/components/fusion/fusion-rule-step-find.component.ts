import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FusioRuleStepBaseComponent } from './fusion-rule-step-base.component';
import { FusionService } from '../../services/index';
import { FusionRuleStep, FusionRuleStepEditorModel, PromotionObject, FusionRule } from '../../models/fusion.model';
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
        console.log(s);
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
            });
    }

    changeFindSearchType(search): Promise<any> {

        //Clear out values
        //this.findParentSetting = false;
        console.log(search);
        switch (search) {
            case 'Glossary':
                return Promise.resolve();
            case 'ResultFromStep':
                this.steps = [];
                return this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.steps = r;
                    });
            case 'FusionOwner':
                return this.loadFusionOwners();
            case 'Fusion':
                this.objects = [];
                return this.fusionService.getFindFusionAttributeTypes()
                    .then(r => {
                        this.objects = r;
                    });
            case 'Promotion':
                this.objects = [];
                return this.fusionService.getFindAttributeTypes()
                    .then(r => {
                        this.objects = r;
                    });
            default:
                return Promise.resolve();
        }
    }

    loadFusionOwners(): Promise<any> {
        return this.fusionService.getPromotionFusionOwnerRules(this.fusionID)
            .then(r => {
                console.log(r);
                this.owners = r;
            });
    }

    changeGlossaryType(): Promise<any> {
        this.objects = [];
        if (this.settings.Object == 'ArtifactType')
            return this.fusionService.getFindArtifactTypes()
                .then(r => {
                    this.objects = r;
                });
        if (this.settings.Object == 'TaxonomyType')
           return  this.fusionService.getFindModels()
                .then(r => {
                    this.objects = r;
                });
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
                });
        } else
            this.showTargetField = false;
        return Promise.resolve();

    }

};

