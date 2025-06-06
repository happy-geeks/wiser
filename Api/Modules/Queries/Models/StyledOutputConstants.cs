namespace Api.Modules.Queries.Models;

/// <summary>
/// Class for styled output Constants
/// </summary>
public class StyledOutputConstants
{
    /// <summary>
    /// The default starting keyword for 'styledoutput properties'
    /// </summary>
    public const string DefaultStartKeyWord = "{StyledOutput";

    /// <summary>
    /// The default ending keyword for 'styledoutput properties'
    /// </summary>
    public const string DefaultEndKeyWord = "}";

    /// <summary>
    /// buildIn specs for 'SingleDetail'
    /// </summary>
    public static readonly StyledOutputBuiltIn SingleDetail = new StyledOutputBuiltIn {
        Key = "StyledOutputSingleDetail",
        Query = @"
SELECT
    ?styled_name AS `result_name`,
    IFNULL(detail.`value`, '') AS `result_value`
FROM wiser_itemdetail detail
WHERE detail.item_id = ?styled_id AND detail.`key` = ?styled_key LIMIT 1;
",
        UnitLayout = @"
""{result_name}"": ""{result_value}""
"
    };

    /// <summary>
    /// buildIn specs for 'SingleDetailArrayElm'
    /// </summary>
    public static readonly StyledOutputBuiltIn SingleDetailArrayElm = new StyledOutputBuiltIn {
        Key = "StyledOutputSingleDetailArrayElm",
        Query = @"
SELECT
    ?styled_name AS `result_name`,
    IFNULL(detail.`value`, '') AS `result_value`
FROM wiser_itemdetail detail
WHERE detail.item_id = ?styled_id AND detail.`key` = ?styled_key LIMIT 1;
",
        UnitLayout = @"{
""key"": ""{result_name}"", 
""value"":""{result_value}""
}"
    };

    /// <summary>
    /// buildIn specs for 'MultiDetail'
    /// </summary>
    public static readonly StyledOutputBuiltIn MultiDetail = new StyledOutputBuiltIn {
        Key = "StyledOutputMultiDetail",
        Query = @"
SELECT
    ?styled_name AS `result_name`,
    IFNULL(detail.`value`, '') AS `result_value`
FROM wiser_itemdetail detail
WHERE detail.item_id = ?styled_id AND detail.`key` = ?styled_key;
",
        UnitLayout = @"
{""value"": ""{result_value}""}
",
        BeginLayout = @"
""{result_name}"": {
    ""values"": [",
        EndLayout = @" 
    ] 
}"
    };

    /// <summary>
    /// buildIn specs for 'MultiDetail'
    /// </summary>
    public static readonly StyledOutputBuiltIn MultiDetailArrayElm = new StyledOutputBuiltIn {
        Key = "StyledOutputMultiDetailArrayElm",
        Query = @"
SELECT
    ?styled_name AS `result_name`,
    IFNULL(detail.`value`, '') AS `result_value`
FROM wiser_itemdetail detail
WHERE detail.item_id = ?styled_id AND detail.`key` = ?styled_key;
",
        UnitLayout = @"
{""value"": ""{result_value}""}
",
        BeginLayout = @"
""key"": ""{result_name}"", 
""value"":"": {
    ""values"": [",
        EndLayout = @" 
    ] 
}"
    };

    /// <summary>
    /// buildIn specs for 'LanguageDetail'
    /// Note: This version assumes nl-vr-fr, in a future version we might want to make this more dynamic.
    /// </summary>
    public static readonly StyledOutputBuiltIn LanguageDetail = new StyledOutputBuiltIn {
        Key = "StyledOutputLanguageDetail",
        Query = @"
SELECT
    ?styled_name AS `result_name`,
    IFNULL(detail_nl.`value`, '') AS `result_value_nl`,
    IFNULL(detail_vl.`value`, '') AS `result_value_vl`,
    IFNULL(detail_fr.`value`, '') AS `result_value_fr`
FROM wiser_itemdetail detail
LEFT JOIN wiser_itemdetail detail_nl ON detail_nl.item_id = detail.item_id AND detail_nl.key = ?styled_key AND detail_nl.language_code = 'nl'
LEFT JOIN wiser_itemdetail detail_vl ON detail_vl.item_id = detail.item_id AND detail_vl.key = ?styled_key AND detail_vl.language_code = 'vl'
LEFT JOIN wiser_itemdetail detail_fr ON detail_fr.item_id = detail.item_id AND detail_fr.key = ?styled_key AND detail_fr.language_code = 'fr'
WHERE detail.item_id = ?styled_id AND detail.key = ?styled_key LIMIT 1;
",
        UnitLayout = @"
  ""{result_name}"": {
    ""localisedValue"": {
      ""nl"": ""{result_value_nl}"",
      ""vl"": ""{result_value_vl}"",
      ""fr"": ""{result_value_fr}""
    }
  }
"
    };

    /// <summary>
    /// buildIn specs for 'LanguageDetail'
    /// Note: This version assumes nl-vr-fr, in a future version we might want to make this more dynamic.
    /// </summary>
    public static readonly StyledOutputBuiltIn LanguageDetailArrayElm = new StyledOutputBuiltIn {
        Key = "StyledOutputLanguageDetail",
        Query = @"
SELECT
    ?styled_name AS `result_name`,
    IFNULL(detail_nl.`value`, '') AS `result_value_nl`,
    IFNULL(detail_vl.`value`, '') AS `result_value_vl`,
    IFNULL(detail_fr.`value`, '') AS `result_value_fr`
FROM wiser_itemdetail detail
LEFT JOIN wiser_itemdetail detail_nl ON detail_nl.item_id = detail.item_id AND detail_nl.key = ?styled_key AND detail_nl.language_code = 'nl'
LEFT JOIN wiser_itemdetail detail_vl ON detail_vl.item_id = detail.item_id AND detail_vl.key = ?styled_key AND detail_vl.language_code = 'vl'
LEFT JOIN wiser_itemdetail detail_fr ON detail_fr.item_id = detail.item_id AND detail_fr.key = ?styled_key AND detail_fr.language_code = 'fr'
WHERE detail.item_id = ?styled_id AND detail.key = ?styled_key LIMIT 1;
",
        UnitLayout = @"{
""key"": ""{result_name}"", 
""value"": {
    ""localisedValue"": {
      ""nl"": ""{result_value_nl}"",
      ""vl"": ""{result_value_vl}"",
      ""fr"": ""{result_value_fr}""
    }
  }
}
"
    };

    /// <summary>
    /// buildIn specs for 'Singlelinked'
    /// </summary>
    public static readonly StyledOutputBuiltIn Singlelinked = new StyledOutputBuiltIn {
        Key = "StyledOutputSingleLinked",
        Query = @"
SELECT
  ?styled_name AS `result_name`,
  value_item.title AS `value`
FROM wiser_itemlink `link`
JOIN wiser_item value_item ON value_item.id = `link`.item_id 
WHERE link.destination_item_id = ?styled_id AND link.type = ?styled_key
",
        UnitLayout = @"
""{result_name}"": ""{result_value}""
"
    };

    /// <summary>
    /// buildIn specs for 'SinglelinkedArrayElm'
    /// </summary>
    public static readonly StyledOutputBuiltIn SinglelinkedArrayElm = new StyledOutputBuiltIn {
        Key = "StyledOutputSingleLinkedArrayElm",
        Query = @"
SELECT
  ?styled_name AS `result_name`,
  value_item.title AS `value`
FROM wiser_itemlink `link`
JOIN wiser_item value_item ON value_item.id = `link`.item_id 
WHERE link.destination_item_id = ?styled_id AND link.type = ?styled_key
",
        UnitLayout = @"{
""key"": ""{result_name}"", 
""value"":""{result_value}""
}"
    };
}