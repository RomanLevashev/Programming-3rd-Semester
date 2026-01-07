// <copyright file="Program.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using MyNUnit.Core;

if (args.Length == 0)
{
    Console.WriteLine("Использование: MyNUnit.Cli <путь к папке с тестовыми сборками>");
    return 1;
}

var rootPath = Path.GetFullPath(args[0]);
if (!Directory.Exists(rootPath))
{
    Console.WriteLine($"Путь \"{rootPath}\" не существует");
    return 1;
}

var runner = new TestRunner();
Console.WriteLine($"Запуск тестов в \"{rootPath}\"...");

var stopwatch = Stopwatch.StartNew();
TestRunResult runResult;

try
{
    runResult = await runner.RunPathAsync(rootPath);
}
catch (Exception ex)
{
    Console.WriteLine($"Не удалось выполнить: {ex}");
    return 1;
}

stopwatch.Stop();

var ordered = runResult.Tests
    .OrderBy(t => t.AssemblyName, StringComparer.OrdinalIgnoreCase)
    .ThenBy(t => t.ClassName, StringComparer.OrdinalIgnoreCase)
    .ThenBy(t => t.MethodName, StringComparer.OrdinalIgnoreCase)
    .ToList();

foreach (var test in ordered)
{
    var status = test.Status switch
    {
        TestStatus.Passed => "Успех",
        TestStatus.Failed => "Провал",
        TestStatus.Ignored => "Пропущен",
        _ => test.Status.ToString(),
    };

    var duration = test.Status == TestStatus.Ignored
        ? "-"
        : $"{test.Duration.TotalMilliseconds:F1} мс";

    var details = test.Status switch
    {
        TestStatus.Passed => string.Empty,
        TestStatus.Ignored => $" - причина: {test.Details}",
        TestStatus.Failed => $" - ошибка: {test.Details}",
        _ => string.Empty,
    };

    Console.WriteLine($"{status,-10} {test.AssemblyName}::{test.ClassName}.{test.MethodName} ({duration}){details}");
}

Console.WriteLine();
Console.WriteLine($"Итоги: всего {runResult.Tests.Count}, успешных {runResult.Passed}, упавших {runResult.Failed}, пропущенных {runResult.Ignored}");
Console.WriteLine($"Общее время тестов: {runResult.TotalDuration.TotalMilliseconds:F1} мс, время запуска: {stopwatch.Elapsed.TotalMilliseconds:F1} мс");

return runResult.Failed == 0 ? 0 : 1;
