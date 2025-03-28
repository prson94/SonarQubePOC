import { Component, EventEmitter, Input, Output } from '@angular/core';
import { HeroSearch } from '../../../_shared/components/hero-search';
import { BaseComponent } from '../../../components/shared/base.component';
import { SearchResults, SearchAggregation } from '../../../models/search-result.model';
import { CompanySettingEnum } from '../../../models/settings.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
	selector: 'search',
	standalone: true,
	imports: [HeroSearch],
	template: `<hero-search [(isExactMatch)]="isExactMatch" [(searchTypes)]="searchTypes"></hero-search>`
})
export class HomeSearch extends BaseComponent {
	@Output() resultsChange = new EventEmitter();
	@Input() hasResults: boolean;
	public searchResults: SearchResults;
	public categories: SearchAggregation[] = [];
	isExactMatch: boolean = true;
	searchTypes: string[] = [];

	constructor(
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit() {
		this.searchTypes = (this.settingsService.getSettingById(CompanySettingEnum.DefaultSearchTypes).ScalarValue ?? "").split(',');
	}
}