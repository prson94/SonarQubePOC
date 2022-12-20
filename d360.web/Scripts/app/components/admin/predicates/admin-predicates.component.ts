import { Component, OnDestroy } from '@angular/core';
import { Predicate, PredicateFriendlyType } from '../../../models/predicate.model';
import { PredicatesService } from '../../../services/predicates.service';
import { AdminBaseComponent } from '../admin-base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-predicates-component',
    providers: [PredicatesService],
    templateUrl: './admin-predicates.component.html'
})

export class AdminPredicatesComponent extends AdminBaseComponent implements OnDestroy {
    predicates: Predicate[] = [];

    showEditor: boolean = false;
    showDelete: boolean = false;
    selected: Predicate = null;
    theDeleteCallback: Function;

    searchText = $localize`Search...`;
    get deletePromptText(): string {
        return $localize`Are you sure you want to delete the predicate [${this.selected?.Name}]?`;
    }

    constructor(
        private predicatesService: PredicatesService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.theDeleteCallback = this.deletePredicate.bind(this);
        this.areaName = StringConstants.Section_Predicates;
		this.setCommonItems();
		this.buildSecondaryNavigation({ predicateTypeUid: '00000001-0000-0000-0000-b00000000012' });
    }

    ngOnInit() {
        this.getPredicates();
    }

    getPredicates() {
        this.isLoading = true;
        this.predicatesService.getPredicates()
            .subscribe((predicates) => {
                this.predicates = predicates;
                this.selected = predicates[0];
                this.predicates.forEach((p) => p.FriendlyTypeName = PredicateFriendlyType[p.Type] ? PredicateFriendlyType[p.Type] : p.Type);
                this.isLoading = false;
            });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    deletePredicate(uid: string) {
        this.predicatesService.deletePredicate(uid)
            .subscribe((result) => {
                this.showMessageForApiResults(this.messagesService, result, $localize`Predicate deleted`, true);
                this.showDelete = false;
                if (!result.some((x) => x.Success === false)) {
                    this.predicates = this.predicates.filter((x) => x.Uid !== uid);
                }
            });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.predicates.length > 0)
            {this.selected = this.predicates[0];}
    }

    savePredicate(event) {
        const predicate: Predicate = event.item;

        if (this.selected) {
            predicate.Uid = this.selected.Uid;
            if (this.selected.IsInUse) {
                predicate.Type = this.selected.Type;
            }
        }

        this.predicatesService.savePredicate(event.item)
            .subscribe((result) => {

                if (event.action === 'new') {
                    this.showMessageForApiResults(this.messagesService, result, $localize`Predicate succesfully added!`, true);
                }
                else {
                    this.showMessageForApiResults(this.messagesService, result, $localize`Predicate succesfully updated!`, true);
                }
                this.getPredicates();
                this.showEditor = false;
            });
    }

    private showPredicateEditor() {
        if (this.selected.IsSystem) {return;} //dont allow edit of system predicates
        this.showEditor = true;
    }
}