import { Component, ViewChild } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { Subscription }   from 'rxjs/Subscription';

@Component({
    selector: 'd3s-header-breadcrumb',
    template: ` <div #bread class="breadcrumbs" (window:resize)="onResize($event,bread)">
                 <div *ngFor="let breadcrumb of breadcrumbs;let last=last" [ngClass]="{'active':last,'inactive':!last}">
                    <d3s-header-breadcrumb-item *ngIf="(showLastOnly && last) || !showLastOnly" [breadcrumb]="breadcrumb" [lastItem]="last" (treeClick)="handleTreeClick($event)"></d3s-header-breadcrumb-item>                    
                 </div>                
                </div>                
              `
})

export class HeaderBreadcrumbComponent {
    subscriptionPop: Subscription;
    subscriptionClear: Subscription;
    subscriptionAdd: Subscription;
    breadcrumbs: Breadcrumb[];
    showLastOnly: boolean = false;
    @ViewChild('bread') breadcrumbUIElement;
        
    constructor(private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.breadcrumbs = [];
        this.subscriptionAdd = headerBreadcrumbService.breadcrumbs$.subscribe(
            breadcrumb => {
                this.breadcrumbs.push(breadcrumb);
                this.resizeControlsToFit(window.innerWidth, this.breadcrumbUIElement);    
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

    resizeControlsToFit(windowWidth, element) {
        if (windowWidth < 650) {
            this.showLastOnly = true;
            return;
        }

        var controlsWidth = (windowWidth > 991) ? 360 : 0; // only visible medium and up
        var logoWidth = 200;
        var breadcrumbWidth = element.offsetWidth;        

        var combinedWidth = controlsWidth + logoWidth + breadcrumbWidth;

        //if the width of this + the logo + the controls is bigger than screen start hiding breadcrumbs
        
        if (combinedWidth > windowWidth) {        
            this.showLastOnly = true;
        }
        else {
            //check how many breadcrumbs there are and what would happen if we showed the full version            
            var worseCaseWidth = this.maxLength() + logoWidth + controlsWidth;
            if (worseCaseWidth > windowWidth) {                
                this.showLastOnly = true;
            }
            else {                
                this.showLastOnly = false;
            }
        }
    }

    onResize(event, element) {                
        this.resizeControlsToFit(event.target.innerWidth, element);
    }

    maxLength(): number {
        let max = 0;
        //for (let breadcrumb of this.breadcrumbs) {
        for (var i = 0; i < this.breadcrumbs.length; i++){
            var last = (this.breadcrumbs.length - 1) == i;
            if (!last)
                max += this.breadcrumbs[i].text.length * 10; // 10 is based on the font size.
            else
                max += 280; //width of search textbox shown on hoover.
        }
        return max;
    }
}
