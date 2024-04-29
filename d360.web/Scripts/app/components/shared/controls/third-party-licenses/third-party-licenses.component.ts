/* eslint-disable no-prototype-builtins */
import { CommonModule } from "@angular/common";
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    Input,
    NgModule,
    OnChanges,
    OnInit,
    SimpleChanges,
    ViewEncapsulation
} from "@angular/core";
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CoreModule } from "../../core.module";
import { AccordionModule } from 'primeng/accordion';
import { TreeTableModule } from 'primeng/treetable';
import { LicenseInformationModel } from "../../../../models/third-party-license-model";

/*global $localize*/

@Component({
    selector: "ig-thirdpartylicenses",
    templateUrl: 'third-party-licenses.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ThirdPartyLicenses implements OnInit, OnChanges {
    @Input() src: string;
    @Input() lazy: boolean = false;
    @Input() visible: boolean = false;

    public licensemodel: LicenseInformationModel;
    public loadError: boolean = false;
	public loading: boolean = true;
	public isErrorVisible: boolean = false;

	cols1: any[];
	cols2: any[];
	cols3: any[];
	componentLicenses: any[];
	licenseTexts: any[];
	componentCopyrightTexts: any[];

	componentLicensesHeader: string = $localize`Component Licenses`;
	licenseInformationHeader: string = $localize`License Information`;
	copyrightInformationHeader: string = $localize`Copyright Information`;
	errorMessage: string = $localize`There's been an error.`;

	loadErrorMessage: string = $localize`Third Party License information could not be loaded.`;

    constructor(public ref: ChangeDetectorRef, private http: HttpClient) { }

    ngOnInit() {
        if (!this.lazy) {
            this.fetchLicenseInformation();
        }
    }

    ngOnChanges(changes: SimpleChanges) {
        if (this.lazy && changes["visible"]) {
            if (changes["visible"].currentValue === true) {
                this.fetchLicenseInformation();
            }
        }
    }

    private fetchLicenseInformation() {
        if (typeof this.licensemodel === "undefined") {
            const headers = new HttpHeaders({ "Content-Type": "application/json" });
            this.http.get<LicenseInformationModel>(this.src, { headers }).subscribe(
                (res) => {
					this.licensemodel = res;
					if (this.licensemodel && Object.keys(this.licensemodel).length !== 0) {
						this.isErrorVisible = false;
						this.getLicenseInfo(this.licensemodel);
					} else {
						this.isErrorVisible = true;
					}

                    this.loading = false;
                    this.ref.markForCheck();
                },
                () => {
                    this.loadError = true;
                    this.ref.markForCheck();
                }
            );
        }
    }

	getLicenseInfo(data: LicenseInformationModel) {
		if (data.hasOwnProperty('componentLicenses')) {
			this.componentLicenses = data.componentLicenses.map((cL) => {
				return {
					expanded: true,
					data: {
						projectName: cL.component.projectName,
						versionName: cL.component.versionName,
						licenseName: cL.licenses[0]?.name ?? ''
					},
					children: [
						{
							data: {
								summary: cL.component.projectName
							}
						}
					]
				};
			});
		}
		if (data.hasOwnProperty('licenseTexts')) {
			this.licenseTexts = data.licenseTexts.map((lT) => {
				return {
					expanded: false,
					data: {
						licenseName: lT.name,
						versionName: lT.components[0].versionName
					},
					children: [
						{
							data: {
								summary:
									$localize`License Text` +
									':\n' +
									'<span>' +
									lT.text.replace(/\n/g, '<br>') +
									'</span>'
							}
						}
					]
				};
			});
		}
		if (data.hasOwnProperty('componentCopyrightTexts')) {
			this.componentCopyrightTexts = data.componentCopyrightTexts.map((cT) => {
				return {
					expanded: false,
					data: {
						componentVersionSummary: cT.componentVersionSummary.projectName,
						componentVersionName: cT.componentVersionSummary.versionName,
						originFullName: cT.originFullName,
						copyrightTexts: cT.copyrightTexts
					},
					children: [
						{
							data: {
								summary:
									$localize`License Text` +
									': ' +
									cT.originFullName +
									'<br>' +
									$localize`Copyright Texts` +
									': <p>' +
									cT.copyrightTexts.map((t) => t.replace(/\n/g, '<br>')).join('</p><p>') +
									'</p>'
							}
						}
					]
				};
			});
		}

		this.cols1 = [
			{ field: 'projectName', header: $localize`Component Name` },
			{ field: 'licenseName', header: $localize`License` },
			{ field: 'versionName', header: $localize`Version Number` }
		];
		this.cols2 = [
			{ field: 'licenseName', header: $localize`License Name` },
			{ field: 'versionName', header: $localize`License Version` }
		];
		this.cols3 = [
			{ field: 'componentVersionSummary', header: $localize`Component Version Summary` },
			{ field: 'componentVersionName', header: $localize`Component Version` }
		];
	}

}

@NgModule({
    imports: [
		CommonModule,
		AccordionModule,
		TreeTableModule,
        CoreModule
    ],
    declarations: [ThirdPartyLicenses],
    exports: [ThirdPartyLicenses]
})

export class ThirdPartyLicensesModule { }