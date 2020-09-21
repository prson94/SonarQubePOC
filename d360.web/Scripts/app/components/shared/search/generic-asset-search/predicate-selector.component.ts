import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, HostListener, Output, EventEmitter, ElementRef, OnInit } from '@angular/core';
import { AssetService } from '../../../../services/asset.service';
import { CommonComponentAssetTypeFilterRelationshipSide } from '../../../../models/asset-search.model';
import { PredicateType, Predicate } from '../../../../models/predicate.model';
import { RelationshipsService } from '../../../../services/relationships.service';
import { PredicatesService } from '../../../../services/predicates.service';


@Component({
    selector: 'd3s-predicate-selector',
    templateUrl: 'predicate-selector.component.html',
    providers: [AssetService, RelationshipsService, PredicatesService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class PredicateSelectorComponent implements OnInit {

    @Input() predicateType: PredicateType;;
    @Input() relationshipSide: CommonComponentAssetTypeFilterRelationshipSide;
    @Output() onChange: EventEmitter<Predicate> = new EventEmitter();
    @Input() selected: Predicate;
    @Input() assetTypeUid: string;


    noPredicates: boolean = false;
    predicates: Predicate[] = [];

    isSelectionVisible: boolean = false;

    constructor(
        private predicatesService: PredicatesService,
        private relationshipService: RelationshipsService,
        private ref: ChangeDetectorRef,
        private eRef: ElementRef
    ) {

    }

    @HostListener('document:click', ['$event'])
    onclick(ev: MouseEvent) {
        // if clicked outside of the component
        if (!this.eRef.nativeElement.contains(ev.target)) {
            this.isSelectionVisible = false;
        }
    }

    openSelection() {
        this.isSelectionVisible = !this.isSelectionVisible;
    }

    selectPredicate(item: Predicate) {
        this.selected = item;
        this.onChange.emit(item);
    }

    ngOnInit() {
        if (this.predicateType) {
            if (!this.assetTypeUid) {
                this.predicatesService.getPredicatesByType(this.predicateType)
                    .subscribe(res => {
                        this.predicates = res;
                        if (this.predicates.length == 0) {
                            this.noPredicates = true;
                        }
                        else {
                            this.noPredicates = false;
                        }
                        if (this.predicates.length == 1) {
                            this.selected = this.predicates[0];
                            this.onChange.emit(this.selected);
                        }
                        this.ref.markForCheck();

                    });
            }
            else {
                this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid)
                    .subscribe(result => {
                        this.predicates = [];
                        result.forEach(rel => {

                            if (rel.Predicate.Type == this.predicateType.toString()) {
                                if (this.relationshipSide == CommonComponentAssetTypeFilterRelationshipSide.Subject && this.assetTypeUid == rel.Subject.Uid)
                                    this.predicates.push(rel.Predicate);
                                if (this.relationshipSide == CommonComponentAssetTypeFilterRelationshipSide.Object && this.assetTypeUid == rel.Object.Uid)
                                    this.predicates.push(rel.Predicate);
                            }
                        });

                        if (this.predicates.length == 0) {
                            this.noPredicates = true;
                        }
                        else {
                            this.noPredicates = false;
                        }
                        if (this.predicates.length == 1) {
                            this.selected = this.predicates[0];
                            this.onChange.emit(this.selected);
                        }
                        this.ref.markForCheck();
                    });
            }
        }
        else {
            console.warn("PredicateSelectorType not set!");
        }

    }

    renderPredicateTitle(p: Predicate) {
        if (this.relationshipSide == null) {
            console.warn("Relationship side not set!");
            return '#';
        }

        if (this.relationshipSide == CommonComponentAssetTypeFilterRelationshipSide.Object)
            return p.Name;
        else return p.Inverse;
    }

}


