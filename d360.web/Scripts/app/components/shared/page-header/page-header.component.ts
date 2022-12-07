import { Component, Input } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { Subscription } from "rxjs";
import { filter } from "rxjs/operators";
import { CompanySettingEnum } from "../../../models/settings.model";
import { SecondaryNavService } from "../../../services/right-sidebar.service";
import { CompanySettingsService } from "../../../services/settings.service";


@Component({
    selector: "d3s-page-header",
    templateUrl: './page-header.component.html',
    styleUrls: ['./page-header.component.less']
})
export class PageHeaderComponent {
    @Input() icon: string;
    @Input() header: string;

    constructor(
        private titleService: Title,
        private settingsService: CompanySettingsService,
        private secondaryNavService: SecondaryNavService) { }

    ngOnChanges() {
        this.titleService.setTitle(`${this.settingsService.getSettingById(CompanySettingEnum.BrowserTitlePrefix).StringSetting.Value} - ${this.header}`);
    }

    secondaryNavSubscription: Subscription;
    ngOnInit() {
        this.secondaryNavService.showHeader(false);

        // Next lines fix concurrent issue when breadcrumbs are set by other page after page destroyed
        this.secondaryNavSubscription = this.secondaryNavService.hideHeader$
            .pipe(filter((x) => x === true))
            .subscribe(() => {
                this.secondaryNavService.showHeader(false);
            });
    }

    ngOnDestroy() {
        this.secondaryNavSubscription.unsubscribe();
    }
}
