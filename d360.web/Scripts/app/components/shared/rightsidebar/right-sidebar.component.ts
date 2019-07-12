import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnInit, SimpleChange, OnChanges, OnDestroy, AfterViewInit } from '@angular/core';
import { Router } from '@angular/router';
import { RightSidebarService  } from '../../../services/right-sidebar.service';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
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

declare var CompanySettings;


@Component({
    selector: 'd3s-right-sidebar',      
    template: ` <div *ngIf="showHeader" class="title-bar" [ngClass]="{'menu-open': menuOpen}">
                    <div class="title">
                         <img class="icon" *ngIf="!IsIcon(area.icon)" [src]="GetURL(area.icon)"  height="20" width="20" />
                         <i *ngIf="IsIcon(area.icon)" [class]="'icon fa ' + area.icon"></i>
                        <h1 >{{area.title ? area.title: 'D3S'}}</h1>
                        <span *ngIf="statistics && statistics.Score;else noScore" class="d3s-icon large-icon" 
                                [ngClass]="{
                                    'bad':scoreBetween(0,49),
                                    'ok':scoreBetween(50,89),
                                    'good':scoreBetween(90,1000)
                                }">
                            <d3s-dynamic-percentage [percentage]="statistics?.Score"></d3s-dynamic-percentage> 
                             <span class="text">{{statistics?.Score}}%</span>
                        </span> 
                        <ng-template #noScore><span *ngIf="object && !object?.isType" title="Governance Score not yet calculated" class="d3s-icon large-icon">No Score</span></ng-template>
                        <span *ngIf="showStatus" class="d3s-icon large-icon">
                            <i class="fa fa-certificate"></i>
                            <span class="text">{{status}}</span>
                        </span>
                        <span class="grow"></span>
                        <button class="button" *ngIf="showCertify" (click)="requestCertification()"><i class="fa fa-certificate"></i><span>Request Certification</span></button>
                        <button class="primary button" (click)="navigateToSurvey()" *ngIf="showSurvey"><i class="fa fa-edit"></i><span>Take Survey</span></button>
                    </div>
                    <div *ngIf="items && items.length > 0" class="tab-view">
                        <div class="tab-bar-outer">
                            <div class="tab-bar can-overflow">
                                <button class="tab" [ngClass]="{'selected':AllClosed()}" (click)="itemClicked({active:false,title:'homeClick', url: 'blank'})">{{area.title}}</button>
                                <button class="tab" 
                                        [ngClass]="{'selected':item.active}" 
                                        *ngFor="let item of items; trackBy: trackById" 
                                        (click)="item.active=!item.active;itemClicked(item);">
                                            {{item.title}}
                                        <span *ngIf="statistics?.CommentCount && item.title === 'Comments'" class="d3s-icon small-icon primary">{{statistics?.CommentCount}}</span>
                                        <span *ngIf="statistics?.IssueCount && item.title === 'Actions'" class="d3s-icon small-icon bad">{{statistics?.IssueCount}}</span>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [SurveysService, ObjectStatisticsService, ArtifactService]
})

export class RightSidebarComponent implements OnChanges, OnDestroy, AfterViewInit{
        
    subscription: Subscription;
    subscriptionClear: Subscription;
    areaSub: Subscription;
    objectSub: Subscription;
    hideHeaderSub: Subscription;

    items: RightSidebarItem[];  

    hostUrl: string;
    area: any = {icon:'fa-folder',title: ''};
    @Input() menuOpen: boolean;

    private currentObject: any;
    private surveyType: SurveyType;

    private statistics: ObjectStatistics;

    status: string;
    showStatus = false;
    showCertify = false;
    showHeader: boolean = false;
    showSurvey: boolean = false;

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


    load() {
        this.showStatus = false;
        this.statistics = null; 
        this.showCertify = false;
        this.showHeader = false;
        this.showSurvey = false;
        this.items = [];

        this.subscription = this.rightSidebarService.rightSidebar$.subscribe(
            item => {
                this.items.push(item);
                this.items = _.sortBy(this.items, 'title');
                this.ref.markForCheck();
            });
        this.subscriptionClear = this.rightSidebarService.rightSidebarClear$.subscribe(
            item => {
                this.items.splice(0, this.items.length);
                this.currentObject = null;
                this.statistics = null;
                this.ref.markForCheck();
            })
        this.areaSub = this.rightSidebarService.currentArea$.subscribe(
            area => {
                this.area = area;
                this.ref.markForCheck();
            }
        );
        this.hideHeaderSub = this.rightSidebarService.hideHeader$.subscribe(result => {
            this.showHeader = result;
            this.ref.markForCheck();
        });

        this.objectSub = this.rightSidebarService.currentObject$.subscribe(res => {
            this.currentObject = res;
            if (this.currentObject && !this.currentObject.isType) {
                this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID);
            } else {
                this.showStatus = false;
                this.statistics = null; 
                this.ref.markForCheck();
            }
        });
        this.ref.markForCheck();
    }

    private loadItemStats(objectID: number, objectName: string, objectType: string, objectTypeID: number) {
        this.objectStatisticsService.getObjectStatus(objectID, objectName).subscribe(
            result => {
                this.status = result;
                if (this.status != undefined && this.status != null && this.status.length > 0) {
                    var draftValues = CompanySettings.RequestCertificationDraft;

                    if (!draftValues) {
                        draftValues = "DRAFT";
                    }

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

    private requestCertification() {
        if (this.currentObject && this.currentObject.objectID)
            this.artifactService
                .requestCertification(this.currentObject.objectID)
                .subscribe(result => { this.loadItemStats(this.currentObject.objectID, this.currentObject.objectName, this.currentObject.objectType, this.currentObject.objectTypeID); });
    }   

    navigateToSurvey() {
        if (this.currentObject) {
            let Url = `${SiteUrlHelpers.SITE_URL_SURVEY_ROOT}/${this.currentObject.objectType}/${this.currentObject.objectTypeID}/${this.currentObject.objectName}/${this.currentObject.objectID}`
            this.showSurvey = false;
            this.router.navigateByUrl(Url);
        }
    }

};
