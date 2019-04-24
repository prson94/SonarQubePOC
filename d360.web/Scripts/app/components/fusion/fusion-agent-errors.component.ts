import {Component, Input, OnInit} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionAgentError} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-agent-errors',
    templateUrl: './fusion-agent-errors.component.html',
    providers: [FusionService],
})

export class FusionAgentErrorsComponent extends BaseComponent implements OnInit {
    private errors: FusionAgentError[] = [];
    private selected: FusionAgentError;

    destroySubject$: Subject<void> = new Subject();

    @Input() maxRows: number = 1000;
    @Input() days: number = 0; // 0 = all up to max

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;

        this.fusionService
            .getFusionAgentErrorHistory(this.maxRows, this.days)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                res => {
                    this.errors = res;
                    this.selected = this.errors.length > 0 ? this.errors[0] : null;
                    this.isLoading = false;
                }
            )
        ;
    }
}
