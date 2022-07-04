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
export const FORGOT_PASSWORD = "forgotPassword";
export const RESET_PASSWORD_SUCCESS = "resetPasswordSuccess";
export const RESET_PASSWORD_ERROR = "resetPasswordError";
export const CHANGE_PASSWORD_LOGIN = "changePasswordLogin";
export const CHANGE_PASSWORD_SUCCESS = "changePasswordSuccess";
export const CHANGE_PASSWORD_ERROR = "changePasswordError";

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

// Customers module.
export const GET_CUSTOMER_TITLE = "getCustomerTitle";
export const VALID_SUB_DOMAIN = "validSubDomain";

// Users module.
export const CHANGE_PASSWORD = "changePassword";

// Branches module.
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