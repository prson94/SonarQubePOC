export module SiteUrlHelpers {
    //prefix route for all routes
    // THIS SETTING NEEDS TO BE IN SYNC WITH THE SETTING IN D360.WEB / STARTUP.CS SO THE APPROPRIATE HTML PAGE IS INITIALLY SERVED
    export var SITE_URL_PREFIX = 'a';


    //main site routes
    // WARNING!! - SOME URLS SUCH AS TOOLTIPS ARE BURNED IN THE DB DO NOT CHANGES THE BELOW WITHOUT 
    // UPDATING BOTH!!
    export var SITE_URL_FUSION_ROOT = `${SITE_URL_PREFIX}/fusion`;
    export var SITE_URL_REFERENCE_ROOT = `${SITE_URL_PREFIX}/reference`;
    export var SITE_URL_ARTIFACT_ROOT = `${SITE_URL_PREFIX}/artifact`;
    export var SITE_URL_COMMUNITY_ROOT = `${SITE_URL_PREFIX}/community`;
    export var SITE_URL_MONITOR_ROOT = `${SITE_URL_PREFIX}/monitor`;
    export var SITE_URL_POLICY_ROOT = `${SITE_URL_PREFIX}/policy`;
    export var SITE_URL_GROUP_ROOT = `${SITE_URL_PREFIX}/group`;
    export var SITE_URL_RESOURCE_ROOT = `${SITE_URL_PREFIX}/resource`;
    export var SITE_URL_RULE_ROOT = `${SITE_URL_PREFIX}/quality/rule`;
    export var SITE_URL_SEARCH_ROOT = `${SITE_URL_PREFIX}/search`;
    export var SITE_URL_WORKFLOW_ROOT = `${SITE_URL_PREFIX}/workflow`;
    export var SITE_URL_MODEL_ROOT = `${SITE_URL_PREFIX}/model`;
    export var SITE_URL_ADMIN_ROOT = `${SITE_URL_PREFIX}/admin`;
    export var SITE_URL_HOME_ROOT = `${SITE_URL_PREFIX}/home`;

    //model child routes
    export var SITE_URL_MODEL_CLASSIFICATION = 'classification';

    //workflow child routes
    export var SITE_URL_WORKFLOW_RAISE_ISSUE = 'raiseissue';
    export var SITE_URL_WORKFLOW_VIEW_ISSUE = 'work/issue';

    //fusion child routes
    export var SITE_URL_FUSION_BY_FUSIONATTRIBUTEID = 'fusionattribute'
    export var SITE_URL_FUSION_LIST = '';

    //admin child routes
    export var SITE_URL_ADMIN_BULK_LOAD = `load`;
    export var SITE_URL_ADMIN_FUSION = `fusion`;
    export var SITE_URL_ADMIN_ATTRIBUTES = `attributes`;
    export var SITE_URL_ADMIN_ARTIFACTS = `artifacts`;
    export var SITE_URL_ADMIN_LOOKUPS = 'lookups';
    export var SITE_URL_ADMIN_MODELS = 'taxonomies';
    export var SITE_URL_ADMIN_POLICIES = 'policies';
    export var SITE_URL_ADMIN_RELATIONSHIPS = 'relationships';
    export var SITE_URL_ADMIN_RULES = 'rules';
    export var SITE_URL_ADMIN_SURVEYS = 'surveys';
    export var SITE_URL_ADMIN_ANALYTICS = 'analytics';
    export var SITE_URL_ADMIN_DASHBOARDS = 'dashboards';
    export var SITE_URL_ADMIN_GROUPS = 'groups';
    export var SITE_URL_ADMIN_RESPONSIBILITIES = 'responsibilities';
    export var SITE_URL_ADMIN_RESOURCES = 'resources';
    export var SITE_URL_ADMIN_SETTINGS = 'settings';
    export var SITE_URL_ADMIN_TEMPLATES = 'templates';
    export var SITE_URL_ADMIN_WORKFLOW = 'workflow';
    export var SITE_URL_ADMIN_DOMAIN = 'domain';

    export function getObjectUrl(objectType, objectId, objectName, parentId) {
        switch (objectType.toUpperCase()) {
            case 'ARTIFACT':
                return `${SITE_URL_ARTIFACT_ROOT}/${parentId}/${objectId}`;                
            default:
                console.log('Unable to generate object link', objectType, objectId);
        }
    }
        
    // convertClassicUrl - Converts a url from the legacy site to the new url used in angular
    // inputs - url the old url
    // output - the converted url
    // CURRENT USES mainly used by search as elastic search stores the url of the results but doesnt store the parent type
    // of objects making it not posible to get the object url by building it
    export function convertClassicUrl(url) {
        console.log("convert", url);
        if (url.startsWith('#/artifacts'))
            return url.replace('#/artifacts', SITE_URL_ARTIFACT_ROOT);
        else if (url.startsWith('#/resources'))
            return url.replace('#/resources', SITE_URL_RESOURCE_ROOT);
        else if (url.startsWith('#/groups'))
            return url.replace('#/groups', SITE_URL_GROUP_ROOT);
        else if (url.startsWith('#/fusion/item')) {
            var parts = url.split('/');
            if (parts.length == 5) {
                return `${SITE_URL_FUSION_ROOT}/${SITE_URL_FUSION_BY_FUSIONATTRIBUTEID}/${parts[3]}/${parts[4]}`;
            }
            console.log('[ERROR] - INVALID FORMAT FOR FUSION ATTRIBUTE URL', url);
        }
        else if (url.startsWith('#/fusion/')) {
            var parts = url.split('/');
            if (parts.length == 4) {
                return `${SITE_URL_FUSION_ROOT}/${SITE_URL_FUSION_LIST}${parts[3]}`;
            }
            console.log('[ERROR] - INVALID FORMAT FOR FUSION TYPE URL', url);
        }
        else if (url.startsWith('#/catalogs')) {
            var parts = url.split('/');
            if (parts.length == 4) {
                return `${SITE_URL_MODEL_ROOT}/${parts[2]};hierarchyId=${parts[3]}`;
            }
            console.log('[ERROR] - INVALID FORMAT FOR MODEL URL', url);
        }
        else if (url.startsWith('#/domains')) {
            console.log('[ERROR] - DOMAIN TYPE NOT SUPPORTED BY NEW UI');
            return url;
        }
        else {
            console.log('[ERROR] - CANNOT CONVERT CLASSIC URL TO NEW URL',url);
            return url;
        }
    }
    
}