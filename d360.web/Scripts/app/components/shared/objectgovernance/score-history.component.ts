import { Component, Input, OnChanges, SimpleChange, ViewChildren, QueryList, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked, ViewEncapsulation, DebugElement } from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown, ScorePoint } from '../../../models/score.model';
import * as Highcharts from 'highcharts';
import { ScoreType } from '../../../models/metrics.model';
import { ObjectHealthDetailsItemComponent } from './object-health-details-item.component';
import { SearchDetail } from '../../../models/search-result.model';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';
import { Observable, Subject } from 'rxjs';
import { SelectItem } from 'primeng/api';

@Component({
    selector: 'score-history',
    templateUrl: `score-history.component.html`,
    providers: [ScoreService, ObjectStatisticsService],
})
export class ScoreHistoryComponent extends BaseComponent {

    constructor(protected scoreService: ScoreService,
        protected objectStatisticsService: ObjectStatisticsService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
    }
}
