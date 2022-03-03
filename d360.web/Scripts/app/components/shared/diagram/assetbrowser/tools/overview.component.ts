import { Component, ChangeDetectionStrategy, Output, EventEmitter, Input, OnInit, HostBinding, ViewChild, ElementRef, Renderer2  } from '@angular/core';
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
    overviewElementId: string;
    overviewContainer: any | undefined;

    constructor(private renderer: Renderer2, private el: ElementRef) { }

    ngOnInit(): void {        
        this.enabled = this.getState();
        this.overviewElementId = 'overlay' + Math.floor(Math.random() * 1000);
        this.createOverview();

        if (this.diagram && this.overview) this.overview.observed = this.diagram;
    }

    private createOverviewContainer(): void {
        this.overviewContainer = this.renderer.createElement('div');
        this.renderer.addClass(this.overviewContainer, 'overview');
        this.renderer.setStyle(this.overviewContainer, 'visibility', this.enabled ? 'visible' : 'hidden');        
        this.renderer.setProperty(this.overviewContainer, 'id', this.overviewElementId);        
        this.renderer.appendChild(this.el.nativeElement, this.overviewContainer);
        
        return this.overviewContainer;
    }

    private createOverview(): void {
        if (this.overview === undefined || this.overview === null) {            
            
            this.createOverviewContainer();
            
            this.overview = new go.Overview(this.overviewElementId);
            const highlightBox = this.overview.box.elt(0) as (go.Shape);

            if (highlightBox) highlightBox.stroke = '#202020'; //color of the square in the overview panel
        }
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
        this.renderer.setStyle(this.overviewContainer, 'visibility', this.enabled ? 'visible' : 'hidden');
        
        this.saveState();        
    }
     
    public initialize(diagram: go.Diagram): void {
        this.clear();
        this.createOverview();
        if (diagram && this.overview) {
            this.overview.observed = diagram;
        }
    }

    public clear(): void {
        this.overviewElementId = 'overlay' + Math.floor(Math.random() * 1000);
        if (this.overview) {
            this.overview.observed = null;
        }
        this.overview = null;
        this.renderer.removeChild(this.el.nativeElement, this.overviewContainer, true);
    }
} 