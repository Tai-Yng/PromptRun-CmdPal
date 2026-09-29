// Copyright (c) Tai-Yng. MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PromptRun.Library;

namespace PromptRun.CmdPal;

/// <summary>Create-or-edit form for a prompt (title / content / tags).</summary>
internal sealed partial class PromptFormPage : ContentPage
{
    private readonly PromptLibrary _library;
    private readonly PromptEntry? _editing;
    private readonly Action? _onChanged;

    public PromptFormPage(PromptLibrary library)
    {
        _library = library;
        Title = "New prompt";
        Name = "Open";
        Icon = new IconInfo("\uE710"); // Add glyph
    }

    public PromptFormPage(PromptLibrary library, PromptEntry editing, Action onChanged)
    {
        _library = library;
        _editing = editing;
        _onChanged = onChanged;
        Title = "Edit prompt";
        Name = "Open";
        Icon = new IconInfo("\uE70F"); // Edit glyph
    }

    public override IContent[] GetContent() => [new PromptForm(_library, _editing, _onChanged)];
}

internal sealed partial class PromptForm : FormContent
{
    private readonly PromptLibrary _library;
    private readonly PromptEntry? _editing;
    private readonly Action? _onChanged;

    public PromptForm(PromptLibrary library, PromptEntry? editing, Action? onChanged)
    {
        _library = library;
        _editing = editing;
        _onChanged = onChanged;

        // Values are injected as JSON string literals — no templating, no injection surprises.
        var titleJson = JsonSerializer.Serialize(editing?.Title ?? string.Empty);
        var contentJson = JsonSerializer.Serialize(editing?.Content ?? string.Empty);
        var tagsJson = JsonSerializer.Serialize(editing is null ? string.Empty : string.Join(", ", editing.Tags));

        TemplateJson =
            "{\n" +
            "  \"type\": \"AdaptiveCard\",\n" +
            "  \"version\": \"1.6\",\n" +
            "  \"body\": [\n" +
            "    {\"type\": \"Input.Text\", \"id\": \"Title\", \"label\": \"Title\", \"isRequired\": true, \"errorMessage\": \"Title is required\", \"value\": " + titleJson + "},\n" +
            "    {\"type\": \"Input.Text\", \"id\": \"Content\", \"label\": \"Content\", \"isMultiline\": true, \"isRequired\": true, \"errorMessage\": \"Content is required\", \"value\": " + contentJson + ", \"placeholder\": \"The prompt text...\"},\n" +
            "    {\"type\": \"Input.Text\", \"id\": \"Tags\", \"label\": \"Tags (comma separated, optional)\", \"value\": " + tagsJson + ", \"placeholder\": \"code, writing, daily\"}\n" +
            "  ],\n" +
            "  \"actions\": [\n" +
            "    {\"type\": \"Action.Submit\", \"title\": \"Save\"}\n" +
            "  ]\n" +
            "}";
    }

    public override CommandResult SubmitForm(string payload)
    {
        var form = JsonNode.Parse(payload)?.AsObject();
        var title = (form?["Title"]?.ToString() ?? string.Empty).Trim();
        var content = form?["Content"]?.ToString() ?? string.Empty;
        var tagsRaw = form?["Tags"]?.ToString() ?? string.Empty;

        if (title.Length == 0 || content.Trim().Length == 0)
        {
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = "Title and content are required",
                Result = CommandResult.KeepOpen(),
            });
        }

        var tags = tagsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        CommandResult result;
        if (_editing is null)
        {
            var added = _library.Add(new PromptEntry { Title = title, Content = content, Tags = tags });
            result = CommandResult.ShowToast(new ToastArgs
            {
                Message = added ? $"Saved \"{title}\" to your library" : $"\"{title}\" already exists in your library",
                Result = CommandResult.GoHome(),
            });
        }
        else
        {
            var updated = _library.Update(_editing.Id, title, content, tags);
            result = CommandResult.ShowToast(new ToastArgs
            {
                Message = updated ? $"Updated \"{title}\"" : "This prompt no longer exists",
                Result = CommandResult.GoHome(),
            });
        }

        _onChanged?.Invoke();
        return result;
    }
}
