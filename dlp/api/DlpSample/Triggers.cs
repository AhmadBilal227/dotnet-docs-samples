﻿// Copyright 2018 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using CommandLine;
using Google.Cloud.Dlp.V2;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Google.Cloud.Dlp.V2.InspectConfig.Types;
using static Google.Cloud.Dlp.V2.JobTrigger.Types;
using static Google.Cloud.Dlp.V2.CloudStorageOptions.Types;

namespace GoogleCloudSamples
{
    [Verb("createJobTrigger", HelpText = "Create a Data Loss Prevention API job trigger.")]
    class CreateJobTriggerOptions
    {
        [Value(0, HelpText = "The project ID to run the API call under.", Required = true)]
        public string ProjectId { get; set; }

        [Value(1, HelpText = "The name of the bucket to scan.", Required = true)]
        public string BucketName { get; set; }

        [Value(2, HelpText = "The minimum likelihood required before returning a match.", Required = true)]
        public string MinLikelihood { get; set; }

        [Value(3, HelpText = "The maximum number of findings to report per request. (0 = server maximum)", Default = 0)]
        public int MaxFindings { get; set; }

        [Value(4, HelpText = "How often to wait between scans, in days. (minimum = 1 day)", Required = true)]
        public int ScanPeriod { get; set; }

        [Value(5, HelpText = "The name of the trigger to be created.", Default = "")]
        public string TriggerId { get; set; }

        [Value(6, HelpText = "A display name for the trigger to be created.", Default = "")]
        public string DisplayName { get; set; }

        [Value(7, HelpText = "A description for the trigger to be created.", Default = "")]
        public string Description { get; set; }
    }

    [Verb("listJobTriggers", HelpText = "List Data Loss Prevention API triggers.")]
    class ListJobTriggersOptions
    {
        [Value(0, HelpText = "The project ID to run the API call under.", Required = true)]
        public string ProjectId { get; set; }
    }

    [Verb("deleteJobTrigger", HelpText = "Delete a Data Loss Prevention API job trigger.")]
    class DeleteJobTriggerOptions
    {
        [Value(0, HelpText = "The full name of the trigger to be deleted.", Required = true)]
        public string TriggerName { get; set; }
    }

    public partial class Dlp
    {
        static object CreateJobTrigger(CreateJobTriggerOptions opts)
        {
            DlpServiceClient dlp = DlpServiceClient.Create();

            // ----- Construct job config -----
            // Specify finding limits
            FindingLimits limits = new FindingLimits
            {
                MaxFindingsPerRequest = opts.MaxFindings
            };

            // Create the inspectConfig object
            InspectConfig inspectConfig = new InspectConfig {
                MinLikelihood = (Likelihood) Enum.Parse(typeof(Likelihood), opts.MinLikelihood),
                Limits = limits
            };
            inspectConfig.InfoTypes.Add(new InfoType
            {
                Name = "PERSON_NAME"
            });
            inspectConfig.InfoTypes.Add(new InfoType
            {
                Name = "US_STATE"
            });

            // Create trigger
            Trigger trigger = new Trigger {
                Schedule = new Schedule
                {
                    RecurrencePeriodDuration = new Google.Protobuf.WellKnownTypes.Duration {
                        Seconds = opts.ScanPeriod * 60 * 60 * 24
                    }
                }
            };

            StorageConfig storageConfig = new StorageConfig {
                CloudStorageOptions = new CloudStorageOptions
                {
                    FileSet = new FileSet
                    {
                        Url = $"gs://{opts.BucketName}/*"
                    }
                }
            };

            InspectJobConfig jobConfig = new InspectJobConfig {
                InspectConfig = inspectConfig,
                StorageConfig = storageConfig
            };

            JobTrigger jobTrigger = new JobTrigger {
                Triggers = { trigger },
                InspectJob = jobConfig,
                Status = Status.Healthy,
                DisplayName = opts.DisplayName,
                Description = opts.Description
            };

            JobTrigger response = dlp.CreateJobTrigger(new CreateJobTriggerRequest
            {
                Parent = $"projects/{opts.ProjectId}",
                JobTrigger = jobTrigger,
                TriggerId = opts.TriggerId
            });

            Console.WriteLine($"Successfully created trigger {response.Name}");
            return 0;
        }

        static object ListJobTriggers(ListJobTriggersOptions opts) {
            DlpServiceClient dlp = DlpServiceClient.Create();

            var response = dlp.ListJobTriggers(new ListJobTriggersRequest
            {
                Parent = $"projects/{opts.ProjectId}"
            });

            foreach (var trigger in response) {
                Console.WriteLine($"Name: {trigger.Name}");
                Console.WriteLine($"  Created: {trigger.CreateTime.ToString()}");
                Console.WriteLine($"  Updated: {trigger.UpdateTime.ToString()}");
                Console.WriteLine($"  Display Name: {trigger.DisplayName}");
                Console.WriteLine($"  Description: {trigger.Description}");
                Console.WriteLine($"  Status: {trigger.Status}");
                Console.WriteLine($"  Error count: {trigger.Errors.Count}");
            }

            return 0;
        }

        static object DeleteJobTrigger(DeleteJobTriggerOptions opts)
        {
            DlpServiceClient dlp = DlpServiceClient.Create();

            dlp.DeleteJobTrigger(new DeleteJobTriggerRequest
            {
                Name = opts.TriggerName
            });

            Console.WriteLine($"Successfully deleted trigger {opts.TriggerName}.");
            return 0;
        }
    }
}