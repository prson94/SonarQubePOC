///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit } from '@angular/core';
import { NgSwitch, NgSwitchDefault, NgSwitchCase } from '@angular/common';
import { FormMode } from '../../models/form.model';
import { AttributeHeirarchyItem } from '../../models/object-detail.model';
import { ObjectDetailService } from '../../services/object-detail.service';


@Component({
    selector: 'd3s-attributes-tile',
    template: `
<div *ngIf="isLoading">
    <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
</div>
<div *ngIf="!isLoading">
    <div class="row">
        <div class="col l5 m5 s6" [class]="readonly ? 'col s12' : 'col l5 m5 s6'">
            
        </div>
        <div *ngIf="!readonly" class="col l7 m7 s6">
            <div [ngSwitch]="formMode">
                <div *ngSwitchDefault>
                    
                </div>
            </div>
        </div>
    </div>

</div>
`,
    directives: [NgSwitch, NgSwitchCase, NgSwitchDefault],
    providers: [ObjectDetailService],
})

export class AttributesTile implements OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean = true;
    @Output() itemCount: number = 0;

    private isLoading = false;
    private formMode = FormMode.Default;
    private FormMode = FormMode;

    private items: AttributeHeirarchyItem[];
    private selectedItem: AttributeHeirarchyItem;

    constructor(private objectDetailService: ObjectDetailService) {
    }

    ngOnInit() {
        this.load();
    }


    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;

        this.objectDetailService.getAttributeHierarchyItems(this.objectID, this.objectType)
            .then(d => {
                this.items = d;
                this.itemCount = this.items.length;
                console.log(this.items);
                this.isLoading = false;
            });
    }

    add() {
        this.formMode = FormMode.Default;
    }

    edit() {
        this.formMode = FormMode.Default;
    }

    delete() {
        this.formMode = FormMode.Default;
    }

    save() {
        if (this.formMode == FormMode.Adding) {

        } else if (this.formMode == FormMode.Editing) {

        }
        this.formMode = FormMode.Default;


    }
}
