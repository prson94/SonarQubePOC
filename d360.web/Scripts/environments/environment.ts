
// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.
declare var VersionNumber: string;

export const environment = {
	production: false,
	version: typeof VersionNumber === "undefined" ? "" : VersionNumber,
	timeStamp: "{BUILD_TIMESTAMP}",
	appInsights: "InstrumentationKey=540e47d4-1cf5-4647-86b1-3c5478eaf78e;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;ApplicationId=bd3bdb48-93d6-4833-821d-d3ee225280da"
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/plugins/zone-error';  // Included with Angular CLI.