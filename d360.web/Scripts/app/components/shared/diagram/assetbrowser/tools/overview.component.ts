import { Component, ChangeDetectionStrategy, Output, EventEmitter, Input, OnChanges, SimpleChanges, OnInit, HostBinding } from '@angular/core';
import * as go from 'gojs';
import { MessagesObservableService } from '../../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-assetbrowser-overview',
    templateUrl: './overview.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./overview.component.less']
})
export class AssetBrowserOverviewComponent implements OnInit {    
    @Input() enabled: boolean;
    overview: go.Overview;
                
    @HostBinding('class') classes = 'controls-bottom-left';
            
    @Output() enabledChanged: EventEmitter <boolean> = new EventEmitter();

    constructor(        
        protected messagesService: MessagesObservableService
    ) {

    }
    ngOnInit(): void {        
        this.overview = new go.Overview('assetBrowserOverview');        
    }
     
    closeOverview(): void {
        this.enabled = false;
        this.enabledChanged.emit(this.enabled);              
    }

    openOverview(): void {
        this.enabled = true;
        this.enabledChanged.emit(this.enabled);                
    }


    public initialize(diagram: go.Diagram): void {
        this.overview.observed = diagram;
    }

} 