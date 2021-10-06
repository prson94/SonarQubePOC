import { Component, OnInit, Input } from '@angular/core';

export interface Detail {
    value: number;
    description: string;
    details: Detail[];
    expanded: boolean;
}

@Component({
    selector: "explain-widget",
    templateUrl: "./explain-widget.component.html",
    styleUrls: ["./explain-widget.component.less"]
})
export class ExplainWidgetComponent implements OnInit {

    @Input() json: any;
    @Input() expanded = false;
    @Input() level: number = 1;

    details: Detail[] = [];

    ngOnInit() {
        this.details = [];

        if (typeof this.json === 'string') {
            this.json = JSON.parse(this.json);
        }

        if (typeof this.json === 'object' && Array.isArray(this.json)) {
            Object.keys(this.json).forEach((key) => {
                this.details.push(this.parseDetail(this.json[`${key}`]));
            });
        } else {
            this.details.push(this.parseDetail(this.json));
        }
    }

    isExpandable(detail: Detail) {
        return detail.details.length > 0;
    }

    toggle(detail: Detail) {
        if (this.isExpandable(detail)) {
            detail.expanded = !detail.expanded;
        }
    }

    private parseDetail(val: any): Detail {
        const detail: Detail = {
            value: val.value,
            description: val.description,
            details: val.details,
            expanded: this.expanded
        };

        var IsDuplicateDetails = true;

        while (IsDuplicateDetails) {
            IsDuplicateDetails = this.reduceDuplicateDetails(detail);
        }

        if (detail.description.substring(0, 4) === "idf,") {
            let desc = this.parseIdf(detail);
            detail.description = desc;
            detail.details = [];
        } else if (detail.description.substring(0, 7) === "tfNorm,") {
            var desc = this.parseTfNorm(detail);
            detail.description = desc;
            detail.details = [];
        }

        return detail;
    }

    private parseIdf(detail: Detail):string {
        let desc = detail.description.slice(0, -6);
        detail.details.forEach((d) => {
            let r = new RegExp('\\b' + d.description + '\\b', 'g');
            desc = desc.replace(r, '<span class="parameter" title="' + d.value + '">' + d.description + '</span>');
        });
        return desc;
    }

    private parseTfNorm(detail: Detail): string {
        let desc = detail.description.slice(0, -6);
        detail.details.forEach((d) => {
            let fld = '';
            if (d.description.substring(0, 9) === "termFreq=") {
                fld = "freq";
            } else if (d.description.substring(0, 10) === "parameter ") {
                fld = d.description.substring(10);
            } else {
                fld = d.description;
            }
            let r = new RegExp('\\b' + fld + '\\b', 'g');
            desc = desc.replace(r, '<span class="parameter" title="' + d.value + '">' + fld + '</span>');
        });
        return desc;
    }

    private reduceDuplicateDetails(detail: Detail): boolean {
        if (detail.details.length !== 1) {
            return false;
        }

        //Remove repetiive details (multiple sums of 1 detail) to simplify output
        if (detail.value === detail.details[0].value && detail.description === detail.details[0].description) {
            detail.details = detail.details[0].details;
            return true;
        }

        return false;
    }
}
