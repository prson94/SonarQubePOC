import { Input, Output, Component, OnInit, EventEmitter } from '@angular/core';
import { ResponsibilityItem, ResponsibilityContextItem, ResponsibilityEditorModel } from '../../models/responsibility.model';
import { FormMessage, FormHelper } from '../../models/form.model';
import { SelectItem } from 'primeng/primeng';
import { MessagesService, ResponsibilityService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-responsibility-item-form',
    templateUrl: './responsibility-item.form.html',
    providers: [ ResponsibilityService ],
})

export class ResponsibilityItemForm extends BaseComponent implements OnInit {
    @Input() item: ResponsibilityItem;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private model: ResponsibilityEditorModel;
    
    private message: FormMessage = new FormMessage();
    private initialItem = new ResponsibilityItem();

    private showVisible: boolean = false;

    constructor(private responsibilityService: ResponsibilityService, private messagesService: MessagesService) {
        super();
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

        this.responsibilityService.getResponsibilityItemEditor(this.item.ObjectID, this.item.ObjectType, this.item.ID)
            .then(data => {                
                this.model = data;
                if (!this.item.ID) {
                    this.item.ObjectID = data.responsibility.ObjectID;
                    this.item.ObjectType = data.responsibility.ObjectType;
                }
                this.showVisible = this.item.ObjectType.toLowerCase().endsWith('type');
                this.item.Visible = data.responsibility.Visible;
                this.onLoadComplete.emit({ item: this.item });
                this.isLoading = false;
            });
    }

    private save(): void {        
        try {
            this.item.ResponsibleObjectType = this.model.selectedResource.split('|')[0];
            this.item.ResponsibleObjectID = parseInt(this.model.selectedResource.split('|')[1]);
            this.item.ResponsibilityTypeID = parseInt(this.model.selectedResponsibilityType);

        } catch (exception) {            
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

        this.isLoading = true;
        this.responsibilityService.postResponsibility(this.item)
            .then(data => {
                this.showMessageForResult(this.messagesService, data);
                this.isLoading = false;                
                this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.initialItem });
            });
    }

    private cancel(): void {
        this.onCancel.emit({ item: this.initialItem });
    }
}
