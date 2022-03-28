import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { head } from 'lodash';
import { Category } from '../../../models/object-detail.model';

import { Theme } from '../../../services/branding.service';

@Component({
    selector: "theme-detail",
    templateUrl: "theme-details.component.html",
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./theme-details.component.less"]
})

export class ThemeDetailComponent implements OnChanges {
    @Input() theme: Theme;

    categories: Category[] = new Array<Category>();

    constructor() {

    }

    ngOnChanges(simpleChange: SimpleChanges) {
        this.loadData();
    }

    loadData() {
        console.log("load");
        this.categories = [];


        var header = new Category('Header Bar');
        header.rows = [];
        header.rows.push(
            { title: 'Header logo image', value: this.theme.headerLogo, type: "img" });
        header.rows.push(
            { title: 'Browser logo icon', value: this.theme.icon, type: "img" });
        header.rows.push(
            { title: 'Background color', value: this.theme.navbarBackColor, type: "color" });
        header.rows.push(
            { title: 'Background color selected', value: this.theme.navbarBackColorSelected, type: "color" });
        header.rows.push(
            { title: 'Background link color', value: this.theme.breadcrumbLinkColor, type: "color" });
        header.rows.push(
            { title: 'Button color', value: this.theme.buttonBackColor, type: "color" }
        );

        var navSidebar = new Category('Navigation Sidebar');

        var home = new Category('Home Page');
        home.rows.push(
            { title: 'Background Image', value: this.theme.homeBackground, type: "img" });

        var general = new Category('General');
        general.rows.push(
            { title: 'Primary Button Color', value: this.theme.primaryButtonBackColor, type: "color" }
        );
        general.rows.push(
            { title: 'Background color', value: this.theme.backColor, type: "color" }
        );
        general.rows.push(
            { title: 'Tab/link color', value: this.theme.tabLinkColor, type: "color" }
        );
        general.rows.push(
            { title: 'Table Header Color', value: this.theme.tableHeaderBackColor, type: "color" }
        );
        general.rows.push(
            { title: 'Table Row Background Color', value: this.theme.tableRowBackColor, type: "color" }
        );
        this.categories.push(header);
        this.categories.push(navSidebar);
        this.categories.push(home);
        this.categories.push(general);

        this.categories.forEach((cat) => {
            cat.loaded = true;
            cat.hasData = true;
        });
    }
}