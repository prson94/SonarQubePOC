import { Component, NgZone, OnDestroy, Input, Output } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { MessagesService } from '../../../services/messages.service';
import { MapsService } from '../../../services/maps.service';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { FormMode } from '../../../models/form.model';
import { MapType } from '../../../models/map.model';

@Component({
    selector: 'd3s-admin-maps-template-editor',
    providers: [MapsService],
    template: ``
})

export class AdminMapsTemplateEditorComponent extends BaseComponent {
    @Input() mapTypeTemplateId: number = null;
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(private mapsService: MapsService) {
        super();
    }
}


