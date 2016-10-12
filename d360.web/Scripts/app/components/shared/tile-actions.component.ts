
import {Component, EventEmitter, Output, Input, OnInit, OnChanges, SimpleChange} from '@angular/core';
import {MenuItem} from 'primeng/primeng';

@Component({
    selector: 'd3s-tile-actions',
    styles: [`
     :host{
            text-transform:none;
        }
  `],
    template: `
                <div class="TileTools">                                    
                    <p-menubar [model]="items"></p-menubar>
                </div>          
                `
})

export class TileActionsComponent implements OnInit, OnChanges {
    @Output() addClick = new EventEmitter();
    @Output() exportClick = new EventEmitter();
    @Output() editClick = new EventEmitter();
    @Output() dateClick = new EventEmitter();
    @Output() closeClick = new EventEmitter();
    @Output() refreshClick = new EventEmitter();
    @Output() authenticateClick = new EventEmitter();
    @Output() apiClick = new EventEmitter();
    @Output() passwordClick = new EventEmitter();

    @Input() filterMode: boolean = false;        
    @Output() filterModeChange = new EventEmitter();
    
    @Input() hasAdd: boolean = false;
    @Input() hasExport: boolean = false;
    @Input() hasEdit: boolean = false;
    @Input() hasDate: boolean = false;
    @Input() hasClose: boolean = false;
    @Input() hasFilterMode: boolean = false;
    @Input() hasRefresh: boolean = false;
    @Input() hasAuthenticate: boolean = false;
    @Input() hasApi: boolean = false;
    @Input() hasPassword: boolean = false;

    private items: MenuItem[] = [];

    

    ngOnInit() {        

        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.buildMenu();
    }

    private buildMenu() {        
        this.items = [];
        if (this.hasAdd) {
            this.items.push({
                icon: 'fa-plus', command: () => this.addClick.emit(null)
            });
        }

        if (this.hasExport) {
            this.items.push({
                icon: 'fa-download', command: () => this.exportClick.emit(null)
            });
        }

        if (this.hasEdit) {
            this.items.push({
                icon: 'fa-pencil', command: () => this.editClick.emit(null)
            });
        }

        if (this.hasClose) {
            this.items.push({
                icon: 'fa-remove', command: () => this.closeClick.emit(null)
            });
        }

        if (this.hasDate) {
            this.items.push({
                icon: 'fa-clock-o',
                items: [
                    { label: 'Past Week', command: () => this.dateClick.emit({ days: 7 }) },
                    { label: 'Past Month', command: () => this.dateClick.emit({ days: 30 }) },
                    { label: 'Past Year', command: () => this.dateClick.emit({ days: 365 }) },
                    { label: 'All', command: () => this.dateClick.emit({ days: 0 }) }
                ]
            });
        }

        if (this.hasFilterMode) {
            this.items.push({
                icon: 'fa-filter', command: () => { this.filterMode = !this.filterMode; this.filterModeChange.emit(this.filterMode); }
            });
        }

        if (this.hasRefresh) {
            this.items.push({
                icon: 'fa-refresh', command: () => this.refreshClick.emit(null)
            });
        }

        if (this.hasAuthenticate) {
            this.items.push({
                icon: 'fa-sign-in', command: () => this.authenticateClick.emit(null)
            });
        }

        if (this.hasApi) {
            this.items.push({
                icon: 'fa-key', command: () => this.apiClick.emit(null)
            });
        }

        if (this.hasPassword) {
            this.items.push({
                icon: 'fa-asterisk', command: () => this.passwordClick.emit(null)
            });
        }

    }

}