import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChange } from '@angular/core';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipType, RelationshipTypeSimpleUIModel } from '../../../models/relationship.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { PredicateFriendlyType } from '../../../models/predicate.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { Observable } from 'rxjs';

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
    theDeleteCallback: Function;
    private gridStorageKey: string = "admin-relationships-grid";

    constructor(
        private messagesService: MessagesObservableService,
        private relationshipsService: RelationshipsService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(settingsService);
        this.filterToName = '';
        this.theDeleteCallback = this.deleteRelationship.bind(this);
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

    private export() {
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
		let obs = this.relationshipsService.getRelationshipTypes();

		if (this.objectID && this.objectType) {
			obs = this.relationshipsService.getRelationshipTypesById(this.objectID, this.objectType)
		}

		obs.subscribe((result) => {
			this.relationships = [];

			(result ?? []).forEach((rel) => {
				this.relationships.push({
					RelationshipTypeName: rel.Object.Name + " - " + rel.Predicate.Name + " - " + rel.Subject.Name,
					Uid: rel.Uid,
					HasRelationships: rel.HasRelationships
				})
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

            if (gridData.filters && Object.keys(gridData.filters).filter((x) => x !== "global").length > 0)
                {this.showSimpleFilter = false;}

            this.cdRef.detectChanges();
        }
    }

    findRelationshipIndex(uid: string) {
        var index: number = -1;
        for (var relationship of this.relationships) {
            index++;
            if (relationship.Uid === uid) {return index;}
        }
    }

    deleteRelationship(uid: string) {
        this.relationshipsService.deleteRelationshipType(uid)
            .subscribe((result) => {
                result = result[0];
                this.showMessageForApiResult(this.messagesService, result);
                this.showDelete = false;
                if (result.Success === true) {
                    this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
                    this.relationships.splice(this.findRelationshipIndex(uid), 1);
                }
            });
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

    public downloadRel(relationship: RelationshipType) {
        this.relationshipsService.exportRelationshipTypeItems(relationship);
    }

	get deletePromptText(): string {
		return $localize`Are you sure you want to delete the relationship[${this.selected?.RelationshipTypeName}]?`;
    }
}
