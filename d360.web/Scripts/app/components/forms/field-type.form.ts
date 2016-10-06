
import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem } from 'primeng/primeng';
import { FieldType, FieldTypeEditorModel, Lookups, FieldTypeFusionItemEditorModel, FieldTypeFusionLookupDisplayField } from '../../models/fields.model';
import { FieldsService } from '../../services/fields.service';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-field-type-form',
    templateUrl: './field-type.form.html',
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

    constructor(private fieldsService: FieldsService) {
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

        if (this.model.FieldType.ID > 0) {
            this.fieldsService.putFieldType(this.model)
                .then(r => {
                    this.onComplete.emit({ action: 'edit', field: this.model });
                });
        } else {
            this.fieldsService.postFieldType(this.model)
                .then(r => {                    
                    this.onComplete.emit({ action: 'add', field: this.model });
                });
        }
    }
}
