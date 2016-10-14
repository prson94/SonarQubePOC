
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
                    <p-menubar *ngIf="hasDate" [model]="dateMenuItems"></p-menubar><!--workaround to position bug in menu-->
                    <div *ngIf="!hasDate">
                        <ul>                                                      
                            <li class="left" *ngIf="hasAdd"><a class="Action" (click)="addClick.emit(null)" pTooltip="Add"><i class="fa fa-plus fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasExport"><a class="Action" (click)="exportClick.emit(null)" pTooltip="Export to Excel"><i class="fa fa-download fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasEdit"><a class="Action" (click)="editClick.emit(null)" pTooltip="Edit"><i class="fa fa-pencil fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasClose"><a class="Action" (click)="closeClick.emit(null)" pTooltip="Close"><i class="fa fa-remove fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasFilterMode"><a class="Action" (click)="filterClick()" pTooltip="Filter Mode"><i class="fa fa-filter fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasRefresh"><a class="Action" (click)="refreshClick.emit()" pTooltip="Refresh"><i class="fa fa-refresh fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasAuthenticate"><a class="Action" (click)="authenticateClick.emit()" pTooltip="Authenticate"><i class="fa fa-sign-in fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasApi"><a class="Action" (click)="apiClick.emit()" pTooltip="API Key"><i class="fa fa-key fa-fw"></i></a></li>
                            <li class="left" *ngIf="hasPassword"><a class="Action" (click)="passwordClick.emit()" pTooltip="Password"><i class="fa fa-asterisk fa-fw"></i></a></li>                        
                        </ul>
                    </div>
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
        
    private dateMenuItems: MenuItem[] = [];
    

    ngOnInit() {        

        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.buildMenu();
    }

    private buildMenu() {        
       
        if (this.hasDate) {
            this.dateMenuItems.push({
                icon: 'fa-clock-o',
                items: [
                    { label: 'Past Week', command: () => this.dateClick.emit({ days: 7 }) },
                    { label: 'Past Month', command: () => this.dateClick.emit({ days: 30 }) },
                    { label: 'Past Year', command: () => this.dateClick.emit({ days: 365 }) },
                    { label: 'All', command: () => this.dateClick.emit({ days: 0 }) }
                ]
            });
        }
        
    }

    private filterClick() {
        this.filterMode = !this.filterMode;
        this.filterModeChange.emit(this.filterMode);
    }

}