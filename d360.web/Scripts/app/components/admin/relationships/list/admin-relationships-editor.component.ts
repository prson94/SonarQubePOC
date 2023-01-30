import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { result } from 'lodash';
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

export class AdminRelationshipsEditor implements OnChanges, OnInit {
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

	selectedPredicate: any;
	limitedChangesOnly: boolean = false;

	canUpdateSides: boolean = true;

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

	async ngOnInit() {
		await this.loadCardinalityOptionsAsync();
		await this.loadSubjectOptionsAsync();
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes && changes.relationshipTypeUid && changes.relationshipTypeUid.currentValue !== changes.relationshipTypeUid.previousValue) {
			this.loadForm();
		}
	}

	async loadForm() {
		this.canUpdateSides = true;

		if (this.relationshipTypeUid) {
			await this.loadItem(this.relationshipTypeUid);
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

	private async loadItem(uid: string) {
		this.isLoadingItem = true;
		if (this.relationshipType && this.relationshipType.Subject) {
			const results = await this.relationshipsService.getRelationshipType(uid).toPromise();
			const typeToLoad = results[0];

			if (typeToLoad) {
				await this.loadObjectOptionsAsync(typeToLoad.Subject.Uid, typeToLoad.Object.Uid, typeToLoad.Predicate.Uid);
				await this.loadPredicatesAsync(typeToLoad.Subject.Uid, typeToLoad.Object.Uid, typeToLoad.Predicate.Uid, true);
				this.relationshipType = typeToLoad;

				if (this.relationshipType.Predicate.Type === "InterTypeHierarchy" || this.relationshipType.Predicate.Type === "IntraTypeHierarchy") {

					if (this.relationshipType.Subject.Cardinality === Cardinality[Cardinality.One]
						&& this.relationshipType.Object.Cardinality === Cardinality[Cardinality.Many]) {
						this.canUpdateSides = false;
					}
				}

				if (this.relationshipType.HasRelationships) {
					this.limitedChangesOnly = true;
				}

				if (this.relationshipType.Predicate.Uid != null && this.relationshipType.HasRelationships) {
					this.canUpdateSides = false;
				}
			}
			this.isLoadingItem = false;

		} else {
			this.isLoadingItem = false;
		}
	}

	async subjectChanged($event) {
		console.log($event);
		if (!$event) { return; }
		return;

		this.relationshipType.Object.Uid = null;
		this.relationshipType.Predicate.Uid = null;

		await this.loadPredicatesAsync($event);
	}

	async predicateChanged(value) {
		if (!value) { return; }
		const predicate = this.predicates.find((p) => p.value === value);
		this.selectedPredicate = predicate;

		if (predicate != null && predicate.isSemantic === true) {
			this.canUpdateSides = false;
			this.objectOptions = this.subjectOptions.slice();
			this.cdRef.detectChanges();
			this.relationshipType.Object.Uid = this.relationshipType.Subject.Uid;
		}
		else {
			this.canUpdateSides = true;
		}

		if (!this.limitedChangesOnly && this.canUpdateSides) {
			this.relationshipType.Object.Uid = null;
			await this.loadObjectOptionsAsync(this.relationshipType.Subject.Uid, null, value);
		}
	}


	private async loadPredicatesAsync(subjectUid: string, objectUid?: string, predicateUid?: string, Loadpredicate?: boolean) {
		if (Loadpredicate) {
			this.isLoadingPredicate = Loadpredicate;
		}
		const result = await this.relationshipsService
			.getRelationshipPredicates(subjectUid, objectUid, predicateUid)
			.toPromise();

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
		this.isLoadingPredicate = false;
	}

	private async loadSubjectOptionsAsync() {
		const result = await this.relationshipsService
			.getSubjectOptions()
			.toPromise();

		this.subjectOptions = [];
		for (const item of result) {
			this.subjectOptions.push({
				value: item.value,
				label: item.title
			});
		}
	}

	private async loadObjectOptionsAsync(subjectUid: string, objectUid?: string, predicateUid?: string) {
		const result = await this.relationshipsService
			.getObjectOptions(subjectUid, objectUid, predicateUid)
			.toPromise();

		this.objectOptions = [];
		for (const item of result) {
			this.objectOptions.push({
				value: item.value,
				label: item.title
			});
		}
	}

	private async loadCardinalityOptionsAsync() {
		this.isLoadingCardinality = true;
		const result = await this.relationshipsService.getCardinalityOptions().toPromise();

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
	}

	onSubmit() {
		//save the item back to the save or edit url        
		this.saveClick.emit({ relationship: this.relationshipType, action: this.relationshipType.Uid == null ? "new" : "edit" });
	}
}