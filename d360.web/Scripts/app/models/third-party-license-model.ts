export interface LicenseInformationModel {
	projectVersion: ProjectVersion;
	componentLicenses: ComponentLicense[];
	licenseTexts: LicenseText[];
	componentCopyrightTexts: ComponentCopyrightText[];
}

export interface ProjectVersion {
	projectName: string;
	versionName: string;
	versionPhase: string;
	versionDistribution: string;
}

export interface ComponentLicense {
	component: Component;
	licenses: License[];
}

export interface Component {
	projectName: string;
	versionName: string;
}

export interface License {
	name: string;
}

export interface LicenseText {
	name: string;
	text: string;
	modified: boolean;
	components: Component[];
}

export interface ComponentCopyrightText {
	componentVersionSummary: ComponentVersionSummary;
	originFullName: string;
	copyrightTexts: string[];
	componentProjectName: string;
}

export interface ComponentVersionSummary {
	projectName: string;
	versionName: string;
}