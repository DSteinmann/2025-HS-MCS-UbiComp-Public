using System;
using System.Threading.Tasks;
using SolidInteractionLibrary;

namespace SolidActivityReader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: SolidActivityReader <serverUrl> <webId> <email> <password>");
                Console.WriteLine("Example: SolidActivityReader https://wiser-solid-xi.interactions.ics.unisg.ch/ https://wiser-solid-xi.interactions.ics.unisg.ch/dominik-ubicomp2025/profile/card#me user@example.com password");
                return;
            }

            string serverUrl = args[0];
            string webId = args[1];
            string email = args[2];
            string password = args[3];

            try
            {
                Console.WriteLine("Authenticating with Solid pod...");
                var solidClient = await AuthenticatedPodClient.BuildAsync(serverUrl, webId, email, password);
                Console.WriteLine("Authentication successful!");

                // Read the current activity
                string activityUrl = $"{webId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/currentActivity.ttl";
                Console.WriteLine($"Reading activity from: {activityUrl}");

                string ttlContent = await solidClient.GetFileAsync(activityUrl);
                Console.WriteLine("Raw TTL content:");
                Console.WriteLine(ttlContent);
                Console.WriteLine();

                // Parse the activity information
                var activityInfo = ParseActivityInfo(ttlContent);
                if (activityInfo != null)
                {
                    Console.WriteLine("=== PARSED ACTIVITY INFORMATION ===");
                    Console.WriteLine($"Person Name: {activityInfo.PersonName}");
                    Console.WriteLine($"Activity: {activityInfo.ActivityName}");
                    Console.WriteLine($"Probability: {activityInfo.Probability:P2}");
                    Console.WriteLine($"End Time: {activityInfo.EndTime:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    Console.WriteLine("Failed to parse activity information.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        public class ActivityInfo
        {
            public string PersonName { get; set; }
            public string ActivityName { get; set; }
            public float Probability { get; set; }
            public DateTime EndTime { get; set; }
        }

        static ActivityInfo ParseActivityInfo(string ttlContent)
        {
            try
            {
                var info = new ActivityInfo();

                // Parse person name (foaf:name)
                var nameMatch = System.Text.RegularExpressions.Regex.Match(ttlContent, @"foaf:name ""([^""]+)""");
                if (nameMatch.Success)
                {
                    info.PersonName = nameMatch.Groups[1].Value;
                }

                // Parse probability (bm:probability)
                var probMatch = System.Text.RegularExpressions.Regex.Match(ttlContent, @"bm:probability ""([^""]+)""");
                if (probMatch.Success)
                {
                    if (float.TryParse(probMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float prob))
                    {
                        info.Probability = prob;
                    }
                }

                // Parse end time (prov:endedAtTime)
                var timeMatch = System.Text.RegularExpressions.Regex.Match(ttlContent, @"prov:endedAtTime ""([^""]+)""");
                if (timeMatch.Success)
                {
                    if (DateTime.TryParse(timeMatch.Groups[1].Value, out DateTime endTime))
                    {
                        info.EndTime = endTime;
                    }
                }

                // Parse activity name (schema:name)
                var activityMatch = System.Text.RegularExpressions.Regex.Match(ttlContent, @"schema:name ""([^""]+) action""");
                if (activityMatch.Success)
                {
                    info.ActivityName = activityMatch.Groups[1].Value;
                }

                return info;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to parse activity TTL: {e.Message}");
                return null;
            }
        }
    }
}