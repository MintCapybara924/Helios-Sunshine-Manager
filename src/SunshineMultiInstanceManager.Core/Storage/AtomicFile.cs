using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SunshineMultiInstanceManager.Core.Storage;

public static class AtomicFile
{
	private static readonly UTF8Encoding s_utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public static async Task<T?> ReadJsonAsync<T>(string path, T? defaultValue = default(T?), CancellationToken ct = default(CancellationToken))
	{
		if (!File.Exists(path))
		{
			return defaultValue;
		}
		FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
		T result;
		try
		{
			T val = await JsonSerializer.DeserializeAsync<T>(stream, s_jsonOptions, ct);
			result = (T)((val != null) ? ((object)val) : ((object)defaultValue));
		}
		finally
		{
			if (stream != null)
			{
				await stream.DisposeAsync();
			}
		}
		return result;
	}

	public static T? ReadJson<T>(string path, T? defaultValue = default(T?))
	{
		if (!File.Exists(path))
		{
			return defaultValue;
		}
		using FileStream utf8Json = File.OpenRead(path);
		T val = JsonSerializer.Deserialize<T>(utf8Json, s_jsonOptions);
		return (T?)((val != null) ? ((object)val) : ((object)defaultValue));
	}

	public static async Task WriteJsonAsync<T>(string path, T value, CancellationToken ct = default(CancellationToken))
	{
		string directoryName = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		string tmpPath = path + ".tmp";
		string s = JsonSerializer.Serialize(value, s_jsonOptions);
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		FileStream fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
		try
		{
			await fs.WriteAsync(bytes, ct);
			await fs.FlushAsync(ct);
		}
		finally
		{
			if (fs != null)
			{
				await fs.DisposeAsync();
			}
		}
		File.Move(tmpPath, path, overwrite: true);
	}

	public static void WriteJson<T>(string path, T value)
	{
		string directoryName = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		string text = path + ".tmp";
		string contents = JsonSerializer.Serialize(value, s_jsonOptions);
		File.WriteAllText(text, contents, Encoding.UTF8);
		File.Move(text, path, overwrite: true);
	}

	public static Dictionary<string, string> ReadConf(string path)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (!File.Exists(path))
		{
			return dictionary;
		}
		foreach (string item in File.ReadLines(path, Encoding.UTF8))
		{
			string text = item.Trim();
			if (text.Length == 0 || text[0] == '#' || text[0] == ';')
			{
				continue;
			}
			int num = text.IndexOf('=');
			if (num > 0)
			{
				string key = text.Substring(0, num).Trim().TrimStart('\uFEFF');
				string text2 = text;
				int num2 = num + 1;
				string text3 = text2.Substring(num2, text2.Length - num2).Trim();
				if (text3.StartsWith('[') && text3.EndsWith(']'))
				{
					text2 = text3;
					text3 = text2.Substring(1, text2.Length - 1 - 1);
				}
				dictionary[key] = text3;
			}
		}
		return dictionary;
	}

	public static bool WriteConf(string path, Dictionary<string, string> conf)
	{
		string directoryName = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (var (value, value2) in conf)
		{
			stringBuilder.Append(value).Append(" = ").AppendLine(value2);
		}
		string text3 = stringBuilder.ToString();
		if (File.Exists(path) && File.ReadAllText(path, s_utf8NoBom) == text3)
		{
			return false;
		}
		string text4 = path + ".tmp";
		File.WriteAllText(text4, text3, s_utf8NoBom);
		File.Move(text4, path, overwrite: true);
		return true;
	}

	public static void SafeDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch
		{
		}
	}

	public static void CleanupTempFiles(string directory)
	{
		if (!Directory.Exists(directory))
		{
			return;
		}
		foreach (string item in Directory.EnumerateFiles(directory, "*.tmp"))
		{
			SafeDelete(item);
		}
	}
}
