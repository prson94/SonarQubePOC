import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SelectItem } from 'primeng/primeng';
import { TaxonomiesService } from '../../../services/taxonomies.service';
import { ObjectStyleService } from '../../../services/object-style.service';
import { Taxonomy, TaxonomyClassification } from '../../../models/taxonomy.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-model-editor',
    template: ` 
                <header>{{action}} Model</header>
                <form (ngSubmit)="onSubmit()" #modelForm="ngForm">
                <div class="row">
                    <div class="col l6 s12">
                        <div class="FieldName">Name</div>
                        <div><input required type="text" name="name" pInputText [(ngModel)]="editedTaxonomy.Name" style="width: 100%;" #name="ngModel" maxlength="250" /></div>
                        <div *ngIf="name.errors && (name.dirty || name.touched)"
                             class="alert alert-danger">
                            <div [hidden]="!name.errors.required" style="color: maroon">
                              A Model name is required
                            </div>                            
                            <div [hidden]="!name.errors.maxlength" style="color: maroon">
                              A Model name cannot be more than 250 characters long.
                            </div>
                        </div>      
                    </div>
                    <div class="col l6 s12">
                        <div class="FieldName">Classification</div>
                        <div><p-dropdown required name="classification" [options]="classifications" [(ngModel)]="editedTaxonomy.Class" [style]="{width:'100%'}" #classification="ngModel"></p-dropdown></div>
                        <div [hidden]="classification.valid || classification.pristine" style="color: maroon">Model classification is required</div>
                    </div>
                    <div class="col l6 s12">
                        <div class="FieldName">Maximum Depth</div>
                        <div><input type="number" name="depth" [(ngModel)]="editedTaxonomy.MaximumDepth"  style="width:25%; height: 25px" /></div>
                    </div>                    
                    <div class="col l6 s12">
                        <div class="FieldName">Display Format</div>
                        <div><input type="text" name="format" [(ngModel)]="editedTaxonomy.DisplayFormat"  style="width:25%; height: 25px" /></div>
                    </div>                    
                    <div class="col s12">
                        <div class="FieldName">Description</div>
                        <p-editor [style]="{'height':'150px'}" name="description" [(ngModel)]="editedTaxonomy.Description"></p-editor>
                    </div>                    
                    <div class="col l6 s12">
                        <div class="FieldName">Background Color</div>
                        <p-colorPicker [(ngModel)]="editedTaxonomy.IconBackColor" name="iconbackcolor"></p-colorPicker>                         
                        <input type="text" [(ngModel)]="editedTaxonomy.IconBackColor" name="iconbackcolortext" style="padding:2px;" />
                    </div>
                    <div class="col l6 s12">
                        <div class="FieldName">Text Color</div>
                        <p-colorPicker [(ngModel)]="editedTaxonomy.IconForeColor" name="iconforecolor"></p-colorPicker>  
                        <input type="text" [(ngModel)]="editedTaxonomy.IconForeColor" name="iconforecolor" style="padding:2px;" />
                    </div>
                    <div class="col s12">
                        <div *ngIf="editedTaxonomy.IconForeColor == editedTaxonomy.IconBackColor" class="errorMessage">Foreground and background color cannot be the same</div>
                    </div>
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="submit" [disabled]="!modelForm.form.valid || (editedTaxonomy.IconForeColor == editedTaxonomy.IconBackColor)" label="Save" style="width: 150px;"></button>
                        <button pButton type="button" (click)="close()" label="Close" style="width: 150px;"></button>
                    </div>                    
                </div>
                </form>
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
        if (this.taxonomy != undefined)
            this.editedTaxonomy = _.cloneDeep(this.taxonomy);
        else {
            this.editedTaxonomy = new Taxonomy();            
            this.editedTaxonomy.MaximumDepth = 1;
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
                if (this.editedTaxonomy.Class == undefined && this.classifications.length > 0) this.editedTaxonomy.Class = this.classifications[0].value;
            })
            .catch(error => this.error = error);                
    }

    onSubmit() {
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