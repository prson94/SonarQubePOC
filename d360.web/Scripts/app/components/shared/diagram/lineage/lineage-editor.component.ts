import * as go from 'gojs';
import {
    Component,
    Input,
    EventEmitter,
    Output
} from '@angular/core';

import {LineageNode} from '../../../../models/lineage.model';

import {LineageService} from '../../../../services/lineage.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-lineage-editor',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading && node != null">
            <div *ngIf="node.category == 'object' || node.category == 'focal'">
                <ng-container *ngIf="node.object != null && node.objectId != null; else chooseObject">
                    <div class="row"
                         style="padding-bottom: 10px;">
                        <div class="col s12">
                            <div style="font-weight: bold; display: inline-block">
                                {{node.name}}
                                <span style="font-size: .7rem; font-weight: normal">{{node.objectTypeName}}</span>
                                <span *ngIf="node.isRequired == true"
                                      style="font-size: .7rem; font-weight: normal"> (Required)</span>
                            </div>
                            <div *ngIf="node != null && node.key != null && node.key.toString().indexOf('-') == 0"
                                 style="float: right">
                                <button pButton
                                        type="button"
                                        label="Change Object"
                                        (click)="edit(node)"></button>
                            </div>
                        </div>
                    </div>
                </ng-container>
                <ng-template #chooseObject>
                    <div class="row"
                         style="padding-bottom: 10px;">
                        <div class="col s12">
                            <div class="FieldName">
                                Choose {{node.objectTypeName}}  {{(node.isRequired != null && node.isRequired == true) ? '(Required)' : ''}}
                            </div>
                            <div>
                                <p-autoComplete
                                        field="Name"
                                        dataKey="ID"
                                        (completeMethod)="search($event, node.objectType, node.objectTypeId)"
                                        [suggestions]="suggestions"
                                        forceSelection="true"
                                        (onSelect)="selected = $event"
                                        [style]="{'width':'80%'}"
                                        [inputStyle]="{'width':'100%'}">
                                </p-autoComplete>

                                <button pButton
                                        type="button"
                                        label="Choose"
                                        (click)="selectObject(node)"></button>
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
                    <textarea pInputTextarea
                              [ngModel]="node.businessTransformation"
                              (ngModelChange)="node.businessTransformation = $event; nodeChange.emit(node)"></textarea>
                </div>
                <div class="FieldName">
                    Technical Transformation
                </div>
                <div>
                    <textarea pInputTextarea
                              [ngModel]="node.technicalTransformation"
                              (ngModelChange)="node.technicalTransformation = $event; nodeChange.emit(node)"></textarea>
                </div>
            </div>
            <div *ngIf="node.category == 'map'">
                <div *ngFor="let o of objects">
                    <ng-container *ngIf="o.object != null && o.objectId != null; else chooseObjectMulti">
                        <div class="row"
                             style="padding-bottom: 10px;">
                            <div class="col s12">
                                <div style="font-weight: bold; display: inline-block">
                                    {{o.name}}
                                    <span style="font-size: .7rem; font-weight: normal">{{o.objectTypeName}}</span>
                                    <span *ngIf="o.isRequired == true"
                                          style="font-size: .7rem; font-weight: normal"> (Required)</span>
                                </div>
                                <div *ngIf="o != null && o.key != null && o.key.toString().indexOf('-') == 0"
                                     style="float: right">
                                    <button pButton
                                            type="button"
                                            label="Change Object"
                                            (click)="edit(o)"></button>
                                </div>
                            </div>
                        </div>
                    </ng-container>
                    <ng-template #chooseObjectMulti>
                        <div class="row"
                             style="padding-bottom: 10px;">
                            <div class="col s12">
                                <div class="FieldName">
                                    Choose {{o.objectTypeName}} {{(o.isRequired != null && o.isRequired == true) ? '(Required)' : ''}}
                                </div>
                                <div>
                                    <p-autoComplete
                                            field="Name"
                                            dataKey="ID"
                                            (completeMethod)="search($event, o.objectType, o.objectTypeId)"
                                            [suggestions]="suggestions"
                                            forceSelection="true"
                                            (onSelect)="selected = $event"
                                            [style]="{'width':'80%'}"
                                            [inputStyle]="{'width':'100%'}">
                                    </p-autoComplete>

                                    <button pButton
                                            type="button"
                                            label="Choose"
                                            (click)="selectObject(o)"></button>
                                </div>
                            </div>
                        </div>
                    </ng-template>
                </div>
            </div>
        </div>
    `,
    providers: [LineageService]
})

export class LineageEditorComponent {
    @Input() diagram: go.Diagram = null;
    @Input() node: LineageNode = null;
    @Output() nodeChange = new EventEmitter();

    private suggestions = [];
    private objects = [];
    private selected;

    isLoading = false;

    constructor(private lineageService: LineageService) {
    }

    search(
        e: any,
        objectType: string,
        objectTypeId: number
    ) {
        this.lineageService.queryObjectTypes(objectType, objectTypeId, e.query).subscribe(
            s => {
                this.suggestions = s;
            }
        );
    }

    selectObject(node: LineageNode) {
        node.object = this.selected.Object;
        node.objectId = this.selected.ObjectID;
        node.name = this.selected.Name;
        this.nodeChange.emit(node);
    }

    edit(node: LineageNode) {
        node.object = null;
        node.objectId = null;
        this.nodeChange.emit(node);
    }
}
