import { Input, Component, OnChanges, SimpleChange, OnDestroy, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';

@Component({
    selector: 'gov-relationship-detail',
    templateUrl: './relationship-detail.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['relationship-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class RelationshipDetailComponent implements OnChanges, OnDestroy {
    @Input() assetUid: string = "";

    isLoading: boolean = false;

    constructor(
        private cdRef: ChangeDetectorRef
    ) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p === 'assetUid') {
                this.load();
            }
        }

    }

    ngOnDestroy() {

    }

    public load(): void {
        console.log("loading");
    }
}
