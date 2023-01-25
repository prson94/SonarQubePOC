import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SelectItem } from 'primeng/api';
import { forkJoin } from 'rxjs';
import { Predicate } from '../../../../models/predicate.model';
import { Cardinality, PredicateDropdown, RelationshipType, RelationshipTypeEdge } from '../../../../models/relationship.model';
import { RelationshipsService } from '../../../../services/relationships.service';

@Component({
	selector: 'd3s-admin-relationships-editor',
	templateUrl: './admin-relationships-editor.component.html',
	providers: [RelationshipsService],

})

export class AdminRelationshipsEditor implements OnChanges {
	@Input() relationshipID: number = 0;
	@Input() relationshipTypeUid: string;
	@Input() isModalVisible: false;

	@Output() closeClick = new EventEmitter();
	@Output() saveClick = new EventEmitter();

	relationshipType: RelationshipType;
	title: string = $localize`Edit`;

	saveLabel: string;

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
	isLoadingPredicate: boolean = false;
	isLoadingCardinality: boolean = false;
	canChangePredicate: boolean = true;
	canChangeCardinality: boolean = true;
	canChangeObject: boolean = true;
	selectedPredicate: any;
	limitedChangesOnly: boolean = false;

	relationshipTypeForm: FormGroup = null;
	@ViewChild('form', { static: false }) formElement: ElementRef;

	constructor(
		private fb: FormBuilder,
		private relationshipsService: RelationshipsService,
		private cdRef: ChangeDetectorRef) {
		this.relationshipType = new RelationshipType();
		this.relationshipType.Subject = new RelationshipTypeEdge();
		this.relationshipType.Object = new RelationshipTypeEdge();
		this.relationshipType.Predicate = new Predicate();
		this.title = $localize`Add Relationship Type`;

		this.relationshipTypeForm = this.fb.group({
			subject: [null, { validators: [Validators.required], updateOn: "blur" }],
			object: [null, { validators: [Validators.required], updateOn: "blur" }],
			predicate: [null, { validators: [Validators.required], updateOn: "blur" }],
			subjectCardinality: [null, { validators: [Validators.required], updateOn: "blur" }],
			objectCardinality: [null, { validators: [Validators.required], updateOn: "blur" }]
		});
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes && changes.relationshipTypeUid && changes.relationshipTypeUid.currentValue !== changes.relationshipTypeUid.previousValue) {
			this.loadForm();
		}

	}

	loadForm() {
		this.loadSubjectOptions();
		this.loadCardinalityOptions();
		if (this.relationshipTypeUid) {
			this.loadItem(this.relationshipTypeUid);
		}
		else {
			this.relationshipType = new RelationshipType();
			this.relationshipType.Subject = new RelationshipTypeEdge();
			this.relationshipType.Object = new RelationshipTypeEdge();
			this.relationshipType.Predicate = new Predicate();
			this.title = $localize`Add Relationship Type`;
			this.saveLabel = this.title;
		}
	}

	private loadItem(uid: string) {
		this.isLoadingItem = true;

		if (this.relationshipType && this.relationshipType.Subject) {
			this.relationshipsService.getRelationshipType(uid)
				.subscribe((results) => {
					this.relationshipType = results[0];

					if (this.relationshipType) {
						this.loadObjectOptions(this.relationshipType.Subject.Uid, this.relationshipType.Object.Uid, this.relationshipType.Predicate.Uid);
						this.loadPredicates(this.relationshipType.Subject.Uid, this.relationshipType.Object.Uid, this.relationshipType.Predicate.Uid, true);

						if (this.relationshipType.Predicate.Type === "InterTypeHierarchy" || this.relationshipType.Predicate.Type === "IntraTypeHierarchy") {

							if (this.relationshipType.Subject.Cardinality === Cardinality[Cardinality.One]
								&& this.relationshipType.Object.Cardinality === Cardinality[Cardinality.Many]) {
								this.canChangeCardinality = false;
							}
						}

						if (this.relationshipType.HasRelationships) {
							this.limitedChangesOnly = true;
						}

						if (this.relationshipType.Predicate.Uid != null && this.relationshipType.HasRelationships) {
							this.canChangePredicate = false;
						}
					}
					this.isLoadingItem = false;
				});

		} else {
			this.isLoadingItem = false;
		}
	}

	subjectChanged(value) {
		if (!value) { return; }

		this.relationshipType.Object.Uid = null;
		this.relationshipType.Predicate.Uid = null;

		this.loadPredicates(value);
	}

	predicateChanged(value) {
		if (!value) { return; }
		const predicate = this.predicates.find((p) => p.value === value);
		this.selectedPredicate = predicate;
		this.loadCardinalityOptions();

		if (predicate != null && predicate.isSemantic === true) {
			this.canChangeObject = false;
			this.objectOptions = this.subjectOptions.slice();
			this.cdRef.detectChanges();
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


	private loadPredicates(subjectUid: string, objectUid?: string, predicateUid?: string, Loadpredicate?: boolean) {
		if (Loadpredicate) {
			this.isLoadingPredicate = Loadpredicate;
		}
		this.relationshipsService
			.getRelationshipPredicates(subjectUid, objectUid, predicateUid)
			.subscribe((result) => {
				this.predicates = [];
				for (const item of result) {
					this.predicates.push({
						label: item.label,
						value: item.value,
						isSemantic: item.isSemantic,
						type: item.type
					});
				}

				this.selectedPredicate = this.predicates.find((p) => p.value === this.relationshipType.Predicate.Uid);
				this.loadCardinalityOptions();
				this.isLoadingPredicate = false;
			});
	}

	private loadSubjectOptions() {
		this.isLoading = true;
		this.relationshipsService
			.getSubjectOptions()
			.subscribe((result) => {
				this.subjectOptions = [];
				for (const item of result) {
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
				for (const item of result) {
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
				for (const item of result) {
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