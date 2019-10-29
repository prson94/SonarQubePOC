import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { AdvancedSearchFilter } from '../../../../models/search-result.model';
import { Element } from '@angular/compiler/src/render3/r3_ast';

@Component({
    selector: 'd3s-chips-filter',
    template: `
		        <div class="chips-input">
                    <div class="chip-option" *ngFor="let item of selectedFilters">{{item.field}}: {{item.value}}<i class="fa fa-times-circle" (click)="removeFilterOption(item)"></i></div>
                    <div class="chip-option clickable" (click)="toggleMenu()" (blur)="close()">
                        <span *ngIf="!currentFilter">Add Filter...</span>
                        <span *ngIf="currentFilter" class="chip-option input">{{currentFilter.field}}: Any<i class="fa fa-times-circle" (click)="close()"></i></span>
                        <div class="popup-menu" [ngClass]="{'popup-open': openMenu || isInputOpen}" #popup>
                            <ul *ngIf="!isInputOpen" class="chips-options-list">
                                <li *ngFor="let item of filterOption" (click)="openInput($event,item)">
                                    <span>{{item.field}}</span>
                                    <span class="col3"></span>
                                </li>
                            </ul>
                            <ul *ngIf="isInputOpen" class="chips-input-list" (click)="doNothing($event)">
                                <li>
                                    <span>
                                        <div class="field mr10"><input [(ngModel)]="filterText" type="text" placeholder="Please enter a value" #searchInput></div>
                                    </span>
                                </li>
                                <li>
                                    <span>
                                        <label class="checkbox mr10"><input type="checkbox" #exact><span>Match Whole Words</span></label>
                                    </span>
                                </li>
                                <li>
                                    <span>
                                        <button class="button" (click)="close();">Cancel</button>
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

export class ChipsFilterComponent {
    @ViewChild('searchInput') searchInputElement: ElementRef;
    @Input() filterOption: any[] = [];
    @Output() applyFlter = new EventEmitter();
    private openMenu: boolean = false;
    private changeWait: any;
    private filterText: string = '';
    private selectedFilters: AdvancedSearchFilter[] = [];
    private currentFilter: AdvancedSearchFilter;
    private isInputOpen: boolean = false;
    @ViewChild('popup') popup: ElementRef;
    constructor(
        ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    private toggleMenu() {
        this.openMenu = !this.openMenu;
        this.isInputOpen = false;

        this.checkMenuPosistion();
    }
    doNothing(event) {
        event.stopPropagation();
    }
    update(filterValue: string, exact: any) {
        this.currentFilter.value = filterValue;
        this.currentFilter.exact = exact;

        var newFilter = { ...this.currentFilter };

        this.selectedFilters.push(newFilter);
        this.close();
        this.applyFlter.emit(this.selectedFilters);
    }

   

    removeFilterOption(item) {
        let index = this.selectedFilters.indexOf(item);
        if (index > -1)
            this.selectedFilters.splice(index, 1);
        this.applyFlter.emit(this.selectedFilters);
        this.checkMenuPosistion();
    }

    openInput(event,item) {
        event.stopPropagation();
        this.isInputOpen = true;
        this.currentFilter = item;
        this.setFocus();
        this.checkMenuPosistion();
    }

    private checkMenuPosistion() {
        this.popup.nativeElement.style.right = 'auto';
        window.setTimeout(() => {
            let dims = this.popup.nativeElement.getBoundingClientRect();
            let maxLeft = window.innerWidth;
            if (dims.right > maxLeft) {
                this.popup.nativeElement.style.right = '0px';
            }
        }, 100);
    }
    close() {
        this.openMenu = true;
        this.isInputOpen = false;
        this.filterText = '';
        this.currentFilter = undefined;
        this.checkMenuPosistion();
    }
    
    private setFocus() {        
        setTimeout(() => { // this will make the execution after the above boolean has changed
            if (this.searchInputElement)
                this.searchInputElement.nativeElement.focus();
        }, 150);
    }
};
