using System;
using System.Collections.Generic;
using System.Text;

namespace Api.Core.Helpers;

/// <summary>
/// Builder for character seperated value strings
/// </summary>
public class CsvBuilder
{
    private readonly char _separator;
    private StringBuilder _csvBuilder = new();
    
    /// <summary>
    /// Creates a new instance of <see cref="CsvBuilder"/>.
    /// </summary>
    /// <param name="separator">The character the value is seperated by</param>
    public CsvBuilder(char separator)
    {
        _separator = separator;
    }

    /// <summary>
    /// Adds a new row of character seperated values
    /// </summary>
    /// <param name="values">The values if the row</param>
    /// <param name="getValue">Delegate used to get the correct string</param>
    /// <typeparam name="T">The type of the enumerable</typeparam>
    public void AddRow<T>(IEnumerable<T> values, Func<T, string> getValue)
    {
        var isFirstColumn = true;
        foreach (var field in values)
        {
            if (!isFirstColumn)
            {
                _csvBuilder.Append(_separator);
            }

            AppendCsvValue(getValue(field));
            isFirstColumn = false;
        }
        
        _csvBuilder.AppendLine();
    }

    private void AppendCsvValue(string value)
    {
        var valueContainsSeparator = value.Contains(_separator);
        if (valueContainsSeparator)
        {
            _csvBuilder.Append('\"');
        }

        _csvBuilder.Append(value);
        if (valueContainsSeparator)
        {
            _csvBuilder.Append('\"');
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _csvBuilder.ToString();
    }
}