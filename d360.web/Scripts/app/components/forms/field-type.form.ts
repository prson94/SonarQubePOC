///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { NgForm } from '@angular/common';
import { Button, Editor, Header, InputText, Checkbox, Dropdown, SelectItem, MultiSelect } from 'primeng/primeng';
import { FieldType, FieldTypeEditorModel, Lookups, FieldTypeFusionItemEditorModel, FieldTypeFusionLookupDisplayField } from '../../models/fields.model';
import { FieldsService } from '../../services/fields.service';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-field-type-form',
    templateUrl: 'scripts/app/components/forms/field-type.form.html',
    providers: [FieldsService],
    directives: [Button, Editor, Header, InputText, Checkbox, Dropdown, MultiSelect],
})

export class FieldTypeForm implements OnInit, OnChanges {
    @Input() id: number;
    @Input() title: string = "Add Field Type";
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private lookups: Lookups = new Lookups();
    private model: FieldTypeEditorModel;
    private isLoading = false;
    private isSaving = false;
    private initialItem: FieldTypeEditorModel;

    private testPattern: string;
    private testPatternValidationText: string;

    constructor(private fieldsService: FieldsService) {
        this.model = new FieldTypeEditorModel();
        this.model.FieldType = new FieldType();
    }

    ngOnInit() {
        this.initialItem = _.cloneDeep(this.model);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
                this.initialItem = _.cloneDeep(this.model);
            }
        }
    }

    //#region load functions

    private load(): void {
        this.isLoading = true;
        this.fieldsService.getFieldTypeEditor(this.id)
            .then(data => {
                this.model = data;
                this.model.selectedLookup = this.model.FieldType.LookupObjectType + '|' + this.model.FieldType.LookupObjectID;
            })
            .then(() => this.fieldsService.getLookups(this.model.FieldType.ObjectID, this.model.FieldType.Object))
            .then(d => {
                this.lookups = d;
                this.lookups.ReferenceTypes = this.fieldsService.getReferenceTypes()
            })
            .then(() => { if (this.id > 0) return this.fieldsService.getFormData(this.id) })
            .then(f => {
                if (f) {
                    this.model.FusionItems = f.FusionItems;
                    console.log('Model');
                    console.log(this.model);
                }
            })
            .then(() => {
                return this.loadDataType(this.model.FieldType.Type);
            })
            .then(() => this.isLoading = false);
    }

    private loadDataType(value: string): Promise<void> {
        let promises = [];
        switch (value.toLowerCase()) {
            case 'lookup':
                promises.push(this.loadTokens(this.model.FieldType.LookupObjectType + '|' + this.model.FieldType.LookupObjectID));
            case 'fusionlookup':
                if (this.model.FusionItems && this.model.FusionItems.length)
                    this.model.FusionItems.forEach(i => {
                        promises.push(
                            this.loadTargetFusionAttributes(i)
                            .then(() => this.loadFusionDisplayFields(i)) 
                            );
                    });
                break;
            default:
                break;
        }
        return Promise.all(promises).then(() => { });
    }

    private loadTokens(value: string): Promise<void> {
        let id = parseInt(value.split('|')[1]);
        let type = value.split('|')[0];
        if (type != "DomainItem") type += 'Type';
        return this.fieldsService.getLookupTokens(id, type)
            .then(r => {
                this.model.LookupTokens = r;
                if (this.model.LookupTokens.length > 0)
                    this.model.FieldType.LookupDisplayFormat = this.model.LookupTokens[0].value;
            });
    }

    private loadTargetFusionAttributes(item: FieldTypeFusionItemEditorModel): Promise<void> {
        return this.fieldsService.getFusionLookupTargetAttributeTypes(item.SourceFusionAttributeType, item.ReferenceType)
            .then(d => {
                item.TargetFusionAttributeTypes = d;
            });
    }

    private loadFusionDisplayFields(item: FieldTypeFusionItemEditorModel): Promise<void> {
        return this.fieldsService.getFusionDisplayFields(item.TargetFusionAttributeType)
            .then(d => {
                item.FusionDisplayFields = d;
            });
    }

    //#endregion

    private selectToken(value: string) {
        this.model.FieldType.LookupDisplayFormat += value;
    }

    private addFusion() {
        let i = new FieldTypeFusionItemEditorModel();
        i.ReferenceType = this.lookups.ReferenceTypes[0].value;
        this.model.FusionItems.push(i);
    }
    private removeFusion(i: number) {
       this.model.FusionItems.splice(i, 1);
    }

    private validatePattern() {
        if (this.model.FieldType.Pattern > "" && this.testPattern > "") {
            var patternRegex = new RegExp(this.model.FieldType.Pattern);
            this.testPatternValidationText = (patternRegex.test(this.testPattern )) ? 'Success' : 'Fail';
        }
        else {
            this.testPatternValidationText = '';
        }
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private save(): void {

        //console.log(this.model);

        //convert DisplayFields to objects
        this.model.FusionItems.forEach(i => {
            let d: FieldTypeFusionLookupDisplayField[] = [];

            (<string[]>i.DisplayFields).forEach(j => {
                let k = new FieldTypeFusionLookupDisplayField();
                try {
                    k.FieldTypeID = parseInt(j.split('|')[0]);
                    k.FieldTypeName = j.split('|')[1];
                } catch(e) {
                    return;
                }
                d.push(k);
            });

            i.DisplayFields = d;

        });

        //        if (self.RelationItems().length > 0) {
        //            try {
        //                var ri = self.RelationItems()[0];
        //                if (ri.IntersectType() > '') {
        //                    postModel.RelationItem["ID"] = ri.ID();
        //                    postModel.RelationItem.IntersectType = ri.IntersectType().split('|')[0];
        //                    postModel.RelationItem.ReferenceType = ri.ReferenceType();
        //                    if (ri.ChildIntersectType() > '') {
        //                        postModel.RelationItem.ChildIntersectType = ri.ChildIntersectType().split('|')[0];
        //                    }
        //                    $.each(ri.DisplayFieldOptions(), function (fix, fid) {
        //                        var values = fid.value().split('|');

        //                        if (fid.Show() || fid.SortOrder() || fid.FilterValue() != '') {
        //                            postModel.RelationItem.DisplayFields.push({
        //                                FieldTypeID: values[0],
        //                                FieldTypeName: values[1],
        //                                Show: fid.Show(),
        //                                SortOrder: fid.SortOrder(),
        //                                FilterValue: fid.FilterValue()
        //                            });
        //                        }
        //                    });
        //                    postModel.RelationItem.HideHeader = ri.HideHeader();
        //                    postModel.RelationItem.HideFooter = ri.HideFooter();
        //                }
        //            } catch (e) {
        //                console.log(e);
        //            }
        //        }

        //        var uri = '';
        //        var method = '';
        //        if (self.ID() > 0) {
        //            uri = '/form/EditFieldType';
        //            method = 'PUT';
        //        }
        //        else {
        //            uri = '/form/AddFieldType';
        //            method = 'POST';
        //        }

        //        $.ajax(uri, {
        //            data: postModel,
        //            dataType: 'json',
        //            method: method
        //        }).done(function (data, status, xhr) {
        //            if (data != null) {
        //                if (data.type == 'error') {
        //                    amplify.publish("ShowMessage", { type: "error", title: data.title, message: data.message });
        //                } else {
        //                    amplify.publish("SaveAction", { context: self.Context(), action: 'add', id: 0, custom: {} });
        //                    amplify.publish("ShowMessage", { type: "confirm", title: "Success!", message: 'Field type successfully created.' });
        //                }
        //            }

        //        }).fail(function (xhr, status, error) {
        //            amplify.publish("ShowMessage", { type: "error", title: "Error!", message: error });
        //        }).always(function (data, status, error) {
        //            self.InProgress(false);
        //        });
        //    }
        //}

    
        if (this.model.FieldType.ID > 0) {
            this.fieldsService.putFieldType(this.model)
                .then(r => {
                    this.onSuccess.emit(null);
                    this.onComplete.emit(null);
                });
        } else {
            this.fieldsService.postFieldType(this.model)
                .then(r => {
                    this.onSuccess.emit(null);
                    this.onComplete.emit(null);
                });
        }

    }
}
