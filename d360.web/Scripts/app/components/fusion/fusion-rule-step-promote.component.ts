import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { FusioRuleStepBaseComponent } from './fusion-rule-step-base.component';
import { FusionService } from '../../services/index';
import { FusionRuleStep, FusionRuleStepEditorModel, PromotionObject, FusionRule } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rule-step-promote',
    templateUrl: './fusion-rule-step-promote.component.html',
    providers: [FusionService] 
})

export class FusionRuleStepPromoteComponent extends FusioRuleStepBaseComponent implements OnInit {
    @Input() fusionID: number;
    @Input() ruleID: number;
    @Input() ruleStepID: number = 0;
    @Input() settings: any;
    @Input() showErrors = false;
    @Input() isValid = false;
    @Output() isValidChange = new EventEmitter();

    @Output() settingsChange = new EventEmitter();

    promotionObjectTypes: any[] = [
        { value: "ArtifactType", text: "Artifact" },
        { value: "TaxonomyType", text: "Model" },
        { value: "ReferenceItemType", text: "Reference" }
    ];

    parentSearchTypes: any[] = [
        { value: "Direct", text: "Direct" },
        { value: "FusionOwner", text: "Fusion Owner" },
        { value: "ResultFromStep", text: "Result From Step" }
    ];

    rule: FusionRule;

    showPromotionParent: boolean = false;
    steps: any[] = [];
    promotionObjects: any[] = [];
    parents: any[] = [];

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        
        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "Promote");
        this.loadTypes()
            .then(() => {
                this.switchParentDisplay(this.settings.ObjectID).then(() => {
                    this.switchParentSearch();
                });
            });
        
    }

    loadTypes(): Promise<any> {
        this.promotionObjects = [];
        if (this.settings.Object == 'ArtifactType')
            return this.fusionService.getFindArtifactTypes()
                .then(r => {
                    this.promotionObjects = r;
                    this.validate();
                });
        if (this.settings.Object == 'TaxonomyType')
            return this.fusionService.getFindModels()
                .then(r => {
                    this.promotionObjects = r;
                    this.validate();
                });
        if (this.settings.Object == 'ReferenceItemType')
            return this.fusionService.getFindReferenceItemTypes()
                .then(r => {
                    this.promotionObjects = r;
                    this.validate();
                });
        return Promise.resolve();
    }

    changePromotionObjectType(): Promise<any> {
        this.showPromotionParent = false;
        return this.loadTypes();
    }

    switchParentDisplay(id): Promise<any> {
        if (id != undefined) {
            let item = this.promotionObjects.find(i => i.ID == id);
            if (item) {
                if (this.settings.Object == "ArtifactType") {
                    if (item.ParentID) {
                        if (item.ParentID != 0)
                            this.showPromotionParent = true;
                    }
                    else {
                        this.showPromotionParent = false;
                        this.settings.ParentObjectSearch = null;
                        this.settings.ParentObject = null;
                        this.settings.ParentObjectID = null;
                    }
                }
                else {
                    this.showPromotionParent = false;
                    this.settings.ParentObjectSearch = null;
                    this.settings.ParentObject = null;
                    this.settings.ParentObjectID = null;
                }

            }
        }
        this.validate();
        return Promise.resolve();
    }

    changePromotionObject(id): Promise<any> {
        return this.switchParentDisplay(id);
    }

    switchParentSearch(): Promise<any> {
        this.parents = [];
        switch (this.settings.ParentObjectSearch) {
            case "Direct":
                let item = this.promotionObjects.find(i => i.ID == this.settings.ObjectID);
                let obj = this.settings.Object;
                if (obj == "ArtifactType")
                    obj = "Artifact";
                let objid = item.ParentID;

                this.settings.ParentObject = obj; //need to set this when selecting Direct.

                return this.fusionService.getPromotionParents(objid, obj)
                    .then(r => {
                        this.parents = r;
                        this.validate();
                    });
            case "ResultFromStep":
                this.settings.ParentObject = "Step"; //need to set this when selecting ResultFromStep.
                return this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.parents = r;
                        this.validate();
                    });
            case "FusionOwner":
                this.settings.ParentObject = "Artifact"; //need to set this when selecting FusionOwner.
                return this.fusionService.getPromotionFusionOwnerRules(this.fusionID)
                    .then(r => {
                        this.parents = r;
                        this.validate();
                    });
        }
        return Promise.resolve();
    }

    changeParentSearch(): Promise<any> {
        this.settings.ParentObjectID = null;
        return this.switchParentSearch();
    }

    validate() {
        this.isValid = true;
        if (this.settings.Object == null || this.settings.ObjectID == null)
            this.isValid = false;
        if (this.showPromotionParent) {
            if (this.settings.ParentObjectSearch == null || this.settings.ParentObjectID == null)
                this.isValid = false;
        }
        this.isValidChange.emit(this.isValid);
    }

};

