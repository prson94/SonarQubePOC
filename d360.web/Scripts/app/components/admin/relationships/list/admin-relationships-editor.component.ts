import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SelectItem } from 'primeng/api';
import { Subscription } from 'rxjs';
import { Predicate } from '../../../../models/predicate.model';
import { Cardinality, PredicateDropdown, RelationshipType, RelationshipTypeEdge } from '../../../../models/relationship.model';
import { RelationshipsService } from '../../../../services/relationships.service';

/*global $localize*/

@Component({
	selector: 'd3s-admin-relationships-editor',
	templateUrl: './admin-relationships-editor.component.html',
	styles: [`
	.form-wrapper { 
		padding-top:16px; 
	} 

	.form-editor-container {     
		max-height: calc(100vh - 147px);
		overflow: auto;
		margin-bottom: 16px;
	}`],
	providers: [RelationshipsService],

})

export class AdminRelationshipsEditor implements OnChanges, OnInit {
	@Input() relationshipID: number = 0;
	@Input() relationshipTypeUid: string;
	@Input() isModalVisible: false;

	@Output() closeClick = new EventEmitter();
	@Output() onSave = new EventEmitter();

	relationshipType: RelationshipType;
	title: string = $localize`Edit`;

	saveLabel: string;
	cancelLabel: string;

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

	selectedPredicate: PredicateDropdown;

	isFormDisabled: boolean = false;
	isFormSet: boolean = false;
	hasChanges: boolean = false;

	isSaving: boolean = false;
	formSub: Subscription;

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
		this.isFormDisabled = false;
		this.isFormSet = false;
		this.hasChanges = false;

		if (this.formSub) {
			this.formSub.unsubscribe();
		}

		if (this.relationshipTypeUid) {
			this.saveLabel = $localize`Save Changes`;
			this.cancelLabel = $localize`Close`;
			this.title = $localize`Edit Relationship Type`;
			await this.loadItem(this.relationshipTypeUid);
		}
		else {
			this.relationshipType = new RelationshipType();
			this.relationshipType.Subject = new RelationshipTypeEdge();
			this.relationshipType.Object = new RelationshipTypeEdge();
			this.relationshipType.Predicate = new Predicate();
			this.title = $localize`Add Relationship Type`;
			this.saveLabel = this.title;
			this.cancelLabel = $localize`Cancel`;
			this.isFormSet = true;
		}
	}

	private async loadItem(uid: string) {
		this.isLoadingItem = true;
		if (this.relationshipType && this.relationshipType.Subject) {
			const results = await this.relationshipsService.getRelationshipType(uid).toPromise();
			const typeToLoad = results[0];

			if (typeToLoad) {
				await this.loadObjectOptionsAsync(typeToLoad.Subject.Uid, typeToLoad.Object.Uid, typeToLoad.Predicate.Uid);
				await this.loadPredicatesAsync(typeToLoad.Subject.Uid, typeToLoad.Object.Uid, typeToLoad.Predicate.Uid, true, typeToLoad.Predicate);
				this.relationshipType = typeToLoad;

				if (this.relationshipType.Predicate.Type === "InterTypeHierarchy" || this.relationshipType.Predicate.Type === "IntraTypeHierarchy") {

					if (this.relationshipType.Subject.Cardinality === Cardinality[Cardinality.One]
						&& this.relationshipType.Object.Cardinality === Cardinality[Cardinality.Many]) {
						this.isFormDisabled = true;
					}
				}

				if (this.relationshipType.HasRelationships) {
					this.isFormDisabled = true;
				}

				setTimeout(() => {
					this.isFormSet = true;
					this.hasChanges = false;
					this.formSub = this.relationshipTypeForm.valueChanges.subscribe(() => {
						this.hasChanges = true;
						this.cancelLabel = $localize`Discard Changes`;
					});
				}, 200);
			}
			this.isLoadingItem = false;

		} else {
			this.isLoadingItem = false;
		}
	}

	async subjectChanged($event) {
		if (!this.isFormSet || !$event) {
			return;
		}

		this.relationshipType.Object.Uid = null;
		this.relationshipType.Predicate.Uid = null;

		await this.loadPredicatesAsync($event);
	}

	async predicateChanged(value) {
		if (!this.isFormSet || !value) {
			return;
		}

		const predicate = this.predicates.find((p) => p.value === value);
		this.selectedPredicate = predicate;

		if (predicate != null && predicate.isSemantic === true) {
			this.objectOptions = this.subjectOptions.slice();
			this.cdRef.detectChanges();
			this.relationshipType.Object.Uid = this.relationshipType.Subject.Uid;
		}

		this.relationshipType.Object.Uid = null;
		await this.loadObjectOptionsAsync(this.relationshipType.Subject.Uid, null, value);
	}


	private async loadPredicatesAsync(subjectUid: string, objectUid?: string, predicateUid?: string, Loadpredicate?: boolean, predicate?: Predicate) {
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
		if (predicate && !this.selectedPredicate) {
			this.predicates.push({
				label: predicate.Name + " / " + predicate.Inverse,
				value: predicate.Uid,
				isSemantic: false,
				type: predicate.Type
			});

			this.selectedPredicate = this.predicates.find((p) => p.value === this.relationshipType.Predicate.Uid);
		}

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
		this.isSaving = true;
		this.relationshipsService.saveRelationshipType(this.relationshipType)
			.subscribe((result) => {
				this.isSaving = false;
				this.onSave.emit(result);
				this.cancel();
			});
	}

	get isSubmitDisabled(): boolean {
		return !this.relationshipTypeForm.valid || (this.relationshipType.Uid && !this.hasChanges);
	}

	async cancel() {
		await this.loadForm();
		this.closeClick.emit();
	}
}