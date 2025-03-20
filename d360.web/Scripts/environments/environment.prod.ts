declare var VersionNumber: string;


export const environment = {
    production: true,
	version: typeof VersionNumber === "undefined" ? "" : VersionNumber,
	timeStamp: "{BUILD_TIMESTAMP}",
	appInsights: "InstrumentationKey=74ce9442-cbb2-4989-a2d7-c393e0175440;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;ApplicationId=7463ce58-3b8c-4455-af96-165743a24d7f"
};
