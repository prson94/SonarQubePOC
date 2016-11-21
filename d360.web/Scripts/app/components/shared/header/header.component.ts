import { Component } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-header',
    template: ` <div class="navbar-fixed">
                <nav class="top">  
                    <span class="logo" routerLink="" style="cursor:pointer;"></span>                                 
                    <d3s-header-breadcrumb></d3s-header-breadcrumb>                                          
                    <d3s-header-actions></d3s-header-actions>
                </nav>
                </div>
              `,
})

export class HeaderComponent extends BaseComponent {    

    constructor(private router: Router, private route: ActivatedRoute) {
        super();
    }
}

