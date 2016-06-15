///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { HeaderBreadcrumbComponent } from './header.breadcrumb.component';
import { HeaderActionsComponent } from './header.actions.component';

@Component({
    selector: 'd3s-header',
    template: ` <nav class="top">                    
                    <d3s-header-breadcrumb></d3s-header-breadcrumb>
                    <d3s-header-actions></d3s-header-actions>
                </nav>
              `,
    directives: [HeaderBreadcrumbComponent, HeaderActionsComponent]
})

export class HeaderComponent { }

