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

    export function getObjectLinkByObjectTypeAndId(objectType, objectId, parentId) {
        switch (objectType.toUpperCase()) {
            case 'ARTIFACT':
                return `${SITE_URL_ARTIFACT_ROOT}/${parentId}/${objectId}`;                
            default:
                console.log('Unable to generate object link', objectType, objectId);
        }
    }


    // this is used by search
    export function getObjectLinkFromOldUrl(type, url) {
        console.log("convert", type, url);
        switch (type.toUpperCase()) {
            case 'ARTIFACT':
                return url.replace('#/artifacts', SITE_URL_ARTIFACT_ROOT);
            case 'USERS':
                return url.replace('#/resources', SITE_URL_RESOURCE_ROOT);
            default:
                console.log('unable to update url');
                return url.replace('#', '/a');                
        }
    }
    
}