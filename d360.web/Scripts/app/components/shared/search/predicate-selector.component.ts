import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, HostListener, Output, EventEmitter, ViewChild, ElementRef, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AssetService } from '../../../services/asset.service';
import { AssetSearchFilter, CommonComponentAssetResult } from '../../../models/asset-search.model';
import { PredicateType, Predicate } from '../../../models/predicate.model';
import { RelationshipsService } from '../../../services/relationships.service';
import { PredicatesService } from '../../../services/predicates.service';


@Component({
    selector: 'd3s-predicate-selector',
    templateUrl: 'predicate-selector.component.html',
    providers: [AssetService, RelationshipsService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class PredicateSelectorComponent implements OnInit {

    @Input() predicateType: PredicateType;;

    @Output() onChange: EventEmitter<Predicate> = new EventEmitter();
    private selected: Predicate;

    private predicates: Predicate[] = [];

    private isSelectionVisible: boolean = false;

    constructor(
        private predicatesService: PredicatesService,
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
            this.predicatesService.getPredicatesByType(this.predicateType)
                .subscribe(res => {
                    this.predicates = res;
                    this.ref.markForCheck();
                });
        }
        else {
            console.warn("PredicateSelectorType not set!");
        }

    }

}


