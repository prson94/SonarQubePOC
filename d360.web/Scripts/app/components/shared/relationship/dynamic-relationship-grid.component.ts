import {Component, Input, Output, OnChanges, SimpleChange, EventEmitter, ViewChild} from '@angular/core';
import {Router} from '@angular/router';
import {Lookup, LookupItem} from '../../../models/lookup.model';
import {GridDefinition, GridColumn, GridField} from '../../../models/grid-definition.model';
import {GridDefinitionService} from '../../../services/grid-definition.service';
import {RelationshipsService} from '../../../services/relationships.service';
import {BaseComponent} from '../base.component';
import {SiteUrlHelpers} from '../../../static/site-url-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';

declare var CompanySettings;

@Component({
    selector: 'd3s-dynamic-relationship-grid',
    providers: [GridDefinitionService, RelationshipsService],
    templateUrl: './dynamic-relationship-grid.component.html'
})

export class DynamicRelationshipGridComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectUid: number;
    @Input() objectName: string;
    @Input() targetType: string;
    @Input() targetTypeID: number;
    @Input() targetName: string;
    @Input() intersectTypeID: number;
    @Input() addRelationship: boolean;
    @Input() hasEdit: boolean = true;
    @Input() hasDelete: boolean = true;
    @Input() readOnly: boolean = false;

    @Output() readOnlyChange = new EventEmitter();
    @Output() addRelationshipChange = new EventEmitter();
    @Output() relationshipAdded = new EventEmitter();
    @Output() relationshipRemoved = new EventEmitter();
    @Output() deleteOn = new EventEmitter();
    @Output() deleteOff = new EventEmitter();
    @Output() onFilterChange = new EventEmitter();

    @Input() simpleFilter: boolean;
    @Input() isSubject: boolean;

    private fields: GridField[] = [];

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    get taxonomyName() {
        return CompanySettings.ArtifactType_TaxonomyTypeID || '';
    }

    relations: any[] = [];
    columns: GridColumn[] = [];

    selected: any = null;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    private showTechnical: boolean = false;

    @ViewChild('dt') datatable;

    constructor(private router: Router, private gridDefinitionService: GridDefinitionService, protected relationshipsService: RelationshipsService, private messagesService: MessagesObservableService) {
        super();
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if ((changes['objectID'] || changes['objectType'] || changes['intersectTypeID'] || changes['targetTypeID']) && (this.objectID != null && this.objectType != null && this.targetType != null && this.targetTypeID != null && this.intersectTypeID != null)) {
            this.load();
            this.showTechnical = false;
        }
    }

    load() {
        this.getFieldsDefinition();
        this.getData();
    }

    getFieldsDefinition() {

        this.gridDefinitionService.getGridDefinition(this.intersectTypeID, 'IntersectType', this.targetTypeID, this.targetType).subscribe(
            result => {
                this.columns = result.Columns;
                this.fields = result.Fields;
                this.readOnly = result.IsReadOnly;
                this.readOnlyChange.emit(this.readOnly);
                if (result.Fields.findIndex(x => x.name == 'TaxonomyType') >= 0) {
                    this.columns.unshift({
                        text: this.taxonomyName,
                        cellsformat: '',
                        datafield: 'TaxonomyType',
                        type: 'string',
                        description: null,
                        columnWidth: null
                    });
                }
            }
        );
    }

    getData() {
        this.isLoading = true;
        this.relationshipsService.getObjectRelationships(this.objectType, this.objectID, this.targetType, this.targetTypeID, this.intersectTypeID)
            .subscribe(result => {
                this.relations = result;
                this.isLoading = false;
                if (this.relations.length > 0) this.selected = this.relations[0];
                this.relationshipAdded.emit({ count: result.length });
                if (this.shouldShowEditor()) this.closeEditor();
            });
    }

    private shouldShowEditor(): boolean {
        return (this.addRelationship || this.showEditor) && !this.showTechnical;
    }

    public export() {
        if (this.datatable)
            this.datatable.exportCSV();
    }

    closeEditor() {
        this.showEditor = false;
        if (this.addRelationship) {
            this.addRelationship = !this.addRelationship;
            this.addRelationshipChange.emit(this.addRelationship);
        }
    }

    saveRelationship(event) {
        let model: any[] = [];
        let fields: any = {};
        for (var prop in event.item) {
            if (prop != 'IntersectTypeID' && prop != 'Source' && prop != 'SourceID' && prop != 'Items' && prop != 'ID' && prop != 'Uid') {
                fields[prop] = event.item[prop];
            }
        }

        if (event.action == 'new') {
            var assets = event.item.Items.split(',');
            assets.forEach(a => {
                let newRel: any = {};
                if (this.isSubject)
                    newRel = { SubjectAssetUid: this.objectUid, ObjectAssetUid: a, Fields: fields };
                else
                    newRel = { ObjectAssetUid: this.objectUid, SubjectAssetUid: a, Fields: fields };

                model.push(newRel);
            });
        }
        else {
            let newRel: any = {};
            if (this.isSubject)
                newRel = { SubjectAssetUid: this.objectUid, ObjectAssetUid: this.selected.ObjectUid, Fields: fields };
            else
                newRel = { ObjectAssetUid: this.objectUid, SubjectAssetUid: this.selected.ObjectUid, Fields: fields };

            model.push(newRel);
        }

        if (event.item.id != undefined && event.item.id == 0) {
            let count = 1;
            if (event.values && event.values.Items) {
                count = event.values.Items.split(',').length;
            }
        }

        this.relationshipsService.saveRelationships(this.intersectTypeID, model)
            .subscribe(res => {
                this.getData();
                this.closeEditor();
                if (event.action == 'new') {
                    this.showMessageForResult(this.messagesService, res, 'Relationships succesfully added!');
                }
                else {
                    this.showMessageForResult(this.messagesService, res, 'Relationships succesfully updated!');
                }

            });
    }

    deleteItem(item) {
        let model: any[] = [];
        let deleteItem: any = {};

        deleteItem['Cascade'] = true;
        deleteItem['uid'] = item;
        model.push(deleteItem);

        this.relationshipsService.deleteRelationshipV2(this.intersectTypeID, model)
            .subscribe(res => {
                this.relations = this.relations.filter(x => x.Uid != item);
                this.relationshipRemoved.emit();
                this.deleteOff.emit();
                this.showDelete = false;
                this.showMessageForResult(this.messagesService, res, 'Relationship succesfully deleted!');

            });

    }

    doDelete() {
        this.deleteOn.emit();
        this.showDelete = true;
    }

    cancelDelete() {
        this.deleteOff.emit();
        this.showDelete = false;
    }

    selectObject(item) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(item.Object, item.ObjectID, item.TypeID));
    }

    onFilter(event: any) {

        let count = 0;
        let qstring: string = "";

        for (var key in event.filters) {
            var matchcondition: string = event.filters[key].matchMode == "startsWith" ? "STARTS_WITH" : event.filters[key].matchMode;
            qstring += `&filterdatafield${count}=${key}&filtercondition${count}=${matchcondition}&filtervalue${count}=${event.filters[key].value}`;
            count++;
        }
        qstring += '&filterscount=' + count;
        this.onFilterChange.emit(qstring);

    }

}
