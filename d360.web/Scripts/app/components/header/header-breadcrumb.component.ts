///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ROUTER_DIRECTIVES } from '@angular/router';

import { Subscription }   from 'rxjs/Subscription';

@Component({
    selector: 'd3s-header-breadcrumb',
    directives: [ROUTER_DIRECTIVES],
    template: ` <span class="breadcrumbs">
                 <span *ngFor="let breadcrumb of breadcrumbs;let last=last" [ngClass]="{active:last}">
                    <a *ngIf="breadcrumb.hasLink()" [routerLink]="[breadcrumb.link]">{{ breadcrumb.text }}</a>
                    <span *ngIf="!breadcrumb.hasLink()">{{ breadcrumb.text }}</span> <span *ngIf="!last" class="sep"> :: </span>
                 </span>                
                </span>
              `
})

export class HeaderBreadcrumbComponent {
    subscription: Subscription;
    breadcrumbs : Breadcrumb[];

    constructor(private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.breadcrumbs = [];
        this.subscription = headerBreadcrumbService.breadcrumbs$.subscribe(
            breadcrumb => {
                this.breadcrumbs.push(breadcrumb);               
            });
        this.subscription = headerBreadcrumbService.breadcrumbClear$.subscribe(
            breadcrumb => {
                this.breadcrumbs.splice(0, this.breadcrumbs.length);                
            })
    }

    ngOnDestroy() {
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
    }
}
