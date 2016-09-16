import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter} from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { MessagesService, RelationshipsService} from '../../services/index';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-relationship-technical-relations',
    providers: [RelationshipsService],
    template: `                   
                <div>
                    <h4>Technical Relations for <em>{{objectName}}/{{relationship?.Name}}</em></h4>
                    <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                    <p-dataTable #dt [globalFilter]="gb"  scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="[5,10,20]" [value]="relations" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data;openFusionItem();">                                                                                                  
                        <p-column field="Name" header="Name" [sortable]="true" [style]="{'width':'250px'}"></p-column>                         
                        <p-column field="TypeName" header="Type" [sortable]="true" [style]="{'width':'250px'}"></p-column>                         
                    </p-dataTable>
                </div>
                `
})

export class RelationshipTechnicalRelationsComponent extends BaseComponent implements OnChanges {
    @Input() relationship: any;
    @Input() objectName: string;

    private relations: any[] = [];
    private selected: any;

    constructor(protected router: Router, protected relationshipsService: RelationshipsService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.relationship) this.load();
    }
    
    private load() {
        this.isLoading = true;
        this.relationshipsService.getTechnicalRelationships('Intersect', this.relationship.ID).
            then(res => {
                this.relations = res;
                this.isLoading = false;
            });
    }

    private openFusionItem() {
        if (!this.selected) return;
        
        // update to use new page once we create it...
        window.location.href = this.selected.Url;

        //this.router.navigateByUrl
    }
}


