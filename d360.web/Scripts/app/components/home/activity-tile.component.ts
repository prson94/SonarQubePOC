import { Component, OnInit, Output, Input, EventEmitter} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ArtifactService } from '../../services/artifacts.service';
import { Count} from '../../models/counts.model';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-activity-tile',
    providers: [ArtifactService],
    templateUrl: './activity-tile.component.html'
})

export class ActivityTile extends BaseComponent implements OnInit {
    counts: Count[] = [];
    selected: Count;    
    isLoaded: boolean = false;

    @Input() daysToLookBack: number = 7;
    @Output() daysToLookBackChange = new EventEmitter();

    @Output() showItemDetail = new EventEmitter();

    constructor(
        private artifactService: ArtifactService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit() {
        if (!this.isLoaded) {
            this.load();
        }
    }

    private load() {
        this.isLoading = true;

        this
            .artifactService
            .getActivityCount(this.daysToLookBack)
            .subscribe(
                res => {
                    this.counts = res;
                    this.isLoading = false;
                    this.isLoaded = true;
                }
            )
        ;
    }

    doSelect(item) {
        this.showItemDetail.emit({
            Id: item.Id,
            name: item.Name
        });
    }

    changeDates(event) {
        this.daysToLookBack = event.days;
        this.daysToLookBackChange.emit(this.daysToLookBack);
        this.load();
    }

    timeFrameMessage() {
        let text;

        switch (this.daysToLookBack) {
            case 7:
                text = ' (Past week)';
                break;
            case 30:
                text = ' (Past month)';
                break;
            case 365:
                text =  ' (Past year)';
                break;
            default:
                text = ' (All Activity)';
                break;
        }

        return text;
    }
}
