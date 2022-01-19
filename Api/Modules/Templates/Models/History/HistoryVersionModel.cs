using System;
using System.Collections.Generic;

namespace Api.Modules.Templates.Models.History
{
    public class HistoryVersionModel
    {
        int version;
        DateTime changedOn;
        string changedBy;

        List<DynamicContentChangeModel> changes;

        string rawVersionString;

        string component;
        string componentMode;

        public HistoryVersionModel(int version, DateTime changedOn, string changedBy, string component, string componentMode, string rawVersionString)
        {
            this.version = version;
            this.changedOn = changedOn;
            this.changedBy = changedBy;
            this.component = component;
            this.componentMode = componentMode;
            this.rawVersionString = rawVersionString;
            changes = null;
        }

        /// <summary>
        /// Get the version number.
        /// </summary>
        /// <returns>The version as a number.</returns>
        public int GetVersion()
        {
            return version;
        }

        /// <summary>
        /// Get the Change date.
        /// </summary>
        /// <returns>DateTime of the date when the version was saved.</returns>
        public DateTime GetChangedOn()
        {
            return changedOn;
        }

        /// <summary>
        /// Get the change date in a displayable format(DD-MM-YYYY om HH:MM:SS).
        /// </summary>
        /// <returns>A string containing a displayable date.</returns>
        public string GetDisplayChangedOn()
        {
            return changedOn.ToShortDateString() + " om " + changedOn.ToLongTimeString();
        }

        /// <summary>
        /// Get the Name of the user that made the change.
        /// </summary>
        /// <returns>String containing the username.</returns>
        public string GetChangedBy()
        {
            return changedBy;
        }

        /// <summary>
        /// Get the list of changes compared to the previous version. This does not generated the changelist, but retrieves it from the model if set.
        /// </summary>
        /// <returns>List containing the changes made.</returns>
        public List<DynamicContentChangeModel> GetChanges()
        {
            return changes;
        }
        /// <summary>
        /// Set the list of changes compared to the previous version.
        /// </summary>
        /// <param name="changes">A List of DynamicContentChangeModel containing the changes to the previous version.</param>
        public void SetChanges(List<DynamicContentChangeModel> changes)
        {
            this.changes = changes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetRawString()
        {
            return rawVersionString;
        }

        public string GetComponent()
        {
            return component;
        }

        public string GetComponentMode()
        {
            return componentMode;
        }

    }
}
