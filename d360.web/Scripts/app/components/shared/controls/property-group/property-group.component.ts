import {
    AfterViewInit,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    ElementRef,
    EventEmitter,
    HostBinding,
    HostListener,
    Input,
    NgModule,
    OnInit,
    Output,
    ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { UntypedFormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { FormFeedbackBadgesModule } from '../form-feedback-badges/form-feedback-badges.component';
import { isFormContainerValid } from '../form-feedback-badges/form-feedback-utils';
import { uuidv4 } from '../../../../static/lang';
import * as _ from 'lodash';
import { PropertyGroupsService } from './property-groups.service';

export const PropertyGroupInstanceIdAttributeName = 'data-property-group-instance-id';

@Component({
    selector: 'ig-property-group',
    templateUrl: './property-group.component.html',
    styleUrls: ['./property-group.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PropertyGroupComponent implements OnInit, AfterViewInit {
    @HostBinding(`attr.${PropertyGroupInstanceIdAttributeName}`) instanceId: string;

    @Input() igformGroup: UntypedFormGroup;
    @Input() title: string = $localize`Property Group`;
    @Input() showMoreInfo: boolean = false;
    @Input() moreInfoHtml: string = "";
    @Input() shouldBePadded: boolean = true;
    @Input() showHeaderLine: boolean = true;
    @Input() hideIfNoTitle: boolean = false;

    @Output() isValid = new EventEmitter();
    @Input() expanded: boolean = true;
    @Output() expandedChange = new EventEmitter();

    delayedRefresh = _.debounce(() => {
        this.isValid.emit(isFormContainerValid({ formGroup: this.igformGroup, formContainer: this.inputContainer }));
    }, 200);

    @ViewChild("pgcontainer", { static: false }) inputContainer: ElementRef;

    constructor(
        private propertyGroups: PropertyGroupsService,
        private ref: ChangeDetectorRef,
        private thisRef: ElementRef) {
    }

    ngAfterViewInit(): void {
        if (this.igformGroup) {
            this.igformGroup.valueChanges.subscribe((x) => {
                this.delayedRefresh();
            });
        }
    }

    ngOnInit(): void {
        this.instanceId = uuidv4();
        this.propertyGroups.register(this);

        if (this.igformGroup) {
            this.igformGroup.valueChanges.subscribe((x) => {
                this.delayedRefresh();
            });
        }
    }

    public forceExpand() {
        if (this.expanded) {
            return;
        }

        this.expanded = true;
        this.ref.markForCheck();
        this.expandedChange.next(this.expanded);
    }

    public refreshBadgeCounts() {
        this.delayedRefresh();
    }

    onInputKeyUp(event) {
        event.preventDefault();
        event.stopPropagation();
        switch (event.which) {
            case 32:
                event.target.click();
                return false;
        }
    }

    ngOnDestroy() {
        this.propertyGroups.unregister(this);
    }

    // This is a hack to prevent showing incorrect tooltips on all property group
    // Browser automatically shows tooltip [title] because of @Input() title
    // This hack is required, because renaming @Input() dom attribute via bindingPropertyName doesn't works in angular 13.3.7
    @HostListener('mouseenter') mouseenter() {
        (this.thisRef.nativeElement as Element).removeAttribute('title');
    }
}

@NgModule({
    declarations: [
        PropertyGroupComponent
    ],
    exports: [
        PropertyGroupComponent
    ]
    , imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        TooltipModule,
        FormFeedbackBadgesModule
    ]
})
export class PropertyGroupModule { }