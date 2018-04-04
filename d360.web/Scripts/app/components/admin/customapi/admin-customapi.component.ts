import { Component } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-customapi',
    providers: [],
    template: ` 
                Welcome to the custom API Admin page
                `
})

export class AdminCustomAPIComponent extends AdminBaseComponent {
    constructor(headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesService, titleService: Title) {
        super(headerBreadcrumbService, titleService);
        this.areaName = "Custom API";
        this.setCommonItems();
    }
}