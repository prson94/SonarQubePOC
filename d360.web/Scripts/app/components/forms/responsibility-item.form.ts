///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit, EventEmitter } from '@angular/core';
import { ResponsibilityItem, ResponsibilityContextItem, ResponsibilityEditorModel } from '../../models/responsibility.model';
import { FormMessage, FormHelper } from '../../models/form.model';
import { FormMessagePart } from '../parts/form-message.part';
import { Dropdown, Button, MultiSelect, Checkbox, SelectItem } from 'primeng/primeng';
import { ResponsibilityService } from '../../services/responsibility.service';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-responsibility-item-form',
    templateUrl: 'scripts/app/components/forms/responsibility-item.form.html',
    providers: [ ResponsibilityService ],
    directives: [ FormMessagePart, Dropdown, Button, Checkbox, MultiSelect ]
})

export class ResponsibilityItemForm implements OnInit {
    @Input() item: ResponsibilityItem;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private model: ResponsibilityEditorModel;

    private isLoading = false;
    private isSaving = false;

    private message: FormMessage = new FormMessage();
    private initialItem = new ResponsibilityItem();


    constructor(private respsonsibilityService: ResponsibilityService) {
    }

    ngOnInit() {
        this.initialItem = _.cloneDeep(this.item);

        if (this.item == null || (this.item.ID < 0 && !this.item.ObjectID && !this.item.ObjectType)) {
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

        this.respsonsibilityService.getResponsibilityItemEditor(this.item.ObjectID, this.item.ObjectType, this.item.ID)
            .then(data => {
                this.model = data;
                if (!this.item.ID) {
                    this.item.ObjectID = data.responsibility.ObjectID;
                    this.item.ObjectType = data.responsibility.ObjectType;
                }
                this.item.Visible = data.responsibility.Visible;
                this.onLoadComplete.emit({ item: this.item });
                this.isLoading = false;
            });
    }

    private save(): void {
        this.isSaving = true;

        try {
            this.item.ResponsibleObjectType = this.model.selectedResource.split('|')[0];
            this.item.ResponsibleObjectID = parseInt(this.model.selectedResource.split('|')[1]);
            this.item.ResponsibilityTypeID = parseInt(this.model.selectedResponsibilityType);
        } catch (exception) {
            this.isSaving = false;
            this.message.Error("An error occurred while parsing the select item values.");
            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
            return;
        }

        var contextItems = new Array<ResponsibilityContextItem>();
        this.model.selectedContexts.forEach(c => {
            contextItems.push({
                ResponsibiltyID: 0,
                ObjectID: parseInt(c),
                ObjectType: "DomainType"
            });
        });

        this.item.ResponsibilityContextItems = contextItems;
        this.item.ContextItems = this.model.contexts.filter(c => this.model.selectedContexts.findIndex(x => x == c.value) > -1).map(c => c.label).join('; ');
        this.item.Role = this.model.responsibilityTypes.find(r => r.value == this.model.selectedResponsibilityType).label;
        this.item.ResponsibleObjectName = this.model.resources.find(r => r.value == this.model.selectedResource).label;

        this.item.ContextItems = null;
        this.item.ResponsibilityContextItems = contextItems;
        

        this.respsonsibilityService.postResponsibility(this.item)
            .then(data => {
                this.isSaving = false;
            });
    }

    private cancel(): void {
        this.onCancel.emit({ item: this.initialItem });
    }
}
