
export class PowerBIReport {
	embedUrl: string;
	id: string;
	name: string;
	webUrl: string;
}

export class DashboardTokens {
	AccessToken: string;
	Report: PowerBIReport;
}

export class DashboardDefinition {
	url: string;
	fileName: string;
	powerBiReportId: string;
	powerBiDatasetId: string;
}

export class DashboardModel {
	Id: number;
	uid: string;
	AssetTypeUid: string;
	Name: string;
	Description: string;
	DashboardType: DashboardType;
	Location: DashboardLocation;
	Definition: DashboardDefinition;
	TypeDisplayValue: string;
	Responsibilities: string[];

	FileContent: string;
	//only for ui
	SelectedObjectData: string;
}

export enum DashboardType {
	PowerBi = 'PowerBi',
	DqPlus = 'DqPlus'
}

export enum DashboardLocation {
	List = 'List',
	Detail = 'Detail',
	Homepage = 'Homepage'
}