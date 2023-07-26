import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';

interface FeatureFlag {
	flag: string,
	value: boolean
}

@Component({
	selector: 'gallery-featureflag',
	templateUrl: './gallery.featureflag.component.html',
	styles: [
		`
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
	], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryFeatureflagComponent implements OnInit {
	public featureFlags: FeatureFlag[] = [];

	constructor(
		private featureFlagService: LaunchDarklyService
	) {	}

	ngOnInit(): void {
		const allFlags = this.featureFlagService.client.allFlags()
		this.featureFlags = Object.keys(allFlags)
			.filter((key) => key.startsWith("Govern"))
			.reduce((obj, key) => {
				obj.push({
					flag: key,
					value: allFlags[key]
				});
				return obj;
			}, []);
	}
}
