///<reference path="../../../../node_modules/typings/index.d.ts"/>  

import { Input, Component, EventEmitter, Output } from '@angular/core';
import { TemplatesService } from '../../services/templates.service';
import { Template } from '../../models/template.model';
import {Button, Editor, InputText, Dropdown, SelectItem} from 'primeng/primeng';
import _ from 'lodash';

@Component({
    selector: 'd3s-admin-template-editor',
    template: ` 
                <div class="row">
                    <div class="col l6 s12">
                        <div class="FieldName">Name</div>
                        <div><input type="text" pInputText [(ngModel)]="editedTemplate.Name" style="width: 100%;" /></div>
                    </div>
                    <div class="col l6 s12">
                        <div class="FieldName">Action</div>
                        <div><p-dropdown [options]="actions" [(ngModel)]="editedTemplate.Action" [style]="{width:'100%'}"></p-dropdown></div>
                    </div>
                    <div class="col s12">
                        <div class="FieldName">Description</div>
                        <p-editor [style]="{'height':'150px'}" [(ngModel)]="editedTemplate.Description"></p-editor>
                    </div>
                    <div class="col s12">
                        <div class="FieldName">Body</div>
                        <p-editor [style]="{'height':'150px'}" [(ngModel)]="editedTemplate.TemplateBody"></p-editor>               
                    </div>
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="button" (click)="update()" label="Save" style="width: '150px';"></button>
                        <button pButton type="button" (click)="close()" label="Close" style="width: '150px';"></button>
                    </div>                    
                </div>
                `,
    providers: [TemplatesService],
    directives: [Button, Editor, InputText, Dropdown]
})
    
export class AdminTemplateEditorComponent {    
    @Input() template: Template;   
    @Output() closeClick = new EventEmitter();
    @Output() updateClick = new EventEmitter();
    

    editedTemplate: Template;

    actions: SelectItem[];
    
    constructor() {                
        this.actions = [];
        this.actions.push({ label:"Preview", value:"Preview" });
        this.actions.push({ label: "Assigning Item Preview", value: "AssigningItemPreview" });
        this.actions.push({ label: "Lookup Preview", value: "LookupPreview" });
        this.actions.push({ label: "View Statistics", value: "Statistics" });        
    }

    ngOnInit() {
        this.editedTemplate = _.cloneDeep(this.template);
    }

    update()
    {
        this.updateClick.emit({ template: this.editedTemplate });
    }

    close()
    {
        this.closeClick.emit({ templateId: this.template.ID });
    }
};