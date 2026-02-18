# Examples

This directory contains example projects demonstrating the usage of bielu.tdsharp.aspnetcore.logger.

## Example.LoggerDemo

A console application that demonstrates:
- Using ILoggerFactory with TDLib
- Adding TDLib logger to factory for routing .NET logs through TDLib
- Log level conversion between TDLib and Microsoft.Extensions.Logging

### Running the example

```bash
cd src/examples/Example.LoggerDemo
dotnet run
```

Note: The example shows API usage patterns without requiring TDLib native binaries. To use TdClient in a real application, you need to install the `tdlib.native` package.
