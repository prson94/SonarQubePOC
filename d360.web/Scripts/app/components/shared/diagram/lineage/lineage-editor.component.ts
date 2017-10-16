import { Component, Input, OnInit, OnChanges, EventEmitter, Output, OnDestroy } from '@angular/core';
import { LineageService } from '../../../../services/lineage.service';
import { NodeModelV2 } from '../../../../models/lineage.model';

@Component({
    selector: 'd3s-lineage-editor',
    template: `
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading && node != null">
    <div *ngIf="node.category == 'object' || node.category == 'focal'">
        <ng-container *ngIf="node.object != null && node.objectId != null; else chooseObject">
            <div style="font-weight: bold">
                {{node.name}}
            </div>
            <div *ngIf="node != null && node.key != null && node.key.indexOf('-') == 0">
                <button pButton type="button" label="Change Object" (click)="edit()"></button>
            </div>
        </ng-container>
        <ng-template #chooseObject>
            <div class="row">
                <div class="col s12">
                    <div class="FieldName">
                        Choose an object
                    </div>
                    <div>
                        <p-autoComplete 
                            field="Name"  
                            dataKey="ID" 
                            (completeMethod)="search($event)" 
                            [suggestions]="suggestions"
                            forceSelection="true"
                            (onSelect)="selected = $event"
                            [style]="{'width':'80%'}"
                            [inputStyle]="{'width':'100%'}">
                        </p-autoComplete>
                        
                        <button pButton type="button" label="Choose" (click)="selectObject()"></button>
                    </div>
                </div>
            </div>
        </ng-template>
    </div>
    <div *ngIf="node.category == 'transform'">
        <div class="FieldName">
            Business Transformation
        </div>
        <div>
            <textarea pInputTextarea [ngModel]="node.businessTransformation" (ngModelChange)="node.businessTransformation = $event; nodeChange.emit(node)"></textarea>
        </div>
        <div class="FieldName">
            Technical Transformation
        </div>
        <div>
            <textarea pInputTextarea [ngModel]="node.technicalTransformation" (ngModelChange)="node.technicalTransformation = $event; nodeChange.emit(node)"></textarea>
        </div>
    </div>
</div>
    `,
    providers: [LineageService]
})

export class LineageEditorComponent implements OnInit, OnChanges, OnDestroy {
    @Input() node: NodeModelV2 = null;
    @Output() nodeChange = new EventEmitter();

    private suggestions = [];
    private objects = [];
    private selected;
    isLoading = false;

    constructor(private lineageService: LineageService) { }

    ngOnChanges() {

    }

    ngOnInit() {

    }

    ngOnDestroy() {
    }


    search(e: any) {
        this.lineageService.queryObjectTypes(this.node.objectType, this.node.objectTypeId, e.query).subscribe(s => {
            this.suggestions = s;
        });
    }

    selectObject() {
        this.node.object = this.selected.Object;
        this.node.objectId = this.selected.ObjectID;
        this.node.name = this.selected.Name;
        this.nodeChange.emit(this.node);
    }

    edit() {
        this.node.object = null;
        this.node.objectId = null;
    }
}