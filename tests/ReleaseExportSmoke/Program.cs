if(args.Contains("--orders-only")) { await OrdersSmoke.RunAsync(); return; }
if(args.Contains("--offline-only")) { await OfflineProductionSmoke.RunAsync(); return; }
if(args.Contains("--manual-backup-only"))
{
    await ManualBackupSmoke.RunAsync();
    return;
}
await ExportWorkflowSmoke.RunAsync();
await DatabaseBackupSmoke.RunAsync();
