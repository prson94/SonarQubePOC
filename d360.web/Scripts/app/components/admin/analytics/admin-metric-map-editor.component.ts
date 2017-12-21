import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { Map, MapForm } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';
import { FormMode } from "../../../models/form.model";

@Component({
    selector: 'd3s-admin-metric-map-editor',
    template: ` 
                <header>{{verb}} Mapping</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                    <div class="row">
                        <div class="col s12">
                            <div class="FieldName">
                                Weight
                            </div>
                            <div>
                                <input type="number" style="width: 95%" [(ngModel)]="model.Map.Weight" />
                            </div>
                        </div>
                        <div class="col s6">
                            <div class="FieldName">
                                Metric Item
                            </div>
                            <div>
                                <select [(ngModel)]="model.Map.ItemID" style="width: 95%">
                                    <option></option>
                                    <option *ngFor="let i of model.Items" [value]="i.Value">{{i.Text}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="col s6">
                            <div class="FieldName">
                                Object Type
                            </div>
                            <div>
                                <select [ngModel]="objectTypeString" (ngModelChange)="changeObjectType($event)" style="width: 95%">
                                    <option></option>
                                    <option *ngFor="let i of model.ObjectTypes" [value]="i.Value">{{i.Text}}</option>
                                </select>
                            </div>
                        </div>   
                        <div class="col s6">
                            <div class="FieldName">
                                Effective Start Date
                            </div>
                            <div>
                                <p-calendar [(ngModel)]="model.Map.EffectiveStartDate" [showTime]="false" [dateFormat]="getLocaleDateString()"></p-calendar>
                            </div>
                        </div> 
                        <div class="col s6">
                            <div class="FieldName">
                                Effective End Date
                            </div>
                            <div>
                                <p-calendar [(ngModel)]="model.Map.EffectiveEndDate" [showTime]="false" [dateFormat]="getLocaleDateString()"></p-calendar>
                            </div>
                        </div> 
                        <div class="col s12">
                            <div class="FieldName">
                                Conditions
                            </div>
                            <div>
                                <d3s-admin-metric-condition-list [mapId]="model?.Map?.ID" (formModeChange)="conditionFormMode = $event">
                                </d3s-admin-metric-condition-list>
                            </div>
                        </div> 
                        <div class="col s12" style="padding-top: 15px">
                            <button pButton type="button" label="Save" [disabled]="!valid() || conditionFormMode != FormMode.Default" (click)="save()"></button>
                            <button pButton type="button" label="Cancel" (click)="cancel()" [disabled]="conditionFormMode != FormMode.Default"></button>
                        </div>
                    </div>
                </div>
                `,
    providers: [MetricsService, MessagesService]
})

export class AdminMetricMapEditorComponent extends BaseComponent implements OnInit {
    @Input() mapId: number = -1;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    verb = "Add";

    model: MapForm = null;
    objectTypeString: string = "";
    conditionFormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        if (this.mapId > 0) {
            this.verb = "Edit"
            this.isLoading = true;
            this.metricsService.getMapFormModel(this.mapId)
                .then(r => {
                    this.model = r;
                    this.objectTypeString = r.Map.Object + '|' + r.Map.ObjectID.toString();
                    this.model.Map.EffectiveStartDate = new Date(this.model.Map.EffectiveStartDate);
                    this.model.Map.EffectiveEndDate = new Date(this.model.Map.EffectiveEndDate);
                    this.isLoading = false;
                    //console.log(this.model);
                });
        } else {
            this.verb = "Add";
            this.model = new MapForm();
            this.model.Map = new Map();
            this.isLoading = true;
            this.metricsService.getMapFormModel(-1)
                .then(r => {
                    this.model.Items = r.Items;
                    this.model.ObjectTypes = r.ObjectTypes;

                    this.isLoading = false;
                    //console.log(this.model);
                });

        }
    }

    valid() {
        let valid = true;

        if (this.model == null || this.model.Map == null) {
            valid = false;
        } else {
          //validation goes here
            if (this.model.Map.Object == null || this.model.Map.ObjectID == null)
                valid = false;
            if (this.model.Map.ItemID == null || this.model.Map.ItemID < 1)
                valid = false;
            if (this.model.Map.Weight == null || this.model.Map.Weight < 0 || this.model.Map.Weight > 1)
                valid = false;
        }

        return valid;
    }

    save() {
        this.isLoading = true;
        this.metricsService.saveMap(this.model.Map)
            .then(r => {
                this.showMessageForResult(this.messagesService, r);
                this.isLoading = false;
                this.onSave.emit();
            });
    }

    cancel() {
        this.onCancel.emit();
    }

    changeObjectType(e: any) {
        this.objectTypeString = e;
        if (this.objectTypeString != null && this.objectTypeString.indexOf('|') > -1) {
            this.model.Map.Object = this.objectTypeString.split('|')[0];
            this.model.Map.ObjectID = +this.objectTypeString.split('|')[1];
        }
    }

    getUTCDate(date: Date): Date {
        date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
        return date;
    }


    getLocaleDateString(): string {
        var formats = {
            "ar-SA": "dd/mm/y",
            "bg-BG": "dd.m.yy",
            "ca-ES": "dd/mm/yy",
            "zh-TW": "yy/m/d",
            "cs-CZ": "d.M.yy",
            "da-DK": "dd-mm-yy",
            "de-DE": "dd.mm.yy",
            "el-GR": "d/m/yy",
            "en-US": "m/d/yy",
            "fi-FI": "d.m.yy",
            "fr-FR": "dd/mm/yy",
            "he-IL": "dd/mm/yy",
            "hu-HU": "yy. mm. dd.",
            "is-IS": "d.m.yy",
            "it-IT": "dd/mm/yy",
            "ja-JP": "yy/mm/dd",
            "ko-KR": "yy-mm-dd",
            "nl-NL": "d-m-yy",
            "nb-NO": "dd.mm.yy",
            "pl-PL": "yy-mm-dd",
            "pt-BR": "d/m/yy",
            "ro-RO": "dd.mm.yy",
            "ru-RU": "dd.mm.yy",
            "hr-HR": "d.m.yy",
            "sk-SK": "d. m. yy",
            "sq-AL": "yy-mm-dd",
            "sv-SE": "yy-mm-dd",
            "th-TH": "d/m/yy",
            "tr-TR": "dd.mm.yy",
            "ur-PK": "dd/mm/yy",
            "id-ID": "dd/mm/yy",
            "uk-UA": "dd.mm.yy",
            "be-BY": "dd.mm.yy",
            "sl-SI": "d.m.yy",
            "et-EE": "d.mm.yy",
            "lv-LV": "yy.mm.dd.",
            "lt-LT": "yy.mm.dd",
            "fa-IR": "mm/dd/yy",
            "vi-VN": "dd/mm/yy",
            "hy-AM": "dd.mm.yy",
            "az-Latn-AZ": "dd.mm.yy",
            "eu-ES": "yy/mm/dd",
            "mk-MK": "dd.mm.yy",
            "af-ZA": "yy/mm/dd",
            "ka-GE": "dd.mm.yy",
            "fo-FO": "dd-mm-yy",
            "hi-IN": "dd-mm-yy",
            "ms-MY": "dd/mm/yy",
            "kk-KZ": "dd.mm.yy",
            "ky-KG": "dd.mm.y",
            "sw-KE": "m/d/yy",
            "uz-Latn-UZ": "dd/mm yy",
            "tt-RU": "dd.mm.yy",
            "pa-IN": "dd-mm-y",
            "gu-IN": "dd-mm-y",
            "ta-IN": "dd-mm-yy",
            "te-IN": "dd-mm-y",
            "kn-IN": "dd-mm-y",
            "mr-IN": "dd-mm-yy",
            "sa-IN": "dd-mm-yy",
            "mn-MN": "y.mm.dd",
            "gl-ES": "dd/mm/y",
            "kok-IN": "dd-mm-yy",
            "syr-SY": "dd/mm/yy",
            "dv-MV": "dd/mm/y",
            "ar-IQ": "dd/mm/yy",
            "zh-CN": "yy/m/d",
            "de-CH": "dd.mm.yy",
            "en-GB": "dd/mm/yy",
            "es-MX": "dd/mm/yy",
            "fr-BE": "d/mm/yy",
            "it-CH": "dd.mm.yy",
            "nl-BE": "d/mm/yy",
            "nn-NO": "dd.mm.yy",
            "pt-PT": "dd-mm-yy",
            "sr-Latn-CS": "d.m.yy",
            "sv-FI": "d.m.yy",
            "az-Cyrl-AZ": "dd.mm.yy",
            "ms-BN": "dd/mm/yy",
            "uz-Cyrl-UZ": "dd.mm.yy",
            "ar-EG": "dd/mm/yy",
            "zh-HK": "d/M/yy",
            "de-AT": "dd.mm.yy",
            "en-AU": "d/mm/yy",
            "es-ES": "dd/mm/yy",
            "fr-CA": "yy-mm-dd",
            "sr-Cyrl-CS": "d.m.yy",
            "ar-LY": "dd/mm/yy",
            "zh-SG": "d/M/yy",
            "de-LU": "dd.mm.yy",
            "en-CA": "dd/mm/yy",
            "es-GT": "dd/mm/yy",
            "fr-CH": "dd.mm.yy",
            "ar-DZ": "dd-mm-yy",
            "zh-MO": "d/m/yy",
            "de-LI": "dd.mm.yy",
            "en-NZ": "d/mm/yy",
            "es-CR": "dd/mm/yy",
            "fr-LU": "dd/mm/yy",
            "ar-MA": "dd-mm-yy",
            "en-IE": "dd/mm/yy",
            "es-PA": "mm/dd/yy",
            "fr-MC": "dd/mm/yy",
            "ar-TN": "dd-mm-yy",
            "en-ZA": "yy/mm/dd",
            "es-DO": "dd/mm/yy",
            "ar-OM": "dd/mm/yy",
            "en-JM": "dd/mm/yy",
            "es-VE": "dd/mm/yy",
            "ar-YE": "dd/mm/yy",
            "en-029": "mm/dd/yy",
            "es-CO": "dd/mm/yy",
            "ar-SY": "dd/mm/yy",
            "en-BZ": "dd/mm/yy",
            "es-PE": "dd/mm/yy",
            "ar-JO": "dd/mm/yy",
            "en-TT": "dd/mm/yy",
            "es-AR": "dd/mm/yy",
            "ar-LB": "dd/mm/yy",
            "en-ZW": "m/d/yy",
            "es-EC": "dd/mm/yy",
            "ar-KW": "dd/mm/yy",
            "en-PH": "m/d/yy",
            "es-CL": "dd-mm-yy",
            "ar-AE": "dd/mm/yy",
            "es-UY": "dd/mm/yy",
            "ar-BH": "dd/mm/yy",
            "es-PY": "dd/mm/yy",
            "ar-QA": "dd/mm/yy",
            "es-BO": "dd/mm/yy",
            "es-SV": "dd/mm/yy",
            "es-HN": "dd/mm/yy",
            "es-NI": "dd/mm/yy",
            "es-PR": "dd/mm/yy",
            "am-ET": "d/m/yy",
            "tzm-Latn-DZ": "dd-mm-yy",
            "iu-Latn-CA": "d/mm/yy",
            "sma-NO": "dd.mm.yy",
            "mn-Mong-CN": "yy/m/d",
            "gd-GB": "dd/mm/yy",
            "en-MY": "d/m/yy",
            "prs-AF": "dd/mm/y",
            "bn-BD": "dd-mm-y",
            "wo-SN": "dd/mm/yy",
            "rw-RW": "m/d/yy",
            "qut-GT": "dd/mm/yy",
            "sah-RU": "mm.dd.yy",
            "gsw-FR": "dd/mm/yy",
            "co-FR": "dd/mm/yy",
            "oc-FR": "dd/mm/yy",
            "mi-NZ": "dd/mm/yy",
            "ga-IE": "dd/mm/yy",
            "se-SE": "yy-mm-dd",
            "br-FR": "dd/mm/yy",
            "smn-FI": "d.m.yy",
            "moh-CA": "m/d/yy",
            "arn-CL": "dd-mm-yy",
            "ii-CN": "yy/m/d",
            "dsb-DE": "d. m. yy",
            "ig-NG": "d/m/yy",
            "kl-GL": "dd-mm-yy",
            "lb-LU": "dd/mm/yy",
            "ba-RU": "dd.mm.y",
            "nso-ZA": "yy/mm/dd",
            "quz-BO": "dd/mm/yy",
            "yo-NG": "d/m/yy",
            "ha-Latn-NG": "d/m/yy",
            "fil-PH": "m/d/yy",
            "ps-AF": "dd/mm/y",
            "fy-NL": "d-m-yy",
            "ne-NP": "m/d/yy",
            "se-NO": "dd.mm.yy",
            "iu-Cans-CA": "d/m/yy",
            "sr-Latn-RS": "d.m.yy",
            "si-LK": "yy-mm-dd",
            "sr-Cyrl-RS": "d.m.yy",
            "lo-LA": "dd/mm/yy",
            "km-KH": "yy-mm-dd",
            "cy-GB": "dd/mm/yy",
            "bo-CN": "yy/m/d",
            "sms-FI": "d.m.yy",
            "as-IN": "dd-mm-yy",
            "ml-IN": "dd-mm-y",
            "en-IN": "dd-mm-yy",
            "or-IN": "dd-mm-y",
            "bn-IN": "dd-mm-y",
            "tk-TM": "dd.mm.y",
            "bs-Latn-BA": "d.m.yy",
            "mt-MT": "dd/mm/yy",
            "sr-Cyrl-ME": "d.m.yy",
            "se-FI": "d.m.yy",
            "zu-ZA": "yy/mm/dd",
            "xh-ZA": "yy/mm/dd",
            "tn-ZA": "yy/mm/dd",
            "hsb-DE": "d. m. yy",
            "bs-Cyrl-BA": "d.m.yy",
            "tg-Cyrl-TJ": "dd.mm.y",
            "sr-Latn-BA": "d.m.yy",
            "smj-NO": "dd.mm.yy",
            "rm-CH": "dd/mm/yy",
            "smj-SE": "yy-mm-dd",
            "quz-EC": "dd/mm/yy",
            "quz-PE": "dd/mm/yy",
            "hr-BA": "d.m.yy.",
            "sr-Latn-ME": "d.m.yy",
            "sma-SE": "yy-mm-dd",
            "en-SG": "d/m/yy",
            "ug-CN": "yy-m-d",
            "sr-Cyrl-BA": "d.m.yy",
            "es-US": "m/d/yy"
        };
        return formats[navigator.language] || 'mm/dd/yy';
    }
};