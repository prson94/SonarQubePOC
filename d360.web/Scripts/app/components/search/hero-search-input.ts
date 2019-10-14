import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SelectItem } from 'primeng/api';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SettingsHelper } from '../../models/settings.model';

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
        }
    });

    ngOnInit() {
        if (CompanySettings) {
            if (CompanySettings.FusionEnabled == 'false') {
                this.searchObjectTypes = this.searchObjectTypes.filter(x => x.value != 'FusionAttributes' && x.value != 'FusionType');
            }
            if (+CompanySettings.LineageVersion != 3) {
                this.searchObjectTypes = this.searchObjectTypes.filter(x => x.label != StringConstants.AssetTypeClass_Technical);
            }
        }
    }
};
