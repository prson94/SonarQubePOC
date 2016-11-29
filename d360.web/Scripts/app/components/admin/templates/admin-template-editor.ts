import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { TemplatesService } from '../../../services/templates.service';
import { Template } from '../../../models/template.model';
import { SelectItem } from 'primeng/primeng';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-template-editor',
    template: ` 
                <header>{{action}} Template</header>
                <div class="row">
                    <form (ngSubmit)="update()" #templateForm="ngForm">                        
                    <div class="col l6 s12">
                        <div class="FieldName">Name</div>
                        <div><input name="name" required type="text" pInputText [(ngModel)]="editedTemplate.Name" style="width: 100%;" #name="ngModel"/></div>
                        <div [hidden]="name.valid || name.pristine">Template name is required</div>
                    </div>
                    <div class="col l6 s12">
                        <div class="FieldName">Action</div>
                        <div><p-dropdown name="actions" [options]="actions" [(ngModel)]="editedTemplate.Action" [style]="{width:'100%'}"></p-dropdown></div>
                    </div>
                    <div class="col s12">
                        <div class="FieldName">Description</div>
                        <p-editor name="description" [style]="{'height':'150px'}" [(ngModel)]="editedTemplate.Description"></p-editor>
                    </div>
                    <div class="col s12">
                        <div class="FieldName">Body</div>
                        <p-editor required #body="ngModel" name="body" [style]="{'height':'150px'}" [(ngModel)]="editedTemplate.TemplateBody"></p-editor>               
                        <div [hidden]="body.valid || body.pristine">Template body is required</div>
                    </div>
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="submit" [disabled]="!templateForm.form.valid" label="Save" style="width: '150px';"></button>
                        <button pButton type="button" (click)="close()" label="Close" style="width: '150px';"></button>
                    </div>                    
                    </form>
                </div>
                `,
    providers: [TemplatesService],
})
    
export class AdminTemplateEditorComponent {    
    @Input() template: Template;   
    @Output() closeClick = new EventEmitter();
    @Output() updateClick = new EventEmitter();
    action: string = "Edit";

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
        if (this.template != undefined)
            this.editedTemplate = _.cloneDeep(this.template);
        else {
            this.editedTemplate = new Template();
            this.action = "New";
        }
    }

    update()
    {
        this.updateClick.emit({ template: this.editedTemplate });
    }

    close()
    {
        this.closeClick.emit({ templateId: (this.template ? this.template.ID : -1) });
    }
};