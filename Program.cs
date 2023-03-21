using Google.Apis.Auth.OAuth2;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Admin.Directory.directory_v1.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Newtonsoft.Json;

namespace GoogleSharedDrives
{
    class Program
    {
        static readonly string ApplicationName = "GoogleSharedDrives";

        static async Task Main(string[] args)
        {
            var settings = Settings.LoadSettings();
            
            ServiceAccountCredential credential;
            // Load client secrets.
            using (var stream =
                   new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = ServiceAccountCredential.FromServiceAccountData(stream);
            }

            var cred = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(settings.ServiceAccountId!)
            {
                User = settings.AdminEmail!,
                Scopes = new[] { DriveService.ScopeConstants.Drive },
                Key = credential.Key
            });

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = ApplicationName
            });
            
            string? pageToken = null;

            do
            {
                var request = service.Drives.List();
                request.UseDomainAdminAccess = true;
                request.Fields = "nextPageToken, drives(id, name)";
                request.PageSize = 100;
                if (pageToken is not null)
                {
                    request.PageToken = pageToken;
                }
                var result = await request.ExecuteAsync();
                foreach (var drive in result.Drives)
                {
                    // Console.WriteLine("## {0} ({1})", drive.Name, drive.Id);

                    var req = service.Permissions.List(drive.Id);
                    req.SupportsAllDrives = true;
                    req.UseDomainAdminAccess = true;
                    req.Fields = "*";
                    req.PageSize = 100;

                    
                    var found = false;
                    
                    var res = await req.ExecuteAsync();
                    foreach (var perm in res.Permissions)
                    {
                        if (perm.Type == "user" && perm.Role == "organizer")
                        {
                            Console.WriteLine("{0}\t{1}\t{2}", drive.Name, drive.Id, perm.EmailAddress);
                            found = true;
                        }   
                    }

                    if (!found)
                    {
                        Console.WriteLine("{0}\t{1}\t{2}", drive.Name, drive.Id, "-");
                    }
                }

                pageToken = result.NextPageToken;
            } while (pageToken != null);
        }
    }
}
