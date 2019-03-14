import {Component, OnDestroy, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';

import {Breadcrumb} from '../../models/breadcrumb.model';

import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {DiagramService} from '../../services/diagram.service';
import {RightSidebarService} from '../../services/right-sidebar.service';
import {MessagesService} from '../../services/messages.service';
import {PermissionsService} from '../../services/permissions.service';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-mapping-component',
    templateUrl: './mapping.component.html',
    providers: [DiagramService, PermissionsService]
})

export class MappingComponent extends BaseComponent implements OnInit, OnDestroy {
    private mappings: any[] = [];
    private selected: any = null;
    private showEditor: boolean = false;
    private showDelete: boolean = false;
    private theDeleteCallback: Function;

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected diagramService: DiagramService,
        protected messagesService: MessagesService,
        rightSidebarService: RightSidebarService,
        protected permissionsService: PermissionsService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.setCommonRightSideBar(true, false, false, false, false, true);
        this.theDeleteCallback = this.deleteMapping.bind(this);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Mapping');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Mappings'));

        this.loadPermissions(this.permissionsService, 'Map', 0);

        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private load(): void {
        this.isLoading = true;
        this.diagramService.getLineageMappings().subscribe(
            res => {
                this.isLoading = false;

                for (let item of res) {
                    if (item.MapClass == 1) item.MapClassName = "Source To Target";
                }

                this.mappings = res;

                if (this.selected == null && this.mappings.length > 0) {
                    this.selected = this.mappings[0];
                }
            });
    }

    private deleteMapping(id: number): void {
        this.diagramService.deleteLineageMapping(id);
        this.mappings = this.mappings.filter(x => x.ID != id);
        this.showDelete = false;
    }

    private saveMap(event): void {
        this.diagramService.saveLineageMapping(event.item).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type != 'error') {
                    this.load();
                }

                this.showEditor = false;
            });
    }
}
