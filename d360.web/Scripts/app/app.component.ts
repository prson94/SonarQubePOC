import { Component, AfterViewInit, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { MessagesService } from './services/messages.service';
import { HeaderBreadcrumbService } from './services/header-breadcrumb.service';
import { HeaderActionsService } from './services/header-actions.service';
import { RightSidebarService } from './services/right-sidebar.service';
import { StateService } from './services/state.service';
import { SiteMessage } from './models/site-message.model';
import { Subscription } from 'rxjs';
import { Message } from 'primeng/components/common/api';



@Component({
    selector: 'd3s-app',
    template: ` <header>
                    <d3s-header></d3s-header>
                    <d3s-site-menu (menuChanged)="handleMenuChange($event)" [menuOpen]="menuOpen"></d3s-site-menu>
                </header>
                <main>
                    <div class="row">
                        <div class="col s12">
                            <div [class.maincontent]="!menuOpen" [class.maincontent-open]="menuOpen">
                                <router-outlet></router-outlet>
                            </div>
                        </div>
                    </div>
                    <d3s-right-sidebar></d3s-right-sidebar>
                </main>
                <p-growl [immutable] ="false" [value]="msgs"></p-growl>
              `
})

export class AppComponent implements AfterViewInit, OnDestroy {    
    subscription: Subscription;
    msgs: Message[];
    public menuOpen: boolean = false;

    constructor(                
        private messagesService: MessagesService,
        protected headerActionsService: HeaderActionsService) {
        this.msgs = [];
        this.subscription = messagesService.errorMessage$.subscribe(
            errorMsg => {
                this.msgs.push({ severity: 'error', summary: errorMsg.summary, detail: errorMsg.detail });
            });
        this.subscription = messagesService.infoMessage$.subscribe(
            infoMsg => {
                this.msgs.push({ severity: 'info', summary: infoMsg.summary, detail: infoMsg.detail });
            });
    }

    ngAfterViewInit() {        
        this.headerActionsService.emitFavoritesChange(); //on first load when a non-default home page is defined, we need to update the action icons                       
    }

    private handleMenuChange(v: boolean) {
        this.menuOpen = v;
    }

    ngOnDestroy() {        
        this.subscription.unsubscribe();
    }
}
