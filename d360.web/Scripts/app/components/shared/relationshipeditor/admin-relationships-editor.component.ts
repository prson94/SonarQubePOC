import { Input, Component, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipDetail, PredicateDropdown } from '../../../models/relationship.model';

@Component({
    selector: 'd3s-admin-relationships-editor',
    templateUrl: './admin-relationships-editor.component.html',
    providers: [RelationshipsService],

})

export class AdminRelationshipsEditor {
    @Input() relationshipID: number = 0;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedRelationship: RelationshipDetail;
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

    constructor(private relationshipsService: RelationshipsService, private cdRef: ChangeDetectorRef) { }

    ngOnInit() {
        this.loadSubjectOptions();
        this.loadCardinalityOptions();
        if (this.relationshipID > 0) {
            this.loadItem(this.relationshipID);
        }
        else {
            this.editedRelationship = new RelationshipDetail();
            this.action = 'New';
        }
    }

    private loadItem(id: number) {
        this.isLoadingItem = true;
        this.relationshipsService.getRelation(id).subscribe(result => {
            this.editedRelationship = result;
            this.isLoadingItem = false;
            if (this.editedRelationship.Subject) {
                let subject = this.editedRelationship.Subject.split('|');
                let object = this.editedRelationship.Object.split('|');
                if (subject.length >= 2 && object.length >= 2) {
                    this.loadObjectOptions(subject[0], Number(subject[1]), object[0], Number(object[1]), this.editedRelationship.Predicate);
                    this.loadPredicates(subject[0], Number(subject[1]), object[0], Number(object[1]), this.editedRelationship.Predicate);

                    if (this.editedRelationship.Predicate != undefined && this.editedRelationship.LimitedChangesOnly) {
                        this.canChangePredicate = false;
                    }

                    if (this.editedRelationship.PredicateType >= 3 &&  this.editedRelationship.PredicateType  <=4)
                    {
                        var SubCardinality: number = Number(this.editedRelationship.SubjectCardinality);
                        var ObjCardinality: number = Number(this.editedRelationship.ObjectCardinality);
                        if (SubCardinality === 1 && ObjCardinality  === 2) {
                                this.canChangeCardinality = false;
                            }
                    }
                }
                else {
                    this.loadObjectOptions(subject[0], Number(subject[1]), null, null, this.editedRelationship.Predicate);
                    this.loadPredicates(subject[0], Number(subject[1]), null, null, this.editedRelationship.Predicate);
                }
            }
        });
    }

    private subjectChanged(value) {
        if (!value) return;
        let info = value.split('|');
        if (info.length < 2) return;

        this.editedRelationship.Object = null;
        this.editedRelationship.Predicate = null;
        this.loadPredicates(info[0], Number(info[1]));
    }

    private predicateChanged(value) {
        if (!value) return;
        let predicateId = Number(value);
        let predicate = this.predicates.find(p => p.value == value);
        this.selectedPredicate = predicate;
        this.loadCardinalityOptions();

        if (predicate != null && predicate.isSemantic == true) {
            this.canChangeObject = false;
            this.objectOptions = this.subjectOptions.slice();
            this.editedRelationship.Object = this.editedRelationship.Subject;
        }
        else {
            this.canChangeObject = true;
        }

        let subject = this.editedRelationship.Subject.split('|');
        if (!this.editedRelationship.LimitedChangesOnly && this.canChangeObject) {
            this.editedRelationship.Object = null;
            this.loadObjectOptions(subject[0], Number(subject[1]), null, null, predicateId);
        }

    }

    private loadPredicates(subject: string, subjectId: number, object?: string, objectId?: number, predicateId?: number) {
        this.relationshipsService.getRelationshipPredicates(subject, subjectId, object, objectId, predicateId)
            .subscribe(result => {
                this.predicates = [];
                for (let item of result) {
                    this.predicates.push({
                        label: item.label,
                        value: item.value,
                        isSemantic: item.isSemantic, 
                        type: item.type
                    });
                }

                this.selectedPredicate = this.predicates.find(p => +p.value == this.editedRelationship.Predicate);
                this.loadCardinalityOptions();
            });
    }

    private loadSubjectOptions() {
        this.isLoading = true;
        this.relationshipsService.getSubjectOptions().subscribe(result => {
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

    private loadObjectOptions(subject: string, subjectId: number, object?: string, objectId?: number, predicateId?: number) {
        this.isLoadingObject = true;
        this.relationshipsService.getObjectOptions(subjectId, subject, objectId, object, predicateId).subscribe((result) => {
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
        this.relationshipsService.getCardinalityOptions().subscribe((result) => {
            this.cardinalityOptions = [];
            for (let item of result) {
                this.cardinalityOptions.push({
                    value: item.value.toString(),
                    label: item.title
                });
            }
            this.subjectCardinalityOptions = JSON.parse(JSON.stringify(this.cardinalityOptions));
            this.objectCardinalityOptions = JSON.parse(JSON.stringify(this.cardinalityOptions));

            if (this.selectedPredicate && this.selectedPredicate.type == 'DiagramReference') {
                this.objectCardinalityOptions = JSON.parse(JSON.stringify(this.objectCardinalityOptions.filter(x => x.label != 'Many')));

            }

            if (this.selectedPredicate && this.selectedPredicate.type == 'Diagram') {
                this.subjectCardinalityOptions = JSON.parse(JSON.stringify(this.subjectCardinalityOptions.filter((x) => x.label != 'One')));
                this.objectCardinalityOptions = JSON.parse(JSON.stringify(this.objectCardinalityOptions.filter(x => x.label != 'One')));

            }
            this.isLoadingCardinality = false;
        });
    }

    onSubmit() {
        //save the item back to the save or edit url        
        this.saveClick.emit({ relationship: this.editedRelationship, action: this.relationshipID > 0 ? "new" : "edit" });
    }
}