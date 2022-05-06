import { Input, Component, OnInit, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../base.component';
import { HelpMenuService } from '../../shared/helpmenu/helpmenu.service';
import { HelpMenu } from '../../../models/helpmenu.model';
import { FormMode } from '../../../models/form.model';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-helpmenu-list',
    templateUrl: './helpmenu-list.component.html',
    providers: [HelpMenuService]
})

export class HelpMenuListComponent extends BaseComponent implements OnInit {
    @Output() itemsChange = new EventEmitter();
    @Input() items: HelpMenu[] = [];
    @Output() deletedRecordsChange = new EventEmitter();
    @Input() deletedRecords: HelpMenu[] = [];
    @Output() addedRecordsChange = new EventEmitter();
    @Input() addedRecords: HelpMenu[] = [];
    private selectedItem: HelpMenu = null;
    formMode = FormMode.Default;
    FormMode = FormMode;
    editedName: string = '';
    editedUrl: string = '';
    editedDes: string = '';
    newID: number = -1;


    editMenu = $localize`Edit`;
    deleteMenu = $localize`Delete`;
    visibleMenu = $localize`Visible`;
    visibleToAdminsMenu = $localize`Visible to Admins Only`;
    hiddenMenu = $localize`Hidden`;

    private editMenuItems: any[] = [
        { title: this.editMenu },
    ];

    private deleteMenuItem: any[] = [
        { title: this.editMenu },
        { title: this.deleteMenu },
    ];

    private visibleVisibilityItems: any[] = [
        { title: this.visibleMenu, hasSelectedBox: true, isSelected: true },
        { title: this.visibleToAdminsMenu, hasSelectedBox: true, isSelected: false },
        { title: this.hiddenMenu, hasSelectedBox: true, isSelected: false },
    ];
    private adminVisibilityItems: any[] = [
        { title: this.visibleMenu, hasSelectedBox: true, isSelected: false },
        { title: this.visibleToAdminsMenu, hasSelectedBox: true, isSelected: true },
        { title: this.hiddenMenu, hasSelectedBox: true, isSelected: false },
    ];
    private hiddenVisibilityItems: any[] = [
        { title: this.visibleMenu, hasSelectedBox: true, isSelected: false },
        { title: this.visibleToAdminsMenu, hasSelectedBox: true, isSelected: false },
        { title: this.hiddenMenu, hasSelectedBox: true, isSelected: true },
    ];

    menuMTT = $localize`Move to Top`;
    menuMU = $localize`Move Up`;
    menuMD = $localize`Move Down`;
    menuMTB = $localize`Move to Bottom`;

    private upMenuItems: any[] = [
        { title: this.menuMTT },
        { title: this.menuMU }
    ];

    private downMenuItems: any[] = [
        { title: this.menuMD },
        { title: this.menuMTB }
    ];

    constructor(
        private helpMenuService: HelpMenuService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.helpMenuService.getHelpMenuItems()
            .subscribe((r) => {
                this.items = r;
                this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
                this.itemsChange.emit(this.items);
                this.deletedRecordsChange.emit(this.deletedRecords);
                this.addedRecordsChange.emit(this.addedRecords);
            });
    }

    add() {
        this.formMode = FormMode.Adding;
        this.selectedItem = null;
    }

    edit(id: number) {
        this.formMode = FormMode.Editing;
        this.selectedItem = this.items.find((s) => s.ID === id);
        this.editedName = this.selectedItem.Name;
        this.editedUrl = this.selectedItem.Url;
        this.editedDes = this.selectedItem.Description;
    }

    delete(id: number) {
        this.formMode = FormMode.Deleting;
        this.selectedItem = this.items.find((s) => s.ID === id);
    }

    confirmDelete() {
        if (this.selectedItem == null) {
            return;
        }

        const index = this.addedRecords.indexOf(this.selectedItem);
        if (index > -1) {
            this.addedRecords.splice(index, 1);
        }
        this.deletedRecords.push(this.selectedItem);
        this.items.forEach((element, index) => {
            if (element.ID === this.selectedItem.ID) {
                this.items.splice(index, 1);
            }
        });
        this.formMode = FormMode.Default;
    }

    addNew(name: string, url: string, description: string) {
        let newItem = new HelpMenu();
        newItem.ID = this.newID;
        newItem.Name = name;
        newItem.Url = url;
        newItem.Description = description;
        newItem.visibility = 1;
        newItem.order = this.items.length;
        newItem.isEditable = true;
        newItem.isSystem = false;

        this.newID -= 1;

        this.items.push(newItem);
        this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
        this.addedRecords.push(newItem);
        this.addedRecords.sort((a, b) => (a.order < b.order ? -1 : 1));
        this.formMode = FormMode.Default;
    }

    cancel() {
        this.selectedItem.Name = this.editedName;
        this.selectedItem.Url = this.editedUrl;
        this.selectedItem.Description = this.editedDes;
        this.selectedItem = null;
        this.formMode = FormMode.Default;
    }

    closeSave() {
        this.selectedItem = null;
        this.formMode = FormMode.Default;
    }

    moveUp(id: number) {
        let option = this.items.find((r) => r.ID === id);
        var num = option.order - 1;
        let newOption = this.items.find((o) => o.order === num);

        this.items.forEach((i) => {
            if (i.ID === option.ID) {
                i.order = i.order - 1;
            }
            else if (i.ID === newOption.ID) {
                i.order = i.order + 1;
            }
        });

        this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
    }

    moveDown(id: number) {
        let option = this.items.find((r) => r.ID === id);
        var num = option.order + 1;
        let newOption = this.items.find((o) => o.order === num);

        this.items.forEach((i) => {
            if (i.ID === option.ID) {
                i.order = i.order + 1;
            }
            else if (i.ID === newOption.ID) {
                i.order = i.order - 1;
            }
        });

        this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
    }

    moveTop(id: number) {
        this.items.find((i) => {
            if (i.ID === id) {
                i.order = -1;
                this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
            }
        });

        for (let i = 0; i < this.items.length; i++) {
            this.items[i].order = i;
        }

        this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
    }

    moveBottom(id: number) {
        this.items.find((i) => {
            if (i.ID === id) {
                i.order = 10000000;
                this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
            }
        });

        for (let i = 0; i < this.items.length; i++) {
            this.items[i].order = i;
        }

        this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
    }

    menuItems(includeUp: boolean, includeDown: boolean): any[] {
        if (includeUp && includeDown) {
            return this.editMenuItems
                .concat(this.upMenuItems)
                .concat(this.downMenuItems);
        } else if (includeUp) {
            return this.editMenuItems.concat(this.upMenuItems);
        } else if (includeDown) {
            return this.editMenuItems.concat(this.downMenuItems);
        } else {
            return this.editMenuItems;
        }
    }

    deleteMenuItems(includeUp: boolean, includeDown: boolean): any[] {
        if (includeUp && includeDown) {
            return this.deleteMenuItem
                .concat(this.upMenuItems)
                .concat(this.downMenuItems);
        } else if (includeUp) {
            return this.deleteMenuItem.concat(this.upMenuItems);
        } else if (includeDown) {
            return this.deleteMenuItem.concat(this.downMenuItems);
        } else {
            return this.deleteMenuItem;
        }
    }

    visibilityItems(item: any): any[] {
        if (item.visibility === 1) {
            return this.visibleVisibilityItems;
        }
        else if (item.visibility === 2) {
            return this.adminVisibilityItems;
        }
        else {
            return this.hiddenVisibilityItems;
        }
    }

    clickMenu(e: any, item: any) {
        switch (e.value.toLowerCase()) {
            case this.editMenu.toLowerCase():
                this.edit(item.ID);
                break;
            case this.deleteMenu.toLowerCase():
                this.delete(item.ID);
                break;
            case this.menuMU.toLowerCase():
                this.moveUp(item.ID);
                break;
            case this.menuMTT.toLowerCase():
                this.moveTop(item.ID);
                break;
            case this.menuMD.toLowerCase():
                this.moveDown(item.ID);
                break;
            case this.menuMTB.toLowerCase():
                this.moveBottom(item.ID);
                break;
        }
    }

    changeVisibility(e: any, item: any) {
        switch (e.value.toLowerCase()) {
            case this.visibleMenu.toLowerCase():
                item.visibility = 1;
                break;
            case this.visibleToAdminsMenu.toLowerCase():
                item.visibility = 2;
                break;
            case this.hiddenMenu.toLowerCase():
                item.visibility = 3;
                break;
        }
    }

    valid() {
        if (this.selectedItem.Name === null || this.selectedItem.Name === "") {
            return false;
        }
        if (this.selectedItem.Url === null || this.selectedItem.Url === "") {
            return false;
        }

        return true;
    }

    validAdd(name: string, url: string) {
        if (name === null || name === "") {
            return false;
        }
        if (url === null || url === "") {
            return false;
        }

        return true;
    }

    getVisibilityIcon(id: number) {
        if (id === 1) {
            return "fa fa-eye";
        }
        if (id === 2) {
            return "fa fa-cog";
        }
        if (id === 3) {
            return "fa fa-eye-slash";
        }
        else {
            return "";
        }
    }
}