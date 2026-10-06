if(args.Contains("--orders-only")) await OrdersSmoke.RunAsync(); else await OrderCompositionSmoke.RunAsync();
