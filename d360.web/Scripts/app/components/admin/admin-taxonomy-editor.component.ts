///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output } from '@angular/core';
import { SelectItem } from 'primeng/primeng';
import { TaxonomiesService, ObjectStyleService } from '../../services/index';
import { Taxonomy, TaxonomyClassification } from '../../models/taxonomy.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-model-editor',
    template: ` 
                <header>{{action}} Model</header>
                <div class="row">
                    <div class="col l6 s12">
                        <div class="FieldName">Name</div>
                        <div><input type="text" pInputText [(ngModel)]="editedTaxonomy.Name" style="width: 100%;" /></div>
                    </div>
                    <div class="col l6 s12">
                        <div class="FieldName">Classification</div>
                        <div><p-dropdown [options]="classifications" [(ngModel)]="editedTaxonomy.Class" [style]="{width:'100%'}"></p-dropdown></div>
                    </div>
                    <div class="col l12 s12">
                        <div class="FieldName">Maximum Depth</div>
                        <div><p-spinner size="30" [(ngModel)]="editedTaxonomy.MaximumDepth" [min]="0" [max]="100"></p-spinner></div>
                    </div>                    
                    <div class="col s12">
                        <div class="FieldName">Description</div>
                        <p-editor [style]="{'height':'150px'}" [(ngModel)]="editedTaxonomy.Description"></p-editor>
                    </div>                    
                    <div class="col l6 s12">
                        <div class="FieldName">Background Color</div>
                        <div><input type="text" pInputText [(ngModel)]="editedTaxonomy.IconBackColor" style="width: 100%;" /></div>
                    </div>
                    <div class="col l6 s12">
                        <div class="FieldName">Text Color</div>
                        <div><input type="text" pInputText [(ngModel)]="editedTaxonomy.IconForeColor" style="width: 100%;" /></div>
                    </div>
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="button" (click)="update()" label="Save" style="width: '150px';"></button>
                        <button pButton type="button" (click)="close()" label="Close" style="width: '150px';"></button>
                    </div>                    
                </div>
                `,
    providers: [TaxonomiesService, ObjectStyleService],
})

export class AdminTaxonomyEditorComponent {
    @Input() taxonomy: Taxonomy;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedTaxonomy: Taxonomy;
    classifications: SelectItem[] = [];
    
    constructor(private taxonomiesService: TaxonomiesService, private objectStyleService: ObjectStyleService) {
        
    }

    ngOnInit() {
        console.log(this.taxonomy);
        if (this.taxonomy != undefined)
            this.editedTaxonomy = _.cloneDeep(this.taxonomy);
        else {
            this.editedTaxonomy = new Taxonomy();
            this.action = "New";
        }
        this.getClassifications();
        this.getStyle(this.editedTaxonomy);
    }

    getStyle(taxonomy: Taxonomy) {
        if (taxonomy != null && taxonomy.ID > 0) {
            this.objectStyleService.getObjectStyle(taxonomy.ID,"TaxonomyType")
                .then(style => {
                    this.editedTaxonomy.IconBackColor = style.IconBackColor;
                    this.editedTaxonomy.IconForeColor = style.IconForeColor;
                }).catch(error => this.error = error);
        }
        else {
            this.editedTaxonomy.IconBackColor = '#000000';
            this.editedTaxonomy.IconForeColor = '#ffffff';
        }
    }

    getClassifications() {
        this.taxonomiesService
            .getTaxonomyClassifications()
            .then(classifications => {
                this.classifications = [];
                for (let classification of classifications) {
                    if (classification.Name == this.editedTaxonomy.TaxonomyTypeClass) {
                        this.editedTaxonomy.Class = classification.ID;                        
                    }
                    this.classifications.push({
                        label: classification.Name, value: classification.ID
                    });
                }
            })
            .catch(error => this.error = error);                
    }

    update() {
        //update the text that goes with the classification
        this.editedTaxonomy.TaxonomyTypeClass = this.getClassificationName(this.editedTaxonomy.Class);
        
        this.saveClick.emit({ taxonomy: this.editedTaxonomy, action: this.editedTaxonomy.ID == undefined ? "new" : "edit" });
    }

    getClassificationName(id: number): string {
        for (var i = 0; i < this.classifications.length; i++) {
            if (this.classifications[i].value == Number(id)) return this.classifications[i].label;
        }
    }

    close() {
        this.closeClick.emit({ taxonomyId: (this.taxonomy ? this.taxonomy.ID : -1) });
    }
};