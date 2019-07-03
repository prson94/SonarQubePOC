import * as _ from 'lodash';
import {forkJoin} from "rxjs";
import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';

import {
    AutoCompleteItem,
    LineageEditorMode,
    LineageEditorRow,
    LineageEditorTechnicalModel,
    LineageEditorTechnicalRow,
    LineageView,
} from '../../../../models/lineage.model';

import {DiagramService} from '../../../../services/diagram.service';
import {ResponsibilityTypeService} from '../../../../services/responsibility-type.service';

import {BaseComponent} from '../../base.component';
import { MessagesObservableService } from '../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-lineage-technical-editor',
    templateUrl: './lineage-technical-editor.component.html',
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
    providers: [DiagramService, ResponsibilityTypeService]
})

export class LineageTechnicalEditorComponent extends BaseComponent implements OnInit {
    @Input() object: string;
    @Input() objectId: number;
    @Output() onClose = new EventEmitter();
    @Output() onSaveComplete = new EventEmitter();
    @Output() onSaveSuccess = new EventEmitter();

    lineage: LineageEditorTechnicalRow[] = [];
    mapItems: LineageEditorRow[] = [];
    model: LineageEditorTechnicalModel;
    queryResults: AutoCompleteItem[] = [];
    valid = true;

    isLoading = false;
    saveComplete = false;

    mode: LineageEditorMode = LineageEditorMode.Default;
    LineageEditorMode = LineageEditorMode;
    view: LineageView = LineageView.Technical;

    constructor(
        private diagramService: DiagramService,
        protected messagesService: MessagesObservableService,
        protected responsibilityTypeService: ResponsibilityTypeService
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;

        forkJoin(
            this.diagramService.getLineageDiagram(this.object, this.objectId, LineageView.MapRuleItemList, false),
            this.diagramService.getLineageDiagram(this.object, this.objectId, LineageView.MapItemList, false)
        ).subscribe(
            (
                [
                    MapRuleItemList,
                    MapItemList
                ]
            ) => {
                /* MapRuleItemList */
                this.lineage = MapRuleItemList.items;

                if (this.lineage != null && this.lineage.length > 0) {
                    this.lineage.forEach(i => {
                        this.initializeLineageRow(i);
                    });
                } else {
                    this.lineage = [];
                }
                /* ./MapRuleItemList */

                /* MapItemList */
                this.mapItems = MapItemList.items;

                this.lineage.forEach(l => {
                    if (l.MapItemID != null) {
                        l.selectedMapItem = this.mapItems.find(m => m.ID == l.MapItemID);
                    }
                });
                /* ./MapItemList */

                this.isLoading = false;
            }
        );
    }

    select(field: string, i: LineageEditorTechnicalRow, e: any) {
        if (field != 'selectedMapItem') {
            this.setObjectValue(i, i[field]);
            let data = i[field].data;
        }

        switch (field) {
            case 'selectedMapItem':
                i.MapItemID = e;
                i.selectedMapItem = this.mapItems.find(m => m.ID == e);
                break;
            case 'selectedTargetFusionAttribute':
                let t = this.lineage.find(l => l.MapItemID > 0 && l.TargetFusionAttributeID == i.TargetFusionAttributeID);

                if (t != null) {
                    i.MapItemID = t.MapItemID;
                }
                break;
        }

        //update connection checks
        this.updateConnections()
    }

    query(field: string, i: LineageEditorTechnicalRow, e: any) {
        switch (field) {
            case 'selectedSourceFusionAttribute':
                this.diagramService.queryFusionAttributes(e.query).subscribe(
                    r => {
                        this.queryResults = [];

                        r.forEach(i => {
                            let a = new AutoCompleteItem();
                            a.label = i.Name;
                            a.value = i.ID;
                            a.labelField = 'SourceFusionAttributeName';
                            a.valueField = 'SourceFusionAttributeID';
                            a.templateValue = this.formTemplateString(i.Name, e.query);

                            this.queryResults.push(a);
                        });

                        if (this.queryResults.length == 1) {
                            this.setObjectValue(i, this.queryResults[0]);
                        }
                    }
                );
                break;
            case 'selectedTargetFusionAttribute':
                this.diagramService.queryFusionAttributes(e.query).subscribe(
                    r => {
                        this.queryResults = [];

                        r.forEach(i => {
                            let a = new AutoCompleteItem();
                            a.label = i.Name;
                            a.value = i.ID;
                            a.labelField = 'TargetFusionAttributeName';
                            a.valueField = 'TargetFusionAttributeID';
                            a.templateValue = this.formTemplateString(i.Name, e.query);

                            this.queryResults.push(a);
                        });

                        if (this.queryResults.length == 1) {
                            this.setObjectValue(i, this.queryResults[0]);
                        }
                    }
                );
                break;
        }
    }

    blur(field: string, i: LineageEditorTechnicalRow) {
        this.setAutoCompleteValue(i, i[field]);
    }

    add() {
        let l = new LineageEditorTechnicalRow();

        this.initializeLineageRow(l);

        l.ID = this.lineage.length * -1;
        l.isNew = true;
        l.selectedSourceFusionAttribute = "";
        l.selectedTargetFusionAttribute = "";

        this.lineage.push(l);
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
        this.lineage.filter(l => l.isNew).forEach(l => l.isConnected = this.checkConnected(l));
        this.valid = this.lineage.findIndex(l => l.isNew && !l.isConnected) < 0;
        this.lineage.filter(l => l.isNew).forEach(l => l.isDupe = false);
        this.lineage.filter(l => l.isNew).forEach(l => {
            let other = this.lineage.find(
                o => o.ID != l.ID &&
                    o.SourceFusionAttributeID == l.SourceFusionAttributeID && o.TargetFusionAttributeID == l.TargetFusionAttributeID
            );

            l.isDupe = !!other;
        });
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
        this.model = new LineageEditorTechnicalModel();
        this.model.Adds = this.lineage.filter(l => l.isNew);
        this.model.Deletes = this.lineage.filter(l => l.isDeleting);
        this.model.Existing = this.lineage.filter(l => !l.isNew);

        this.model.Adds.forEach(a => {
            if (a.selectedMapItem != null) {
                a.MapItemID = a.selectedMapItem.ID;
            }
        });

        this.mode = LineageEditorMode.Summary;
        this.saveComplete = false;
    }

    preview() {
        let valid = this.lineage.filter(l => l.isDeleting || (l.isNew && l.SourceFusionAttributeID != null && l.TargetFusionAttributeID != null));

        if (this.model == null) {
            this.model = new LineageEditorTechnicalModel();
        }

        this.model.Adds = valid.filter(l => l.isNew);
        this.model.Deletes = valid.filter(l => l.isDeleting);
        this.model.Existing = this.lineage.filter(l => !l.isNew);

        this.mode = LineageEditorMode.Preview;
    }

    save() {
        if (this.isLoading) return;

        this.isLoading = true;

        this.diagramService.updateTechnicalLineage(this.model).subscribe(
            r => {
                this.model = r;

                let addErrors = (this.model.Adds == null) ? null : this.model.Adds.filter(m => m.HasError);
                let deleteErrors = (this.model.Deletes == null) ? null : this.model.Deletes.filter(m => m.HasError);

                this.saveComplete = true;
                this.onSaveComplete.emit();

                if ((addErrors && addErrors.length > 0) || (deleteErrors && deleteErrors.length > 0)) {
                    this.saveComplete = true;
                    this.isLoading = false;

                    //update items
                    if (this.model.Adds) {
                        this.model.Adds.filter(a => !a.HasError).forEach(a => {
                            let added = this.lineage.findIndex(l => l.SourceFusionAttributeID == a.SourceFusionAttributeID && l.TargetFusionAttributeID == a.TargetFusionAttributeID);

                            if (added >= 0) {
                                this.lineage[added] = a;
                                this.lineage[added].isNew = false;
                            }
                        });

                        this.model.Adds.filter(a => a.HasError).forEach(a => {
                            let added = this.lineage.findIndex(l => l.SourceFusionAttributeID == a.SourceFusionAttributeID && l.TargetFusionAttributeID == a.TargetFusionAttributeID);

                            if (added >= 0) {
                                this.lineage[added].HasError = true;
                                this.lineage[added].ErrorMessage = a.ErrorMessage;
                            }
                        });
                    }

                    if (this.model.Deletes) {
                        this.model.Deletes.filter(d => !d.HasError).forEach(d => {
                            let deleted = this.lineage.findIndex(l => l.ID == d.ID);

                            if (deleted >= 0) this.lineage.splice(deleted, 1);
                        });

                        this.model.Deletes.filter(d => d.HasError).forEach(d => {
                            let deleted = this.lineage.findIndex(l => l.ID == d.ID);

                            if (deleted >= 0) {
                                this.lineage[deleted].HasError = true;
                                this.lineage[deleted].ErrorMessage = d.ErrorMessage;
                            }
                        });
                    }

                    this.messagesService.showError("Error occurred", "Not all mappings were added/removed successfully.");
                    this.mode = LineageEditorMode.Default;
                } else {
                    this.messagesService.showInfoMessage("Save Successful", "Mappings were added/removed from the lineage successfully.");
                    this.load();
                    this.mode = LineageEditorMode.Default;
                }
            }
        );
    }

    checkConnected(
        i: LineageEditorTechnicalRow,
        rows: LineageEditorTechnicalRow[] = null
    ) {
        if (rows == null) {
            rows = this.lineage;
        }

        if (i.SourceFusionAttributeID == null || i.SourceFusionAttributeID < 1 || i.TargetFusionAttributeID == null || i.TargetFusionAttributeID < 1) {
            return false;
        }

        //connected to business lineage directly
        if (i.MapItemID != null && i.MapItemID > 0) {
            return true;
        }

        //get a copy of rows
        let r = _.cloneDeep(rows);

        let x = r.findIndex(s => s.ID == i.ID);
        if (x >= 0) r.splice(x, 1);

        for (let j = 0; j < r.length; j++) {
            if (r[j].isDeleting) {
                /* can't connect to a record marked for deletion */
                continue;
            }

            if (i.SourceFusionAttributeID == r[j].TargetFusionAttributeID || i.TargetFusionAttributeID == r[j].SourceFusionAttributeID || i.TargetFusionAttributeID == r[j].TargetFusionAttributeID) {
                return this.checkConnected(r[j], r);
            }
        }
        return false;
    }

    formTemplateString(
        val: string,
        query: string
    ): string {
        let x = val.toLowerCase().indexOf(query.toLowerCase());

        return val.substring(0, x) + '<strong>' + val.substr(x, query.length) + '</strong>' + val.substring(x + query.length);
    }
}
