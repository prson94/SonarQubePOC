import {Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {Taxonomy, TaxonomyLevel} from '../../models/taxonomy.model';
import {LevelsService} from '../../services/levels.service';
import {MessagesService} from '../../services/messages.service';
import {BaseComponent} from '../shared/base.component';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */

@Component({
    selector: 'd3s-admin-level-grid',
    providers: [LevelsService],
    template: `
        <header *ngIf="!showEditor && !showDelete">Levels
            <d3s-tile-actions [hasAdd]="true"
                              (addClick)="add()"
                              [hasFilterMode]="true"
                              [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!isLoading && !showDelete && !showEditor">               
                    <input type="text"
                           [hidden]="!showSimpleFilter"
                           pInputText
                           size="100"
                           (input)="dt.filterGlobal($event.target.value, 'contains')"
                           placeholder="Search..."
                           class="grid-simple-filter">
	                <p-table #dt
                             [value]="levels"
                             selectionMode="single"
                             [globalFilterFields]="['Name','Level','Description']"
                             [(selection)]="selectedLevel"
                             [rows]="10"
                             [paginator]="true"
                             [pageLinks]="3">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Level'">
                                    Level
                                    <d3s-sortIcon [field]="'Level'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Description'">
                                    Description
                                    <d3s-sortIcon [field]="'Description'"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
			                <tr [hidden]="showSimpleFilter">
                                <th>
                                    <d3s-column-filter [field]="'Level'"></d3s-column-filter>
                                </th>
                                <th>
                                    <d3s-column-filter [field]="'Name'"></d3s-column-filter>
                                </th>
                                <th>
                                    <d3s-column-filter [field]="'Description'"></d3s-column-filter>
                                </th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body"
                                     let-item>
                            <tr (dblclick)="showEditor = true"
                                [pSelectableRow]="item">
                                <td>
                                    {{item.Level}}
                                </td>                                
                                <td>
                                    {{item.Name}}
                                </td>
                                <td>
                                     <div [innerHtml]="item?.Description"></div>
                                </td>
                                <td>
                                    <div class="RowTools">
                                        <a style="cursor:pointer;"
                                           (click)="selectedLevel=item;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;"
                                           (click)="selectedLevel=item;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
		                <ng-template *ngIf="dt.totalRecords"
                                     pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first"
                                                  [rows]="dt.rows"
                                                  [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table> 
                </span>
        <d3s-delete-form *ngIf="showDelete"
                         [callback]="theDeleteCallback"
                         [itemId]="selectedLevel?.Level"
                         [method]="'callback'"
                         [prompt]="'Are you sure you want to delete the level [' + [selectedLevel?.Name] + ']?'"
                         (onCancel)="showDelete=false;"
        ></d3s-delete-form>
        <d3s-admin-level-editor *ngIf="showEditor"
                                [maxDepth]="maxDepth"
                                [level]="selectedLevel"
                                [objectId]="objectId"
                                [objectType]="objectType"
                                (closeClick)="closeEditor()"
                                (saveClick)="saveLevel($event)"></d3s-admin-level-editor>
    `
})

export class AdminLevelListComponent extends BaseComponent implements OnChanges {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() maxDepth: number;

    error: any;
    levels: TaxonomyLevel[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;
    selectedLevel: TaxonomyLevel = null;
    theDeleteCallback: Function;

    constructor(private levelsService: LevelsService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteLevel.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectId > 0) this.getLevels();
    }

    getLevels() {
        this.isLoading = true;

        this.levelsService.getObjectLevels(this.objectId, this.objectType).subscribe(
            levels => {
                this.levels = levels;

                this.isLoading = false;
            }
        );
    }

    deleteLevel(id: number) {
        this.levelsService.deleteObjectLevel(this.objectType, this.objectId, id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.levels = this.levels.filter(x => x.Level != id);
            }
        );
    }

    add() {
        this.showEditor = true;
        this.selectedLevel = null;
    }

    closeEditor() {
        this.showEditor = false;

        if (this.selectedLevel == null && this.levels.length > 0) {
            this.selectedLevel = this.levels[0];
        }
    }

    saveLevel(event) {
        this.levelsService.saveObjectLevel(event.level, this.objectType, this.objectId, event.action).then(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.showEditor = false;
                this.getLevels();
            }
        );
    }
}
