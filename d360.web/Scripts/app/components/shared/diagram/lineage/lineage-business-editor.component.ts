import * as _ from 'lodash';
import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';

import {
    AutoCompleteItem,
    LineageEditorMode,
    LineageEditorModel,
    LineageEditorRow,
    LineageView,
} from '../../../../models/lineage.model';

import {DiagramService} from '../../../../services/diagram.service';
import {MessagesService} from '../../../../services/messages.service';
import {PermissionsService} from '../../../../services/permissions.service';

import {BaseComponent} from '../../base.component';

@Component({
    selector: 'd3s-lineage-business-editor',
    templateUrl: './lineage-business-editor.component.html',
    styles: [
            `
            .lineage-editor-table > thead > tr > th {
                border-radius: 0 !important;
                padding: 5px;
            }

            .lineage-editor-table > tbody > tr > td {
                border-radius: 0 !important;
                padding: 3px;
            }

        `
    ],
    providers: [DiagramService]
})

export class LineageBusinessEditorComponent extends BaseComponent implements OnInit {
    @Input() object: string;
    @Input() objectId: number;
    @Output() onClose = new EventEmitter();
    @Output() onSaveComplete = new EventEmitter();
    @Output() onSaveSuccess = new EventEmitter();

    //permissions: ResponsibilityTypeRelationPermission[] = [];
    lineage: LineageEditorRow[] = [];
    model: LineageEditorModel;
    queryResults: AutoCompleteItem[] = [];
    sourceSubResults: AutoCompleteItem[] = [];
    sourceObjResults: AutoCompleteItem[] = [];
    targetSubResults: AutoCompleteItem[] = [];
    targetObjResults: AutoCompleteItem[] = [];

    valid = false;

    intersectTypes: AutoCompleteItem[] = [];

    isLoading = false;
    saveComplete = false;

    mode: LineageEditorMode = LineageEditorMode.Default;
    LineageEditorMode = LineageEditorMode;
    view: LineageView = LineageView.SystemFlow;

    constructor(
        private diagramService: DiagramService,
        protected messagesService: MessagesService,
        protected permissionsService: PermissionsService
    ) {
        super();
    }

    ngOnInit() {
        this.load();

        this.permissionsService.getPermissions(this.objectId, this.object).subscribe(
            data => {
                this.permissions = data;
            }
        );

        this.model = new LineageEditorModel();
        this.model.Focal = this.object;
        this.model.FocalID = this.objectId;
    }

    load() {
        this.isLoading = true;
        this.diagramService.getLineageDiagram(
            this.object,
            this.objectId,
            LineageView.MapItemList,
            false
        ).subscribe(
            r => {
                this.lineage = r.items;

                if (this.lineage != null && this.lineage.length > 0) {
                    this.lineage.forEach(i => {
                        this.initializeLineageRow(i);
                    });
                } else {
                    this.lineage = [];
                }

                this.isLoading = false;
            });
    }

    select(field: string, i: LineageEditorRow, e: any) {
        this.setObjectValue(i, i[field]);

        let data = i[field].data;

        switch (field) {
            case 'selectedSourceRelationshipType':
                i.SourceSubjectType = data.Subject;
                i.SourceSubjectTypeID = data.SubjectID;
                i.SourceObjectType = data.Object;
                i.SourceObjectTypeID = data.ObjectID;
                break;
            case 'selectedSourceSubject':
                i.SourceSubject = data.Object;
                break;
            case 'selectedSourceObject':
                i.SourceObject = data.Object;
                break;
            case 'selectedTargetRelationshipType':
                i.TargetSubjectType = data.Subject;
                i.TargetSubjectTypeID = data.SubjectID;
                i.TargetObjectType = data.Object;
                i.TargetObjectTypeID = data.ObjectID;
                break;
            case 'selectedTargetSubject':
                i.TargetSubject = data.Object;
                break;
            case 'selectedTargetObject':
                i.TargetObject = data.Object;
                break;
        }

        //update source and target keys
        if (i.SourceIntersectTypeID != null && i.SourceSubjectID != null && i.SourceObjectID != null) {
            i.sourcekey = `${i.SourceIntersectTypeID}.${i.SourceSubjectID}.${i.SourceObjectID}`;
        }

        if (i.TargetIntersectTypeID != null && i.TargetSubjectID != null && i.TargetObjectID != null) {
            i.targetkey = `${i.TargetIntersectTypeID}.${i.TargetSubjectID}.${i.TargetObjectID}`;
        }

        //update connection checks
        this.updateConnections()
    }

    query(field: string, i: LineageEditorRow, e: any) {
        switch (field) {
            case 'selectedSourceRelationshipType':
                this.diagramService.queryRelationshipTypes(e.query).subscribe(
                    r => {
                        this.queryResults = [];

                        r.forEach(i => {
                            let a = new AutoCompleteItem();
                            a.label = i.Name;
                            a.value = i.ID;
                            a.labelField = 'SourceIntersectTypeName';
                            a.valueField = 'SourceIntersectTypeID';
                            a.templateValue = this.formTemplateString(i.Name, e.query);

                            a.data = {
                                Subject: i.Subject,
                                SubjectID: i.SubjectID,
                                Object: i.Object,
                                ObjectID: i.ObjectID
                            };

                            this.queryResults.push(a);
                        });

                        if (this.queryResults.length == 1) {
                            this.setObjectValue(i, this.queryResults[0]);
                        }
                    });
                break;
            case 'selectedSourceSubject':
                this.diagramService.queryObjects(i.SourceSubjectType, i.SourceSubjectTypeID, e.query).subscribe(
                    r => {
                        this.sourceSubResults = [];

                        r.forEach(i => {
                            let a = new AutoCompleteItem();
                            a.label = i.TextPath;
                            a.value = i.ObjectID;
                            a.labelField = 'SourceSubjectName';
                            a.valueField = 'SourceSubjectID';
                            a.templateValue = this.formTemplateString(i.TextPath, e.query);

                            a.data = {
                                Object: i.Object
                            };

                            this.sourceSubResults.push(a);
                        });

                        if (this.sourceSubResults.length == 1) {
                            this.setObjectValue(i, this.sourceSubResults[0]);
                        }
                    });
                break;
            case 'selectedSourceObject':
                this.diagramService.queryObjects(i.SourceObjectType, i.SourceObjectTypeID, e.query).subscribe(
                    r => {
                        this.sourceObjResults = [];

                        r.forEach(i => {
                            let a = new AutoCompleteItem();
                            a.label = i.TextPath;
                            a.value = i.ObjectID;
                            a.labelField = 'SourceObjectName';
                            a.valueField = 'SourceObjectID';
                            a.templateValue = this.formTemplateString(i.TextPath, e.query);

                            a.data = {
                                Object: i.Object
                            };

                            this.sourceObjResults.push(a);
                        });

                        if (this.sourceObjResults.length == 1) {
                            this.setObjectValue(i, this.sourceObjResults[0]);
                        }
                    });
                break;
            case 'selectedTargetRelationshipType':
                this.diagramService.queryRelationshipTypes(e.query).subscribe(
                    r => {
                        this.queryResults = [];
                        r.forEach(i => {
                            let a = new AutoCompleteItem();
                            a.label = i.Name;
                            a.value = i.ID;
                            a.labelField = 'TargetIntersectTypeName';
                            a.valueField = 'TargetIntersectTypeID';
                            a.templateValue = this.formTemplateString(i.Name, e.query);

                            a.data = {
                                Subject: i.Subject,
                                SubjectID: i.SubjectID,
                                Object: i.Object,
                                ObjectID: i.ObjectID
                            };

                            this.queryResults.push(a);
                        });

                        if (this.queryResults.length == 1) {
                            this.setObjectValue(i, this.queryResults[0]);
                        }
                    });
                break;
            case 'selectedTargetSubject':
                this.diagramService.queryObjects(i.TargetSubjectType, i.TargetSubjectTypeID, e.query).subscribe(
                    r => {
                        this.targetSubResults = [];

                        r.forEach(i => {
                            let a = new AutoCompleteItem();
                            a.label = i.TextPath;
                            a.value = i.ObjectID;
                            a.labelField = 'TargetSubjectName';
                            a.valueField = 'TargetSubjectID';
                            a.templateValue = this.formTemplateString(i.TextPath, e.query);

                            a.data = {
                                Object: i.Object
                            };

                            this.targetSubResults.push(a);
                        });

                        if (this.targetSubResults.length == 1) {
                            this.setObjectValue(i, this.targetSubResults[0]);
                        }
                    });
                break;
            case 'selectedTargetObject':
                this.diagramService.queryObjects(i.TargetObjectType, i.TargetObjectTypeID, e.query).subscribe(
                    r => {
                        this.targetObjResults = [];

                        if (r == null) {
                            return;
                        }

                        r.forEach(i => {
                            let a = new AutoCompleteItem();
                            a.label = i.TextPath;
                            a.value = i.ObjectID;
                            a.labelField = 'TargetObjectName';
                            a.valueField = 'TargetObjectID';
                            a.templateValue = this.formTemplateString(i.TextPath, e.query);

                            a.data = {
                                Object: i.Object
                            };

                            this.targetObjResults.push(a);
                        });

                        if (this.targetObjResults.length == 1) {
                            this.setObjectValue(i, this.targetObjResults[0]);
                        }
                    });
                break;
            default:
                console.warn(`Invalid field '${field}' passed to query()`);
                break;
        }
    }

    blur(field: string, i: LineageEditorRow) {
        this.setAutoCompleteValue(i, i[field]);
    }

    add() {
        let l = new LineageEditorRow();

        this.initializeLineageRow(l);

        l.ID = this.lineage.length * -1;
        l.isNew = true;
        l.selectedSourceObject = "";
        l.selectedSourceSubject = "";
        l.selectedTargetObject = "";
        l.selectedTargetRelationshipType = "";
        l.selectedTargetSubject = "";
        l.selectedSourceRelationshipType = "";
        this.lineage.push(l);
    }

    delete(i: LineageEditorRow) {
        if (i.isNew) {
            let x = this.lineage.indexOf(i);

            if (x >= 0) {
                this.lineage.splice(x, 1);
            }
        } else {
            i.isDeleting = !i.isDeleting;
        }

        this.updateConnections();
    }

    updateConnections() {
        this.lineage.filter(l => l.isNew).forEach(l => l.isConnected = this.checkConnected(l));

        this.valid = true;

        this.lineage.filter(l => l.isNew).forEach(l => l.isDupe = false);
        this.lineage.filter(l => l.isNew).forEach(l => {
            let other = this.lineage.find(o => o.ID != l.ID &&
                o.sourcekey == l.sourcekey && o.targetkey == l.targetkey);

            l.isDupe = !!other;
        });
    }

    initializeLineageRow(i: LineageEditorRow) {
        i.isNew = false;
        i.isConnected = true;
        i.isDeleting = false;

        i.selectedSourceRelationshipType = new AutoCompleteItem();
        (<AutoCompleteItem>i.selectedSourceRelationshipType).labelField = 'SourceIntersectTypeName';
        (<AutoCompleteItem>i.selectedSourceRelationshipType).valueField = 'SourceIntersectTypeID';

        i.selectedTargetRelationshipType = new AutoCompleteItem();
        (<AutoCompleteItem>i.selectedTargetRelationshipType).labelField = 'TargetIntersectTypeName';
        (<AutoCompleteItem>i.selectedTargetRelationshipType).valueField = 'TargetIntersectTypeID';

        i.selectedSourceSubject = new AutoCompleteItem();
        (<AutoCompleteItem>i.selectedSourceSubject).labelField = 'SourceSubjectName';
        (<AutoCompleteItem>i.selectedSourceSubject).valueField = 'SourceSubjectID';

        i.selectedSourceObject = new AutoCompleteItem();
        (<AutoCompleteItem>i.selectedSourceObject).labelField = 'SourceObjectName';
        (<AutoCompleteItem>i.selectedSourceObject).valueField = 'SourceObjectID';

        i.selectedTargetSubject = new AutoCompleteItem();
        (<AutoCompleteItem>i.selectedTargetSubject).labelField = 'TargetSubjectName';
        (<AutoCompleteItem>i.selectedTargetSubject).valueField = 'TargetSubjectID';

        i.selectedTargetObject = new AutoCompleteItem();
        (<AutoCompleteItem>i.selectedTargetObject).labelField = 'TargetObjectName';
        (<AutoCompleteItem>i.selectedTargetObject).valueField = 'TargetObjectID';

        this.setAutoCompleteValue(i, (<AutoCompleteItem>i.selectedSourceRelationshipType));
        this.setAutoCompleteValue(i, (<AutoCompleteItem>i.selectedTargetRelationshipType));
        this.setAutoCompleteValue(i, (<AutoCompleteItem>i.selectedSourceSubject));
        this.setAutoCompleteValue(i, (<AutoCompleteItem>i.selectedSourceObject));
        this.setAutoCompleteValue(i, (<AutoCompleteItem>i.selectedTargetSubject));
        this.setAutoCompleteValue(i, (<AutoCompleteItem>i.selectedTargetObject));
    }

    setAutoCompleteValue(obj: any, i: AutoCompleteItem) {
        i.label = obj[i.labelField];
        i.value = obj[i.valueField];
    }

    setObjectValue(obj: any, i: AutoCompleteItem) {
        obj[i.labelField] = i.label;
        obj[i.valueField] = i.value;
    }

    summarize() {
        this.model.Adds = this.lineage.filter(l => l.isNew);
        this.model.Deletes = this.lineage.filter(l => l.isDeleting);
        this.model.Existing = this.lineage.filter(l => !l.isNew);
        this.mode = LineageEditorMode.Summary;
        this.saveComplete = false;
    }

    preview() {
        let valid = this.lineage.filter(l => l.isDeleting || (l.isNew &&
            l.SourceObjectID != null &&
            l.SourceSubjectID != null &&
            l.TargetObjectID != null &&
            l.TargetSubjectID != null &&
            l.SourceIntersectTypeID != null &&
            l.TargetIntersectTypeID != null));

        this.model.Adds = valid.filter(l => l.isNew);
        this.model.Deletes = valid.filter(l => l.isDeleting);
        this.model.Existing = this.lineage.filter(l => !l.isNew);
        this.mode = LineageEditorMode.Preview;
    }

    save() {
        if (this.isLoading) {
            return;
        }

        this.isLoading = true;

        this.diagramService.updateLineage(this.model).subscribe(
            r => {
                this.model = r;
                this.model.Focal = this.object;
                this.model.FocalID = this.objectId;

                let addErrors = (this.model.Adds == null) ? null : this.model.Adds.filter(m => m.HasError);
                let deleteErrors = (this.model.Deletes == null) ? null : this.model.Deletes.filter(m => m.HasError);

                this.onSaveComplete.emit();

                if ((addErrors && addErrors.length > 0) || (deleteErrors && deleteErrors.length > 0)) {
                    this.saveComplete = true;
                    this.isLoading = false;

                    //remove successful items
                    if (this.model.Adds) {
                        this.model.Adds.filter(a => !a.HasError).forEach(a => {
                            let added = this.lineage.findIndex(l => l.sourcekey == a.sourcekey && l.targetkey == a.targetkey);

                            if (added >= 0) {
                                this.lineage[added] = a;
                                this.lineage[added].isNew = false;
                            }
                        });
                    }

                    if (this.model.Deletes) {
                        this.model.Deletes.filter(d => !d.HasError).forEach(d => {
                            let deleted = this.lineage.findIndex(l => l.ID == d.ID);
                            if (deleted >= 0) this.lineage.splice(deleted, 1);
                        });
                    }

                    this.messagesService.showError("Error occurred", "Not all mappings were added/removed successfully.");
                    this.mode = LineageEditorMode.Default;
                } else {
                    this.messagesService.showInfoMessage("Save Successful", "Mappings were added/removed from the lineage successfully.");
                    this.load();
                    this.mode = LineageEditorMode.Default;
                }
            });
    }

    checkConnected(i: LineageEditorRow, rows: LineageEditorRow[] = null) {
        if (rows == null) {
            rows = this.lineage;
        }

        //incomplete record
        if (i.SourceIntersectTypeID < 1 ||
            i.TargetIntersectTypeID < 1 ||
            i.SourceSubjectID < 1 ||
            i.SourceObjectID < 1 ||
            i.TargetSubjectID < 1 ||
            i.TargetObjectID < 1) {
            return false;
        }

        //has intersect related to focal
        if ((i.SourceSubjectID == this.objectId && i.SourceSubject == this.object) ||
            (i.SourceObjectID == this.objectId && i.SourceObject == this.object) ||
            (i.TargetSubjectID == this.objectId && i.TargetSubject == this.object) ||
            (i.TargetObjectID == this.objectId && i.TargetObject == this.object) && !i.isDeleting) {
            return true;
        }

        //get a copy of rows
        let r = _.cloneDeep(rows);
        //remove this row from the list
        let x = r.findIndex(s => s.ID == i.ID);

        if (x >= 0) {
            r.splice(x, 1);
        }

        for (let j = 0; j < r.length; j++) {
            if (r[j].isDeleting) {
                /* can't connect to a record marked for deletion */
                continue;
            }

            if (i.sourcekey == r[j].targetkey || i.targetkey == r[j].sourcekey || i.targetkey == r[j].targetkey) {
                return this.checkConnected(r[j], r);
            }
        }

        return false;
    }

    formTemplateString(val: string, query: string): string {
        let x = val.toLowerCase().indexOf(query.toLowerCase());

        return val.substring(0, x) + '<strong>' + val.substr(x, query.length) + '</strong>' + val.substring(x + query.length);
    }
}
