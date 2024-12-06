import { Injectable } from "@angular/core";
import { AppSettingsEnum } from "../models/settings.model";
import { CompanySettingsService } from "./settings.service";

// eslint-disable-next-line no-var
declare let ApplicationLanguageSetting: string;

@Injectable({
	providedIn: 'root'
})
export class HelpService {

	private fluidServer: string = "";
	private fluidVersion: string = "";

	private _fluidUrlPattern = new RegExp("^(.*)/r/Data360-Govern/(.*)\/$");
	//No translated "Preview" help pages, so for non-English, use Latest
	private _fluidVersionLanguage = {
		"Preview": {
			"nl-NL": "Nieuwste", //"Preview",
			"de-DE": "Neuheiten", //"Vorschau",
			"fr-FR": "Dernière", //"Préliminaire",
			"es-ES": "Más reciente", //"Previsualizar",
			"it-IT": "Più recente", //"Anteprima",
			"en-US": "Preview"
		},
		"Latest": {
			"nl-NL": "Nieuwste",
			"de-DE": "Neuheiten",
			"fr-FR": "Dernière",
			"es-ES": "Más reciente",
			"it-IT": "Più recente",
			"en-US": "Latest"
		}
	};
	private _supportedLanguages = ["nl-NL", "de-DE", "fr-FR", "es-ES", "it-IT", "en-US"];

	constructor(protected settingsService: CompanySettingsService) {
		const helpBaseUri: string = this.settingsService.getAppSetting(AppSettingsEnum.HelpBaseUri);
		[ , this.fluidServer, this.fluidVersion] = helpBaseUri.match(this._fluidUrlPattern);
	}

	private getHelpLanguage(lang: string) {
		lang = lang.split("-").map((e, i) => i === 0 ? e.toLowerCase() : e.toUpperCase()).join("-");
		if (this._supportedLanguages.indexOf(lang) >= 0) {
			return lang;
		}
		return "en-US";
	}

	private getFluidVersion(lang: string, version: string) {
		let vers = this._fluidVersionLanguage[version]["en-US"];
		if (this._fluidVersionLanguage[version][lang]) {
			vers = this._fluidVersionLanguage[version][lang];
		}
		return encodeURI(vers);
	}

	public getHelpUrl(helpPath: string): string {
		const helpLocale = this.getHelpLanguage(typeof ApplicationLanguageSetting !== "undefined" ? ApplicationLanguageSetting : "en");
		const fluidUrl = `${this.fluidServer}/access/sources/D3G_Data360_Govern/topic?topicID=${helpPath}&ft:locale=${helpLocale}&vrm_version_custom=${this.getFluidVersion(helpLocale, this.fluidVersion)}`;
		return fluidUrl;
	}
}