﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nitrocoin.Bitcoin.Utilities;

namespace Nitrocoin.Bitcoin.RPC
{
	public interface IRPCJsonOutputFormatter
	{
		void WriteObject(TextWriter writer, object value);
		Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding);
	}

	public class RPCJsonOutputFormatter : TextOutputFormatter, IRPCJsonOutputFormatter
	{
		private readonly IArrayPool<char> _charPool;

		private JsonSerializer _serializer;

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to configure the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
		/// </summary>
		/// <remarks>
		/// Any modifications to the <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> object after this
		/// <see cref="T:Microsoft.AspNetCore.Mvc.Formatters.JsonOutputFormatter" /> has been used will have no effect.
		/// </remarks>
		protected JsonSerializerSettings SerializerSettings
		{
			get;set;
		}

		/// <summary>
		/// Initializes a new <see cref="T:Microsoft.AspNetCore.Mvc.Formatters.JsonOutputFormatter" /> instance.
		/// </summary>
		/// <param name="serializerSettings">
		/// The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />. Should be either the application-wide settings
		/// (<see cref="P:Microsoft.AspNetCore.Mvc.MvcJsonOptions.SerializerSettings" />) or an instance
		/// <see cref="M:Microsoft.AspNetCore.Mvc.Formatters.JsonSerializerSettingsProvider.CreateSerializerSettings" /> initially returned.
		/// </param>
		/// <param name="charPool">The <see cref="T:System.Buffers.ArrayPool`1" />.</param>
		public RPCJsonOutputFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool)
		{
			Guard.NotNull(serializerSettings, nameof(serializerSettings));
			Guard.NotNull(charPool, nameof(charPool));
			
			SerializerSettings = serializerSettings;
			this._charPool = new JsonArrayPool<char>(charPool);
			base.SupportedEncodings.Add(Encoding.UTF8);
			base.SupportedEncodings.Add(Encoding.Unicode);
			base.SupportedMediaTypes.Add(ApplicationJson);
			base.SupportedMediaTypes.Add(TextJson);
		}

		public static readonly MediaTypeHeaderValue ApplicationJson = MediaTypeHeaderValue.Parse("application/json").CopyAsReadOnly();

		public static readonly MediaTypeHeaderValue TextJson = MediaTypeHeaderValue.Parse("text/json").CopyAsReadOnly();

		public static readonly MediaTypeHeaderValue ApplicationJsonPatch = MediaTypeHeaderValue.Parse("application/json-patch+json").CopyAsReadOnly();

		/// <summary>
		/// Writes the given <paramref name="value" /> as JSON using the given
		/// <paramref name="writer" />.
		/// </summary>
		/// <param name="writer">The <see cref="T:System.IO.TextWriter" /> used to write the <paramref name="value" /></param>
		/// <param name="value">The value to write as JSON.</param>
		public void WriteObject(TextWriter writer, object value)
		{
			Guard.NotNull(writer, nameof(writer));

			using (JsonWriter jsonWriter = this.CreateJsonWriter(writer))
			{
				this.CreateJsonSerializer().Serialize(jsonWriter, value);
			}
		}

		/// <summary>
		/// Called during serialization to create the <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="writer">The <see cref="T:System.IO.TextWriter" /> used to write.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.JsonWriter" /> used during serialization.</returns>
		protected virtual JsonWriter CreateJsonWriter(TextWriter writer)
		{			
			Guard.NotNull(writer, nameof(writer));

			JsonTextWriter jsonTextWriter = new JsonTextWriter(writer);
			jsonTextWriter.ArrayPool = this._charPool;
			jsonTextWriter.CloseOutput = false;
			return jsonTextWriter;
		}

		/// <summary>
		/// Called during serialization to create the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
		/// </summary>
		/// <returns>The <see cref="T:Newtonsoft.Json.JsonSerializer" /> used during serialization and deserialization.</returns>
		protected virtual JsonSerializer CreateJsonSerializer()
		{
			if(this._serializer == null)
			{
				this._serializer = JsonSerializer.Create(this.SerializerSettings);
			}

			return this._serializer;
		}

		/// <inheritdoc />
		public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
		{
			Guard.NotNull(context, nameof(context));
			Guard.NotNull(selectedEncoding, nameof(selectedEncoding));          

			MemoryStream result = new MemoryStream();
			using(var writer = context.WriterFactory(result, selectedEncoding))
			{
				WriteObject(writer, context.Object);

				// Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
				// buffers. This is better than just letting dispose handle it (which would result in a synchronous
				// write).
				await writer.FlushAsync();
			}
			result.Position = 0;
			var jsonResult = JToken.Load(new JsonTextReader(new StreamReader(result)));
			//{"result":null,"error":{"code":-32601,"message":"Method not found"},"id":1}
			JObject response = new JObject();
			response["result"] = jsonResult;
			response["id"] = 1;
			response["error"] = null;
			using(var writer = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding))
			{
				WriteObject(writer, response);

				// Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
				// buffers. This is better than just letting dispose handle it (which would result in a synchronous
				// write).
				await writer.FlushAsync();
			}
		}
	}
}