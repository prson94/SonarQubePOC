import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettings, CompanyImage, } from '../../../models/settings.model';
import { SiteNav } from '../../../models/site-menu.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { FormMode } from '../../../models/form.model';
import { JsonResult } from '../../../models/jsonresult.model';

import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FeatureFlagsService } from '../../../services/featureflags.service';

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

export class AdminSiteMenuComponent extends AdminBaseComponent implements OnInit {
    @Input() companySettings: CompanySettings;
    @Output() companySettingsChange = new EventEmitter();
    @Output() onSaveComplete = new EventEmitter();

    formMode = FormMode.Default;
    FormMode = FormMode;
    index: number = 0;
    selection: SiteNav = null;
    folderName: string = '';
    newFolder: SiteNav = new SiteNav();
    newFolderItems: SiteNav[] = [];

    prevFolderSortOrder: number = 0;
    prevFolderID: number = 0;
    nextFolderSortOrder: number = 0;
    nextFolderID: number = 0;
    folderItems: SiteNav[] = [];
    availableItems: SiteNav[] = [];
    iconType = 'icon';
    private iconImage: CompanyImage = new CompanyImage();

    editedMenuItem: SiteNav = null;
    oldFolderItems: SiteNav[] = [];
    oldFolderName;

    IsMenuPermissionsAdding: boolean= false;
    permissionMode: FormMode = FormMode.Default;

    menuItems = [
        { title: 'Edit' },
        { title: 'Delete' },
        { title: 'Move Up' },
        { title: 'Move Down' },
    ];

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        titleService: Title,
        private siteMenuService: SiteMenuService,
        private stateService: StateService,
        private messagesService: MessagesObservableService,
        private featureFlagService: FeatureFlagsService
    ) {
        super(headerBreadcrumbService, titleService, settingsService);
    }

    ngOnInit() {             
        this.isLoading = true;
        this.siteMenuService.getSiteNavItems().subscribe((nav) => {
            this.companySettings.SiteNav = nav;
            this.isLoading = false;
        });
    }

    changeIconType(e: any) {
        if (this.formMode == FormMode.Editing) {
            if (this.iconType == 'icon') {
                this.iconType = 'image'
                this.selection.Icon = null;
            } else {
                this.iconType = 'icon';
                this.selection.ImageIconUrl = null;
                this.selection.IconPayload = null;
                this.iconImage = new CompanyImage();
            }
        } else if (this.formMode == FormMode.Adding) {
            if (this.iconType == 'icon') {
                this.iconType = 'image'
                this.newFolder.Icon = null;
            } else {
                this.iconType = 'icon';
                this.newFolder.ImageIconUrl = null;
                this.newFolder.IconPayload = null;
                this.iconImage = new CompanyImage();
            }
        }
        
    }

    clearIcon() {
        this.iconImage = new CompanyImage();
        if (this.formMode == FormMode.Editing) {
            this.selection.ImageIconUrl = null;
        } else if (this.formMode == FormMode.Adding) {
            this.newFolder.ImageIconUrl = null;
            }
        this.onFileChange(null);
    }

    checkIfImg(value: string) {
        if (value && value.indexOf('/Content') != -1) {
            return true;
        }
        else
            return false;
    }

    onFileChange(event): void {
        if (this.iconImage == null)
            this.iconImage = new CompanyImage();

        if (event == null) {
            this.iconImage.file = null;
            this.iconImage.setDataUrl();

            if (this.formMode == FormMode.Editing) {
                this.selection.IconPayload = null;
            } else if (this.formMode == FormMode.Adding) {
                this.newFolder.IconPayload = null;
            }

            return;
        }

        let target = event.target || event.srcElement;
        let files = target.files;

        if (files[0] != null) {
            if (files[0].size > (1024 * 1024)) {
                this.messagesService.showError('File too large.', `Navigation icon image upload failed - the file is too large. Please choose an image file (ideally in JPG format due to smaller file size) no bigger than 1MB. `);
                target.value = null;
                return;
            }
        }


        this.iconImage.file = files[0];
        this.iconImage.setDataUrl();
        if (this.formMode == FormMode.Editing) {
            this.selection.IconPayload = this.iconImage.dataUrl;
        } else if (this.formMode == FormMode.Adding) {
            this.newFolder.IconPayload = this.iconImage.dataUrl;
        }
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
                .subscribe(() => {
                    this.stateService.reloadLeftNavMenu();
                    this.loadFolderItems();
                })
        }
    }

    deleteFolder(item: SiteNav) {
        this.isLoading = true;
        this.siteMenuService.removeFolder(item.ID)
            .subscribe(res => {
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
                .subscribe(() => {
                    this.stateService.reloadLeftNavMenu();
                    this.loadFolderItems();
                })
        }
    }

    addFolderItem(item: SiteNav) {
        let i = this.availableItems.findIndex(f => f.ObjectID == item.ObjectID && f.Object == item.Object);

        if (i > -1) {
            let item = this.availableItems[i];
            item.ParentID = this.selection.ID;
            this.isLoading = true;
            this.siteMenuService.addFolderItem(item)
                .subscribe(() => {
                    this.stateService.reloadLeftNavMenu();
                    this.loadFolderItems();
                })
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
        this.editedMenuItem = item;
        this.formMode = FormMode.Editing;
        this.iconImage = new CompanyImage();
        this.folderName = this.selection.Name;
        if (this.selection.ImageIconUrl != null)
            this.iconType = 'image';
        else
            this.iconType = 'icon';
        this.loadFolderItems();
        this.oldFolderItems = _.cloneDeep(this.folderItems);
        this.oldFolderName = this.folderName;
        this.loadSiteNavPermissions(this.selection);
    }
    
    delete(item: SiteNav) {
        this.selection = item;
        this.formMode = FormMode.Deleting;
    }

    moveUp(item: SiteNav) {
        this.selection = item;
        this.isLoading = true;
        this.siteMenuService.moveFolderUp(this.selection.ID)
            .subscribe(() => {
                this.siteMenuService.getSiteNavItems()
                    .subscribe(s => {
                        this.companySettings.SiteNav = s;
                        this.companySettingsChange.emit(this.companySettings);
                        this.stateService.reloadLeftNavMenu();
                        this.isLoading = false;
                    })
            })
    }

    moveDown(item: SiteNav) {
        this.selection = item;
        this.isLoading = true;
        this.siteMenuService.moveFolderDown(this.selection.ID)
            .subscribe(() => {
                this.siteMenuService.getSiteNavItems()
                    .subscribe(s => {
                        this.companySettings.SiteNav = s;
                        this.companySettingsChange.emit(this.companySettings);
                        this.stateService.reloadLeftNavMenu();
                        this.isLoading = false;
                    });
                
            })
    }

    moveFolderUp(item: SiteNav, i: number) {
        if (i != 0) {
            this.selection = item;
            this.index = i;
            this.prevFolderID = this.folderItems[i - 1].ID;
            this.isLoading = true;
            this.siteMenuService.moveSiteNavFolderUp(this.selection.ID, this.prevFolderID)
                .subscribe(() => {
                    this.edit(this.editedMenuItem);
                    this.stateService.reloadLeftNavMenu()
                })
        } else {
            this.messagesService.showError("Error", "First item can not be moved up.")            
        }        
        
    }

    moveFolderDown(item: SiteNav, i: number) {
        if (i < this.folderItems.length-1) {
            this.selection = item;
            this.index = i;
            this.nextFolderID = this.folderItems[i + 1].ID;
            this.isLoading = true;
            this.siteMenuService.moveSiteNavFolderDown(this.selection.ID, this.nextFolderID)
                .subscribe(() => {
                    this.edit(this.editedMenuItem);
                    this.stateService.reloadLeftNavMenu()
                })
        } else {
            this.messagesService.showError("Error", "Last item can not be moved down.")                       
        }        
    }

    save() {

        this.isLoading = true;

        switch (this.formMode) {
            case FormMode.Editing:
                this.selection.IconPayload = this.iconImage.dataUrl;
                this.siteMenuService.editFolder(this.selection)
                    .subscribe(result => {
                        this.showMessageForResult(this.messagesService, result);
                        this.siteMenuService.setSiteNavPermissions(this.selection);
                        this.stateService.reloadLeftNavMenu();
                        this.isLoading = false;
                        this.formMode = FormMode.Default;
                        this.onSaveComplete.emit();
                    })
                break;
            case FormMode.Adding:

                this.newFolder.IconPayload = this.iconImage.dataUrl;
                var model = {
                    folder: this.newFolder,
                    items: this.newFolderItems
                };

                this.siteMenuService.addFolder(model)
                    .subscribe(r => {
                        this.showMessageForResult(this.messagesService, r);
                        this.formMode = FormMode.Default;
                        this.isLoading = false;
                        this.stateService.reloadLeftNavMenu();
                        this.onSaveComplete.emit();
                        this.siteMenuService.setSiteNavPermissions(this.selection)
                    })
                break;
            case FormMode.Deleting:
                this.isLoading = true;
                this.siteMenuService.removeFolder(this.selection.ID)
                    .subscribe(res => {
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

    loadFolderItems() {
        this.isLoading = true;

        if (this.selection == null || this.selection.ID == null) {
            return this.siteMenuService.getAvailableItems()
                .subscribe(r => {
                    this.availableItems = r;
                    this.isLoading = false;
                });
        } else {

            return this.siteMenuService.getAvailableItems()
                .subscribe(r => {
                    this.availableItems = r;
                    this.siteMenuService.getSiteNavFolderItems(this.selection.ID)
                        .subscribe(s => {
                            this.folderItems = s;
                            this.folderItems = _.sortBy(this.folderItems, 'SortOrder'); // sort the folderItems by SortOrder
                            this.isLoading = false;
                            this.siteMenuService.getSiteNavFolderItems(this.selection.ID)
                                .subscribe(s => {
                                    this.folderItems = s;
                                    this.folderItems = _.sortBy(this.folderItems, 'SortOrder'); // sort the folderItems by SortOrder
                                    this.isLoading = false;
                                    this.stateService.reloadLeftNavMenu();
                                })
                        })
                })
        }
    }

    loadSiteNavPermissions(item: SiteNav) {
        this.isLoading = true;
        return this.siteMenuService.getSiteNavPermissions(item.ID)
            .subscribe(r => {
                item.Permissions = r;
                this.isLoading = false;
            });
    }

    menuPermissionsOnModeChange($event) {
        this.permissionMode = $event;
        this.IsMenuPermissionsAdding = ($event == FormMode.Adding);
       
    }

    selectRow(data) {
        this.selection = data;
    }

    clickMenuItem(event: any, item: any) {
        let key = event.value.toLowerCase();
        if (key === 'edit') {
            this.edit(item);
        } else if (key === 'delete') {
            this.delete(item);
        } else if (key === 'move up') {
            this.moveUp(item);
        } else if (key === 'move down') {
            this.moveDown(item);
        }
    }
}
