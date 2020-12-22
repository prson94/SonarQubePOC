import { Component, ChangeDetectionStrategy, Output, EventEmitter, Input, OnInit, HostBinding } from '@angular/core';
import * as go from 'gojs';

@Component({
    selector: 'd3s-assetbrowser-overview',
    templateUrl: './overview.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./overview.component.less']
})
export class AssetBrowserOverviewComponent implements OnInit {    
    @Input() enabled: boolean;
    @Input() diagram: go.Diagram;

    private storage = window.sessionStorage;
    private readonly enabledStateKey = 'asset-browser-overview-enabled';
                    
    @HostBinding('class') classes = 'controls-bottom-left';
            
    @Output() enabledChanged: EventEmitter<boolean> = new EventEmitter();

    overview: go.Overview = undefined;

    ngOnInit(): void {
        this.enabled = this.getState();
        if (this.overview === undefined) {
            this.overview = new go.Overview('assetBrowserOverview');
            const highlightBox = this.overview.box.elt(0) as (go.Shape);

            if (highlightBox) highlightBox.stroke = 'black'; //color of the square in the overview panel
        }

        if (this.diagram && this.overview) this.overview.observed = this.diagram;
    }

    private saveState(): void{
        this.storage.setItem(this.enabledStateKey, `${this.enabled}`);
    }

    private getState(): boolean {
        const val = this.storage.getItem(this.enabledStateKey);

        return val === 'true' ? true : false;
    }

    setOverviewState(state: boolean): void {
        this.enabled = state;
        this.enabledChanged.emit(this.enabled);   

        this.saveState();        
    }

    public initialize(diagram: go.Diagram): void {
        this.overview.observed = diagram;        
    }
} 