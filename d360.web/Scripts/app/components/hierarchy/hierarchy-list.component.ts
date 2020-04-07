import { Component } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-hierarchy-list',
    providers: [],
    templateUrl: 'hierarchy-list.component.html' 
})

export class HierarchyListComponent extends BaseComponent {
    constructor() {
        super();
    }
}