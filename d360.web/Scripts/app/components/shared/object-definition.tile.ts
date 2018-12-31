import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { ObjectDetail } from '../../models/object-detail.model';
import { BaseComponent } from '../shared/base.component';
import { NymType } from '../../models/object-detail.model';
import { ResponsibilityTypeRelationPermission } from '../../models/responsibility-type.model';
import { FormMode } from '../../models/form.model';

@Component({
    selector: 'd3s-object-definition-tile',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div *ngIf="formMode == FormMode.Default && !isLoading">
                        <header>&nbsp;<d3s-tile-actions [hasEdit]="hasModifyAssetPermissions()" (editClick)="formMode = FormMode.Editing;formModeChange.emit(formMode);"></d3s-tile-actions></header>
                        <simple-accordion header="Definition" [active]="true">
                            <object-detail [objectID]="objectID" [objectType]="objectType"></object-detail>
                        </simple-accordion>
                        <simple-accordion header="{{nym.Name}} ({{synonyms.itemCount}})" [active]="false" *ngFor="let nym of nymTypes">
                            <d3s-synonyms-tile #synonyms [predicateId]="nym.ID" [predicateName]="nym.Name" [objectID]="objectID" [objectType]="objectType" [readonly]="false" [hasAdd]="hasModifyRelationshipsPermissions()" [hasDelete]="hasDeleteRelationshipsPermissions()"></d3s-synonyms-tile>
                        </simple-accordion>
                        <simple-accordion header="Attributes ({{attributes.itemCount}})" [active]="false" *ngIf="hasAttributes">
                            <d3s-attributes-tile #attributes [objectID]="objectID" [objectType]="objectType" [readonly]="false" [hasAdd]="hasModifyAttributesPermissions()" [hasEdit]="hasModifyAttributesPermissions()" [hasDelete]="hasDeleteAttributesPermissions"></d3s-attributes-tile>
                        </simple-accordion>                     
            </div>
            <d3s-dynamic-editor *ngIf="formMode == FormMode.Editing"
                                            [objectID]="objectID" 
                                            [parentID]="object?.ParentID" 
                                            [objectType]="objectType" 
                                            [selection]="object"
                                            [editUri]="'form/dynamicedit/edit/' + objectType"
                                            [title]="object?.Name" 
                                            (saveClick)="save($event)" 
                                            (closeClick)="formMode = FormMode.Default;formModeChange.emit(formMode);">
            </d3s-dynamic-editor>
            `,
    providers: [ObjectDetailService],
})

export class ObjectDefinitionTile extends BaseComponent implements OnChanges {
    @Input() objectID: number;
    @Input() objectType: string;
        
    @Input() hasAttributes: boolean = true;
    @Input() nymTypes: NymType[] = [];

    @Output() onEditComplete = new EventEmitter();
    @Output() formModeChange = new EventEmitter();

    private formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    
    private object: ObjectDetail = null;

    //private showEditor: boolean = false;;
        
    @Input() objectPermissions: ResponsibilityTypeRelationPermission[] = [];

    constructor(private objectDetailService: ObjectDetailService, private headerActionsService: HeaderActionsService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        this.load();
    }

    load(): Promise<any> {
        // this is to workaround angular limitaiont with inputs in base classes
        this.permissions = this.objectPermissions;
        if (this.objectType == null || this.objectID == null)
            return Promise.resolve();

        this.isLoading = true;

        let type = (this.objectType.toLowerCase() == 'artifact') ? "1" : this.objectType;

        return this.objectDetailService.getObject(this.objectID, type)
            .then(r => {debugger;alert(1);
                this.object = r;
                this.isLoading = false;
            });
    }



    save(e): void {
        this.load().then(() => {
            this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was renamed
            this.onEditComplete.emit(this.object);            
           // this.showEditor = false;
            this.formMode = FormMode.Default;
            this.formModeChange.emit(this.formMode);
            
        });
    }
    
}
