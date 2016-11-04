import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Subscription }   from 'rxjs/Subscription';

@Component({
    selector: 'd3s-header-breadcrumb',
    template: ` <div #bread class="breadcrumbs hide-on-small-only" (window:resize)="onResize($event,bread)">
                 <span *ngFor="let breadcrumb of breadcrumbs;let last=last" [ngClass]="{active:last}">
                    <d3s-header-breadcrumb-item [breadcrumb]="breadcrumb" [lastItem]="last" (treeClick)="handleTreeClick($event)"></d3s-header-breadcrumb-item>                    
                 </span>                
                </div>
                <div class="breadcrumbs hide-on-med-and-up" *ngIf="breadcrumbs.length>0">
                    <d3s-header-breadcrumb-item [breadcrumb]="breadcrumbs[this.breadcrumbs.length-1]" lastItem="true" (treeClick)="handleTreeClick($event)"></d3s-header-breadcrumb-item>
                </div>
              `
})

export class HeaderBreadcrumbComponent {
    subscriptionPop: Subscription;
    subscriptionClear: Subscription;
    subscriptionAdd: Subscription;
    breadcrumbs: Breadcrumb[];
    

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

    onResize(event, element) {
   /*     var windowWidth = event.target.innerWidth;
        var controlsWidth = (windowWidth > 600) ? 360 : 0; // only visible medium and up
        var logoWidth = 200;
        var breadcrumbWidth = element.offsetWidth;

        var combinedWidth = controlsWidth + logoWidth + breadcrumbWidth;

        //if the width of this + the logo + the controls is bigger than screen start hiding breadcrumbs
        console.log(windowWidth); //window width
        console.log(combinedWidth);

        if (combinedWidth > windowWidth) {
            console.log('bigger');
            
        }
        else {
            console.log('smaller');
        }*/
        
    }
}
