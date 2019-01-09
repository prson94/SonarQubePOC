import { Component, EventEmitter, Output, Input, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy } from '@angular/core';
import { MenuItem } from 'primeng/primeng';

@Component({
    selector: 'd3s-tile-actions',
    styles: [`
     :host{
            text-transform:none;
        }   
  `],
    template: `
                <div class="TileTools"> 
                    <p-menubar *ngIf="hasDate" [model]="dateMenuItems"></p-menubar><!--workaround to position bug in menu-->
                    <p-menubar *ngIf="hasMenu" [model]="menuItems"></p-menubar>
                    <div *ngIf="!hasDate && !hasMenu">
                        <ul>                                                      
                            <li class="left" *ngIf="hasAdd"><a class="Action" (click)="addClick.emit(null)" [pTooltip]="hideTooltip ? null : 'Add'"><i class="fa fa-plus fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasSuggest"><a class="Action" (click)="suggestClick.emit(null)" [pTooltip]="hideTooltip ? null : 'Suggest'"><i class="fa fa-commenting fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasExport"><a class="Action" (click)="exportClick.emit(null)" [pTooltip]="hideTooltip ? null : 'Export to Excel'"><i class="fa fa-download fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasCustomExport"><a class="Action" (click)="customExportClick.emit(null)" pTooltip="Choose export to Excel format"><i class="fa fa-download fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasExportErrors"><a class="Action" (click)="exportErrorsClick.emit(null)" [pTooltip]="hideTooltip ? null : 'Export Errors to Excel'"><i class="fa fa-download red-text fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasExportOriginal"><a class="Action" (click)="exportOriginalClick.emit(null)" [pTooltip]="hideTooltip ? null : 'Export Original Spreadsheet'"><i class="fa fa-download blue-text fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasEdit"><a class="Action" (click)="editClick.emit(null)" [pTooltip]="hideTooltip ? null : 'Edit'"><i class="fa fa-pencil fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasSave"><a class="Action" (click)="saveClick.emit(null)" [pTooltip]="hideTooltip ? null : 'Save'"><i class="fa fa-floppy-o fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasFilterMode"><a class="Action" (click)="filterClick()" [pTooltip]="hideTooltip ? null : 'Filter Mode'">
                                <i class="fa fa-filter fa-fw" [ngClass]="{'red-text darken-2':!filterMode}"></i>                                
                            </a></li>
                            <li class="left" *ngIf="hasRefresh"><a class="Action" (click)="refreshClick.emit()" [pTooltip]="hideTooltip ? null : 'Refresh'"><i class="fa fa-refresh fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasAuthenticate"><a class="Action" (click)="authenticateClick.emit()" [pTooltip]="hideTooltip ? null : 'Authentication'"><i class="fa fa-sign-in fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasApi"><a class="Action" (click)="apiClick.emit()" [pTooltip]="hideTooltip ? null : 'API Key'"><i class="fa fa-key fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasPassword"><a class="Action" (click)="passwordClick.emit()" [pTooltip]="hideTooltip ? null : 'Password'"><i class="fa fa-asterisk fa-fw"></i></a></li>                        
                            <li class="left" *ngIf="hasFullScreen"><a class="Action" (click)="fullScreenClick.emit()" [pTooltip]="hideTooltip ? null : 'Fullscreen'"><i class="fa fa-arrows-alt fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasNewWindow"><a class="Action" (click)="newWindowClick.emit()" [pTooltip]="hideTooltip ? null : 'New Window'"><i class="fa fa-expand fa-fw"></i></a></li>                            
                            <li class="left" *ngIf="hasUser"><a class="Action" (click)="userClick()" [pTooltip]="hideTooltip ? null : 'Show my items only'">
                                <i class="fa fa-user fa-fw" [ngClass]="{'red-text darken-2':userMode}"></i>                                
                            </a></li>
                            <li class="left" *ngIf="hasView"><a class="Action" (click)="viewClick.emit()" [pTooltip]="hideTooltip ? null : 'Preview'"><i class="fa fa-eye fa-fw"></i></a></li>                        
                            <li class="left" *ngIf="hasClose"><a class="Action" (click)="closeClick.emit(null)" [pTooltip]="hideTooltip ? null : 'Close'"><i class="fa fa-remove fa-fw"></i></a></li>
                        </ul>
                    </div>
                </div>          
                `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class TileActionsComponent implements OnInit, OnChanges {
    @Output() addClick = new EventEmitter();
    @Output() exportClick = new EventEmitter();
    @Output() customExportClick = new EventEmitter();
    @Output() exportErrorsClick = new EventEmitter();
    @Output() exportOriginalClick = new EventEmitter();
    @Output() editClick = new EventEmitter();
    @Output() dateClick = new EventEmitter();
    @Output() closeClick = new EventEmitter();
    @Output() refreshClick = new EventEmitter();
    @Output() authenticateClick = new EventEmitter();
    @Output() apiClick = new EventEmitter();
    @Output() passwordClick = new EventEmitter();
    @Output() suggestClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    @Output() viewClick = new EventEmitter();
    @Output() newWindowClick = new EventEmitter();

    @Input() userMode: boolean = false;
    @Output() userModeChange = new EventEmitter();

    @Input() filterMode: boolean = false;
    @Output() filterModeChange = new EventEmitter();

    @Input() hasAdd: boolean = false;
    @Input() hasExport: boolean = false;
    @Input() hasCustomExport: boolean = false;
    @Input() hasExportErrors: boolean = false;
    @Input() hasExportOriginal: boolean = false;
    @Input() hasEdit: boolean = false;
    @Input() hasDate: boolean = false;
    @Input() hasClose: boolean = false;
    @Input() hasFilterMode: boolean = false;
    @Input() hasRefresh: boolean = false;
    @Input() hasAuthenticate: boolean = false;
    @Input() hasApi: boolean = false;
    @Input() hasPassword: boolean = false;
    @Input() hasFullScreen: boolean = false;
    @Input() hasSuggest: boolean = false;
    @Input() hasSave: boolean = false;
    @Input() hasUser: boolean = false;
    @Input() hasView: boolean = false;
    @Input() hasNewWindow: boolean = false;

    @Input() hasMenu: boolean = false;
    @Input() menuItems: MenuItem[] = [];
    @Output() menuClick = new EventEmitter();

    @Input() hideTooltip: boolean = false;

    @Output() fullScreenClick = new EventEmitter();

    private dateMenuItems: MenuItem[] = [];


    ngOnInit() {


    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.buildMenu();
    }

    private buildMenu() {

        if (this.hasDate) {
            this.dateMenuItems.push({
                icon: 'fa fa-clock-o',
                items: [
                    { label: 'Past Week', command: () => this.dateClick.emit({ days: 7 }) },
                    { label: 'Past Month', command: () => this.dateClick.emit({ days: 30 }) },
                    { label: 'Past Year', command: () => this.dateClick.emit({ days: 365 }) },
                    { label: 'All', command: () => this.dateClick.emit({ days: 0 }) }
                ]
            });
        }

        if (this.hasMenu && this.menuItems.length > 0) {
            this.setMenuItemCommands(this.menuItems);
        }
    }

    private setMenuItemCommands(items) {
        items.forEach(i => {
            i.command = (e) => this.menuClick.emit(e.item);
            if (i.items && i.items.length > 0) {
                this.setMenuItemCommands(i.items);
            }
        });
    }

    private filterClick() {
        this.filterMode = !this.filterMode;
        this.filterModeChange.emit(this.filterMode);
    }

    private userClick() {
        this.userMode = !this.userMode;
        this.userModeChange.emit(this.userMode);
    }
}
