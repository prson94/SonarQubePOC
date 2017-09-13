import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
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
    templateUrl: './admin-site-menu.component.html',
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

export class AdminSiteMenuComponent extends AdminBaseComponent implements OnInit, OnChanges {
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

    permissionMode: FormMode = FormMode.Default;

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
        this.isLoading = true;
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['companySettings'].isFirstChange)
            this.isLoading = false;
    }

    add() {
        this.selection = null;
        this.newFolder = new SiteNav();
        this.newFolderItems = new Array<SiteNav>();
        this.loadFolderItems();
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
        this.loadFolderItems()
            .then(() => {
                this.oldFolderItems = _.cloneDeep(this.folderItems);
                this.oldFolderName = this.folderName;
            })
            .then(() => this.loadSiteNavPermissions(this.selection));
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
                    })
                    .then(() => this.siteMenuService.setSiteNavPermissions(this.selection))
                    .then(() => {
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
                    })
                    .then(() => this.siteMenuService.setSiteNavPermissions(this.selection))
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

    loadSiteNavPermissions(item: SiteNav): Promise<any> {
        this.isLoading = true;
        return this.siteMenuService.getSiteNavPermissions(item.ID)
            .then(r => {
                item.Permissions = r;
                this.isLoading = false;
            });
    }
}
