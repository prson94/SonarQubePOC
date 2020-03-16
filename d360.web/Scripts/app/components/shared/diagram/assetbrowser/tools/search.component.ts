import * as _ from 'lodash';
import { AfterViewInit, Component, Input, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter, Output } from '@angular/core';

import { BrowserService } from '../../../../../services/browser.service';
import { PermissionsService } from '../../../../../services/permissions.service';
import { MessagesObservableService } from '../../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-assetbrowser-search',
    templateUrl: './search.component.html',
    providers: [BrowserService, PermissionsService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserSearchComponent implements AfterViewInit {
    @Input() numberOfResults: number;
    @Output() search: EventEmitter<string> = new EventEmitter();
    @Output() previous: EventEmitter<number> = new EventEmitter();
    @Output() next: EventEmitter<number> = new EventEmitter();

    private searchTimer;
    private searchValue: string = '';
    private searchCurrentItem: number;

    constructor(
        protected permissionsService: PermissionsService,
        protected messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef
    ) {
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }

    goToPrevious() {
        this.searchCurrentItem--;
        if (this.searchCurrentItem <= 0) {
            if (this.numberOfResults > 0) {
                this.searchCurrentItem = 1;
            }
            else {
                this.searchCurrentItem = 0;
            }
        }
        this.previous.emit(this.searchCurrentItem);
    }

    goToNext() {
        this.searchCurrentItem++;
        if (this.searchCurrentItem > this.numberOfResults)
            this.searchCurrentItem--;

        this.previous.emit(this.searchCurrentItem);
    }

    searchDiagram(event) {
        if (event == null) {
            this.searchValue = '';
        }
        else {
            this.searchValue = event.target.value;
            this.searchCurrentItem = 1;

            if (event.keyCode == 40) {
                this.goToNext();
                return;
            }
            if (event.keyCode == 38) {
                this.goToPrevious();
                return;
            }
        }
        clearTimeout(this.searchTimer);
        this.searchTimer = setTimeout(() => {
            this.search.emit(this.searchValue);
        }, 100);
    }
} 