///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';

@Component({
    selector: 'd3s-header',
    template: ` 
                <nav class="top">                                   
                    <d3s-header-breadcrumb></d3s-header-breadcrumb>                    
                    <d3s-header-actions></d3s-header-actions>
                </nav>
              `,
})

export class HeaderComponent { }

