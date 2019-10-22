import { Input, Component, EventEmitter, Output } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipDetail } from '../../../models/relationship.model';
import { ViewEncapsulation } from '@angular/core';

@Component({
    selector: 'd3s-admin-relationships-editor',
    template: ` 
                <header>{{action}} Relationship Type</header>                
                <d3s-loading [isLoading]="isLoading || isLoadingItem"></d3s-loading>
                <div *ngIf="!isLoading && !isLoadingItem">
                    <div class="form-instructions">When creating a relationship type, Subject should always be the higher-level item in the relationship, while Object is the lower-level, or atomic, item in the relationship.  For example, when defining a relationships between Application and Business Term you would set Application as Subject and Business Term as Object.  This will impact how sourcing and synonym inheritance works, as Object is what you are sourcing as well as where synonyms defined on the relationship will also appear.</div>
                    <form (ngSubmit)="onSubmit()" #relationshipEditorForm="ngForm">

                        <div class="row">
                            <div class="col l4 m4 s12">
                                <div class="FieldName">Subject</div>
                                <p-dropdown filter="true" appendTo="body" name="subject" #subject="ngModel" [options]="subjectOptions" [(ngModel)]="editedRelationship.Subject" [disabled]="editedRelationship.LimitedChangesOnly" required (ngModelChange)="editedRelationship.Subject=$event;subjectChanged($event);" [style]="{ 'width': '100%' }"></p-dropdown>
                            </div>
                            <div class="col l4 m4 s12">
                                <div class="FieldName">Predicate</div>
                                <p-dropdown filter="true" appendTo="body" name="predicate" #predicate="ngModel" [options]="predicates" [(ngModel)]="editedRelationship.Predicate" [disabled]="!canChangePredicate" required (ngModelChange)="editedRelationship.Predicate=$event;predicateChanged($event);" [style]="{ 'width': '100%' }"></p-dropdown>
                            </div>
                            <div class="col l4 m4 s12">
                                <d3s-loading [isLoading]="isLoadingObject"></d3s-loading>
                                <div *ngIf="!isLoadingObject" class="FieldName">Object</div>
                                <p-dropdown *ngIf="!isLoadingObject" filter="true" appendTo="body" name="object" #object="ngModel" [options]="objectOptions" [(ngModel)]="editedRelationship.Object" [disabled]="editedRelationship.LimitedChangesOnly" required [style]="{ 'width': '100%' }"></p-dropdown>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col l4 m4 s12">
                                <div class="FieldName">Subject Cardinality</div>
                                <p-dropdown filter="true" name="subjectCardinality" #subjectCardinality="ngModel" [options]="cardinalityOptions" [(ngModel)]="editedRelationship.SubjectCardinality" required [style]="{ 'width': '100%' }"></p-dropdown>
                            </div>
                            <div class="col l4 m4 s12" style="text-align: center">&nbsp;<br/>to</div>
                            <div class="col l4 m4 s12">
                                <div class="FieldName">Object Cardinality</div>
                                <p-dropdown filter="true" name="objectCardinality" #objectCardinality="ngModel" [options]="cardinalityOptions" [(ngModel)]="editedRelationship.ObjectCardinality" required [style]="{ 'width': '100%' }"></p-dropdown>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col s12">&nbsp;</div>
                        </div>

                        <div class="row">
                            <div class="col s12">
                                <button pButton type="submit" [disabled]="!relationshipEditorForm.form.valid" style="width: '150px';" label="Save"></button>
                                <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                            </div>
                        </div>

                    </form>
                </div>
                `,

    // Having three dropdowns in compact space gives ugly and unusable dropdown panels...esp for long dropdown items
    // insert custom styling using Angular Default View Encapsulation
    // appendTo="body" was added to the dropdowns to address Edge issues leaving the panels show allover.
    // This fix should address the correct left position

    encapsulation: ViewEncapsulation.None,
    styles: [
        `
            .ui-dropdown-panel {
                max-width: 300px;
            }
        `
    ],

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
    subjectOptions: SelectItem[] = [];
    objectOptions: SelectItem[] = [];
    predicates: SelectItem[] = [];
    isLoading: boolean = false;
    isLoadingObject: boolean = false;
    isLoadingItem: boolean = false;
    canChangePredicate: boolean = true;

    constructor(private relationshipsService: RelationshipsService) { }

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

        let subject = this.editedRelationship.Subject.split('|');
        if (!this.editedRelationship.LimitedChangesOnly) {
            this.editedRelationship.Object = null;
            this.loadObjectOptions(subject[0], Number(subject[1]), null, null, predicateId);
        }
    }

    private loadPredicates(subject: string, subjectId: number, object?: string, objectId?: number, predicateId?: number) {
        this.relationshipsService.getRelationshipPredicates(subject, subjectId, object, objectId, predicateId)
            .subscribe(result => {
                this.predicates = [];
                this.predicates.push({ label: 'Select A Predicate', value: null });
                for (let item of result) {
                    this.predicates.push({
                        label: item.title,
                        value: item.value
                    });
                }                
            });
    }

    private loadSubjectOptions() {
        this.isLoading = true;
        this.relationshipsService.getSubjectOptions().subscribe(result => {            
            this.subjectOptions = [];
            this.subjectOptions.push({ label: 'Select Subject', value: null });
            for (let item of result) {
                this.subjectOptions.push({
                    value:item.value,
                    label:item.title
                });
            }
            this.isLoading = false;
        });
    }

    private loadObjectOptions(subject: string, subjectId: number, object?: string, objectId?: number, predicateId?: number) {
        this.isLoadingObject = true;
        this.relationshipsService.getObjectOptions(subjectId, subject, objectId, object, predicateId).subscribe(result => {
            this.objectOptions = [];   
            this.objectOptions.push({ label: 'Select Object', value: null });     
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
        this.isLoading = true;
        this.relationshipsService.getCardinalityOptions().subscribe(result => {
            this.cardinalityOptions = [];
            this.cardinalityOptions.push({ label: 'Select Cardinality', value: null });
            for (let item of result) {
                this.cardinalityOptions.push({
                    value: item.value.toString(),
                    label: item.title
                });
            }
            this.isLoading = false;
        });
    }

    onSubmit() {        
        //save the item back to the save or edit url        
        this.saveClick.emit({ relationship: this.editedRelationship, action: this.relationshipID > 0 ? "new" : "edit" });
    }
};