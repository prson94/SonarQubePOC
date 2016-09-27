import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-header',
    template: ` <div class="navbar-fixed">
                <nav class="top">                                   
                    <d3s-header-breadcrumb></d3s-header-breadcrumb>                      
                    <d3s-header-actions></d3s-header-actions>
                    <d3s-raise-issue-button *ngIf="hasRaiseIssueButton"></d3s-raise-issue-button>              
                </nav>
                </div>
              `,
})

export class HeaderComponent extends BaseComponent implements OnInit, OnDestroy {
    private hasRaiseIssueButton : boolean = true;
    private sub: any;


    constructor(private router: Router, private route: ActivatedRoute) {
        super();
    }

    ngOnInit() {
        this.sub = this.router.events.subscribe(path => {
            //dont show raise issue button on raise issue screen or any admin screens            
            this.hasRaiseIssueButton = (!path.url.toLowerCase().endsWith('workflow/raiseissue') && (path.url.toLowerCase().indexOf('/admin/')==-1));
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

}

