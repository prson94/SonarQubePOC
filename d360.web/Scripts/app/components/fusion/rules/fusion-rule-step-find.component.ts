import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionService} from '../../../services/fusion.service';

import {StringHelpers} from '../../../static/string-helpers';

import {FusioRuleStepBaseComponent} from './fusion-rule-step-base.component';

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
        {value: "Fusion", text: "Fusion"},
        {value: "FusionOwner", text: "Fusion Owner"},
        {value: "Glossary", text: "Glossary"},
        {value: "ResultFromStep", text: "Result From Step"}
    ];

    glossaryFindObjectTypes: any[] = [
        {value: "ArtifactType", text: "Artifact"},
        {value: "TaxonomyType", text: "Model"}
    ];

    sourceFields: any[] = [];
    targetFields: any[] = [];
    steps: any[] = [];
    objects: any[] = [];
    owners: any[] = [];

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        let s = this.settings;

        //Clear out irrelevant properties for this type of step.
        this.removeIrrelevantSettings(this.settings, "Find");

        switch (s.ObjectSearch) {
            case "Fusion":
                this.fusionService
                    .getFindFusionAttributeTypes()
                    .pipe(takeUntil(this.destroySubject$))
                    .subscribe(
                        r => {
                            this.objects = <any>r;
                        }
                    );
                break;
            case "FusionOwner":
                this.loadFusionOwners();
                break;
            case "Glossary":
                this.changeGlossaryType(false)
                    .then(() => this.changeGlossaryTypeFields(false));
                break;
            case "Promotion":
                this.fusionService
                    .getFindAttributeTypes()
                    .pipe(takeUntil(this.destroySubject$))
                    .subscribe(
                        r => {
                            this.objects = <any>r;
                        }
                    );
                break;
            case "ResultFromStep":
                this.fusionService
                    .getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .pipe(takeUntil(this.destroySubject$))
                    .subscribe(
                        r => {
                            this.steps = <any>r;
                        }
                    );
                break;
            default:
                break;
        }

        this.fusionService
            .getFusionRules(this.fusionID)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                r => {
                    this.rule = r.find(i => i.ID == this.ruleID);

                    this.fusionService
                        .getFindSourceFields(this.rule.ObjectType, this.rule.ObjectID)
                        .pipe(takeUntil(this.destroySubject$))
                        .subscribe(
                            r => {
                                this.sourceFields = <any>r;
                                this.sourceFields.push({ID: 0, FriendlyName: 'Name'});
                                this.sourceFields.push({ID: -2, FriendlyName: 'ParentID'});
                                this.validate();
                            }
                        )
                    ;
                }
            )
        ;
    }

    changeFindSearchType(search) {
        //Clear out values
        delete this.settings.Object;
        delete this.settings.ObjectID;
        delete this.settings.FilterField;
        delete this.settings.TargetField;

        if (search != 'ResultFromStep') {
            delete this.settings.FindParent;
        }

        switch (search) {
            case 'Glossary':
                this.validate();
                break;
            case 'ResultFromStep':
                this.steps = [];

                this.fusionService
                    .getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .pipe(takeUntil(this.destroySubject$))
                    .subscribe(
                        r => {
                            this.steps = <any>r;
                            this.validate();
                        }
                    );
                break;
            case 'FusionOwner':
                this.loadFusionOwners();
                break;
            case 'Fusion':
                this.objects = [];

                this.fusionService
                    .getFindFusionAttributeTypes()
                    .pipe(takeUntil(this.destroySubject$))
                    .subscribe(
                        r => {
                            this.objects = <any>r;
                            this.validate();
                        }
                    );
                break;
            case 'Promotion':
                this.objects = [];

                this.fusionService
                    .getFindAttributeTypes()
                    .pipe(takeUntil(this.destroySubject$))
                    .subscribe(
                        r => {
                            this.objects = <any>r;
                            this.validate();
                        }
                    );
                break;
            default:
                this.validate();
                break;
        }
    }

    loadFusionOwners(): Promise<any> {
        return this.fusionService.getPromotionFusionOwnerRules(this.fusionID)
            .then(r => {
                this.owners = r;
                this.validate();
            });
    }

    changeGlossaryType(fromControl: boolean): Promise<any> {
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
            return this.fusionService.getFindModels()
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

    changeGlossaryTypeFields(fromControl: boolean): Promise<any> {

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
            if (StringHelpers.isNullOrEmpty(this.settings.FilterField) || StringHelpers.isNullOrEmpty(this.settings.ObjectID))
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'FusionOwner') {
            if (StringHelpers.isNullOrEmpty(this.settings.ObjectID))
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'Glossary') {
            if (StringHelpers.isNullOrEmpty(this.settings.FilterField) || StringHelpers.isNullOrEmpty(this.settings.Object) || StringHelpers.isNullOrEmpty(this.settings.ObjectID) || StringHelpers.isNullOrEmpty(this.settings.TargetField))
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'Promotion') {
            if (StringHelpers.isNullOrEmpty(this.settings.FilterField) || StringHelpers.isNullOrEmpty(this.settings.ObjectID))
                this.isValid = false;
        } else if (this.settings.ObjectSearch == 'ResultFromStep') {
            if (StringHelpers.isNullOrEmpty(this.settings.ObjectID))
                this.isValid = false;
        }

        this.isValidChange.emit(this.isValid);
    }

    changeFindParent(e: boolean) {
        this.settings.FindParent = e ? 1 : 0;
    }

};

