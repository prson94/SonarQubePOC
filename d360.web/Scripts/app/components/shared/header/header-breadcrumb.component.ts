import { Component, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Input } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { Subscription } from 'rxjs';
import * as _ from 'lodash';
import { windowWhen } from 'rxjs/operators';
import { clearLine } from 'readline';

@Component({
    selector: 'd3s-header-breadcrumb',
    template: ` <div #bread class="breadcrumbs" (window:resize)="onResize($event)">
                <span (mouseleave)="hideSmallPanel(smallPanel)" (mouseenter)="fixHeight($event,smallPanel)"> 
                    <i #collapseIcon *ngIf="showLastOnly" class="fa fa-ellipsis-h breadcrumb-collapse" aria-hidden="true"></i>
                    <div #smallPanel ngClass="collapsed-overlay">
                        <div *ngFor="let breadcrumb of breadcrumbs;let last=last;let index=index" class="collapsed-crumb-container">
                            <d3s-header-breadcrumb-item [ngClass]="'collapsed-crumb'" [ngStyle]="{'padding-left': index *10 + 'px'}" [index]="index" [showSeperator]="false" [breadcrumb]="breadcrumb" [isLastItem]="last" (treeClick)="handleTreeClick($event)"></d3s-header-breadcrumb-item>
                        </div>
                    </div>
                </span>
                <div *ngFor="let breadcrumb of breadcrumbs;let last=last;let index=index">
                    <d3s-header-breadcrumb-item *ngIf="(showLastOnly && last) || breadcrumb.show" [breadcrumb]="breadcrumb" [isLastItem]="last" [lastItem]="breadcrumbs[breadcrumbs.length - 1]" (treeClick)="handleTreeClick($event)" [maxLastCrumbWidth]="maxSpaceForCrumbs"></d3s-header-breadcrumb-item>                    
                </div>  
                <div class="object-state" *ngIf="objectState"> - {{objectState}}</div>
                </div>  
              `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderBreadcrumbComponent {
    @Input() controlWidth: number;
    @Input() showBackButton: boolean = false;
    subscriptionPop: Subscription;
    subscriptionClear: Subscription;
    subscriptionBuildFromStorage: Subscription;
    subscriptionAdd: Subscription;
    subscriptionChangeState: Subscription;
    breadcrumbs: Breadcrumb[];
    showLastOnly: boolean = false;
    showThisManyCrumbs: number = 0;
    @ViewChild('bread', { static: false }) breadcrumbUIElement;
    @ViewChild('collapseIcon', { static: false }) collapseIcon;
    private resizeTimer: any;
    private maxSpaceForCrumbs: number = 800;
    private maxWidthOfSmallPanel: number = window.innerWidth - 200;
    objectState: string = '';

    constructor(
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private ref: ChangeDetectorRef
    ) {
        this.breadcrumbs = [];
        this.subscriptionAdd = headerBreadcrumbService.breadcrumbs$.subscribe(
            breadcrumb => {
                if (!_.isEqual(_.omit(this.breadcrumbs[this.breadcrumbs.length - 1], ['active']), _.omit(breadcrumb, ['active']))) {

                    if (this.breadcrumbs.length != 0) {
                        this.breadcrumbs[this.breadcrumbs.length - 1].active = true;
                        breadcrumb.active = false;
                    }
                    this.breadcrumbs.push(breadcrumb);
                    setTimeout(() => { this.resizeControlsToFit(window.innerWidth); }, 100);
                    headerBreadcrumbService.saveBreacrumbsToStorage(this.breadcrumbs)
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
        this.subscriptionBuildFromStorage = headerBreadcrumbService.buildFromStorage$.subscribe(res => {
            this.breadcrumbs = res;
        });

        this.subscriptionChangeState = headerBreadcrumbService.currentObjectStateSource$.subscribe(res => {
            this.objectState = res;
            this.ref.markForCheck();
        });
    }


    hideSmallPanel(item) {
        item.style.display = "none";
    }

    fixHeight($event, smallPanel) {
        smallPanel.style.display = "block";
        smallPanel.style.maxWidth = this.maxWidthOfSmallPanel + "px";
    }

    ngOnDestroy() {
        if (this.subscriptionPop) {
            this.subscriptionPop.unsubscribe();
        }
        if (this.subscriptionClear) {
            this.subscriptionClear.unsubscribe();
        }
        if (this.subscriptionAdd) {
            this.subscriptionAdd.unsubscribe();
        }
        if (this.subscriptionBuildFromStorage) {
            this.subscriptionBuildFromStorage.unsubscribe();
        }
        if (this.subscriptionChangeState) {
            this.subscriptionChangeState.unsubscribe();
        }
    }

    private handleTreeClick(event) {
        this.headerBreadcrumbService.breadcrumbTreeClick(event.id);
    }
    private setMaxWidth() {
        return this.maxWidthOfSmallPanel;
    }
    resizeControlsToFit(windowWidth) {

        this.breadcrumbs.forEach(x => { x.show = false });

        let element = this.breadcrumbUIElement.nativeElement;
        var controlsWidth = this.controlWidth ? this.controlWidth : 0; // only visible medium and up
        let logo = this.showBackButton ? element.parentElement.previousElementSibling.previousElementSibling : element.parentElement.previousElementSibling;
        var logoWidth = logo.offsetWidth;
        var breadcrumbWidth = element.offsetWidth;

        this.maxSpaceForCrumbs = windowWidth - (controlsWidth + logoWidth);
        this.maxWidthOfSmallPanel = windowWidth - logoWidth;

        //if the width of this + the logo + the controls is bigger than screen start hiding breadcrumbs
        var worseCaseWidth = this.estimateMaxLength(this.maxSpaceForCrumbs) + logoWidth + controlsWidth;

        if (worseCaseWidth > windowWidth) {
            this.showLastOnly = true;
            this.showCrumb();
        }
        else {
            this.showLastOnly = false;
            this.breadcrumbs.forEach(x => { x.show = true; });
        }

        this.ref.markForCheck();
    }

    onResize(event) {
        clearTimeout(this.resizeTimer);
        this.resizeTimer = window.setTimeout(() => this.resizeControlsToFit(event.target.innerWidth), 50);
    }

    estimateMaxLength(maxSpaceForCrumbs: number): number {
        let max = 0;
        let maxNumberOfCrumbsInSpace = 0;
        let html = '';
        for (var i = this.breadcrumbs.length - 1; i >= 0; i--) {

            html = '<a class="breadcrumb"><span class="breadcrumb-text">' + this.breadcrumbs[i].text + ' </span>';
            if (this.breadcrumbs[i].parentTypeName !== undefined)
                html += '<span class="parent">' + this.breadcrumbs[i].parentTypeName ? this.breadcrumbs[i].parentTypeName : '' + '</span>'
            html += '</a>';

            this.breadcrumbUIElement.nativeElement.insertAdjacentHTML('beforeend', html);

            let tempCrumb = this.breadcrumbUIElement.nativeElement.lastElementChild;

            max += tempCrumb.offsetWidth;

            var last = (this.breadcrumbs.length - 1) == i;
            if (!last)
                max += 25 // for the icon separator

            if (max < maxSpaceForCrumbs)
                maxNumberOfCrumbsInSpace++;

            this.breadcrumbUIElement.nativeElement.removeChild(tempCrumb);
        }
        this.showThisManyCrumbs = maxNumberOfCrumbsInSpace;
        if (this.showLastOnly)
            max += 40;
        return max + 20;//for the left margin on the breadcrumb
    }

    showCrumb() {
        if (this.showThisManyCrumbs == 0)
            return;

        if (this.breadcrumbs.length == this.showThisManyCrumbs) {
            this.showLastOnly = false;
            this.breadcrumbs.forEach(x => { x.show = true });
            return;
        }
        let maxIndex = this.breadcrumbs.length - 1;
        let minIndex = this.breadcrumbs.length - this.showThisManyCrumbs;
        for (var i = 0; i < this.breadcrumbs.length; i++) {
            if (i >= minIndex && i <= maxIndex) this.breadcrumbs[i].show = true;
            else this.breadcrumbs[i].show = false;

        }


    }
}
