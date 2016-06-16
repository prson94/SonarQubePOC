///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit, EventEmitter } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { ResponsibilityItem, ResponsibilityContextItem } from '../../models/responsibility.model';
import { SelectItem, FormMessage, FormHelper } from '../../models/form.model';
import { FormMessagePart } from '../parts/form-message.part';
import { Dropdown, Button, MultiSelect, Checkbox } from 'primeng/primeng';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-responsibility-item-form',
    templateUrl: 'scripts/app/components/forms/responsibility-item.form.html',
    viewProviders: [ HTTP_PROVIDERS ],
    directives: [ FormMessagePart, Dropdown, Button, Checkbox, MultiSelect ]
})

export class ResponsibilityItemForm implements OnInit {
    @Input() item: ResponsibilityItem;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private responsibilitySelectList: SelectItem[];
    private responsibilitySelectedItem: string;
    private resourceSelectList: SelectItem[];
    private resourceSelectedItem: string;
    private contextsSelectList: SelectItem[];
    private contextsSelectedItems: string[];


    private isLoading = false;
    private isSaving = false;

    private message: FormMessage = new FormMessage();
    private initialItem = new ResponsibilityItem();


    private http: Http;

    constructor(http: Http) {
        this.http = http;
    }

    ngOnInit() {
        this.initialItem = _.cloneDeep(this.item);

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

        this.http.get(`form/Responsibility?responsibilityID=${this.item.ResponsibilityID}&id=${this.item.ObjectID}&type=${this.item.ObjectType}`)
            .map(data => data.json())
            .subscribe(data => {
                this.isLoading = false;

                this.resourceSelectList = data.resources;
                FormHelper.mapSelectItems(this.resourceSelectList);
                if (data.responsibility.ResponsibleObjectType)
                    this.resourceSelectedItem = data.responsibility.ResponsibleObjectType + '|' + data.responsibility.ResponsibleObjectID;
                else
                    this.resourceSelectedItem = this.resourceSelectList[0].value;


                this.responsibilitySelectList = data.responsibilityTypes;
                FormHelper.mapSelectItems(this.responsibilitySelectList);
                if (data.responsibility.ResponsibilityTypeID)
                    this.responsibilitySelectedItem = data.responsibility.ResponsibilityTypeID;
                else
                    this.responsibilitySelectedItem = this.responsibilitySelectList[0].value;

                this.contextsSelectList = data.contexts;
                FormHelper.mapSelectItems(this.contextsSelectList);
                this.contextsSelectedItems = this.contextsSelectList.filter(c => c.Selected).map(c => c.value);
                
                if (!this.item.ResponsibilityID) {
                    this.item.ObjectID = data.responsibility.ObjectID;
                    this.item.ObjectType = data.responsibility.ObjectType;
                }

                this.onLoadComplete.emit({ item: this.item });

            });
    }

    private save(): void {
        this.isSaving = true;

        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        console.log(this.responsibilitySelectedItem);
        console.log(this.resourceSelectedItem);

        try {
            this.item.ResponsibleObjectType = this.resourceSelectedItem.split('|')[0];
            this.item.ResponsibleObjectID = parseInt(this.resourceSelectedItem.split('|')[1]);
            this.item.ResponsibilityTypeID = parseInt(this.responsibilitySelectedItem);
        } catch (exception) {
            this.isSaving = false;
            this.message.Error("An error occurred while parsing the select item values.");
            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
            return;
        }
        var contextItems = new Array<ResponsibilityContextItem>();
        this.contextsSelectedItems.forEach(c => {
            contextItems.push({
                ResponsibiltyID: 0,
                ObjectID: parseInt(c),
                ObjectType: "DomainType"
            });
        });


        console.log(this.resourceSelectedItem);
        this.item.ResponsibilityContextItems = contextItems;
        this.item.ContextItems = this.contextsSelectList.filter(c => this.contextsSelectedItems.indexOf(c.value) != -1).map(c => c.label).join('; ');
        this.item.Role = this.responsibilitySelectList.find(r => r.value == this.responsibilitySelectedItem).label;
        this.item.ResponsibleObjectName = this.resourceSelectList.find(r => r.value == this.resourceSelectedItem).label;

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
            ObjectID: this.item.ObjectID,
            ResponsibilityContextItems: contextItems
        };

        this.http.post('/form/Responsibility', JSON.stringify(model), { headers: headers })
            .map(data => data.json())
            .subscribe(data => {
                this.isSaving = false;
                this.message.Success("Save completed successfully.");
                this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
            });
             
    }

    private cancel(): void {
        this.onCancel.emit({ item: this.initialItem });
    }
}
