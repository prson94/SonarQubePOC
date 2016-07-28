///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';
import { HeaderActionsService } from '../../services/header-actions.service';
import { HeaderTypeaheadSearchComponent } from './header-typeahead-search.component';

@Component({
    selector: 'd3s-header-actions',
    templateUrl: 'Navigation/HeaderActions',
    directives: [ROUTER_DIRECTIVES, HeaderTypeaheadSearchComponent]
})

export class HeaderActionsComponent {    
    constructor(private headerActionsService: HeaderActionsService) { }
}

