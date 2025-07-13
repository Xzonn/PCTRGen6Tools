using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PCTRGen6Tools;

internal partial class TextConverter
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    [GeneratedRegex(@"^(?:\[[^\[\]]+\]|\\n|\\r|\\c)+$")]
    public static partial Regex TrashPattern();

    private const ushort KEY_BASE = 0x7C89;
    private const ushort KEY_ADVANCE = 0x2983;
    private const ushort KEY_VARIABLE = 0x0010;
    private const ushort KEY_TERMINATOR = 0x0000;
    private const ushort KEY_TEXTRETURN = 0xBE00;
    private const ushort KEY_TEXTCLEAR = 0xBE01;
    private const ushort KEY_TEXTWAIT = 0xBE02;
    private const ushort KEY_TEXTNULL = 0xBDFF;

    public static string? GetLineString(byte[] data)
    {
        if (data == null)
            return null;

        var s = new StringBuilder();
        int i = 0;
        while (i < data.Length)
        {
            var val = BitConverter.ToUInt16(data, i);
            if (val == KEY_TERMINATOR) break;
            i += 2;

            switch (val)
            {
                case KEY_TERMINATOR: return s.ToString();
                case KEY_VARIABLE:
                    {
                        var count = BitConverter.ToUInt16(data, i); i += 2;
                        var variable = BitConverter.ToUInt16(data, i); i += 2;

                        switch (variable)
                        {
                            case KEY_TEXTRETURN: // "Waitbutton then scroll text; \r"
                                s.Append("\\r");
                                break;
                            case KEY_TEXTCLEAR: // "Waitbutton then clear text;; \c"
                                s.Append("\\c");
                                break;
                            case KEY_TEXTWAIT: // Dramatic pause for a text line. New!
                                var time = BitConverter.ToUInt16(data, i); i += 2;
                                s.Append($"[WAIT {time}]");
                                break;
                            case KEY_TEXTNULL: // Empty Text line? Includes linenum so maybe for betatest finding used-unused lines?
                                return null;
                            default:
                                string varName = variable.ToString("X4");

                                s.Append("[VAR ").Append(varName);
                                if (count > 1)
                                {
                                    s.Append('(');
                                    while (count > 1)
                                    {
                                        ushort arg = BitConverter.ToUInt16(data, i); i += 2;
                                        s.Append(arg.ToString("X4"));
                                        if (--count == 1) break;
                                        s.Append(',');
                                    }
                                    s.Append(')');
                                }
                                s.Append(']');
                                break;
                        }
                    }
                    break;
                case '[': s.Append(@"\["); break;
                case ']': s.Append(@"\]"); break;
                case '\uE09A': s.Append('♪'); break; // Special character for "♪" in the game.
                default: s.Append((char)val); break;
            }
        }
        return s.ToString(); // Shouldn't get hit if the string is properly terminated.
    }

    public static byte[][] ConvertLinesToData(string?[] value)
    {
        ushort key = KEY_BASE;

        // Get Line Data
        byte[][] lineData = new byte[value.Length][];
        for (int i = 0; i < value.Length; i++)
        {
            byte[] DecryptedLineData = GetLineData(value[i]);
            lineData[i] = CryptLineData(DecryptedLineData, key);
            if (lineData[i].Length % 4 == 2)
                Array.Resize(ref lineData[i], lineData[i].Length + 2);
            key += KEY_ADVANCE;
        }

        return lineData;
    }
    private static byte[] CryptLineData(byte[] data, ushort key)
    {
        byte[] result = new byte[data.Length];
        for (int i = 0; i < result.Length; i += 2)
        {
            BitConverter.GetBytes((ushort)(BitConverter.ToUInt16(data, i) ^ key)).CopyTo(result, i);
            key = (ushort)(key << 3 | key >> 13);
        }
        return result;
    }

    private static byte[] GetLineData(string? line)
    {
        if (line == null)
            return new byte[2];

        MemoryStream ms = new();
        using BinaryWriter bw = new(ms);
        int i = 0;
        while (i < line.Length)
        {
            ushort val = line[i++];

            switch (val)
            {
                // Variable
                case '[':
                    {
                        // grab the string
                        int bracket = line.IndexOf(']', i);
                        if (bracket < 0)
                            throw new ArgumentException("Variable text is not capped properly: " + line);
                        string varText = line[i..bracket];
                        var varValues = GetVariableValues(varText);
                        foreach (ushort v in varValues) bw.Write(v);
                        i += 1 + varText.Length;
                        break;
                    }
                // Escaped Formatting
                case '\\':
                    {
                        var escapeValues = GetEscapeValues(line[i++]);
                        foreach (ushort v in escapeValues) bw.Write(v);
                        break;
                    }
                case '♪':
                    bw.Write((ushort)0xE09A); // Special character for "♪" in the game.
                    break;
                default:
                    bw.Write(val);
                    break;
            }
        }
        bw.Write(KEY_TERMINATOR); // cap the line off
        return ms.ToArray();
    }

    private static List<ushort> GetEscapeValues(char esc)
    {
        var vals = new List<ushort>();
        switch (esc)
        {
            case 'n': vals.Add('\n'); return vals;
            case '\\': vals.Add('\\'); return vals;
            case '[': vals.Add('['); return vals;
            case ']': vals.Add(']'); return vals;
            case 'r': vals.AddRange([KEY_VARIABLE, 1, KEY_TEXTRETURN]); return vals;
            case 'c': vals.AddRange([KEY_VARIABLE, 1, KEY_TEXTCLEAR]); return vals;
            default: throw new Exception("Invalid terminated line: \\" + esc);
        }
    }

    private static List<ushort> GetVariableValues(string variable)
    {
        string[] split = variable.Split(' ');
        if (split.Length < 2)
            throw new ArgumentException("Incorrectly formatted variable text: " + variable);

        var vals = new List<ushort> { KEY_VARIABLE };
        switch (split[0])
        {
            case "~": // Blank Text Line Variable (No text set - debug/quality testing variable?)
                vals.Add(1);
                vals.Add(KEY_TEXTNULL);
                vals.Add(Convert.ToUInt16(split[1]));
                break;
            case "WAIT": // Event pause Variable.
                vals.Add(1);
                vals.Add(KEY_TEXTWAIT);
                vals.Add(Convert.ToUInt16(split[1]));
                break;
            case "VAR": // Text Variable
                vals.AddRange(GetVariableParameters(split[1]));
                break;
            default: throw new Exception("Unknown variable method type: " + variable);
        }
        return vals;
    }

    private static List<ushort> GetVariableParameters(string text)
    {
        var vals = new List<ushort>();
        int bracket = text.IndexOf('(');
        bool noArgs = bracket < 0;
        string variable = noArgs ? text : text[..bracket];
        ushort varVal = GetVariableNumber(variable);

        if (!noArgs)
        {
            string[] args = text.Substring(bracket + 1, text.Length - bracket - 2).Split(',');
            vals.Add((ushort)(1 + args.Length));
            vals.Add(varVal);
            vals.AddRange(args.Select(t => Convert.ToUInt16(t, 16)));
        }
        else
        {
            vals.Add(1);
            vals.Add(varVal);
        }
        return vals;
    }

    private static ushort GetVariableNumber(string variable)
    {
        try
        {
            return Convert.ToUInt16(variable, 16);
        }
        catch { throw new ArgumentException("Variable parse error: " + variable); }
    }

}
