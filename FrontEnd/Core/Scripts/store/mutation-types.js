// Base module.
export const START_REQUEST = "startRequest";
export const END_REQUEST = "endRequest";

// Login module.
export const AUTH_REQUEST = "authenticationRequest";
export const AUTH_SUCCESS = "authenticationSuccess";
export const AUTH_ERROR = "authenticationError";
export const AUTH_LOGOUT = "authenticationLogout";
export const AUTH_LIST = "authenticationUsersList";
export const AUTH_DATA = "authenticationUserData";
export const AUTH_TOTP_SETUP = "authenticationTotpSetup"
export const AUTH_TOTP_PIN = "authenticationTotpPin"
export const FORGOT_PASSWORD = "forgotPassword";
export const RESET_PASSWORD_SUCCESS = "resetPasswordSuccess";
export const RESET_PASSWORD_ERROR = "resetPasswordError";
export const USE_TOTP_BACKUP_CODE = "useTotpBackupCode";
export const USE_TOTP_BACKUP_CODE_ERROR = "useTotpBackupCodeError";
export const USER_BACKUP_CODES_GENERATED = "userBackupCodesGenerated";

// Modules module.
export const MODULES_REQUEST = "modulesRequest";
export const MODULES_LOADED = "modulesLoaded";
export const OPEN_MODULE = "openModule";
export const CLOSE_MODULE = "closeModule";
export const CLOSE_ALL_MODULES = "closeAllModules";
export const ACTIVATE_MODULE = "activateModule";
export const TOGGLE_PIN_MODULE = "togglePinModule";

// Items module.
export const LOAD_ENTITY_TYPES_OF_ITEM_ID = "loadEntityTypesOfItemId";

// Tenants module.
export const GET_TENANT_TITLE = "getTenantTitle";
export const VALID_SUB_DOMAIN = "validSubDomain";

// Users module.
export const CHANGE_PASSWORD = "changePassword";
export const CHANGE_PASSWORD_LOGIN = "changePasswordLogin";
export const CHANGE_PASSWORD_SUCCESS = "changePasswordSuccess";
export const CHANGE_PASSWORD_ERROR = "changePasswordError";
export const START_UPDATE_TIME_ACTIVE_TIMER = "startUpdateTimeActiveTimer";
export const STOP_UPDATE_TIME_ACTIVE_TIMER = "stopUpdateTimeActiveTimer";
export const SET_ACTIVE_TIMER_INTERVAL = "setActiveTimerInterval";
export const CLEAR_ACTIVE_TIMER_INTERVAL = "clearActiveTimerInterval";
export const UPDATE_ACTIVE_TIME = "updateActiveTime";
export const GENERATE_TOTP_BACKUP_CODES = "generateTotpBackupCodes";
export const GENERATE_TOTP_BACKUP_CODES_SUCCESS = "generateTotpBackupCodesSuccess";
export const GENERATE_TOTP_BACKUP_CODES_ERROR = "generateTotpBackupCodesError";
export const CLEAR_LOCAL_TOTP_BACKUP_CODES = "clearLocalTotpBackupCodes";

// Branches module.
export const BRANCH_CHANGE_COMPLETED = "branchChangeCompleted";
export const CREATE_BRANCH = "createBranch";
export const CREATE_BRANCH_SUCCESS = "createBranchSuccess";
export const CREATE_BRANCH_ERROR = "createBranchError";
export const GET_BRANCHES = "getBranches";
export const MERGE_BRANCH = "mergeBranch";
export const MERGE_BRANCH_SUCCESS = "mergeBranchSuccess";
export const MERGE_BRANCH_ERROR = "mergeBranchError";
export const GET_ENTITIES_FOR_BRANCHES = "getEntitiesForBranches";
export const IS_MAIN_BRANCH = "isMainBranch";
export const GET_BRANCH_CHANGES = "getBranchChanges"
export const RESET_BRANCH_CHANGES = "resetBranchChanges";
export const HANDLE_CONFLICT = "handleConflict";
export const HANDLE_MULTIPLE_CONFLICTS = "handleMultipleConflicts";
export const DELETE_BRANCH = "deleteBranch";
export const DELETE_BRANCH_SUCCESS = "deleteBranchSuccess";
export const DELETE_BRANCH_ERROR = "deleteBranchError";
export const GET_LINK_TYPES = "getLinkTypes";

export const GET_DATA_SELECTORS_FOR_BRANCHES = "getDataSelectorsForBranches";

// Cache module.
export const CLEAR_CACHE = "clearCache";
export const CLEAR_CACHE_SUCCESS = "clearCacheSuccess";
export const CLEAR_CACHE_ERROR = "clearCacheError";

// Database modules.
export const DO_TENANT_MIGRATIONS = "doTenantMigrations";