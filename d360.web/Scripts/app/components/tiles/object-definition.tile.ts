import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { ObjectDetail } from '../../models/object-detail.model';
import { BaseComponent } from '../shared/base.component';
import { Permission } from '../../models/permission.model'

@Component({
    selector: 'd3s-object-definition-tile',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div *ngIf="!showEditor && !isLoading">
                        <header>&nbsp;<d3s-tile-actions [hasEdit]="hasRootUpdatePermissions()" (editClick)="showEditor=true"></d3s-tile-actions></header>
                        <simple-accordion header="Definition" [active]="true">
                            <object-detail [objectID]="objectID" [objectType]="objectType"></object-detail>
                        </simple-accordion>
                        <simple-accordion header="Synonyms ({{synonyms.itemCount}})" [active]="false" *ngIf="hasSynonyms">
                            <d3s-synonyms-tile #synonyms [objectID]="objectID" [objectType]="objectType" [readonly]="false" [hasAdd]="hasRelationshipCreatePermissions()" [hasDelete]="hasRelationshipDeletePermissions()"></d3s-synonyms-tile>
                        </simple-accordion>
                        <simple-accordion header="Attributes ({{attributes.itemCount}})" [active]="false" *ngIf="hasAttributes">
                            <d3s-attributes-tile #attributes [objectID]="objectID" [objectType]="objectType" [readonly]="false" [hasAdd]="hasAttributeCreatePermissions()" [hasEdit]="hasAttributeUpdatePermissions()" [hasDelete]="hasAttributeDeletePermissions"></d3s-attributes-tile>
                        </simple-accordion>
                     <!--   <simple-accordion header="Structure" [active]="false">
                            <d3s-structure-tile [objectID]="objectID" [objectType]="objectType" [readonly]="false"></d3s-structure-tile>
                        </simple-accordion>-->
            </div>
            <d3s-dynamic-editor *ngIf="showEditor"
                                            [objectID]="objectID" 
                                            [parentID]="object?.ParentID" 
                                            [objectType]="objectType" 
                                            [selection]="object"
                                            [editUri]="'form/dynamicedit/edit/' + objectType"
                                            [title]="object?.Name" 
                                            (saveClick)="save($event)" 
                                            (closeClick)="showEditor=false">
            </d3s-dynamic-editor>
            `,
    providers: [ObjectDetailService],
})

export class ObjectDefinitionTile extends BaseComponent implements OnChanges {
    @Input() objectID: number;
    @Input() objectType: string;
    
    @Input() hasSynonyms: boolean = true;
    @Input() hasAttributes: boolean = true;

    @Output() onEditComplete = new EventEmitter();
    
    private object: ObjectDetail = null;

    private showEditor: boolean = false;;
    

    //ideally base permissions would be an input but angular doesnt support this yet
    @Input() objectPermissions: Permission[] = [];

    constructor(private objectDetailService: ObjectDetailService) {
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
            .then(r => {
                this.object = r;
                this.isLoading = false;
            });
    }

    save(e): void {
        this.load().then(() => {
            this.onEditComplete.emit(this.object);            
            this.showEditor = false;
        });
    }
    
}
