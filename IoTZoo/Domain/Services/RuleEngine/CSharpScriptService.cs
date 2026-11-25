// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Interfaces.MQTT;
using Domain.Interfaces.RuleEngine;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Domain.Services.RuleEngine;

public class CSharpScriptService : IScriptService
{
    protected ILogger<CSharpScriptService> Logger { get; }
    protected IScriptCrudService ScriptCrudService { get; }
    private readonly IProjectCrudService projectService;
    private readonly IDataTransferService dataTransferService;
    private readonly IMqttPublisher mqttPublisher;

    public CSharpScriptService(ILogger<CSharpScriptService> logger, IScriptCrudService scriptCrudService,
                               IProjectCrudService projectService, IDataTransferService dataTransferService,
                               IMqttPublisher mqttPublisher)
    {
        Logger = logger;
        ScriptCrudService = scriptCrudService;
        this.projectService = projectService;
        this.dataTransferService = dataTransferService;
        this.mqttPublisher = mqttPublisher;
    }

    public async Task<string> ProcessScript(string scriptName, string inputData)
    {
        var script = await ScriptCrudService.LoadScript(scriptName);
        if (script == null)
        {
            Logger.LogError($"Script {scriptName} not found!");
            return string.Empty;
        }
        string? result = await ProcessScript(script, inputData);
        return result ?? string.Empty;
    }

    public async Task<string> ProcessScript(Pocos.Script script, string inputData)
    {
         string? baseCode = script.SourceCode;

        string sourceCode = string.IsNullOrEmpty(inputData)
           ? baseCode + $"{Environment.NewLine}return {script.ScriptName}();"
           : baseCode + $"{Environment.NewLine}return {script.ScriptName}({inputData});";
        ScriptOptions scriptOptions = GetScriptOptions();

        if (string.IsNullOrEmpty(sourceCode))
        {
            Logger.LogError($"Script {script.ScriptName} has no code!");
        }

        object? result;
        try
        {
            result = await CSharpScript.EvaluateAsync(sourceCode, scriptOptions);
        }
        catch (Exception ex)
        {
            await PublishExceptionAsync(script, ex);
            throw;
        }

        if (null == result)
        {
            return string.Empty;
        }
        return result.ToString()!;
    }

    public async Task<string?> EvaluateScript(Pocos.Script script)
    {
        ScriptOptions scriptOptions = GetScriptOptions();
        try
        {
            await CSharpScript.EvaluateAsync(script.SourceCode, scriptOptions);
        }
        catch (CompilationErrorException compilationErrorException)
        {
            return compilationErrorException.GetBaseException().Message;
        }
        catch (Exception ex)
        {
            await PublishExceptionAsync(script, ex);
            return ex.GetBaseException().Message;
        }

        return null; // no error!
    }

    private async Task PublishExceptionAsync(Pocos.Script script, Exception exception)
    {
        Logger.LogError(exception, "Script {ScriptName} failed", script.ScriptName);

        try
        {
            var projects = await projectService.LoadProjects();
            if (projects.Count == 0)
            {
                Logger.LogWarning("Cannot publish script exception: no projects are configured");
                return;
            }

            await mqttPublisher.Initialization;
            foreach (var project in projects)
            {
                string topic = $"{dataTransferService.NamespaceName}/{project.ProjectName}/Exception";
                try
                {
                    var result = await mqttPublisher.MqttClient.PublishAsync(topic, exception.ToString());
                    if (!result.IsSuccess)
                    {
                        Logger.LogError("Could not publish script exception to {Topic}: {Reason}", topic, result.ReasonCode);
                    }
                }
                catch (Exception publishException)
                {
                    Logger.LogError(publishException, "Could not publish script exception to {Topic}", topic);
                }
            }
        }
        catch (Exception notificationException)
        {
            Logger.LogError(notificationException, "Could not publish exception for script {ScriptName}", script.ScriptName);
        }
    }

    private ScriptOptions GetScriptOptions()
    {
        ScriptOptions scriptOptions = ScriptOptions.Default.AddReferences(typeof(JsonSerializer).Assembly).
                                                                          AddReferences(typeof(MudBlazor.Color).Assembly).
                                                                          AddImports("System.Text.Json").
                                                                          AddImports("MudBlazor");
        return scriptOptions;
    }
}
