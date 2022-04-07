import { Input, Component, Output, EventEmitter, OnChanges, SimpleChange, ChangeDetectorRef } from '@angular/core';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipType } from '../../../models/relationship.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { PredicateFriendlyType } from '../../../models/predicate.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-relationships-list',
    providers: [RelationshipsService],
    templateUrl: 'admin-relationships-list.component.html'
})

export class AdminRelationshipsListComponent extends BaseComponent implements OnChanges {
    relationships: RelationshipType[] = [];

    private id: number = null;
    private subject: string = "";
    private predicate: string = "";
    private object: string = "";

    @Input() filterToName: string = "";

    @Input() objectType: string;
    @Input() objectID: number;

    @Input() selected: RelationshipType;
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
        if ((changes['filterToName'] && changes['filterToName'].currentValue != changes['filterToName'].previousValue) || (changes['objectID'] && changes['objectID'].currentValue != changes['objectID'].previousValue)) {
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

    private filterResults() {
        if (this.filterToName && this.filterToName.length > 0) {
            var search = this.filterToName.toLowerCase();
            this.relationships = this.relationships.filter(item =>
                item.Predicate && item.Predicate.Name && item.Predicate.Name.toLowerCase().includes(search) ||
                item.Predicate && item.Predicate.Inverse && item.Predicate.Inverse.toLowerCase().includes(search) ||
                item.Object && item.Object.Class && item.Object.Class.toLowerCase().includes(search) ||
                item.Object && item.Object.Name && item.Object.Name.toLowerCase().includes(search) ||
                item.Subject && item.Subject.Class && item.Subject.Class.toLowerCase().includes(search) ||
                item.Subject && item.Subject.Name && item.Subject.Name.toLowerCase().includes(search)
            );
        }
    }

    getFriendlyNameForFunctionalType(type: string): string {
        let friendly: string = type;

        friendly = PredicateFriendlyType[type];

        return friendly;
    }

    getRelationships() {
        this.updateStorageKey();
        this.isLoading = true;
        if (this.objectID && this.objectType) {
            this.relationshipsService.getRelationshipTypesById(this.objectID, this.objectType)
                .subscribe(result => {
                    this.relationships = result ?? [];
                    this.isLoading = false;
                    if (this.relationships && !this.showEditor) {
                        if (this.relationships.length > 0) {
                            this.selected = this.relationships[0];
                            this.selectedChange.emit(this.selected)
                        }
                    }
                    this.checkGridState();
                });
        } else {
            this.relationshipsService.getRelationshipTypes()
                .subscribe(result => {
                    this.relationships = result ?? [];
                    this.filterResults();
                    this.isLoading = false;
                    if (this.relationships && !this.showEditor) {
                        if (this.relationships.length > 0) {
                            this.selected = this.relationships[0];
                            this.selectedChange.emit(this.selected)
                        }
                    }
                    this.checkGridState();
                });
        }
    }

    private checkGridState() {
        if (sessionStorage.getItem(this.gridStorageKey)) {
            let gridData = JSON.parse(sessionStorage.getItem(this.gridStorageKey));

            if (gridData.filters && Object.keys(gridData.filters).filter(x => x != "global").length > 0)
                this.showSimpleFilter = false;

            this.cdRef.detectChanges();
        }
    }

    findRelationshipIndex(uid: string) {
        var index: number = -1;
        for (var relationship of this.relationships) {
            index++;
            if (relationship.Uid == uid) return index;
        }
    }

    deleteRelationship(uid: string) {
        this.relationshipsService.deleteRelationshipType(uid)
            .subscribe(result => {
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

    displayTypeName(type: string) {
        if (!type) return "";
        return type.replace("Type", "");
    }

    onFilter($event) {
        this.id = $event.filters["Id"]?.value;
        this.subject = $event.filters["Subject.Name"]?.value;
        this.predicate = $event.filters["Predicate.Name"]?.value;
        this.object = $event.filters["Object.Name"]?.value;

        if ($event && $event.filteredValue) {
            this.selected = $event.filteredValue[0];
            this.filterToName = $event.filters?.global?.value;            

            if (this.selected) {
                this.selectedChange.emit(this.selected)
            }
        }
    }

    public downloadRel(relationship: RelationshipType) {
        this.relationshipsService.exportRelationshipTypeItems(relationship);
    }
}
