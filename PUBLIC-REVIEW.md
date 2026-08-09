# Costing App public review build

This is an interface-only edition intended for inspecting the app without
private business data.

- No first-run database setup is required.
- Database discovery, linking, importing, refreshing and removal are disabled.
- Retained central-data tables and saved app preferences are not read.
- The material store is empty and in memory only, so selectors that depend on
  Copper, Compounds, Masterbatch, Contacts or Operators are intentionally empty.
- Project open, save and revision controls are hidden.
- Every bound text and number input is detached and presented blank. Normal
  working assumptions belong to an installed user's LocalAppData, not this
  public interface shell.
- The A4 quotation button remains usable without linked materials. It creates a
  neutral `Costing App` draft with blank/not-specified commercial fields and no
  organisation name, address, telephone number or internal data.
- The normal installed build and its local retained data are unchanged.
- Debug metadata is disabled across every project so the public binaries do
  not record the development computer's workspace path.

Build it with:

```powershell
dotnet publish src\ATAG.Costing.WinUI\ATAG.Costing.WinUI.csproj -c Release -p:Platform=x64 -p:AtagPublicReview=true -p:PublishTrimmed=false -p:PublishReadyToRun=false -r win-x64 --self-contained true -o output\Costing-App-Public-Review
```

Open the completed build with `Open Costing App Public Review.cmd`.
