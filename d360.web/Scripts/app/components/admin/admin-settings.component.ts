import { Component, NgZone } from '@angular/core';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ICompanySettingsService, CompanySettings, IpRestriction, CompanyImage, SearchType, SettingsHelper } from '../../models/settings.model';
import { SiteNav } from '../../models/site-menu.model';
import { CompanySettingsService, SiteMenuService, HeaderActionsService, StateService } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { Title } from '@angular/platform-browser';
import { FormMode } from '../../models/form.model';

import * as _ from 'lodash';

@Component({
    selector: 'admin-settings',
    providers: [CompanySettingsService, SiteMenuService],
    templateUrl: './admin-settings.component.html',
    styleUrls: ['./admin-settings.component.css']
})

export class AdminSettingsComponent extends AdminBaseComponent {
    
    companySettings: CompanySettings = new CompanySettings();
    searchTypes: SearchType[] = SettingsHelper.getSearchTypesList();
    companyLogo: CompanyImage = new CompanyImage();
    companyIcon: CompanyImage = new CompanyImage();
    formMode = FormMode.Default;
    FormMode = FormMode;
    selection: SiteNav = null;
    folderName: string = '';
    newFolder: SiteNav = new SiteNav();
    newFolderItems: SiteNav[] = new Array<SiteNav>();

    folderItems: SiteNav[] = new Array<SiteNav>();
    availableItems: SiteNav[] = new Array<SiteNav>();
    formIsLoading = false;

    oldFolderItems: SiteNav[] = [];
    oldFolderName;

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        private companySettingsService: CompanySettingsService,
        titleService: Title,
        private siteMenuService: SiteMenuService,
        private headerActionsService: HeaderActionsService,
        private stateService: StateService    
    ) {

        super(headerBreadcrumbService, titleService);        
        this.areaName = "Settings";
        this.setCommonItems();

        this.load();
    }


    addIpRestriction(): void {
        this.companySettings.IpRestrictions.push(new IpRestriction());
    }

    removeIpRestriction(i: number): void {
        this.companySettings.IpRestrictions.splice(i, 1);
    }

    onLogoFileChange(event): void {
        if (this.companyLogo == null)
            this.companyLogo = new CompanyImage();

        if (!event) {
            this.companyLogo.file = null;
            this.companyLogo.setDataUrl();
            return;
        }

        var files = event.srcElement.files;
        this.companyLogo.file = files[0];
        this.companyLogo.setDataUrl();
    }

    onIconFileChange(event): void {
        if (this.companyIcon == null)
            this.companyIcon = new CompanyImage();
        if (!event) {
            this.companyIcon.file = null;
            this.companyIcon.setDataUrl();
            return;
        }

        var files = event.srcElement.files;
        this.companyIcon.file = files[0];
        this.companyIcon.setDataUrl();
    }

    load(): void {
        this.isLoading = true;
        this.companySettingsService.getSettings()
            .then(data => {
                this.companyLogo = new CompanyImage();
                this.companyIcon = new CompanyImage();

                this.companySettings = data;
                this.searchTypes = SettingsHelper.searchTypeStringToList(this.companySettings.DefaultSearchTypes);

                this.companySettings.SiteNav.forEach(s => {
                    if (s.Name.indexOf('#') == 0) {
                        s.IsCustom = false;
                        s.DisplayName = s.Name.substring(1);
                    } else {
                        s.DisplayName = s.Name;
                        s.IsCustom = true;
                    }
                });
                
                this.isLoading = false;
            });
    }

    save(): void {
        this.isLoading = true;
        this.companySettings.DefaultSearchTypes = SettingsHelper.searchTypeListToString(this.searchTypes);
        this.companySettings.CompanyIcon = this.companyIcon.dataUrl;
        this.companySettings.CompanyLogo = this.companyLogo.dataUrl;

        this.companySettingsService.putSettings(this.companySettings)
            .then(data => {                
                this.isLoading = false;
                window.location.reload();
            });
    }

    action(a: string, args: any) {

        let i;
        let x;
        switch (a) {
            case 'moveUp':
                this.formIsLoading = true;
                this.siteMenuService.moveFolderUp(args.ID)
                    .then(() => this.siteMenuService.getSiteNavItems())
                    .then(s => this.companySettings.SiteNav = s)                    
                    .then(() => this.stateService.reloadLeftNavMenu())
                    .then(() => this.formIsLoading = false);
                break;
            case 'moveDown':
                this.formIsLoading = true;
                this.siteMenuService.moveFolderDown(args.ID)
                    .then(() => this.siteMenuService.getSiteNavItems())
                    .then(s => this.companySettings.SiteNav = s)                   
                    .then(() => this.stateService.reloadLeftNavMenu())
                    .then(() => this.formIsLoading = false);
                break;
            case 'delete':
                this.formMode = FormMode.Deleting;
                break;
            case 'edit':
                this.formMode = FormMode.Editing;
                this.folderName = this.selection.Name;                
                this.loadFolderItems().then(() => {                    
                    this.oldFolderItems = _.cloneDeep(this.folderItems);
                    this.oldFolderName = this.folderName;
                });
                break;
            case 'add':
                this.newFolder = new SiteNav();
                this.newFolderItems = new Array<SiteNav>();
                this.loadFolderItems();
                this.formMode = FormMode.Adding;
                break;
            case 'rename':
                this.formIsLoading = true;
                this.siteMenuService.renameFolder(this.selection.ID, this.selection.Name)                    
                    .then(() => this.stateService.reloadLeftNavMenu())
                    .then(() => { this.selection.DisplayName = this.selection.Name; this.formIsLoading = false; });
                break;
            case 'deleteFolderItem':
                i = this.folderItems.findIndex(f => f.ID == args.ID);

                if (i > -1) {
                    let item = this.folderItems[i];
                    item.ParentID = this.selection.ID;

                    this.formIsLoading = true;
                    this.siteMenuService.removeFolderItem(item.ID)                        
                        .then(() => this.stateService.reloadLeftNavMenu())
                        .then(() => this.loadFolderItems());
                }
                break;
            case 'addFolderItem':
                i = this.availableItems.findIndex(f => f.ObjectID == args.ObjectID && f.Object == args.Object);

                if (i > -1) {
                    let item = this.availableItems[i];
                    item.ParentID = this.selection.ID;
                    this.formIsLoading = true;
                    this.siteMenuService.addFolderItem(item)                        
                        .then(() => this.stateService.reloadLeftNavMenu())
                        .then(() => this.loadFolderItems());
                }
                break;
            case 'deleteFolder':
                this.formIsLoading = true;
                this.siteMenuService.removeFolder(args.ID)                    
                    .then(() => this.stateService.reloadLeftNavMenu())
                    .then(() => this.load())
                    .then(() => { this.formMode = FormMode.Default; this.formIsLoading = false; });                    
                break;

            case 'addNewFolderItem':                
                x = this.availableItems.findIndex(i => i.ObjectID == args.ObjectID && i.Object == args.Object);
                i = _.cloneDeep(this.availableItems.splice(x, 1)[0]);                
                this.newFolderItems.push(i);
                break;

            case 'deleteNewFolderItem':                
                x = this.availableItems.findIndex(i => i.ObjectID == args.ObjectID && i.Object == args.Object);
                i = _.cloneDeep(this.newFolderItems.splice(x, 1)[0]);                
                this.availableItems.push(i);
                break;

            case 'save':
                this.formIsLoading = true;
                var model = {
                    folder: this.newFolder,
                    items: this.newFolderItems
                };

                this.siteMenuService.addFolder(model)
                    .then(r => {
                        this.formMode = FormMode.Default;
                        this.formIsLoading = false;
                    })
                    .then(() => this.load())
                    .then(() => this.stateService.reloadLeftNavMenu());
                break;
            case 'saveEdit':
                this.formIsLoading = true;
                this.siteMenuService.renameFolder(this.selection.ID, this.selection.Name)                    
                    .then(() => this.stateService.reloadLeftNavMenu())
                    .then(() => { this.selection.DisplayName = this.selection.Name; this.formIsLoading = false; });
                this.formMode = FormMode.Default;
                break;
            case 'cancelEdit':
                let promises = [];
                this.formIsLoading = true;

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

                if (this.oldFolderName != this.selection.Name) {
                    promises.push(this.siteMenuService.renameFolder(this.selection.ID, this.oldFolderName));
                }

                Promise.all(promises)
                    .then(() => this.loadFolderItems())
                    .then(() => {
                        this.formIsLoading = false;
                        this.formMode = FormMode.Default;
                    });
                break;
        }
    }

    loadFolderItems(): Promise<any> {
        this.formIsLoading = true;

        if (this.selection == null || this.selection.ID == null) {
            return this.siteMenuService.getAvailableItems()
                .then(r => {
                    this.availableItems = r;
                    this.formIsLoading = false;
                });
        } else {

            return this.siteMenuService.getAvailableItems()
                .then(r => {
                    console.log(r);
                    this.availableItems = r;
                })
                .then(() => this.siteMenuService.getSiteNavFolderItems(this.selection.ID))
                .then(s => {
                    this.folderItems = s;
                    this.formIsLoading = false;
                });
        }
    }
}
