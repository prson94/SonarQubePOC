import { Component, Input } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { CompanySettingEnum } from "../../../models/settings.model";
import { CompanySettingsService } from "../../../services/settings.service";


@Component({
    selector: "d3s-page-header",
    templateUrl: './page-header.component.html',
    styleUrls: ['./page-header.component.less']
})
export class PageHeaderComponent {
    @Input() icon: string;
    @Input() header: string;

    constructor(private titleService: Title, private settingsService: CompanySettingsService) { }

    ngOnChanges() {
        this.titleService.setTitle(`${this.settingsService.getSettingById(CompanySettingEnum.BrowserTitlePrefix).StringSetting.Value} - ${this.header}`);
    }
}