import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-header',
    template: ` <div class="navbar-fixed">
                <nav class="top">  
                    <span class="logo" routerLink="/home" style="cursor:pointer;"></span>                                 
                    <d3s-header-breadcrumb [controlWidth]="controlWidth"></d3s-header-breadcrumb>                                          
                    <d3s-header-actions style="margin-left:auto;" (controlWidthChange)="controlWidth = $event"></d3s-header-actions>
                </nav>
                </div>
              `,    
})

export class HeaderComponent extends BaseComponent {   
    public controlWidth: number = 0;

    constructor(private router: Router, private route: ActivatedRoute) {
        super();
    }
}

