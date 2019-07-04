import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input } from '@angular/core';
import { Router } from '@angular/router';
import { RightSidebarService  } from '../../../services/right-sidebar.service';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { Subscription }   from 'rxjs';
import * as _ from 'lodash';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';

@Component({
    selector: 'd3s-right-sidebar',      
    template: ` <div *ngIf="showHeader" class="title-bar" [ngClass]="{'menu-open': menuOpen}">
                    <div class="title">
                         <img class="icon" *ngIf="!IsIcon(area.icon)" [src]="GetURL(area.icon)"  height="20" width="20" />
                         <i *ngIf="IsIcon(area.icon)" [class]="'icon fa ' + area.icon"></i>
                        <h1 >{{area.title ? area.title: 'D3S'}}</h1>
                        <span *ngIf="ShowScore()" class="d3s-icon large-icon light-orange">
                            <d3s-dynamic-percentage [percentage]="50"></d3s-dynamic-percentage> 
                             <span class="text">50%</span>
                        </span> 
                        <span *ngIf="IsDraft()" class="d3s-icon large-icon">
                            <i class="fa fa-certificate"></i>
                            <span class="text">Draft</span>
                        </span>
                        <span class="grow"></span>
                        <button class="button"><i class="fa fa-certificate"></i><span>Request Certification</span></button>
                        <button class="primary button"><i class="fa fa-edit"></i><span>Take Survey</span></button>
                    </div>
                    <div *ngIf="items && items.length > 0" class="tab-view">
                        <div class="tab-bar-outer">
                            <div class="tab-bar can-overflow">
                                <button class="tab" [ngClass]="{'selected':AllClosed()}" (click)="itemClicked({active:false,title:'homeClick', url: 'blank'})">{{area.title}}</button>
                                <button class="tab" [ngClass]="{'selected':item.active}" *ngFor="let item of items; trackBy: trackById" (click)="item.active=!item.active;itemClicked(item);">{{item.title}}</button>
                            </div>
                        </div>
                    </div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class RightSidebarComponent {    
    subscription: Subscription;
    subscriptionClear: Subscription;
    areaSub: Subscription;
    hideHeaderSub: Subscription;
    items: RightSidebarItem[];  
    hostUrl: string;
    area: any = {icon:'fa-folder',title: ''};
    @Input() menuOpen: boolean;
    showHeader: boolean = false;

    /*
     <div *ngIf="items && items.length > 0" class="hide-on-small-only right-sidebar">
        <div *ngFor="let item of items; trackBy: trackById">
            <d3s-right-sidebar-item [active]="item.active" (activeChange)="item.active=$event;itemClicked(item)" [title]="item.title" [activeIcons]="item.icons"></d3s-right-sidebar-item>
        </div>
    </div>
     */

    constructor(
        private rightSidebarService: RightSidebarService,
        private ref: ChangeDetectorRef,
        private router: Router
    ) {        
        this.items = [];
        this.subscription = rightSidebarService.rightSidebar$.subscribe(
            item => {
                this.items.push(item);
                this.items = _.sortBy(this.items, 'title');                
                ref.markForCheck();
            });
        this.subscriptionClear = rightSidebarService.rightSidebarClear$.subscribe(
            item => {
                this.items.splice(0, this.items.length);                                
                ref.markForCheck();
            })
        this.areaSub = rightSidebarService.currentArea$.subscribe(
            area => {
                this.area = area;
                ref.markForCheck();
            }
        );
        this.hideHeaderSub = rightSidebarService.hideHeader$.subscribe(result => {
            this.showHeader = result;
            ref.markForCheck();
        });
    }

    IsIcon(icon: string) {
        return !_.startsWith(icon.toUpperCase(), "URL-");
    }

    GetURL(icon: string) {
        if(icon)
            return icon.replace(/^URL-+/i, '');
    }
    ShowScore() {
        return true;
    }

    IsDraft() {
        return true;
    }
    ngOnDestroy() {        
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
        this.subscriptionClear.unsubscribe();
        this.areaSub.unsubscribe();
        this.hideHeaderSub.unsubscribe();
    }

    trackById(index, item) {        
        return item.tag;
    }
    
    itemClicked(item: RightSidebarItem) {   
        console.log(item);
        if (item.active) {
            //look for any other already active items and fire click for them
            let isFirstItemOpen = true;
            for (let ritem of this.items) {                
                if (ritem.active && ritem.title != item.title) {
                    this.rightSidebarService.itemClicked(ritem);
                    ritem.active = false;
                    isFirstItemOpen = false;                     
                }
            }            
            if (isFirstItemOpen) this.hostUrl = this.router.url;            
            this.rightSidebarService.itemClicked(item);
            if (item.hasDynamicUrl) this.router.navigateByUrl(item.dynamicUrlCallback());
            else if (item.url) this.router.navigateByUrl(item.url);
        }        
        else {
            //return to previous url if the item is a url otherwise fire click event            
            if (item.url)
                this.router.navigateByUrl(this.hostUrl);
            else
                this.rightSidebarService.itemClicked(item);
        }
        this.AllClosed();
    }     
    AllClosed() {
        let count = this.items.filter(x => x.active == true).length;
        
        return count == 0;
    }
};
