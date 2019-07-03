import {Component, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {JsonResult} from '../../models/jsonresult.model';
import {FusionConfigurationDetails} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {SiteUrlHelpers} from '../../static/site-url-helpers';

import {BaseComponent} from '../shared/base.component';
import { MessagesObservableService } from '../../services/messages-observable.service';


@Component({
    selector: 'd3s-fusion-manual-load',
    templateUrl: './fusion-manual-load.component.html',
    providers: [FusionService],
})

export class FusionManualLoadComponent extends BaseComponent implements OnInit {
    fusionID: number = 0;
    fusionTypeID: number = 0;
    fusionName: string;
    uploadedFiles: any[] = [];
    fusion: FusionConfigurationDetails;
    routeParams: any;
    getFusionConfiguration: any;

    destroySubject$: Subject<void> = new Subject();

    private selectedFusionAttributeTypeId: number;

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private fusionService: FusionService,
        private messagesService: MessagesObservableService
    ) {
        super();
    }

    ngOnInit() {
        this.route.params
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                params => {
                    this.fusionID = +params['fusionId']; // (+) converts string 'id' to a number

                    this.fusionService
                        .getFusionConfiguration(this.fusionID)
                        .pipe(takeUntil(this.destroySubject$))
                        .subscribe(
                            res => {
                                this.fusion = res;
                                this.fusionName = res.Name;
                                this.fusionTypeID = res.FusionTypeID;
                            }
                        )
                    ;
                }
            );
    }

    onErrorFileUpload(event: any) {
        if (event.xhr && event.xhr.status > 300) {
            try {
                let result: JsonResult;

                result = JSON.parse(event.xhr.responseText);
                this.messagesService.showError(result.title, result.message);
            } catch (e) {
                let msg: string = "";
                let errMsg = JSON.parse(event.xhr.responseText);

                msg = errMsg.message != null ? errMsg.message : event.xhr.responseText;
                this.messagesService.showError('Error', msg);
            }
        }

    }

    private fileUploadUrl() {
        return `services/fusion/${this.fusionTypeID}/configurations/${this.fusionID}/template/${this.selectedFusionAttributeTypeId}`;
    }

    private onUpload(event) {
        let msg: string = "";

        for (let file of event.files) {
            this.uploadedFiles.push(file);
            msg += file.name + "; ";
        }

        if (event.xhr && event.xhr.status == 200) {
            try {
                let result: JsonResult;

                result = JSON.parse(event.xhr.responseText);
                this.messagesService.showInfoMessage(result.title, result.message);
            } catch (e) {
                msg += event.xhr.responseText;
                this.messagesService.showInfoMessage('Success', msg);
            }
        }

    }

    private downloadTemplate() {
        if (this.fusionID == undefined || this.fusionID == null || !this.fusionTypeID || !this.selectedFusionAttributeTypeId) {
            console.log("ERROR - NO FUSION / FUSIONATTRIBUTE TYPE ID POPULATED");

            return;
        }

        this.fusionService.downloadFusionManualLoadTemplate(this.fusionID, this.fusionTypeID, this.selectedFusionAttributeTypeId);
    }

    private goToFusion() {
        this.router.navigateByUrl(SiteUrlHelpers.SITE_URL_FUSION_ROOT);
    }
}
