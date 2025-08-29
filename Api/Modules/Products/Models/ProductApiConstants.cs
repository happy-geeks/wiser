namespace Api.Modules.Products.Models;

/// <summary>
/// Class for product api Constants
/// </summary>
public class ProductsServiceConstants
{

    /// <summary>
    /// The property name used for the products api being enabled.
    /// </summary>
    public const string PropertyProductsApiEnabled = "wiser_product_api_enabled";

    /// <summary>
    /// The property name used for datasource type on a product.
    /// </summary>
    public const string ProductPropertyDatasourceType = "wiser_product_api_datasource_type";

    /// <summary>
    /// The property name used for static output id on a product.
    /// </summary>
    public const string ProductPropertyStatic = "wiser_product_api_static";

    /// <summary>
    /// The property name used for query id on a product.
    /// </summary>
    public const string ProductPropertyQueryId = "wiser_product_api_query_id";

    /// <summary>
    /// The property name used for styledoutput id on a product.
    /// </summary>
    public const string ProductPropertyStyledOutputId = "wiser_product_api_styledoutput_id";

    /// <summary>
    /// The property name used for the 'select products' id in global settings.
    /// </summary>
    public const string PropertySelectProductsKey = "product_ids_query";

    /// <summary>
    /// The property name used for the 'select products' id in global settings.
    /// </summary>
    public const string PropertyEntityName = "product_entity_name";

    /// <summary>
    /// The property name used for the 'default per-product data source type setting' stored in global settings.
    /// </summary>
    public const string PropertyDatasourceType = "datasource_type";

    /// <summary>
    /// The property name used for the 'default per-product static setting' stored in global settings.
    /// </summary>
    public const string PropertyStatic = "datasource_static";

    /// <summary>
    /// The property name used for the 'default per-product query id setting' stored in global settings.
    /// </summary>
    public const string PropertyQueryId = "datasource_query";

    /// <summary>
    /// The property name used for the 'default per-product styled output id setting' stored in global settings.
    /// </summary>
    public const string PropertyStyledOutputId = "datasource_styledoutput";

    /// <summary>
    /// The property name used for the minimal cooldown time in global settings.
    /// </summary>
    public const string PropertyMinimalRefreshCoolDown = "minimal_refresh_cooldown";

    /// <summary>
    /// The name used for the settings entity.
    /// </summary>
    public const string SettingsEntityName = "ProductsApiSettings";
}