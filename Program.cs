using CG.Web.MegaApiClient;
using GtaVersionSelector;
using SharpCompress.Archives;
using SharpCompress.Common;

Dictionary<DeliveryPlatformEnum, string> _downloadLinks = new()
{
    { DeliveryPlatformEnum.RockstarLauncher, "https://mega.nz/file/ygVhXabJ#YrqiD4zkY0pc45fJE0Wjb5a3BS28TXwWcJUAQpoUBks" },
    { DeliveryPlatformEnum.Steam, "https://mega.nz/file/fwdDEKST#xmXqqH0eMZM_47H35VLfxYQp6BRxqivA0tpelwAvXI8" },
    { DeliveryPlatformEnum.EpicGames, "https://mega.nz/file/eoVlWC4a#coHKMSWeWl74CbmPCSU8yh4umdMO0K99HgJBTpVJPjk" }
};

List<string> _filesToBackup = new()
{
    "GTA5.exe",
    "GTAVLanguageSelect.exe",
    "GTAVLauncher.exe",
    "PlayGTAV.exe",
    @"update\update.rpf"
};

const string _logo = @"          _____                _____                    _____                    _____          
         /\    \              /\    \                  /\    \                  /\    \         
        /::\    \            /::\    \                /::\    \                /::\    \        
       /::::\    \           \:::\    \              /::::\    \              /::::\    \       
      /::::::\    \           \:::\    \            /::::::\    \            /::::::\    \      
     /:::/\:::\    \           \:::\    \          /:::/\:::\    \          /:::/\:::\    \     
    /:::/  \:::\    \           \:::\    \        /:::/ __\:::\    \        /:::/ __\:::\    \    
   /:::/    \:::\    \          /::::\    \      /::::\   \:::\    \      /::::\   \:::\    \   
  /:::/    / \:::\    \        /::::::\    \    /::::::\   \:::\    \    /::::::\   \:::\    \  
 /:::/    /   \:::\ ___\      /:::/\:::\    \  /:::/\:::\   \:::\____\  /:::/\:::\   \:::\____\ 
/:::/ ____ / ___\:::|    |    /:::/  \:::\____\/:::/  \:::\   \:::|    |/:::/  \:::\   \:::|    |
\:::\    \ /\  /:::|____|   /:::/    \::/    /\::/   |::::\  /:::|____|\::/    \:::\  /:::|____|
 \:::\    /::\ \::/    /   /:::/    / \/____/  \/____|:::::\/:::/    /  \/_____/\:::\/:::/    / 
  \:::\   \:::\ \/____/   /:::/    /                 |:::::::::/    /            \::::::/    /  
   \:::\   \:::\____\    /:::/    /                  |::|\::::/    /              \::::/    /   
    \:::\  /:::/    /    \::/    /                   |::| \::/____/                \::/____/    
     \:::\/:::/    /      \/____/                    |::|  ~|                       ~~          
      \::::::/    /                                  |::|   |                                   
       \::::/    /                                   \::|   |                                   
        \::/____/                                     \:|   |                                   
                                                       \|___|                                   
Made with love by Arochka for https://gtrp.co";

Console.Clear();

Console.WriteLine(_logo);
Console.WriteLine(@"Where's the GTA V folder ?(C:\Program Files\Rockstar Games\Grand Theft Auto V)");

var gameFolder = Console.ReadLine();

if (!Directory.Exists(gameFolder))
{
    Console.Error.WriteLine("Invalid game folder");
    return;
}

Console.WriteLine("What's your platform:");
Console.WriteLine("0) Rockstar Launcher");
Console.WriteLine("1) Steam");
Console.WriteLine("2) Epic Games");
Console.Write("\r\nSelect an option: ");

if (!Enum.TryParse(typeof(DeliveryPlatformEnum), Console.ReadLine(), out var platform)
    || platform is not DeliveryPlatformEnum deliveryPlatform
    || !Enum.IsDefined(typeof(DeliveryPlatformEnum), deliveryPlatform))
{
    Console.Error.WriteLine("Unknown platform");
    return;
}

Console.WriteLine("Downgrade / Restore:");
Console.WriteLine("1) Downgrade");
Console.WriteLine("2) Restore");
Console.Write("\r\nSelect an option: ");

var choice = Console.ReadLine();

if (choice == "1")
{
    var backupDir = new DirectoryInfo(Path.Combine(gameFolder, "backup"));

    if (!backupDir.Exists)
    {
        Console.WriteLine("Backup files...");

        backupDir.Create();

        foreach (var fileToBackup in _filesToBackup)
        {
            var dirName = Path.GetDirectoryName(Path.Combine(gameFolder, "backup", fileToBackup));
            if (string.IsNullOrEmpty(dirName))
                continue;

            Directory.CreateDirectory(dirName);
            File.Copy(Path.Combine(gameFolder, fileToBackup), Path.Combine(gameFolder, "backup", fileToBackup), true);
        }
    }

    if (!_downloadLinks.TryGetValue(deliveryPlatform, out var downloadLink))
    {
        Console.Error.WriteLine($"No download link for platform: {Enum.GetName(typeof(DeliveryPlatformEnum), deliveryPlatform)}");
        return;
    }

    if (deliveryPlatform == DeliveryPlatformEnum.RockstarLauncher)
    {
        var launcherIndexFile = new FileInfo(Path.Combine(gameFolder, "index.bin"));

        if (launcherIndexFile.Exists)
            launcherIndexFile.Delete();

        Console.WriteLine("You must have Rockstar Launcher running and ready to play before continue !");
        Console.ReadKey();
    }

    MegaApiClient megaClient = new();
    await megaClient.LoginAnonymousAsync();
    var node = await megaClient.GetNodeFromLinkAsync(new Uri(downloadLink));

    var file = new FileInfo(node.Name);

    if (!file.Exists || file.Length != node.Size)
    {
        if (file.Exists)
            File.Delete(node.Name);

        Console.WriteLine("Start downloading version...");

        var progress = new ProgressBar();
        var progressHandler = new Progress<double>(x => progress.Report(x / 100));
        await megaClient.DownloadFileAsync(node, node.Name, progressHandler);
        progress.Dispose();
    }

    await megaClient.LogoutAsync();

    Console.WriteLine("Extract version...");

    using var archive = ArchiveFactory.Open(node.Name);
    foreach (var entry in archive.Entries.Where(x => !x.IsDirectory))
    {
        var extractPath = gameFolder;

        if (entry.Key.EndsWith(".rpf"))
            extractPath = Path.Combine(gameFolder, "update");
        else if (entry.Key.EndsWith(".exe"))
            extractPath = gameFolder;
        else
            continue;

        entry.WriteToDirectory(extractPath, new ExtractionOptions
        {
            Overwrite = true
        });
    }
}
else if(choice == "2")
{
    var backupDir = new DirectoryInfo(Path.Combine(gameFolder, "backup"));

    if (!backupDir.Exists)
    {
        Console.WriteLine("Backup directory doesn't exist.");
        return;
    }

    foreach (var fileToBackup in _filesToBackup)
    {
        var file = new FileInfo(Path.Combine(gameFolder, "backup", fileToBackup));

        if (!file.Exists)
            continue;

        file.CopyTo(Path.Combine(gameFolder, fileToBackup), true);
    }

    backupDir.Delete(true);
}

Console.WriteLine("Done. Press a key to exit...");
Console.ReadKey();