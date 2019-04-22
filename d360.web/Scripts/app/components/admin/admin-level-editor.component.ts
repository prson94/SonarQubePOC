import {Input, Component, EventEmitter, Output} from '@angular/core';
import {SelectItem} from 'primeng/primeng';
import {LevelsService} from '../../services/levels.service';
import {TaxonomyLevel} from '../../models/taxonomy.model';
import {BaseComponent} from '../shared/base.component';
import * as _ from 'lodash';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */

@Component({
    selector: 'd3s-admin-level-editor',
    template: `
        <header>{{action}} Level</header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <form (ngSubmit)="onSubmit()"
              #levelForm="ngForm">
            <div class="row"
                 *ngIf="!isLoading && ((level != null) || levels.length > 0)">
                <div class="col l6 s12">
                    <div class="FieldName">Name</div>
                    <div><input required
                                type="text"
                                name="name"
                                pInputText
                                [(ngModel)]="editedLevel.Name"
                                style="width: 100%;"
                                #name="ngModel"
                                maxlength="250"/></div>
                    <div [hidden]="name.valid || name.pristine">Level name is required</div>
                </div>
                <div class="col l6 s12"
                     *ngIf="level==null">
                    <div class="FieldName">Level</div>
                    <div>
                        <select required
                                name="availableLevels"
                                style="width:100%;"
                                placeholder="Choose a value"
                                [(ngModel)]="editedLevel.Level"
                                #level="ngModel">
                            <option></option>
                            <option *ngFor="let p of levels"
                                    [value]="p">{{p}}</option>
                        </select>
                    </div>
                    <div [hidden]="level.valid || level.pristine">Level value is required</div>
                </div>
                <div class="col s12">
                    <div class="FieldName">Description</div>
                    <p-editor name="description"
                              [style]="{'height':'150px'}"
                              [(ngModel)]="editedLevel.Description"></p-editor>
                </div>
                <div class="col s12">&nbsp;</div>
                <div class="col s12">
                    <button pButton
                            [disabled]="!levelForm.form.valid"
                            type="submit"
                            label="Save"></button>
                    <button pButton
                            type="button"
                            (click)="close()"
                            label="Close"></button>
                </div>
            </div>
            <div class="row"
                 *ngIf="!isLoading && !level && levels.length == 0">
                <div class="center">The maximum number of levels available for this item have already been allocated. In
                    order to define new levels you can either increase the maximum available levels for this item or
                    delete an existing level for this item.
                </div>
                <div class="col s12">&nbsp;</div>
                <div class="col s12">
                    <button pButton
                            type="button"
                            (click)="close()"
                            label="Close"></button>
                </div>
            </div>
        </form>
    `,
    providers: [LevelsService],
})

export class AdminLevelEditorComponent extends BaseComponent {
    @Input() level: TaxonomyLevel;
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() maxDepth: number;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedLevel: TaxonomyLevel;
    levels: number[] = [];

    constructor(private levelsService: LevelsService) {
        super();
    }

    ngOnInit() {
        if (this.level != undefined)
            this.editedLevel = _.cloneDeep(this.level);
        else {
            this.editedLevel = new TaxonomyLevel();
            this.action = "New";
        }
        this.getUnusedLevels();
    }

    getUnusedLevels() {
        this.isLoading = true;
        this.levelsService.getObjectLevels(this.objectId, this.objectType).subscribe(
            result => {
                this.isLoading = false;

                for (var i = 1; i <= this.maxDepth; i++) {
                    this.levels.push(i);
                }

                for (var i = 0; i < result.length; i++) {
                    //remove the used level
                    let index = this.levels.map((e) => e).indexOf(result[i].Level);

                    this.levels.splice(index, 1);
                }
            }
        );
    }

    onSubmit() {
        this.saveClick.emit({level: this.editedLevel, action: this.level == null ? "new" : "edit"});
    }

    close() {
        this.closeClick.emit({});
    }
}
