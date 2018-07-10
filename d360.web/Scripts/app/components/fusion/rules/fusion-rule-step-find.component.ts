import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FusioRuleStepBaseComponent } from './fusion-rule-step-base.component';
import { FusionService } from '../../../services/fusion.service';
import { FusionRuleStep, FusionRuleStepEditorModel, PromotionObject, FusionRule } from '../../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';
import { StringHelpers } from '../../../static/string-helpers';

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
    findParent = false;

    searchTypes: any[] = [
        { value: "Fusion", text: "Fusion" },
        { value: "FusionOwner", text: "Fusion Owner" },
        { value: "Glossary", text: "Glossary" },
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
                this.changeGlossaryType(false)
                    .then(() => this.changeGlossaryTypeFields(false));
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
        if (search != 'ResultFromStep') delete this.settings.FindParent;

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

    changeGlossaryType(fromControl:boolean): Promise<any> {
        if (fromControl) {
            this.settings.TargetField = null;
            this.settings.ObjectID = null;
        }
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

    changeGlossarySourceMatchField(): Promise<any> {
        this.settings.Object = null;
        this.settings.ObjectID = null;
        this.settings.TargetField = null;
        this.validate();
        return Promise.resolve();
    }
    changeGlossaryTypeFields(fromControl:boolean): Promise<any> {

        if (StringHelpers.isNullOrEmpty(this.settings.ObjectID)) {
            this.validate();
            return Promise.resolve();
        }
        if (fromControl)
            this.settings.TargetField = null;
        this.targetFields = [];
        if (this.settings.Object == 'ArtifactType') {
            return this.fusionService.getFindSourceFields('ArtifactType', this.settings.ObjectID)
                .then(r => {
                    let t = r.filter(x => x.Type != "ComplexRelationLookup" && x.Type != "OwnershipLookup")
                    this.targetFields = t;
                    this.showTargetField = true;
                    this.validate();
                });
        } else if (this.settings.Object == 'TaxonomyType') {
            return this.fusionService.getFindSourceFields('TaxonomyType', this.settings.ObjectID)
                .then(r => {
                    let t = r.filter(x => x.Type != "ComplexRelationLookup" && x.Type != "OwnershipLookup")
                    this.targetFields = t;
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
        if (StringHelpers.isNullOrEmpty(this.settings.ObjectSearch))
            this.isValid = false;
        else if (this.settings.ObjectSearch == 'Fusion') {
            if (StringHelpers.isNullOrEmpty(this.settings.FilterField) || StringHelpers.isNullOrEmpty(this.settings.ObjectID ))
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'FusionOwner') {
            if (StringHelpers.isNullOrEmpty(this.settings.ObjectID ))
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'Glossary') {
            if (StringHelpers.isNullOrEmpty(this.settings.FilterField) || StringHelpers.isNullOrEmpty(this.settings.Object) || StringHelpers.isNullOrEmpty(this.settings.ObjectID) || StringHelpers.isNullOrEmpty(this.settings.TargetField))
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'Promotion') {
            if (StringHelpers.isNullOrEmpty( this.settings.FilterField)  || StringHelpers.isNullOrEmpty(this.settings.ObjectID))
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'ResultFromStep') {
            if (StringHelpers.isNullOrEmpty(this.settings.ObjectID ))
                this.isValid = false;
        }

        this.isValidChange.emit(this.isValid);
    }

    changeFindParent(e: boolean) {
        this.settings.FindParent = e ? 1 : 0;
    }

};

