import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BaseComponent } from '../../../components/shared/base.component';
import { Count } from '../../../models/counts.model';
import { ArtifactService } from '../../../services/artifacts.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { CoreModule } from '../../../components/shared/core.module';
import { TilesModule } from '../../../components/shared/tiles/tiles.module';
import { TableModule } from 'primeng/table';

@Component({
	selector: 'activity-tile',
	standalone: true,
	imports: [CoreModule, TableModule, TilesModule],
    //providers: [ArtifactService],
    templateUrl: './activity-tile.html'
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
                (res) => {
                    this.counts = res;
                    this.isLoading = false;
                    this.isLoaded = true;
                }
            );
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
                text = ' (' + $localize`Past week` + ') ';
                break;
            case 30:
                text = ' (' + $localize`Past month` + ')';
                break;
            case 365:
                text = ' (' + $localize`Past year` + ')';
                break;
            default:
                text = ' (' + $localize`All Activity` + ')';
                break;
        }

        return text;
    }
}
