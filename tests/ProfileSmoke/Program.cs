if (!args.Contains("--ui-only")) await ApiSmoke.RunAsync();
await UiSmoke.RunAsync();
Console.WriteLine("Personal profile smoke passed.");
