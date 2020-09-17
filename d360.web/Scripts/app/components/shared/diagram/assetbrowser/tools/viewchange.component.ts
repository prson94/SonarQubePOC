import * as _ from 'lodash';
import { AfterViewInit, Component, Input, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges, OnChanges } from '@angular/core';
import { DiagramType } from '../../../../../models/lineage.model';

@Component({
    selector: 'd3s-assetbrowser-viewchange',
    templateUrl: './viewchange.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserViewChangeComponent implements AfterViewInit, OnChanges {
    @Input() current: DiagramType;
    @Input() items: any[] = [];
    @Output() apply: EventEmitter<DiagramType> = new EventEmitter();

    constructor(
          private cdRef: ChangeDetectorRef
    ) {
        
    }

    public ngOnChanges(changes: SimpleChanges) {
        if (changes != null && changes['items'] != null && (changes['items'].firstChange || changes['items'].currentValue != changes['items'].previousValue)) {
            this.cdRef.markForCheck();
        }
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }


    diagramTypeChange(e: any) {
        this.current = e.value;
        this.apply.emit(e.value);
    }

    get disabled(): boolean {
        return this.items == null || this.items.length < 2;
    }

} 