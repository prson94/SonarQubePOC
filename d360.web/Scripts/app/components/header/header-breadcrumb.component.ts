
import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Subscription }   from 'rxjs/Subscription';

@Component({
    selector: 'd3s-header-breadcrumb',
    template: ` <span class="breadcrumbs">
                 <span *ngFor="let breadcrumb of breadcrumbs;let last=last" [ngClass]="{active:last}">
                    <d3s-header-breadcrumb-item [breadcrumb]="breadcrumb" [lastItem]="last" (treeClick)="handleTreeClick($event)"></d3s-header-breadcrumb-item>                    
                 </span>                
                </span>
              `
})

export class HeaderBreadcrumbComponent {
    subscriptionPop: Subscription;
    subscriptionClear: Subscription;
    subscriptionAdd: Subscription;
    breadcrumbs : Breadcrumb[];

    constructor(private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.breadcrumbs = [];
        this.subscriptionAdd = headerBreadcrumbService.breadcrumbs$.subscribe(
            breadcrumb => {
                this.breadcrumbs.push(breadcrumb);               
            });
        this.subscriptionClear = headerBreadcrumbService.breadcrumbClear$.subscribe(
            breadcrumb => {
                this.breadcrumbs.splice(0, this.breadcrumbs.length);                
            })
        this.subscriptionPop = headerBreadcrumbService.breadcrumbPopLastSource$.subscribe(
            breadcrumb => {
                this.breadcrumbs.pop();
            })
    }

    ngOnDestroy() {
        // prevent memory leak when component destroyed
        this.subscriptionPop.unsubscribe();
        this.subscriptionClear.unsubscribe();
        this.subscriptionAdd.unsubscribe();
    }

    private handleTreeClick(event) {
        this.headerBreadcrumbService.breadcrumbTreeClick(event.id);
    }
}
