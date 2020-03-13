import * as _ from 'lodash';
import { AfterViewInit, Component, Input, ChangeDetectionStrategy, ChangeDetectorRef, , Output, EventEmitter } from '@angular/core';
import { DiagramType } from '../../../../../models/lineage.model';

import { MessagesObservableService } from '../../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-assetbrowser-viewchange',
    templateUrl: './viewchange.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserViewChangeComponent implements AfterViewInit {
    @Input() current: DiagramType;
    @Output() apply: EventEmitter<DiagramType> = new EventEmitter();

    constructor(
        protected messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef
    ) {
        
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }

    private switchToImpactView(event) {
        this.apply.emit(DiagramType.Impact);
    }

    private switchToLineageView(event) {
        this.apply.emit(DiagramType.Lineage);
    }

    private impactViewButtonSelectedClass() {
        return (this.current == DiagramType.Impact) ? "right-margin-4 selected" : "right-margin-4";
    }

    private lineageViewButtonSelectedClass() {
        return (this.current == DiagramType.Lineage) ? "selected" : "";
    }
} 