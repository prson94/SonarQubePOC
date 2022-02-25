import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, SimpleChange, OnChanges, OnDestroy, AfterViewInit, Output, EventEmitter, ViewChild, ViewChildren, QueryList } from '@angular/core';
import { Router, NavigationEnd, NavigationStart, ActivatedRoute } from '@angular/router';
import { Event as NavigationEvent } from "@angular/router";
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { SecondaryNavItem, DynamicButton, AssetAction } from '../../../models/secondaryNav.model';
import { Subscription } from 'rxjs';
import * as _ from 'lodash';
import { ObjectStatistics } from '../../../models/object-statistics.model';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';
import { SurveysService } from '../../../services/surveys.service';
import { ArtifactService } from '../../../services/artifacts.service';
import { Survey } from '../../../models/survey.model';
import { WorkflowService } from '../../../services/workflow.service';
import { filter } from "rxjs/operators";
import { SearchDetail } from '../../../models/search-result.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-right-sidebar',
    templateUrl: 'right-sidebar.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [SurveysService, ObjectStatisticsService, ArtifactService, WorkflowService],
    host: { '(window:resize)': 'checkSize()' }
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
    statsSub: Subscription;
    updateSub: Subscription;
    paramsSub: Subscription;

    items: SecondaryNavItem[];
    buttons: DynamicButton[];
    homeUrl: string;
    area: any = { icon: 'fa-folder', title: '' };
    @Input() menuOpen: boolean;
    @Output() changed = new EventEmitter();
    currentObject: any;
    survey: Survey;
    @ViewChild('badge', { static: false }) badge: ElementRef;
    @ViewChild('noScore', { static: false }) noScore: ElementRef;
    @ViewChildren('tabScroller') tabScroller: QueryList<ElementRef>;
    private statistics: ObjectStatistics;
    private searchDetails: SearchDetail;
    private actionsAssigned: boolean = false;
    private currentResouceID: number;
    private isScoringScreen: boolean = false;
    private menuWarningType: string = '';
    private showOnlyMainTab: boolean = false;

    status: string;
    showStatus = false;
    showCertify = false;
    showHeader: boolean = false;
    showSurvey: boolean = false;
    showNav: boolean = true;
    showSurveyPopup: boolean = false;
    showScrollButtons: boolean = false;
    disableScrollLeft: boolean = false;
    disableScrollRight: boolean = false;
    showCertifyModal: boolean = false;
    assetAction: AssetAction;
    dataClassification: string;
    showDataClassification: boolean = false;
    assetActionWidth: number = 0;

    //keep record of previous url, sometimes we dont need to clear all items (ie. asset -> asset audit page)
    private previousUrl: string = '';

    constructor(
        private secondaryNavService: SecondaryNavService,
        protected objectStatisticsService: ObjectStatisticsService,
        private surveysService: SurveysService,
        private ref: ChangeDetectorRef,
        private artifactService: ArtifactService,
        private workflowService: WorkflowService,
        protected settingsService: CompanySettingsService,
        private router: Router,
        private route: ActivatedRoute
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
                        } else {
                            let extras = router.getCurrentNavigation().extras;
                            if (extras.state?.invalidateKey) {
                                this.secondaryNavService.invalidateKey();
                            }
                        }
                    }
                    if (event instanceof NavigationEnd) {
                        this.previousUrl = event.url;
                        if (event.url.indexOf('/admin/scoring/') > -1) {
                            this.isScoringScreen = true;
                        }
                        else {
                            this.isScoringScreen = false;
                        }
                    }
                });
    }

    ngOnInit(): void {
        this.paramsSub = this.route.queryParams.subscribe((params) => {
            let markForCheck = false;
            if (params['nonavigation'] != null) {
                this.showNav = params['nonavigation'].toLocaleLowerCase() !== 'true';
                markForCheck = true;
            }

            if (markForCheck) {
                this.ref.markForCheck();
            }
        });
    }

    ngAfterViewInit(): void {
        this.load();
    }

    filterScoringTabHasNoValue = (tab: SecondaryNavItem) => {
        if (tab.title === "Scoring") {
            return Boolean(this.searchDetails?.Scores.length);
        }
        return true;
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
            let maxWidth = this.getElementRightPosition(this.tabScroller.first.nativeElement.parentElement);
            let lastTab = this.getElementRightPosition(this.tabScroller.first.nativeElement.lastElementChild);
            this.showScrollButtons = lastTab > maxWidth;
        }
        this.checkScrollPos();
    }

    checkScrollPos() {
        if (this.tabScroller && this.tabScroller.length > 0) {
            let currentPosition = this.tabScroller.first.nativeElement.scrollLeft;
            this.disableScrollLeft = currentPosition == 0;

            let maxWidth = this.getElementRightPosition(this.tabScroller.first.nativeElement.parentElement);
            let lastTab = this.getElementRightPosition(this.tabScroller.first.nativeElement.lastElementChild);
            this.disableScrollRight = lastTab <= maxWidth;

            this.ref.markForCheck();
        }
    }

    getElementRightPosition(element) {
        if (element && element.getBoundingClientRect) {
            return element.getBoundingClientRect().right
        }
        return NaN;
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
                this.checkScrollPos();
                window.clearInterval(id);
            }
            this.checkScrollPos();
        };

        let id = window.setInterval(move, 5);
    }

    getTitle(item: SecondaryNavItem) {
        if (this.statistics && this.statistics.IssueCount > 0 && item.title === 'Actions') {
            let plurality = this.statistics.IssueCount == 1 ? ' is' : 's are';
            return this.statistics.IssueCount + " outstanding action" + plurality + " assigned to you";
        } else {
            return "";
        }
    }

    load() {
        this.showStatus = false;
        this.showDataClassification = false;
        this.statistics = null;
        this.showCertify = false;
        this.showHeader = false;
        this.showSurvey = false;
        this.searchDetails = null;
        this.items = [];
        this.buttons = [];
        this.showScrollButtons = false;
        this.currentResouceID = +CurrentResourceID;
        this.subscription = this.secondaryNavService.rightSidebar$.subscribe(
            (item) => {
                if (item.title === this.secondaryNavService.activeTabTitle) {
                    item.active = true;
                }
                this.items.push(item);
                this.items = _.sortBy(this.items, 'orderPriority'); this.emitChanges();
                this.secondaryNavService.setLocalCurrentTabs([...this.items]);

                if (item.tag === 'GovernanceRoles') {
                    let setting = this.settingsService.getSettingById(CompanySettingEnum.GovernanceRoleReferenceListUid);
                    if (!setting.ScalarValue || setting.ScalarValue === "00000000-0000-0000-0000-000000000000") {
                        item.warningMessage = `GovRoleWarning`;
                        this.ref.markForCheck();
                    }
                }
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
                this.showStatus = false;
                this.showDataClassification = false;
                this.showOnlyMainTab = false;
                this.showSurvey = false;
                this.searchDetails = null;
                this.emitChanges();
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
                this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasRequestCertificationWorkflow);
            } else {
                this.showStatus = false;
                this.showDataClassification = false;
                this.statistics = null;
                this.showCertify = false;
                this.showSurvey = false;
                this.searchDetails = null;
                this.emitChanges();
            }
        });


        this.assetActionSub = this.secondaryNavService.assetAction$.subscribe(res => {
            this.assetAction = res;
            if (this.assetAction && this.assetAction.type == "CONNECTORLABEL") {
                this.showOnlyMainTab = true;
            }
            if (this.assetAction && this.assetAction.type == "TAG") {
                var AssetActionwidthCalc = 0;
                if (this.assetAction.showBack) {
                    AssetActionwidthCalc = AssetActionwidthCalc + 110;
                }
                if (this.assetAction.showDelete) {
                    AssetActionwidthCalc = AssetActionwidthCalc + 110;
                }
                if (this.assetAction.showEdit) {
                    AssetActionwidthCalc = AssetActionwidthCalc + 110;
                }
                this.assetActionWidth = AssetActionwidthCalc;
            }
        });

        this.assetActionClearSub = this.secondaryNavService.assetActionClear$.subscribe(
            item => {
                //check if router is navigated to asset paga audit
                if (!this.previousUrl || this.router.url.toLowerCase().indexOf(this.previousUrl.toLowerCase()) <= 0) {
                    this.assetAction = null;
                    this.emitChanges();
                }
            })

        this.homeUrlChangeSub = this.secondaryNavService.homeUrlChange$.subscribe(
            item => {
                this.homeUrl = item;
            }
        )

        this.statsSub = this.secondaryNavService.refreshStats$.subscribe(res => {
            if (this.currentObject && !this.currentObject.isType) {
                this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasRequestCertificationWorkflow);
            }
        })

        this.updateSub = this.secondaryNavService.updateObject$.subscribe(res => {
            if (res) {
                if (res.key == 'firstTabTitle') {
                    this.items[0].title = res.value;
                }
                if (res.key === 'areaTitle') {
                    this.area.title = res.value;
                    this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasRequestCertificationWorkflow);
                }
                this.ref.markForCheck();
            }
        })


        this.emitChanges();
    }

    loadItemStats(objectID: number, objectName: string, objectType: string, objectTypeID: number, HasRequestCertificationWorkflow: boolean) {
        this.objectStatisticsService.getObjectColorAndValue(objectID, objectName, "status").subscribe(
            result => {
                this.status = result;
                if (this.status != undefined && this.status != null && this.status.length > 0) {
                    var draftValues = this.settingsService.getSettingById(CompanySettingEnum.RequestCertificationDraft).StringSetting.Value;

                    if (!draftValues) {
                        draftValues = "DRAFT";
                    }
                    let statusHeading = "";
                    try {
                        let colorObj = JSON.parse(this.status);
                        if (colorObj.length && colorObj.length > 0)
                            statusHeading = colorObj[0].name;
                    } catch (e) {
                        statusHeading = this.status;
                    }
                    let isDraft = false;
                    let draftArray = draftValues.toUpperCase().split(',');
                    draftArray.forEach(x => {
                        if (statusHeading.toUpperCase().indexOf(x.toUpperCase()) != -1)
                            isDraft = true;
                    });

                    this.showCertify = statusHeading && isDraft && HasRequestCertificationWorkflow;

                    this.showStatus = true;
                    this.ref.markForCheck();
                }
                else {
                    this.showCertify = false;
                    this.showStatus = false;
                    this.ref.markForCheck();
                }
            }
        );

        this.objectStatisticsService.getObjectColorAndValue(objectID, objectName, "dataClassification", false).subscribe(
            result => {
                this.dataClassification = result;
                try {
                    let dataClassificationAttributes = JSON.parse(this.dataClassification);
                    if (this.dataClassification != undefined && this.dataClassification != null && dataClassificationAttributes.length > 0) {
                        this.showDataClassification = true;
                    }
                    else {
                        this.showDataClassification = false;
                    }
                } catch (e) {
                    this.showDataClassification = false;
                }
                this.ref.markForCheck();
            }
        );

        if (this.currentObject.Uid && this.currentObject.Uid != '00000000-0000-0000-0000-000000000000') {
            this.objectStatisticsService.getSearchDetails(this.currentObject.Uid).subscribe(
                result => {
                    this.searchDetails = result;
                    this.ref.markForCheck();
                }
            );
            this.surveysService.getObjectSurvey(this.currentObject.Uid)
                .subscribe(result => {
                    this.survey = undefined;
                    if (result) {
                        this.survey = result;
                        this.showSurvey = true;
                        this.ref.markForCheck();
                    }
                });
        }



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

    }

    ngOnDestroy() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
        if (this.subscriptionClear) {
            this.subscriptionClear.unsubscribe();
        }
        if (this.areaSub) {
            this.areaSub.unsubscribe();
        }
        if (this.hideHeaderSub) {
            this.hideHeaderSub.unsubscribe();
        }
        if (this.objectSub) {
            this.objectSub.unsubscribe();
        }
        if (this.buttonSubscriptionClear) {
            this.buttonSubscriptionClear.unsubscribe();
        }
        if (this.buttonSubscription) {
            this.buttonSubscription.unsubscribe();
        }
        if (this.homeUrlChangeSub) {
            this.homeUrlChangeSub.unsubscribe();
        }
        if (this.statsSub) {
            this.statsSub.unsubscribe();
        }
        if (this.paramsSub) {
            this.paramsSub.unsubscribe();
        }
    }

    trackById(index, item) {
        return item.tag;
    }

    itemClicked(item: SecondaryNavItem) {
        // debugger;
        if (item.active == true)
            return;

        if (this.AllClosed()) {
            this.secondaryNavService.setLocalHomeUrl(this.router.url);
            this.homeUrl = this.router.url;
        }
        this.closeAll();
        if (item.title == "homeClick") {
            this.secondaryNavService.clearLocalActiveItem();
            let home = this.homeUrl ? this.homeUrl : this.secondaryNavService.getLocalHomeUrl();
            if (!home) {
               // debugger;
               home = `/artifact/${this.secondaryNavService.artifactTypeId}`;
               this.secondaryNavService.activeTabTitle = null;
               this.secondaryNavService.artifactTypeId = null;
            }
            this.router.navigateByUrl(home);
            return;
        }
        item.active = true;
        if (item.url) this.router.navigateByUrl(item.url);
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

    checkIfImg(value: string) {
        if (value && value.indexOf('/Content') != -1) {
            return true;
        }
        else
            return false;
    }

    IsIcon(icon: string) {
        return !_.startsWith(icon.toUpperCase(), "URL-");
    }

    GetURL(icon: string) {
        if (icon)
            return icon.replace(/^URL-+/i, '');
    }

    requestCertification() {
        this.showCertifyModal = true;
        this.showCertify = false;
    }

    closeCertifyModal() {
        this.showCertifyModal = false;
        this.showCertify = true;
    }

    certify() {
        this.showCertifyModal = false;
        if (this.currentObject && this.currentObject.Uid)
            this.artifactService
                .requestCertification(this.currentObject.Uid)
                .subscribe(result => {
                    window.setTimeout(
                        x => {
                            this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasRequestCertificationWorkflow);
                        }, 6000);
                });
    }

    navigateToSurvey() {
        if (this.currentObject) {
            this.showSurveyPopup = true;
        }
    }

    closeSurveyPopup() {
        this.showSurveyPopup = false;
        this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasRequestCertificationWorkflow);
    }

    handleComplete(event) {
        this.closeSurveyPopup();
        this.showSurvey = false;
    }

    OpenScoring(scoreType: string) {
        if (this.currentObject.Uid) {
            let scoreItems = this.items.filter(x => x.title === 'Scoring');
            if (scoreItems.length == 1) {
                this.router.navigateByUrl(`/sidebar/score/${this.currentObject.Uid}/${scoreType}`);
            }
        }
    }

    emitChanges() {
        this.ref.markForCheck();
        this.changed.emit();
        this.checkSize();
    }
};
