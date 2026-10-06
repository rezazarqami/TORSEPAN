if (!args.Contains("--ui-only")) await ApiSmoke.RunAsync();
await UiSmoke.RunAsync();
Console.WriteLine("Workshop messaging smoke passed.");
