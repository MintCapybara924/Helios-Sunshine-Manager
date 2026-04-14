using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Helios.Core.Update;

[JsonSerializable(typeof(GitHubReleaseDto))]
[JsonSerializable(typeof(GitHubReleaseDto[]))]
[GeneratedCode("System.Text.Json.SourceGeneration", "8.0.12.47513")]
internal class GitHubJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<List<GitHubAssetDto>>? _ListGitHubAssetDto;

	private JsonTypeInfo<DateTimeOffset>? _DateTimeOffset;

	private JsonTypeInfo<GitHubAssetDto>? _GitHubAssetDto;

	private JsonTypeInfo<GitHubReleaseDto>? _GitHubReleaseDto;

	private JsonTypeInfo<GitHubReleaseDto[]>? _GitHubReleaseDtoArray;

	private JsonTypeInfo<long>? _Int64;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions();

	private static readonly JsonEncodedText PropName_name = JsonEncodedText.Encode("name");

	private static readonly JsonEncodedText PropName_browser_download_url = JsonEncodedText.Encode("browser_download_url");

	private static readonly JsonEncodedText PropName_size = JsonEncodedText.Encode("size");

	private static readonly JsonEncodedText PropName_tag_name = JsonEncodedText.Encode("tag_name");

	private static readonly JsonEncodedText PropName_prerelease = JsonEncodedText.Encode("prerelease");

	private static readonly JsonEncodedText PropName_published_at = JsonEncodedText.Encode("published_at");

	private static readonly JsonEncodedText PropName_body = JsonEncodedText.Encode("body");

	private static readonly JsonEncodedText PropName_assets = JsonEncodedText.Encode("assets");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<List<GitHubAssetDto>> ListGitHubAssetDto => _ListGitHubAssetDto ?? (_ListGitHubAssetDto = (JsonTypeInfo<List<GitHubAssetDto>>)base.Options.GetTypeInfo(typeof(List<GitHubAssetDto>)));

	public JsonTypeInfo<DateTimeOffset> DateTimeOffset => _DateTimeOffset ?? (_DateTimeOffset = (JsonTypeInfo<DateTimeOffset>)base.Options.GetTypeInfo(typeof(DateTimeOffset)));

	public JsonTypeInfo<GitHubAssetDto> GitHubAssetDto => _GitHubAssetDto ?? (_GitHubAssetDto = (JsonTypeInfo<GitHubAssetDto>)base.Options.GetTypeInfo(typeof(GitHubAssetDto)));

	public JsonTypeInfo<GitHubReleaseDto> GitHubReleaseDto => _GitHubReleaseDto ?? (_GitHubReleaseDto = (JsonTypeInfo<GitHubReleaseDto>)base.Options.GetTypeInfo(typeof(GitHubReleaseDto)));

	public JsonTypeInfo<GitHubReleaseDto[]> GitHubReleaseDtoArray => _GitHubReleaseDtoArray ?? (_GitHubReleaseDtoArray = (JsonTypeInfo<GitHubReleaseDto[]>)base.Options.GetTypeInfo(typeof(GitHubReleaseDto[])));

	public JsonTypeInfo<long> Int64 => _Int64 ?? (_Int64 = (JsonTypeInfo<long>)base.Options.GetTypeInfo(typeof(long)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static GitHubJsonContext Default { get; } = new GitHubJsonContext(new JsonSerializerOptions(s_defaultOptions));


	protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;


	private JsonTypeInfo<bool> Create_Boolean(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<bool> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<bool>(options, JsonMetadataServices.BooleanConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<List<GitHubAssetDto>> Create_ListGitHubAssetDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<GitHubAssetDto>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<GitHubAssetDto>> collectionInfo = new JsonCollectionInfoValues<List<GitHubAssetDto>>
			{
				ObjectCreator = () => new List<GitHubAssetDto>(),
				SerializeHandler = ListGitHubAssetDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<GitHubAssetDto>, GitHubAssetDto>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListGitHubAssetDtoSerializeHandler(Utf8JsonWriter writer, List<GitHubAssetDto>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			GitHubAssetDtoSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<DateTimeOffset> Create_DateTimeOffset(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DateTimeOffset> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<DateTimeOffset>(options, JsonMetadataServices.DateTimeOffsetConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<GitHubAssetDto> Create_GitHubAssetDto(JsonSerializerOptions options)
	{
		JsonSerializerOptions options2 = options;
		if (!TryGetTypeInfoForRuntimeCustomConverter(options2, out JsonTypeInfo<GitHubAssetDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<GitHubAssetDto> objectInfo = new JsonObjectInfoValues<GitHubAssetDto>
			{
				ObjectCreator = () => new GitHubAssetDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => GitHubAssetDtoPropInit(options2),
				ConstructorParameterMetadataInitializer = null,
				SerializeHandler = GitHubAssetDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options2, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] GitHubAssetDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubAssetDto),
			Converter = null,
			Getter = (object obj) => ((GitHubAssetDto)obj).Name,
			Setter = delegate(object obj, string? value)
			{
				((GitHubAssetDto)obj).Name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Name",
			JsonPropertyName = "name"
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubAssetDto),
			Converter = null,
			Getter = (object obj) => ((GitHubAssetDto)obj).BrowserDownloadUrl,
			Setter = delegate(object obj, string? value)
			{
				((GitHubAssetDto)obj).BrowserDownloadUrl = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "BrowserDownloadUrl",
			JsonPropertyName = "browser_download_url"
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<long> propertyInfo3 = new JsonPropertyInfoValues<long>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubAssetDto),
			Converter = null,
			Getter = (object obj) => ((GitHubAssetDto)obj).Size,
			Setter = delegate(object obj, long value)
			{
				((GitHubAssetDto)obj).Size = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Size",
			JsonPropertyName = "size"
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void GitHubAssetDtoSerializeHandler(Utf8JsonWriter writer, GitHubAssetDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_name, value.Name);
		writer.WriteString(PropName_browser_download_url, value.BrowserDownloadUrl);
		writer.WriteNumber(PropName_size, value.Size);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<GitHubReleaseDto> Create_GitHubReleaseDto(JsonSerializerOptions options)
	{
		JsonSerializerOptions options2 = options;
		if (!TryGetTypeInfoForRuntimeCustomConverter(options2, out JsonTypeInfo<GitHubReleaseDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<GitHubReleaseDto> objectInfo = new JsonObjectInfoValues<GitHubReleaseDto>
			{
				ObjectCreator = () => new GitHubReleaseDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => GitHubReleaseDtoPropInit(options2),
				ConstructorParameterMetadataInitializer = null,
				SerializeHandler = GitHubReleaseDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options2, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] GitHubReleaseDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[6];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubReleaseDto),
			Converter = null,
			Getter = (object obj) => ((GitHubReleaseDto)obj).TagName,
			Setter = delegate(object obj, string? value)
			{
				((GitHubReleaseDto)obj).TagName = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "TagName",
			JsonPropertyName = "tag_name"
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubReleaseDto),
			Converter = null,
			Getter = (object obj) => ((GitHubReleaseDto)obj).ReleaseName,
			Setter = delegate(object obj, string? value)
			{
				((GitHubReleaseDto)obj).ReleaseName = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ReleaseName",
			JsonPropertyName = "name"
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubReleaseDto),
			Converter = null,
			Getter = (object obj) => ((GitHubReleaseDto)obj).PreRelease,
			Setter = delegate(object obj, bool value)
			{
				((GitHubReleaseDto)obj).PreRelease = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PreRelease",
			JsonPropertyName = "prerelease"
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<DateTimeOffset> propertyInfo4 = new JsonPropertyInfoValues<DateTimeOffset>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubReleaseDto),
			Converter = null,
			Getter = (object obj) => ((GitHubReleaseDto)obj).PublishedAt,
			Setter = delegate(object obj, DateTimeOffset value)
			{
				((GitHubReleaseDto)obj).PublishedAt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PublishedAt",
			JsonPropertyName = "published_at"
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubReleaseDto),
			Converter = null,
			Getter = (object obj) => ((GitHubReleaseDto)obj).Body,
			Setter = delegate(object obj, string? value)
			{
				((GitHubReleaseDto)obj).Body = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Body",
			JsonPropertyName = "body"
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<List<GitHubAssetDto>> propertyInfo6 = new JsonPropertyInfoValues<List<GitHubAssetDto>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GitHubReleaseDto),
			Converter = null,
			Getter = (object obj) => ((GitHubReleaseDto)obj).Assets,
			Setter = delegate(object obj, List<GitHubAssetDto>? value)
			{
				((GitHubReleaseDto)obj).Assets = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Assets",
			JsonPropertyName = "assets"
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		return array;
	}

	private void GitHubReleaseDtoSerializeHandler(Utf8JsonWriter writer, GitHubReleaseDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_tag_name, value.TagName);
		writer.WriteString(PropName_name, value.ReleaseName);
		writer.WriteBoolean(PropName_prerelease, value.PreRelease);
		writer.WriteString(PropName_published_at, value.PublishedAt);
		writer.WriteString(PropName_body, value.Body);
		writer.WritePropertyName(PropName_assets);
		ListGitHubAssetDtoSerializeHandler(writer, value.Assets);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<GitHubReleaseDto[]> Create_GitHubReleaseDtoArray(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<GitHubReleaseDto[]> jsonTypeInfo))
		{
			JsonCollectionInfoValues<GitHubReleaseDto[]> collectionInfo = new JsonCollectionInfoValues<GitHubReleaseDto[]>
			{
				ObjectCreator = null,
				SerializeHandler = GitHubReleaseDtoArraySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateArrayInfo(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void GitHubReleaseDtoArraySerializeHandler(Utf8JsonWriter writer, GitHubReleaseDto[]? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Length; i++)
		{
			GitHubReleaseDtoSerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<long> Create_Int64(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<long> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<long>(options, JsonMetadataServices.Int64Converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<string> Create_String(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<string> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	public GitHubJsonContext()
		: base(null)
	{
	}

	public GitHubJsonContext(JsonSerializerOptions options)
		: base(options)
	{
	}

	private static bool TryGetTypeInfoForRuntimeCustomConverter<TJsonMetadataType>(JsonSerializerOptions options, out JsonTypeInfo<TJsonMetadataType> jsonTypeInfo)
	{
		JsonConverter runtimeConverterForType = GetRuntimeConverterForType(typeof(TJsonMetadataType), options);
		if (runtimeConverterForType != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<TJsonMetadataType>(options, runtimeConverterForType);
			return true;
		}
		jsonTypeInfo = null;
		return false;
	}

	private static JsonConverter? GetRuntimeConverterForType(Type type, JsonSerializerOptions options)
	{
		for (int i = 0; i < options.Converters.Count; i++)
		{
			JsonConverter jsonConverter = options.Converters[i];
			if (jsonConverter != null && jsonConverter.CanConvert(type))
			{
				return ExpandConverter(type, jsonConverter, options, validateCanConvert: false);
			}
		}
		return null;
	}

	private static JsonConverter ExpandConverter(Type type, JsonConverter converter, JsonSerializerOptions options, bool validateCanConvert = true)
	{
		if (validateCanConvert && !converter.CanConvert(type))
		{
			throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
		}
		if (converter is JsonConverterFactory jsonConverterFactory)
		{
			converter = jsonConverterFactory.CreateConverter(type, options);
			if (converter == null || converter is JsonConverterFactory)
			{
				throw new InvalidOperationException($"The converter '{jsonConverterFactory.GetType()}' cannot return null or a JsonConverterFactory instance.");
			}
		}
		return converter;
	}

	public override JsonTypeInfo? GetTypeInfo(Type type)
	{
		base.Options.TryGetTypeInfo(type, out JsonTypeInfo typeInfo);
		return typeInfo;
	}

	JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == typeof(bool))
		{
			return Create_Boolean(options);
		}
		if (type == typeof(List<GitHubAssetDto>))
		{
			return Create_ListGitHubAssetDto(options);
		}
		if (type == typeof(DateTimeOffset))
		{
			return Create_DateTimeOffset(options);
		}
		if (type == typeof(GitHubAssetDto))
		{
			return Create_GitHubAssetDto(options);
		}
		if (type == typeof(GitHubReleaseDto))
		{
			return Create_GitHubReleaseDto(options);
		}
		if (type == typeof(GitHubReleaseDto[]))
		{
			return Create_GitHubReleaseDtoArray(options);
		}
		if (type == typeof(long))
		{
			return Create_Int64(options);
		}
		if (type == typeof(string))
		{
			return Create_String(options);
		}
		return null;
	}
}

