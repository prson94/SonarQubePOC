import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { ICompanySettingsService, CompanySettings, } from '../../../models/settings.model';
import { SiteNav } from '../../../models/site-menu.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { StateService } from '../../../services/state.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { FormMode } from '../../../models/form.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-site-menu',
    providers: [CompanySettingsService, SiteMenuService],
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div [ngSwitch]="formMode" *ngIf="!isLoading">
            <div *ngSwitchDefault>
                <header>
                    Site Nav
                    <d3s-tile-actions hasAdd="true" (addClick)="add()"></d3s-tile-actions>
                </header>
                <p-dataTable [value]="companySettings.SiteNav" selectionMode="single" [(selection)]="selection">
                    <p-column field="Title" header="Name"></p-column>
                    <p-column header="">
                        <template let-col let-item="rowData" pTemplate type="body">
                            <div class="RowTools">
                                <a *ngIf="item.IsCustom" (click)="delete(item)" style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>
                                <a (click)="edit(item)" style="cursor:pointer;"><i class="fa fa-pencil"></i></a>
                                <a (click)="moveUp(item)" style="cursor:pointer;"><i class="fa fa-caret-up"></i></a>
                                <a (click)="moveDown(item)" style="cursor:pointer;"><i class="fa fa-caret-down"></i></a>
                            </div>
                        </template>
                    </p-column>
                </p-dataTable>
            </div>
            <div *ngSwitchCase="FormMode.Editing">
                <header>
                    Edit {{selection.Title}}
                </header>
                <div *ngIf="!formIsLoading">
                    <div class="row" style="margin-bottom:15px;">
                        <div class="col s12">
                            <div class="FieldName" style="display:block;">Folder Name</div>
                            <input type="text" maxlength="250" [(ngModel)]="selection.Title" style="width:100%" />
                        </div>
                        <div class="col s12">
                            <div class="FieldName" style="display:block;">Folder Icon</div>
                            <d3s-icon-picker [(ngModel)]="selection.Icon" ngDefaultControl></d3s-icon-picker>
                        </div>
                    </div>
                    <div *ngIf="selection.IsCustom" class="row">
                        <div class="col s12 m6">
                            <p-dataTable #dt [value]="availableItems" [rows]="10" [paginator]="true" selectionMode="single">
                                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                <p-column field="Title" header="Available Folder Items"></p-column>
                                <p-column>
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools" style="height: initial;">
                                            <a (click)="addFolderItem(item)" style="cursor:pointer;"><i class="fa fa-plus"></i></a>
                                        </div>
                                    </template>
                                </p-column>
                            </p-dataTable>
                        </div>
                        <div class="col s12 m6">
                            <p-dataTable [value]="folderItems" [rows]="10" [paginator]="true" selectionMode="single">
                                <p-column field="Name" header="Existing Folder Items"></p-column>
                                <p-column>
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools" style="height: initial;">
                                            <a (click)="deleteFolderItem(item)" style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>
                                        </div>
                                    </template>
                                </p-column>
                            </p-dataTable>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <button pButton type="button" label="Save" (click)="save()" [disabled]="selection == null || selection.Title == null || selection.Title == '' && (selection.IsCustom && folderItems == null || folderItems.length < 1)"></button>
                            <button pButton type="button" label="Cancel" (click)="cancel()"></button>
                        </div>
                    </div>
                </div>
            </div>
            <div *ngSwitchCase="FormMode.Deleting">
                <header>
                    Delete Folder
                </header>
                <div clas="row">
                    <div class="col s12" style="padding-bottom:10px">
                        Are you sure you want to delete the {{selection.Name}} folder?
                    </div>
                </div>
                <button pButton type="button" label="Delete" (click)="save()"></button>
                <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
            </div>
            <div *ngSwitchCase="FormMode.Adding">
                <header>
                    Add Navigation Folder
                </header>
                <div *ngIf="!formIsLoading">
                    <div class="row" style="margin-bottom:15px;">
                        <div class="col s12">
                            <div class="FieldName" style="display:block;">Folder Name</div>
                            <input maxlength="250" type="text" [(ngModel)]="newFolder.Name" style="width:100%" />
                        </div>
                        <div class="col s12">
                            <div class="FieldName" style="display:block;">Folder Icon</div>
                            <d3s-icon-picker [(ngModel)]="newFolder.Icon" ngDefaultControl></d3s-icon-picker>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12 m6">
                            <p-dataTable [value]="availableItems" [rows]="10" [paginator]="true" selectionMode="single">
                                <p-column field="Title" header="Available Folder Items"></p-column>
                                <p-column>
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools" style="height: initial;">
                                            <a (click)="addNewFolder(item)" style="cursor:pointer;"><i class="fa fa-plus"></i></a>
                                        </div>
                                    </template>
                                </p-column>
                            </p-dataTable>
                        </div>
                        <div class="col s12 m6">
                            <p-dataTable [value]="newFolderItems" [rows]="10" [paginator]="true" selectionMode="single">
                                <p-column field="Name" header="Folder Items"></p-column>
                                <p-column>
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools" style="height: initial;">
                                            <a (click)="deleteNewFolder(item)" style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>
                                        </div>
                                    </template>
                                </p-column>
                            </p-dataTable>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <button pButton type="button" label="Save" (click)="save()" [disabled]="newFolder == null || newFolder.Name == null || newFolder.Name == '' || newFolderItems == null || newFolderItems.length < 1"></button>
                            <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
`,
    styles: [`
        .remove {
            cursor: pointer; 
            color: maroon; 
            font-size: 1.5em;
            vertical-align: middle;
        }
        input[type=text] {
            width: 90%;
            height:25px;
        }
  `],
})

export class AdminSiteMenuComponent extends AdminBaseComponent implements OnInit {
    @Input() companySettings: CompanySettings;
    @Output() companySettingsChange = new EventEmitter();
    @Output() onSaveComplete = new EventEmitter();

    formMode = FormMode.Default;
    FormMode = FormMode;
    selection: SiteNav = null;
    folderName: string = '';
    newFolder: SiteNav = new SiteNav();
    newFolderItems: SiteNav[] = [];

    folderItems: SiteNav[] = [];
    availableItems: SiteNav[] = [];

    oldFolderItems: SiteNav[] = [];
    oldFolderName;

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        private companySettingsService: CompanySettingsService,
        titleService: Title,
        private siteMenuService: SiteMenuService,
        private stateService: StateService,
        private messagesService: MessagesService
    ) {
        super(headerBreadcrumbService, titleService);
    }

    ngOnInit() {
    }


    add() {
        this.selection = null;
        this.newFolder = new SiteNav();
        this.newFolderItems = new Array<SiteNav>();
        this.loadFolderItems();
        console.log(this.availableItems, this.folderItems);
        this.formMode = FormMode.Adding;
    }

    addFolder(item: SiteNav) {
        let i = this.availableItems.findIndex(f => f.ObjectID == item.ObjectID && f.Object == item.Object);

        if (i > -1) {
            let item = this.availableItems[i];
            item.ParentID = this.selection.ID;
            this.isLoading = true;
            this.siteMenuService.addFolderItem(item)
                .then(() => this.stateService.reloadLeftNavMenu())
                .then(() => this.loadFolderItems());
        }
    }

    deleteFolder(item: SiteNav) {
        this.isLoading = true;
        this.siteMenuService.removeFolder(item.ID)
            .then(res => {
                this.showMessageForResult(this.messagesService, res);
                this.stateService.reloadLeftNavMenu();
                this.formMode = FormMode.Default;
                this.isLoading = false;
            });                    
    }

    deleteFolderItem(item: SiteNav) {
        let i = this.folderItems.findIndex(f => f.ID == item.ID);

        if (i > -1) {
            let item = this.folderItems[i];
            item.ParentID = this.selection.ID;

            this.isLoading = true;
            this.siteMenuService.removeFolderItem(item.ID)
                .then(() => this.stateService.reloadLeftNavMenu())
                .then(() => this.loadFolderItems());
        }
    }

    addFolderItem(item: SiteNav) {
        let i = this.availableItems.findIndex(f => f.ObjectID == item.ObjectID && f.Object == item.Object);

        if (i > -1) {
            let item = this.availableItems[i];
            item.ParentID = this.selection.ID;
            this.isLoading = true;
            this.siteMenuService.addFolderItem(item)
                .then(() => this.stateService.reloadLeftNavMenu())
                .then(() => this.loadFolderItems());
        }
    }

    addNewFolder(item: SiteNav) {
        let x = this.availableItems.findIndex(i => i.ObjectID == item.ObjectID && i.Object == item.Object);
        let i = _.cloneDeep(this.availableItems.splice(x, 1)[0]);
        this.newFolderItems.push(i);
    }

    deleteNewFolder(item: SiteNav) {
        let x = this.availableItems.findIndex(i => i.ObjectID == item.ObjectID && i.Object == item.Object);
        let i = _.cloneDeep(this.newFolderItems.splice(x, 1)[0]);
        this.availableItems.push(i);
    }

    edit(item: SiteNav) {
        this.selection = item;
        this.formMode = FormMode.Editing;
        this.folderName = this.selection.Name;
        this.loadFolderItems().then(() => {
            this.oldFolderItems = _.cloneDeep(this.folderItems);
            this.oldFolderName = this.folderName;
        });
    }

    delete(item: SiteNav) {
        this.selection = item;
        this.formMode = FormMode.Deleting;
    }


    moveUp(item: SiteNav) {
        this.selection = item;
        this.isLoading = true;
        this.siteMenuService.moveFolderUp(this.selection.ID)
            .then(() => this.siteMenuService.getSiteNavItems())
            .then(s => { this.companySettings.SiteNav = s; this.companySettingsChange.emit(this.companySettings); })
            .then(() => this.stateService.reloadLeftNavMenu())
            .then(() => this.isLoading= false);
    }

    moveDown(item: SiteNav) {
        this.selection = item;
        this.isLoading = true;
        this.siteMenuService.moveFolderDown(this.selection.ID)
            .then(() => this.siteMenuService.getSiteNavItems())
            .then(s => { this.companySettings.SiteNav = s; this.companySettingsChange.emit(this.companySettings); })
            .then(() => this.stateService.reloadLeftNavMenu())
            .then(() => this.isLoading = false);
    }

    save() {

        this.isLoading = true;

        switch (this.formMode) {
            case FormMode.Editing:
                this.siteMenuService.editFolder(this.selection)
                    .then(result => {
                        this.showMessageForResult(this.messagesService, result);
                        this.stateService.reloadLeftNavMenu();
                        this.isLoading = false;
                        this.formMode = FormMode.Default;
                        this.onSaveComplete.emit();
                    });
                break;
            case FormMode.Adding:
                var model = {
                    folder: this.newFolder,
                    items: this.newFolderItems
                };

                this.siteMenuService.addFolder(model)
                    .then(r => {
                        this.showMessageForResult(this.messagesService, r);
                        this.formMode = FormMode.Default;
                        this.isLoading = false;
                        this.stateService.reloadLeftNavMenu();
                        this.onSaveComplete.emit();
                    });
                break;
            case FormMode.Deleting:
                this.isLoading = true;
                this.siteMenuService.removeFolder(this.selection.ID)
                    .then(res => {
                        this.showMessageForResult(this.messagesService, res);
                        this.stateService.reloadLeftNavMenu();
                        this.formMode = FormMode.Default;
                        this.isLoading = false;
                        this.onSaveComplete.emit();
                    }); 
                break;
        }
    }

    cancel() {
        let promises = [];
        this.isLoading = true;

        this.folderItems.forEach(o => {
            let s = this.oldFolderItems.find(i => i.ID == o.ID);

            if (s == null) {
                promises.push(this.siteMenuService.removeFolderItem(o.ID));
            }
        });

        this.oldFolderItems.forEach(o => {

            let s = this.folderItems.find(i => i.ID == o.ID);

            if (s == null) {
                promises.push(this.siteMenuService.addFolderItem(o));
            }
        });

        Promise.all(promises)
            .then(() => this.loadFolderItems())
            .then(() => {
                this.isLoading = false;
                this.stateService.reloadLeftNavMenu();
                this.formMode = FormMode.Default;
            });
    }


    loadFolderItems(): Promise<any> {
        this.isLoading = true;

        if (this.selection == null || this.selection.ID == null) {
            return this.siteMenuService.getAvailableItems()
                .then(r => {
                    this.availableItems = r;
                    this.isLoading = false;
                });
        } else {

            return this.siteMenuService.getAvailableItems()
                .then(r => {
                    this.availableItems = r;
                })
                .then(() => this.siteMenuService.getSiteNavFolderItems(this.selection.ID))
                .then(s => {
                    this.folderItems = s;
                    this.isLoading = false;
                });
        }
    }

}
