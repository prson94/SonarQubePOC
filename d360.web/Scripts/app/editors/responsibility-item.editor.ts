///<reference path="../es6-shim.d.ts"/>
import {Input, Output, Component, OnInit, EventEmitter } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';
import { ResponsibilityItem } from '../models/responsibility.model';
import { SelectItem, FormMessage } from '../models/form.model';
import { FormMessagePart } from '../parts/form-message.part';

@Component({
    selector: 'responsibility-item-editor',
    templateUrl: 'scripts/app/editors/responsibility-item.editor.html',
    viewProviders: [HTTP_PROVIDERS],
    directives: [FormMessagePart]
})

export class ResponsibilityItemEditor implements OnInit {
    @Input() item: ResponsibilityItem;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private responsibleObjectValue: string;

    private isLoading = false;
    private isSaving = false;

    private resources = new Array<SelectItem>();
    private responsibilityTypes = new Array<SelectItem>();
    private message: FormMessage = new FormMessage();
    private initialItem = new ResponsibilityItem();

    private http: Http;

    constructor(http: Http) {
        this.http = http;
    }

    ngOnInit() {
        this.responsibleObjectValue = this.item.ResponsibleObjectType + '|' + this.item.ResponsibleObjectID;
        this.initialItem = JSON.parse(JSON.stringify(this.item));

        if (this.item == null || (this.item.ResponsibilityID < 0 && !this.item.ObjectID && !this.item.ObjectType)) {
            throw new Error("responsibility-item-editor [item] requires either a ResponsibilityID or a ObjectID and ObjectType");
        }

        this.load();
    }

    private load(): void {

        if (this.item == null) {
            this.onLoadComplete.emit({ item: null });
            return;
        }
           
        this.isLoading = true;

        var url = '';

        if (this.item.ResponsibilityID != -1) 
            url = '/form/ResponsibilityItemEditor?id=' + this.item.ResponsibilityID;
        else
            url = '/form/ResponsibilityItemEditor?id=' + this.item.ObjectID + '&type=' + this.item.ObjectType;


        this.http.get(url)
            .map(data => data.json())
            .subscribe(data => {
                this.isLoading = false;

                this.resources = data.resources;
                this.responsibilityTypes = data.responsibilityTypes

                if (this.item.ResponsibilityID == -1) {
                    this.item.ObjectID = data.responsibility.ObjectID;
                    this.item.ObjectType = data.responsibility.ObjectType;
                }
                 

                this.onLoadComplete.emit({ item: this.item });
                //console.log(data);
                //console.log(this.item);
            });
    }

    private save(): void {
        this.isSaving = true;

        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        try {
            this.item.ResponsibleObjectID = parseInt(this.responsibleObjectValue.split('|')[1]);
            this.item.ResponsibleObjectType = this.responsibleObjectValue.split('|')[0];
        } catch (exception) {
            this.isSaving = false;
            this.message.Error("An error occurred while parsing the select item values.");
            this.revert();
            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
            return;
        }

        
        var res = this.resources.find(r => r.Value == this.responsibleObjectValue);
        var role = this.responsibilityTypes.find(r => r.Value == this.item.ResponsibilityTypeID.toString());

        if (res)
            this.item.ResponsibleObjectName = res.Text;
        if (role)
            this.item.Role = role.Text;


        var model = {
            ID: this.item.ResponsibilityID,
            ResponsibleObjectType: this.item.ResponsibleObjectType,
            ResponsibleObjectID: this.item.ResponsibleObjectID,
            ResponsibilityTypeID: this.item.ResponsibilityTypeID,
            Role: this.item.Role,
            ResponsibleObjectName: this.item.ResponsibleObjectName,
            Visible: this.item.Visible,
            ContextItems: null,
            ObjectType: this.item.ObjectType,
            ObjectID: this.item.ObjectID
        };



        this.http.post('/form/EditResponsibilityItemEditor', JSON.stringify(model), { headers: headers })
            .map(data => data.json())
            .subscribe(data => {
                //console.log(data);
                this.isSaving = false;
                this.message.Success("Save completed successfully.");
                this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
            });
            
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private revert(): void {
        //TODO: need a better way to do this. item is passed by reference and setting the row detail to a new item does not refresh the view
        // need to manually set each property back to initial state

        this.item.AssigningItemID = this.initialItem.AssigningItemID;
        this.item.AssigningItemType = this.initialItem.AssigningItemType;
        this.item.ContextItems = this.initialItem.ContextItems;
        this.item.ObjectID = this.initialItem.ObjectID;
        this.item.ObjectName = this.initialItem.ObjectName;
        this.item.ObjectType = this.initialItem.ObjectType;
        this.item.ObjectTypeID = this.initialItem.ObjectTypeID;
        this.item.ObjectTypeName = this.initialItem.ObjectTypeName;
        this.item.PrimaryOwnerResourceID = this.initialItem.PrimaryOwnerResourceID;
        this.item.PrimaryOwnerResourceName = this.initialItem.PrimaryOwnerResourceName;
        this.item.PrimaryOwnerResourceUrl = this.initialItem.PrimaryOwnerResourceUrl;
        this.item.ResponsibilityTypeID = this.initialItem.ResponsibilityTypeID;
        this.item.ResponsibleObjectID = this.initialItem.ResponsibleObjectID;
        this.item.ResponsibleObjectName = this.initialItem.ResponsibleObjectName;
        this.item.ResponsibleObjectType = this.initialItem.ResponsibleObjectType;
        this.item.ResponsibleObjectUrl = this.initialItem.ResponsibleObjectUrl;
        this.item.Role = this.initialItem.Role;
        this.item.Visible = this.initialItem.Visible;

    }
}
