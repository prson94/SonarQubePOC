import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SelectItem } from 'primeng/api';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SettingsHelper } from '../../models/settings.model';
import { StringConstants } from '../../static/string-constants';

declare var CompanySettings;
@Component({
    selector: 'd3s-hero-search-input',
    templateUrl: 'hero-search-input.html',
    providers: [SearchService, TypeaheadSearchService],
})

export class HeroSearchInputComponent extends BaseComponent implements OnInit {
    @Input() isExactMatch: boolean = true;
    @Input() searchTypes: string[] = ["BusinessAsset", "Synonym"];


    constructor() {
        super();
    }

    private searchObjectTypes: SelectItem[] = SettingsHelper.getSearchTypesList().map((set) => {
        return {
            label: set.title,
            value: set.value
        };
    });

    ngOnInit() {
        if (CompanySettings) {
            if (CompanySettings.FusionEnabled == 'false') {
                this.searchObjectTypes = this.searchObjectTypes.filter(x => x.value != 'FusionAttributes' && x.value != 'FusionType');
            }
            if (CompanySettings.FusionEnabled == 'true') {
                this.searchObjectTypes = this.searchObjectTypes.filter(x => x.value != 'TechnicalAsset');
            }
        }
    }
    setEventTypeLabel() {
        let label = (document.getElementById('searchMultiSelect')
            .getElementsByClassName('ui-multiselect-label-container')[0]
            .getElementsByClassName('ui-multiselect-label')[0]);
        if (this.searchTypes.length == 0) {
            label.textContent = 'Search All Categories';
        } else if (this.searchTypes.length == 1) {
            label.textContent = 'Search ' + this.searchObjectTypes.filter((x) => this.searchTypes.indexOf(x.value) >= 0).map((x) => x.label).join(', ');
        } else if (this.searchTypes.length == this.searchObjectTypes.length) {
            label.textContent = 'Search All Categories';
        } else {
            label.textContent = 'Search ' + this.searchTypes.length + ' Categories';
        }
    }
};
