import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnInit, SimpleChange, OnChanges, OnDestroy, AfterViewInit, Output, EventEmitter, ViewChild, ViewChildren, QueryList } from '@angular/core';
import { Router } from '@angular/router';
import { RightSidebarService  } from '../../../services/right-sidebar.service';
import { RightSidebarItem, DynamicButton, AssetAction } from '../../../models/rightsidebar.model';
import { Subscription }   from 'rxjs';
import * as _ from 'lodash';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { ObjectStatistics } from '../../../models/object-statistics.model';
import { load } from '@angular/core/src/render3';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';
import { SurveysService } from '../../../services/surveys.service';
import { ArtifactService } from '../../../services/artifacts.service';
import { SurveyType } from '../../../models/survey.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Artifact } from '../../../models/artifacts.model';

declare var CompanySettings;


@Component({
    selector: 'd3s-right-sidebar',      
    template: ` <div *ngIf="showHeader" class="title-bar" [ngClass]="{'menu-open': menuOpen}">
                    <div class="title">
                         <img class="icon" *ngIf="!IsIcon(area.icon)" [src]="GetURL(area.icon)"  height="20" width="20" />
                         <i *ngIf="IsIcon(area.icon)" [class]="'icon fa ' + area.icon"></i>
                        <h1 title="{{area.title ? area.title: 'D3S'}}">{{area.title ? area.title: 'D3S'}}</h1>
                        <span #badge *ngIf="statistics && statistics.Score;else noScore" class="d3s-icon large-icon"
                                title="{{lastCalculatedMessage()}}"
                                (click)="OpenScoring()"
                                [ngClass]="{
                                    'bad':scoreBetween(0,49),
                                    'ok':scoreBetween(50,89),
                                    'good':scoreBetween(90,1000)
                                }">
                            <d3s-dynamic-percentage [innerCircleColor]="getColor(badge)" [percentage]="statistics?.Score"></d3s-dynamic-percentage> 
                             <span class="text">{{statistics?.Score}}%</span>
                        </span> 
                        <ng-template #noScore>
                            <span #noScoreBadge *ngIf="currentObject && !currentObject?.isType" title="Governance Score not yet calculated" class="d3s-icon large-icon">
                                <d3s-dynamic-percentage [innerCircleColor]="getColor(noScoreBadge)" [percentage]="0"></d3s-dynamic-percentage> 
                                <span class="text">N/A</span>
                            </span>
                        </ng-template>
                        <span *ngIf="showStatus" class="d3s-icon large-icon" [style.background-color]="getCertificationStatusColor(status)">
                            <i class="fa fa-certificate"></i>
                            <span class="text">{{status}}</span>
                        </span>
                        <span class="grow"></span>
                        <button class="button" *ngIf="showCertify" (click)="requestCertification()"><i class="fa fa-certificate"></i><span>Request Certification</span></button>
                        <button class="primary button" (click)="navigateToSurvey()" *ngIf="showSurvey"><i class="fa fa-edit"></i><span>Take Survey</span></button>
                        <button *ngFor="let button of buttons" (click)="button.dynamicCallback()" [ngClass]="{'loading': button.isLoading}"  class="primary button" [disabled]="button.disabled">
                            <span class="content">{{button.text}}</span>
                            <span *ngIf="button.isLoading" class="loader"><span class="spinner light"></span></span>    
                        </button>
                    </div>
                    <div *ngIf="items && items.length > 0" class="tab-view">
                        <div class="tab-bar-outer">
                            <button *ngIf="showScrollButtons" class="left tab-scroller" (click)="scroll('L')"><i class="fa fa-chevron-circle-left"></i></button>
                            <div #tabScroller class="tab-bar can-overflow" [ngStyle]="{'margin-left.px': showScrollButtons ? 40 : 0,'margin-right.px': showScrollButtons ? 40 : 0}">
                                <button class="tab" [ngClass]="{'selected':AllClosed()}" (click)="itemClicked({active:false,title:'homeClick', url: 'blank'})">{{area.tabTitle}}</button>
                                <button class="tab" 
                                        [ngClass]="{'selected':item.active}" 
                                        *ngFor="let item of items; trackBy: trackById" 
                                        (click)="item.active=!item.active;itemClicked(item);">
                                            {{item.title}}
                                        <span *ngIf="statistics?.CommentCount && item.title === 'Comments'" class="d3s-icon small-icon primary">{{statistics?.CommentCount}}</span>
                                        <span *ngIf="statistics?.IssueCount && item.title === 'Actions'" class="d3s-icon small-icon bad">{{statistics?.IssueCount}}</span>
                                </button>
                            </div>
                            <button *ngIf="showScrollButtons" class="right tab-scroller" (click)="scroll('R')"><i class="fa fa-chevron-circle-right"></i></button>
                        </div>
                    </div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [SurveysService, ObjectStatisticsService, ArtifactService],
    host: { '(window:resize)': 'checkSize()' }
})

export class RightSidebarComponent implements OnChanges, OnDestroy, AfterViewInit{
        
    subscription: Subscription;
    buttonSubscription: Subscription;
    buttonSubscriptionClear: Subscription;
    subscriptionClear: Subscription;
    areaSub: Subscription;
    objectSub: Subscription;
    hideHeaderSub: Subscription;
    assetActionSub: Subscription;
    assetActionClearSub: Subscription;

    items: RightSidebarItem[];  
    buttons: DynamicButton[];
    hostUrl: string;
    area: any = {icon:'fa-folder',title: ''};
    @Input() menuOpen: boolean;
    @Output() changed = new EventEmitter();
    private currentObject: any;
    private surveyType: SurveyType;
    @ViewChild('badge') badge: ElementRef;
    @ViewChild('noScore') noScore: ElementRef;
    @ViewChildren('tabScroller') tabScroller: QueryList<ElementRef>;
    private statistics: ObjectStatistics;
    private lastCalculatedDate: number;

    status: string;
    showStatus = false;
    showCertify = false;
    showHeader: boolean = false;
    showSurvey: boolean = false;
    showScrollButtons: boolean = false;
    assetAction: AssetAction;
    constructor(
        private rightSidebarService: RightSidebarService,
        protected objectStatisticsService: ObjectStatisticsService,
        private surveysService: SurveysService,
        private ref: ChangeDetectorRef,
        private artifactService: ArtifactService,
        private router: Router
    ) {
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

        let id = window.setInterval(move,5);

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

        this.subscription = this.rightSidebarService.rightSidebar$.subscribe(
            item => {
                this.items.push(item);
                this.items = _.sortBy(this.items, 'title').reverse(); this.emitChanges();
            });

        this.buttonSubscription = this.rightSidebarService.rightSidebarButton$.subscribe(
            button => {
                this.buttons.push(button);
                this.buttons = _.sortBy(this.buttons, 'orderPriority'); this.emitChanges();
            });
        this.buttonSubscriptionClear = this.rightSidebarService.rightSidebarButtonClear$.subscribe(
            item => {
                this.buttons.splice(0, this.buttons.length); this.emitChanges();
            })


        this.subscriptionClear = this.rightSidebarService.rightSidebarClear$.subscribe(
            item => {
                this.items.splice(0, this.items.length);
                this.currentObject = null;
                this.statistics = null;
                this.showStatus = false; this.emitChanges(); 
            })
        this.areaSub = this.rightSidebarService.currentArea$.subscribe(
            area => {
                this.area = area; this.emitChanges(); 
            }
        );
        this.hideHeaderSub = this.rightSidebarService.hideHeader$.subscribe(result => {
            this.showHeader = result;
            this.emitChanges(); 
        });

        this.objectSub = this.rightSidebarService.currentObject$.subscribe(res => {
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


        this.assetActionSub = this.rightSidebarService.assetAction$.subscribe(res => {
            this.assetAction = res;
        });

        this.assetActionClearSub = this.rightSidebarService.assetActionClear$.subscribe(
            item => {
                this.assetAction = null;
                this.emitChanges();
            })
        this.emitChanges();

    }

    getColor(badge: any) {
        return window.getComputedStyle(badge, 'background')['background'];
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
                    if (objectName === 'Artifact')
                        this.showCertify = this.status && (draftValues.toUpperCase().split(',').indexOf(this.status.toUpperCase()) > -1) && hasWorkFlow;
                    else
                        this.showCertify = this.status && (draftValues.toUpperCase().split(',').indexOf(this.status.toUpperCase()) > -1);
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
        // prevent memory leak when component destroyed
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
    
    itemClicked(item: RightSidebarItem) {   

        if (item.active) {
            //look for any other already active items and fire click for them
            let isFirstItemOpen = true;
            for (let ritem of this.items) {                
                if (ritem.active && ritem.title != item.title) {
                    this.rightSidebarService.itemClicked(ritem);
                    ritem.active = false;
                    isFirstItemOpen = false;                     
                }
            }            
            if (isFirstItemOpen) this.hostUrl = this.router.url;            
            this.rightSidebarService.itemClicked(item);
            if (item.hasDynamicUrl) this.router.navigateByUrl(item.dynamicUrlCallback());
            else if (item.url) this.router.navigateByUrl(item.url);
        }        
        else {
            //return to previous url if the item is a url otherwise fire click event            
            if (item.url)
                this.router.navigateByUrl(this.hostUrl);
            else
                this.rightSidebarService.itemClicked(item);
        }
        this.AllClosed();
    }     

    AllClosed() {
        let count = this.items.filter(x => x.active == true).length;
        
        return count == 0;
    }

    IsIcon(icon: string) {
        return !_.startsWith(icon.toUpperCase(), "URL-");
    }

    GetURL(icon: string) {
        if(icon)
            return icon.replace(/^URL-+/i, '');
    }

    scoreBetween(start, end) {
        if (this.statistics) {
            return this.statistics.Score >= start && this.statistics.Score <= end;
        }
    }

    getCertificationStatusColor(status: string) {
        status = status.toLowerCase().trim();

        switch (status) {
            case 'draft':
                return '#BBBBBB';
            case 'certified':
                return '#3f9d40';
            case 'under review':
                return '#e2792a';
            default:
                //custom status, we need to generate a color
                let hash = 0;
                for (let i = 0; i < status.length; i++) {
                    hash = status.charCodeAt(i) + ((hash << 5) - hash);
                    hash = hash & hash;
                }
                return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
        }
    }

    private requestCertification() {
        if (this.currentObject && this.currentObject.objectID)
            this.artifactService
                .requestCertification(this.currentObject.objectID)
                .subscribe(result => { this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID, this.currentObject.hasWorkFlow); });
    }   

    navigateToSurvey() {
        if (this.currentObject) {
            let Url = `${SiteUrlHelpers.SITE_URL_SURVEY_ROOT}/${this.currentObject.objectType}/${this.currentObject.objectTypeID}/${this.currentObject.objectName}/${this.currentObject.objectID}`
            this.showSurvey = false;
            this.router.navigateByUrl(Url);
        }
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
            let scoreItems = this.items.filter(x => x.title === 'Scoring' );
            if (scoreItems.length == 1) {
                scoreItems[0].active = !scoreItems[0].active; 
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
