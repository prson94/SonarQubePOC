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

                        this.model.RelationItems.forEach(r => {
                            let intersectType = this.lookups.IntersectTypes.find(i => i.id == r.IntersectType.toString());
                            r.Object = intersectType.value.split('|')[1];
                            r.ObjectID = parseInt(intersectType.value.split('|')[2]);

                            r.DisplayFields.forEach(d => {
                                d.FieldTypeID = parseInt(d.value.split('|')[0]);
                                d.FieldTypeName = d.value.split('|')[1];
                            });
                        });
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
                            console.log(this.relationItemCount);
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

    changeRefType(item: FieldTypeRelationItemEditorModel) {
        item.relationsLoading = true;
        item.DisplayFields = [];
        item.selectedRelationItemID = null;
        console.log(item);
        switch (item.ReferenceType.toString()) {
            case ComplexLookupRelationType.ChildItem.toString(): //child item
                item.relationsLoading = false;
                break;
            case ComplexLookupRelationType.ChildRelationship.toString(): //child relationship
                this.fieldsService.getRelationLookupChildIntersectTypes(item.IntersectType).then(z => {
                    item.relationItems = z;
                    item.relationsLoading = false;
                });
                break;
            case ComplexLookupRelationType.ParentItem.toString():
                item.relationsLoading = false;
                break;
            case ComplexLookupRelationType.StandardRelationhip.toString():
                this.fieldsService.getStandardRelations(item.Object, item.ObjectID)
                    .then(z => {
                        console.log(z);
                        item.relationItems = z;
                        item.relationItems.forEach(i => {
                            i.value = i.IntersectTypeID + '|' + i.TargetType + '|' + i.TargetTypeID;
                            i.label = i.TargetName;
                        });
                        item.relationsLoading = false;
                    });
                break;
        }
    }

    changeRel(item: FieldTypeRelationItemEditorModel) {
        console.log(item.selectedRelationItemID);

        item.DisplayFields = [];
        let params = item.selectedRelationItemID.split('|');

        try {
            if (params.length < 3)
                return;
            let id = parseInt(params[2]);
            let type = params[1];
            let intersectType = parseInt(params[0]);
            this.fieldsService.getRelationLookupDisplayFields(id, type, intersectType)
                .then(r => {
                    console.log(r);
                    item.DisplayFields = [];
                    r.forEach(i => {
                        let params = i.value.split('|');
                        let d = new FieldTypeItemDisplayFieldEditorModel();
                        d.FieldTypeID = parseInt(params[0]);
                        d.FieldTypeName = params[1];
                        d.Show = false;
                        d.FilterValue = "";
                        d.SortOrder = null;
                        d.value = i.value;
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
            return;
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
        console.log(item);
    }
}
