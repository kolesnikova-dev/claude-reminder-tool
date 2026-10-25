This project is to create a reminder with Claude via tool use.

The language of choice is C#, this is for learning purposes. I am most comfortable with TypeScript, and I believe it provides me with a good launchpad to pick up C#. 

## Current state of things
![alt text](image.png)

## The loop
```text
User: "Remind me 3 days after April 3, 2025 to call the dentist"
  │
  ▼
[you] client.messages.create(model, messages, tools=ToolSchemas.All)
  │
  ▼
Claude sees:  tools = [ add_duration_to_datetime {input_schema...}, set_reminder {...} ]
  │
  ▼
Claude returns:  stop_reason = "tool_use"
  content = [
    { type: "tool_use", name: "add_duration_to_datetime",
      input: { datetime_str: "2025-04-03", duration: 3, unit: "days" },
      id: "toolu_01..." }
  ]
  │
  ▼
[you] look up name → call Tools.AddDurationToDatetime("2025-04-03", 3, "days")
  → DurationResult.Ok(2025-04-06)
  │
  ▼
[you] send back:
  messages += { role: "user", content: [
      { type: "tool_result", tool_use_id: "toolu_01...",
        content: "New datetime: Sunday, April 06, 2025 12:00:00 AM" }
  ]}
  │
  ▼
Claude now knows the answer, emits second tool_use: set_reminder(content, timestamp)
  ... loop again ...
  │
  ▼
Claude emits final text: "I've set a reminder for April 6."
```

## Add a new tool

A tool lives in four places. Each step below must use the same tool name and the same argument keys, or Claude's call will fall through to `Unknown tool`.

1. **Name it** in `ToolNames` (`ChatService/Tools.cs`). Add a `const string` in `snake_case`, e.g. `public const string DeleteReminder = "delete_reminder";`. It must be a `const` because it is used as a `case` label.
2. **Implement it** in the `Tools` class (`ChatService/Tools.cs`) as a `static` method. Return a `string` (or a result type you convert to a string). Throwing is fine: `ExecuteTool` catches exceptions and reports them to Claude as an error result.
3. **Describe it** by adding a `new Tool { ... }` to `ToolSchemas.All` (`ChatService/ToolSchemas.cs`):
   - `Name = ToolNames.YourTool`
   - `Description`: what it does and when Claude should use it. This is what Claude reads to decide whether to call it.
   - `InputSchema.Properties`: one entry per argument, with a `type` and a `description`.
   - `Required`: only the arguments Claude must always send (an empty list is fine).
4. **Dispatch it** by adding a `case ToolNames.YourTool:` to `ExecuteTool` (`ChatService/ChatService.cs`). Read the arguments with `GetString` / `GetDouble` using the exact property names from step 3, call your `Tools` method, and return `(content, isError)`.
5. **Try it**: run the app and ask for something that should trigger the tool. Check that Claude calls it, that the arguments arrive as expected, and that the final answer uses the result.

Things worth knowing:
- Serialization is handled by the SDK. Tool input arrives already parsed, and the `content` string you return is wrapped in a `tool_result` block for you. Only call `JsonSerializer.Serialize` if you want to return structured data as a string.
- Missing optional arguments come back as `null` (strings) or the fallback (numbers), so give them defaults in the `Tools` method.
- Return plain, readable text. Claude uses it to write the final reply.
- If Claude never calls the tool, improve the `Description` first.