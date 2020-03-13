import * as _ from 'lodash';
import { Component, ChangeDetectionStrategy, Output, EventEmitter, Input, OnChanges, SimpleChanges } from '@angular/core';
import { MessagesObservableService } from '../../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-assetbrowser-zoom',
    templateUrl: './zoom.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserZoomComponent implements OnChanges {
    @Input() scale: number;
    @Output() change: EventEmitter<number> = new EventEmitter();

    private zoomText: string = '100%';

    constructor(
        protected messagesService: MessagesObservableService
    ) {
        
    }

    ngOnChanges(changes: SimpleChanges): void {

    }

    private update(scale: number) {
        console.log(scale);
        this.change.emit(scale);
        this.zoomText = Math.round(scale * 100) + '%';
        //this.cdRef.markForCheck();
    }

    private in(e) {
        let scale: number = this.scale + .1;
        if (scale > 2.5) {
            scale = 2.5;
        }
        this.update(scale);
    }

    private out(e) {
        let scale: number = this.scale - .1;
        if (scale < .1) {
            scale = .1;
        }
        this.update(scale);
    }

} 