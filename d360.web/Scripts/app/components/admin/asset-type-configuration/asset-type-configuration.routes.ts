import { Injectable } from '@angular/core';
import { Routes, ActivatedRouteSnapshot, RouterStateSnapshot, CanActivate } from '@angular/router';

import { ConfigurationAssetTypeListPageComponent } from './list/configuration-asset-type-list-page.component';
import { AssetTypeClass } from '../../../models/asset.model';
import { ConfigurationAssetTypeEditorPageComponent } from './edit/configuration-asset-type-editor-page.component';
import { ConfigurationAssetTypeDeletePageComponent } from './delete/configuration-asset-type-delete-page.component';
import { ConfigurationAssetTypeFieldsPageComponent } from './tabs/fields/configuration-asset-type-fields-page.component';
import { ConfigurationAssetTypeOwnersPageComponent } from './tabs/owners/configuration-asset-type-owners-page.component';
import { ConfigurationAssetTypeAllocationsPageComponent } from './tabs/allocations/configuration-asset-type-allocations-page.component';
import { ConfigurationAssetTypeRelationshipsPageComponent } from './tabs/relationships/configuration-asset-type-relationships-page.component';
import { ConfigurationAssetTypeLogPageComponent } from './tabs/log/configuration-asset-type-log-page.component';
import { featuresToTypeClasses } from './shared/featuresToTypeClasses';
import { GovernanceRolesComponent } from './governanceRoles/governance-roles.component';
import { ConfigurationAssetTypeConnectorLabelsPageComponent } from './connectorLabels/configuration-asset-type-connector-labels-page.component';
import { ConfigurationAssetTypeLevelsPageComponent } from './tabs/levels/configuration-asset-type-levels-page.component';


abstract class CanActivateOnlyForAvailableTypeClasses implements CanActivate {
    protected abstract typeClasses: AssetTypeClass[];

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
        return this.typeClasses.includes(AssetTypeClass[route.params.typeClass as string]);
    }
}

@Injectable({ providedIn: 'root' })
class WhenCanAccessBasicFeaturesGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.Model,
		AssetTypeClass.Policy,
		AssetTypeClass.Rule,
		AssetTypeClass.DiagramAsset
    ]
}

@Injectable({ providedIn: 'root' })
class WhenCanCreateNewAssetTypeChildGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = featuresToTypeClasses.assetTypeChilds;
}

@Injectable({ providedIn: 'root' })
class WhenCanSeeFieldDefinitionsGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.Model,
		AssetTypeClass.Policy,
		AssetTypeClass.Rule,
		AssetTypeClass.DiagramAsset
    ]
}

@Injectable({ providedIn: 'root' })
class WhenCanSeeOwnersGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.Model,
		AssetTypeClass.Policy,
		AssetTypeClass.Rule,
		AssetTypeClass.DiagramAsset
    ]
}

@Injectable({ providedIn: 'root' })
class WhenCanSeeAllocationsGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.Model,
		AssetTypeClass.Policy,
		AssetTypeClass.Rule,
		AssetTypeClass.DiagramAsset
    ]
}

@Injectable({ providedIn: 'root' })
class WhenCanSeeRelationshipsGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.Model,
		AssetTypeClass.Policy,
		AssetTypeClass.Rule,
		AssetTypeClass.DiagramAsset
    ]
}

@Injectable({ providedIn: 'root' })
class WhenCanSeeLogGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.Model,
		AssetTypeClass.Policy,
		AssetTypeClass.Rule,
		AssetTypeClass.DiagramAsset
    ]
}

@Injectable({ providedIn: 'root' })
class WhenCanSeeLevelsGuard extends CanActivateOnlyForAvailableTypeClasses {
	protected typeClasses: AssetTypeClass[] = [
		AssetTypeClass.Model,
		AssetTypeClass.Policy
	]
}

export const assetTypeConfigurationRoutes: Routes = [
    {
        path: 'DiagramAsset/governanceRoles',
        component: GovernanceRolesComponent
    },
    {
        path: 'DiagramAsset/connectorLabels',
        component: ConfigurationAssetTypeConnectorLabelsPageComponent,
    },
    {
        path: ':typeClass/new',
        component: ConfigurationAssetTypeEditorPageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
    {
        path: ':typeClass/:parentUid/new',
        component: ConfigurationAssetTypeEditorPageComponent,
        canActivate: [WhenCanCreateNewAssetTypeChildGuard]
    },
    {
        path: ':typeClass/:uid/edit',
        component: ConfigurationAssetTypeEditorPageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
    {
        path: ':typeClass/:uid/delete',
        component: ConfigurationAssetTypeDeletePageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
    {
        path: ':typeClass/:uid/fields',
        component: ConfigurationAssetTypeFieldsPageComponent,
        canActivate: [WhenCanSeeFieldDefinitionsGuard]
    },
    {
        path: ':typeClass/:uid/owners',
        component: ConfigurationAssetTypeOwnersPageComponent,
        canActivate: [WhenCanSeeOwnersGuard]
    },
    {
        path: ':typeClass/:uid/allocations',
        component: ConfigurationAssetTypeAllocationsPageComponent,
        canActivate: [WhenCanSeeAllocationsGuard]
    },
    {
        path: ':typeClass/:uid/relationships',
        component: ConfigurationAssetTypeRelationshipsPageComponent,
        canActivate: [WhenCanSeeRelationshipsGuard]
    },
    {
        path: ':typeClass/:uid/log',
        component: ConfigurationAssetTypeLogPageComponent,
        canActivate: [WhenCanSeeLogGuard]
	},
	{
		path: ':typeClass/:uid/levels',
		component: ConfigurationAssetTypeLevelsPageComponent,
		canActivate: [WhenCanSeeLevelsGuard]
	},
    {
        path: ':typeClass',
        component: ConfigurationAssetTypeListPageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
];
