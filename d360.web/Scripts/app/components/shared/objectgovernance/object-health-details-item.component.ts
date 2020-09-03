import { Component, Input, EventEmitter, OnChanges, AfterViewInit, SimpleChange, Output, ViewChild, ElementRef,   } from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { TreeNode } from 'primeng/api';
import { ScoreType } from '../../../models/metrics.model';
import { expand } from 'rxjs/operators';
import { clearTimeout } from 'timers';
import { PointBreakdown } from '../../../models/score.model';


@Component({
    selector: 'd3s-object-health-details-item',
    templateUrl: `./object-health-details-item.component.html`,
    providers: [ScoreService],
})

export class ObjectHealthDetailsItemComponent extends BaseComponent implements OnChanges, AfterViewInit {
    @Input() item: PointBreakdown;
    @Input() isloading: boolean = false;
    @Input() showtype: ScoreType;
    @Output() checkExpander = new EventEmitter();
    private ScoreType = ScoreType;
    private currentItemDetails: any;
    private scoreItemUid: string;
    private scoreItem: any;
    private disableToggle: boolean = false;
    public isCollapsed: boolean = false;
    private handle: any;
    public expandable: boolean = false;
    @ViewChild('DQDescription', { static: false }) dqDescription: ElementRef;

    constructor(protected scoreService: ScoreService) {
        super();
    }
    
    ngAfterViewInit(): void {
        this.checkExpanders();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad: boolean = false;
        //for (let p in changes) {
        //    if (p == 'definition') {
        //        requiresLoad = (changes['definition'].currentValue != changes['definition'].previousValue) && changes['definition'] != undefined;
        //    }
        //    if (p == 'item') {
        //        requiresLoad = (changes['item'].currentValue != changes['item'].previousValue) && changes['item'] != undefined;
        //    }
        //}
        //if (requiresLoad) {
        //    this.isLoading = true;
        //}
    }

    private toggleDetails() {
        this.isCollapsed = !this.isCollapsed;
    }

    public setCollapsed(val: boolean) {
        if (!this.disableToggle)
            this.isCollapsed = val;
    }
    private getReadableValue(value: string) {
        switch (value.toLowerCase()) {
            case 'eq':
                return 'Equals';
            case 'neq':
                return 'Not Equals';
            case 'lt':
                return 'Less Than';
            case 'lte':
                return 'Less Than or Equals';
            case 'gt':
                return 'Greater Than';
            case 'gte':
                return 'Greater Than or Equals';
            default: return '';
        }
    }

    getAsPrecentage(val: number) {
        if (val == 0)
            return '0%';
        if (!val)
            return;
        if (val == 1)
            return '100%'
        let s = val + '0000';
        s = s.replace('0.', '');
        if (s.length > 6)
            s = (s.substr(0, 2)) + '.' + s[2] + "%";
        else
            s = (s.substr(0, 2)) + "%";
        if (s.startsWith('0'))
            s = s.substr(1, s.length);
        return s;   
    }

    private checkExpanders() {
        clearTimeout(this.handle);
        this.handle = window.setTimeout(() => {
            if (this.item && this.item) {
                if (this.showtype == ScoreType.Governance) {
                    this.expandable = !(!this.item.Description && !this.item.Measures);// && !this.currentItemDetails.Conditions);
                    if (this.item.Measures) {
                        this.item.Measures.forEach(x => {
                            let expandable: boolean = ((x.Description != undefined && x.Description !== "") || (x.Conditions && x.Conditions.length > 0)); //!(!x.Description && !x.Conditions && !(x.Conditions.length > 0));
                            x.expandable = expandable;
                        });
                    }
                    this.checkExpander.emit();
                }
                else {
                    if (this.dqDescription) {
                        let htmlEl = this.dqDescription.nativeElement;
                        if (htmlEl.offsetHeight > 34) {
                            this.expandable = true;
                            this.checkExpander.emit();
                        } else {
                            this.expandable = false;
                            this.checkExpander.emit();
                        }
                    } else {
                        this.expandable = false;
                        this.checkExpander.emit();
                    }
                }
            }
        }, 100);
    }
}