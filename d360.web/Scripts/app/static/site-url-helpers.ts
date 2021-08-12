declare var CompanySettings;
declare var ResourceHomePage;

export class SiteUrlHelpers {

    //main site routes
    // WARNING!! - SOME URLS SUCH AS TOOLTIPS ARE BURNED IN THE DB DO NOT CHANGES THE BELOW WITHOUT 
    // UPDATING BOTH!!
    static SITE_URL_FUSION_ROOT = 'fusion';
    static SITE_URL_REFERENCE_ROOT = 'reference';
    static SITE_URL_ARTIFACT_ROOT = 'artifact';
    static SITE_URL_ASSET_ROOT = 'asset';
    static SITE_URL_ASSETTYPE_ROOT = 'assettype';
    static SITE_URL_ASSETS_ROOT = 'assets';
    static SITE_URL_COMMUNITY_ROOT = 'community';
    static SITE_URL_HELP_ROOT = 'help';
    static SITE_URL_MONITOR_ROOT = 'monitor';
    static SITE_URL_WORKFLOWMONITOR_ROOT = 'workflowitems';
    static SITE_URL_POLICY_ROOT = 'policy';
    static SITE_URL_GROUP_ROOT = 'group';
    static SITE_URL_RESOURCE_ROOT = 'resource';
    static SITE_URL_RULE_ROOT = 'quality/rule';
    static SITE_URL_TAG_ROOT = 'tag';
    static SITE_URL_CONNECTORLABEL_ROOT = 'connectorLabel';
    static SITE_URL_SEARCH_ROOT = 'search';
    static SITE_URL_WORKFLOW_ROOT = 'workflow';
    static SITE_URL_MODEL_ROOT = 'model';
    static SITE_URL_ADMIN_ROOT = 'admin';
    static SITE_URL_HOME_ROOT = 'home';
    static SITE_URL_GALLERY_ROOT = 'gallery';
    static SITE_URL_AUDIT_ROOT = 'sidebar/audit';
    static SITE_URL_DASHBOARD_ROOT = 'dashboard';
    static SITE_URL_FOLLOWERS_ROOT = 'sidebar/followers';
    static SITE_URL_OWNERSHIP_ROOT = 'sidebar/ownership';
    static SITE_URL_VISUALIZATION_ROOT = 'sidebar/visualization';
    static SITE_URL_RELATIONSHIP_ROOT = 'sidebar/relationships'
    static SITE_URL_CHILDREN_ROOT = 'sidebar/children';
    static SITE_URL_WORKFLOW_MONITOR_ROOT = 'sidebar/workflowmonitor';
    static SITE_URL_FIELDS_ROOT = 'sidebar/fields';
    static SITE_URL_RESPONSIBILITIES_ROOT = 'sidebar/responsibilities';
    static SITE_URL_SHOPPING_CART_ROOT = 'cart';
    static SITE_URL_ITEM_FOLLOW_ROOT = 'sidebar/itemfollow';
    static SITE_URL_ITEM_OWN_ROOT = 'sidebar/itemown';
    static SITE_URL_MEMBER_GROUP_ROOT = 'sidebar/membergroup';
    static SITE_URL_COMMENTS_ROOT = 'sidebar/comments';
    static SITE_URL_SCORE_ROOT = 'sidebar/score';
    static SITE_URL_ACTIONS_ROOT = 'sidebar/actions';
    static SITE_URL_RULERESULT_ROOT = 'sidebar/ruleResults';
    static SITE_URL_GOVERNANCE_ROLES_ROOT = 'sidebar/governanceRoles';
    static SITE_URL_CONNECTOR_LABELS_ROOT = 'sidebar/connectorLabels';
    //hierarchy child routes
    static SITE_URL_HIERARCHY_CLASSIFICATION = 'classification';

    //workflow child routes
    static SITE_URL_WORKFLOW_RAISE_ISSUE = 'raiseissue';
    static SITE_URL_WORKFLOW_VIEW_ITEM = 'work';
    static SITE_URL_WORKFLOW_VIEW_STATUS = 'status';
    static SITE_URL_WORKFLOW_V2_VIEW_STATUS = 'details';
    static SITE_URL_WORKFLOW_LIST = 'workflowlist';
    static SITE_URL_WORKFLOW_LIST_V2 = 'workflowlistnew';
    static SITE_URL_WORKFLOW_FORM = 'form';

    //admin child routes
    static SITE_URL_ADMIN_BULK_LOAD = `load`;
    static SITE_URL_ADMIN_ASSET = `assets`;
    static SITE_URL_ADMIN_ASSET_BUSINESS = `BusinessAsset`;
    static SITE_URL_ADMIN_ASSET_TECHNICAL = `TechnicalAsset`;
    static SITE_URL_ADMIN_CUSTOMIZATIONS = 'customizations';
    static SITE_URL_ADMIN_CUSTOM_API = 'customapi';
    static SITE_URL_ADMIN_MODELS = 'taxonomies';
    static SITE_URL_ADMIN_POLICIES = 'policies';
    static SITE_URL_ADMIN_RELATIONSHIPS = 'relationships';
    static SITE_URL_ADMIN_RULES = 'rules';
    static SITE_URL_ADMIN_SURVEYS = 'surveys';
    static SITE_URL_ADMIN_TAGS = 'tags';
    static SITE_URL_ADMIN_SCORING = 'scoring';
    static SITE_URL_ADMIN_DASHBOARDS = 'dashboard';
    static SITE_URL_ADMIN_GROUPS = 'groups';
    static SITE_URL_ADMIN_RESPONSIBILITIES = 'responsibilities';
    static SITE_URL_ADMIN_RESOURCES = 'resources';
    static SITE_URL_ADMIN_SETTINGS = 'settings';
    static SITE_URL_ADMIN_WORKFLOW = 'workflow';
    static SITE_URL_ADMIN_ISSUE_TYPES = 'issuetypes';
    static SITE_URL_ADMIN_ORGANIZATIONS = 'organizations';
    static SITE_URL_ADMIN_PREDICATES = 'predicates';
    static SITE_URL_ADMIN_EXPORT_TEMPLATES = 'exporttemplates';
    static SITE_URL_ADMIN_DIAGRAM_ASSETS = 'diagrams';



    static getDefaultRoute() {
        if (ResourceHomePage != null && ResourceHomePage != "" && ResourceHomePage != '/') {
            return ResourceHomePage;
        }
        else if (CompanySettings != null && CompanySettings.DefaultRoute != null && CompanySettings.DefaultRoute != '' && CompanySettings.DefaultRoute != '/') {
            return CompanySettings.DefaultRoute;
        } else {
            return this.SITE_URL_HOME_ROOT;
        }
    }

    static getUrl(objectType: string, objectId: number, parentId: number, areaName: string, uid: string) {
        if (objectType.toLowerCase() == "referenceitemtype") {
            return "/reference;referenceListId=" + objectId;
        }
        if (objectType.toLowerCase() == "artifacttype" && areaName == 'Business Assets') {
            return `admin/assets/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_BUSINESS}`;
        }
        if (objectType.toLowerCase() == "artifacttype" && areaName == 'Technical Assets') {
            return `admin/assets/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`;
        }
        if (objectType.toLowerCase() == "taxonomytype") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_MODELS}`;
        }
        if (objectType.toLowerCase() == "policytype") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_POLICIES}`;
        }
        if (objectType.toLowerCase() == "intersecttype") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS}`;
        }
        if (objectType.toLowerCase() == "issuetype") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_ISSUE_TYPES}`;
        }
        if (objectType.toLowerCase() == "responsibilitytype") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES}`;
        }
        if (objectType.toLowerCase() == "report") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS}`;
        }
        if (objectType.toLowerCase() == "tag" && uid && uid != '00000000-0000-0000-0000-000000000000') {
            return `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${uid}`;
        }
        if (objectType.toLowerCase() == "tag" && !objectId) {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_TAGS}`;
        }
        if (objectType.toLowerCase() == "ruletype") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_RULES}`;
        }
        if (objectType.toLowerCase() === "resourcetype") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES}`;
        }
        if (objectType.toLowerCase() === "grouptype") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_GROUPS}`;
        }
        if (objectType.toLowerCase() === "metricallocation") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_SCORING}`;
        }
        if (objectType.toLowerCase() === "predicate") {
            return `admin/${SiteUrlHelpers.SITE_URL_ADMIN_PREDICATES}`;
        }
        return SiteUrlHelpers.getObjectUrl(objectType, objectId, parentId);
    }

    // getObjectUrl - Generates the url for an object based on its type
    static getObjectUrl(objectType: string, objectId: number, parentId?: number, objectName?: string): string {
        switch (objectType.toUpperCase()) {
            case 'ARTIFACTTYPE':
                return `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${objectId}`;
            case 'ARTIFACT':
                return `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${parentId}/${objectId}`;
            case 'COMMENTS':
                return `${SiteUrlHelpers.SITE_URL_COMMENTS_ROOT}/${objectId}/${objectName}`;
            case 'GROUP':
                return `${SiteUrlHelpers.SITE_URL_GROUP_ROOT}/${objectId}`;
            case 'RESOURCE':
                return `${SiteUrlHelpers.SITE_URL_RESOURCE_ROOT}/${objectId}`;
            case 'TAXONOMY':
                return `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${parentId};hierarchyId=${objectId}`;
            case 'TAXONOMYTYPE':
                return `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${objectId}/structure`;
            case 'POLICYTYPE':
                return `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${objectId}/structure`;
            case 'POLICY':
                return `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${parentId};hierarchyId=${objectId}`;
            case 'RULE':
                return `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${parentId}/${objectId}`;
            case 'DASHBOARD':
                return `${SiteUrlHelpers.SITE_URL_DASHBOARD_ROOT}/${objectId}`;
            case 'TAG':
                return `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${objectId}`;
            case 'TASKTYPE':
                return `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_DIAGRAM_ASSETS}`;
            default:
                console.log('Unable to generate object link', objectType, objectId);
        }
    }

    // getAssetUrl - Generates the url for an object based on its type
    static getAssetUrl(uid: string): string {
        return `${SiteUrlHelpers.SITE_URL_ASSET_ROOT}/${uid}`;
    }

    // getAssetTypeUrl - Generates the url for an object based on its type
    static getAssetTypeUrl(type: string, uid: string): string {
        let uri: string = "";

        switch (type.toUpperCase()) {
            case 'TAXONOMY':
            case 'TAXONOMYTYPE':
                uri = `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/structure/${uid}`;
                break;
            case 'POLICY':
            case 'POLICYTYPE':
                uri = `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/structure/${uid}`;
                break;
            default:
                uri = `${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}/${uid}`;
                break;
        }

        return uri;
    }

    // convertClassicUrl - Converts a url from the legacy site to the new url used in angular
    // inputs - url the old url
    // output - the converted url
    // CURRENT USES mainly used by search as elastic search stores the url of the results but doesnt store the parent type
    // of objects making it not posible to get the object url by building it
    static convertClassicUrl(url): string {
        if (url.startsWith('#/artifacts'))
            return url.replace('#/artifacts', SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT);
        else if (url.startsWith('#/resources'))
            return url.replace('#/resources', SiteUrlHelpers.SITE_URL_RESOURCE_ROOT);
        else if (url.startsWith('#/groups'))
            return url.replace('#/groups', SiteUrlHelpers.SITE_URL_GROUP_ROOT);
        else if (url.startsWith('#/catalogs')) {
            var parts = url.split('/');
            if (parts.length == 4) {
                return `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${parts[2]};hierarchyId=${parts[3]}`;
            }
            else if (parts.length == 3) {
                return `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${parts[2]}/structure`;
            }
            console.log('[ERROR] - INVALID FORMAT FOR MODEL URL', url);
        }
        else if (url.startsWith('#/domains')) {
            console.log('[ERROR] - DOMAIN TYPE NOT SUPPORTED BY NEW UI');
            return url;
        }
        else {
            if (url.startsWith('#'))
                console.log('[ERROR] - CANNOT CONVERT CLASSIC URL TO NEW URL', url);

            return url;
        }
    }
}
