import { Component, OnInit, Input, Output, EventEmitter, } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SiteNav, SiteNavPermission } from '../../../models/site-menu.model';
import { SiteMenuService } from '../../../services/site-menu.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { FormMode } from '../../../models/form.model';

@Component({
    selector: 'd3s-admin-site-menu-permissions',
    providers: [SiteMenuService],
    template: `
<div [ngSwitch]="formMode">
    <div *ngSwitchCase="FormMode.Default">
        <div class="row">
            <div class="col s12" *ngSwitchCase="FormMode.Default">
                <div class="FieldName">
                    View Permissions
                </div>
                <div class="directions">
                    By default all users can see each nav folder. If there are any permissions defined below, only those users/groups will see the folder on their nav.
                </div>
                <header>
                    &nbsp;<d3s-tile-actions hasAdd="true" (addClick)="addPermission()"></d3s-tile-actions>
                </header>
                <div>
                    <p-dataTable [value]="siteNav.Permissions" selectionMode="single">
                        <p-column field="Name" header="Permissions"></p-column>
                        <p-column [style]="{'width': '35px'}">
                            <template let-item="rowData" let-i="index" pTemplate type="body">
                                <div class="RowTools">
                                    <a (click)="delete(item)"><i class="fa fa-trash-o"></i></a>
                                </div>
                            </template>
                        </p-column>
                    </p-dataTable>
                </div>
            </div>
        </div>
    </div>
    <div *ngSwitchCase="FormMode.Adding">
        <div class="row">
            <div class="col s12">
                <div class="FieldName">
                    View Permissions
                </div>
                <div class="directions">
                    By default all users can see each nav folder. If there are any permissions defined below, only those users/groups will see the folder on their nav.
                </div>
                <header>
                    &nbsp;
                </header>
                <div class="FieldName">
                    Choose a group or resource
                </div>
                <div>
                    <select [(ngModel)]="selection" style="min-width: 200px;">
                        <option *ngFor="let p of permissionItems" [value]="p.value">{{p.label}}</option>
                    </select>
                    <button pButton type="button" label="Cancel" (click)="selection = null; formMode = FormMode.Default; onModeChange.emit(this.formMode);"></button>
                    <button pButton type="button" label="Add" (click)="add()" [disabled]="selection == null"></button>
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
        .mock-header {
            padding: 3px 0 15px 5px;
            position: relative;
        }
  `],
})

export class AdminSiteMenuPermissionsComponent extends AdminBaseComponent implements OnInit {
    @Input() siteNav: SiteNav;
    @Output() siteNavChange = new EventEmitter();
    @Output() onModeChange = new EventEmitter();

    formMode = FormMode.Default;
    FormMode = FormMode;

    permissionItems: any[] = [];
    selection: string;


    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        private siteMenuService: SiteMenuService
    ) {
        super(headerBreadcrumbService, titleService);
    }

    ngOnInit() {
        this.isLoading = true;
    }

    add() {
        let p: SiteNavPermission = new SiteNavPermission();
        p.SiteNavID = this.siteNav.ID;
        p.Object = this.selection.split('|')[0];
        p.ObjectID = +this.selection.split('|')[1];
        p.Name = this.permissionItems.find(i => i.value == this.selection).label;

        console.log(p, this.permissionItems);

        this.siteNav.Permissions.push(p);
        this.siteNavChange.emit(this.siteNav);
        this.formMode = FormMode.Default;
        this.onModeChange.emit(this.formMode);
        this.selection = null;
        
    }

    delete(item: SiteNavPermission) {
        let x = this.siteNav.Permissions.findIndex(p => p.Object == item.Object && p.ObjectID == item.ObjectID);
        this.siteNav.Permissions.splice(x, 1);
        this.siteNavChange.emit(this.siteNav);
    }

    addPermission() {
        this.loadPermissionsList()
            .then(() => {
                this.formMode = FormMode.Adding;
                this.onModeChange.emit(this.formMode);
            });
    }

    loadPermissionsList(item: SiteNav = null): Promise<any> {
        let id = 0;
        if (item != null)
            id = item.ID;
        return this.siteMenuService.getSiteNavPermissionsList(id)
            .then(r => {
                this.permissionItems = [];
                r.forEach(i => {
                    let ix = this.siteNav.Permissions.findIndex(p => p.Object + '|' + p.ObjectID.toString() == i.value);
                    if (ix < 0)
                        this.permissionItems.push(i);
                });
            });
    }
}
