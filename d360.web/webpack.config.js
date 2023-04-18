const ModuleFederationPlugin = require('webpack/lib/container/ModuleFederationPlugin');
const mf = require('@angular-architects/module-federation/webpack');
const path = require('path');
const { shareAll } = require('@angular-architects/module-federation/webpack');

const sharedMappings = new mf.SharedMappings();
sharedMappings.register(path.join(__dirname, 'Scripts/tsconfig.json'), [
	/* mapped paths to share */
]);

module.exports = {
	output: {
		uniqueName: 'govern',
		publicPath: 'auto',
		scriptType: process.env.FEDERATION_BUILD?.trim() === 'TRUE' ? 'module' : 'text/javascript'
	},
	optimization: {
		runtimeChunk: false
	},
	resolve: {
		alias: {
			...sharedMappings.getAliases()
		}
	},
	experiments: {
		outputModule: true
	},
	plugins: [
		new ModuleFederationPlugin({
			library: { type: 'module' },
			name: 'govern',
			filename: 'remoteEntry.js',
			exposes: {
				'./HomeModule': 'Scripts/app/components/home/home.module.ts',
				'./SearchModule': 'Scripts/app/components/search/search.module.ts',
				'./AssetModule': 'Scripts/app/components/asset/asset.module.ts',
				'./AssetsBaseModule': 'Scripts/app/components/assets-base/assets-base.module.ts',
				'./HierarchyModule': 'Scripts/app/components/hierarchy/hierarchy.module.ts',
				'./DataCatalogModule': 'Scripts/app/components/data-catalog/data-catalog.module.ts',
				'./RightsidebarModule': 'Scripts/app/components/shared/rightsidebar/right-sidebar.module'
			},
			shared: {
				...shareAll({ singleton: true, strictVersion: false, requiredVersion: 'auto', includeSecondaries: true }),
				...sharedMappings.getDescriptors()
			}
		}),
		sharedMappings.getPlugin()
	]
};