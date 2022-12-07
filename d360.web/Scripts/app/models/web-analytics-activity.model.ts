export enum UsageAction {
	View = 1,
	Import = 2,
	Export = 3
}

export enum UsageBrowser {
	Chrome = 1,
	Edge = 2,
	FireFox = 3,
	Opera = 4,
	Safari = 5,
	Brave = 6,
	Vivaldi = 7,
	InternetExplorer = 8,
	Other = 9
}

export class UsageEntry {
	assetUid?: string;
	assetTypeUid?: string;
	dashboardUid?: string;
	issueUid?: string;
	semanticUid?: string;
	tagUid?: string;
	tab?: string;
	sidebar?: string;

	action: UsageAction = UsageAction.View;
	browser: UsageBrowser = UsageBrowser.Other;
	language: string = "en";
	locale: string = "en-US";
}