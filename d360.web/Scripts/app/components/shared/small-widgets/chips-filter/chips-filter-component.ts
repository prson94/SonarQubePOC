import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, Output, EventEmitter, ViewChild, ElementRef, ViewChildren, QueryList } from '@angular/core';
import { Router } from '@angular/router';
import { AdvancedSearchFilter } from '../../../../models/search-result.model';

@Component({
    selector: 'd3s-chips-filter',
    template: `
		        <div class="chips-input" (clickOutside)="closeMenu()">
                    <div 
                            class="chip-option"
                            *ngFor="let item of selectedFilters"
                            (click)="openEdit(editor)">
                        {{item.field}}: {{item.value}}  
                        <i class="fa fa-times-circle" (click)="removeFilterOption(item)"></i>
                        <div class="popup-menu" #editor>
                            <ul class="chips-input-list" (click)="doNothing($event)" (keydown.enter)="edit(item,filterText,exact.checked)">
                                <li>
                                    <span>
                                        <div class="field mr10"><input [(ngModel)]="filterText" type="text" placeholder="Please enter a value" #searchInput/></div>
                                    </span>
                                </li>
                                <li>
                                    <span>
                                        <label class="checkbox mr10"><input type="checkbox" #exact/><span>Match Whole Words</span></label>
                                    </span>
                                </li>
                                <li>
                                    <span>
                                        <button class="button" (click)="closeMenu();">Cancel</button>
                                        <button class="button primary pull-right" (click)="edit(item,filterText,exact.checked)"  [disabled]="!filterText || filterText == ''">Update</button>
                                    </span>
                                </li>
                            </ul>
                        </div>
                    </div>
                    <div class="chip-option clickable" tabindex=0
                                (click)="toggleMenu()" 
                                (keydown.esc)="closeMenu()">
                        <span *ngIf="!currentFilter">Add Filter...</span>
                        <span *ngIf="currentFilter" class="chip-option input">{{currentFilter.field}}: Any<i class="fa fa-times-circle" (click)="closeMenu()"></i></span>
                        <div class="popup-menu" [ngClass]="{'popup-open': openMenu || isInputOpen}"  #popup>
                            <ul *ngIf="!isInputOpen" class="chips-options-list">
                                <li *ngFor="let item of filterOption" (click)="openInput($event,item)">
                                    <span>{{item.field}}</span>
                                    <span class="col3"></span>
                                </li>
                            </ul>
                            <ul *ngIf="isInputOpen" class="chips-input-list" (click)="doNothing($event)" (keydown.enter)="update(filterText,exact.checked)">
                                <li>
                                    <span>
                                        <div class="field mr10"><input [(ngModel)]="filterText" type="text" placeholder="Please enter a value" #searchInput/></div>
                                    </span>
                                </li>
                                <li>
                                    <span>
                                        <label class="checkbox mr10"><input type="checkbox" #exact/><span>Match Whole Words</span></label>
                                    </span>
                                </li>
                                <li>
                                    <span>
                                        <button class="button" (click)="closeMenu();">Cancel</button>
                                        <button class="button primary pull-right" (click)="update(filterText,exact.checked)"  [disabled]="!filterText || filterText == ''">Update</button>
                                    </span>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
			  `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(window:resize)': 'checkMenuPosistion()' }
})

export class ChipsFilterComponent {
    private openMenu: boolean = false;
    private changeWait: any;
    private filterText: string = '';
    private selectedFilters: AdvancedSearchFilter[] = [];
    private currentFilter: AdvancedSearchFilter;
    private isInputOpen: boolean = false;
    private isEditOpen: boolean = false;

    @Input() filterOption: any[] = [];
    @Output() applyFlter = new EventEmitter();
    @ViewChild('searchInput', { static: false }) searchInputElement: ElementRef;
    @ViewChild('popup', { static: false }) popup: ElementRef;
    @ViewChildren('editor') allEditors: QueryList<ElementRef>;

    constructor(
        private ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    private toggleMenu() {
        if (this.openMenu || this.isEditOpen)
            this.closeMenu();
        else
            this.openMenu = true;
        this.checkMenuPosistion();
        this.ref.markForCheck();
    }
    private openEdit(editor: HTMLElement) {
        return;
        this.closeMenu();
        editor.classList.add('popup-open');
        setTimeout(() => { this.isEditOpen = true; }, 150);
    }
    doNothing(event) {
        event.stopPropagation();
    }

    update(filterValue: string, exact: any) {
        if (!filterValue)
            return;
        this.currentFilter.value = filterValue;
        this.currentFilter.exact = exact;

        var newFilter = { ...this.currentFilter };

        this.selectedFilters.push(newFilter);
        this.closeMenu();
        this.applyFlter.emit(this.selectedFilters);
    }
    edit(item: AdvancedSearchFilter, filterValue: string, exact: any) {
        item.value = filterValue;
        item.exact = exact;
        this.closeMenu();
        this.applyFlter.emit(this.selectedFilters);
    }
   

    removeFilterOption(item) {
        let index = this.selectedFilters.indexOf(item);
        if (index > -1)
            this.selectedFilters.splice(index, 1);
        this.applyFlter.emit(this.selectedFilters);
        this.checkMenuPosistion();
    }

    openInput(event, item) {
        event.stopPropagation();
        this.isInputOpen = true;
        this.currentFilter = item;
        this.setFocus();
        this.checkMenuPosistion();
    }

    private checkMenuPosistion() {
        window.setTimeout(() => {
            if (this.popup || this.allEditors.length > 0) {
                let editor = this.allEditors.filter(x => x.nativeElement.classList.contains('popup-open'))[0];
                let menu = this.popup.nativeElement.classList.contains('popup-open') ? this.popup : editor;
                if (menu) {
                    menu.nativeElement.style.right = 'auto';
                    let dims = this.popup.nativeElement.getBoundingClientRect();
                    let maxLeft = window.innerWidth;
                    if (dims.right > maxLeft) {
                        menu.nativeElement.style.right = '0px';
                    }
                }
            }
        }, 100);
    }

    closeMenu() {
        this.filterText = '';
        this.currentFilter = undefined;
        this.allEditors.forEach(x => {x.nativeElement.classList.remove('popup-open');});
        setTimeout(() => {
            this.isInputOpen = false;
            this.openMenu = false;
            this.isEditOpen = false;
            this.ref.markForCheck();
        }, 100);
    }

    private setFocus() {
        setTimeout(() => {
            if (this.searchInputElement)
                this.searchInputElement.nativeElement.focus();
        }, 150);
    }
};
