import { Component, Input, OnInit, OnChanges, Output, EventEmitter } from '@angular/core';
import { DiagramService } from '../../../services/diagram.service';
import { MessagesService } from '../../../services/messages.service';
import { PermissionsService } from '../../../services/permissions.service';
import { BaseComponent } from '../base.component';
import { Permission } from '../../../models/permission.model';
import {
    LineageEditorTechnicalRow,
    AutoCompleteItem,
    LineageEditorTechnicalModel,
    LineageView,
    LineageEditorMode,
} from '../../../models/lineage.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-lineage-technical-editor',
    templateUrl: './lineage-technical-editor.component.html',
    styles: [
        `
.lineage-editor-table>thead>tr>th {
    border-radius: 0 !important;
    padding: 5px;
}
.lineage-editor-table>tbody>tr>td {
    border-radius: 0 !important;
    padding: 3px;
}

`
    ],
    providers: [DiagramService]
})

export class LineageTechnicalEditorComponent extends BaseComponent implements OnInit {
    @Input() object: string;
    @Input() objectId: number;
    @Output() onClose = new EventEmitter();
    @Output() onSaveComplete = new EventEmitter();
    @Output() onSaveSuccess = new EventEmitter();

    permissions: Permission[] = [];
    lineage: LineageEditorTechnicalRow[] = [];
    model: LineageEditorTechnicalModel;
    queryResults: AutoCompleteItem[] = [];
    valid = false;

    isLoading = false;
    saveComplete = false;

    mode: LineageEditorMode = LineageEditorMode.Default;
    LineageEditorMode = LineageEditorMode;
    view: LineageView = LineageView.Technical;


    constructor(private diagramService: DiagramService, protected messagesService: MessagesService, protected permissionsService: PermissionsService) {
        super();
    }

    ngOnInit() {
        this.load();
        this.permissionsService.getPermissions(this.objectId, this.object)
            .then(data => {
                this.permissions = data;
            });
        //this.model = new LineageEditorModel();
        //this.model.Focal = this.object;
        //this.model.FocalID = this.objectId;
    }

    load() {
        this.isLoading = true;
        //this.diagramService.getLineageDiagram(this.object, this.objectId, LineageView.ItemList)
        //    .then(r => {
        //        this.lineage = r.items;
        //        if (this.lineage != null && this.lineage.length > 0)
        //            this.lineage.forEach(i => {
        //                this.initializeLineageRow(i);
        //            });
        //        else
        //            this.lineage = [];
        //        this.isLoading = false;
        //        //console.log(this.lineage);
        //    });
        this.isLoading = false;
    }

    select(field: string, i: LineageEditorTechnicalRow, e: any) {
        this.setObjectValue(i, i[field]);

        let data = i[field].data;

        switch (field) {
            case 'selectedSourceSubject':
               // i.SourceSubject = data.Object;
                break;
          
        }

        //update connection checks
        this.updateConnections()
    }

    query(field: string, i: LineageEditorTechnicalRow, e: any) {
        
    }

    blur(field: string, i: LineageEditorTechnicalRow) {
        this.setAutoCompleteValue(i, i[field]);
        //console.log(i);
    }

    add() {
        let l = new LineageEditorTechnicalRow();
        this.initializeLineageRow(l);
        l.ID = this.lineage.length * -1;
        l.isNew = true;
        l.selectedSourceFusionAttribute = "";
        l.selectedTargetFusionAttribute = "";

        this.lineage.push(l);

        //console.log(l);
    }

    delete(i: LineageEditorTechnicalRow) {
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
        //this.lineage.filter(l => l.isNew).forEach(l => l.isConnected = this.checkConnected(l));
        //if (this.lineage.findIndex(l => l.isNew && !l.isConnected) >= 0)
        //    this.valid = false;
        //else
        //    this.valid = true;

        //this.lineage.filter(l => l.isNew).forEach(l => l.isDupe = false);

        //this.lineage.filter(l => l.isNew).forEach(l => {
        //    let other = this.lineage.find(o => o.ID != l.ID &&
        //        o.sourcekey == l.sourcekey && o.targetkey == l.targetkey);
        //    if (other)
        //        l.isDupe = true;
        //});
    }

    initializeLineageRow(i: LineageEditorTechnicalRow) {
        i.isNew = false;
        i.isDeleting = false;

        i.selectedSourceFusionAttribute = new AutoCompleteItem();
        (<AutoCompleteItem>i.selectedSourceFusionAttribute).labelField = 'SourceFusionAttributeName';
        (<AutoCompleteItem>i.selectedSourceFusionAttribute).valueField = 'SourceFusionAttributeID';

        i.selectedTargetFusionAttribute = new AutoCompleteItem();
        (<AutoCompleteItem>i.selectedTargetFusionAttribute).labelField = 'TargetFusionAttributeName';
        (<AutoCompleteItem>i.selectedTargetFusionAttribute).valueField = 'TargetFusionAttributeID';

        this.setAutoCompleteValue(i, (<AutoCompleteItem>i.selectedSourceFusionAttribute));
        this.setAutoCompleteValue(i, (<AutoCompleteItem>i.selectedTargetFusionAttribute));

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
        //let valid = this.lineage.filter(l => l.isDeleting || (l.isNew &&
        //    l.SourceObjectID != null &&
        //    l.SourceSubjectID != null &&
        //    l.TargetObjectID != null &&
        //    l.TargetSubjectID != null &&
        //    l.SourceIntersectTypeID != null &&
        //    l.TargetIntersectTypeID != null));

        //this.model.Adds = valid.filter(l => l.isNew);
        //this.model.Deletes = valid.filter(l => l.isDeleting);
        this.model.Existing = this.lineage.filter(l => !l.isNew);
        this.mode = LineageEditorMode.Preview;
    }

    save() {
        if (this.isLoading) return;

        this.isLoading = true;

        //this.diagramService.updateLineage(this.model)
        //    .then(r => {
        //        this.model = r;
        //        this.model.Focal = this.object;
        //        this.model.FocalID = this.objectId;

        //        let addErrors = (this.model.Adds == null) ? null : this.model.Adds.filter(m => m.HasError);
        //        let deleteErrors = (this.model.Deletes == null) ? null : this.model.Deletes.filter(m => m.HasError);

        //        this.onSaveComplete.emit();

        //        if ((addErrors && addErrors.length > 0) || (deleteErrors && deleteErrors.length > 0)) {
        //            this.saveComplete = true;
        //            this.isLoading = false;

        //            //remove successful items
        //            if (this.model.Adds)
        //                this.model.Adds.filter(a => !a.HasError).forEach(a => {
        //                    let added = this.lineage.findIndex(l => l.sourcekey == a.sourcekey && l.targetkey == a.targetkey);
        //                    if (added >= 0) {
        //                        this.lineage[added] = a; this.lineage[added].isNew = false;
        //                    }
        //                });
        //            if (this.model.Deletes)
        //                this.model.Deletes.filter(d => !d.HasError).forEach(d => {
        //                    let deleted = this.lineage.findIndex(l => l.ID == d.ID);
        //                    if (deleted >= 0) this.lineage.splice(deleted, 1);
        //                });
        //        } else {
        //            this.messagesService.showInfoMessage("Save Successful", "Mappings were added/removed from the lineage successfully.");
        //            this.load();
        //            this.mode = LineageEditorMode.Default;
        //        }

        //    });
    }

    checkConnected(i: LineageEditorTechnicalRow, rows: LineageEditorTechnicalRow[] = null) {
        if (rows == null)
            rows = this.lineage;

        //incomplete record
        //if (i.SourceIntersectTypeID < 1 ||
        //    i.TargetIntersectTypeID < 1 ||
        //    i.SourceSubjectID < 1 ||
        //    i.SourceObjectID < 1 ||
        //    i.TargetSubjectID < 1 ||
        //    i.TargetObjectID < 1) {
        //    return false;
        //}

        ////has intersect related to focal
        //if ((i.SourceSubjectID == this.objectId && i.SourceSubject == this.object) ||
        //    (i.SourceObjectID == this.objectId && i.SourceObject == this.object) ||
        //    (i.TargetSubjectID == this.objectId && i.TargetSubject == this.object) ||
        //    (i.TargetObjectID == this.objectId && i.TargetObject == this.object) && !i.isDeleting) {
        //    return true;
        //}

        ////get a copy of rows
        //let r = _.cloneDeep(rows);
        ////remove this row from the list
        //let x = r.findIndex(s => s.ID == i.ID);
        //if (x >= 0) r.splice(x, 1);

        //for (let j = 0; j < r.length; j++) {
        //    if (r[j].isDeleting) //can't connect to a record marked for deletion
        //        continue;
        //    if (i.sourcekey == r[j].targetkey || i.targetkey == r[j].sourcekey || i.targetkey == r[j].targetkey) {
        //        let a = this.checkConnected(r[j], r);
        //        return a;
        //    }
        //}
        return false;

    }
}




