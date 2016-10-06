import { Component } from '@angular/core';
import { HeaderActionsService } from '../../services/header-actions.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-header-actions',
    template: `
                <ul class="right hide-on-med-and-down">
                    <li *ngIf="headerActionsService.showFavorite" style="cursor: pointer"><d3s-header-favorites></d3s-header-favorites></li>
                    <li *ngIf="headerActionsService.showFollow" style="cursor: pointer"><d3s-header-follow></d3s-header-follow></li>
                    <li *ngIf="headerActionsService.showHelp"><a href="#" class="help"><i class="fa fa-question-circle"></i></a></li>
                    <li *ngIf="headerActionsService.showSearch"><d3s-header-typeahead-search></d3s-header-typeahead-search></li>
                    <li *ngIf="headerActionsService.showNotifications"><a href="#"><i class="fa fa-bell-o"></i></a></li>
                    <li><a [routerLink]="'/a/resource/' + resourceId" class="photo"><img [src]="'/resources/image/' + resourceId + '?size=25'" height="25" width="25" /></a></li>
                </ul> 
                `,
})

export class HeaderActionsComponent {        
    private resourceId: number = CurrentResourceID;

    constructor(private headerActionsService: HeaderActionsService) { }
}

