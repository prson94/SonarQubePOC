import {Component, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {Classification} from '../../models/object-detail.model';
import {MessagesService} from '../../services/messages.service';
import {ObjectDetailService} from '../../services/object-detail.service';
import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-admin-classifications',
    providers: [ObjectDetailService],
    template: `
        <div class="tile tile-detail">
            <header *ngIf="!showEditor && !showDelete">{{objectType == 'TaxonomyTypeClass' ? 'Model' : 'Policy'}}
                Classifications
                <d3s-tile-actions [hasAdd]="true"
                                  (addClick)="add()"></d3s-tile-actions>
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
                                 [value]="classifications"
                                 sortField="Name"
                                 [sortOrder]="1"
                                 selectionMode="single"
                                 [rows]="10"
                                 [paginator]="true"
                                 [pageLinks]="3"
                                 [(selection)]="selected">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th>
                                        Name
                                    </th>
                                   <th style="width: 40px"></th>
                                   <th style="width: 40px"></th>
                                </tr>
			                    <tr [hidden]="showSimpleFilter">
                                    <th>
                                        <d3s-column-filter [field]="'Name'"
                                                           [datatype]="'text'"></d3s-column-filter>
                                    </th>
                                    <th></th>
                                    <th></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body"
                                         let-item>
                                <tr (dblclick)="selected = item; showEditor = true;"
                                    [pSelectableRow]="item">
                                    <td>
                                        {{item.Name}}
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <a style="cursor:pointer;"
                                               (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </td>
                                    <td>
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;"
                                               (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </td>
                                </tr>
                            </ng-template>
                        </p-table>
                    </span>
            <d3s-dynamic-editor *ngIf="showEditor"
                                [objectID]="selected?.ID"
                                [objectType]="objectType"
                                [title]="objectType == 'TaxonomyTypeClass' ? 'Model Classification' : 'Policy Classification'"
                                [selection]="selected"
                                (saveClick)="saveClassification($event)"
                                (closeClick)="closeEditor()"></d3s-dynamic-editor>
            <d3s-delete-form *ngIf="showDelete"
                             [callback]="theDeleteCallback"
                             [itemId]="selected?.ID"
                             [method]="'callback'"
                             [prompt]="'Are you sure you want to delete the Classification [' + [selected?.Name] + ']?'"
                             (onCancel)="showDelete=false;"
            ></d3s-delete-form>
        </div>
    `
})

export class AdminClassificationsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    classifications: Classification[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;
    selected: Classification = null;
    theDeleteCallback: Function;

    constructor(
        private objectDetailService: ObjectDetailService,
        private messagesService: MessagesService,
        private route: ActivatedRoute,
        private router: Router,
    ) {
        super();
        this.theDeleteCallback = this.deleteClassification.bind(this);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectType = params['objectType'];

            this.load();
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private load() {
        this.objectDetailService.getClassifications(this.objectType).subscribe(
            result => {
                this.classifications = result;

                this.selected = this.classifications.length > 0 ? this.classifications[0] : null;
            }
        );
    }

    deleteClassification(id: number) {
        this.objectDetailService.deleteClassification(id, this.objectType).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.classifications = this.classifications.filter(x => x.ID != id);

                this.showDelete = false;
            }
        );
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.classifications.length > 0)
            this.selected = this.classifications[0];
    }

    saveClassification(event) {
        this.objectDetailService.saveClassification(event.item, this.objectType).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);

                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
                    this.classifications[this.classifications.length] = event.item;
                } else {
                    let index = this.classifications.findIndex(x => x.ID == event.item.ID);

                    if (index >= 0 && index < this.classifications.length)
                        this.classifications[index] = event.item;
                }

                this.selected = event.item;

                this.showEditor = false;
            }
        );
    }
}


