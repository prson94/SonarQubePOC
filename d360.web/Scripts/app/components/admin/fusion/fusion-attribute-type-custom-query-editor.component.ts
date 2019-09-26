import {Input, Component, EventEmitter, Output, OnInit} from '@angular/core';
import {FusionService} from '../../../services/fusion.service';
import {BaseComponent} from '../../shared/base.component';
import {FusionAttributeType, FusionAttributeTypeCustomQuery} from '../../../models/fusion.model';
import 'codemirror/mode/sql/sql.js';
import * as _ from 'lodash';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-fusion-attribute-type-custom-query-editor',
    template: `
        <header>{{action}} Query Override</header>
        <div *ngIf="isLoading">
            <div class="row">
                <div *ngIf="isLoading"
                     style="text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
            </div>
        </div>
        <div class="row"
             *ngIf="!isLoading">
            <form (ngSubmit)="onSubmit()"
                  #FusionAttributeTypeCustomQueryForm="ngForm">
                <div class="col s12">
                    <div class="FieldName">Attribute Type</div>
                    <div>
                        <select required
                                name="fat"
                                [(ngModel)]="editedOverride.FusionAttributeTypeID"
                                style="width:75%">
                            <option *ngFor="let i of attributeTypes"
                                    [value]="i.ID">{{i.TextPath}}</option>
                        </select>
                    </div>
                </div>
                <div class="col s12">&nbsp;</div>
                <div class="col s12">
                    <div class="FieldName">Query</div>
                    <div>
                        <codemirror name="query"
                                    [(ngModel)]="editedOverride.Query"
                                    [config]="baseConfig"
                                    style="height:400px;">
                        </codemirror>
                    </div>
                </div>
                <div class="col s12">&nbsp;</div>
                <div class="col s12">
                    <button pButton
                            type="submit"
                            label="Save"></button>
                    <button pButton
                            type="button"
                            (click)="closeClick.emit();"
                            label="Close"></button>
                </div>
            </form>
        </div>
    `,
    providers: [FusionService]
})

export class FusionAttributeTypeCustomQueryEditorComponent extends BaseComponent implements OnInit {
    @Input() fusionId: number;
    @Input() selection: FusionAttributeTypeCustomQuery = null;
    @Input() existingOverrides: FusionAttributeTypeCustomQuery[];

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    attributeTypes: FusionAttributeType[];
    editedOverride: FusionAttributeTypeCustomQuery = null;
    isLoading: boolean = false;

    private baseConfig = {
        lineNumbers: true,
        theme: 'eclipse',
        mode: 'sql'
    };

    constructor(private fusionService: FusionService) {
        super();
        this.editedOverride = new FusionAttributeTypeCustomQuery();
        this.editedOverride.FusionAttributeTypeID = 0;
    }

    ngOnInit() {
        this.fusionService.getFusionAttributeTypeList(this.fusionId).subscribe(
            data => {
                this.attributeTypes = data;
                this.isLoading = false;

                if (this.selection) {
                    this.editedOverride = _.cloneDeep(this.selection);
                } else {
                    this.action = "New";
                }
            }
        );
    }

    onSubmit() {
        //save the item back to the save or edit url        
        this.saveClick.emit({override: this.editedOverride, action: this.action == "New" ? "new" : "edit"});
    }
}
