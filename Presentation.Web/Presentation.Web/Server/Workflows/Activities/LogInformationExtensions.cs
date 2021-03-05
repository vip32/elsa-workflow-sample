using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Presentation.Web.Server
{
    public static partial class BuilderExtensions
    {
        public static IActivityBuilder LogInformation(this IBuilder builder, Action<ISetupActivity<LogInformation>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder LogInformation(this IBuilder builder, Func<ActivityExecutionContext, string> text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.LogInformation(activity => activity.WithText(text), lineNumber, sourceFile);

        public static IActivityBuilder LogInformation(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.LogInformation(activity => activity.WithText(text!), lineNumber, sourceFile);

        public static IActivityBuilder LogInformation(this IBuilder builder, Func<string> text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.LogInformation(activity => activity.WithText(text!), lineNumber, sourceFile);

        public static IActivityBuilder LogInformation(this IBuilder builder, Func<ValueTask<string>> text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.LogInformation(activity => activity.WithText(text!), lineNumber, sourceFile);

        public static IActivityBuilder LogInformation(this IBuilder builder, string text, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.LogInformation(activity => activity.WithText(text!), lineNumber, sourceFile);
    }

    public static class LogInformationExtensions
    {
        public static ISetupActivity<LogInformation> WithText(this ISetupActivity<LogInformation> writeLine, Func<ActivityExecutionContext, ValueTask<string?>> message) => writeLine.Set(x => x.Message, message);

        public static ISetupActivity<LogInformation> WithText(this ISetupActivity<LogInformation> writeLine, Func<ActivityExecutionContext, string> message) => writeLine.Set(x => x.Message, message);

        public static ISetupActivity<LogInformation> WithText(this ISetupActivity<LogInformation> writeLine, Func<ValueTask<string?>> message) => writeLine.Set(x => x.Message, message);

        public static ISetupActivity<LogInformation> WithText(this ISetupActivity<LogInformation> writeLine, Func<string?> message) => writeLine.Set(x => x.Message, message);

        public static ISetupActivity<LogInformation> WithText(this ISetupActivity<LogInformation> writeLine, string? message) => writeLine.Set(x => x.Message, message);
    }
}