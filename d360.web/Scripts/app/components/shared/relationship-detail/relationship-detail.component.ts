import { Input, Component, OnChanges, SimpleChange, OnDestroy, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RelationshipsService } from '../../../services/relationships.service';

@Component({
    selector: 'gov-relationship-detail',
    templateUrl: './relationship-detail.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['relationship-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RelationshipsService]
})


export class RelationshipDetailComponent implements OnChanges, OnDestroy {
    @Input() assetUid: string = "";
    @Input() assetTypeUid: string = "";

    isLoading: boolean = false;

    constructor(
        private cdRef: ChangeDetectorRef,
        private relationshipService: RelationshipsService
    ) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p === 'assetUid' && this.assetUid) {
                this.load();
            }
        }

    }

    ngOnDestroy() {

    }

    public load(): void {
        console.log("loading");
        this.isLoading = true;
        var params = {};
        this.relationshipService.getRelationshipsForAsset(this.assetUid, params)
            .subscribe((res) => {
                console.log(res);
                this.isLoading = false;
            })
    }
}
