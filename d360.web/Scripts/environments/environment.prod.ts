declare var VersionNumber: string;


export const environment = {
    production: true,
	version: typeof VersionNumber === "undefined" ? "" : VersionNumber,
	timeStamp: "{BUILD_TIMESTAMP}"
};
