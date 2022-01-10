import { Input, Component, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { RelationshipsService } from '../../../services/relationships.service';
import {
    PredicateDropdown,
    RelationshipType,
    Cardinality,
    RelationshipTypeEdge
} from '../../../models/relationship.model';
import { forkJoin } from 'rxjs';
import { Predicate } from '../../../models/predicate.model';

@Component({
    selector: 'd3s-admin-relationships-editor',
    templateUrl: './admin-relationships-editor.component.html',
    providers: [RelationshipsService],

})

export class AdminRelationshipsEditor {
    @Input() relationshipID: number = 0;
    @Input() relationshipType: RelationshipType;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    cardinalityOptions: SelectItem[] = [];
    subjectCardinalityOptions: SelectItem[] = [];
    objectCardinalityOptions: SelectItem[] = [];
    subjectOptions: SelectItem[] = [];
    objectOptions: SelectItem[] = [];
    predicates: PredicateDropdown[] = [];
    isLoading: boolean = false;
    isLoadingObject: boolean = false;
    isLoadingItem: boolean = false;
    isLoadingCardinality: boolean = false;
    canChangePredicate: boolean = true;
    canChangeCardinality: boolean = true;
    canChangeObject: boolean = true;
    selectedPredicate: any;
    limitedChangesOnly: boolean = false;


    constructor(private relationshipsService: RelationshipsService, private cdRef: ChangeDetectorRef) { }

    ngOnInit() {
        this.loadSubjectOptions();
        this.loadCardinalityOptions();
        if (this.relationshipID > 0 || this.relationshipType != null) {
            //copy model to avoid storing changes to properties after closing the editor
            this.relationshipType = JSON.parse(JSON.stringify(this.relationshipType));
            this.loadItem(this.relationshipID);
        }
        else {
            this.relationshipType = new RelationshipType();
            this.relationshipType.Subject = new RelationshipTypeEdge();
            this.relationshipType.Object = new RelationshipTypeEdge();
            this.relationshipType.Predicate = new Predicate();
            this.action = 'New';
        }
    }

    private loadItem(id: number) {
        this.isLoadingItem = true;

        if (this.relationshipType && this.relationshipType.Subject) {
            forkJoin(
                this.relationshipsService.getIntersectTypeById(this.relationshipType.Id),
                this.relationshipsService.getRelationshipUids(this.relationshipType.Uid)
            )
                .subscribe((results) => {
                    let relationshipType = results[0];
                    let relationships = results[1];

                    if (relationshipType) {
                        relationshipType = relationshipType[0];

                        this.loadObjectOptions(this.relationshipType.Subject.Uid, this.relationshipType.Object.Uid, this.relationshipType.Predicate.Uid);
                        this.loadPredicates(this.relationshipType.Subject.Uid, this.relationshipType.Object.Uid, this.relationshipType.Predicate.Uid);

                        if (relationshipType.PredicateID >= 3 && relationshipType.PredicateID <= 4) {

                            if (this.relationshipType.Subject.Cardinality === Cardinality[Cardinality.One]
                                && this.relationshipType.Object.Cardinality === Cardinality[Cardinality.Many]) {
                                this.canChangeCardinality = false;
                            }
                        }

                        let hasRelationships = (relationships != null && relationships.Results != null && relationships.Results.length > 0) ? true : false;
                        if (hasRelationships) {
                            this.limitedChangesOnly = true;
                        }

                        if (this.relationshipType.Predicate.Uid != undefined && hasRelationships) {
                            this.canChangePredicate = false;
                        }
                    }
                    this.isLoadingItem = false;
                });

        } else {
            this.isLoadingItem = false;
        }
    }

    private subjectChanged(value) {
        if (!value) return;

        this.relationshipType.Object.Uid = null;
        this.relationshipType.Predicate.Uid = null;

        this.loadPredicates(value);
    }

    private predicateChanged(value) {
        if (!value) return;
        let predicate = this.predicates.find(p => p.value == value);
        this.selectedPredicate = predicate;
        this.loadCardinalityOptions();

        if (predicate != null && predicate.isSemantic == true) {
            this.canChangeObject = false;
            this.objectOptions = this.subjectOptions.slice();
            this.relationshipType.Object.Uid = this.relationshipType.Subject.Uid;
        }
        else {
            this.canChangeObject = true;
        }

        if (!this.limitedChangesOnly && this.canChangeObject) {
            this.relationshipType.Object.Uid = null;
            this.loadObjectOptions(this.relationshipType.Subject.Uid, null, value);
        }
    }


    private loadPredicates(subjectUid: string, objectUid?: string, predicateUid?: string) {
        this.relationshipsService
            .getRelationshipPredicates(subjectUid, objectUid, predicateUid)
            .subscribe((result) => {
                this.predicates = [];
                for (let item of result) {
                    this.predicates.push({
                        label: item.label,
                        value: item.value,
                        isSemantic: item.isSemantic,
                        type: item.type
                    });
                }

                this.selectedPredicate = this.predicates.find((p) => p.value === this.relationshipType.Predicate.Uid);
                this.loadCardinalityOptions();
            });
    }

    private loadSubjectOptions() {
        this.isLoading = true;
        this.relationshipsService
            .getSubjectOptions()
            .subscribe((result) => {
            this.subjectOptions = [];
            for (let item of result) {
                this.subjectOptions.push({
                    value: item.value,
                    label: item.title
                });
            }
            this.isLoading = false;
        });
    }

    private loadObjectOptions(subjectUid: string, objectUid?: string, predicateUid?: string) {
        this.isLoadingObject = true;
        this.relationshipsService
            .getObjectOptions(subjectUid, objectUid, predicateUid)
            .subscribe((result) => {
            this.objectOptions = [];
            for (let item of result) {
                this.objectOptions.push({
                    value: item.value,
                    label: item.title
                });
            }
            this.isLoadingObject = false;
        });
    }

    private loadCardinalityOptions() {
        this.isLoadingCardinality = true;
        this.relationshipsService
            .getCardinalityOptions()
            .subscribe((result) => {
            this.cardinalityOptions = [];
            for (let item of result) {
                this.cardinalityOptions.push({
                    value: item.value.toString(),
                    label: item.title
                });
            }
            this.subjectCardinalityOptions = JSON.parse(JSON.stringify(this.cardinalityOptions));
            this.objectCardinalityOptions = JSON.parse(JSON.stringify(this.cardinalityOptions));

            if (this.selectedPredicate && this.selectedPredicate.type === 'DiagramReference') {
                this.objectCardinalityOptions = JSON.parse(JSON.stringify(this.objectCardinalityOptions.filter((x) => x.label !== Cardinality[Cardinality.Many])));

            }

            if (this.selectedPredicate && this.selectedPredicate.type === 'Diagram') {
                this.subjectCardinalityOptions = JSON.parse(JSON.stringify(this.subjectCardinalityOptions.filter((x) => x.label !== Cardinality[Cardinality.One])));
                this.objectCardinalityOptions = JSON.parse(JSON.stringify(this.objectCardinalityOptions.filter((x) => x.label !== Cardinality[Cardinality.One])));

            }
            this.isLoadingCardinality = false;
        });
    }

    onSubmit() {
        //save the item back to the save or edit url        
        this.saveClick.emit({ relationship: this.relationshipType, action: this.relationshipType.Uid == null ? "new" : "edit" });
    }
}