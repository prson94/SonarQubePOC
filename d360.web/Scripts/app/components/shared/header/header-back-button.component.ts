import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    OnDestroy,
    OnInit,
} from '@angular/core';
import {
    Router,
    NavigationEnd
} from '@angular/router';


@Component({
    selector: 'd3s-header-back-button',
    template:
        `
            <button igButton
                    class="header-back-button"
                    tooltip="Click to go back"
                    tooltipPosition="bottom"
                    [disabled]="disabled"
                    type="button" 
                    icon="fa-chevron-left" 
                    (click)="back()" ></button>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderBackButtonComponent implements OnInit, OnDestroy {
    disabled: boolean = false;
    private sub: any = null;


    constructor(private router: Router,
        private ref: ChangeDetectorRef
    ) {
    }

    ngOnInit() {
        this.checkHistory();

        this.sub = this.router.events.subscribe((val) => {
            if (val instanceof NavigationEnd) {
                this.checkHistory();
            }
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    back() {
        window.history.back();
    }

    checkHistory() {
        this.disabled = window.history.length <= 2;
        this.ref.markForCheck();
    }
}

