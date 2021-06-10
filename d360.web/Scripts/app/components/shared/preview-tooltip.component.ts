import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    ElementRef,
    EventEmitter,
    HostBinding,
    Input,
    Output,
    ViewChild
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
    @Input() uid: string;
    @Input() align: string;
    @Input() contentAnchor: string = 'left';
    @HostBinding('style.color') @Input() iconColor: string;
    @HostBinding('style.background') @Input() foreColor: string;

    public active: boolean = false;
    public data: TooltipInfo = null;

    private subscriptions: Subscription = new Subscription();

    private pending: boolean = false;
    public hideDebounce: Subject<any> = new Subject();
    public mouseIn: boolean = false;
    private colorHtml: string = "";
    @Output() click = new EventEmitter();

    @ViewChild('previewText') previewText: ElementRef;

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
        if (this.subscriptions) {
            this.subscriptions.unsubscribe();
        }
    }

    private load(item, tip) {
        this.active = false;

        if (!this.data) {
            //get object properties for the tooltip
            if (this.uid) {
                this.toolTipService.getTooltipInfoByUid(this.uid, this.objectType)
                    .subscribe(res => {
                        if (!res.ShowTooltip || !this.pending) {
                            this.active = false;
                            return;
                        }

                        this.data = res;
                        if (tip.innerText != " " && tip.textContent != " ") {
                            this.showPanel(tip, item);
                            this.ref.markForCheck();
                        }
                        this.data.FieldValues.filter(x => x.Type == "Color").length > 0 ?
                            this.setColorHtml(this.data.FieldValues.filter(x => x.Type == "Color")[0].Value) : null;
                    });
            } else {
                this.toolTipService.getTooltipInfo(this.objectType, this.objectId)
                    .subscribe(res => {
                        if (!res.ShowTooltip || !this.pending) {
                            this.active = false;
                            return;
                        }

                        this.data = res;
                        this.data.FieldValues.filter(x => x.Type == "Color").length > 0 ?
                            this.setColorHtml(this.data.FieldValues.filter(x => x.Type == "Color")[0].Value) : null;

                        if (tip.innerText != " " && tip.textContent != " ") {
                            this.showPanel(tip, item);
                            this.ref.markForCheck();
                        }
                });
            }
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
                var leftOffset = Math.max(windowWidth - dims.width - 30, 0);
                if (this.isRightAligned()) {
                    leftOffset += 30;
                    element.style.width = dims.width + 'px';
                }
                element.style.left = leftOffset + 'px';
            }
        }
    }

    showPanel(panel, item) {
        let xoffset = 0;
        if (this.contentAnchor === 'right' && this.previewText && this.previewText.nativeElement) {
            xoffset = this.previewText.nativeElement.offsetWidth + 5;
        }

        if (panel && !this.active) {
            this.active = true;
            panel.style.zIndex = 1000;
            panel.style.top = item.getBoundingClientRect().bottom + 'px';

            if (this.align) {
                let minwidth = getComputedStyle(panel).minWidth;
                let panelWidth = parseInt(minwidth.substr(0, minwidth.length - 2)) || 400;

                if (this.isRightAligned()) {
                    panel.style.left = xoffset + (item.getBoundingClientRect().right - panelWidth) + 'px';
                } else if (this.isLeftAligned()) {
                    panel.style.right = (window.innerWidth - item.getBoundingClientRect().x) + 5 + 'px';
                }
            } else {
                panel.style.left = xoffset + item.getBoundingClientRect().left + 'px';

            }

            window.setTimeout(() => {
                this.repositionMenuToFit(window.innerHeight, this.isRightAligned() ? item.getBoundingClientRect().right : window.innerWidth, panel);
            }, 50);
        }
    }

    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch (err) {
            return "NULL";
        }
    }

    setColorHtml(colorJSON: string) {
        try {
            let colorObj = JSON.parse(colorJSON);
            this.colorHtml = "<div class=\"ig-colorfield-item-selected\"><span class=\"ig-colorfield-swatch tooltip-no-top\" style=\"background-color:" + colorObj.Value + "\"></span><span class=\"ig-colorfield-item-label tooltip-no-top\">" + colorObj.Name + "</span></div>";
            this.ref.markForCheck();
        } catch (err) {
            console.error("err");
        }
    }

    hide() {
        this.pending = false;
        this.active = false;
        this.ref.markForCheck();
    }

    isRightAligned(): boolean {
        return this.align === 'right';
    }

    isLeftAligned(): boolean {
        return this.align === 'left';
    }

    public navigate(e: any, url: string) {
        this.router.navigateByUrl(url);
        e.preventDefault();
    }
}
