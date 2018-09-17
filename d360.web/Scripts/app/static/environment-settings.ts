declare var EnvironmentSettings;

export class CurrentEnvironmentSettings {
    static settings: any = EnvironmentSettings;

    static HelpBaseUri: string = CurrentEnvironmentSettings.settings.HelpBaseUri;
}