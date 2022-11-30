import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SocialService } from '../../services/social.service';
import { Count } from '../../models/counts.model';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-board-tile',
    templateUrl: 'board-tile.component.html',
    providers: [SocialService],
})

export class BoardTile extends BaseComponent implements OnInit {
    counts: Count[] = [];
    selected: any;
    @Input() daysToLookBack: number = 7;
    @Output() daysToLookBackChange = new EventEmitter();

    @Output() showItemDetail = new EventEmitter();

    constructor(
        protected settingsService: CompanySettingsService,
        private socialService: SocialService) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;

        this.socialService.getMyCounts(this.daysToLookBack).subscribe(
            (res) => {
                this.counts = res.filter((item) => item.Total > 0);
                this.isLoading = false;
            });
    }

    doSelect(item: Count) {
        this.showItemDetail.emit({
            selected: item
        });
    }

    changeDates(event) {
        this.daysToLookBack = event.days;
        this.daysToLookBackChange.emit(this.daysToLookBack);
        this.load();
    }

    timeFrameMessage() {
        switch (this.daysToLookBack) {
            case 7:
                return ' (' + $localize`Past week` + ')';
            case 30:
                return ' (' + $localize`Past month` + ')';
            case 365:
                return ' (' + $localize`Past year` + ')';
        }
        return ' (' + $localize`All` + ')';
    }
}


