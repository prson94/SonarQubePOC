export class SiteUrlHelpers {
    //prefix route for all routes
    // THIS SETTING NEEDS TO BE IN SYNC WITH THE SETTING IN D360.WEB / STARTUP.CS SO THE APPROPRIATE HTML PAGE IS INITIALLY SERVED
    static SITE_URL_PREFIX = '';// a/


    //main site routes
    // WARNING!! - SOME URLS SUCH AS TOOLTIPS ARE BURNED IN THE DB DO NOT CHANGES THE BELOW WITHOUT 
    // UPDATING BOTH!!
    static SITE_URL_FUSION_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}fusion`;
    static SITE_URL_REFERENCE_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}reference`;
    static SITE_URL_ARTIFACT_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}artifact`;
    static SITE_URL_COMMUNITY_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}community`;
    static SITE_URL_HELP_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}help`;
    static SITE_URL_MONITOR_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}monitor`;
    static SITE_URL_POLICY_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}policy`;
    static SITE_URL_GROUP_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}group`;
    static SITE_URL_RESOURCE_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}resource`;
    static SITE_URL_RULE_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}quality/rule`;
    static SITE_URL_SEARCH_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}search`;
    static SITE_URL_WORKFLOW_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}workflow`;
    static SITE_URL_MODEL_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}model`;
    static SITE_URL_ADMIN_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}admin`;
    static SITE_URL_HOME_ROOT = `${SiteUrlHelpers.SITE_URL_PREFIX}home`;

    //model child routes
    static SITE_URL_MODEL_CLASSIFICATION = 'classification';

    //policy child routes 
    static SITE_URL_POLICY_CLASSIFICATION = 'classification';

    //workflow child routes
    static SITE_URL_WORKFLOW_RAISE_ISSUE = 'raiseissue';
    static SITE_URL_WORKFLOW_VIEW_ITEM = 'work';    
    static SITE_URL_WORKFLOW_VIEW_STATUS = 'status';
    
    //fusion child routes
    static SITE_URL_FUSION_BY_FUSIONATTRIBUTEID = 'fusionattribute'
    static SITE_URL_FUSION_LIST = '';

    //admin child routes
    static SITE_URL_ADMIN_BULK_LOAD = `load`;
    static SITE_URL_ADMIN_FUSION = `fusion`;
    static SITE_URL_ADMIN_ATTRIBUTES = `attributes`;
    static SITE_URL_ADMIN_ARTIFACTS = `artifacts`;
    static SITE_URL_ADMIN_LOOKUPS = 'lookups';
    static SITE_URL_ADMIN_MODELS = 'taxonomies';
    static SITE_URL_ADMIN_POLICIES = 'policies';
    static SITE_URL_ADMIN_RELATIONSHIPS = 'relationships';
    static SITE_URL_ADMIN_RULES = 'rules';
    static SITE_URL_ADMIN_SURVEYS = 'surveys';
    static SITE_URL_ADMIN_ANALYTICS = 'analytics';
    static SITE_URL_ADMIN_DASHBOARDS = 'dashboards';
    static SITE_URL_ADMIN_GROUPS = 'groups';
    static SITE_URL_ADMIN_RESPONSIBILITIES = 'responsibilities';
    static SITE_URL_ADMIN_RESOURCES = 'resources';
    static SITE_URL_ADMIN_SETTINGS = 'settings';
    static SITE_URL_ADMIN_TEMPLATES = 'templates';
    static SITE_URL_ADMIN_WORKFLOW = 'workflow';
    static SITE_URL_ADMIN_DOMAIN = 'domain';

    // getObjectUrl - Generates the url for an object based on its type
    static getObjectUrl(objectType: string, objectId: number, parentId?: number, objectName?: string) : string {
        switch (objectType.toUpperCase()) {
            case 'ARTIFACTTYPE':
                return `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${objectId}`;
            case 'ARTIFACT':
                return `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${parentId}/${objectId}`;
            case 'FUSIONTYPE':
                return `${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${objectId}`;
            case 'FUSIONATTRIBUTE':
                return `${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${SiteUrlHelpers.SITE_URL_FUSION_BY_FUSIONATTRIBUTEID}/${parentId}/${objectId}`;
            case 'GROUP':
                return `${SiteUrlHelpers.SITE_URL_GROUP_ROOT}/${objectId}`;
            case 'RESOURCE':
                return `${SiteUrlHelpers.SITE_URL_RESOURCE_ROOT}/${objectId}`;
            case 'TAXONOMY':
                return `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${parentId};hierarchyId=${objectId}`;
            case 'TAXONOMYTYPE':
                return `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${objectId}/structure`;
            case 'TAXONOMYTYPECLASS':
                return `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/classification/${objectName}`;                
            case 'POLICYTYPECLASS':                
                return `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/classification/${objectId}`;                
            case 'POLICYTYPE':
                return `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${objectId}/structure`;                
            case 'POLICY':
                return `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${parentId};hierarchyId=${objectId}`;
            case 'RULE':
                return `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${objectId}`;
            default:
                console.log('Unable to generate object link', objectType, objectId);
        }
    }

    // convertClassicUrl - Converts a url from the legacy site to the new url used in angular
    // inputs - url the old url
    // output - the converted url
    // CURRENT USES mainly used by search as elastic search stores the url of the results but doesnt store the parent type
    // of objects making it not posible to get the object url by building it
    static convertClassicUrl(url) : string {
        console.log("convert", url);
        if (url.startsWith('#/artifacts'))
            return url.replace('#/artifacts', SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT);
        else if (url.startsWith('#/resources'))
            return url.replace('#/resources', SiteUrlHelpers.SITE_URL_RESOURCE_ROOT);
        else if (url.startsWith('#/groups'))
            return url.replace('#/groups', SiteUrlHelpers.SITE_URL_GROUP_ROOT);
        else if (url.startsWith('#/fusion/item')) {
            var parts = url.split('/');
            if (parts.length == 5) {
                return `${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${SiteUrlHelpers.SITE_URL_FUSION_BY_FUSIONATTRIBUTEID}/${parts[3]}/${parts[4]}`;
            }
            console.log('[ERROR] - INVALID FORMAT FOR FUSION ATTRIBUTE URL', url);
        }
        else if (url.startsWith('#/fusion/')) {
            var parts = url.split('/');
            if (parts.length == 4) {
                return `${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${SiteUrlHelpers.SITE_URL_FUSION_LIST}${parts[3]}`;
            }
            console.log('[ERROR] - INVALID FORMAT FOR FUSION TYPE URL', url);
        }
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
            console.log('[ERROR] - CANNOT CONVERT CLASSIC URL TO NEW URL', url);
            return url;
        }
    }

    // returns the font awesome icon for the associated url
    static getObjectIcon(objectType: string) {
        switch (objectType.toUpperCase()) {
            case 'ARTIFACTTYPE':
            case 'ARTIFACT':
                return 'book';
            case 'FUSIONTYPE':
            case 'GROUP':
            case 'COMMUNITY':
                return 'users';
            case 'RESOURCE':
                return 'user';
            case 'TAXONOMY':
            case 'TAXONOMYTYPE':
            case 'TAXONOMYTYPECLASS':
            case 'MODEL':
                return 'sitemap';
            case 'POLICY':
                return 'university';
            case 'RULE':
                return 'pie-chart';
            case 'MONITOR':
                return 'tachometer';
            case 'REFERENCE':
                return 'cubes';
            case 'FUSION':
                return 'database';
            default:
                return 'question';
        }
    }

}
