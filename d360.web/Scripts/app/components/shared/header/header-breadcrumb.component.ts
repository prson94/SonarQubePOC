import { Component, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Input } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { Subscription }   from 'rxjs';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-header-breadcrumb',
    template: ` <div #bread class="breadcrumbs" (window:resize)="onResize($event)">
                <span (mouseleave)="smallPanel.hide($event)" (mouseenter)="smallPanel.show($event)"> 
                    <i *ngIf="showLastOnly" class="fa fa-ellipsis-h breadcrumb-collapse" aria-hidden="true"></i>
                    <p-overlayPanel #smallPanel ngClass="collapsed-overlay">
                        <div *ngFor="let breadcrumb of breadcrumbs;let last=last;let index=index" class="collapsed-crumb-container">
                            <d3s-header-breadcrumb-item [ngClass]="'collapsed-crumb'" [ngStyle]="{'padding-left': index *10 + 'px'}" [index]="index" [showSeperator]="false" [breadcrumb]="breadcrumb" [isLastItem]="last" (treeClick)="handleTreeClick($event)"></d3s-header-breadcrumb-item>
                        </div>
                    </p-overlayPanel>
                </span>
                <div *ngFor="let breadcrumb of breadcrumbs;let last=last">
                    <d3s-header-breadcrumb-item *ngIf="(showLastOnly && last) || !showLastOnly" [breadcrumb]="breadcrumb" [isLastItem]="last" [lastItem]="breadcrumbs[breadcrumbs.length - 1]" (treeClick)="handleTreeClick($event)"></d3s-header-breadcrumb-item>                    
                </div>                
                </div>  
              `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderBreadcrumbComponent {
    @Input() controlWidth: number;
    subscriptionPop: Subscription;
    subscriptionClear: Subscription;
    subscriptionAdd: Subscription;
    breadcrumbs: Breadcrumb[];
    showLastOnly: boolean = false;
    @ViewChild('bread') breadcrumbUIElement;
        
    constructor(
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private ref: ChangeDetectorRef
    ) {
        this.breadcrumbs = [];
        this.subscriptionAdd = headerBreadcrumbService.breadcrumbs$.subscribe(
            breadcrumb => {
                if (!_.isEqual(_.omit(this.breadcrumbs[this.breadcrumbs.length - 1], ['active']), _.omit(breadcrumb,['active']))) {

                    if (this.breadcrumbs.length != 0) {
                        this.breadcrumbs[this.breadcrumbs.length - 1].active = true;
                        breadcrumb.active = false;
                    }
                    this.breadcrumbs.push(breadcrumb);
                    this.resizeControlsToFit(window.innerWidth);
                    this.ref.markForCheck();
                }
            });
        this.subscriptionClear = headerBreadcrumbService.breadcrumbClear$.subscribe(
            breadcrumb => {
                this.breadcrumbs.splice(0, this.breadcrumbs.length);                
                this.ref.markForCheck();
            })
        this.subscriptionPop = headerBreadcrumbService.breadcrumbPopLastSource$.subscribe(
            breadcrumb => {
                this.breadcrumbs.pop();                
                this.ref.markForCheck();
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

    resizeControlsToFit(windowWidth) {
        if (windowWidth < 650) {
            this.showLastOnly = true;
            return;
        } 
        let element = this.breadcrumbUIElement.nativeElement;
        var controlsWidth = (windowWidth > 991) ? this.controlWidth : 0; // only visible medium and up
        let logo = element.parentElement.previousSibling;
        var logoWidth = logo.offsetWidth;
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

    onResize(event) {                
        this.resizeControlsToFit(event.target.innerWidth);
    }

    maxLength(): number {
        let max = 0;                
        for (var i = 0; i < this.breadcrumbs.length; i++){
            var last = (this.breadcrumbs.length - 1) == i;
            if (!last)
                max += (this.breadcrumbs[i].text ? (this.breadcrumbs[i].text.length * 10) : 0); // 10 is based on the font size.
            else
                max += 280; //width of search textbox shown on hoover.
        }
        return max;
    }
 
}
