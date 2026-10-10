if(args.Contains("--manual-backup-only"))
{
    await ManualBackupSmoke.RunAsync();
    return;
}
await ExportWorkflowSmoke.RunAsync();
await DatabaseBackupSmoke.RunAsync();
