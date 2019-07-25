import {Input, Output, Component, OnChanges, SimpleChange, OnInit} from '@angular/core';
import {ObjectDetailService} from '../../services/object-detail.service';
import {Synonym, SynonymItem, SynonymEditModel} from '../../models/object-detail.model';
import {FormMode, FormHelper} from '../../models/form.model';
import {BaseComponent} from '../shared/base.component';
import {Router} from '@angular/router';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import { MessagesObservableService } from '../../services/messages-observable.service';

declare var CompanySettings: any;

@Component({
    selector: 'd3s-synonyms-tile',
    styles: [
            `
            p-autoComplete > span > input {
                width: 100%;
            }
        `],
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <div [ngSwitch]="formMode">
                <div *ngSwitchDefault>
                    <header>&nbsp;<d3s-tile-actions *ngIf="!readonly"
                                                    (addClick)="add();"
                                                    [hasAdd]="hasAdd"></d3s-tile-actions>
                    </header>


                    <input type="text"
                           [hidden]="!showSimpleFilter"
                           pInputText
                           size="100"
                           (input)="dt.filterGlobal($event.target.value, 'contains')"
                           placeholder="Search..."
                           class="grid-simple-filter">
                    <p-table #dt
                             [value]="items"
                             selectionMode="single"
                             [metaKeySelection]="true"
                             [globalFilterFields]="['Name','ObjectTypeName','ParentName']"
                             sortField="Name"
                             [sortOrder]="1"
                             [paginator]="true"
                             [rows]="defaultInitialItemsPerPage"
                             [rowsPerPageOptions]="defaultPagingOptions"
                             [(selection)]="selectedItem">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'ObjectTypeName'">
                                    Type
                                    <d3s-sortIcon [field]="'ObjectTypeName'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'ParentName'">
                                    Parent
                                    <d3s-sortIcon [field]="'ParentName'"></d3s-sortIcon>
                                </th>
                                <th style="width:   48px "></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body"
                                     let-item>
                            <tr [pSelectableRow]="item">
                                <td>
                                    <d3s-preview-tooltip *ngIf="item.Object"
                                                         [objectType]="item.Object"
                                                         [objectId]="item.ObjectID">
                                        <a (click)="navigate(item.Url)">{{item.Name}}</a>
                                    </d3s-preview-tooltip>
                                    <span *ngIf="!item.Object">{{item.Name}}</span>
                                </td>
                                <td>{{item.ObjectTypeName}}</td>
                                <td>
                                    <d3s-preview-tooltip *ngIf="item.Object"
                                                         [objectType]="item.Object"
                                                         [objectId]="item.ParentID">
                                        <a (click)="navigate(item.ParentUrl)">{{item.ParentName}}</a>
                                    </d3s-preview-tooltip>
                                </td>
                                <td>
                                    <div class="RowTools">
                                        <a (click)="selectedItem=item;delete();"
                                           style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>
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
                </div>
                <div *ngSwitchCase="FormMode.Adding">
                    <header>Add {{predicateName}}</header>
                    <div class="row">
                        <div class="col s12">
                            <div class="FieldName"
                                 style="display:block;">Type
                            </div>
                            <select [(ngModel)]="selectedType"
                                    style="width:35em;"
                                    (ngModelChanged)="clearSearch()">
                                <option></option>
                                <option *ngFor="let i of synonymTypes"
                                        [value]="i.Value">
                                    {{i.Name}}
                                </option>
                                <option value="_custom">Custom</option>
                            </select>
                        </div>
                    </div>
                    <div class="row"
                         style="padding-bottom: 15px"
                         *ngIf="selectedType != '_custom'">
                        <div class="col s12">
                            <div class="FieldName"
                                 style="display:block;">Value
                            </div>
                            <p-autoComplete [suggestions]="synonymItems"
                                            (completeMethod)="search($event)"
                                            field="Name"
                                            [(ngModel)]="selectedSynonym"
                                            placeholder="Search..."
                                            size="64"
                                            [disabled]="selectedType == ''"></p-autoComplete>
                            <span *ngIf="isLoadingItems"><i class="fa fa-spinner fa-spin"></i></span>
                        </div>
                    </div>
                    <div class="row"
                         style="padding-bottom: 15px"
                         *ngIf="selectedType == '_custom'">
                        <div class="col s12">
                            <div class="FieldName">Value</div>
                            <div><input maxlength="250"
                                        pInputText
                                        name="name"
                                        type="text"
                                        style="width:35em;"
                                        [(ngModel)]="customSynonymName"
                                        required/></div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <button pButton
                                    type="button"
                                    label="Save"
                                    (click)="save();"
                                    [disabled]="(selectedType != '_custom' && selectedSynonym?.ID == null) || (selectedType == '_custom' && (!customSynonymName))"></button>
                            <button pButton
                                    type="button"
                                    label="Cancel"
                                    (click)="formMode = FormMode.Default;"></button>
                        </div>
                    </div>
                </div>
                <d3s-delete-form *ngSwitchCase="FormMode.Deleting"
                                 [callback]="theDeleteCallback"
                                 [itemId]="selectedItem"
                                 [method]="'callback'"
                                 [prompt]="'Are you sure you want to remove the ' + predicateName + ' ' + selectedItem.Name + '?'"
                                 (onCancel)="formMode = FormMode.Default;"
                ></d3s-delete-form>
            </div>
        </div>
    `,
    providers: [ObjectDetailService],
})

export class SynonymsTile extends BaseComponent implements OnChanges, OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean = true;
    @Input() predicateId: number;
    @Output() itemCount: number = 0;
    @Input() predicateName: string;

    @Input() hasAdd: boolean = true;
    @Input() hasDelete: boolean = true;

    theDeleteCallback: Function;

    private defaultSort = [
        {field: 'ObjectTypeName', order: -1},
        {field: 'ParentName', order: -1},
        {field: 'Name', order: -1}
    ];

    private formMode = FormMode.Default;
    private FormMode = FormMode;
    private items: Synonym[] = [];
    private selectedItem;


    private synonymTypes = [];    
    private selectedType: string = '';
    private synonymItems = [];
    private selectedSynonym: SynonymItem;
    private areSynonymOptionsLoaded: boolean = false;
    private customSynonymName: string = '';

    private isLoadingItems = false;

    constructor(private messagesService: MessagesObservableService, private objectDetailService: ObjectDetailService, private router: Router) {
        super();

        this.theDeleteCallback = this.deleteSynonym.bind(this);
    }

    ngOnInit() {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
    }

    load(): void {
        if (this.objectType == null || this.objectID == null) {
            return;
        }

        this.isLoading = true;

        this.objectDetailService.getObjectSynonyms(this.objectID, this.objectType, this.predicateId).subscribe(
            d => {
                this.items = d;
                this.itemCount = this.items.length;

                this.isLoading = false;
            }
        );
    }

    private deleteSynonym(item: Synonym) {
        this.isLoading = true;

        this.objectDetailService.deleteSynonym(item).subscribe(
            res => {
                this.isLoading = false;

                this.showMessageForResult(this.messagesService, res);

                if (item.IntersectID > 0) {
                    this.items = this.items.filter(x => x.IntersectID != item.IntersectID);
                } else if (item.CustomID > 0) {
                    this.items = this.items.filter(x => x.CustomID != item.CustomID);
                }

                this.itemCount = this.items.length;
                this.formMode = FormMode.Default;
            }
        );
    }

    add() {
        this.selectedSynonym = null;

        //if we havent loaded synonym types already do so now
        if (this.synonymTypes.length == 0) {
            this.objectDetailService.getSynonymTypes(this.objectID, this.objectType, this.predicateId).subscribe(
                d => {
                    this.synonymTypes = d;
                    this.formMode = FormMode.Adding;
                }
            );
        } else {
            this.formMode = FormMode.Adding;
        }
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {
        this.isLoading = true;

        if (this.selectedSynonym && this.selectedSynonym.ID) {
            var model = new SynonymEditModel();
            model.Synonym = this.selectedSynonym.ID;
            model.ID = this.objectID;
            model.Type = this.objectType;
            model.TypeIsSubject = this.selectedSynonym.TargetingSubject;
            model.PredicateID = this.predicateId;

            this.objectDetailService.postSynonym(model).subscribe(
                d => {
                    this.showMessageForResult(this.messagesService, d);
                    this.formMode = FormMode.Default;
                    this.load();
                }
            );
        } else if (this.customSynonymName) {
            this.objectDetailService.postCustomSynonym(this.customSynonymName, this.predicateId, this.objectType, this.objectID).subscribe(
                d => {
                    this.showMessageForResult(this.messagesService, d);
                    this.customSynonymName = '';
                    this.formMode = FormMode.Default;
                    this.load();
                }
            );
        }
    }

    navigate(url: string) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }

    navigateTaxonomy(item: Synonym) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TaxonomyType', item.TaxonomyTypeID));
    }

    search(e: any) {
        this.isLoadingItems = true;

        let type = this.synonymTypes.find(t => t.Value == this.selectedType);

        if (!type) {
            this.isLoadingItems = false;
            return;
        }

        this.objectDetailService.getSynonymOptions(this.predicateId, type.Object, type.ObjectID, this.objectType, this.objectID, (e.query || '')).subscribe(
            r => {
                this.synonymItems = r.items;

                this.isLoadingItems = false;
            }
        );
    }

    clearSearch() {
        this.synonymItems = [];
        this.selectedSynonym = null;
    }
}