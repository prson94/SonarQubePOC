import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Observable, Subject} from "rxjs";

import {FusionRule} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';

import {StringHelpers} from '../../../static/string-helpers';

import {FusioRuleStepBaseComponent} from './fusion-rule-step-base.component';

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
        {value: "ArtifactType", text: "Artifact"},
        {value: "TaxonomyType", text: "Model"},
        {value: "ReferenceItemType", text: "Reference"}
    ];

    parentSearchTypes: any[] = [
        {value: "Direct", text: "Direct"},
        {value: "FusionOwner", text: "Fusion Owner"},
        {value: "ResultFromStep", text: "Result From Step"}
    ];

    rule: FusionRule;

    showPromotionParent: boolean = false;

    steps: any[] = [];
    promotionObjects: any[] = [];
    parents: any[] = [];

    destroySubject$: Subject<void> = new Subject();

    private get disableTypeChange(): boolean {
        return this.ruleStepID > 0;
    }

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
        this.settings.ObjectID = null;

        return this.loadTypes();
    }

    switchParentDisplay(id): Promise<any> {
        if (id != undefined) {
            let item = this.promotionObjects.find(i => i.ID == id);

            if (item) {
                if (this.settings.Object == "ArtifactType") {
                    if (item.ParentID) {
                        if (item.ParentID != 0) {
                            this.showPromotionParent = true;
                        }
                    } else {
                        this.showPromotionParent = false;
                        this.settings.ParentObjectSearch = null;
                        this.settings.ParentObject = null;
                        this.settings.ParentObjectID = null;
                    }
                } else {
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

    switchParentSearch(): Observable<any> {
        this.parents = [];

        switch (this.settings.ParentObjectSearch) {
            case "Direct":
                let item = this.promotionObjects.find(i => i.ID == this.settings.ObjectID);
                let obj = this.settings.Object;
                let objid = item.ParentID;

                if (obj == "ArtifactType") {
                    obj = "Artifact";
                }

                this.settings.ParentObject = obj; //need to set this when selecting Direct.

                this.fusionService
                    .getPromotionParents(objid, obj)
                    .pipe(takeUntil(this.destroySubject$))
                    .subscribe(
                        r => {
                            this.parents = <any>r;
                            this.validate();
                        }
                    );
            case "ResultFromStep":
                this.settings.ParentObject = "Step"; //need to set this when selecting ResultFromStep.

                this.fusionService
                    .getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .pipe(takeUntil(this.destroySubject$))
                    .subscribe(
                        r => {
                            this.parents = <any>r;
                            this.validate();
                        }
                    );
                break;
            case "FusionOwner":
                this.settings.ParentObject = "Artifact"; //need to set this when selecting FusionOwner.
                return this.fusionService.getPromotionFusionOwnerRules(this.fusionID)
                    .then(r => {
                        this.parents = r;
                        this.validate();
                    });
        }
    }

    changeParentSearch() {
        this.settings.ParentObjectID = null;

        this.switchParentSearch();
    }

    validate() {
        this.isValid = true;

        if (StringHelpers.isNullOrEmpty(this.settings.Object) || StringHelpers.isNullOrEmpty(this.settings.ObjectID)) {
            this.isValid = false;
        }
        if (this.showPromotionParent) {
            if (StringHelpers.isNullOrEmpty(this.settings.ParentObjectSearch) || StringHelpers.isNullOrEmpty(this.settings.ParentObjectID)) {
                this.isValid = false;
            }
        }

        this.isValidChange.emit(this.isValid);
    }
}
