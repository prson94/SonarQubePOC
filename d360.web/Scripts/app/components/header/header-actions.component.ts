///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { HeaderActionsService } from '../../services/header-actions.service';

@Component({
    selector: 'd3s-header-actions',
    templateUrl: 'Navigation/HeaderActions',
})

export class HeaderActionsComponent {    
    constructor(private headerActionsService: HeaderActionsService) { }
}

