import { Component, Input, OnInit, AfterViewInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SelectItem } from 'primeng/api';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { AuthenticationService } from '../../services/authentication.service';
import { CompanySettingsService } from '../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'd3s-hero-search-input',
    templateUrl: 'hero-search-input.html',
    providers: [TypeaheadSearchService],
})

export class HeroSearchInputComponent extends BaseComponent implements OnInit, AfterViewInit {
    @Input() isExactMatch: boolean = true;
    @Input() searchTypes: string[] = ["BusinessAsset", "Synonym"];

    searchObjectTypes: SelectItem[] = [];

    constructor(
        protected authenticationService: AuthenticationService,
        protected searchService: SearchService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.searchService.getSearchCategories(this.authenticationService.isAdmin, false).subscribe((cat) => {
            this.searchObjectTypes = cat.map((set) => {
                return {
                    label: set.title,
                    value: set.value
                };
            });
            var availableTypes = this.searchObjectTypes.map((x) => x.value);
            this.searchTypes = this.searchTypes.filter(st => availableTypes.indexOf(st) >= 0);

            this.setEventTypeLabel();
        });
    }

    ngAfterViewInit(): void {
        this.setEventTypeLabel();
    }

    setEventTypeLabel() {
        let label = (document.getElementById('searchMultiSelect')
            .getElementsByClassName('p-multiselect-label-container')[0]
            .getElementsByClassName('p-multiselect-label')[0]);
        if (this.searchTypes.length == 0) {
            label.textContent = $localize`Search All Categories`;
        } else if (this.searchTypes.length == 1) {
            label.textContent = $localize`Search` + ' ' + this.searchObjectTypes.filter((x) => this.searchTypes.indexOf(x.value) >= 0).map((x) => x.label).join(', ');
        } else if (this.searchTypes.length == this.searchObjectTypes.length) {
            label.textContent = $localize`Search All Categories`;
        } else {
            label.textContent = $localize`Search ${this.searchTypes.length} Categories`;
        }
    }
}
