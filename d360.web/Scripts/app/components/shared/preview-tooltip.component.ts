import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    HostBinding,
    Input,
    Output
} from '@angular/core';
import {Router} from '@angular/router';
import {ToolTipService} from '../../services/tooltip.service';
import {TooltipInfo} from '../../models/tooltip-info.model';
import {TooltipSingletonService} from '../../services/tooltip-singleton.service';
import {Subject, Subscription} from "rxjs";
import {debounceTime} from "rxjs/operators";

@Component({
    selector: 'd3s-preview-tooltip',
    templateUrl: './preview-tooltip.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ToolTipService]
})

export class PreviewTooltipComponent {
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() icon: string;
    @Input() class: string;
    @Input() innerHtmlContent: string;
    @HostBinding('style.color') @Input() iconColor: string;
    @HostBinding('style.background') @Input() foreColor: string;

    public active: boolean = false;
    public data: TooltipInfo = null;

    private subscriptions: Subscription = new Subscription();

    private pending: boolean = false;
    public hideDebounce: Subject<any> = new Subject();
    public mouseIn: boolean = false;

    @Output() click = new EventEmitter();

    constructor(
        private toolTipService: ToolTipService,
        private router: Router,
        protected tooltipSingletonService: TooltipSingletonService,
        private ref: ChangeDetectorRef
    ) {
        this.tooltipSingletonService.tooltipMessage$.subscribe(
            info => {
                if (info.objectId == this.objectId && info.objectType == this.objectType) return;
                this.hide();
            });

        this
            .hideDebounce
            .pipe((debounceTime(100)))
            .subscribe(() => {
                if (!this.mouseIn) {
                    this.hide();
                }
            });
    }

    ngOnDestroy() {
        this.subscriptions.unsubscribe();
    }

    private load(item, tip) {
        this.active = false;

        if (!this.data) {
            //get object properties for the tooltip
            this.toolTipService.getTooltipInfo(this.objectType, this.objectId).then(res => {
                if (!res.ShowTooltip || !this.pending) {
                    this.active = false;
                    return;
                }

                this.data = res;
                if (tip.innerText != " " && tip.textContent != " ") {
                    this.showPanel(tip, item);
                    this.ref.markForCheck();
                }
            });
        } else {
            if (tip.innerText != " " && tip.textContent != " ") {
                this.showPanel(tip, item);
                this.ref.markForCheck();
            }
        }
    }

    private formattedUrl(url: string): string {
        if (url != null && !url.startsWith("/"))
            return "/" + url;
        else
            return url;
    }

    show(item, tip) {
        this.mouseIn = true;

        if (this.pending || this.active) {
            return;
        }

        this.pending = true;
        this.tooltipSingletonService.tooltipShow(this.objectType, this.objectId);
        this.load(item, tip);
    }

    repositionMenuToFit(windowHeight, windowWidth, element) {
        var dims = element.getBoundingClientRect();

        if (dims) {
            var maxHeight = dims.top + dims.height;
            var maxWidth = dims.left + dims.width;

            if (maxHeight > windowHeight) { //case where bottom is below page
                var topOffset = windowHeight - dims.height - 10;
                element.style.top = topOffset + 'px';
            }

            if (maxWidth > windowWidth) {
                var leftOffset = windowWidth - dims.width - 30;
                element.style.left = leftOffset + 'px';
            }
        }
    }

    showPanel(panel, item) {
        if (panel && !this.active) {
            this.active = true;
            panel.style.zIndex = 1000;
            panel.style.top = item.getBoundingClientRect().bottom + 'px';
            panel.style.left = item.getBoundingClientRect().left + 'px';

            window.setTimeout(() => {
                this.repositionMenuToFit(window.innerHeight, window.innerWidth, panel);
            }, 50);
        }
    }

    hide() {
        this.pending = false;
        this.active = false;
        this.ref.markForCheck();
    }
}
