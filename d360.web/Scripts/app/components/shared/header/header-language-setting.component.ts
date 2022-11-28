import { Component, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef, OnInit, Input, Output, EventEmitter } from "@angular/core";
import * as _ from "lodash";
import { CompanySettingsService } from "../../../services/settings.service";

declare let ApplicationLanguageSetting: string;

interface Language {
	name: string,
	code: string
}

@Component({
	selector: 'd3s-header-language-setting',
	templateUrl: `header-language-setting.component.html`,
	changeDetection: ChangeDetectionStrategy.OnPush,
	styles: [`
	.language-picker-form {
		padding: 16px 0px;
	}
	.language-picker-form label {
		display: block;
		margin-bottom: 8px;
	}
	`]
})
export class HeaderLanguageSettingComponent {
	@Input() isModalVisible = false;
	@Output() onClose = new EventEmitter();

	languages: Language[] = [];
	selectedLanguage: Language;
	initialLanguage: Language;
	savingInProgress = false;

	@ViewChild("popupBox", { static: false }) popupBox: ElementRef;

	defaultSelectionTooltip: string = $localize`Interface will be translated to the browser language if the language version is available. If the version is not available, interface will be displayed in English.`;

	constructor(
		private ref: ChangeDetectorRef,
		private settingService: CompanySettingsService
	) {
		this.languages = [
			{
				name: 'Deutsch - DE',
				code: 'de-De'
			},
			{
				name: 'English - US',
				code: 'en'
			},
			{
				name: 'Espa\u00F1ol - ES',
				code: 'es-ES'
			},
			{
				name: 'Fran\u00E7ais - FR',
				code: 'fr-FR'
			},
			{
				name: 'Italiano - IT',
				code: 'it-IT'
			},
			{
				name: 'Nederlands - NL',
				code: 'nl-NL'
			},
			{
				name: $localize`Browser Language`,
				code: null
			}
		];

		if (!ApplicationLanguageSetting) {
			this.selectedLanguage = this.languages.find((x) => x.code === null);
		}
		else {
			this.selectedLanguage = this.languages.find((x) => x.code.toLowerCase() === ApplicationLanguageSetting.toLowerCase());
		}

		this.initialLanguage = _.cloneDeep(this.selectedLanguage);
	}

	saveChanges() {
		this.savingInProgress = true;
		this.settingService.setLanguage(this.selectedLanguage.code).subscribe((res) => {
			location.reload();
		});
	}

	get isSaveDisabled() {
		return this.initialLanguage.code === this.selectedLanguage.code;
	}
}

