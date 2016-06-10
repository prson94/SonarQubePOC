
import { Component } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { TemplatesService } from '../../services/templates.service';
import { Template } from '../../models/template.model';
import {DataTable, Column, Button} from 'primeng/primeng';



@Component({
    selector: 'admin-templates',
    template: `<div class="row">
                <div class="col s12">                    
                    <div class="tile tile-detail">
                        <header>Tooltip Templates</header>
                        <p-dataTable [value]="templates">
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="true" [style]="{width : '150px' }"></p-column>
                            <p-column field="Action" header="Action" [sortable]="true" [filter]="true" [style]="{width : '100px' }"></p-column>
                            <p-column field="Description" header="Description" [sortable]="true" [filter]="true"></p-column>                    
                            <p-column [style]="{width : '120px' }">
                                <template let-template="rowData">
                                    <button pButton type="text" (click)="deleteTemplate(template)" icon="fa fa-trash-o"></button>
                                    <button pButton type="text" (click)="editTemplate(template)" icon="fa fa-pencil"></button>
                                </template>
                            </p-column>
                        </p-dataTable>
                    </div>
                </div>  
               </div>
                `,
    providers: [TemplatesService],
    directives: [DataTable, Column, Button]
})

export class AdminTemplatesComponent {
    pageHeader: PageHeader;
    templateService: TemplatesService;
    templates: Template[];
    error: any;
    
    constructor(pageHeader: PageHeader, templateService: TemplatesService) {
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Templates';
        this.pageHeader.description = 'All email and tooltip templates for notifications.';
        this.templateService = templateService;        
    }

    ngOnInit() {
        this.getTemplates();
    }

    getTemplates() {        
        this.templateService
            .getTemplates()
            .then(templates => this.templates = templates)
            .catch(error => this.error = error); // TODO: Display error message
    }

    deleteTemplate(template: Template) {
        this.templateService.deleteTemplate(template);
    }

    editTemplate(template: Template) {
        this.templateService.putTemplate(template);
    }
};
