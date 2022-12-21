import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { SurveyTypeV2 } from '../../../models/survey.model';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';
import { LazyLoadEvent } from 'primeng/api';
import { SortOrder } from '../../../models/enums.model';
import { V2ApiFilters } from '../../../models/asset-search.model';
import { AdvancedFiltersHelper } from '../../../static/advanced-filter-helpers';

@Component({
    selector: 'd3s-admin-surveys',
    templateUrl: './admin-surveys.component.html',
    providers: [SurveysService],
})

export class AdminSurveysComponent extends AdminBaseComponent {
    surveys: SurveyTypeV2[] = [];
    selected: SurveyTypeV2;

    pageNum = 0;
    rowsPerPage = 10;
    sortOrder: number = 1;
    sortField: string = 'Name';
    simpleTextFilter: string = '';
    filters: LazyLoadEvent['filters'] = {};
    totalRecords: number;

    error: any;

    showDelete: boolean = false;
    showEditor: boolean = false;

    get deletePromptText(): string {
        return $localize`Are you sure you want to delete the survey [${this.selected?.Name}]?`;
    }

    public theDeleteCallback: Function;

    constructor(private surveysService: SurveysService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        titleService: Title,
        secondaryNavService: SecondaryNavService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Surveys;
        this.setCommonItems();
    }

    ngOnInit() {
        this.getTemplates();
        this.theDeleteCallback = this.deleteSurveyType.bind(this);
    }

    getTemplates() {
        this.isLoading = true;
        this.surveysService
            .getSurveyTypes(this.getSurveyTypesParams())
            .subscribe((res) => {
                this.totalRecords = res.total;
                this.surveys = res.items.sort((a, b) => a.Name.localeCompare(b.Name));
                if (this.surveys.length > 0) {this.selected = this.surveys[0];}
                this.isLoading = false;
            }, (err) => { this.error = err; });
    }
    
    getSurveyTypesParams() {
        const params = new V2ApiFilters();
        params._pageNum = this.pageNum + 1;

        params._pageSize = this.rowsPerPage;
        if (this.sortField) {
            params._order = this.sortField;
        }

        if (this.sortOrder !== SortOrder.None) {
            params._direction = this.sortOrder === SortOrder.Ascending ? "asc" : "desc";
        }

        if (this.simpleTextFilter && this.simpleTextFilter.length > 0) {
            params._simpleFilter = encodeURIComponent(this.simpleTextFilter);
        }
        
        const advancedFilter = AdvancedFiltersHelper.parseFiltersFromTableFilters(this.filters, [
            {
                apiName: 'Name',
                fieldType: 'text',
                name: 'Name',
                type: 'text'
            },
            {
                apiName: 'ValidForDays',
                fieldType: 'number',
                name: 'ValidForDays',
                type: 'number'
            }
        ]);

        if (advancedFilter.length > 0) {
            params['_filter'] = advancedFilter;
        }

        return params;
    }

    loadSurveyTypesLazy(event: LazyLoadEvent) {
        this.pageNum = event.first / event.rows;
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField;
        this.rowsPerPage = event.rows;
        this.simpleTextFilter = event.globalFilter;
        this.filters = event.filters;
        this.getTemplates();
    }

    deleteSurveyType(uid: string) {
        this.surveysService.deleteSurveyTypeById(uid).
            subscribe((result) => {
                if (result !== true) {
                    // error happened
                    this.showDelete = false;
                    return;
                }
                
                this.messagesService.showInfoMessage(
                    null,
                    $localize`Success`
                );

                //remove the template with this id from the grid
                this.surveys.splice(this.findSurveyTypeIndex(uid), 1);
                this.selected = this.surveys.length > 0 ? this.surveys[0] : null;
                this.showDelete = false;
            });
    }

    findSurveyTypeIndex(uid: string) {
        var index: number = -1;
        for (var survey of this.surveys) {
            index++;
            if (survey.Uid === uid) {
                return index;
            }
        }
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.surveys.length > 0)
            {this.selected = this.surveys[0];}
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    saveSurvey(event) {
        this.surveysService.saveSurveyType(event.item)
            .subscribe((result) => {
                if (result == null) {
                    return;
                }

                this.messagesService.showInfoMessage(
                    null,
                    $localize`Success`
                );

                if (event.item.Uid == null) {
                    event.item.Uid = result.Uid;
                    this.surveys[this.surveys.length] = event.item;
                }
                else {
                    this.surveys[this.findSurveyTypeIndex(event.item.Uid)] = event.item;
                }

                this.selected = event.item;
                this.showEditor = false;
            });
    }

}
