using System;
using System.Collections.Generic;
using System.Linq;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.Templates.Helpers
{
    public class PublishedEnvironmentHelper
    {
        /// <summary>
        /// Creates a PublishedEnvironmentModel for a given item. To creates this a dictionary containing the versions and their respective publishvalue is used. This model will contain the current live, acceptence and test environment as well as a list of versions that are available.
        /// </summary>
        /// <param name="versionsAndPublished">A dictionary containing the available versions and their respective publishvalue</param>
        /// <returns>A PublishedEnvironmentModel that will contain the current live, acceptence and test environment as well as a list of versions that are available</returns>
        public static PublishedEnvironmentModel CreatePublishedEnvironmentsFromVersionDictionary(Dictionary<int, int> versionsAndPublished)
        {
            var liveVersion = 0;
            var acceptVersion = 0;
            var testVersion = 0;

            foreach (var versionAndPublish in versionsAndPublished)
            {
                if (versionAndPublish.Value == 0)
                {
                    continue;
                }

                if (((Environments)versionAndPublish.Value).HasFlag(Environments.Live))
                {
                    liveVersion = versionAndPublish.Key;
                }
                if (((Environments)versionAndPublish.Value).HasFlag(Environments.Acceptance))
                {
                    acceptVersion = versionAndPublish.Key;
                }
                if (((Environments)versionAndPublish.Value).HasFlag(Environments.Test))
                {
                    testVersion = versionAndPublish.Key;
                }

            }

            var versionList = new List<int>(versionsAndPublished.Keys);
            versionList = versionList.OrderBy(v => v).ToList();

            return new PublishedEnvironmentModel
            {
                LiveVersion = liveVersion,
                AcceptVersion = acceptVersion,
                TestVersion = testVersion,
                VersionList = versionList
            };
        }

        /// <summary>
        /// This function will match a string to the PublishedEnvironmentsEnum. In case this doesn't match any environment a ArgumentOutOfRangeException is thrown
        /// </summary>
        /// <param name="environment">The string that will be matched against the environments.</param>
        /// <returns></returns>
        public static Environments EnvironmentStringToEnum(string environment)
        {
            switch (environment)
            {
                case "test":
                    return Environments.Test;
                case "accept":
                    return Environments.Acceptance;
                case "live":
                    return Environments.Live;
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment);
            }
        }

        /// <summary>
        /// This function generates a changelog containing the alterations that should be made towards the different versions and their publishenvironment value to achieve the publishing to the given environment. 
        /// This method will also calculate the publishing of underlaying environments if an environment is pushed forward. 
        /// After invoking this method the changelog can be used to update the values in the dataservice
        /// </summary>
        /// <param name="publishModel">A publishmodel containing the current situation of the item.</param>
        /// <param name="version">The version that is to be published to an environment.</param>
        /// <param name="environment">The string of the environment that needs to be published.</param>
        /// <returns>A changelog in the form of a Dictionary containing the versions and their respective value changes to achieve the publishing of the environment given in the params.</returns>
        public static Dictionary<int, int> CalculateEnvironmentsToPublish(PublishedEnvironmentModel publishModel, int version, string environment)
        {
            var environmentEnum = EnvironmentStringToEnum(environment);

            var versionsToUpdate = new Dictionary<int, int>();
            var versionsToPublish = (int)environmentEnum;

            switch (environmentEnum)
            {
                case Environments.Test:
                    // Add this publish.
                    TryAddToIntDictionary(versionsToUpdate, version, ((int)environmentEnum));
                    // Remove the old publish of this environment.
                    TryAddToIntDictionary(versionsToUpdate, publishModel.TestVersion, -(int)Environments.Test);

                    break;
                case Environments.Acceptance:
                    // Check if other environments should also be pushed.
                    if (version > publishModel.TestVersion)
                    {
                        versionsToPublish += (int)Environments.Test;
                        TryAddToIntDictionary(versionsToUpdate, publishModel.TestVersion, -(int)Environments.Test);
                    }

                    // Add this publish 
                    TryAddToIntDictionary(versionsToUpdate, version, versionsToPublish);

                    // Remove the old publish of this environment.
                    TryAddToIntDictionary(versionsToUpdate, publishModel.AcceptVersion, -(int)Environments.Acceptance);

                    break;
                case Environments.Live:
                    // Check if other environments should also be pushed.
                    if (version > publishModel.AcceptVersion)
                    {
                        versionsToPublish += (int)Environments.Acceptance;
                        TryAddToIntDictionary(versionsToUpdate, publishModel.AcceptVersion, -(int)Environments.Acceptance);
                    }
                    if (version > publishModel.TestVersion)
                    {
                        versionsToPublish += (int)Environments.Test;
                        TryAddToIntDictionary(versionsToUpdate, publishModel.TestVersion, -(int)Environments.Test);
                    }

                    // Add this publish.
                    TryAddToIntDictionary(versionsToUpdate, version, versionsToPublish);

                    // Remove the old publish of this environment.
                    TryAddToIntDictionary(versionsToUpdate, publishModel.LiveVersion, -(int)Environments.Live);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(environmentEnum), environmentEnum.ToString());
            }

            return versionsToUpdate;
        }

        /// <summary>
        /// This will add a value to the dictionary by creating a new entry or adding it to the already existing entry under the key.
        /// </summary>
        /// <param name="dictionary">The dictionary to edit.</param>
        /// <param name="key">The key value that will be added or added to.</param>
        /// <param name="value">The value that is to be set or added to the key.</param>
        public static void TryAddToIntDictionary(Dictionary<int, int> dictionary, int key, int value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] += value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Generates a publishlogmodel containing the changes made for saving as a log of the publish event.
        /// </summary>
        /// <param name="templateId">The id of the item that is published.</param>
        /// <param name="currentPublished">A PublishedEnvironmentModel containing the current situation before the publish action is executed.</param>
        /// <param name="publishModel">A changelog in the form of a Dictionary containing the versions and their respective value changes to achieve the publishing of the environment</param>
        /// <returns>A model containing the PublishLogModel to log the event of publishing the environment of the item.</returns>
        public static PublishLogModel GeneratePublishLog(int templateId, PublishedEnvironmentModel currentPublished, Dictionary<int, int> publishModel)
        {
            var publishLog = new PublishLogModel(templateId, currentPublished.LiveVersion, currentPublished.AcceptVersion, currentPublished.TestVersion);

            foreach (var publishAction in publishModel)
            {
                //Negative value means the value is the old environment. These have already been set and need no further action.
                if (publishAction.Value <= 0)
                {
                    continue;
                }

                if (((Environments)publishAction.Value).HasFlag(Environments.Live))
                {
                    publishLog.NewLive = publishAction.Key;
                }
                if (((Environments)publishAction.Value).HasFlag(Environments.Acceptance))
                {
                    publishLog.NewAccept = publishAction.Key;
                }
                if (((Environments)publishAction.Value).HasFlag(Environments.Test))
                {
                    publishLog.NewTest = publishAction.Key;
                }
            }

            return publishLog;
        }
    }
}
