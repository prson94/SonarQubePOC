import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AssetScore } from '../../models/search-result.model';


@Component({
    selector: 'gallery-badge',
    templateUrl: './gallery.badge.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ],    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryBadgeComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<ig-badge [text]="\'Im a badge!\'"></ig-badge>';
    
	protected clicks: string[] = [];

	scoreType: boolean = true;
	useMiniBadge: boolean = false;
	_precision: number = 1;
	get precision(): number {
		return this._precision;
	}
	set precision(val: number|string) {
		this._precision = (typeof val == 'string') ? parseInt(val) : val;
	}
	_scoreValue: number = 0.123456789;
	get scoreValue(): number {
		return this._scoreValue;
	}
	set scoreValue(val: number | string) {
		this._scoreValue = (typeof val == 'string') ? parseFloat(val) : val;
	}

	public get score(): AssetScore {
		return {
			AssetUid: "0000",
			EffectiveDate: "2025-01-01T00:00:00.000Z",
			EndDate: null,
			Value: this.scoreValue,
			ScoreType: this.scoreType ? "Data Quality" : "Governance",
			ShortName: this.scoreType ? "DQ": "GV",
			RunDate: "2025-01-01T00:00:00.000Z",
			LowerThreshold: 50,
			UpperThreshold: 90,
		};
	}

    ngOnInit(): void {
        this.properties = [];
        this.properties.push({ Name: "text", Type: "string", Description: "The text value to be displayed by the badge.", Default: "" });
        this.properties.push({ Name: "variant", Type: "string", Description: "String value for the style for the badge. [default, emphasis, positive, negative, warning, light, custom-light and custom-dark] are the options", Default: "default" });
        this.properties.push({ Name: "backgroundColor", Type: "string", Description: "An override for the background color.", Default: "" });
    }
}
