declare var VersionNumber: string;


export const environment = {
    production: true,
	version: typeof VersionNumber === "undefined" ? "" : VersionNumber,
	timeStamp: "{BUILD_TIMESTAMP}",
	appInsights: "InstrumentationKey=d5129af9-d673-4b69-91d2-a8e8464cd20b;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;ApplicationId=b3e7e1d3-7964-49d3-8e92-f5b77320b095"
};
