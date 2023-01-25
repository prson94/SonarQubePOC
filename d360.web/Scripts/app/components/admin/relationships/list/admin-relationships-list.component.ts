import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { PredicateFriendlyType } from '../../../../models/predicate.model';
import { RelationshipType, RelationshipTypeSimpleUIModel } from '../../../../models/relationship.model';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { RelationshipsService } from '../../../../services/relationships.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { BaseComponent } from '../../../shared/base.component';


@Component({
	selector: 'd3s-admin-relationships-list',
	providers: [RelationshipsService],
	templateUrl: 'admin-relationships-list.component.html'
})

export class AdminRelationshipsListComponent extends BaseComponent implements OnChanges {
	relationships: RelationshipTypeSimpleUIModel[] = [];

	private id: number = null;
	private subject: string = "";
	private predicate: string = "";

	@Input() filterToName: string = "";

	@Input() object: string;
	@Input() objectType: string;
	@Input() objectID: number;

	@Input() showTitle = true;

	@Input() selected: RelationshipTypeSimpleUIModel;
	@Output() selectedChange = new EventEmitter();

	showEditor: boolean = false;
	showDelete: boolean = false;
	gridStorageKey: string = "admin-relationships-grid";
	simpleFilterValue: string = "";
	constructor(
		private messagesService: MessagesObservableService,
		private relationshipsService: RelationshipsService,
		protected settingsService: CompanySettingsService,
		private cdRef: ChangeDetectorRef,
		private router: Router
	) {
		super(settingsService);
		this.filterToName = '';
	}

	ngOnInit() {
		this.getRelationships();
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if ((changes['filterToName'] && changes['filterToName'].currentValue !== changes['filterToName'].previousValue) || (changes['objectID'] && changes['objectID'].currentValue !== changes['objectID'].previousValue)) {
			this.getRelationships();
		}
	}

	private updateStorageKey() {
		if (this.objectType && this.objectID) {
			this.gridStorageKey = `admin-relationships-grid_${this.objectType}_${this.objectID}`;
		}
	}

	export() {
		this.relationshipsService.exportRelationshipTypes(this.filterToName ?? "", this.id ?? null, this.subject ?? "", this.predicate ?? "", this.object ?? "");
	}

	getFriendlyNameForFunctionalType(type: string): string {
		let friendly: string = type;

		friendly = PredicateFriendlyType[type];

		return friendly;
	}

	getRelationships() {
		this.updateStorageKey();
		this.isLoading = true;
		let obs = this.relationshipsService.getRelationshipTypes(null, true)

		if (this.objectID && this.objectType) {
			obs = this.relationshipsService.getRelationshipTypesById(this.objectID, this.objectType)
		}

		obs.subscribe((result) => {
			this.relationships = [];
			this.relationships = result.map((rel) => RelationshipType.ConvertToUIModeldata(rel));

			this.relationships =
				this.relationships.sort((a, b) => a.RelationshipTypeName > b.RelationshipTypeName ? 1 : -1);

			this.relationships.forEach((rel) => {
				const menuItems = [];
				menuItems.push({ "title": $localize`View Information`, callback: () => { this.selected = rel; } });
				menuItems.push({ "title": $localize`Open`, callback: () => this.open(rel.Uid) });
				menuItems.push({ "title": $localize`Open In A New Tab`, callback: () => this.open(rel.Uid, true) });

				menuItems.push({ "title": $localize`Edit`, callback: () => this.edit(rel) });
				menuItems.push({ "title": $localize`Delete`, callback: () => { this.showDelete = true; } });
				menuItems.push({ "title": $localize`Export`, callback: () => { this.downloadRel(rel) } });
				rel.MenuItems = menuItems;
			});

			this.isLoading = false;
			if (this.relationships && !this.showEditor) {
				if (this.relationships.length > 0) {
					this.selected = this.relationships[0];
					this.selectedChange.emit(this.selected);
				}
			}
			this.checkGridState();
		});
	}

	private checkGridState() {
		if (sessionStorage.getItem(this.gridStorageKey)) {
			const gridData = JSON.parse(sessionStorage.getItem(this.gridStorageKey));

			if (gridData.filters && Object.keys(gridData.filters).filter((x) => x !== "global").length > 0) { this.showSimpleFilter = false; }

			this.cdRef.detectChanges();
		}
	}

	findRelationshipIndex(uid: string) {
		var index: number = -1;
		for (var relationship of this.relationships) {
			index++;
			if (relationship.Uid === uid) { return index; }
		}
	}

	deleteRelationship($event) {
		this.showMessageForApiResult(this.messagesService, $event);
		this.showDelete = false;
		if ($event.Success === true) {
			this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
			this.relationships.splice(this.findRelationshipIndex($event.uid), 1);
		}
	}

	saveRelationship(event) {
		this.relationshipsService.saveRelationshipType(event.relationship)
			.subscribe((result) => {

				result = result[0];
				this.showMessageForApiResult(this.messagesService, result);

				if (result.Success === true) {
					this.getRelationships();
					this.showEditor = false;
				}
			});
	}

	closeEditor() {
		this.showEditor = false;
		if (this.selected == null) {
			this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
		}
	}

	add() {
		this.showEditor = true;
		this.selected = null;
	}

	edit(rel: RelationshipTypeSimpleUIModel) {
		this.showEditor = true;
		this.selected = rel;
	}

	public downloadRel(relationship: RelationshipTypeSimpleUIModel) {
		this.relationshipsService.exportRelationshipTypeItems(relationship);
	}

	get deletePromptText(): string {
		return $localize`Are you sure you want to delete the relationship[${this.selected?.RelationshipTypeName}]?`;
	}

	open(uid: string, newTab: boolean = false) {
		const url = `/admin/relationships/${uid}/fields`;
		if (newTab) {
			window.open(url, "_blank");
		}
		else {
			this.router.navigateByUrl(url);
		}
	}
}
