import { Component, OnInit, Input, Output, EventEmitter, } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SiteNav, SiteNavPermission } from '../../../models/site-menu.model';
import { SiteMenuService } from '../../../services/site-menu.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { FormMode } from '../../../models/form.model';
import { EditorField } from '../../../models/editor-field.model';
import { ResourcesService } from '../../../services/resources.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-site-menu-permissions',
    providers: [SiteMenuService, ResourcesService],
    template: `
<div [ngSwitch]="formMode">
    <div *ngSwitchCase="FormMode.Default">
        <div class="row">
            <div class="col s12" *ngSwitchCase="FormMode.Default">
                <div class="FieldName" i18n>
                    View Permissions
                </div>
                <div class="directions" i18n>
                    By default all users can see each nav folder. If there are any permissions defined below, only those users/groups will see the folder on their nav.
                </div>
                <header>
                    &nbsp;<d3s-tile-actions hasAdd="true" (addClick)="addPermission()"></d3s-tile-actions>
                </header>
                <div>
                    <p-table #dt [value]="siteNav.Permissions" selectionMode="single" [metaKeySelection]="true">
                        <ng-template pTemplate="header">
                            <tr>
                                <th i18n>Permissions</th>
                                <th style="width: 35px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr [pSelectableRow]="item">
                                <td>{{item.Name}}</td>
                                <td>
                                    <div class="RowTools">
                                        <a (click)="delete(item)"><i class="fa fa-trash-o"></i></a>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                    </p-table>
                </div>
            </div>
        </div>
    </div>
    <div *ngSwitchCase="FormMode.Adding">
        <div class="row">
            <div class="col s12">
                <div class="FieldName" i18n>
                    View Permissions
                </div>
                <div class="directions" i18n>
                    By default all users can see each nav folder. If there are any permissions defined below, only those users/groups will see the folder on their nav.
                </div>
                <header>
                    &nbsp;
                </header>
                <div>
                    <d3s-resource-multiselect-grid [multiple]="field.MultiSelect" [(ngModel)]="field.Value" showToolTip="false"  ngDefaultControl [field]="field" [showResourceType]="true" ></d3s-resource-multiselect-grid>  
                    <button pButton type="button" i18n-label label="Cancel" (click)="selection = null; formMode = FormMode.Default; onModeChange.emit(this.formMode);"></button>
                    <button pButton type="button" i18n-label label="Add" (click)="add()" [disabled]="field.Value == null || field.Value.length==0"></button>
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
    field: EditorField;

    public formMode = FormMode.Default;
    FormMode = FormMode;

    permissionItems: any[] = [];
    selection: string;


    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        protected settingsService: CompanySettingsService,
        private siteMenuService: SiteMenuService
    ) {
        super(headerBreadcrumbService, titleService, settingsService);
    }

    ngOnInit() {
        this.isLoading = true;
    }

    add() {
        this.field.Value.forEach(x => {
            let p: SiteNavPermission = new SiteNavPermission();
            p.SiteNavID = this.siteNav.ID;
            p.Object = x.split('|')[0];
            p.ObjectID = +x.split('|')[1];
            p.Name = x.split('|')[2];
           
            let d = this.siteNav.Permissions.filter(x => x.ObjectID == p.ObjectID && x.Object == p.Object);
            if (d == null || d.length==0) 
                this.siteNav.Permissions.push(p);
        });
        this.siteNavChange.emit(this.siteNav);
        this.formMode = FormMode.Default;
        this.onModeChange.emit(this.formMode);

        delete this.field;
    }

    delete(item: SiteNavPermission) {
        let x = this.siteNav.Permissions.findIndex(p => p.Object == item.Object && p.ObjectID == item.ObjectID);
        this.siteNav.Permissions.splice(x, 1);
        this.siteNavChange.emit(this.siteNav);
    }

    addPermission() {
        this.loadPermissionsList(this.siteNav)
            .then(() => {
                this.formMode = FormMode.Adding;
                this.onModeChange.emit(this.formMode);
            });
    }

    loadPermissionsList(item: SiteNav = null): Promise<any> {
        let id = 0;
        if (item && item.ID)
            id = item.ID;

        this.field = new EditorField(); 
        this.field.TypeaheadUri = `navigation/permissions/get/list?id=${id}`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = true;
        return Promise.resolve();
        
    }
}
