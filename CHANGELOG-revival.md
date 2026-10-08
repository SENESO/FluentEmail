# FluentEmail revival — change log

Local modernization of lukencode/FluentEmail (MIT). No behavior rewrites; minimal reviewable changes only.

## Target frameworks (.NET 8 support — issue #380)
- `src/FluentEmail.Core/FluentEmail.Core.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `src/Renderers/FluentEmail.Liquid/FluentEmail.Liquid.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `src/Renderers/FluentEmail.Razor/FluentEmail.Razor.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `src/Senders/FluentEmail.Graph/FluentEmail.Graph.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `src/Senders/FluentEmail.MailKit/FluentEmail.MailKit.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `src/Senders/FluentEmail.Mailgun/FluentEmail.Mailgun.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `src/Senders/FluentEmail.Mailtrap/FluentEmail.Mailtrap.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `src/Senders/FluentEmail.SendGrid/FluentEmail.SendGrid.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `src/Senders/FluentEmail.Smtp/FluentEmail.Smtp.csproj` — `netstandard2.0` → `netstandard2.0;net8.0`
- `test/FluentEmail.Core.Tests/FluentEmail.Core.Tests.csproj` — `netcoreapp3.1` (EOL) → `net8.0`
- `test/FluentEmail.Liquid.Tests/FluentEmail.Liquid.Tests.csproj` — `netcoreapp3.1` (EOL) → `net8.0`
- `test/FluentEmail.Razor.Tests/FluentEmail.Razor.Tests.csproj` — `netcoreapp3.1` (EOL) → `net8.0`

## Dependency updates
- `src/Senders/FluentEmail.MailKit/FluentEmail.MailKit.csproj` — MailKit `2.10.1` → `4.18.1`. Removes the vulnerable Portable.BouncyCastle 1.8.5 transitive dependency (issue #292) and fixes `MissingMethodException` conflicts when consumers use a newer MailKit than the library (issues #296, #326).
- `src/Senders/FluentEmail.SendGrid/FluentEmail.SendGrid.csproj` — SendGrid `9.26.0` → `9.29.3`; added `Microsoft.CSharp` `4.7.0` (required for `dynamic` use in `SendGridSender` on the `netstandard2.0` target — CS0656 without it).
- `src/Senders/FluentEmail.Mailgun/FluentEmail.Mailgun.csproj` — Newtonsoft.Json `12.0.3` → `13.0.3`.
- `src/FluentEmail.Core/FluentEmail.Core.csproj` — Microsoft.Extensions.DependencyInjection.Abstractions `5.0.0` → `8.0.0`.
- `src/Renderers/FluentEmail.Liquid/FluentEmail.Liquid.csproj` — Microsoft.Extensions.Options `5.0.0` → `8.0.0`.
- `test/Directory.Build.props` + `test/FluentEmail.Core.Tests/FluentEmail.Core.Tests.csproj` — Microsoft.NET.Test.Sdk `16.6.1/16.8.3` → `17.8.0`, NUnit `3.12.0/3.13.0` → `3.14.0`, NUnit3TestAdapter `3.16.1/3.17.0` → `4.5.0`; fixed duplicate PackageReference (Include → Update) in Core.Tests.

## Bug fixes
- `src/FluentEmail.Core/Email.cs` — `GetRenderer()` helper: throws `InvalidOperationException` with a clear message when no template renderer is configured, instead of `NullReferenceException` (issue #367). Applied at all 6 `UsingTemplate*` call sites.
- `src/FluentEmail.Core/Email.cs` — `AttachFromFilename` now tracks the `FileStream` it opens; `Send`/`SendAsync` dispose those streams in a `finally` block so attachment files are no longer locked after sending (issue #269). Streams supplied by the caller via `Attach()` are left alone.

## Deliberately left alone
- RazorLight `2.0.0-rc.3`, Fluid.Core `2.0.13`, Microsoft.Graph `3.x` — updating these risks breaking API changes; nothing verified broken.
- Issue #190 (SaveToDiskSender path) — already fixed upstream (commit 39b04c1).
- Issue #378 (`.To()` appends) — long-standing by-design fluent behavior; changing it would break existing users.
