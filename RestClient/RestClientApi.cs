using System;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using RestClient;

namespace RestClient
{

    public class UnixDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long unixTime = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            long unixTime = new DateTimeOffset(value).ToUnixTimeSeconds();
            writer.WriteNumberValue(unixTime);
        }
    }

    public class RestClientMessageHandler : DelegatingHandler
    {
        public RestClientMessageHandler(HttpClientHandler nextHandler)
        {
            InnerHandler = nextHandler;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
        #region private
        #endregion
    }

    public interface IFormatter
    {
        string ToString<T>(T value, int option = 0);
    }

    public abstract class ParameterBlock
    {
        public ParameterBlock() { }
        abstract public void AddToQueryParameters(QueryParameters parms);
    }


    public class QueryParameters
    {
        public bool Add(string name, string value)
        {
            if (_parms.ContainsKey(name)) { return false; }
            if (_parent != null) { if (_parent._parms.ContainsKey(name)) { return false; } }

            _parms.Add(name, value);

            return false;
        }
        public bool Add<T>(string name, T? value, int option = 0)
        {
            if (value == null) { return true; }
            return Add(name, _formatter.ToString<T>(value, option));
        }

        #region internal
        internal QueryParameters(IFormatter formatter, QueryParameters parent = null)
        {
            _parms = new Dictionary<string, string>();
            _parent = parent;
            _string = new StringBuilder();
            _formatter = formatter;
        }

        internal string generateUrlComponent()
        {
            _string.Clear();
            int parentCount = 0;
            if (_parent != null) { parentCount = _parent._parms.Count; }
            int childCount = _parms.Count;

            if (parentCount == 0 && childCount == 0) { return ""; }
            _string.Append('?');
            if (parentCount != 0) { _append(_parent._parms, _string); }
            if (childCount > 0) { if (parentCount != 0) { _string.Append('&'); } _append(_parms, _string); }

            return _string.ToString();
        }
        internal void clear()
        {
            _parms.Clear();
        }
        internal QueryParameters parent { get { return _parent; } }
        #endregion
        #region private
        private void _append(Dictionary<string, string> parms, StringBuilder sb)
        {
            bool first = true;
            foreach (var ele in parms)
            {
                if (!first)
                {
                    sb.Append('&');
                }
                first = false;
                sb.Append(ele.Key);
                sb.Append("=");
                sb.Append(ele.Value);
            }
        }
        private Dictionary<string, string> _parms;
        private QueryParameters _parent;
        private StringBuilder _string;
        private IFormatter _formatter;
        #endregion
    }

    public class ApiResponse<T>
    {
        public ApiResponse(HttpStatusCode httpStatusCode, string content, T? response, string? errorMessage = null, Exception? exception = null) 
        { 
            _httpStatusCode = httpStatusCode;
            _content = content;
            _response = response;
            _errorMessage = errorMessage;
            _exception = exception;
        }
        public HttpStatusCode HttpStatusCode { get { return _httpStatusCode; } }
        public string Content { get { return _content; } }  
        public T? Response { get { return _response; } }
        public string? ErrorMessage { get { return _errorMessage; } }   
        public Exception? Exception { get { return _exception; } }
        public bool Ok { get { if (_httpStatusCode == HttpStatusCode.OK && _errorMessage == null) { return true; }  return false; } }
        #region private
        private HttpStatusCode _httpStatusCode;
        private string _content;
        private T? _response;
        private string? _errorMessage;
        private Exception? _exception;
        #endregion
    }



    public sealed class RestClientApi : IDisposable
    {
        public RestClientApi(string domain, IFormatter formatter, int defaultTimeout = 600000)
        {
            _messageHandler = new RestClientMessageHandler(new HttpClientHandler());   
            _httpClient = new HttpClient(_messageHandler);
            _httpClient.BaseAddress = new Uri(domain);
            _defaultQueryParameters = new QueryParameters(formatter);
            _queryParameters = new QueryParameters(formatter, _defaultQueryParameters);

            _serializerOptions = new JsonSerializerOptions();
            _serializerOptions.Converters.Add(new UnixDateTimeConverter());

            _defaultCancelTokenSource = new CancellationTokenSource(defaultTimeout);

          }
        public HttpRequestHeaders DefaultRequestHeaders
        {
            get { return _httpClient.DefaultRequestHeaders; }
        }
        public QueryParameters DefaultQueryParameters { get { return _defaultQueryParameters; } }
        public QueryParameters QueryParameters { get { return _queryParameters; } }

        public async Task<ApiResponse<T>> CallApi<T>(string relativeUrl, bool deserialize = true)
        {
            var ret =  await CallApi<T>(relativeUrl, _defaultCancelTokenSource.Token, deserialize);
            return ret;
        }

        public async Task<ApiResponse<T>> CallApiUrl<T>(string url, bool deserialize = true)
        {
            var ret = await CallApiUrl<T>(url, _defaultCancelTokenSource.Token, deserialize);
            return ret;
        }

        public async Task<ApiResponse<T>> CallApi<T>(string relativeUrl, CancellationToken cancel, bool deserialize = true)
        {
            // set up URL and Query parameters
            var relativeUrlQuery = relativeUrl + _queryParameters.generateUrlComponent();
            _queryParameters.clear();
            HttpResponseMessage response;

            // Do the Http call 
            try
            {
                response = await _httpClient.GetAsync(relativeUrlQuery, cancel);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception("The requestUri must be an absolute URI or BaseAddress must be set.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception("The request failed due to timeout.", ex);
            }
            catch (UriFormatException ex)
            {
                throw new Exception("The provided request URI is not valid relative or absolute URI.", ex);
            }


            // Retrieve the content
            string content;
            try
            {
                content = await response.Content.ReadAsStringAsync(cancel);
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>(response.StatusCode, null, default(T), "Error in reading response content", ex);
            }

            // Attempt to deserialize
            T? deserializedObject = default(T);
            try
            {
                if (deserialize)
                {
                    deserializedObject = JsonSerializer.Deserialize<T>(content, _serializerOptions);
                }
                return new ApiResponse<T>(response.StatusCode, content, deserializedObject);
            }
            catch (ArgumentNullException ex)
            {
                return new ApiResponse<T>(response.StatusCode, content, default(T), "The JSON is invalid. OR TResponse is not compatible with the JSON. OR There is remaining data in the string beyond a single JSON value.", ex);
            }
            catch (NotSupportedException ex)
            {
                return new ApiResponse<T>(response.StatusCode, content, default(T), "There is no compatible JsonConverter for TResponse or its serializable members.", ex);
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>(response.StatusCode, content, default(T), "Unknow JSON Exception - Examine inner exception.", ex);
            }

         }
        public async Task<ApiResponse<T>> CallApiUrl<T>(string url, CancellationToken cancel, bool deserialize = true)
        {
            HttpResponseMessage response;
            // Do the Http call 
            try
            {
                response = await _httpClient.GetAsync(url, cancel);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception("The requestUri must be an absolute URI or BaseAddress must be set.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception("The request failed due to timeout.", ex);
            }
            catch (UriFormatException ex)
            {
                throw new Exception("The provided request URI is not valid relative or absolute URI.", ex);
            }


            // Retrieve the content
            string content;
            try
            {
                content = await response.Content.ReadAsStringAsync(cancel);
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>(response.StatusCode, null, default(T), "Error in reading response content", ex);
            }

            // Attempt to deserialize
            T? deserializedObject = default(T);
            try
            {
                if (deserialize)
                {
                    deserializedObject = JsonSerializer.Deserialize<T>(content, _serializerOptions);
                }
                return new ApiResponse<T>(response.StatusCode, content, deserializedObject);
            }
            catch (ArgumentNullException ex)
            {
                return new ApiResponse<T>(response.StatusCode, content, default(T), "The JSON is invalid. OR TResponse is not compatible with the JSON. OR There is remaining data in the string beyond a single JSON value.", ex);
            }
            catch (NotSupportedException ex)
            {
                return new ApiResponse<T>(response.StatusCode, content, default(T), "There is no compatible JsonConverter for TResponse or its serializable members.", ex);
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>(response.StatusCode, content, default(T), "Unknow JSON Exception - Examine inner exception.", ex);
            }

        }
        public async Task<ApiResponse<string>> Download(string sourceUrl, string destFile)
        {
            return await Download(sourceUrl, destFile, _defaultCancelTokenSource.Token);
        }
        public async Task<ApiResponse<string>> Download(string sourceUrl, string destFile, CancellationToken cancel)
        {
            using var source = await _httpClient.GetStreamAsync(sourceUrl, cancel);
            using var dest = new FileStream(destFile, FileMode.Create);
            await source.CopyToAsync(dest, cancel);
            dest.Close();
            source.Close();
            return new ApiResponse<string>(HttpStatusCode.OK, destFile, destFile);
        }
        public void Decompress(string sourceFile, string destFile)
        {
            using var compressedFileStream = new FileStream(sourceFile, FileMode.Open);
            using var outputFileStream = new FileStream(destFile, FileMode.Create);
            using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
            decompressor.CopyTo(outputFileStream);
            compressedFileStream.Close();
            outputFileStream.Close();
            return;
        }
        public void Dispose()
        {
            _httpClient.Dispose();
        }
        public CancellationTokenSource CancellationTokenSource {  get { return _defaultCancelTokenSource; } }   
        #region private
        private HttpClient _httpClient;
        private RestClientMessageHandler _messageHandler;
        private QueryParameters _defaultQueryParameters;
        private QueryParameters _queryParameters;
        private JsonSerializerOptions _serializerOptions;
        private CancellationTokenSource _defaultCancelTokenSource;
        #endregion
    }
}
