
import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms'; 
import * as _ from 'lodash';
import { AdvancedSearchFilter } from '../../../../models/search-result.model';

@Component({
    selector: 'd3s-chips-filter',
    template: `
		        <div class="chips-input">
                    <div class="chip-option" *ngFor="let item of selectedFilters">{{item.field}}: {{item.value}}<i class="fa fa-times-circle" (click)="removeFilterOption(item)"></i></div>
                    <div class="chip-option clickable" (click)="toggleMenu()" (blur)="close()">
                        Add Filter...
                        <div class="popup-menu" [ngClass]="{'popup-open': openMenu || isInputOpen}">
                            <ul *ngIf="!isInputOpen">
                                <li *ngFor="let item of filterOption" (click)="openInput($event,item)">
                                    <span>{{item.field}}</span>
                                    <span class="col3"></span>
                                </li>
                            </ul>
                            <ul *ngIf="isInputOpen" class="chips-input-list" (click)="doNothing($event)">
                                <li>
                                    <span>
                                        <div class="field mr10"><input [(ngModel)]="filterText" type="text" placeholder="Please enter a value"></div>
                                    </span>
                                </li>
                                <li>
                                    <span>
                                        <label class="checkbox mr10"><input type="checkbox" #exact><span>Match Whole Words</span></label>
                                    </span>
                                </li>
                                <li>
                                    <span>
                                        <button class="button" (click)="close();">Close</button>
                                        <button class="button primary pull-right" (click)="update(filterText,exact.checked)"  [disabled]="!filterText || filterText == ''">Update</button>
                                    </span>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
			  `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class ChipsFilterComponent implements OnInit {

    @Input() filterOption: any[] = [];
    @Output() applyFlter = new EventEmitter();
    private openMenu: boolean = false;
    private changeWait: any;
    private filterText: string = '';
    private selectedFilters: AdvancedSearchFilter[] = [];
    private currentFilter: AdvancedSearchFilter;
    private isInputOpen: boolean = false;
    constructor(
        ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    private toggleMenu() {
        this.openMenu = !this.openMenu;
        this.isInputOpen = false;
    }
    doNothing(event) {
        event.stopPropagation();
    }
    update(filterValue: string, exact: any) {
        this.currentFilter.value = filterValue;
        this.currentFilter.exact = exact;
        this.selectedFilters.push(this.currentFilter);
        this.close();
        this.applyFlter.emit(this.selectedFilters);
    }

    removeFilterOption(item) {
        let index = this.selectedFilters.indexOf(item);
        if (index > -1)
            this.selectedFilters.splice(index, 1);
        console.log(this.selectedFilters);
        this.applyFlter.emit(this.selectedFilters);
    }

    openInput(event,item) {
        event.stopPropagation();
        this.isInputOpen = true;
        this.currentFilter = item;
    }

    close() {
        this.openMenu = true;
        this.isInputOpen = false;
        this.filterText = '';
        this.currentFilter = undefined;
    }

    ngOnInit(): void {
    }
};
