import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnInit, SimpleChange, OnChanges, OnDestroy, AfterViewInit, Output, EventEmitter, ViewChild, ViewChildren, QueryList } from '@angular/core';
import { Router, NavigationEnd, NavigationStart } from '@angular/router';
import { Event as NavigationEvent } from "@angular/router";
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { SecondaryNavItem, DynamicButton, AssetAction } from '../../../models/secondaryNav.model';
import { Subscription } from 'rxjs';
import * as _ from 'lodash';
import { ObjectStatistics } from '../../../models/object-statistics.model';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';
import { SurveysService } from '../../../services/surveys.service';
import { ArtifactService } from '../../../services/artifacts.service';
import { SurveyType } from '../../../models/survey.model';
import { WorkflowService } from '../../../services/workflow.service';
import { filter } from "rxjs/operators";
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';


declare var CompanySettings
declare var CurrentResourceID;

@Component({
    selector: 'd3s-right-sidebar',
    templateUrl: 'right-sidebar.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [SurveysService, ObjectStatisticsService, ArtifactService, WorkflowService],
    host: { '(window:resize)': 'checkSize()', '(window:beforeunload)': 'destroy()' }
})

export class RightSidebarComponent implements OnChanges, OnDestroy, AfterViewInit {

    subscription: Subscription;
    buttonSubscription: Subscription;
    buttonSubscriptionClear: Subscription;
    subscriptionClear: Subscription;
    areaSub: Subscription;
    objectSub: Subscription;
    hideHeaderSub: Subscription;
    assetActionSub: Subscription;
    assetActionClearSub: Subscription;
    homeUrlChangeSub: Subscription;

    items: SecondaryNavItem[];
    buttons: DynamicButton[];
    homeUrl: string;
    area: any = { icon: 'fa-folder', title: '' };
    @Input() menuOpen: boolean;
    @Output() changed = new EventEmitter();
    private currentObject: any;
    private surveyType: SurveyType;
    @ViewChild('badge', { static: false }) badge: ElementRef;
    @ViewChild('noScore', { static: false }) noScore: ElementRef;
    @ViewChildren('tabScroller') tabScroller: QueryList<ElementRef>;
    private statistics: ObjectStatistics;
    private actionsAssigned: boolean = false;
    private currentResouceID: number;

    status: string;
    showStatus = false;
    showCertify = false;
    showHeader: boolean = false;
    showSurvey: boolean = false;
    showSurveyPopup: boolean = false;
    showScrollButtons: boolean = false;
    showCertifyModal: boolean = false;
    assetAction: AssetAction;

    //keep record of previous url, sometimes we dont need to clear all items (ie. asset -> asset audit page)
    private previousUrl: string = '';

    constructor(
        private secondaryNavService: SecondaryNavService,
        protected objectStatisticsService: ObjectStatisticsService,
        private surveysService: SurveysService,
        private ref: ChangeDetectorRef,
        private artifactService: ArtifactService,
        private workflowService: WorkflowService,
        private router: Router
    ) {
        router.events
            .pipe(
            filter(
                (event: NavigationEvent) => {
                    return (event instanceof NavigationStart || event instanceof NavigationEnd);
                    }
                )
        ).subscribe(
            (event: NavigationEvent) => {
                this.secondaryNavService.saveLastState();
                if (event instanceof NavigationStart) {
                    if (event.navigationTrigger != 'imperative') {
                        let state = this.secondaryNavService.getItemState(event.url);
                        if (state) {
                            this.secondaryNavService.rebuildFromStorage(state);
                        }
                        
                    }
                    window.setTimeout(() => {
                        this.items.forEach((item => {
                            if (item.url === event.url) {
                                item.active = true;
                                this.secondaryNavService.setLocalActiveItem(item);
                            } else {
                                item.active = false;
                            }
                            this.ref.markForCheck();
                        }));
                    },200);
                }
                if (event instanceof NavigationEnd) {
                    this.previousUrl = event.url;
                }
            });
    }

    ngAfterViewInit(): void {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['menuOpen'])
            return;
        if (this.currentObject) {
            this.load();
        }
    }

    checkSize() {
        if (this.tabScroller && this.tabScroller.length > 0) {
            let maxWidth = this.tabScroller.first.nativeElement.parentElement.getBoundingClientRect().right;
            let lastTab = this.tabScroller.first.nativeElement.lastChild.getBoundingClientRect().right;
            this.showScrollButtons = lastTab > maxWidth;
        }
    }

    scroll(direction: string) {
        let scrollAmount = 0;
        let scrollDistance = 300;
        let move = () => {
            if (direction == 'L') {
                this.tabScroller.first.nativeElement.scrollLeft -= 10;
            } else {
                this.tabScroller.first.nativeElement.scrollLeft += 10;
            }
            scrollAmount += 10;
            if (scrollAmount >= scrollDistance) {
                window.clearInterval(id);
            }
        };

        let id = window.setInterval(move, 5);

    }

    load() {
        this.showStatus = false;
        this.statistics = null;
        this.showCertify = false;
        this.showHeader = false;
        this.showSurvey = false;
        this.items = [];
        this.buttons = [];
        this.showScrollButtons = false;
        this.currentResouceID = +CurrentResourceID;
        this.subscription = this.secondaryNavService.rightSidebar$.subscribe(
            item => {
                this.items.push(item);
                this.items = _.sortBy(this.items, 'orderPriority'); this.emitChanges();
                this.secondaryNavService.setLocalCurrentTabs([ ...this.items ]);
            });

        this.buttonSubscription = this.secondaryNavService.rightSidebarButton$.subscribe(
            button => {
                this.buttons.push(button);
                this.buttons = _.sortBy(this.buttons, 'text'); this.emitChanges();
            });
        this.buttonSubscriptionClear = this.secondaryNavService.rightSidebarButtonClear$.subscribe(
            item => {
                this.buttons.splice(0, this.buttons.length); this.emitChanges();
            })


        this.subscriptionClear = this.secondaryNavService.rightSidebarClear$.subscribe(
            item => {
                this.items.splice(0, this.items.length);
                
                this.currentObject = null;
                this.statistics = null;
                this.showStatus = false; this.emitChanges();
            })
        this.areaSub = this.secondaryNavService.currentArea$.subscribe(
            area => {
                this.area = area; this.emitChanges();
            }
        );
        this.hideHeaderSub = this.secondaryNavService.hideHeader$.subscribe(result => {
            this.showHeader = result;
            this.emitChanges();
        });

        this.objectSub = this.secondaryNavService.currentObject$.subscribe(res => {
            this.currentObject = res;
            if (this.currentObject && !this.currentObject.isType) {
                this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasWorkFlow);
            } else {
                this.showStatus = false;
                this.statistics = null;
                this.showCertify = false; 
                this.showSurvey = false;
                this.emitChanges();
            }
        });


        this.assetActionSub = this.secondaryNavService.assetAction$.subscribe(res => {
            this.assetAction = res;
        });
        
        this.assetActionClearSub = this.secondaryNavService.assetActionClear$.subscribe(
            item => {
                //check if router is navigated to asset paga audit
                if (!this.previousUrl || this.router.url.toLowerCase().indexOf(this.previousUrl.toLowerCase()) <= 0) {
                    this.assetAction = null;
                    this.emitChanges();
                }
            })
        this.emitChanges(); 
    }

    private loadItemStats(objectID: number, objectName: string, objectType: string, objectTypeID: number, hasWorkFlow: boolean) {
        this.objectStatisticsService.getObjectStatus(objectID, objectName).subscribe(
            result => {
                this.status = result;
                if (this.status != undefined && this.status != null && this.status.length > 0) {
                    var draftValues = CompanySettings.RequestCertificationDraft;

                    if (!draftValues) {
                        draftValues = "DRAFT";
                    }
                    this.showCertify = this.status && (draftValues.toUpperCase().split(',').indexOf(this.status.toUpperCase()) > -1) && hasWorkFlow;

                    this.showStatus = true;
                    this.ref.markForCheck();
                }
            }
        );

        this.objectStatisticsService.getObjectStatistics(objectID, objectName).subscribe(
            result => {
                this.statistics = result;
                this.ref.markForCheck();
            }
        );
        this.workflowService.getIssues(objectID, objectName)
            .subscribe(result => {
                let issues = result;
                if (issues.length && issues.length > 0) {
                    this.actionsAssigned = true;
                }
                this.ref.markForCheck();
            });

        this.surveysService.getObjectSurvey(objectTypeID, objectType, objectID, objectName)
            .subscribe(result => {
                this.surveyType = undefined;
                if (result) {
                    this.surveyType = result;
                    this.showSurvey = true;
                    this.ref.markForCheck();
                }
            });

    }
    
    ngOnDestroy() {
        this.subscription.unsubscribe();
        this.subscriptionClear.unsubscribe();
        this.areaSub.unsubscribe();
        this.hideHeaderSub.unsubscribe();
        this.objectSub.unsubscribe();
        this.buttonSubscriptionClear.unsubscribe();
        this.buttonSubscription.unsubscribe();
    }

    trackById(index, item) {
        return item.tag;
    }

    itemClicked(item: SecondaryNavItem) {
        if (this.AllClosed()) {
            this.secondaryNavService.setLocalHomeUrl(this.router.url);
            this.homeUrl = this.router.url;
        }
        this.closeAll();
        if (item.title == "homeClick") {
            this.secondaryNavService.clearLocalActiveItem();
            let home = this.homeUrl ? this.homeUrl : this.secondaryNavService.getLocalHomeUrl();
            this.router.navigateByUrl(home);
            return;
        }
        item.active = true;
        if (item.hasDynamicUrl) this.router.navigateByUrl(item.dynamicUrlCallback());
        else if (item.url) this.router.navigateByUrl(item.url);
        this.secondaryNavService.itemClicked(item);
        this.AllClosed();
    }

    AllClosed() {
        let count = this.items.filter(x => x.active == true).length;
        if (count === 0)
            this.secondaryNavService.setLocalActiveItem(undefined);
        return count == 0;
    }

    closeAll() {
        for (let ritem of this.items) {
            if (ritem.active) {
                ritem.active = false;
                this.secondaryNavService.itemClicked(ritem);
            }
        }
    }

    IsIcon(icon: string) {
        return !_.startsWith(icon.toUpperCase(), "URL-");
    }

    GetURL(icon: string) {
        if (icon)
            return icon.replace(/^URL-+/i, '');
    }

    scoreBetween(start, end) {
        if (this.statistics) {
            return this.statistics.Score >= start && this.statistics.Score <= end;
        }
    }

    getCertificationStatusColor(status: string) {
        return this.objectStatisticsService.getCertificationStatusColor(status);
    }

    private requestCertification() {
        this.showCertifyModal = true;
        this.showCertify = false;
    }
    closeCertifyModal() {
        this.showCertifyModal = false;
        this.showCertify = true;
    }
    certify() {
        this.showCertifyModal = false;
        if (this.currentObject && this.currentObject.objectID)
            this.artifactService
                .requestCertification(this.currentObject.objectID)
                .subscribe(result => {
                    window.setTimeout(
                        x => {
                            
                            this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasWorkFlow);

                        }, 5000);
                });
    }

    navigateToSurvey() {
        if (this.currentObject) {
            this.showSurveyPopup = true;
        }
    }
    closeSurveyPopup() {
        this.showSurveyPopup = false;
        this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasWorkFlow);
    }
    handleComplete(event) {
        this.closeSurveyPopup();
        this.showSurvey = false;
    }
    private lastCalculatedMessage() {
        if (!this.statistics) {
            return "Governance Score not yet calculated";
        }
        var diff = new Date(Date.now() - Date.parse(this.statistics.ScoreLast));

        var years = diff.getUTCFullYear() - 1970;

        if (years > 0) return "Governance Score last calculated " + years + " years ago.";

        var months = diff.getUTCMonth();

        if (months > 0) return "Governance Score last calculated " + months + " months ago.";

        var days = diff.getUTCDate() - 1;

        if (days > 0) return "Governance Score last calculated " + days + " days ago.";

        var hours = diff.getUTCHours();

        if (hours > 0) return "Governance Score last calculated " + hours + " hours ago.";

        var minutes = diff.getUTCMinutes();

        if (minutes > 0) return "Governance Score last calculated " + minutes + " minutes ago.";

        return "Governance Score last calculated a few seconds ago.";
    }

    OpenScoring() {
        if (this.currentObject.Uid) {
            let scoreItems = this.items.filter(x => x.title === 'Scoring');
            if (scoreItems.length == 1) {
                this.itemClicked(scoreItems[0]);
            }
        }
    }

    emitChanges() {
        this.ref.markForCheck();
        this.changed.emit();
        this.checkSize();
    }
};
