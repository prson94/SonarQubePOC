import { Component, OnDestroy, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SelectItem } from 'primeng/api';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResult } from '../../models/search-result.model';
import { DropdownOption } from '../../models/dropdown.model';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { StringConstants } from '../../static/string-constants';

declare var CompanySettings;
@Component({
    selector: 'd3s-hero-search-input',
    templateUrl: 'hero-search-input.html',
    providers: [SearchService, TypeaheadSearchService],
})

export class HeroSearchInputComponent extends BaseComponent implements OnInit {
    @Input() isExactMatch: boolean = true;
    @Input() searchTypes: string[] = ["Artifact", "Synonym"];


    constructor() {
        super();
    }

    private fields: DropdownOption[] = [
        { title: "Category", value: "Type" },
        { title: "Description", value: "Description" },
        { title: "Name", value: "Name" },
        { title: "Type", value: "_type" },
    ];

    private types: DropdownOption[] = [
        { title: "Attribute", value: "Attribute" },
        { title: "Fusion", value: "FusionAttributes" },
        { title: "Fusion Type", value: "FusionType" },
        { title: StringConstants.AssetTypeClass_Business, value: "Artifact" },
        { title: StringConstants.AssetTypeClass_Technical, value: "Artifact" },
        { title: "Group", value: "Group" },
        { title: "Model", value: "Taxonomy" },
        { title: "Reference", value: "Reference" },
        { title: "User", value: "Resource" },
        { title: "Grammatic Type", value: "Synonym" },
        { title: "Data Quality", value: "Rule" },
    ];

    private searchObjectTypes: SelectItem[] = [
        { value: "Attribute", label: "Attribute" },
        { value: "FusionAttributes", label: "Fusion" },
        { value: "FusionType", label: "Fusion Type" },
        { value: "Artifact", label: StringConstants.AssetTypeClass_Business },
        { value: "Artifact", label: StringConstants.AssetTypeClass_Technical },
        { value: "Group", label: "Group" },
        { value: "Taxonomy", label: "Model" },
        { value: "Policy", label: "Policy" },
        { value: "Reference", label: "Reference" },
        { value: "Resource", label: "User" },
        { value: "Synonym", label: "Grammatic Type" },
        { value: "Rule", label: "Data Quality" },
    ];
    ngOnInit() {
        if (CompanySettings && CompanySettings.FusionEnabled == 'false') {
            this.searchObjectTypes = this.searchObjectTypes.filter(x => x.value != 'FusionAttributes' && x.value != 'FusionType');
            this.types = this.types.filter(x => x.value != 'FusionAttributes' && x.value != 'FusionType');
        }
    }
};
