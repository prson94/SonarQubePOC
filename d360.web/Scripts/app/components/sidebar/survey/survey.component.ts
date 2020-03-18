import { Component, OnInit, Input, OnDestroy, AfterViewInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SurveysService } from '../../../services/surveys.service';
import { SurveyType } from '../../../models/survey.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-survey',
    template: `
            <d3s-take-survey 
                *ngIf="!isLoading && showBoard" 
                    [surveyType]="surveyType" 
                    [objectID]="objectId" 
                    [objectType]="objectName" 
                    [ShowCloseButton]="true" 
                    (surveyBack)="goBack()" 
                    (surveyComplete)="handleComplete($event)">
            </d3s-take-survey>
        `,    
    providers: [SurveysService]
})

export class SurveyComponent extends BaseComponent implements AfterViewInit, OnDestroy {
    @Input() objectType: string = "";
    @Input() objectTypeId: number = 0;
    @Input() objectName: string = "";
    @Input() objectId: number = 0;

    private surveyType: SurveyType;
    private sub: any;
    daysToLookBack: number = 180;
    hasCloseButton: boolean = false;
    showBoard: boolean = false;

    constructor(private route: ActivatedRoute,
        private surveysService: SurveysService,
        private msgService: MessagesObservableService,
        private router: Router) { super(); }

    ngAfterViewInit(): void {    
        this.isLoading = true;

        this.sub = this.route.params.subscribe(params => {
            this.objectType = params['objectType'];
            this.objectTypeId = +params['objectTypeId'];
            this.objectName = params['objectName'];
            this.objectId = +params['objectId'];
            this.GetSurvey(this.objectId, this.objectName, this.objectType, this.objectTypeId)
            this.isLoading = false;
        });
    }

    GetSurvey(objectID: number, objectName: string, objectType: string, objectTypeID: number) {
        this.surveysService.getObjectSurvey(objectTypeID, objectType, objectID, objectName)
            .subscribe(result => {
                this.surveyType = undefined;

                if (result) {
                    this.surveyType = result;
                    this.showBoard = true;
                }

            }); 
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    handleComplete(res) {
        this.showMessageForResult(this.msgService, res);
        this.showBoard = true;
        this.goBack();
    }

    goBack() {
        let Url = SiteUrlHelpers.getObjectUrl(this.objectName, this.objectId, this.objectTypeId, this.objectType);
        this.router.navigateByUrl(Url); 
    }
}