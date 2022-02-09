import { Component, EventEmitter, Output, Input, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy } from '@angular/core';
import { MenuItem } from 'primeng/api';

@Component({
    selector: 'd3s-tile-actions',
    templateUrl: './tile-actions.component.html',
    styles: [`
     :host{
            text-transform:none;
        }   
  `],
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

    @Input() dataCyPrefix: string = '';
    @Input() hasAdd: boolean = false;
    @Input() addTooltip: string = "Add";
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
    @Input() isExportDisabled: boolean = false;
    @Input() exportDisabledMessage: string = 'Export Disabled';
    @Input() hasMenu: boolean = false;
    @Input() menuItems: MenuItem[] = [];
    @Output() menuClick = new EventEmitter();

    @Input() hideTooltip: boolean = false;

    @Input() IsExportInProgress: boolean = false;

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
