import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem } from 'primeng/primeng';
import { FieldType, FieldTypeEditorModel, Lookups, FieldTypeFusionItemEditorModel, FieldTypeFusionLookupDisplayField, FieldTypeRelationItemEditorModel, ComplexLookupRelationType, FieldTypeItemDisplayFieldEditorModel } from '../../models/fields.model';
import { FieldsService } from '../../services/fields.service';
import { MessagesService } from '../../services/messages.service';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-field-type-form',
    templateUrl: './field-type.form.html',
    styles: [
        `
        .display-table tr td {
            padding:3px;
            border-radius: 0;
        }

        .relation-table tr td {
            border-radius: 0;
        }

        .display-table-title {
            text-align:center;
            width:100%;
            font-family: "Roboto", Tahoma !important;
            text-transform: uppercase;
            color: #5c5e60 !important;
            font-size: 1rem;
            font-weight: bold;
        }
`
    ],
    providers: [FieldsService],
})

export class FieldTypeForm implements OnInit, OnChanges {
    @Input() id: number;
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() actionName: string = "Add";
    @Output() onComplete = new EventEmitter();   
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private lookups: Lookups = new Lookups();
    private model: FieldTypeEditorModel;
    private isLoading = false;
    private isSaving = false;
    private initialItem: FieldTypeEditorModel;

    private testPattern: string;
    private testPatternValidationText: string;
    private syncApiNameWithName: boolean = true;

    private relationItemCount = 0;

    constructor(private fieldsService: FieldsService, private messagesService: MessagesService) {
        this.model = new FieldTypeEditorModel();
        this.model.FieldType = new FieldType(); 
        this.model.FieldType.Object = this.objectType;
        this.model.FieldType.ObjectID = this.objectID;       
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
            else if (p == 'objectID' && this.model.FieldType != null) {
                this.model.FieldType.Object = this.objectType;
                this.model.FieldType.ObjectID = this.objectID;
            }
        }
    }

    //#region load functions

    private load(): void {
        if (this.id > 0) {
            this.actionName = 'Edit';
            this.isLoading = true;
            this.fieldsService.getFieldTypeEditor(this.id)
                .then(data => {
                    console.log('data: ');
                    console.log(data);
                    this.model = data;
                    if (this.model.FieldType.Type == 'ComplexRelationLookup')
                        this.model.FieldType.Type = 'RelationLookup';
                    this.model.selectedLookup = this.model.FieldType.LookupObjectType + '|' + this.model.FieldType.LookupObjectID;
                })
                .then(() => this.fieldsService.getLookups(this.model.FieldType.ObjectID, this.model.FieldType.Object))
                .then(d => {
                    console.log('lookups: ');
                    console.log(d);
                    this.lookups = d;

                    this.lookups.IntersectTypes.forEach(i => {
                        i.id = i.value.split('|')[0];

                    });

                    this.lookups.ReferenceTypes = this.fieldsService.getReferenceTypes()
                })
                .then(() => { if (this.id > 0) return this.fieldsService.getFormData(this.id) })
                .then(f => {
                    if (f) {
                        console.log("form data: ");
                        console.log(f);
                        this.model.RelationItems = f.RelationItems;
                        if (this.model.RelationItems)
                            this.model.RelationItems.forEach(r => {
                                let intersectType = this.lookups.IntersectTypes.find(i => i.id == r.IntersectType.toString());
                                if (r.Object == null || r.Object == '')
                                    r.Object = intersectType.value.split('|')[1];
                                if (r.ObjectID == null || r.ObjectID < 0)
                                    r.ObjectID = parseInt(intersectType.value.split('|')[2]);

                                r.DisplayFields.forEach(d => {
                                    if (d.FieldTypeID == null && d.value)
                                        d.FieldTypeID = parseInt(d.value.split('|')[0]);
                                    if (d.FieldTypeName == null && d.value)
                                        d.FieldTypeName = d.value.split('|')[1];

                                    if (!d.value)
                                        d.value = d.FieldTypeID + '|' + d.FieldTypeName;
                                });
                            });


                        let clone = _.cloneDeep(this.model.RelationItems);

                        for (let i = 0; i < this.model.RelationItems.length; i++) {
                            let item = this.model.RelationItems[i];
                            let lastItem = i > 0 ? this.model.RelationItems[i - 1] : null;

                            if (i == this.model.RelationItems.length - 1) {
                                //last item is for final right-side selection only, remove after load
                                lastItem.selectedRelationItemID = item.IntersectType + '|' + item.Object + '|' + item.ObjectID;
                                //console.log('last item on load');
                                //console.log(lastItem);
                                this.changeRefType(lastItem, lastItem.selectedRelationItemID).
                                    then(() => this.changeRel(lastItem))
                                    .then(() => {
                                        lastItem.DisplayFields.forEach(d => {
                                            let item = clone[i - 1].DisplayFields.find(f => f.FieldTypeID == d.FieldTypeID && f.FieldTypeName == d.FieldTypeName);

                                            if (item) {
                                                d.Show = true;
                                                d.DisplayOrder = item.DisplayOrder;
                                                d.FilterValue = item.Filter;
                                                d.OverrideDisplayName = item.OverrideDisplayName;
                                                d.SortOrder = item.SortOrder;
                                            }
                                        })
                                    })
                                    .then(() => this.deleteRelation(this.model.RelationItems[i]));
                                    //.then(() => this.model.RelationItems.pop());
                                break;
                            }

                            if (lastItem == null)
                                this.changeRefType(item);
                            else
                                this.changeRefType(item)
                                    .then(() => {
                                        lastItem.selectedRelationItemID = item.IntersectType + '|' + item.Object + '|' + item.ObjectID;
                                    })
                                    .then(() => this.changeRel(lastItem))
                                    .then(() => {
                                       // console.log('load display fields');
                                        
                                        //console.log(lastDisplayFields);
                                        //console.log(lastItem.DisplayFields);
                                        lastItem.DisplayFields.forEach(d => {
                                            let item = clone[i-1].DisplayFields.find(f => f.FieldTypeID == d.FieldTypeID && f.FieldTypeName == d.FieldTypeName);

                                            if (item) {
                                                d.Show = true;
                                                d.DisplayOrder = item.DisplayOrder;
                                                d.FilterValue = item.Filter;
                                                d.OverrideDisplayName = item.OverrideDisplayName;
                                                d.SortOrder = item.SortOrder;
                                            }

                                        });

                                        let r = lastItem.relationItems.find(f => f.value == lastItem.selectedRelationItemID);
                                        if (r)
                                            item.displayValue = r.label;
                                    });


                        }

                        //this.model.RelationItems.forEach(r => {
                        //    r.selectedRelationItemID = r.IntersectType + '|' + r.Object + '|' + r.ObjectID;
                        //    this.changeRefType(r, r.selectedRelationItemID)
                        //        ;//.then(() => this.changeRel(r));
                             
                        //    //this.changeRefType(r)
                        //    //    .then(() => this.changeRel(r))
                        //    //    .then(() => {
                        //    //        let i = r.relationItems.find(i => i.value == r.selectedRelationItemID);
                        //    //        //console.log('relation item');
                        //    //        //console.log(r);
                        //    //        //console.log(i);
                        //    //        if (i)
                        //    //            r.displayValue = i.label;
                        //    //        //r.displayValue = r.relationItems.find(i => i.value == r.selectedRelationItemID).label;
                                   
                        //    //    });
                        //});

                        this.model.FusionItems = f.FusionItems; 


                        if (this.model.FieldType.Type == 'RelationLookup') {
                            this.model.RelationItems.forEach(r => {
                                let s = [];
                                for (let i = 1; i <= r.DisplayFields.length; i++) {
                                    r.DisplayFields[i-1].DisplayOrder = i;
                                    s.push({ id: i , text: i  });
                                }
                                r.SortOrderList = s;
                                
                            });
                            this.relationItemCount = this.model.RelationItems.length;
                            //console.log(this.relationItemCount);
                        }

                    }
                })
                .then(() => {
                    return this.loadDataType(this.model.FieldType.Type);
                })
                .then(() => this.isLoading = false);
        } else {
            this.actionName = 'Add';
            this.isLoading = true;
            this.model = new FieldTypeEditorModel();
            this.model.FieldType = new FieldType();

            this.fieldsService.getLookups(this.objectID, this.objectType)
                .then(d => {
                    this.lookups = d;
                    this.lookups.ReferenceTypes = this.fieldsService.getReferenceTypes()
                })
                .then(() => this.isLoading = false);;
        }
    }

    private loadDataType(value: string): Promise<void> {
        let promises = [];
        switch (value.toLowerCase()) {
            case 'lookup':
                promises.push(this.loadTokens(this.model.FieldType.LookupObjectType,this.model.FieldType.LookupObjectID));                
            case 'fusionlookup':
                if (this.model.FusionItems && this.model.FusionItems.length)
                    this.model.FusionItems.forEach(i => {
                        promises.push(
                            this.loadTargetFusionAttributes(i)
                            .then(() => this.loadFusionDisplayFields(i)) 
                            );
                    });
                break;
            case 'relationlookup':
                if (this.model.RelationItems == null || this.model.RelationItems.length == 0) {
                    let r = new FieldTypeRelationItemEditorModel();
                    r.DisplayFields = [];
                    r.ReferenceType = 1;
                    r.Object = this.objectType;
                    r.ObjectID = this.objectID;
                    this.model.RelationItems.push(r);
                    console.log(this.lookups);
                }
                break;
            default:
                break;
        }
        return Promise.all(promises).then(() => { });
    }

    // called when the lookup type field is changed
    private lookupTypeSelected(value: string) {
                
        if (value == undefined) {
            console.log("[ERROR] - LOOKUP TYPE IS UNDEFINED", value);

            return;
        }

        //update the model to have correct lookuptype object and id
        let id = parseInt(value.split('|')[1]);
        let type = value.split('|')[0];

        this.model.FieldType.LookupObjectID = id;
        this.model.FieldType.LookupObjectType = type;

        this.loadTokens(type, id);
    }


    private loadTokens(objectType: string, objectId: number): Promise<void> {
        if (this.model.FieldType.LookupObjectType == undefined || this.model.FieldType.LookupObjectID == undefined) {
            console.log("[ERROR] - NO TYPE OR ID SPECIFIED TO LOAD TOKENS FOR", this.model.FieldType.LookupObjectID, this.model.FieldType.LookupObjectType);

            return;
        }

        if (objectType != "DomainItem") objectType += 'Type';
        
        return this.fieldsService.getLookupTokens(objectId, objectType)
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
        if (this.model.FusionItems == null) {            
            this.model.FusionItems = [];
        }
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

    private updateApiName(event) {
        this.model.FieldType.Name = event.target.value.replace(/[^a-zA-Z0-9-_]/g,'');
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }
        
    private onSubmit(): void {
        if (this.model.FieldType.Type == 'RelationLookup') {
            this.model.FieldType.Type = 'ComplexRelationLookup';
        }

        if (this.model.FieldType.Type == 'ComplexRelationLookup') {
            let r = new FieldTypeRelationItemEditorModel();
            let last = _.last(this.model.RelationItems);
            let i = last.relationItems.find(f => f.value == last.selectedRelationItemID);

            //console.log('save items');
            //console.log(r);
            //console.log(last);
            //console.log(i);

                
            if (i) {
                r.IntersectType = i.IntersectTypeID;
                r.IntersectTypeID = i.IntersectTypeID;
                r.Object = i.TargetType;
                r.ObjectID = i.TargetTypeID;
                r.ReferenceType = 0;
                r.DisplayFields = [];
                this.model.RelationItems.push(r);
            }
        }
        //    this.model.RelationItems.forEach(r => {
        //        if (r.selectedRelationItemID) {
        //            let f = r.relationItems.find(i => i.value == r.selectedRelationItemID);
        //            if (f != null) {
        //                //r.IntersectType = f.IntersectTypeID;
        //                r.IntersectTypeID = f.IntersectTypeID;
        //                r.Object = f.TargetType;
        //                r.ObjectID = f.TargetTypeID;
        //            }
        //        }
        //    });
        //}


        //convert DisplayFields to objects
        if (this.model.FusionItems) {
            this.model.FusionItems.forEach(i => {
                let d: FieldTypeFusionLookupDisplayField[] = [];

                (<string[]>i.DisplayFields).forEach(j => {
                    let k = new FieldTypeFusionLookupDisplayField();
                    try {
                        k.FieldTypeID = parseInt(j.split('|')[0]);
                        k.FieldTypeName = j.split('|')[1];
                    } catch (e) {
                        return;
                    }
                    d.push(k);
                });

                i.DisplayFields = d;

            });
        }
        this.isLoading = true;
        //console.log('save model:');
        //console.log(this.model);
        if (this.model.FieldType.ID > 0) {
            this.fieldsService.putFieldType(this.model)
                .then(r => {
                    this.isLoading = false;
                    if (r.isError) {
                        this.messagesService.showError(r.title, r.message);
                    }
                    else {
                        this.messagesService.showInfoMessage("Success", "Field Definition Edited");
                        this.onComplete.emit({ action: 'edit', field: this.model });
                    }
                });
        } else {
            this.fieldsService.postFieldType(this.model)
                .then(r => {                    
                    this.isLoading = false;
                    if (r.isError) {
                        this.messagesService.showError(r.title, r.message);
                    }
                    else {
                        this.messagesService.showInfoMessage("Success", "Field Definition Created");
                        this.onComplete.emit({ action: 'add', field: this.model });
                    }
                });
        }
    }

    changeRefType(item: FieldTypeRelationItemEditorModel, selected: string = null): Promise<any> {
        item.relationsLoading = true;
        item.DisplayFields = [];
        item.selectedRelationItemID = selected;
        console.log('changeRefType()');
        console.log(item);

        if (item.IntersectType == null)
            item.IntersectType = parseInt(item.selectedRelationItemID.split('|')[0]);
        if (item.Object == null)
            item.Object = item.selectedRelationItemID.split('|')[1];
        if (item.ObjectID == null)
            item.ObjectID = parseInt(item.selectedRelationItemID.split('|')[2]);

        switch (item.ReferenceType.toString()) {
            case ComplexLookupRelationType.ChildItem.toString(): //child item
                return this.fieldsService.getStandardRelations(item.Object, item.ObjectID)
                    .then(z => {
                        item.relationItems = z;
                        console.log('z', z);
                        
                        item.relationItems.forEach(i => {
                            i.value = i.IntersectTypeID + '|' + i.TargetType + '|' + i.TargetTypeID;
                            i.label = i.TargetName;
                            item.relationsLoading = false;
                        });
                    });
            case ComplexLookupRelationType.ChildRelationship.toString(): //child relationship
                return this.fieldsService.getRelationLookupChildIntersectTypes(item.IntersectType).then(z => {
                    item.relationItems = z;
                    item.relationsLoading = false;
                });
            case ComplexLookupRelationType.ParentItem.toString():
                return this.fieldsService.getStandardRelations(item.Object, item.ObjectID)
                    .then(z => {
                        item.relationItems = z;
                        item.relationItems.forEach(i => {
                            i.value = i.IntersectTypeID + '|' + i.TargetType + '|' + i.TargetTypeID;
                            i.label = i.TargetName;
                            item.relationsLoading = false;
                        });
                    });
            case ComplexLookupRelationType.StandardRelationhip.toString():
                return this.fieldsService.getStandardRelations(item.Object, item.ObjectID)
                    .then(z => {
                        //console.log('relationItems');
                        //console.log(z);
                        item.relationItems = z;
                        item.relationItems.forEach(i => {
                            i.value = i.IntersectTypeID + '|' + i.TargetType + '|' + i.TargetTypeID;
                            i.label = i.TargetName;
                        });
                        
                    }).then(() => item.relationsLoading = false);
        }
    }

    changeRel(item: FieldTypeRelationItemEditorModel): Promise<any> {
        //console.log(item.selectedRelationItemID);

        let params = [];
        //item.DisplayFields = [];
        if (item.selectedRelationItemID) {
            params = item.selectedRelationItemID.split('|');
        } else {
            params.push(item.IntersectType);
            params.push(item.Object);
            params.push(item.ObjectID);
            item.selectedRelationItemID = item.IntersectType + '|' + item.Object + '|' + item.ObjectID;
        }
        

        try {
            if (params.length < 3)
                return;
            let id = parseInt(params[2]);
            let type = params[1];
            let intersectType = parseInt(params[0]);
            return this.fieldsService.getRelationLookupDisplayFields(id, type, intersectType)
                .then(r => {
                    //console.log('changeRel()');
                    //console.log(r);
                    r.forEach(i => {
                        let params = i.value.split('|');
                        let d = new FieldTypeItemDisplayFieldEditorModel();
                        d.FieldTypeID = parseInt(params[0]);
                        d.FieldTypeName = params[1];
                        d.Show = false;
                        d.FilterValue = "";
                        d.SortOrder = null;
                        d.value = i.value;
                        let e = item.DisplayFields.find(j => j.FieldTypeID == d.FieldTypeID && j.FieldTypeName == d.FieldTypeName);
                        if (e != null) {
                            //console.log('found matching display field');
                            e.Show = true;
                            e.value = i.value;
                        } else 
                            item.DisplayFields.push(d);
                    });

                    let s = [];
                    for (let i = 1; i <= item.DisplayFields.length; i++) {
                        item.DisplayFields[i - 1].DisplayOrder = i;
                        s.push({ id: i, text: i });
                    }
                    item.SortOrderList = s;

                });

        } catch (e) {
            return Promise.resolve();
        }
    }

    addRelation(item: FieldTypeRelationItemEditorModel) {
        let i = new FieldTypeRelationItemEditorModel();
        let params = item.selectedRelationItemID.split('|');
        let id = parseInt(params[2]);
        let type = params[1];
        let intersectType = parseInt(params[0]);


        i.ObjectID = id;
        i.Object = type;
        i.IntersectTypeID = intersectType;
        i.IntersectType = intersectType;
        i.displayValue = item.relationItems.find(i => i.value == item.selectedRelationItemID).label;

        this.model.RelationItems.push(i);
        this.relationItemCount = this.model.RelationItems.length;
    }

    deleteRelation(item: FieldTypeRelationItemEditorModel) {

        //only last item can be deleted
        this.model.RelationItems.pop();
        this.relationItemCount = this.model.RelationItems.length;
    }

    changeDisplayOrder(item: FieldTypeItemDisplayFieldEditorModel) {
       // console.log(item);
    }
}
